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
        DraftMontagemAvailabilityChange availability = DraftMontagemAvailabilityChange.None)
    {
        var montagem = await ReloadAsync(draftId, availability);
        if (montagem is null)
        {
            return;
        }

        var snapshot = DraftMontagemRealtimeStateFactory.CreateShared(montagem, DateTimeOffset.UtcNow);
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

    private async Task<DraftMontagem?> ReloadAsync(
        Guid draftId,
        DraftMontagemAvailabilityChange availability)
    {
        var operation = availability == DraftMontagemAvailabilityChange.Archived
            ? nameof(IDraftMontagemRepository.ReloadByIdIncludingArchivedAsync)
            : nameof(IDraftMontagemRepository.ReloadByIdAsync);
        var stopwatch = Stopwatch.StartNew();
        using var timeout = new CancellationTokenSource(InternalTimeout);

        try
        {
            return availability == DraftMontagemAvailabilityChange.Archived
                ? await repository.ReloadByIdIncludingArchivedAsync(draftId, timeout.Token)
                : await repository.ReloadByIdAsync(draftId, timeout.Token);
        }
        catch (Exception exception)
        {
            telemetry.RecordFailure(draftId, null, operation, stopwatch.ElapsedMilliseconds, exception);
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
            telemetry.RecordFailure(draftId, stateVersion, operation, stopwatch.ElapsedMilliseconds, exception);
        }
    }
}
