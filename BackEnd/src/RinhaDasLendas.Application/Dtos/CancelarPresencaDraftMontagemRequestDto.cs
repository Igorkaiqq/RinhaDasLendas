using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Dtos;

public sealed record CancelarPresencaDraftMontagemRequestDto(Guid? UsuarioId, string? DiscordUserId);
