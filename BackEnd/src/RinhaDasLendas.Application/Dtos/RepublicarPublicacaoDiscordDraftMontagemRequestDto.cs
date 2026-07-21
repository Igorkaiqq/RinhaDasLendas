using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Dtos;

public sealed record RepublicarPublicacaoDiscordDraftMontagemRequestDto(DraftMontagemPublicacaoDiscordTipo Tipo, string? Motivo);
