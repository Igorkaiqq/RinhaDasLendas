using MediatR;
using RinhaDasLendas.Application.Commands.DraftMontagens;
using RinhaDasLendas.Domain.Models;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Api.Services;

public sealed class DraftMontagemPresenceClosureService(
    IServiceScopeFactory scopeFactory,
    ILogger<DraftMontagemPresenceClosureService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                {
                    return;
                }

                await RunCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    "Failed to run automatic presence closure cycle. Error type: {ErrorType}.",
                    exception.GetType().Name);
            }
        }
    }

    internal async Task<int> RunCycleAsync(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<DraftMontagemPresenceClosureCandidate> candidates;
        using (var scanScope = scopeFactory.CreateScope())
        {
            var repository = scanScope.ServiceProvider.GetRequiredService<IDraftMontagemRepository>();
            candidates = await repository.ListExpiredPresenceAsync(DateTimeOffset.UtcNow, 20, cancellationToken);
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
                    new EncerrarPresencaDraftMontagemAutomaticamenteCommand(candidate.Id),
                    cancellationToken);
                processed++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    "Failed to process automatic presence closure for draft {DraftMontagemId}. Error type: {ErrorType}.",
                    candidate.Id,
                    exception.GetType().Name);
            }
        }

        return processed;
    }
}
