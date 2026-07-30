using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Security;
using RinhaDasLendas.Domain.Constants;

namespace RinhaDasLendas.Infrastructure.Identity;

public sealed class CompetitiveAuthorizationService(ICurrentActor currentActor) : ICompetitiveAuthorizationService
{
    public Task<bool> AuthorizeAsync(
        CompetitiveAuthorizationContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(
            currentActor.UserId.HasValue
            && CompetitiveAuthorizationEvaluator.IsAuthorized(currentActor.Roles, context));
    }

    public Task<IReadOnlyCollection<string>> GetAllowedActionsAsync(
        IReadOnlyCollection<CompetitiveAuthorizationContext> actions,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyCollection<string> allowed = actions
            .Where(action => currentActor.UserId.HasValue
                && CompetitiveAuthorizationEvaluator.IsAuthorized(currentActor.Roles, action))
            .Select(action => action.Operation)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        return Task.FromResult(allowed);
    }

}

internal static class CompetitiveAuthorizationEvaluator
{
    internal static bool IsAuthorized(
        IReadOnlyCollection<string> trustedRoles,
        CompetitiveAuthorizationContext context)
    {
        if (!AuthPermissions.CompetitiveCapabilities.Contains(
                context.Capability,
                StringComparer.Ordinal))
        {
            return false;
        }

        return trustedRoles
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Any(role => IsRoleAuthorized(role, context));
    }

    private static bool IsRoleAuthorized(string role, CompetitiveAuthorizationContext context)
    {
        if (!AuthPermissions.CompetitiveBaseRoleGrants.TryGetValue(role, out var grants)
            || !grants.Contains(context.Capability))
        {
            return false;
        }

        if (IsCapabilityCheck(context))
        {
            return true;
        }

        if (role.Equals(AuthRoles.Presidente, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (role.Equals(AuthRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase))
        {
            return IsSuperAdminAuthorized(context);
        }

        if (role.Equals(AuthRoles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            return IsAdminAuthorized(context);
        }

        return role.Equals(AuthRoles.Moderador, StringComparison.OrdinalIgnoreCase)
            && IsModeratorAuthorized(context);
    }

    private static bool IsSuperAdminAuthorized(CompetitiveAuthorizationContext context)
    {
        if (context.Capability is AuthPermissions.CanManageSeasons or AuthPermissions.CanViewCompetitiveAudit)
        {
            return true;
        }

        if (context.Capability is not (AuthPermissions.CanManageMatches or AuthPermissions.CanFinalizeMatches))
        {
            return false;
        }

        return context.ResourceType == "Partida"
            && context.Operation is "CorrecaoTecnica" or "AnulacaoTecnica"
            && context.HasRequiredJustification;
    }

    private static bool IsAdminAuthorized(CompetitiveAuthorizationContext context)
    {
        if (context.Capability is AuthPermissions.CanManageMatches or AuthPermissions.CanFinalizeMatches)
        {
            return true;
        }

        if (context.Capability == AuthPermissions.CanViewCompetitiveAudit)
        {
            return context.ResourceType is "Serie" or "Partida";
        }

        if (context.Capability != AuthPermissions.CanManageCompetitions)
        {
            return false;
        }

        return !context.HasStartedSeries
            && context.SeasonState is "Planejada" or "Ativa"
            && (context.ResourceType == "Competicao" && context.Operation == "ConfigurarCompeticao"
                || context.ResourceType == "Rodada" && context.Operation == "ConfigurarRodada");
    }

    private static bool IsModeratorAuthorized(CompetitiveAuthorizationContext context)
    {
        if (context.Capability is not (AuthPermissions.CanManageMatches or AuthPermissions.CanFinalizeMatches))
        {
            return false;
        }

        var expectedOperation = context.Capability == AuthPermissions.CanManageMatches
            ? "OperacaoNormal"
            : "FinalizacaoNormal";
        return context.ResourceType == "Serie"
            && context.Operation == expectedOperation
            && context.SerieType is "DiariaTemporaria" or "Amistoso";
    }

    private static bool IsCapabilityCheck(CompetitiveAuthorizationContext context) =>
        context.Operation == "CapabilityCheck" && context.ResourceType == "Global";
}
