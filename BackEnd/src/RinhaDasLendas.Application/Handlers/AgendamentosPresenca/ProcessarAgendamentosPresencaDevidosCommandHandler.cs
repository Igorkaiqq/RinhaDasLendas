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
using RinhaDasLendas.Domain.Models;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Handlers.AgendamentosPresenca;

public sealed class ProcessarAgendamentosPresencaDevidosCommandHandler(
    IAgendamentoPresencaRepository repository,
    IAgendamentoPresencaTimeZone timeZone,
    IDiscordConfigurationService discordConfiguration,
    IAgendamentoPresencaMetrics metrics,
    IAgendamentoPresencaDiagnostics diagnostics,
    ISystemClock clock,
    AgendamentoPresencaProcessingOptions processingOptions)
    : IRequestHandler<ProcessarAgendamentosPresencaDevidosCommand, AgendamentoPresencaCycleResult>
{
    private readonly AgendamentoPresencaProcessingOptions options = processingOptions.Normalize();

    public async Task<AgendamentoPresencaCycleResult> Handle(
        ProcessarAgendamentosPresencaDevidosCommand command,
        CancellationToken cancellationToken)
    {
        var startedAt = Stopwatch.GetTimestamp();
        var totals = new CycleTotals();
        try
        {
            var throughDate = timeZone.GetLocalDate(clock.UtcNow);
            var blockedCursor = await ProcessBlockedBatchAsync(
                command.BlockedCursor, totals, cancellationToken);

            var candidates = await repository.ListCandidatesAsync(
                clock.UtcNow, command.Cursor, options.MaxSchedulesPerCycle, cancellationToken);
            Guid? cursor = command.Cursor;
            foreach (var candidate in candidates)
            {
                cursor = candidate.Id;
                cancellationToken.ThrowIfCancellationRequested();
                totals.Evaluated(metrics);
                try
                {
                    var processingCandidate = await repository.GetProcessingCandidateAsync(
                        candidate.Id, cancellationToken);
                    if (processingCandidate is not null)
                    {
                        await ProcessScheduleAsync(
                            processingCandidate, throughDate, totals, cancellationToken);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    totals.Failed(metrics, MessageCodes.PresenceScheduleOccurrenceConflict);
                    diagnostics.RecordFailure(
                        AgendamentoPresencaDiagnosticStage.CandidateSchedule,
                        exception.GetType().Name,
                        StableCode(exception));
                }
            }

            return totals.ToResult(cursor, blockedCursor);
        }
        finally
        {
            metrics.RecordCycleDuration(Stopwatch.GetElapsedTime(startedAt));
        }
    }

    private async Task<Guid?> ProcessBlockedBatchAsync(
        Guid? cursor,
        CycleTotals totals,
        CancellationToken cancellationToken)
    {
        var blocked = await repository.ListBlockedAsync(
            clock.UtcNow, options.MaxBlockedPerCycle, cancellationToken, cursor);
        var nextCursor = cursor;
        foreach (var occurrence in blocked)
        {
            nextCursor = occurrence.Id;
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var configuration = await GetConfigurationAsync(totals, cancellationToken);
                await ProcessBlockedAsync(occurrence, configuration, totals, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                totals.Failed(metrics, MessageCodes.PresenceScheduleOccurrenceConflict);
                diagnostics.RecordFailure(
                    AgendamentoPresencaDiagnosticStage.BlockedOccurrence,
                    exception.GetType().Name,
                    StableCode(exception));
            }
        }

        return nextCursor;
    }

    private async Task ProcessScheduleAsync(
        AgendamentoPresencaProcessingCandidate processingCandidate,
        DateOnly throughDate,
        CycleTotals totals,
        CancellationToken cancellationToken)
    {
        var schedule = processingCandidate.Agenda;
        var processedDates = 0;
        for (var date = schedule.UltimaDataAvaliada.AddDays(1);
             date <= throughDate && processedDates < options.MaxDatesPerSchedulePerCycle;
             date = date.AddDays(1))
        {
            cancellationToken.ThrowIfCancellationRequested();
            processedDates++;
            if (!schedule.OcorreEm(date))
            {
                await AdvanceMarkerAsync(schedule, date, cancellationToken);
                continue;
            }

            DateTimeOffset publicationAt;
            DateTimeOffset closureAt;
            try
            {
                publicationAt = timeZone.ToUtc(date, schedule.HorarioPublicacaoLocal);
                closureAt = timeZone.ToUtc(date, schedule.HorarioEncerramentoLocal);
            }
            catch (DomainException exception) when (
                exception.MessageCode == MessageCodes.PresenceScheduleTimeZoneInvalid)
            {
                var failed = await repository.TryUpsertFailedTimeZoneOccurrenceAsync(
                    schedule.Id,
                    date,
                    processingCandidate.Version,
                    schedule.DiasSemana.Single(item => item.DiaSemana == ToIsoDay(date)).DiaSemana,
                    schedule.HorarioPublicacaoLocal,
                    schedule.HorarioEncerramentoLocal,
                    clock.UtcNow,
                    cancellationToken);
                if (!failed.IsTerminal)
                {
                    return;
                }

                if (failed.Changed)
                {
                    totals.Failed(metrics, MessageCodes.PresenceScheduleTimeZoneInvalid);
                }

                await AdvanceMarkerAsync(schedule, date, cancellationToken);
                continue;
            }

            var now = clock.UtcNow;
            if (now < publicationAt)
            {
                return;
            }

            if (schedule.AtivadoEm > publicationAt)
            {
                await AdvanceMarkerAsync(schedule, date, cancellationToken);
                continue;
            }

            var configuration = await GetConfigurationAsync(totals, cancellationToken);
            var classified = await ClassifyAsync(
                schedule, date, publicationAt, closureAt, configuration, totals, cancellationToken);
            if (!classified)
            {
                return;
            }

            await AdvanceMarkerAsync(schedule, date, cancellationToken);
        }
    }

    private static DiaSemanaIso ToIsoDay(DateOnly date) =>
        (DiaSemanaIso)(date.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)date.DayOfWeek);

    private async Task<bool> ClassifyAsync(
        AgendamentoPresenca schedule,
        DateOnly date,
        DateTimeOffset publicationAt,
        DateTimeOffset closureAt,
        ConfigurationLookup configuration,
        CycleTotals totals,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        if (now >= closureAt)
        {
            var result = await repository.TryUpsertMissedOccurrenceAsync(
                schedule.Id, date, publicationAt, closureAt,
                MessageCodes.PresenceScheduleWindowExpired, now, cancellationToken);
            RecordWrite(result, totals);
            return result.IsTerminal;
        }

        if (configuration.State == ConfigurationState.TransientFailure)
        {
            return false;
        }

        if (configuration.State == ConfigurationState.Unavailable)
        {
            var result = await repository.TryUpsertBlockedOccurrenceAsync(
                schedule.Id, date, publicationAt, closureAt,
                MessageCodes.PresenceScheduleDiscordUnavailable, now, cancellationToken);
            RecordWrite(result, totals);
            return result.Status == OcorrenciaAgendamentoPresencaStatus.Bloqueada || result.IsTerminal;
        }

        return await ClaimAndCompleteAsync(
            schedule.Id, date, publicationAt, closureAt, configuration.Value!, totals, cancellationToken);
    }

    private async Task ProcessBlockedAsync(
        OcorrenciaAgendamentoPresenca occurrence,
        ConfigurationLookup configuration,
        CycleTotals totals,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        if (now >= occurrence.EncerramentoPrevistoEm)
        {
            var result = await repository.TryUpsertMissedOccurrenceAsync(
                occurrence.AgendamentoPresencaId,
                occurrence.DataLocal,
                occurrence.PublicacaoPrevistaEm,
                occurrence.EncerramentoPrevistoEm,
                MessageCodes.PresenceScheduleWindowExpired,
                now,
                cancellationToken);
            RecordWrite(result, totals);
            return;
        }

        if (configuration.State != ConfigurationState.Available)
        {
            return;
        }

        await ClaimAndCompleteAsync(
            occurrence.AgendamentoPresencaId,
            occurrence.DataLocal,
            occurrence.PublicacaoPrevistaEm,
            occurrence.EncerramentoPrevistoEm,
            configuration.Value!,
            totals,
            cancellationToken);
    }

    private async Task<bool> ClaimAndCompleteAsync(
        Guid scheduleId,
        DateOnly date,
        DateTimeOffset publicationAt,
        DateTimeOffset closureAt,
        DiscordConfigurationDto configuration,
        CycleTotals totals,
        CancellationToken cancellationToken)
    {
        var claimNow = clock.UtcNow;
        if (claimNow >= closureAt)
        {
            var missed = await repository.TryUpsertMissedOccurrenceAsync(
                scheduleId, date, publicationAt, closureAt,
                MessageCodes.PresenceScheduleWindowExpired, claimNow, cancellationToken);
            RecordWrite(missed, totals);
            return missed.IsTerminal;
        }

        var claimId = Guid.NewGuid();
        var claim = await repository.TryClaimOccurrenceAsync(
            scheduleId, date, publicationAt, closureAt, claimId,
            claimNow.AddMinutes(5), claimNow, cancellationToken,
            configuration.GuildId, configuration.PresenceChannelId);
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

        var completionNow = clock.UtcNow;
        if (completionNow >= closureAt)
        {
            var missed = await repository.TryMarkClaimedOccurrenceMissedAsync(
                claim.OcorrenciaId, claimId, completionNow, cancellationToken);
            RecordWrite(missed, totals);
            return missed.IsTerminal;
        }

        var draft = DraftMontagem.CriarPorPresenca(
            $"{claim.NomeSnapshot} - {date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)}",
            claim.ObservacaoSnapshot,
            5);
        draft.ConfigurarEncerramentoPresenca(closureAt);
        draft.ConfigurarPublicacaoDiscord(configuration.GuildId, null);
        if (!await repository.TryCompleteWithDraftAsync(
            claim.OcorrenciaId, claimId, draft, completionNow, cancellationToken,
            configuration.GuildId, configuration.PresenceChannelId))
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
        CancellationToken cancellationToken)
    {
        schedule.MarcarDataAvaliada(date, clock.UtcNow);
        await repository.SaveChangesAsync(cancellationToken);
    }

    private async Task<ConfigurationLookup> GetConfigurationAsync(
        CycleTotals totals,
        CancellationToken cancellationToken)
    {
        try
        {
            var configuration = await discordConfiguration.GetAsync(cancellationToken);
            return IsAvailable(configuration)
                ? new ConfigurationLookup(ConfigurationState.Available, configuration)
                : new ConfigurationLookup(ConfigurationState.Unavailable, configuration);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            totals.Failed(metrics, MessageCodes.PresenceScheduleOccurrenceConflict);
            diagnostics.RecordFailure(
                AgendamentoPresencaDiagnosticStage.DiscordConfiguration,
                exception.GetType().Name,
                MessageCodes.PresenceScheduleOccurrenceConflict);
            return new ConfigurationLookup(ConfigurationState.TransientFailure, null);
        }
    }

    private void RecordWrite(AgendamentoPresencaOccurrenceWriteResult result, CycleTotals totals)
    {
        if (!result.Changed)
        {
            return;
        }

        switch (result.Status)
        {
            case OcorrenciaAgendamentoPresencaStatus.Bloqueada:
                totals.Blocked(metrics);
                break;
            case OcorrenciaAgendamentoPresencaStatus.Perdida:
                totals.Missed(metrics);
                break;
            case OcorrenciaAgendamentoPresencaStatus.Falha:
                totals.Failed(metrics, MessageCodes.PresenceScheduleTimeZoneInvalid);
                break;
        }
    }

    private static bool IsAvailable(DiscordConfigurationDto? configuration) =>
        configuration is { BotEnabled: true }
        && !string.IsNullOrWhiteSpace(configuration.GuildId)
        && !string.IsNullOrWhiteSpace(configuration.PresenceChannelId);

    private static string StableCode(Exception exception) =>
        exception is DomainException domain
            ? domain.MessageCode
            : MessageCodes.PresenceScheduleOccurrenceConflict;

    private enum ConfigurationState
    {
        Available,
        Unavailable,
        TransientFailure
    }

    private sealed record ConfigurationLookup(ConfigurationState State, DiscordConfigurationDto? Value);

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

        public AgendamentoPresencaCycleResult ToResult(Guid? cursor, Guid? blockedCursor) =>
            new(evaluated, created, blocked, missed, failed, cursor, blockedCursor);
    }
}
