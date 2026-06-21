using RinhaDasLendas.Domain.Constants;

namespace RinhaDasLendas.Domain.Services;

public sealed class RoleHierarchyService
{
    public string GetEffectiveRole(IEnumerable<string> roles)
    {
        return roles
            .Where(AuthRoles.Levels.ContainsKey)
            .OrderByDescending(role => AuthRoles.Levels[role])
            .FirstOrDefault() ?? AuthRoles.Jogador;
    }

    public bool CanAdminister(IEnumerable<string> actorRoles, IEnumerable<string> targetRoles)
    {
        var actorRole = GetEffectiveRole(actorRoles);

        if (actorRole is not AuthRoles.SuperAdmin and not AuthRoles.Admin)
        {
            return false;
        }

        if (actorRole == AuthRoles.SuperAdmin)
        {
            return true;
        }

        var targetLevel = GetHighestLevel(targetRoles);
        return targetLevel < AuthRoles.Levels[AuthRoles.Admin];
    }

    public bool CanAssignRoles(IEnumerable<string> actorRoles, IEnumerable<string> targetCurrentRoles, IEnumerable<string> requestedRoles)
    {
        var actorRole = GetEffectiveRole(actorRoles);

        if (actorRole == AuthRoles.SuperAdmin)
        {
            return true;
        }

        if (actorRole != AuthRoles.Admin || !CanAdminister(actorRoles, targetCurrentRoles))
        {
            return false;
        }

        return requestedRoles.All(role => AuthRoles.Levels.TryGetValue(role, out var level) && level < AuthRoles.Levels[AuthRoles.Admin]);
    }

    public IReadOnlyCollection<string> GetAssignableRoles(IEnumerable<string> actorRoles)
    {
        var actorRole = GetEffectiveRole(actorRoles);

        return actorRole switch
        {
            AuthRoles.SuperAdmin => AuthRoles.Levels.Keys.ToArray(),
            AuthRoles.Admin => AuthRoles.Levels.Where(role => role.Value < AuthRoles.Levels[AuthRoles.Admin]).Select(role => role.Key).ToArray(),
            _ => Array.Empty<string>(),
        };
    }

    private static int GetHighestLevel(IEnumerable<string> roles)
    {
        return roles
            .Where(AuthRoles.Levels.ContainsKey)
            .Select(role => AuthRoles.Levels[role])
            .DefaultIfEmpty(AuthRoles.Levels[AuthRoles.Jogador])
            .Max();
    }
}
