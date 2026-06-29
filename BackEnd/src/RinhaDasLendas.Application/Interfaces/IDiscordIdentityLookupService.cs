using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Interfaces;

public interface IDiscordIdentityLookupService
{
    Task<DiscordUserLinkDto> GetByDiscordUserIdAsync(string discordUserId, CancellationToken cancellationToken);
}
