using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Services;
using RinhaDasLendas.Infrastructure.Persistence;

namespace RinhaDasLendas.Infrastructure.Identity;

public sealed class UsuarioService(
    UserManager<ApplicationUser> userManager,
    RinhaDasLendasDbContext dbContext,
    ICurrentUser currentUser,
    IUsuarioAuditoriaService auditoriaService,
    RoleHierarchyService roleHierarchyService) : IUsuarioService
{
    public async Task<PaginatedResponseDto<UsuarioResumoDto>> ListAsync(string? search, string? nome, string? email, string? role, string? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = dbContext.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim().ToUpperInvariant();
            query = query.Where(user => user.Nome.ToUpper().Contains(value) || (user.NormalizedEmail != null && user.NormalizedEmail.Contains(value)));
        }

        if (!string.IsNullOrWhiteSpace(nome))
        {
            query = query.Where(user => user.Nome.ToUpper().Contains(nome.Trim().ToUpperInvariant()));
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            query = query.Where(user => user.NormalizedEmail != null && user.NormalizedEmail.Contains(email.Trim().ToUpperInvariant()));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var ativo = status.Equals("Ativo", StringComparison.OrdinalIgnoreCase) || status.Equals("true", StringComparison.OrdinalIgnoreCase);
            query = query.Where(user => user.Ativo == ativo);
        }

        var users = await query.OrderBy(user => user.Nome).ToListAsync(cancellationToken);
        var filtered = new List<ApplicationUser>();
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            if (!string.IsNullOrWhiteSpace(role) && !roles.Contains(role, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            if (CanView(user, roles))
            {
                filtered.Add(user);
            }
        }

        var total = filtered.Count;
        var items = new List<UsuarioResumoDto>();
        foreach (var user in filtered.Skip((page - 1) * pageSize).Take(pageSize))
        {
            items.Add(await BuildResumoAsync(user, cancellationToken));
        }

        return new PaginatedResponseDto<UsuarioResumoDto>(page, pageSize, items, total, (int)Math.Ceiling(total / (double)pageSize));
    }

    public async Task<UsuarioResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return null;
        }

        var roles = await userManager.GetRolesAsync(user);
        return CanView(user, roles) ? await BuildResponseAsync(user, cancellationToken) : throw new UnauthorizedAccessException(MessageCodes.InsufficientPermission);
    }

    public async Task<UsuarioResponseDto?> UpdateAsync(Guid id, UpdateUsuarioRequestDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return null;
        }

        await EnsureCanManageAsync(user);
        user.Nome = request.Nome.Trim();
        user.DataAtualizacao = DateTimeOffset.UtcNow;
        EnsureIdentitySuccess(await userManager.UpdateAsync(user));
        await auditoriaService.RegistrarAsync("UserUpdated", user.Id, currentUser.UserId, null, currentUser.IpAddress, currentUser.UserAgent, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildResponseAsync(user, cancellationToken);
    }

    public async Task<UsuarioRolesResponseDto?> UpdateRolesAsync(Guid id, UpdateUsuarioRolesRequestDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return null;
        }

        if (currentUser.UserId == id)
        {
            throw new DomainException(MessageCodes.UserCannotChangeOwnRoles);
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        var requested = request.Roles.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (!roleHierarchyService.CanAssignRoles(currentUser.Roles, currentRoles, requested))
        {
            throw new UnauthorizedAccessException(MessageCodes.RoleHierarchyOperationDenied);
        }

        await EnsureAtLeastOneSuperAdminAsync(user, currentRoles, requested, cancellationToken);

        EnsureIdentitySuccess(await userManager.RemoveFromRolesAsync(user, currentRoles));
        EnsureIdentitySuccess(await userManager.AddToRolesAsync(user, requested));
        user.DataAtualizacao = DateTimeOffset.UtcNow;
        EnsureIdentitySuccess(await userManager.UpdateAsync(user));
        await auditoriaService.RegistrarAsync("RolesUpdated", user.Id, currentUser.UserId, string.Join(",", requested), currentUser.IpAddress, currentUser.UserAgent, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new UsuarioRolesResponseDto(user.Id, requested, user.DataAtualizacao);
    }

    public async Task<UsuarioResponseDto?> AtivarAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return null;
        }

        await EnsureCanManageAsync(user);
        user.Ativo = true;
        user.DataAtualizacao = DateTimeOffset.UtcNow;
        EnsureIdentitySuccess(await userManager.UpdateAsync(user));
        await auditoriaService.RegistrarAsync("UserActivated", user.Id, currentUser.UserId, null, currentUser.IpAddress, currentUser.UserAgent, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildResponseAsync(user, cancellationToken);
    }

    public async Task<UsuarioResponseDto?> DesativarAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return null;
        }

        await EnsureCanManageAsync(user);
        var currentRoles = await userManager.GetRolesAsync(user);
        await EnsureAtLeastOneSuperAdminAsync(user, currentRoles, currentRoles, cancellationToken, deactivating: true);

        user.Ativo = false;
        user.DataAtualizacao = DateTimeOffset.UtcNow;
        EnsureIdentitySuccess(await userManager.UpdateAsync(user));
        await RevokeUserSessionsAsync(user.Id, "UserDeactivated", cancellationToken);
        await auditoriaService.RegistrarAsync("UserDeactivated", user.Id, currentUser.UserId, null, currentUser.IpAddress, currentUser.UserAgent, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildResponseAsync(user, cancellationToken);
    }

    public async Task ResetPasswordAsync(Guid id, ResetUsuarioPasswordRequestDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(id.ToString()) ?? throw new DomainException(MessageCodes.UserNotFound);
        await EnsureCanManageAsync(user);

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        EnsureIdentitySuccess(await userManager.ResetPasswordAsync(user, token, request.NovaSenha));
        await RevokeUserSessionsAsync(user.Id, "AdminPasswordReset", cancellationToken);
        await auditoriaService.RegistrarAsync("AdminPasswordReset", user.Id, currentUser.UserId, null, currentUser.IpAddress, currentUser.UserAgent, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<RoleListResponseDto> GetAssignableRolesAsync(CancellationToken cancellationToken)
    {
        var roles = roleHierarchyService.GetAssignableRoles(currentUser.Roles)
            .Select(role => new RoleResponseDto(role, AuthRoles.Levels[role]))
            .OrderByDescending(role => role.Nivel)
            .ToArray();
        return Task.FromResult(new RoleListResponseDto(roles));
    }

    public async Task<PaginatedResponseDto<UsuarioAuditoriaResponseDto>> GetAuditoriaAsync(Guid id, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = dbContext.AuditoriaUsuarios.AsNoTracking().Where(audit => audit.UsuarioAlvoId == id).OrderByDescending(audit => audit.DataCadastro);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(audit => new UsuarioAuditoriaResponseDto(audit.Id, audit.Acao, audit.UsuarioAlvoId, audit.UsuarioExecutorId, audit.DataCadastro, audit.Detalhes))
            .ToListAsync(cancellationToken);
        return new PaginatedResponseDto<UsuarioAuditoriaResponseDto>(page, pageSize, items, total, (int)Math.Ceiling(total / (double)pageSize));
    }

    private bool CanView(ApplicationUser target, IEnumerable<string> targetRoles)
    {
        return currentUser.Roles.Contains(AuthRoles.SuperAdmin)
            || currentUser.Roles.Contains(AuthRoles.Moderador)
            || roleHierarchyService.CanAdminister(currentUser.Roles, targetRoles)
            || currentUser.UserId == target.Id;
    }

    private async Task EnsureCanManageAsync(ApplicationUser target)
    {
        var targetRoles = await userManager.GetRolesAsync(target);
        if (!roleHierarchyService.CanAdminister(currentUser.Roles, targetRoles))
        {
            throw new UnauthorizedAccessException(MessageCodes.InsufficientPermission);
        }
    }

    private async Task<UsuarioResumoDto> BuildResumoAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = await userManager.GetRolesAsync(user);
        var jogadorId = await GetJogadorIdAsync(user.Id, cancellationToken);
        var discord = await HasDiscordAsync(user.Id, cancellationToken);
        return new UsuarioResumoDto(user.Id, user.Nome, user.Email ?? string.Empty, roles.ToArray(), user.Ativo, jogadorId, discord, user.DataCadastro, user.UltimoLoginEm, BuildActions(user, roles));
    }

    private async Task<UsuarioResponseDto> BuildResponseAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = await userManager.GetRolesAsync(user);
        var jogadorId = await GetJogadorIdAsync(user.Id, cancellationToken);
        var discord = await dbContext.ExternalAccounts.AsNoTracking().FirstOrDefaultAsync(link => link.UsuarioId == user.Id && link.Provider == "Discord" && link.UnlinkedAt == null, cancellationToken);
        var legacyDiscord = discord is null
            ? await dbContext.VinculosDiscord.AsNoTracking().FirstOrDefaultAsync(link => link.UsuarioId == user.Id && link.DesvinculadoEm == null, cancellationToken)
            : null;
        return new UsuarioResponseDto(
            user.Id,
            user.Nome,
            user.Email ?? string.Empty,
            roles.ToArray(),
            user.Ativo,
            jogadorId,
            discord is not null
                ? new DiscordLinkStatusDto(true, discord.Username ?? discord.DisplayName, discord.LinkedAt)
                : legacyDiscord is null ? new DiscordLinkStatusDto(false, null) : new DiscordLinkStatusDto(true, legacyDiscord.DiscordUsername ?? legacyDiscord.DiscordGlobalName, legacyDiscord.VinculadoEm),
            user.DataCadastro,
            user.DataAtualizacao,
            user.UltimoLoginEm,
            BuildActions(user, roles));
    }

    private IReadOnlyCollection<string> BuildActions(ApplicationUser target, IEnumerable<string> roles)
    {
        if (!roleHierarchyService.CanAdminister(currentUser.Roles, roles))
        {
            return Array.Empty<string>();
        }

        return new[] { "edit", "updateRoles", target.Ativo ? "deactivate" : "activate", "resetPassword" };
    }

    private Task<Guid?> GetJogadorIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return dbContext.Jogadores.AsNoTracking().Where(jogador => jogador.UsuarioId == userId).Select(jogador => (Guid?)jogador.Id).FirstOrDefaultAsync(cancellationToken);
    }

    private Task<bool> HasDiscordAsync(Guid userId, CancellationToken cancellationToken)
    {
        return dbContext.ExternalAccounts.AsNoTracking().AnyAsync(link => link.UsuarioId == userId && link.Provider == "Discord" && link.UnlinkedAt == null, cancellationToken);
    }

    private async Task EnsureAtLeastOneSuperAdminAsync(ApplicationUser user, IEnumerable<string> currentRoles, IEnumerable<string> requestedRoles, CancellationToken cancellationToken, bool deactivating = false)
    {
        if (!currentRoles.Contains(AuthRoles.SuperAdmin) || (!deactivating && requestedRoles.Contains(AuthRoles.SuperAdmin)))
        {
            return;
        }

        var superAdminRole = await dbContext.Roles.FirstAsync(role => role.Name == AuthRoles.SuperAdmin, cancellationToken);
        var activeSuperAdmins = await dbContext.UserRoles
            .Where(userRole => userRole.RoleId == superAdminRole.Id && userRole.UserId != user.Id)
            .Join(dbContext.Users, userRole => userRole.UserId, appUser => appUser.Id, (_, appUser) => appUser)
            .CountAsync(appUser => appUser.Ativo, cancellationToken);

        if (activeSuperAdmins == 0)
        {
            throw new DomainException(MessageCodes.MustKeepActiveSuperAdmin);
        }
    }

    private async Task RevokeUserSessionsAsync(Guid userId, string reason, CancellationToken cancellationToken)
    {
        var tokens = await dbContext.RefreshTokens.Where(token => token.UsuarioId == userId && token.RevogadoEm == null).ToListAsync(cancellationToken);
        foreach (var token in tokens)
        {
            token.RevogadoEm = DateTimeOffset.UtcNow;
            token.MotivoRevogacao = reason;
        }
    }

    private static void EnsureIdentitySuccess(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new DomainException(MessageCodes.RequestProcessingFailed);
        }
    }
}
