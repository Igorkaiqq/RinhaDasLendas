using System.Diagnostics;
using System.Globalization;
using MediatR;
using RinhaDasLendas.Application.Commands.AgendamentosPresenca;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.AgendamentosPresenca;

public sealed class ProcessarAgendamentosPresencaDevidosCommandHandler(
    IAgendamentoPresencaRepository repository,
    IAgendamentoPresencaTimeZone timeZone,
    IDiscordConfigurationService discordConfiguration,
    IAgendamentoPresencaMetrics metrics)
    : IRequestHandler<ProcessarAgendamentosPresencaDevidosCommand, AgendamentoPresencaCycleResult>
{
    public async Task<AgendamentoPresencaCycleResult> Handle(
        ProcessarAgendamentosPresencaDevidosCommand command,
        CancellationToken cancellationToken)
    {
        var startedAt = Stopwatch.GetTimestamp();
        var totals = new CycleTotals();
        try
        {
            var localDate = timeZone.GetLocalDate(command.Agora);
            var configuration = await GetConfigurationAsync(cancellationToken);

            var blocked = await repository.ListBlockedAsync(command.Agora, cancellationToken);
            foreach (var occurrence in blocked)
            {
                cancellationToken.ThrowIfCancellationRequested();
                totals.Evaluated(metrics);
                try
                {
                    await ProcessBlockedAsync(occurrence, configuration, command.Agora, totals, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception)
                {
                    totals.Failed(metrics, MessageCodes.PresenceScheduleOccurrenceConflict);
                }
            }

            var candidates = await repository.ListCandidatesAsync(localDate, cancellationToken);
            foreach (var schedule in candidates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                totals.Evaluated(metrics);
                try
                {
                    await ProcessScheduleAsync(schedule, localDate, configuration, command.Agora, totals, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (DomainException exception) when (
                    exception.MessageCode == MessageCodes.PresenceScheduleTimeZoneInvalid)
                {
                    totals.Failed(metrics, MessageCodes.PresenceScheduleTimeZoneInvalid);
                }
                catch (Exception)
                {
                    totals.Failed(metrics, MessageCodes.PresenceScheduleOccurrenceConflict);
                }
            }

            return totals.ToResult();
        }
        finally
        {
            metrics.RecordCycleDuration(Stopwatch.GetElapsedTime(startedAt));
        }
    }

    private async Task ProcessScheduleAsync(
        AgendamentoPresenca schedule,
        DateOnly throughDate,
        DiscordConfigurationDto? configuration,
        DateTimeOffset now,
        CycleTotals totals,
        CancellationToken cancellationToken)
    {
        for (var date = schedule.UltimaDataAvaliada.AddDays(1); date <= throughDate; date = date.AddDays(1))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!schedule.OcorreEm(date))
            {
                await AdvanceMarkerAsync(schedule, date, now, cancellationToken);
                continue;
            }

            var publicationAt = timeZone.ToUtc(date, schedule.HorarioPublicacaoLocal);
            var closureAt = timeZone.ToUtc(date, schedule.HorarioEncerramentoLocal);
            if (now < publicationAt)
            {
                return;
            }

            if (schedule.AtivadoEm > publicationAt)
            {
                await AdvanceMarkerAsync(schedule, date, now, cancellationToken);
                continue;
            }

            var classified = await ClassifyAsync(
                schedule, date, publicationAt, closureAt, configuration, now, totals, cancellationToken);
            if (!classified)
            {
                return;
            }

            await AdvanceMarkerAsync(schedule, date, now, cancellationToken);
        }
    }

    private async Task<bool> ClassifyAsync(
        AgendamentoPresenca schedule,
        DateOnly date,
        DateTimeOffset publicationAt,
        DateTimeOffset closureAt,
        DiscordConfigurationDto? configuration,
        DateTimeOffset now,
        CycleTotals totals,
        CancellationToken cancellationToken)
    {
        if (now >= closureAt)
        {
            var missed = await repository.TryUpsertMissedOccurrenceAsync(
                schedule.Id, date, publicationAt, closureAt, MessageCodes.PresenceScheduleWindowExpired,
                now, cancellationToken);
            if (missed)
            {
                totals.Missed(metrics);
            }
            else
            {
                totals.Conflict(metrics);
            }

            return missed || await HasTerminalOccurrenceAsync(schedule.Id, date, cancellationToken);
        }

        if (!IsAvailable(configuration))
        {
            var blocked = await repository.TryUpsertBlockedOccurrenceAsync(
                schedule.Id, date, publicationAt, closureAt, MessageCodes.PresenceScheduleDiscordUnavailable,
                now, cancellationToken);
            if (blocked)
            {
                totals.Blocked(metrics);
            }
            else
            {
                totals.Conflict(metrics);
            }

            return blocked || await HasTerminalOccurrenceAsync(schedule.Id, date, cancellationToken);
        }

        return await ClaimAndCompleteAsync(
            schedule, date, publicationAt, closureAt, configuration!, now, totals, cancellationToken);
    }

    private async Task ProcessBlockedAsync(
        OcorrenciaAgendamentoPresenca occurrence,
        DiscordConfigurationDto? configuration,
        DateTimeOffset now,
        CycleTotals totals,
        CancellationToken cancellationToken)
    {
        if (now >= occurrence.EncerramentoPrevistoEm)
        {
            if (await repository.TryUpsertMissedOccurrenceAsync(
                occurrence.AgendamentoPresencaId,
                occurrence.DataLocal,
                occurrence.PublicacaoPrevistaEm,
                occurrence.EncerramentoPrevistoEm,
                MessageCodes.PresenceScheduleWindowExpired,
                now,
                cancellationToken))
            {
                totals.Missed(metrics);
            }
            else
            {
                totals.Conflict(metrics);
            }

            return;
        }

        if (!IsAvailable(configuration))
        {
            if (await repository.TryUpsertBlockedOccurrenceAsync(
                occurrence.AgendamentoPresencaId,
                occurrence.DataLocal,
                occurrence.PublicacaoPrevistaEm,
                occurrence.EncerramentoPrevistoEm,
                MessageCodes.PresenceScheduleDiscordUnavailable,
                now,
                cancellationToken))
            {
                totals.Blocked(metrics);
            }
            else
            {
                totals.Conflict(metrics);
            }

            return;
        }

        var schedule = await repository.GetByIdAsync(
            occurrence.AgendamentoPresencaId, tracking: false, cancellationToken);
        if (schedule is null)
        {
            totals.Failed(metrics, MessageCodes.PresenceScheduleNotFound);
            return;
        }

        await ClaimAndCompleteAsync(
            schedule,
            occurrence.DataLocal,
            occurrence.PublicacaoPrevistaEm,
            occurrence.EncerramentoPrevistoEm,
            configuration!,
            now,
            totals,
            cancellationToken);
    }

    private async Task<bool> ClaimAndCompleteAsync(
        AgendamentoPresenca schedule,
        DateOnly date,
        DateTimeOffset publicationAt,
        DateTimeOffset closureAt,
        DiscordConfigurationDto configuration,
        DateTimeOffset now,
        CycleTotals totals,
        CancellationToken cancellationToken)
    {
        var claimId = Guid.NewGuid();
        var claim = await repository.TryClaimOccurrenceAsync(
            schedule.Id,
            date,
            publicationAt,
            closureAt,
            claimId,
            now.AddMinutes(5),
            now,
            cancellationToken);
        if (claim is null)
        {
            totals.Conflict(metrics);
            return false;
        }

        if (!claim.Adquirido)
        {
            totals.Conflict(metrics);
            return claim.Status is OcorrenciaAgendamentoPresencaStatus.Criada
                or OcorrenciaAgendamentoPresencaStatus.Perdida
                or OcorrenciaAgendamentoPresencaStatus.Falha;
        }

        var draft = new DraftMontagem(
            $"{schedule.Nome} - {date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)}",
            schedule.Observacao,
            5,
            DraftMontagemCriterioCapitaes.Manual,
            [],
            []);
        draft.ConfigurarEncerramentoPresenca(closureAt);
        draft.ConfigurarPublicacaoDiscord(configuration.GuildId, null);
        if (!await repository.TryCompleteWithDraftAsync(
            claim.OcorrenciaId, claimId, draft, now, cancellationToken))
        {
            totals.Conflict(metrics);
            return false;
        }

        totals.Created(metrics);
        return true;
    }

    private async Task AdvanceMarkerAsync(
        AgendamentoPresenca schedule,
        DateOnly date,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        schedule.MarcarDataAvaliada(date, now);
        await repository.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> HasTerminalOccurrenceAsync(
        Guid scheduleId,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var occurrence = await repository.GetOccurrenceAsync(scheduleId, date, cancellationToken);
        return occurrence?.Status is OcorrenciaAgendamentoPresencaStatus.Criada
            or OcorrenciaAgendamentoPresencaStatus.Perdida
            or OcorrenciaAgendamentoPresencaStatus.Falha;
    }

    private async Task<DiscordConfigurationDto?> GetConfigurationAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await discordConfiguration.GetAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static bool IsAvailable(DiscordConfigurationDto? configuration) =>
        configuration is
        {
            BotEnabled: true,
            GuildId.Length: > 0,
            PresenceChannelId.Length: > 0
        };

    private sealed class CycleTotals
    {
        private int evaluated;
        private int created;
        private int blocked;
        private int missed;
        private int failed;

        public void Evaluated(IAgendamentoPresencaMetrics value)
        {
            evaluated++;
            value.RecordEvaluated();
        }

        public void Created(IAgendamentoPresencaMetrics value)
        {
            created++;
            value.RecordCreated();
        }

        public void Blocked(IAgendamentoPresencaMetrics value)
        {
            blocked++;
            value.RecordBlocked();
        }

        public void Missed(IAgendamentoPresencaMetrics value)
        {
            missed++;
            value.RecordMissed();
        }

        public void Failed(IAgendamentoPresencaMetrics value, string code)
        {
            failed++;
            value.RecordFailure(code);
        }

        public void Conflict(IAgendamentoPresencaMetrics value) =>
            value.RecordConflict(MessageCodes.PresenceScheduleOccurrenceConflict);

        public AgendamentoPresencaCycleResult ToResult() => new(evaluated, created, blocked, missed, failed);
    }
}
