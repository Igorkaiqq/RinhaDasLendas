using System.Text.Json.Serialization;

namespace RinhaDasLendas.Application.Dtos;

public sealed record DiscordLinkStatusDto(bool Vinculado, string? Username, DateTimeOffset? VinculadoEm = null);
