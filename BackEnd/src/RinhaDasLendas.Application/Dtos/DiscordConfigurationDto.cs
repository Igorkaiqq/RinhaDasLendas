using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Dtos;

public sealed record DiscordConfigurationDto(Guid? Id, string GuildId, string PresenceChannelId, string NewsChannelId, string AdminChannelId, string DraftChannelId, string MatchResultChannelId, bool BotEnabled);
