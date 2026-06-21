<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'

import SidebarNav from '@/components/layout/SidebarNav.vue'
import Topbar from '@/components/layout/Topbar.vue'
import { AppRouteNames, AppRoutes } from '@/constants/appRoutes'
import { Permissions } from '@/constants/permissions'
import { logout } from '@/services/auth'
import { useAuthState } from '@/services/authState'
import type { SidebarNavigationItem, TopbarUserSummary } from '@/types/layout'

const { t } = useI18n()
const auth = useAuthState()
const router = useRouter()

const navigationItems = computed<SidebarNavigationItem[]>(() => [
  {
    id: 'dashboard',
    label: t('navigation.dashboard'),
    icon: '#',
    routeName: AppRouteNames.Home,
    path: AppRoutes.Home,
    status: 'available',
  },
  {
    id: 'players',
    label: t('navigation.players'),
    icon: 'J',
    routeName: AppRouteNames.Players,
    path: AppRoutes.Players,
    status: 'available',
  },
  {
    id: 'teams',
    label: t('navigation.teams'),
    icon: 'T',
    routeName: AppRouteNames.Teams,
    path: AppRoutes.Teams,
    status: 'available',
  },
  {
    id: 'draft',
    label: t('navigation.draft'),
    icon: 'D',
    routeName: AppRouteNames.Draft,
    path: AppRoutes.Draft,
    status: 'available',
  },
  {
    id: 'matches',
    label: t('navigation.matches'),
    icon: 'P',
    routeName: AppRouteNames.Matches,
    path: AppRoutes.Matches,
    status: 'placeholder',
  },
  {
    id: 'stats',
    label: t('navigation.stats'),
    icon: 'E',
    routeName: AppRouteNames.Stats,
    path: AppRoutes.Stats,
    status: 'placeholder',
  },
  {
    id: 'settings',
    label: t('navigation.settings'),
    icon: '*',
    routeName: AppRouteNames.Settings,
    path: AppRoutes.Settings,
    status: 'placeholder',
  },
  ...(auth.hasPermission(Permissions.CanViewUsers)
    ? [
        {
          id: 'users',
          label: 'Usuários',
          icon: 'U',
          routeName: AppRouteNames.UsersAdmin,
          path: AppRoutes.UsersAdmin,
          status: 'available' as const,
        },
      ]
    : []),
])

const user = computed<TopbarUserSummary>(() => ({
  displayName: auth.user.value?.nome ?? t('profile.displayName'),
  subtitle: auth.user.value?.roles.join(', ') ?? t('profile.subtitle'),
  initials: (auth.user.value?.nome ?? 'RL')
    .split(' ')
    .map((part) => part[0])
    .join('')
    .slice(0, 2)
    .toUpperCase(),
  menuItems: [
    { id: 'profile', label: t('profile.myProfile'), action: 'profile' },
    { id: 'settings', label: t('profile.preferences'), action: 'settings' },
    { id: 'logout', label: t('navigation.logout'), action: 'logout' },
  ],
}))

const sidebarCollapsed = ref(false)

async function handleMenuAction(action: string) {
  if (action === 'logout') {
    await logout()
    await router.push(AppRoutes.Login)
    return
  }

  if (action === 'profile') {
    await router.push(AppRoutes.Profile)
    return
  }

  if (action === 'settings') {
    await router.push(AppRoutes.Settings)
  }
}
</script>

<template>
  <div class="app-shell" :class="{ 'app-shell--collapsed': sidebarCollapsed }">
    <SidebarNav :items="navigationItems" :collapsed="sidebarCollapsed" @toggle="sidebarCollapsed = !sidebarCollapsed" />
    <section class="app-shell__workspace">
      <Topbar :user="user" @menu-action="handleMenuAction" />
      <main class="app-shell__content">
        <slot />
      </main>
    </section>
  </div>
</template>
