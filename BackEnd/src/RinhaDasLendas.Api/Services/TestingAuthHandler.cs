using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using RinhaDasLendas.Domain.Constants;

namespace RinhaDasLendas.Api.Services;

public sealed class TestingAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Testing";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.Parse("99999999-0000-0000-0000-000000000001").ToString()),
            new Claim(ClaimTypes.Name, "Teste"),
            new Claim(ClaimTypes.Role, AuthRoles.SuperAdmin),
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
