using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Interfaces;

public interface IDraftMontagemRealtimeNotifier
{
    Task StateUpdatedAsync(Guid draftMontagemId, DraftMontagemRealtimeStateDto state, CancellationToken cancellationToken);
    Task SharedStateUpdatedAsync(Guid draftMontagemId, DraftMontagemRealtimeSnapshotDto state, CancellationToken cancellationToken);
    Task ArchivedAsync(Guid draftMontagemId, CancellationToken cancellationToken);
    Task RestoredAsync(Guid draftMontagemId, CancellationToken cancellationToken);
}
