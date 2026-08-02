using MediatR;
using RinhaDasLendas.Application.Commands.DraftMontagens;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Models;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.DraftMontagens;

public sealed class EncerrarPresencaDraftMontagemAutomaticamenteCommandHandler(
    IDraftMontagemRepository repository,
    IDraftMontagemRealtimePublisher publisher,
    IDraftMontagemMetrics metrics)
    : IRequestHandler<EncerrarPresencaDraftMontagemAutomaticamenteCommand>
{
    public async Task Handle(
        EncerrarPresencaDraftMontagemAutomaticamenteCommand command,
        CancellationToken cancellationToken)
    {
        var montagem = await repository.ReloadByIdAsync(command.Id, cancellationToken);
        if (montagem is null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        if (montagem.Status != DraftMontagemStatus.PresencaAberta
            || montagem.HorarioEncerramentoPresenca is null
            || montagem.HorarioEncerramentoPresenca > now)
        {
            return;
        }

        var cancelled = montagem.Presencas.Count(presenca => presenca.Confirmada) < 10;
        if (cancelled)
        {
            montagem.Cancelar(null, DraftMontagemActor.System());
        }
        else
        {
            montagem.EncerrarPresenca(false, montagem.TamanhoEquipe);
        }

        await repository.SaveChangesAsync(cancellationToken);
        await publisher.PublishAfterCommitAsync(command.Id);
        if (cancelled)
        {
            metrics.RecordDraftCancelled(command.Id);
        }
        else
        {
            metrics.RecordPresenceClosed(command.Id);
        }
    }
}
