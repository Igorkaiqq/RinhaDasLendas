using MediatR;
using RinhaDasLendas.Application.Commands.AgendamentosPresenca;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.AgendamentosPresenca;

public sealed class PausarAgendamentoPresencaCommandHandler(
    IAgendamentoPresencaRepository repository,
    ISystemClock clock)
    : IRequestHandler<PausarAgendamentoPresencaCommand, AgendamentoPresencaSummaryDto?>
{
    public async Task<AgendamentoPresencaSummaryDto?> Handle(
        PausarAgendamentoPresencaCommand command,
        CancellationToken cancellationToken)
    {
        var agenda = await repository.GetByIdAsync(command.Id, true, cancellationToken);
        if (agenda is null || agenda.Status == AgendamentoPresencaStatus.Arquivado)
        {
            return null;
        }

        agenda.Pausar(command.ResponsavelUsuarioId, clock.UtcNow);
        await repository.SaveChangesAsync(cancellationToken);
        return await AgendamentoPresencaHandlerHelpers.GetSummaryAsync(repository, agenda.Id, cancellationToken);
    }
}
