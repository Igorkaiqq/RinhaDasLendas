using MediatR;
using RinhaDasLendas.Application.Commands.AgendamentosPresenca;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.AgendamentosPresenca;

public sealed class ReativarAgendamentoPresencaCommandHandler(
    IAgendamentoPresencaRepository repository,
    IAgendamentoPresencaTimeZone timeZone,
    ISystemClock clock) : IRequestHandler<ReativarAgendamentoPresencaCommand, AgendamentoPresencaSummaryDto?>
{
    public async Task<AgendamentoPresencaSummaryDto?> Handle(
        ReativarAgendamentoPresencaCommand command,
        CancellationToken cancellationToken)
    {
        var agenda = await repository.GetByIdAsync(command.Id, true, cancellationToken);
        if (agenda is null || agenda.Status == AgendamentoPresencaStatus.Arquivado)
        {
            return null;
        }

        var wasPaused = agenda.Status == AgendamentoPresencaStatus.Pausado;
        var now = clock.UtcNow;
        agenda.Reativar(command.ResponsavelUsuarioId, now);
        if (wasPaused)
        {
            agenda.MarcarDataAvaliada(
                AgendamentoPresencaHandlerHelpers.CalculateInitialMarker(agenda, now, timeZone),
                now);
        }

        await repository.SaveChangesAsync(cancellationToken);
        return AgendamentoPresencaSummaryDto.FromEntity(agenda, timeZone);
    }
}
