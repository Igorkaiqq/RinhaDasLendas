using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Api.Services;

public sealed class DraftMontagemPresenceClosureService(IServiceProvider serviceProvider, ILogger<DraftMontagemPresenceClosureService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await CloseExpiredPresenceAsync(stoppingToken);
        }
    }

    private async Task CloseExpiredPresenceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IDraftMontagemRepository>();
            var expired = await repository.ListExpiredPresenceAsync(DateTimeOffset.UtcNow, 20, cancellationToken);
            foreach (var montagem in expired)
            {
                try
                {
                    if (montagem.Presencas.Count(presenca => presenca.Confirmada) < 10)
                    {
                        montagem.CancelarPresencaExpirada();
                        continue;
                    }

                    montagem.EncerrarPresenca(false, montagem.TamanhoEquipe);
                }
                catch (DomainException ex)
                {
                    logger.LogInformation(ex, "Automatic presence closure skipped for draft {DraftId}", montagem.Id);
                }
            }

            if (expired.Count > 0)
            {
                await repository.SaveChangesAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to close expired draft presences");
        }
    }
}
