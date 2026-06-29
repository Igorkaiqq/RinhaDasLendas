using MediatR;
using RinhaDasLendas.Application.Commands.DraftMontagens;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Entities;
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
        var now = DateTimeOffset.UtcNow;
        var expired = await repository.ListExpiredRealtimeAsync(now, 25, cancellationToken);
        foreach (var montagem in expired)
        {
            if (RealtimeDurationExpired(montagem, now))
            {
                await sender.Send(new CancelarDraftMontagemCommand(montagem.Id, new CancelarDraftMontagemRequestDto(null)), cancellationToken);
                continue;
            }

            await sender.Send(new AvancarTurnoDraftMontagemTimeoutCommand(montagem.Id), cancellationToken);
        }
    }

    private bool RealtimeDurationExpired(DraftMontagem montagem, DateTimeOffset now)
    {
        var startedAt = montagem.Escolhas.OrderBy(escolha => escolha.RegistradoEm).FirstOrDefault()?.RegistradoEm
            ?? montagem.TurnoIniciadoEm;

        return startedAt is not null && now - startedAt.Value >= maxRealtimeDuration;
    }
}
