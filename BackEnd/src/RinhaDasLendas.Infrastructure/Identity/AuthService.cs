using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Services;
using RinhaDasLendas.Infrastructure.Persistence;

namespace RinhaDasLendas.Infrastructure.Identity;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    RinhaDasLendasDbContext dbContext,
    IOptions<AuthOptions> authOptions,
    IUsuarioAuditoriaService auditoriaService,
    RoleHierarchyService roleHierarchyService) : IAuthService
{
    private const string GenericRecoveryMessage = "Se o e-mail estiver cadastrado, enviaremos instrucoes de recuperacao.";

    public async Task<AuthenticatedUserDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken)
    {
        var existing = await userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
        {
            throw new DomainException("E-mail ja cadastrado.");
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Nome = request.Nome.Trim(),
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            Ativo = true,
            EmailConfirmed = false,
            DataCadastro = DateTimeOffset.UtcNow,
            DataAtualizacao = DateTimeOffset.UtcNow,
        };

        var createResult = await userManager.CreateAsync(user, request.Senha);
        EnsureIdentitySuccess(createResult);

        await EnsureRoleAsync(AuthRoles.Jogador);
        var roleResult = await userManager.AddToRoleAsync(user, AuthRoles.Jogador);
        EnsureIdentitySuccess(roleResult);

        await auditoriaService.RegistrarAsync("UsuarioCriado", user.Id, user.Id, "Cadastro publico", null, null, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await BuildUserDtoAsync(user, cancellationToken);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.Ativo)
        {
            await auditoriaService.RegistrarAsync("LoginFalha", user?.Id, null, "Credenciais invalidas ou usuario desativado", ipAddress, userAgent, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedAccessException("Credenciais invalidas.");
        }

        var validPassword = await userManager.CheckPasswordAsync(user, request.Senha);
        if (!validPassword)
        {
            await userManager.AccessFailedAsync(user);
            await auditoriaService.RegistrarAsync("LoginFalha", user.Id, null, "Credenciais invalidas", ipAddress, userAgent, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedAccessException("Credenciais invalidas.");
        }

        await userManager.ResetAccessFailedCountAsync(user);
        user.UltimoLoginEm = DateTimeOffset.UtcNow;
        user.DataAtualizacao = user.UltimoLoginEm.Value;

        var token = await CreateTokenAsync(user, null, ipAddress, userAgent, cancellationToken);
        await auditoriaService.RegistrarAsync("LoginSucesso", user.Id, user.Id, null, ipAddress, userAgent, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto(token.AccessToken, token.ExpiresIn, await BuildUserDtoAsync(user, cancellationToken)) { RefreshToken = token.RefreshToken };
    }

    public async Task<AuthResponseDto> RefreshAsync(string refreshToken, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        var tokenHash = Hash(refreshToken);
        var storedToken = await dbContext.RefreshTokens.FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
        if (storedToken is null)
        {
            throw new UnauthorizedAccessException("Refresh token invalido.");
        }

        var user = await userManager.FindByIdAsync(storedToken.UsuarioId.ToString());
        if (user is null || !user.Ativo)
        {
            throw new UnauthorizedAccessException("Usuario indisponivel.");
        }

        if (!storedToken.EstaAtivo(DateTimeOffset.UtcNow))
        {
            if (storedToken.RevogadoEm is not null && storedToken.SubstituidoPorTokenId is not null)
            {
                await RevokeFamilyAsync(storedToken.FamiliaId, "Reutilizacao detectada", ipAddress, cancellationToken);
                await auditoriaService.RegistrarAsync("RefreshTokenReutilizado", user.Id, user.Id, null, ipAddress, userAgent, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            throw new UnauthorizedAccessException("Refresh token invalido.");
        }

        storedToken.RevogadoEm = DateTimeOffset.UtcNow;
        storedToken.IpRevogacao = ipAddress;
        storedToken.MotivoRevogacao = "Rotacao";

        var newToken = await CreateTokenAsync(user, storedToken.FamiliaId, ipAddress, userAgent, cancellationToken);
        var replacement = await dbContext.RefreshTokens.FirstAsync(token => token.TokenHash == Hash(newToken.RefreshToken), cancellationToken);
        storedToken.SubstituidoPorTokenId = replacement.Id;

        await auditoriaService.RegistrarAsync("RefreshTokenRotacionado", user.Id, user.Id, null, ipAddress, userAgent, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto(newToken.AccessToken, newToken.ExpiresIn, await BuildUserDtoAsync(user, cancellationToken)) { RefreshToken = newToken.RefreshToken };
    }

    public async Task LogoutAsync(string? refreshToken, Guid? userId, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            var tokenHash = Hash(refreshToken);
            var storedToken = await dbContext.RefreshTokens.FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
            if (storedToken is not null && storedToken.RevogadoEm is null)
            {
                storedToken.RevogadoEm = DateTimeOffset.UtcNow;
                storedToken.IpRevogacao = ipAddress;
                storedToken.MotivoRevogacao = "Logout";
                userId ??= storedToken.UsuarioId;
            }
        }

        await auditoriaService.RegistrarAsync("Logout", userId, userId, null, ipAddress, userAgent, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ForgotPasswordResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is not null)
        {
            _ = await userManager.GeneratePasswordResetTokenAsync(user);
            await auditoriaService.RegistrarAsync("SenhaRedefinida", user.Id, user.Id, "Token de recuperacao gerado", null, null, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new ForgotPasswordResponseDto(GenericRecoveryMessage);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            throw new DomainException("Token de redefinicao invalido.");
        }

        var result = await userManager.ResetPasswordAsync(user, request.Token, request.NovaSenha);
        EnsureIdentitySuccess(result);

        await RevokeUserSessionsAsync(user.Id, "Senha redefinida", cancellationToken);
        await auditoriaService.RegistrarAsync("SenhaRedefinida", user.Id, user.Id, null, null, null, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequestDto request, CancellationToken cancellationToken)
    {
        var user = await FindUserOrThrowAsync(userId);
        var result = await userManager.ChangePasswordAsync(user, request.SenhaAtual, request.NovaSenha);
        EnsureIdentitySuccess(result);

        await RevokeUserSessionsAsync(user.Id, "Senha alterada", cancellationToken);
        await auditoriaService.RegistrarAsync("SenhaAlterada", user.Id, user.Id, null, null, null, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<AuthenticatedUserDto?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : await BuildUserDtoAsync(user, cancellationToken);
    }

    public async Task<AuthenticatedUserDto?> UpdateOwnProfileAsync(Guid userId, UpdateOwnProfileRequestDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return null;
        }

        user.Nome = request.Nome.Trim();
        user.DataAtualizacao = DateTimeOffset.UtcNow;
        EnsureIdentitySuccess(await userManager.UpdateAsync(user));
        return await BuildUserDtoAsync(user, cancellationToken);
    }

    public async Task<UserPermissionsDto> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await FindUserOrThrowAsync(userId);
        var roles = await userManager.GetRolesAsync(user);
        var effectiveRole = roleHierarchyService.GetEffectiveRole(roles);
        var permissions = BuildPermissions(roles).ToArray();
        return new UserPermissionsDto(roles.ToArray(), permissions, effectiveRole);
    }

    public async Task<DiscordLinkStatusDto> GetDiscordStatusAsync(Guid userId, CancellationToken cancellationToken)
    {
        var link = await dbContext.VinculosDiscord
            .AsNoTracking()
            .FirstOrDefaultAsync(vinculo => vinculo.UsuarioId == userId && vinculo.DesvinculadoEm == null, cancellationToken);

        return link is null
            ? new DiscordLinkStatusDto(false, null)
            : new DiscordLinkStatusDto(true, link.DiscordUsername ?? link.DiscordGlobalName, link.VinculadoEm);
    }

    private async Task<TokenResult> CreateTokenAsync(ApplicationUser user, Guid? familyId, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        var roles = await userManager.GetRolesAsync(user);
        var jogadorId = await dbContext.Jogadores
            .AsNoTracking()
            .Where(jogador => jogador.UsuarioId == user.Id)
            .Select(jogador => (Guid?)jogador.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var options = authOptions.Value.Jwt;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(options.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.Nome),
            new("security_stamp", user.SecurityStamp ?? string.Empty),
        };

        if (jogadorId is not null)
        {
            claims.Add(new Claim("jogador_id", jogadorId.Value.ToString()));
        }

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var jwt = new JwtSecurityToken(options.Issuer, options.Audience, claims, expires: expires, signingCredentials: credentials);
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        await dbContext.RefreshTokens.AddAsync(new RefreshToken
        {
            UsuarioId = user.Id,
            TokenHash = Hash(refreshToken),
            FamiliaId = familyId ?? Guid.NewGuid(),
            ExpiraEm = DateTimeOffset.UtcNow.AddDays(options.RefreshTokenDays),
            IpCriacao = ipAddress,
            UserAgentCriacao = userAgent,
        }, cancellationToken);

        return new TokenResult(new JwtSecurityTokenHandler().WriteToken(jwt), refreshToken, options.AccessTokenMinutes * 60);
    }

    private async Task<AuthenticatedUserDto> BuildUserDtoAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = await userManager.GetRolesAsync(user);
        var jogadorId = await dbContext.Jogadores
            .AsNoTracking()
            .Where(jogador => jogador.UsuarioId == user.Id)
            .Select(jogador => (Guid?)jogador.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var discord = await GetDiscordStatusAsync(user.Id, cancellationToken);
        return new AuthenticatedUserDto(user.Id, user.Nome, user.Email ?? string.Empty, roles.ToArray(), user.Ativo, jogadorId, discord);
    }

    private static IEnumerable<string> BuildPermissions(IEnumerable<string> roles)
    {
        var roleSet = roles.ToHashSet(StringComparer.OrdinalIgnoreCase);
        yield return AuthPermissions.CanEditOwnProfile;
        yield return AuthPermissions.CanConfirmPresence;

        if (roleSet.Contains(AuthRoles.SuperAdmin) || roleSet.Contains(AuthRoles.Admin))
        {
            yield return AuthPermissions.CanManageUsers;
            yield return AuthPermissions.CanViewUsers;
            yield return AuthPermissions.CanManageRoles;
            yield return AuthPermissions.CanResetUserPassword;
            yield return AuthPermissions.CanActivateDeactivateUsers;
            yield return AuthPermissions.CanManageDrafts;
            yield return AuthPermissions.CanManageMatches;
        }

        if (roleSet.Contains(AuthRoles.Moderador))
        {
            yield return AuthPermissions.CanViewUsers;
            yield return AuthPermissions.CanManageDrafts;
            yield return AuthPermissions.CanManageMatches;
        }

        if (roleSet.Contains(AuthRoles.SuperAdmin))
        {
            yield return AuthPermissions.CanViewAdminLogs;
        }
    }

    private async Task<ApplicationUser> FindUserOrThrowAsync(Guid userId)
    {
        return await userManager.FindByIdAsync(userId.ToString()) ?? throw new UnauthorizedAccessException("Usuario nao encontrado.");
    }

    private async Task EnsureRoleAsync(string role)
    {
        if (await roleManager.RoleExistsAsync(role))
        {
            return;
        }

        var level = AuthRoles.Levels[role];
        EnsureIdentitySuccess(await roleManager.CreateAsync(new ApplicationRole { Id = Guid.NewGuid(), Name = role, NivelHierarquico = level }));
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

    private async Task RevokeFamilyAsync(Guid familyId, string reason, string? ipAddress, CancellationToken cancellationToken)
    {
        var family = await dbContext.RefreshTokens.Where(token => token.FamiliaId == familyId && token.RevogadoEm == null).ToListAsync(cancellationToken);
        foreach (var token in family)
        {
            token.RevogadoEm = DateTimeOffset.UtcNow;
            token.IpRevogacao = ipAddress;
            token.MotivoRevogacao = reason;
        }
    }

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }

    private static void EnsureIdentitySuccess(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new DomainException(string.Join("; ", result.Errors.Select(error => error.Description)));
        }
    }
}
