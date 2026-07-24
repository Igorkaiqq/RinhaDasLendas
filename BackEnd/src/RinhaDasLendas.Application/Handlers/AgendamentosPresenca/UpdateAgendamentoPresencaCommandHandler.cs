using FluentValidation;
using MediatR;
using RinhaDasLendas.Application.Commands.AgendamentosPresenca;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.AgendamentosPresenca;

public sealed class UpdateAgendamentoPresencaCommandHandler(
    IAgendamentoPresencaRepository repository,
    IValidator<SaveAgendamentoPresencaRequestDto> validator,
    ISystemClock clock,
    IAgendamentoPresencaTimeZone timeZone)
    : IRequestHandler<UpdateAgendamentoPresencaCommand, AgendamentoPresencaSummaryDto?>
{
    public async Task<AgendamentoPresencaSummaryDto?> Handle(
        UpdateAgendamentoPresencaCommand command,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command.Request, cancellationToken);
        var agenda = await repository.GetByIdAsync(command.Id, true, cancellationToken);
        if (agenda is null || agenda.Status == AgendamentoPresencaStatus.Arquivado)
        {
            return null;
        }

        agenda.Editar(
            command.Request.Nome,
            command.Request.Observacao,
            command.Request.HorarioPublicacao,
            command.Request.HorarioEncerramento,
            command.Request.DiasSemana,
            command.ResponsavelUsuarioId,
            clock.UtcNow);
        await repository.SaveChangesAsync(cancellationToken);
        return AgendamentoPresencaSummaryDto.FromEntity(agenda, timeZone);
    }
}
