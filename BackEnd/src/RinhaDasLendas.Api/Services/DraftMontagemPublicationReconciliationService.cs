using RinhaDasLendas.Domain.Repositories;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Enums;

namespace RinhaDasLendas.Api.Services;

public sealed class DraftMontagemPublicationReconciliationService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<DraftMontagemPublicationReconciliationService> logger) : BackgroundService
{
    private readonly TimeSpan interval = TimeSpan.FromSeconds(Math.Clamp(
        configuration.GetValue("DraftMontagem:PublicationReconciliationIntervalSeconds", 30),
        5,
        3600));

    public async Task<int> RunCycleAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDraftMontagemRepository>();
        var publisher = scope.ServiceProvider.GetRequiredService<IDraftMontagemRealtimePublisher>();
        var reconciled = await repository.MarcarPublicacoesExpiradasParaReconciliacaoAsync(
            DateTimeOffset.UtcNow,
            cancellationToken);
        foreach (var stamp in reconciled)
        {
            await publisher.PublishAfterCommitAsync(stamp.Id, DraftMontagemSnapshotScope.IncludingArchived);
        }

        if (reconciled.Count > 0)
        {
            logger.LogInformation("Reconciled {Count} expired Discord publication claims", reconciled.Count);
        }

        return reconciled.Count;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    "Failed to reconcile expired Discord publication claims. Error type: {ErrorType}",
                    exception.GetType().Name);
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }
    }
}
