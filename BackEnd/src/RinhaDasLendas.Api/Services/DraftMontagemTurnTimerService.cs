using MediatR;
using RinhaDasLendas.Application.Commands.DraftMontagens;
using RinhaDasLendas.Domain.Models;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Api.Services;

public sealed class DraftMontagemTurnTimerService(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<DraftMontagemTurnTimerService> logger) : BackgroundService
{
    private readonly TimeSpan maxRealtimeDuration = TimeSpan.FromMinutes(Math.Max(configuration.GetValue("DraftMontagem:RealtimeMaxDurationMinutes", 120), 1));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RunCycleAsync(stoppingToken);
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

    internal async Task<int> RunCycleAsync(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<DraftMontagemRealtimeCandidate> candidates;
        using (var scanScope = scopeFactory.CreateScope())
        {
            var repository = scanScope.ServiceProvider.GetRequiredService<IDraftMontagemRepository>();
            candidates = await repository.ListExpiredRealtimeAsync(DateTimeOffset.UtcNow, 25, cancellationToken);
        }

        var processed = 0;
        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using var commandScope = scopeFactory.CreateScope();
                var sender = commandScope.ServiceProvider.GetRequiredService<ISender>();
                await sender.Send(
                    new ProcessarTurnoDraftMontagemExpiradoCommand(candidate.Id, maxRealtimeDuration),
                    cancellationToken);
                processed++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process expired draft montagem turn {DraftMontagemId}.", candidate.Id);
            }
        }

        return processed;
    }
}
