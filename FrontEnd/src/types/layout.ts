import type { AppRouteName, AppRoutePath } from '@/constants/appRoutes'

export type NavigationStatus = 'available' | 'placeholder' | 'disabled'

export interface SidebarNavigationItem {
  id: string
  label: string
  icon: string
  routeName: AppRouteName
  path: AppRoutePath
  status: NavigationStatus
}

export interface ProfileMenuItem {
  id: string
  label: string
  action: 'profile' | 'settings' | 'logout'
}

export interface TopbarUserSummary {
  displayName: string
  subtitle: string
  avatarUrl?: string
  initials: string
  menuItems: ProfileMenuItem[]
}
