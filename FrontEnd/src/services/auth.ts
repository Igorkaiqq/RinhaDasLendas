import axios, { AxiosError } from 'axios'

import type {
  AuthResponse,
  ChangePasswordPayload,
  ForgotPasswordPayload,
  LoginPayload,
  RegisterPayload,
  ResetPasswordPayload,
  UserPermissions,
  AuthenticatedUser,
  DiscordLinkStatus,
} from '@/types/auth'

import { api } from './api'
import { clearSession, getAccessToken, setPermissions, setSession } from './authState'

export interface UpdateOwnProfilePayload {
  nome: string
}

const baseURL = import.meta.env.VITE_API_URL ?? 'http://localhost:5000'
const rawApi = axios.create({ baseURL, withCredentials: true })

export class AuthServiceError extends Error {
  constructor(public readonly errors: string[]) {
    super(errors[0] ?? 'Não foi possível autenticar')
  }
}

export async function register(payload: RegisterPayload): Promise<AuthenticatedUser> {
  try {
    const response = await api.post<AuthenticatedUser>('/api/v1/auth/register', payload)
    return response.data
  } catch (error) {
    throw toAuthError(error)
  }
}

export async function login(payload: LoginPayload): Promise<AuthResponse> {
  try {
    const response = await api.post<AuthResponse>('/api/v1/auth/login', payload)
    setSession(response.data.accessToken, response.data.usuario)
    await loadPermissions()
    return response.data
  } catch (error) {
    throw toAuthError(error)
  }
}

export async function refreshSession(): Promise<AuthResponse> {
  const response = await rawApi.post<AuthResponse>('/api/v1/auth/refresh')
  setSession(response.data.accessToken, response.data.usuario)
  return response.data
}

export async function logout(): Promise<void> {
  try {
    await api.post('/api/v1/auth/logout')
  } finally {
    clearSession()
  }
}

export async function forgotPassword(payload: ForgotPasswordPayload): Promise<void> {
  await api.post('/api/v1/auth/forgot-password', payload)
}

export async function resetPassword(payload: ResetPasswordPayload): Promise<void> {
  await api.post('/api/v1/auth/reset-password', payload)
}

export async function changePassword(payload: ChangePasswordPayload): Promise<void> {
  await api.post('/api/v1/auth/change-password', payload)
}

export async function loadCurrentUser(): Promise<AuthenticatedUser> {
  const response = await api.get<AuthenticatedUser>('/api/v1/auth/me')
  return response.data
}

export async function updateOwnProfile(payload: UpdateOwnProfilePayload): Promise<AuthenticatedUser> {
  const response = await api.put<AuthenticatedUser>('/api/v1/auth/me/profile', payload)
  setSession(getAccessToken() ?? '', response.data)
  return response.data
}

export async function loadPermissions(): Promise<UserPermissions> {
  const response = await api.get<UserPermissions>('/api/v1/auth/me/permissions')
  setPermissions(response.data)
  return response.data
}

export async function loadDiscordStatus(): Promise<DiscordLinkStatus> {
  const response = await api.get<DiscordLinkStatus>('/api/v1/auth/me/discord')
  return response.data
}

function toAuthError(error: unknown): AuthServiceError {
  if (error instanceof AxiosError) {
    const data = error.response?.data as { message?: string; errors?: string[] } | undefined
    return new AuthServiceError(data?.errors?.length ? data.errors : [data?.message ?? 'Não foi possível autenticar'])
  }

  return new AuthServiceError(['Não foi possível autenticar'])
}
