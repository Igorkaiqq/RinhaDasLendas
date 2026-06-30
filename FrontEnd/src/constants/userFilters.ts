import { AuthRoles, type AuthRole } from '@/constants/authRoles'

export const UserStatusFilterValues = {
  Active: 'Ativo',
  Deactivated: 'Desativado',
} as const

export type UserStatusFilterValue = (typeof UserStatusFilterValues)[keyof typeof UserStatusFilterValues]

export const USER_ROLE_FILTER_OPTIONS: Array<AuthRole | ''> = [
  '',
  AuthRoles.SuperAdmin,
  AuthRoles.Admin,
  AuthRoles.Moderador,
  AuthRoles.Capitao,
  AuthRoles.Jogador,
]

export const USER_STATUS_FILTER_OPTIONS: Array<UserStatusFilterValue | ''> = [
  '',
  UserStatusFilterValues.Active,
  UserStatusFilterValues.Deactivated,
]
