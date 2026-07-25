using MediatR;
using RinhaDasLendas.Application.Commands.AgendamentosPresenca;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.AgendamentosPresenca;

public sealed class ArquivarAgendamentoPresencaCommandHandler(
    IAgendamentoPresencaRepository repository,
    ISystemClock clock) : IRequestHandler<ArquivarAgendamentoPresencaCommand, bool>
{
    public async Task<bool> Handle(ArquivarAgendamentoPresencaCommand command, CancellationToken cancellationToken)
    {
        var agenda = await repository.GetByIdAsync(command.Id, true, cancellationToken);
        if (agenda is null || agenda.Status == AgendamentoPresencaStatus.Arquivado)
        {
            return false;
        }

        agenda.Arquivar(command.ResponsavelUsuarioId, clock.UtcNow);
        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }
}
