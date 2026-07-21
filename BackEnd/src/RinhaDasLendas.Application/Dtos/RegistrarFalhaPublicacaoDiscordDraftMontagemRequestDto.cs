namespace RinhaDasLendas.Application.Dtos;

public sealed record RegistrarFalhaPublicacaoDiscordDraftMontagemRequestDto(string Tipo, string? DiscordGuildId, string? DiscordChannelId, string? ErroCodigo);
