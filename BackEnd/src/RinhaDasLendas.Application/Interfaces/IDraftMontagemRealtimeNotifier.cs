using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Interfaces;

public interface IDraftMontagemRealtimeNotifier
{
    Task StateUpdatedAsync(Guid draftMontagemId, DraftMontagemRealtimeStateDto state, CancellationToken cancellationToken);
}
