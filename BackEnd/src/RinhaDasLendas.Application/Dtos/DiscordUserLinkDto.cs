using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Dtos;

public sealed record DiscordUserLinkDto(bool Vinculado, Guid? UsuarioId, Guid? JogadorId, string? NomeExibicao, IReadOnlyCollection<string> Roles);
