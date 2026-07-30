using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using RinhaDasLendas.Application.Security;

namespace RinhaDasLendas.Infrastructure.Identity;

public sealed record CompetitiveCapabilityRequirement(string Capability) : IAuthorizationRequirement;

public sealed class CompetitiveCapabilityAuthorizationHandler
    : AuthorizationHandler<CompetitiveCapabilityRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CompetitiveCapabilityRequirement requirement)
    {
        var authorizationContext = context.Resource as CompetitiveAuthorizationContext
            ?? new CompetitiveAuthorizationContext(
                requirement.Capability,
                "CapabilityCheck",
                "Global",
                null,
                null,
                false,
                false);
        authorizationContext = authorizationContext with
        {
            Capability = requirement.Capability,
        };
        var authenticatedIdentities = context.User.Identities
            .Where(identity => identity.IsAuthenticated)
            .ToArray();
        var roles = authenticatedIdentities
            .SelectMany(identity => identity.FindAll(ClaimTypes.Role))
            .Select(claim => claim.Value)
            .ToArray();
        var hasValidActor = authenticatedIdentities
            .SelectMany(identity => identity.FindAll(ClaimTypes.NameIdentifier))
            .Any(claim => Guid.TryParse(claim.Value, out var userId) && userId != Guid.Empty);

        if (hasValidActor
            && CompetitiveAuthorizationEvaluator.IsAuthorized(roles, authorizationContext))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
