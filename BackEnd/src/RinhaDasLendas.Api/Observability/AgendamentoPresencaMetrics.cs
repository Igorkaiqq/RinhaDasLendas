using System.Diagnostics.Metrics;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Constants;

namespace RinhaDasLendas.Api.Observability;

public sealed class AgendamentoPresencaMetrics : IAgendamentoPresencaMetrics
{
    public const string MeterName = "RinhaDasLendas.PresenceSchedule";

    private readonly Counter<long> evaluated;
    private readonly Counter<long> created;
    private readonly Counter<long> blocked;
    private readonly Counter<long> missed;
    private readonly Counter<long> failures;
    private readonly Counter<long> conflicts;
    private readonly Histogram<double> cycleDuration;
    private static readonly HashSet<string> StableCodes =
    [
        MessageCodes.PresenceScheduleTimeZoneInvalid,
        MessageCodes.PresenceScheduleOccurrenceConflict,
        MessageCodes.PresenceScheduleDiscordUnavailable,
        MessageCodes.PresenceScheduleNotFound,
        MessageCodes.PresenceScheduleWindowExpired
    ];

    public AgendamentoPresencaMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        evaluated = meter.CreateCounter<long>("rinha_presence_schedule_evaluated_total");
        created = meter.CreateCounter<long>("rinha_presence_schedule_created_total");
        blocked = meter.CreateCounter<long>("rinha_presence_schedule_blocked_total");
        missed = meter.CreateCounter<long>("rinha_presence_schedule_missed_total");
        failures = meter.CreateCounter<long>("rinha_presence_schedule_failures_total");
        conflicts = meter.CreateCounter<long>("rinha_presence_schedule_conflicts_total");
        cycleDuration = meter.CreateHistogram<double>("rinha_presence_schedule_cycle_duration_ms", "ms");
    }

    public void RecordEvaluated() => evaluated.Add(1);

    public void RecordCreated() => created.Add(1);

    public void RecordBlocked() => blocked.Add(1);

    public void RecordMissed() => missed.Add(1);

    public void RecordFailure(string code) =>
        failures.Add(1, new KeyValuePair<string, object?>("code", NormalizeCode(code)));

    public void RecordConflict(string code) =>
        conflicts.Add(1, new KeyValuePair<string, object?>("code", NormalizeCode(code)));

    public void RecordCycleDuration(TimeSpan duration) => cycleDuration.Record(duration.TotalMilliseconds);

    private static string NormalizeCode(string code) =>
        StableCodes.Contains(code) ? code : MessageCodes.PresenceScheduleOccurrenceConflict;
}
