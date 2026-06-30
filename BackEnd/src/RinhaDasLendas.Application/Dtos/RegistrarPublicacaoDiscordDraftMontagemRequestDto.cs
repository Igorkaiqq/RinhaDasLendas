using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Dtos;

public sealed record RegistrarPublicacaoDiscordDraftMontagemRequestDto(string? DiscordGuildId, string DiscordPresenceMessageId);
