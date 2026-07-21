using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;

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
    public Guid? ClaimId { get; private set; }
    public DateTimeOffset? ClaimExpiraEm { get; private set; }

    public void RegistrarPublicada(string? guildId, string? channelId, string messageId)
    {
        throw new DomainException(MessageCodes.DiscordPublicationClaimMismatch);
    }

    public void IniciarTentativa(Guid claimId, DateTimeOffset expiraEm, DateTimeOffset agora)
    {
        if (Status != DraftMontagemPublicacaoDiscordStatus.Pendente)
        {
            throw new DomainException(MessageCodes.DiscordPublicationNotPending);
        }

        ClaimId = claimId;
        ClaimExpiraEm = expiraEm;
        UltimaTentativaEm = agora;
        Status = DraftMontagemPublicacaoDiscordStatus.EmAndamento;
    }

    public void RegistrarPublicada(Guid claimId, string? guildId, string? channelId, string messageId, DateTimeOffset agora)
    {
        EnsureClaimAtivo(claimId);
        GuildId = Normalize(guildId);
        ChannelId = Normalize(channelId);
        MessageId = string.IsNullOrWhiteSpace(messageId) ? throw new ArgumentException(MessageCodes.FieldRequired, nameof(messageId)) : messageId.Trim();
        Status = DraftMontagemPublicacaoDiscordStatus.Publicada;
        UltimoErroCodigo = null;
        PublicadaEm = agora;
        UltimaTentativaEm = agora;
        ClaimExpiraEm = null;
    }

    public void RegistrarFalha(string? guildId, string? channelId, string? erroCodigo)
    {
        throw new DomainException(MessageCodes.DiscordPublicationClaimMismatch);
    }

    public void RegistrarFalha(Guid claimId, string? guildId, string? channelId, string? erroCodigo, DateTimeOffset agora)
    {
        EnsureClaimAtivo(claimId);
        GuildId = Normalize(guildId);
        ChannelId = Normalize(channelId);
        Status = DraftMontagemPublicacaoDiscordStatus.Falha;
        UltimoErroCodigo = Normalize(erroCodigo);
        UltimaTentativaEm = agora;
        ClaimExpiraEm = null;
    }

    public bool MarcarRequerReconciliacao(DateTimeOffset agora)
    {
        if (Status != DraftMontagemPublicacaoDiscordStatus.EmAndamento || ClaimExpiraEm is null || ClaimExpiraEm > agora)
        {
            return false;
        }

        Status = DraftMontagemPublicacaoDiscordStatus.RequerReconciliacao;
        ClaimExpiraEm = null;
        return true;
    }

    public void SolicitarRepublicacao(DateTimeOffset agora)
    {
        Status = DraftMontagemPublicacaoDiscordStatus.Pendente;
        UltimoErroCodigo = null;
        ClaimId = null;
        ClaimExpiraEm = null;
        UltimaTentativaEm = agora;
    }

    private void EnsureClaimAtivo(Guid claimId)
    {
        if (Status != DraftMontagemPublicacaoDiscordStatus.EmAndamento || ClaimId != claimId)
        {
            throw new DomainException(MessageCodes.DiscordPublicationClaimMismatch);
        }
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
