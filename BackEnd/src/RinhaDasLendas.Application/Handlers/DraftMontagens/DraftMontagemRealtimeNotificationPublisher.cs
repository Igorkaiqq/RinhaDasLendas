using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.DraftMontagens;

public static class DraftMontagemRealtimeNotificationPublisher
{
    public static async Task PublishReloadedAsync(
        IEnumerable<Guid> draftMontagemIds,
        IDraftMontagemRepository repository,
        IDraftMontagemRealtimeNotifier notifier,
        CancellationToken cancellationToken)
    {
        foreach (var id in draftMontagemIds.Distinct())
        {
            if (await repository.ReloadByIdAsync(id, cancellationToken) is not { } montagem)
            {
                continue;
            }

            await notifier.StateUpdatedAsync(
                id,
                DraftMontagemRealtimeStateFactory.Create(montagem, DateTimeOffset.UtcNow),
                cancellationToken);
        }
    }
}
