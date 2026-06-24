using MediatR;
using RinhaDasLendas.Application.Commands.DraftMontagens;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Api.Services;

public sealed class DraftMontagemTurnTimerService(IServiceScopeFactory scopeFactory, ILogger<DraftMontagemTurnTimerService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessExpiredTurnsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process expired draft montagem turns.");
            }
        }
    }

    private async Task ProcessExpiredTurnsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDraftMontagemRepository>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var expired = await repository.ListExpiredRealtimeAsync(DateTimeOffset.UtcNow, 25, cancellationToken);
        foreach (var montagem in expired)
        {
            await sender.Send(new AvancarTurnoDraftMontagemTimeoutCommand(montagem.Id), cancellationToken);
        }
    }
}
