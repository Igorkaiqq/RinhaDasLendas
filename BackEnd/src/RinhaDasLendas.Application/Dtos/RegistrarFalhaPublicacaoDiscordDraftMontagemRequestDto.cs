namespace RinhaDasLendas.Application.Dtos;

public sealed record RegistrarFalhaPublicacaoDiscordDraftMontagemRequestDto(
    string Tipo,
    Guid ClaimId,
    string? DiscordGuildId,
    string? DiscordChannelId,
    string? ErroCodigo);
