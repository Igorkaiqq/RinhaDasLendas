namespace RinhaDasLendas.Application.Dtos;

public sealed record RegistrarPublicacaoDiscordDraftMontagemRequestDto(
    string Tipo,
    Guid ClaimId,
    string? DiscordGuildId,
    string? DiscordChannelId,
    string MessageId);
