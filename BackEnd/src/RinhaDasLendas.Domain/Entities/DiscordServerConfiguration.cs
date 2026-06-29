using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Domain.Entities;

public sealed class DiscordServerConfiguration
{
    private DiscordServerConfiguration()
    {
    }

    public DiscordServerConfiguration(string guildId, string presenceChannelId, string newsChannelId, string adminChannelId, string draftChannelId, string matchResultChannelId, bool botEnabled)
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
        Atualizar(guildId, presenceChannelId, newsChannelId, adminChannelId, draftChannelId, matchResultChannelId, botEnabled);
    }

    public Guid Id { get; private set; }
    public string GuildId { get; private set; } = string.Empty;
    public string PresenceChannelId { get; private set; } = string.Empty;
    public string NewsChannelId { get; private set; } = string.Empty;
    public string AdminChannelId { get; private set; } = string.Empty;
    public string DraftChannelId { get; private set; } = string.Empty;
    public string MatchResultChannelId { get; private set; } = string.Empty;
    public bool BotEnabled { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void Atualizar(string guildId, string presenceChannelId, string newsChannelId, string adminChannelId, string draftChannelId, string matchResultChannelId, bool botEnabled)
    {
        GuildId = Required(guildId);
        PresenceChannelId = Required(presenceChannelId);
        NewsChannelId = Required(newsChannelId);
        AdminChannelId = Required(adminChannelId);
        DraftChannelId = Required(draftChannelId);
        MatchResultChannelId = Required(matchResultChannelId);
        BotEnabled = botEnabled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string Required(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(MessageCodes.FieldRequired);
        }

        return value.Trim();
    }
}
