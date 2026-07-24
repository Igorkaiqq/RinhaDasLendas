using System.Diagnostics.Metrics;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RinhaDasLendas.Api.Observability;
using RinhaDasLendas.Domain.Constants;

namespace RinhaDasLendas.Tests.Services;

public sealed class AgendamentoPresencaMetricsTests
{
    [Fact]
    public void MetricasDevemRegistrarTodosOsResultadosEDuracaoSomenteComTagsEstaveis()
    {
        var measurements = new List<(string Name, double Value, Dictionary<string, object?> Tags)>();
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == AgendamentoPresencaMetrics.MeterName)
                {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
            measurements.Add((instrument.Name, value, Capture(tags))));
        listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) =>
            measurements.Add((instrument.Name, value, Capture(tags))));
        listener.Start();
        using var provider = new ServiceCollection().AddMetrics().BuildServiceProvider();
        var metrics = new AgendamentoPresencaMetrics(provider.GetRequiredService<IMeterFactory>());

        metrics.RecordEvaluated();
        metrics.RecordCreated();
        metrics.RecordBlocked();
        metrics.RecordMissed();
        metrics.RecordFailure(MessageCodes.PresenceScheduleTimeZoneInvalid);
        metrics.RecordFailure("sensitive-user-value");
        metrics.RecordConflict(MessageCodes.PresenceScheduleOccurrenceConflict);
        metrics.RecordCycleDuration(TimeSpan.FromMilliseconds(125));

        measurements.Select(item => item.Name).Should().BeEquivalentTo([
            "rinha_presence_schedule_evaluated_total",
            "rinha_presence_schedule_created_total",
            "rinha_presence_schedule_blocked_total",
            "rinha_presence_schedule_missed_total",
            "rinha_presence_schedule_failures_total",
            "rinha_presence_schedule_failures_total",
            "rinha_presence_schedule_conflicts_total",
            "rinha_presence_schedule_cycle_duration_ms"]);
        measurements.Where(item => item.Name.EndsWith("failures_total", StringComparison.Ordinal))
            .Select(item => item.Tags["code"])
            .Should().BeEquivalentTo([
                MessageCodes.PresenceScheduleTimeZoneInvalid,
                MessageCodes.PresenceScheduleOccurrenceConflict]);
        measurements.SelectMany(item => item.Tags.Values).Should().NotContain("sensitive-user-value");
        measurements.Single(item => item.Name.EndsWith("conflicts_total", StringComparison.Ordinal)).Tags
            .Should().BeEquivalentTo(new Dictionary<string, object?> { ["code"] = MessageCodes.PresenceScheduleOccurrenceConflict });
        measurements.SelectMany(item => item.Tags.Keys).Should().NotContain([
            "name", "observation", "user", "user_id", "discord_id", "guild_id", "channel_id", "message_id", "token"]);
    }

    private static Dictionary<string, object?> Capture(ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        var result = new Dictionary<string, object?>();
        foreach (var tag in tags)
        {
            result[tag.Key] = tag.Value;
        }

        return result;
    }
}
