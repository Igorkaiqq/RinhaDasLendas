using RinhaDasLendas.Domain.Repositories;
using RinhaDasLendas.Application.Handlers.DraftMontagens;
using RinhaDasLendas.Application.Interfaces;

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
        var notifier = scope.ServiceProvider.GetRequiredService<IDraftMontagemRealtimeNotifier>();
        var reconciledIds = await repository.MarcarPublicacoesExpiradasParaReconciliacaoAsync(
            DateTimeOffset.UtcNow,
            cancellationToken);
        await DraftMontagemRealtimeNotificationPublisher.PublishReloadedAsync(
            reconciledIds,
            repository,
            notifier,
            cancellationToken);

        if (reconciledIds.Count > 0)
        {
            logger.LogInformation("Reconciled {Count} expired Discord publication claims", reconciledIds.Count);
        }

        return reconciledIds.Count;
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
