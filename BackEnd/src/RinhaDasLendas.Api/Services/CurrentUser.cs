using System.Security.Claims;
using RinhaDasLendas.Application.Interfaces;

namespace RinhaDasLendas.Api.Services;

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private IEnumerable<ClaimsIdentity> AuthenticatedIdentities =>
        httpContextAccessor.HttpContext?.User.Identities
            .Where(identity => identity.IsAuthenticated)
        ?? [];

    public Guid? UserId
    {
        get
        {
            return AuthenticatedIdentities
                .SelectMany(identity => identity.FindAll(ClaimTypes.NameIdentifier))
                .Select(claim => Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty)
                .FirstOrDefault(id => id != Guid.Empty) is { } userId && userId != Guid.Empty
                    ? userId
                    : null;
        }
    }

    public IReadOnlyCollection<string> Roles =>
        AuthenticatedIdentities
            .SelectMany(identity => identity.FindAll(ClaimTypes.Role))
            .Select(claim => claim.Value)
            .ToArray();

    public string? IpAddress => httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent => httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();
}
