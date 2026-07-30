using System.Diagnostics;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Enums;
using RinhaDasLendas.Application.Handlers.DraftMontagens;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Services;

public sealed class DraftMontagemRealtimePublisher(
    IDraftMontagemRepository repository,
    IDraftMontagemRealtimeNotifier notifier,
    IDraftMontagemRealtimeTelemetry telemetry) : IDraftMontagemRealtimePublisher
{
    private static readonly TimeSpan InternalTimeout = TimeSpan.FromSeconds(5);

    public async Task PublishAfterCommitAsync(
        Guid draftId,
        DraftMontagemSnapshotScope snapshotScope = DraftMontagemSnapshotScope.Active,
        DraftMontagemAvailabilityChange availability = DraftMontagemAvailabilityChange.None)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var montagem = await ReloadAsync(draftId, snapshotScope);
            if (montagem is null)
            {
                return;
            }

            var snapshot = CreateSnapshot(draftId, montagem);
            if (snapshot is null)
            {
                return;
            }

            await SendAsync(
                draftId,
                montagem.VersaoEstado,
                nameof(IDraftMontagemRealtimeNotifier.SharedStateUpdatedAsync),
                token => notifier.SharedStateUpdatedAsync(draftId, snapshot, token));

            if (availability == DraftMontagemAvailabilityChange.Archived)
            {
                await SendAsync(
                    draftId,
                    montagem.VersaoEstado,
                    nameof(IDraftMontagemRealtimeNotifier.ArchivedAsync),
                    token => notifier.ArchivedAsync(draftId, token));
            }
            else if (availability == DraftMontagemAvailabilityChange.Restored)
            {
                await SendAsync(
                    draftId,
                    montagem.VersaoEstado,
                    nameof(IDraftMontagemRealtimeNotifier.RestoredAsync),
                    token => notifier.RestoredAsync(draftId, token));
            }
        }
        catch (Exception exception)
        {
            TryRecordFailure(
                draftId,
                null,
                nameof(PublishAfterCommitAsync),
                stopwatch.ElapsedMilliseconds,
                exception);
        }
    }

    private DraftMontagemRealtimeSnapshotDto? CreateSnapshot(Guid draftId, DraftMontagem montagem)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            return DraftMontagemRealtimeStateFactory.CreateShared(montagem, DateTimeOffset.UtcNow);
        }
        catch (Exception exception)
        {
            TryRecordFailure(
                draftId,
                montagem.VersaoEstado,
                nameof(DraftMontagemRealtimeStateFactory.CreateShared),
                stopwatch.ElapsedMilliseconds,
                exception);
            return null;
        }
    }

    private async Task<DraftMontagem?> ReloadAsync(
        Guid draftId,
        DraftMontagemSnapshotScope snapshotScope)
    {
        var operation = snapshotScope == DraftMontagemSnapshotScope.IncludingArchived
            ? nameof(IDraftMontagemRepository.ReloadByIdIncludingArchivedAsync)
            : nameof(IDraftMontagemRepository.ReloadByIdAsync);
        var stopwatch = Stopwatch.StartNew();
        using var timeout = new CancellationTokenSource(InternalTimeout);

        try
        {
            return snapshotScope == DraftMontagemSnapshotScope.IncludingArchived
                ? await repository.ReloadByIdIncludingArchivedAsync(draftId, timeout.Token)
                : await repository.ReloadByIdAsync(draftId, timeout.Token);
        }
        catch (Exception exception)
        {
            TryRecordFailure(draftId, null, operation, stopwatch.ElapsedMilliseconds, exception);
            return null;
        }
    }

    private async Task SendAsync(
        Guid draftId,
        long stateVersion,
        string operation,
        Func<CancellationToken, Task> send)
    {
        var stopwatch = Stopwatch.StartNew();
        using var timeout = new CancellationTokenSource(InternalTimeout);

        try
        {
            await send(timeout.Token);
        }
        catch (Exception exception)
        {
            TryRecordFailure(draftId, stateVersion, operation, stopwatch.ElapsedMilliseconds, exception);
        }
    }

    private void TryRecordFailure(
        Guid draftId,
        long? stateVersion,
        string operation,
        long elapsedMilliseconds,
        Exception exception)
    {
        var failureType = exception is OperationCanceledException
            ? nameof(OperationCanceledException)
            : exception.GetType().Name;

        try
        {
            telemetry.RecordFailure(draftId, stateVersion, operation, elapsedMilliseconds, failureType);
        }
        catch
        {
            // Observability is best-effort and must never change a committed operation result.
        }
    }
}
