export const Permissions = {
  CanManageUsers: 'CanManageUsers',
  CanViewUsers: 'CanViewUsers',
  CanManageRoles: 'CanManageRoles',
  CanResetUserPassword: 'CanResetUserPassword',
  CanActivateDeactivateUsers: 'CanActivateDeactivateUsers',
  CanManageDrafts: 'CanManageDrafts',
  CanManageMatches: 'CanManageMatches',
  CanConfirmPresence: 'CanConfirmPresence',
  CanEditOwnProfile: 'CanEditOwnProfile',
  CanViewAdminLogs: 'CanViewAdminLogs',
} as const

export type Permission = (typeof Permissions)[keyof typeof Permissions]
