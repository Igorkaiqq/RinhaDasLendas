import type { AuthRole } from '@/constants/authRoles'

import type { DiscordLinkStatus } from './auth'

export interface UserSummary {
  id: string
  nome: string
  email: string
  roles: AuthRole[]
  ativo: boolean
  jogadorId?: string | null
  discordVinculado: boolean
  dataCadastro: string
  ultimoLoginEm?: string | null
  acoesPermitidas: string[]
}

export interface UserDetails extends UserSummary {
  discord: DiscordLinkStatus
  dataAtualizacao: string
}

export interface PaginatedUsers {
  page: number
  pageSize: number
  items: UserSummary[]
  totalItems: number
  totalPages: number
}

export interface UpdateUserPayload {
  nome: string
}

export interface UpdateUserRolesPayload {
  roles: AuthRole[]
}

export interface ResetUserPasswordPayload {
  novaSenha: string
  confirmacaoSenha: string
}

export interface RoleOption {
  nome: AuthRole
  nivel: number
}
