using RinhaDasLendas.Domain.Models;

namespace RinhaDasLendas.Application.Dtos;

public sealed record ClaimPublicacaoDiscordResponseDto(
    bool Adquirido,
    Guid? ClaimId,
    DateTimeOffset? ExpiraEm,
    string Status)
{
    public static ClaimPublicacaoDiscordResponseDto FromModel(DraftMontagemPublicacaoClaim claim)
    {
        return new(claim.Adquirido, claim.ClaimId, claim.ExpiraEm, claim.Status.ToString());
    }
}
