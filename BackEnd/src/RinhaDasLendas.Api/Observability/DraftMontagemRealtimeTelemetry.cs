using RinhaDasLendas.Application.Interfaces;

namespace RinhaDasLendas.Api.Observability;

public sealed class DraftMontagemRealtimeTelemetry : IDraftMontagemRealtimeTelemetry
{
    private readonly ILogger<DraftMontagemRealtimeTelemetry> logger;
    private readonly ApiMetrics metrics;

    public DraftMontagemRealtimeTelemetry(
        ILogger<DraftMontagemRealtimeTelemetry> logger,
        ApiMetrics metrics)
    {
        this.logger = logger;
        this.metrics = metrics;
    }

    public void RecordFailure(
        Guid draftId,
        long? stateVersion,
        string operation,
        long elapsedMilliseconds,
        string failureType)
    {
        metrics.RecordDraftRealtimePublicationFailure(
            NormalizeEvent(operation),
            failureType == nameof(OperationCanceledException) ? "timeout" : "failure",
            elapsedMilliseconds);
        logger.LogWarning(
            "Draft realtime publication failed. Draft: {DraftId}; State version: {StateVersion}; Operation: {Operation}; Elapsed milliseconds: {ElapsedMilliseconds}; Failure type: {FailureType}",
            draftId,
            stateVersion,
            operation,
            elapsedMilliseconds,
            failureType);
    }

    private static string NormalizeEvent(string operation) => operation switch
    {
        "SharedStateUpdatedAsync" => "state_updated",
        "ArchivedAsync" => "archived",
        "RestoredAsync" => "restored",
        "ReloadByIdAsync" => "reload_active",
        "ReloadByIdIncludingArchivedAsync" => "reload_archived",
        "CreateShared" => "snapshot",
        "PublishAfterCommitAsync" => "publish",
        _ => "unknown",
    };
}
