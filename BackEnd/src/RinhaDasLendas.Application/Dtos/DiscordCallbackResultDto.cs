using System.Text.Json.Serialization;

namespace RinhaDasLendas.Application.Dtos;

public sealed record DiscordCallbackResultDto(string RedirectUrl, AuthResponseDto? AuthResponse = null);
