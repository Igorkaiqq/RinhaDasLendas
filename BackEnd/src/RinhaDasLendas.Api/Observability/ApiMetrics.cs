using System.Diagnostics.Metrics;

namespace RinhaDasLendas.Api.Observability;

public sealed class ApiMetrics
{
    private readonly Counter<long> authFailures;
    private readonly Counter<long> botAuthFailures;
    private readonly Counter<long> rateLimitedRequests;
    private readonly Counter<long> stuckDrafts;

    public ApiMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("RinhaDasLendas.Api");
        authFailures = meter.CreateCounter<long>("rinha_auth_failures_total");
        botAuthFailures = meter.CreateCounter<long>("rinha_bot_auth_failures_total");
        rateLimitedRequests = meter.CreateCounter<long>("rinha_rate_limited_requests_total");
        stuckDrafts = meter.CreateCounter<long>("rinha_stuck_drafts_total");
    }

    public void RecordAuthFailure(string scheme) => authFailures.Add(1, new KeyValuePair<string, object?>("scheme", scheme));

    public void RecordBotAuthFailure(string reason) => botAuthFailures.Add(1, new KeyValuePair<string, object?>("reason", reason));

    public void RecordRateLimitedRequest(string path) => rateLimitedRequests.Add(1, new KeyValuePair<string, object?>("path", path));

    public void RecordStuckDraft(Guid draftId) => stuckDrafts.Add(1, new KeyValuePair<string, object?>("draft_id", draftId.ToString()));
}
