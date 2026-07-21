using RinhaDasLendas.Application.Interfaces;

namespace RinhaDasLendas.Api.Observability;

public sealed class DraftMontagemMetrics(ApiMetrics metrics) : IDraftMontagemMetrics
{
    public void RecordPresenceConfirmed(Guid draftMontagemId, string origin) => metrics.RecordDraftAction(draftMontagemId, "presence_confirmed", new KeyValuePair<string, object?>("origin", origin));

    public void RecordPresenceCancelled(Guid draftMontagemId, string origin) => metrics.RecordDraftAction(draftMontagemId, "presence_cancelled", new KeyValuePair<string, object?>("origin", origin));

    public void RecordPresenceClosed(Guid draftMontagemId) => metrics.RecordDraftAction(draftMontagemId, "presence_closed");

    public void RecordDiscordPublication(Guid draftMontagemId, string type, string status) => metrics.RecordDraftAction(draftMontagemId, "discord_publication", new KeyValuePair<string, object?>("type", type), new KeyValuePair<string, object?>("status", status));

    public void RecordPick(Guid draftMontagemId, string type) => metrics.RecordDraftAction(draftMontagemId, "pick", new KeyValuePair<string, object?>("type", type));

    public void RecordDraftTimeout(Guid draftMontagemId) => metrics.RecordDraftAction(draftMontagemId, "timeout");
}
