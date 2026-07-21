namespace RinhaDasLendas.Application.Interfaces;

public interface IDraftMontagemMetrics
{
    void RecordPresenceConfirmed(Guid draftMontagemId, string origin);
    void RecordPresenceCancelled(Guid draftMontagemId, string origin);
    void RecordPresenceClosed(Guid draftMontagemId);
    void RecordDiscordPublication(Guid draftMontagemId, string type, string status);
    void RecordPick(Guid draftMontagemId, string type);
    void RecordDraftTimeout(Guid draftMontagemId);
}
