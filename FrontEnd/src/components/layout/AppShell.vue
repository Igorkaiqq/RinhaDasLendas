<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'

import SidebarNav from '@/components/layout/SidebarNav.vue'
import Topbar from '@/components/layout/Topbar.vue'
import type { SidebarNavigationItem, TopbarUserSummary } from '@/types/layout'

const { t } = useI18n()

const navigationItems = computed<SidebarNavigationItem[]>(() => [
  { id: 'dashboard', label: t('navigation.dashboard'), icon: '#', routeName: 'home', path: '/', status: 'available' },
  { id: 'players', label: t('navigation.players'), icon: 'J', routeName: 'players', path: '/jogadores', status: 'available' },
  { id: 'teams', label: t('navigation.teams'), icon: 'T', routeName: 'teams', path: '/times', status: 'placeholder' },
  { id: 'draft', label: t('navigation.draft'), icon: 'D', routeName: 'draft', path: '/draft', status: 'placeholder' },
  { id: 'matches', label: t('navigation.matches'), icon: 'P', routeName: 'matches', path: '/partidas', status: 'placeholder' },
  { id: 'stats', label: t('navigation.stats'), icon: 'E', routeName: 'stats', path: '/estatisticas', status: 'placeholder' },
  {
    id: 'settings',
    label: t('navigation.settings'),
    icon: '*',
    routeName: 'settings',
    path: '/configuracoes',
    status: 'placeholder',
  },
])

const user = computed<TopbarUserSummary>(() => ({
  displayName: t('profile.displayName'),
  subtitle: t('profile.subtitle'),
  initials: 'RL',
  menuItems: [
    { id: 'profile', label: t('profile.myProfile'), action: 'profile' },
    { id: 'settings', label: t('profile.preferences'), action: 'settings' },
    { id: 'logout', label: t('navigation.logout'), action: 'logout' },
  ],
}))

const sidebarCollapsed = ref(false)
</script>

<template>
  <div class="app-shell" :class="{ 'app-shell--collapsed': sidebarCollapsed }">
    <SidebarNav :items="navigationItems" :collapsed="sidebarCollapsed" @toggle="sidebarCollapsed = !sidebarCollapsed" />
    <section class="app-shell__workspace">
      <Topbar :user="user" />
      <main class="app-shell__content">
        <slot />
      </main>
    </section>
  </div>
</template>
