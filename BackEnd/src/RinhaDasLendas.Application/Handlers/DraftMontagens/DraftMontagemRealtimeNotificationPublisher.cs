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
        var failures = await PublishPassAsync(
            draftMontagemIds.Distinct(),
            repository,
            notifier,
            cancellationToken);
        if (failures.Count == 0)
        {
            return;
        }

        var retryFailures = await PublishPassAsync(
            failures.Select(failure => failure.Id),
            repository,
            notifier,
            cancellationToken);
        if (retryFailures.Count > 0)
        {
            throw new AggregateException(retryFailures.Select(failure => failure.Exception));
        }
    }

    private static async Task<IReadOnlyCollection<(Guid Id, Exception Exception)>> PublishPassAsync(
        IEnumerable<Guid> draftMontagemIds,
        IDraftMontagemRepository repository,
        IDraftMontagemRealtimeNotifier notifier,
        CancellationToken cancellationToken)
    {
        var failures = new List<(Guid Id, Exception Exception)>();
        foreach (var id in draftMontagemIds)
        {
            try
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
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                failures.Add((id, exception));
            }
        }

        return failures;
    }
}
