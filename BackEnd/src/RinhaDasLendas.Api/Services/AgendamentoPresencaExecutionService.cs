using MediatR;
using RinhaDasLendas.Application.Commands.AgendamentosPresenca;
using RinhaDasLendas.Application.Interfaces;

namespace RinhaDasLendas.Api.Services;

public sealed class AgendamentoPresencaExecutionService(
    IServiceScopeFactory scopeFactory,
    ISystemClock clock,
    IConfiguration configuration,
    ILogger<AgendamentoPresencaExecutionService> logger) : BackgroundService
{
    private readonly SemaphoreSlim cycleGate = new(1, 1);
    private readonly TimeSpan interval = TimeSpan.FromSeconds(Math.Clamp(
        configuration.GetValue("PresenceSchedule:IntervalSeconds", 30),
        1,
        3600));

    public async Task<AgendamentoPresencaCycleResult> RunCycleAsync(CancellationToken cancellationToken)
    {
        await cycleGate.WaitAsync(cancellationToken);
        try
        {
            using var scope = scopeFactory.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            return await sender.Send(
                new ProcessarAgendamentosPresencaDevidosCommand(clock.UtcNow), cancellationToken);
        }
        finally
        {
            cycleGate.Release();
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(interval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = await RunCycleAsync(stoppingToken);
                logger.LogInformation(
                    "Presence schedule cycle completed. Evaluated: {Evaluated}; Created: {Created}; Blocked: {Blocked}; Missed: {Missed}; Failures: {Failures}",
                    result.Avaliadas,
                    result.Criadas,
                    result.Bloqueadas,
                    result.Perdidas,
                    result.Falhas);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    "Presence schedule cycle failed. Error type: {ErrorType}",
                    exception.GetType().Name);
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                {
                    return;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }
    }
}
