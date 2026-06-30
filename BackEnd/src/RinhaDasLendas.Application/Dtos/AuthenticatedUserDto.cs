using System.Text.Json.Serialization;

namespace RinhaDasLendas.Application.Dtos;

public sealed record AuthenticatedUserDto(
    Guid Id,
    string Nome,
    string Email,
    IReadOnlyCollection<string> Roles,
    bool Ativo,
    Guid? JogadorId,
    DiscordLinkStatusDto? Discord = null);
