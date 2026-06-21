export const AuthRoles = {
  SuperAdmin: 'SuperAdmin',
  Admin: 'Admin',
  Moderador: 'Moderador',
  Capitao: 'Capitão',
  Jogador: 'Jogador',
} as const

export type AuthRole = (typeof AuthRoles)[keyof typeof AuthRoles]
