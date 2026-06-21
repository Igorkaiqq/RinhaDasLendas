import type { NavigationGuardNext, RouteLocationNormalized } from 'vue-router'

import { AppRouteNames } from '@/constants/appRoutes'
import { refreshSession, loadPermissions } from '@/services/auth'
import { useAuthState } from '@/services/authState'

export async function authGuard(to: RouteLocationNormalized, _from: RouteLocationNormalized, next: NavigationGuardNext) {
  const auth = useAuthState()
  const requiresAuth = Boolean(to.meta.requiresAuth)
  const permissions = (to.meta.permissions as string[] | undefined) ?? []

  if (!requiresAuth) {
    next()
    return
  }

  if (!auth.isAuthenticated.value) {
    try {
      auth.setRestoring(true)
      await refreshSession()
      await loadPermissions()
    } catch {
      next({ name: AppRouteNames.Login, query: { redirect: to.fullPath } })
      return
    } finally {
      auth.setRestoring(false)
    }
  }

  if (permissions.length > 0 && !permissions.every((permission) => auth.hasPermission(permission))) {
    next({ name: AppRouteNames.Forbidden })
    return
  }

  next()
}
