using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Dtos;

public sealed record ConfirmarPresencaDraftMontagemRequestDto(Guid? UsuarioId, string? DiscordUserId, string Origem);
