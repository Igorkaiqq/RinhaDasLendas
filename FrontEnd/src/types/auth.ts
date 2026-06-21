import type { AuthRole } from '@/constants/authRoles'
import type { Permission } from '@/constants/permissions'

export interface DiscordLinkStatus {
  vinculado: boolean
  username?: string | null
  vinculadoEm?: string | null
}

export interface AuthenticatedUser {
  id: string
  nome: string
  email: string
  roles: AuthRole[]
  ativo: boolean
  jogadorId?: string | null
  discord?: DiscordLinkStatus | null
}

export interface AuthResponse {
  accessToken: string
  expiresIn: number
  usuario: AuthenticatedUser
}

export interface RegisterPayload {
  nome: string
  email: string
  senha: string
  confirmacaoSenha: string
}

export interface LoginPayload {
  email: string
  senha: string
}

export interface ForgotPasswordPayload {
  email: string
}

export interface ResetPasswordPayload {
  email: string
  token: string
  novaSenha: string
  confirmacaoSenha: string
}

export interface ChangePasswordPayload {
  senhaAtual: string
  novaSenha: string
  confirmacaoSenha: string
}

export interface UserPermissions {
  roles: AuthRole[]
  permissions: Permission[]
  effectiveRole: AuthRole
}
