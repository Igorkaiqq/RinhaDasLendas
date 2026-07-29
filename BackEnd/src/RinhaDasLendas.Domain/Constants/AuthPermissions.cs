namespace RinhaDasLendas.Domain.Constants;

public static class AuthPermissions
{
    public const string CanManageUsers = nameof(CanManageUsers);
    public const string CanViewUsers = nameof(CanViewUsers);
    public const string CanManageRoles = nameof(CanManageRoles);
    public const string CanResetUserPassword = nameof(CanResetUserPassword);
    public const string CanActivateDeactivateUsers = nameof(CanActivateDeactivateUsers);
    public const string CanManageDrafts = nameof(CanManageDrafts);
    public const string CanManageDraftCycle = nameof(CanManageDraftCycle);
    public const string CanCreateDraftPresenceOrManageCycle = nameof(CanCreateDraftPresenceOrManageCycle);
    public const string CanArchiveDrafts = nameof(CanArchiveDrafts);
    public const string CanManageMatches = nameof(CanManageMatches);
    public const string CanConfirmPresence = nameof(CanConfirmPresence);
    public const string CanEditOwnProfile = nameof(CanEditOwnProfile);
    public const string CanViewAdminLogs = nameof(CanViewAdminLogs);
    public const string CanUseDiscordBotApi = nameof(CanUseDiscordBotApi);
    public const string CanManageDraftsOrUseDiscordBotApi = nameof(CanManageDraftsOrUseDiscordBotApi);
    public const string CanManageUsersOrUseDiscordBotApi = nameof(CanManageUsersOrUseDiscordBotApi);
}
