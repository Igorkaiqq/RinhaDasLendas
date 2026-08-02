export const Permissions = {
  CanManageUsers: 'CanManageUsers',
  CanViewUsers: 'CanViewUsers',
  CanManageRoles: 'CanManageRoles',
  CanResetUserPassword: 'CanResetUserPassword',
  CanActivateDeactivateUsers: 'CanActivateDeactivateUsers',
  CanManageDrafts: 'CanManageDrafts',
  CanArchiveDrafts: 'CanArchiveDrafts',
  CanManageMatches: 'CanManageMatches',
  CanManageSeasons: 'CanManageSeasons',
  CanManageCompetitions: 'CanManageCompetitions',
  CanConfirmPresence: 'CanConfirmPresence',
  CanEditOwnProfile: 'CanEditOwnProfile',
  CanViewAdminLogs: 'CanViewAdminLogs',
} as const

export type Permission = (typeof Permissions)[keyof typeof Permissions]
