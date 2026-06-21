import { computed, reactive } from 'vue'

import type { AuthenticatedUser, UserPermissions } from '@/types/auth'

const state = reactive<{
  accessToken: string | null
  user: AuthenticatedUser | null
  permissions: UserPermissions | null
  restoring: boolean
}>({
  accessToken: null,
  user: null,
  permissions: null,
  restoring: false,
})

export function useAuthState() {
  return {
    accessToken: computed(() => state.accessToken),
    user: computed(() => state.user),
    permissions: computed(() => state.permissions),
    isAuthenticated: computed(() => Boolean(state.accessToken && state.user)),
    restoring: computed(() => state.restoring),
    setSession,
    setAccessToken,
    setPermissions,
    clearSession,
    setRestoring,
    hasPermission,
    hasRole,
  }
}

export function getAccessToken() {
  return state.accessToken
}

export function setSession(accessToken: string, user: AuthenticatedUser) {
  state.accessToken = accessToken
  state.user = user
}

export function setAccessToken(accessToken: string | null) {
  state.accessToken = accessToken
}

export function setPermissions(permissions: UserPermissions | null) {
  state.permissions = permissions
}

export function setRestoring(value: boolean) {
  state.restoring = value
}

export function clearSession() {
  state.accessToken = null
  state.user = null
  state.permissions = null
}

export function hasPermission(permission: string) {
  return state.permissions?.permissions.includes(permission as never) ?? false
}

export function hasRole(role: string) {
  return state.user?.roles.includes(role as never) ?? false
}
