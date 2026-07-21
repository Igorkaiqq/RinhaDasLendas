using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Domain.Models;

public sealed record DraftMontagemPublicacaoClaim(
    bool Adquirido,
    Guid? ClaimId,
    DateTimeOffset? ExpiraEm,
    DraftMontagemPublicacaoDiscordStatus Status);
