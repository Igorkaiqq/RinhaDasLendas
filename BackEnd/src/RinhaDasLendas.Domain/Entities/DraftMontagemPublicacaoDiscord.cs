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
        : this(tipo, guildId, channelId, DateTimeOffset.UtcNow)
    {
    }

    internal DraftMontagemPublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo tipo, string? guildId, string? channelId, DateTimeOffset agora)
    {
        Id = Guid.NewGuid();
        Tipo = tipo;
        GuildId = Normalize(guildId);
        ChannelId = Normalize(channelId);
        Status = DraftMontagemPublicacaoDiscordStatus.Pendente;
        UltimaTentativaEm = agora;
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

    internal static void ValidarInicioTentativa(Guid claimId, DateTimeOffset expiraEm, DateTimeOffset agora)
    {
        if (claimId == Guid.Empty)
        {
            throw new DomainException(MessageCodes.DiscordPublicationClaimInvalid);
        }

        if (expiraEm <= agora)
        {
            throw new DomainException(MessageCodes.DiscordPublicationClaimExpirationInvalid);
        }
    }

    internal void IniciarTentativa(Guid claimId, DateTimeOffset expiraEm, DateTimeOffset agora)
    {
        ValidarInicioTentativa(claimId, expiraEm, agora);
        if (Status != DraftMontagemPublicacaoDiscordStatus.Pendente)
        {
            throw new DomainException(MessageCodes.DiscordPublicationNotPending);
        }

        ClaimId = claimId;
        ClaimExpiraEm = expiraEm;
        UltimaTentativaEm = agora;
        Status = DraftMontagemPublicacaoDiscordStatus.EmAndamento;
    }

    internal void RegistrarPublicada(Guid claimId, string? guildId, string? channelId, string messageId, DateTimeOffset agora)
    {
        EnsureClaimAtivo(claimId, agora);
        GuildId = Normalize(guildId);
        ChannelId = Normalize(channelId);
        MessageId = string.IsNullOrWhiteSpace(messageId) ? throw new ArgumentException(MessageCodes.FieldRequired, nameof(messageId)) : messageId.Trim();
        Status = DraftMontagemPublicacaoDiscordStatus.Publicada;
        UltimoErroCodigo = null;
        PublicadaEm = agora;
        UltimaTentativaEm = agora;
        ClaimExpiraEm = null;
    }

    internal void RegistrarFalha(Guid claimId, string? guildId, string? channelId, string? erroCodigo, DateTimeOffset agora)
    {
        EnsureClaimAtivo(claimId, agora);
        GuildId = Normalize(guildId);
        ChannelId = Normalize(channelId);
        Status = DraftMontagemPublicacaoDiscordStatus.Falha;
        UltimoErroCodigo = Normalize(erroCodigo);
        UltimaTentativaEm = agora;
        ClaimExpiraEm = null;
    }

    internal bool MarcarRequerReconciliacao(DateTimeOffset agora)
    {
        if (Status != DraftMontagemPublicacaoDiscordStatus.EmAndamento || ClaimExpiraEm is null || ClaimExpiraEm > agora)
        {
            return false;
        }

        Status = DraftMontagemPublicacaoDiscordStatus.RequerReconciliacao;
        ClaimExpiraEm = null;
        return true;
    }

    internal bool SolicitarRepublicacao(DateTimeOffset agora, bool confirmarAusenciaPublicacao)
    {
        if (Status == DraftMontagemPublicacaoDiscordStatus.Pendente)
        {
            return false;
        }

        if (Status == DraftMontagemPublicacaoDiscordStatus.EmAndamento)
        {
            throw new DomainException(MessageCodes.DiscordPublicationInProgress);
        }

        if (Status == DraftMontagemPublicacaoDiscordStatus.Publicada && !confirmarAusenciaPublicacao)
        {
            throw new DomainException(MessageCodes.DiscordPublicationStillPublished);
        }

        if (Status is not (DraftMontagemPublicacaoDiscordStatus.Falha
            or DraftMontagemPublicacaoDiscordStatus.Publicada
            or DraftMontagemPublicacaoDiscordStatus.RequerReconciliacao))
        {
            throw new DomainException(MessageCodes.DiscordPublicationNotPending);
        }

        Status = DraftMontagemPublicacaoDiscordStatus.Pendente;
        UltimoErroCodigo = null;
        ClaimId = null;
        ClaimExpiraEm = null;
        UltimaTentativaEm = agora;
        return true;
    }

    private void EnsureClaimAtivo(Guid claimId, DateTimeOffset agora)
    {
        if (claimId == Guid.Empty)
        {
            throw new DomainException(MessageCodes.DiscordPublicationClaimInvalid);
        }

        if (Status != DraftMontagemPublicacaoDiscordStatus.EmAndamento || ClaimId != claimId)
        {
            throw new DomainException(MessageCodes.DiscordPublicationClaimMismatch);
        }

        if (ClaimExpiraEm is null || agora >= ClaimExpiraEm)
        {
            throw new DomainException(MessageCodes.DiscordPublicationClaimExpired);
        }
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
