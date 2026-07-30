namespace RinhaDasLendas.Domain.Models;

public sealed record DraftMontagemPublicacaoClaimResult(
    DraftMontagemPublicacaoClaim Claim,
    DraftMontagemVersionStamp? VersionStamp);
