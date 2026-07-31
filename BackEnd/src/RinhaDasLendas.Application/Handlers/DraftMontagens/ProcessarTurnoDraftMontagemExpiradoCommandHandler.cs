using MediatR;
using RinhaDasLendas.Application.Commands.DraftMontagens;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Models;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.DraftMontagens;

public sealed class ProcessarTurnoDraftMontagemExpiradoCommandHandler(
    IDraftMontagemRepository repository,
    IDraftMontagemRealtimePublisher publisher,
    IDraftMontagemMetrics metrics) : IRequestHandler<ProcessarTurnoDraftMontagemExpiradoCommand>
{
    public async Task Handle(ProcessarTurnoDraftMontagemExpiradoCommand command, CancellationToken cancellationToken)
    {
        var montagem = await repository.ReloadByIdAsync(command.Id, cancellationToken);
        if (montagem is null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        if (montagem.Status != DraftMontagemStatus.Aberta
            || montagem.Modo != DraftMontagemModo.TempoReal
            || montagem.TurnoExpiraEm is null
            || montagem.TurnoExpiraEm > now)
        {
            return;
        }

        var startedAt = montagem.Escolhas.OrderBy(escolha => escolha.RegistradoEm).FirstOrDefault()?.RegistradoEm
            ?? montagem.TurnoIniciadoEm;
        var maximumDurationExpired = startedAt is not null
            && now - startedAt.Value >= command.MaxRealtimeDuration;

        if (maximumDurationExpired)
        {
            montagem.Cancelar(null, DraftMontagemActor.System());
        }
        else if (!montagem.AvancarTurnoPorTimeout(now))
        {
            return;
        }

        await repository.SaveChangesAsync(cancellationToken);
        await publisher.PublishAfterCommitAsync(command.Id);
        if (maximumDurationExpired)
        {
            metrics.RecordDraftCancelled(command.Id);
        }
        else
        {
            metrics.RecordDraftTimeout(command.Id);
        }
    }
}
