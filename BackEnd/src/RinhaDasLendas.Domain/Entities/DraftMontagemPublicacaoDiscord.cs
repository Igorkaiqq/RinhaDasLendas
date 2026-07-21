using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Domain.Entities;

public sealed class DraftMontagemPublicacaoDiscord
{
    private DraftMontagemPublicacaoDiscord()
    {
    }

    public DraftMontagemPublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo tipo, string? guildId, string? channelId)
    {
        Id = Guid.NewGuid();
        Tipo = tipo;
        GuildId = Normalize(guildId);
        ChannelId = Normalize(channelId);
        Status = DraftMontagemPublicacaoDiscordStatus.Pendente;
        UltimaTentativaEm = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid DraftMontagemId { get; private set; }
    public DraftMontagemPublicacaoDiscordTipo Tipo { get; private set; }
    public DraftMontagemPublicacaoDiscordStatus Status { get; private set; }
    public string? GuildId { get; private set; }
    public string? ChannelId { get; private set; }
    public string? MessageId { get; private set; }
    public string? UltimoErroCodigo { get; private set; }
    public DateTimeOffset? PublicadaEm { get; private set; }
    public DateTimeOffset UltimaTentativaEm { get; private set; }

    public void RegistrarPublicada(string? guildId, string? channelId, string messageId)
    {
        GuildId = Normalize(guildId);
        ChannelId = Normalize(channelId);
        MessageId = string.IsNullOrWhiteSpace(messageId) ? throw new ArgumentException(MessageCodes.FieldRequired, nameof(messageId)) : messageId.Trim();
        Status = DraftMontagemPublicacaoDiscordStatus.Publicada;
        UltimoErroCodigo = null;
        PublicadaEm = DateTimeOffset.UtcNow;
        UltimaTentativaEm = PublicadaEm.Value;
    }

    public void RegistrarFalha(string? guildId, string? channelId, string? erroCodigo)
    {
        GuildId = Normalize(guildId);
        ChannelId = Normalize(channelId);
        Status = DraftMontagemPublicacaoDiscordStatus.Falha;
        UltimoErroCodigo = Normalize(erroCodigo);
        UltimaTentativaEm = DateTimeOffset.UtcNow;
    }

    public void SolicitarRepublicacao()
    {
        Status = DraftMontagemPublicacaoDiscordStatus.Pendente;
        UltimoErroCodigo = null;
        UltimaTentativaEm = DateTimeOffset.UtcNow;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
