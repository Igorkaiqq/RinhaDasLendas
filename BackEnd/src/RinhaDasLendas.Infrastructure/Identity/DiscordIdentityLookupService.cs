using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Infrastructure.Persistence;

namespace RinhaDasLendas.Infrastructure.Identity;

public sealed class DiscordIdentityLookupService(
    RinhaDasLendasDbContext dbContext,
    UserManager<ApplicationUser> userManager) : IDiscordIdentityLookupService
{
    public async Task<DiscordUserLinkDto> GetByDiscordUserIdAsync(string discordUserId, CancellationToken cancellationToken)
    {
        var normalized = discordUserId.Trim();
        var account = await dbContext.ExternalAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(link => link.Provider == "Discord" && link.ProviderUserId == normalized && link.UnlinkedAt == null, cancellationToken);

        Guid? usuarioId = account?.UsuarioId;
        if (usuarioId is null)
        {
            var legacy = await dbContext.VinculosDiscord
                .AsNoTracking()
                .FirstOrDefaultAsync(link => link.DiscordUserId == normalized && link.DesvinculadoEm == null, cancellationToken);
            usuarioId = legacy?.UsuarioId;
        }

        if (usuarioId is null)
        {
            return new DiscordUserLinkDto(false, null, null, null, []);
        }

        var user = await userManager.FindByIdAsync(usuarioId.Value.ToString());
        if (user is null || !user.Ativo)
        {
            return new DiscordUserLinkDto(false, null, null, null, []);
        }

        var jogador = await dbContext.Jogadores.AsNoTracking().FirstOrDefaultAsync(item => item.UsuarioId == user.Id, cancellationToken);
        var roles = await userManager.GetRolesAsync(user);
        return new DiscordUserLinkDto(jogador is not null, user.Id, jogador?.Id, jogador?.NomeExibicao, roles.ToList());
    }
}
