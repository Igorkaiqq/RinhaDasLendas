import type { AuthRole } from '@/constants/authRoles'
import type { UserStatusFilterValue } from '@/constants/userFilters'
import type { PaginatedUsers, ResetUserPasswordPayload, RoleOption, UpdateUserPayload, UpdateUserRolesPayload, UserDetails } from '@/types/users'

import { api } from './api'

export interface UserFilters {
  search?: string
  nome?: string
  email?: string
  role?: AuthRole | ''
  status?: UserStatusFilterValue | ''
  page?: number
  pageSize?: number
}

export async function listUsers(filters: UserFilters = {}): Promise<PaginatedUsers> {
  const response = await api.get<PaginatedUsers>('/api/v1/usuarios', { params: filters })
  return response.data
}

export async function getUser(id: string): Promise<UserDetails> {
  const response = await api.get<UserDetails>(`/api/v1/usuarios/${id}`)
  return response.data
}

export async function updateUser(id: string, payload: UpdateUserPayload): Promise<UserDetails> {
  const response = await api.put<UserDetails>(`/api/v1/usuarios/${id}`, payload)
  return response.data
}

export async function updateUserRoles(id: string, payload: UpdateUserRolesPayload): Promise<void> {
  await api.put(`/api/v1/usuarios/${id}/roles`, payload)
}

export async function activateUser(id: string): Promise<UserDetails> {
  const response = await api.patch<UserDetails>(`/api/v1/usuarios/${id}/ativar`)
  return response.data
}

export async function deactivateUser(id: string): Promise<UserDetails> {
  const response = await api.patch<UserDetails>(`/api/v1/usuarios/${id}/desativar`)
  return response.data
}

export async function resetUserPassword(id: string, payload: ResetUserPasswordPayload): Promise<void> {
  await api.post(`/api/v1/usuarios/${id}/reset-password`, payload)
}

export async function listAssignableRoles(): Promise<RoleOption[]> {
  const response = await api.get<{ items: RoleOption[] }>('/api/v1/usuarios/roles')
  return response.data.items
}
