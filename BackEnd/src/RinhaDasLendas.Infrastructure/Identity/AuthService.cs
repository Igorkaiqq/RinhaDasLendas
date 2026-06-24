using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    IOptions<DiscordOAuthOptions> discordOptions,
    HttpClient httpClient,
    IUsuarioAuditoriaService auditoriaService,
    RoleHierarchyService roleHierarchyService,
    IMessageProvider messages) : IAuthService
{
    private const string DiscordProvider = "Discord";
    private const string LoginFlow = "Login";
    private const string LinkFlow = "Link";

    public async Task<AuthenticatedUserDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken)
    {
        var existing = await userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
        {
            throw new DomainException(MessageCodes.EmailAlreadyRegistered);
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

        await auditoriaService.RegistrarAsync("UserCreated", user.Id, user.Id, MessageCodes.UserCreated, null, null, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await BuildUserDtoAsync(user, cancellationToken);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.Ativo)
        {
            await auditoriaService.RegistrarAsync("LoginFailed", user?.Id, null, MessageCodes.InvalidCredentials, ipAddress, userAgent, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedAccessException(MessageCodes.InvalidCredentials);
        }

        var validPassword = await userManager.CheckPasswordAsync(user, request.Senha);
        if (!validPassword)
        {
            await userManager.AccessFailedAsync(user);
            await auditoriaService.RegistrarAsync("LoginFailed", user.Id, null, MessageCodes.InvalidCredentials, ipAddress, userAgent, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedAccessException(MessageCodes.InvalidCredentials);
        }

        await userManager.ResetAccessFailedCountAsync(user);
        user.UltimoLoginEm = DateTimeOffset.UtcNow;
        user.DataAtualizacao = user.UltimoLoginEm.Value;

        var token = await CreateTokenAsync(user, null, ipAddress, userAgent, cancellationToken);
        await auditoriaService.RegistrarAsync("LoginSucceeded", user.Id, user.Id, null, ipAddress, userAgent, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto(token.AccessToken, token.ExpiresIn, await BuildUserDtoAsync(user, cancellationToken)) { RefreshToken = token.RefreshToken };
    }

    public async Task<AuthResponseDto> RefreshAsync(string refreshToken, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        var tokenHash = Hash(refreshToken);
        var storedToken = await dbContext.RefreshTokens.FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
        if (storedToken is null)
        {
            throw new UnauthorizedAccessException(MessageCodes.InvalidRefreshToken);
        }

        var user = await userManager.FindByIdAsync(storedToken.UsuarioId.ToString());
        if (user is null || !user.Ativo)
        {
            throw new UnauthorizedAccessException(MessageCodes.UserDeactivated);
        }

        if (!storedToken.EstaAtivo(DateTimeOffset.UtcNow))
        {
            if (storedToken.RevogadoEm is not null && storedToken.SubstituidoPorTokenId is not null)
            {
                await RevokeFamilyAsync(storedToken.FamiliaId, "RefreshTokenReuseDetected", ipAddress, cancellationToken);
                await auditoriaService.RegistrarAsync("RefreshTokenReused", user.Id, user.Id, null, ipAddress, userAgent, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            throw new UnauthorizedAccessException(MessageCodes.InvalidRefreshToken);
        }

        storedToken.RevogadoEm = DateTimeOffset.UtcNow;
        storedToken.IpRevogacao = ipAddress;
        storedToken.MotivoRevogacao = "Rotation";

        var newToken = await CreateTokenAsync(user, storedToken.FamiliaId, ipAddress, userAgent, cancellationToken);
        storedToken.SubstituidoPorTokenId = newToken.RefreshTokenId;

        await auditoriaService.RegistrarAsync("RefreshTokenRotated", user.Id, user.Id, null, ipAddress, userAgent, cancellationToken);
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
            await auditoriaService.RegistrarAsync("PasswordResetRequested", user.Id, user.Id, MessageCodes.PasswordRecoveryRequested, null, null, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new ForgotPasswordResponseDto(messages.GetMessage(MessageCodes.PasswordRecoveryRequested));
    }

    public async Task ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            throw new DomainException(MessageCodes.InvalidPasswordResetToken);
        }

        var result = await userManager.ResetPasswordAsync(user, request.Token, request.NovaSenha);
        EnsureIdentitySuccess(result);

        await RevokeUserSessionsAsync(user.Id, "PasswordReset", cancellationToken);
        await auditoriaService.RegistrarAsync("PasswordReset", user.Id, user.Id, null, null, null, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequestDto request, CancellationToken cancellationToken)
    {
        var user = await FindUserOrThrowAsync(userId);
        var result = await userManager.ChangePasswordAsync(user, request.SenhaAtual, request.NovaSenha);
        EnsureIdentitySuccess(result);

        await RevokeUserSessionsAsync(user.Id, "PasswordChanged", cancellationToken);
        await auditoriaService.RegistrarAsync("PasswordChanged", user.Id, user.Id, null, null, null, cancellationToken);
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
        var account = await dbContext.ExternalAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(link => link.UsuarioId == userId && link.Provider == DiscordProvider && link.UnlinkedAt == null, cancellationToken);

        if (account is not null)
        {
            return new DiscordLinkStatusDto(true, account.Username ?? account.DisplayName, account.LinkedAt);
        }

        var legacyLink = await dbContext.VinculosDiscord
            .AsNoTracking()
            .FirstOrDefaultAsync(vinculo => vinculo.UsuarioId == userId && vinculo.DesvinculadoEm == null, cancellationToken);

        return legacyLink is null
            ? new DiscordLinkStatusDto(false, null)
            : new DiscordLinkStatusDto(true, legacyLink.DiscordUsername ?? legacyLink.DiscordGlobalName, legacyLink.VinculadoEm);
    }

    public async Task<ExternalAuthStartDto> StartDiscordLoginAsync(CancellationToken cancellationToken)
    {
        EnsureDiscordConfigured();
        var state = await CreateExternalStateAsync(LoginFlow, null, cancellationToken);
        return new ExternalAuthStartDto(BuildDiscordAuthorizationUrl(state));
    }

    public async Task<ExternalAuthStartDto> StartDiscordLinkAsync(Guid userId, CancellationToken cancellationToken)
    {
        EnsureDiscordConfigured();
        _ = await FindUserOrThrowAsync(userId);
        var state = await CreateExternalStateAsync(LinkFlow, userId, cancellationToken);
        return new ExternalAuthStartDto(BuildDiscordAuthorizationUrl(state));
    }

    public async Task<DiscordCallbackResultDto> HandleDiscordCallbackAsync(string? code, string? state, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        EnsureDiscordConfigured();
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
        {
            return RedirectError(MessageCodes.DiscordOAuthFailed, LoginFlow);
        }

        var storedState = await ConsumeExternalStateAsync(state, cancellationToken);
        if (storedState is null)
        {
            return RedirectError(MessageCodes.DiscordOAuthStateInvalid, LoginFlow);
        }

        var token = await ExchangeDiscordCodeAsync(code, cancellationToken);
        if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return RedirectError(MessageCodes.DiscordOAuthFailed, storedState.Flow);
        }

        var discordUser = await GetDiscordUserAsync(token.AccessToken, cancellationToken);
        if (discordUser is null || string.IsNullOrWhiteSpace(discordUser.Id))
        {
            await RevokeDiscordTokenAsync(token.AccessToken, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            return RedirectError(MessageCodes.DiscordOAuthFailed, storedState.Flow);
        }

        var result = storedState.Flow == LinkFlow
            ? await HandleDiscordLinkCallbackAsync(storedState, discordUser, cancellationToken)
            : await HandleDiscordLoginCallbackAsync(discordUser, ipAddress, userAgent, cancellationToken);

        await RevokeDiscordTokenAsync(token.AccessToken, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return result;
    }

    public async Task UnlinkDiscordAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await FindUserOrThrowAsync(userId);
        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            throw new DomainException(MessageCodes.TraditionalLoginRequired);
        }

        var account = await dbContext.ExternalAccounts
            .FirstOrDefaultAsync(link => link.UsuarioId == userId && link.Provider == DiscordProvider && link.UnlinkedAt == null, cancellationToken);

        if (account is null)
        {
            throw new DomainException(MessageCodes.DiscordAccountNotLinked);
        }

        account.UnlinkedAt = DateTimeOffset.UtcNow;
        await auditoriaService.RegistrarAsync("DiscordUnlinked", user.Id, user.Id, MessageCodes.DiscordUnlinked, null, null, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<DiscordCallbackResultDto> HandleDiscordLoginCallbackAsync(DiscordUserResponse discordUser, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        var account = await dbContext.ExternalAccounts
            .FirstOrDefaultAsync(link => link.Provider == DiscordProvider && link.ProviderUserId == discordUser.Id && link.UnlinkedAt == null, cancellationToken);

        if (account is null)
        {
            return await CreateDiscordUserAndLoginAsync(discordUser, ipAddress, userAgent, cancellationToken);
        }

        var user = await userManager.FindByIdAsync(account.UsuarioId.ToString());
        if (user is null || !user.Ativo)
        {
            return RedirectError(MessageCodes.UserDeactivated, LoginFlow);
        }

        SyncExternalAccount(account, discordUser);
        user.UltimoLoginEm = DateTimeOffset.UtcNow;
        user.DataAtualizacao = user.UltimoLoginEm.Value;
        var token = await CreateTokenAsync(user, null, ipAddress, userAgent, cancellationToken);
        await auditoriaService.RegistrarAsync("DiscordLoginSucceeded", user.Id, user.Id, MessageCodes.LoginSuccess, ipAddress, userAgent, cancellationToken);

        return new DiscordCallbackResultDto(discordOptions.Value.FrontendSuccessUrl, new AuthResponseDto(token.AccessToken, token.ExpiresIn, await BuildUserDtoAsync(user, cancellationToken)) { RefreshToken = token.RefreshToken });
    }

    private async Task<DiscordCallbackResultDto> CreateDiscordUserAndLoginAsync(DiscordUserResponse discordUser, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(discordUser.Email) || discordUser.Verified != true)
        {
            return RedirectError(MessageCodes.DiscordVerifiedEmailRequired, LoginFlow);
        }

        var email = discordUser.Email.Trim();
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            return RedirectError(MessageCodes.DiscordEmailAlreadyRegistered, LoginFlow);
        }

        var now = DateTimeOffset.UtcNow;
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Nome = (discordUser.GlobalName ?? discordUser.Username ?? email).Trim(),
            UserName = email,
            Email = email,
            Ativo = true,
            EmailConfirmed = true,
            UltimoLoginEm = now,
            DataCadastro = now,
            DataAtualizacao = now,
        };

        EnsureIdentitySuccess(await userManager.CreateAsync(user));
        await EnsureRoleAsync(AuthRoles.Jogador);
        EnsureIdentitySuccess(await userManager.AddToRoleAsync(user, AuthRoles.Jogador));

        var account = new ExternalAccount
        {
            UsuarioId = user.Id,
            Provider = DiscordProvider,
            ProviderUserId = discordUser.Id,
            LinkedAt = now,
        };
        SyncExternalAccount(account, discordUser);
        await dbContext.ExternalAccounts.AddAsync(account, cancellationToken);

        var token = await CreateTokenAsync(user, null, ipAddress, userAgent, cancellationToken);
        await auditoriaService.RegistrarAsync("DiscordUserCreated", user.Id, user.Id, MessageCodes.UserCreated, ipAddress, userAgent, cancellationToken);
        await auditoriaService.RegistrarAsync("DiscordLinked", user.Id, user.Id, MessageCodes.DiscordLinked, ipAddress, userAgent, cancellationToken);

        return new DiscordCallbackResultDto(discordOptions.Value.FrontendSuccessUrl, new AuthResponseDto(token.AccessToken, token.ExpiresIn, await BuildUserDtoAsync(user, cancellationToken)) { RefreshToken = token.RefreshToken });
    }

    private async Task<DiscordCallbackResultDto> HandleDiscordLinkCallbackAsync(ExternalAuthState state, DiscordUserResponse discordUser, CancellationToken cancellationToken)
    {
        if (state.UsuarioId is null)
        {
            return RedirectError(MessageCodes.DiscordOAuthStateInvalid, LinkFlow);
        }

        var user = await userManager.FindByIdAsync(state.UsuarioId.Value.ToString());
        if (user is null || !user.Ativo)
        {
            return RedirectError(MessageCodes.UserDeactivated, LinkFlow);
        }

        var linkedDiscord = await dbContext.ExternalAccounts
            .FirstOrDefaultAsync(link => link.Provider == DiscordProvider && link.ProviderUserId == discordUser.Id && link.UnlinkedAt == null, cancellationToken);
        if (linkedDiscord is not null && linkedDiscord.UsuarioId != user.Id)
        {
            return RedirectError(MessageCodes.DiscordAccountAlreadyLinked, LinkFlow);
        }

        var userDiscord = await dbContext.ExternalAccounts
            .FirstOrDefaultAsync(link => link.UsuarioId == user.Id && link.Provider == DiscordProvider && link.UnlinkedAt == null, cancellationToken);
        if (userDiscord is not null && userDiscord.ProviderUserId != discordUser.Id)
        {
            return RedirectError(MessageCodes.UserAlreadyHasDiscordLinked, LinkFlow);
        }

        var account = linkedDiscord ?? userDiscord ?? new ExternalAccount
        {
            UsuarioId = user.Id,
            Provider = DiscordProvider,
            ProviderUserId = discordUser.Id,
            LinkedAt = DateTimeOffset.UtcNow,
        };

        SyncExternalAccount(account, discordUser);
        if (linkedDiscord is null && userDiscord is null)
        {
            await dbContext.ExternalAccounts.AddAsync(account, cancellationToken);
        }

        await auditoriaService.RegistrarAsync("DiscordLinked", user.Id, user.Id, MessageCodes.DiscordLinked, null, null, cancellationToken);
        return new DiscordCallbackResultDto(discordOptions.Value.FrontendSuccessUrl);
    }

    private async Task<string> CreateExternalStateAsync(string flow, Guid? userId, CancellationToken cancellationToken)
    {
        var rawState = GenerateUrlToken(32);
        var now = DateTimeOffset.UtcNow;
        await dbContext.ExternalAuthStates.AddAsync(new ExternalAuthState
        {
            StateHash = Hash(rawState),
            Flow = flow,
            UsuarioId = userId,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(discordOptions.Value.StateExpirationMinutes),
        }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return rawState;
    }

    private async Task<ExternalAuthState?> ConsumeExternalStateAsync(string state, CancellationToken cancellationToken)
    {
        var stateHash = Hash(state);
        var storedState = await dbContext.ExternalAuthStates.FirstOrDefaultAsync(item => item.StateHash == stateHash, cancellationToken);
        if (storedState is null || !storedState.IsActive(DateTimeOffset.UtcNow))
        {
            return null;
        }

        storedState.ConsumedAt = DateTimeOffset.UtcNow;
        return storedState;
    }

    private string BuildDiscordAuthorizationUrl(string state)
    {
        var options = discordOptions.Value;
        var query = new Dictionary<string, string>
        {
            ["client_id"] = options.ClientId,
            ["redirect_uri"] = options.RedirectUri,
            ["response_type"] = "code",
            ["scope"] = options.Scopes,
            ["state"] = state,
        };

        return options.AuthorizationUrl + "?" + string.Join("&", query.Select(item => $"{Uri.EscapeDataString(item.Key)}={Uri.EscapeDataString(item.Value)}"));
    }

    private async Task<DiscordTokenResponse?> ExchangeDiscordCodeAsync(string code, CancellationToken cancellationToken)
    {
        var options = discordOptions.Value;
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = options.ClientId,
            ["client_secret"] = options.ClientSecret,
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = options.RedirectUri,
        });

        using var response = await httpClient.PostAsync(options.TokenUrl, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<DiscordTokenResponse>(stream, cancellationToken: cancellationToken);
    }

    private async Task<DiscordUserResponse?> GetDiscordUserAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, discordOptions.Value.UserInfoUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<DiscordUserResponse>(stream, cancellationToken: cancellationToken);
    }

    private async Task RevokeDiscordTokenAsync(string accessToken, CancellationToken cancellationToken)
    {
        var options = discordOptions.Value;
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = options.ClientId,
            ["client_secret"] = options.ClientSecret,
            ["token"] = accessToken,
            ["token_type_hint"] = "access_token",
        });

        try
        {
            _ = await httpClient.PostAsync(options.RevocationUrl, content, cancellationToken);
        }
        catch (HttpRequestException)
        {
            // Revocation failure must not block local login/link completion.
        }
    }

    private void EnsureDiscordConfigured()
    {
        var options = discordOptions.Value;
        if (string.IsNullOrWhiteSpace(options.ClientId) || string.IsNullOrWhiteSpace(options.ClientSecret) || string.IsNullOrWhiteSpace(options.RedirectUri))
        {
            throw new DomainException(MessageCodes.DiscordOAuthConfigurationMissing);
        }
    }

    private DiscordCallbackResultDto RedirectError(string messageCode, string flow)
    {
        var errorUrl = flow == LoginFlow ? discordOptions.Value.FrontendLoginErrorUrl : discordOptions.Value.FrontendErrorUrl;
        var separator = errorUrl.Contains('?') ? "&" : "?";
        return new DiscordCallbackResultDto($"{errorUrl}{separator}code={Uri.EscapeDataString(messageCode)}");
    }

    private static void SyncExternalAccount(ExternalAccount account, DiscordUserResponse user)
    {
        account.Username = user.Username;
        account.DisplayName = user.GlobalName;
        account.Email = user.Email;
        account.AvatarUrl = string.IsNullOrWhiteSpace(user.Avatar)
            ? null
            : $"https://cdn.discordapp.com/avatars/{user.Id}/{user.Avatar}.png";
        account.LastSyncAt = DateTimeOffset.UtcNow;
    }

    private static string GenerateUrlToken(int bytes)
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(bytes))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
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
        var refreshTokenEntity = new RefreshToken
        {
            UsuarioId = user.Id,
            TokenHash = Hash(refreshToken),
            FamiliaId = familyId ?? Guid.NewGuid(),
            ExpiraEm = DateTimeOffset.UtcNow.AddDays(options.RefreshTokenDays),
            IpCriacao = ipAddress,
            UserAgentCriacao = userAgent,
        };
        await dbContext.RefreshTokens.AddAsync(refreshTokenEntity, cancellationToken);

        return new TokenResult(new JwtSecurityTokenHandler().WriteToken(jwt), refreshToken, refreshTokenEntity.Id, options.AccessTokenMinutes * 60);
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
        return await userManager.FindByIdAsync(userId.ToString()) ?? throw new UnauthorizedAccessException(MessageCodes.UserNotFound);
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
            throw new DomainException(MessageCodes.RequestProcessingFailed);
        }
    }

    private sealed record DiscordTokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("token_type")] string TokenType,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("scope")] string? Scope);

    private sealed record DiscordUserResponse(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("username")] string? Username,
        [property: JsonPropertyName("global_name")] string? GlobalName,
        [property: JsonPropertyName("email")] string? Email,
        [property: JsonPropertyName("avatar")] string? Avatar,
        [property: JsonPropertyName("verified")] bool? Verified);
}
