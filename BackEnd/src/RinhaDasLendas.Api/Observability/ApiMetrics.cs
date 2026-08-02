using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace RinhaDasLendas.Api.Observability;

public sealed class ApiMetrics
{
    private readonly Counter<long> authFailures;
    private readonly Counter<long> botAuthFailures;
    private readonly Counter<long> rateLimitedRequests;
    private readonly Counter<long> stuckDrafts;
    private readonly Counter<long> draftActions;
    private readonly Counter<long> draftRealtimePublicationFailures;
    private readonly Histogram<long> draftRealtimePublicationFailureDuration;

    public ApiMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("RinhaDasLendas.Api");
        authFailures = meter.CreateCounter<long>("rinha_auth_failures_total");
        botAuthFailures = meter.CreateCounter<long>("rinha_bot_auth_failures_total");
        rateLimitedRequests = meter.CreateCounter<long>("rinha_rate_limited_requests_total");
        stuckDrafts = meter.CreateCounter<long>("rinha_stuck_drafts_total");
        draftActions = meter.CreateCounter<long>("rinha_draft_actions_total");
        draftRealtimePublicationFailures = meter.CreateCounter<long>("rinha_draft_realtime_publication_failures_total");
        draftRealtimePublicationFailureDuration = meter.CreateHistogram<long>("rinha_draft_realtime_publication_failure_duration_ms", "ms");
    }

    public void RecordAuthFailure(string scheme) => authFailures.Add(1, new KeyValuePair<string, object?>("scheme", scheme));

    public void RecordBotAuthFailure(string reason) => botAuthFailures.Add(1, new KeyValuePair<string, object?>("reason", reason));

    public void RecordRateLimitedRequest(string path) => rateLimitedRequests.Add(1, new KeyValuePair<string, object?>("path", path));

    public void RecordStuckDraft(Guid draftId) => stuckDrafts.Add(1, new KeyValuePair<string, object?>("draft_id", draftId.ToString()));

    public void RecordDraftAction(Guid draftId, string action, params KeyValuePair<string, object?>[] tags)
    {
        var allTags = new List<KeyValuePair<string, object?>>(tags.Length + 2)
        {
            new("draft_id", draftId.ToString()),
            new("action", action)
        };
        allTags.AddRange(tags);
        draftActions.Add(1, allTags.ToArray());
    }

    public void RecordDraftRealtimePublicationFailure(string eventName, string outcome, long elapsedMilliseconds)
    {
        var tags = new TagList
        {
            { "event", eventName },
            { "outcome", outcome },
        };
        draftRealtimePublicationFailures.Add(1, tags);
        draftRealtimePublicationFailureDuration.Record(elapsedMilliseconds, tags);
    }
}
