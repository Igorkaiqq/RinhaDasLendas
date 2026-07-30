namespace RinhaDasLendas.Domain.Constants;

public static class AuthPermissions
{
    public const string CanManageUsers = nameof(CanManageUsers);
    public const string CanViewUsers = nameof(CanViewUsers);
    public const string CanManageRoles = nameof(CanManageRoles);
    public const string CanResetUserPassword = nameof(CanResetUserPassword);
    public const string CanActivateDeactivateUsers = nameof(CanActivateDeactivateUsers);
    public const string CanManageDrafts = nameof(CanManageDrafts);
    public const string CanArchiveDrafts = nameof(CanArchiveDrafts);
    public const string CanManageSeasons = nameof(CanManageSeasons);
    public const string CanManageCompetitions = nameof(CanManageCompetitions);
    public const string CanManageMatches = nameof(CanManageMatches);
    public const string CanFinalizeMatches = nameof(CanFinalizeMatches);
    public const string CanViewCompetitiveAudit = nameof(CanViewCompetitiveAudit);
    public const string CanConfirmPresence = nameof(CanConfirmPresence);
    public const string CanEditOwnProfile = nameof(CanEditOwnProfile);
    public const string CanViewAdminLogs = nameof(CanViewAdminLogs);
    public const string CanUseDiscordBotApi = nameof(CanUseDiscordBotApi);
    public const string CanManageDraftsOrUseDiscordBotApi = nameof(CanManageDraftsOrUseDiscordBotApi);
    public const string CanManageUsersOrUseDiscordBotApi = nameof(CanManageUsersOrUseDiscordBotApi);

    public static readonly IReadOnlyList<string> CompetitiveCapabilities =
    [
        CanManageSeasons,
        CanManageCompetitions,
        CanManageMatches,
        CanFinalizeMatches,
        CanViewCompetitiveAudit,
    ];

    public static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> CompetitiveBaseRoleGrants =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [AuthRoles.SuperAdmin] = new HashSet<string>(StringComparer.Ordinal)
            {
                CanManageSeasons,
                CanManageMatches,
                CanFinalizeMatches,
                CanViewCompetitiveAudit,
            },
            [AuthRoles.Presidente] = new HashSet<string>(CompetitiveCapabilities, StringComparer.Ordinal),
            [AuthRoles.VicePresidente] = new HashSet<string>(StringComparer.Ordinal),
            [AuthRoles.Admin] = new HashSet<string>(StringComparer.Ordinal)
            {
                CanManageCompetitions,
                CanManageMatches,
                CanFinalizeMatches,
                CanViewCompetitiveAudit,
            },
            [AuthRoles.Moderador] = new HashSet<string>(StringComparer.Ordinal)
            {
                CanManageMatches,
                CanFinalizeMatches,
            },
            [AuthRoles.Capitao] = new HashSet<string>(StringComparer.Ordinal),
            [AuthRoles.Jogador] = new HashSet<string>(StringComparer.Ordinal),
        };
}
