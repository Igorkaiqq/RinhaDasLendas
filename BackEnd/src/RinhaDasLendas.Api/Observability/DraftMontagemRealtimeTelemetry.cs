using System.Diagnostics;
using System.Diagnostics.Metrics;
using RinhaDasLendas.Application.Interfaces;

namespace RinhaDasLendas.Api.Observability;

public sealed class DraftMontagemRealtimeTelemetry : IDraftMontagemRealtimeTelemetry
{
    private readonly ILogger<DraftMontagemRealtimeTelemetry> logger;
    private readonly Counter<long> failures;
    private readonly Histogram<long> failureDuration;

    public DraftMontagemRealtimeTelemetry(
        ILogger<DraftMontagemRealtimeTelemetry> logger,
        IMeterFactory meterFactory)
    {
        this.logger = logger;
        var meter = meterFactory.Create("RinhaDasLendas.Api");
        failures = meter.CreateCounter<long>("rinha_draft_realtime_publication_failures_total");
        failureDuration = meter.CreateHistogram<long>("rinha_draft_realtime_publication_failure_duration_ms", "ms");
    }

    public void RecordFailure(
        Guid draftId,
        long? stateVersion,
        string operation,
        long elapsedMilliseconds,
        Exception exception)
    {
        var failureType = exception is OperationCanceledException
            ? nameof(OperationCanceledException)
            : exception.GetType().Name;
        var tags = new TagList
        {
            { "draft_id", draftId.ToString() },
            { "state_version", stateVersion },
            { "operation", operation },
            { "failure_type", failureType }
        };
        failures.Add(1, tags);
        failureDuration.Record(elapsedMilliseconds, tags);
        logger.LogWarning(
            exception,
            "Draft realtime publication failed. Draft: {DraftId}; State version: {StateVersion}; Operation: {Operation}; Elapsed milliseconds: {ElapsedMilliseconds}; Failure type: {FailureType}",
            draftId,
            stateVersion,
            operation,
            elapsedMilliseconds,
            failureType);
    }
}
