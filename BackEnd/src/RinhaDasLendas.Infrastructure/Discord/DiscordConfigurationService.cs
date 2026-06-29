using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Infrastructure.Persistence;

namespace RinhaDasLendas.Infrastructure.Discord;

public sealed class DiscordConfigurationService(
    RinhaDasLendasDbContext dbContext,
    IValidator<DiscordConfigurationDto> validator) : IDiscordConfigurationService
{
    public async Task<DiscordConfigurationDto?> GetAsync(CancellationToken cancellationToken)
    {
        var configuration = await dbContext.DiscordServerConfigurations.AsNoTracking().OrderBy(item => item.CreatedAt).FirstOrDefaultAsync(cancellationToken);
        return configuration is null ? null : ToDto(configuration);
    }

    public async Task<DiscordConfigurationDto> SaveAsync(DiscordConfigurationDto request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var configuration = await dbContext.DiscordServerConfigurations.FirstOrDefaultAsync(item => item.GuildId == request.GuildId, cancellationToken);
        if (configuration is null)
        {
            configuration = new DiscordServerConfiguration(request.GuildId, request.PresenceChannelId, request.NewsChannelId, request.AdminChannelId, request.DraftChannelId, request.MatchResultChannelId, request.BotEnabled);
            await dbContext.DiscordServerConfigurations.AddAsync(configuration, cancellationToken);
        }
        else
        {
            configuration.Atualizar(request.GuildId, request.PresenceChannelId, request.NewsChannelId, request.AdminChannelId, request.DraftChannelId, request.MatchResultChannelId, request.BotEnabled);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(configuration);
    }

    private static DiscordConfigurationDto ToDto(DiscordServerConfiguration configuration)
    {
        return new DiscordConfigurationDto(
            configuration.Id,
            configuration.GuildId,
            configuration.PresenceChannelId,
            configuration.NewsChannelId,
            configuration.AdminChannelId,
            configuration.DraftChannelId,
            configuration.MatchResultChannelId,
            configuration.BotEnabled);
    }
}
