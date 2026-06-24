<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'

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
const route = useRoute()

const navigationItems = computed<SidebarNavigationItem[]>(() => [
  {
    id: 'dashboard',
    label: t('navigation.dashboard'),
    icon: 'NX',
    routeName: AppRouteNames.Home,
    path: AppRoutes.Home,
    status: 'available',
  },
  {
    id: 'players',
    label: t('navigation.players'),
    icon: 'JG',
    routeName: AppRouteNames.Players,
    path: AppRoutes.Players,
    status: 'available',
  },
  {
    id: 'teams',
    label: t('navigation.teams'),
    icon: 'TM',
    routeName: AppRouteNames.Teams,
    path: AppRoutes.Teams,
    status: 'available',
  },
  {
    id: 'draft',
    label: t('navigation.draft'),
    icon: 'DR',
    routeName: AppRouteNames.Draft,
    path: AppRoutes.Draft,
    status: 'available',
  },
  {
    id: 'matches',
    label: t('navigation.matches'),
    icon: 'MD',
    routeName: AppRouteNames.Matches,
    path: AppRoutes.Matches,
    status: 'placeholder',
  },
  {
    id: 'stats',
    label: t('navigation.stats'),
    icon: 'WR',
    routeName: AppRouteNames.Stats,
    path: AppRoutes.Stats,
    status: 'placeholder',
  },
  {
    id: 'settings',
    label: t('navigation.settings'),
    icon: 'CF',
    routeName: AppRouteNames.Settings,
    path: AppRoutes.Settings,
    status: 'placeholder',
  },
  ...(auth.hasPermission(Permissions.CanViewUsers)
    ? [
        {
          id: 'users',
          label: 'Usuários',
          icon: 'AD',
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

const pageTitle = computed(() => String(route.meta.title ?? t('navigation.dashboard')))

const sidebarCollapsed = ref(false)
const showPlayerProfileModal = ref(false)
const dismissedForPath = ref<string | null>(null)

const shouldAskForPlayerProfile = computed(() => {
  return Boolean(auth.isAuthenticated.value && auth.user.value && !auth.user.value.jogadorId && route.name !== AppRouteNames.Profile)
})

watch(
  () => route.fullPath,
  (path) => {
    showPlayerProfileModal.value = shouldAskForPlayerProfile.value && dismissedForPath.value !== path
  },
  { immediate: true },
)

watch(shouldAskForPlayerProfile, (shouldAsk) => {
  showPlayerProfileModal.value = shouldAsk && dismissedForPath.value !== route.fullPath
})

watch(
  () => auth.user.value?.jogadorId,
  (jogadorId) => {
    if (jogadorId) {
      showPlayerProfileModal.value = false
    }
  },
)

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

async function goToProfileCompletion() {
  showPlayerProfileModal.value = false
  await router.push(AppRoutes.Profile)
}

function dismissPlayerProfileModal() {
  dismissedForPath.value = route.fullPath
  showPlayerProfileModal.value = false
}
</script>

<template>
  <div class="app-shell" :class="{ 'app-shell--collapsed': sidebarCollapsed }">
    <SidebarNav :items="navigationItems" :collapsed="sidebarCollapsed" @toggle="sidebarCollapsed = !sidebarCollapsed" />
    <section class="app-shell__workspace">
      <Topbar :user="user" :page-title="pageTitle" @menu-action="handleMenuAction" />
      <main class="app-shell__content">
        <slot />
      </main>
    </section>
    <div v-if="showPlayerProfileModal" class="dialog-backdrop profile-completion-modal-backdrop" role="presentation" @click.self="dismissPlayerProfileModal">
      <section class="dialog-card profile-completion-modal" role="dialog" aria-modal="true" aria-labelledby="profile-completion-title">
        <span class="eyebrow">Perfil incompleto</span>
        <h2 id="profile-completion-title">Complete seu perfil de jogador</h2>
        <p>
          Você já consegue navegar pela plataforma, mas só entra na lista de jogadores e nos drafts depois de preencher Riot ID, elo, links e rotas.
        </p>
        <div class="profile-completion-modal__actions">
          <button class="button" type="button" @click="dismissPlayerProfileModal">Agora não</button>
          <button class="button button--primary" type="button" @click="goToProfileCompletion">Completar perfil</button>
        </div>
      </section>
    </div>
  </div>
</template>
