using RinhaDasLendas.Application.Dtos;

namespace RinhaDasLendas.Application.Interfaces;

public interface IDiscordConfigurationService
{
    Task<DiscordConfigurationDto?> GetAsync(CancellationToken cancellationToken);
    Task<DiscordConfigurationDto> SaveAsync(DiscordConfigurationDto request, CancellationToken cancellationToken);
}
