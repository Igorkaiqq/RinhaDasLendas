using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using RinhaDasLendas.Domain.Constants;

namespace RinhaDasLendas.Api.Services;

public sealed class BotInternalAuthHandler(
    IOptionsMonitor<BotInternalAuthOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<BotInternalAuthOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var validTokens = Options.ValidTokens.Count > 0
            ? Options.ValidTokens
            : string.IsNullOrWhiteSpace(Options.Token) ? Array.Empty<string>() : [Options.Token];
        if (validTokens.Count == 0)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!Request.Headers.TryGetValue(BotInternalAuthOptions.HeaderName, out var providedToken) || !validTokens.Contains(providedToken.ToString(), StringComparer.Ordinal))
        {
            return Task.FromResult(AuthenticateResult.Fail(MessageCodes.BotInternalTokenInvalid));
        }

        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "discord-bot"),
            new Claim(ClaimTypes.Name, "Discord Bot"),
            new Claim("scope", AuthPermissions.CanUseDiscordBotApi),
        ], Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
