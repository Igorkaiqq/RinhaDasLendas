using FluentValidation;
using MediatR;
using RinhaDasLendas.Application.Commands.AgendamentosPresenca;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.AgendamentosPresenca;

public sealed class CreateAgendamentoPresencaCommandHandler(
    IAgendamentoPresencaRepository repository,
    IValidator<SaveAgendamentoPresencaRequestDto> validator,
    IAgendamentoPresencaTimeZone timeZone,
    ISystemClock clock) : IRequestHandler<CreateAgendamentoPresencaCommand, AgendamentoPresencaSummaryDto>
{
    public async Task<AgendamentoPresencaSummaryDto> Handle(
        CreateAgendamentoPresencaCommand command,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command.Request, cancellationToken);
        var now = clock.UtcNow;
        var localDate = timeZone.GetLocalDate(now);
        var publicationAt = timeZone.ToUtc(localDate, command.Request.HorarioPublicacao);
        var initialMarker = now <= publicationAt ? localDate.AddDays(-1) : localDate;
        var agenda = new AgendamentoPresenca(
            command.Request.Nome,
            command.Request.Observacao,
            command.Request.HorarioPublicacao,
            command.Request.HorarioEncerramento,
            command.Request.DiasSemana,
            initialMarker,
            command.ResponsavelUsuarioId,
            now);

        await repository.AddAsync(agenda, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return (await AgendamentoPresencaHandlerHelpers.GetSummaryAsync(repository, agenda.Id, cancellationToken))!;
    }
}
