<script setup lang="ts">
import { ref } from 'vue'

import SidebarNav from '@/components/layout/SidebarNav.vue'
import Topbar from '@/components/layout/Topbar.vue'
import type { SidebarNavigationItem, TopbarUserSummary } from '@/types/layout'

const navigationItems: SidebarNavigationItem[] = [
  { id: 'dashboard', label: 'Painel', icon: '#', routeName: 'home', path: '/', status: 'available' },
  { id: 'players', label: 'Jogadores', icon: 'J', routeName: 'players', path: '/jogadores', status: 'available' },
  { id: 'teams', label: 'Times', icon: 'T', routeName: 'teams', path: '/times', status: 'placeholder' },
  { id: 'draft', label: 'Draft', icon: 'D', routeName: 'draft', path: '/draft', status: 'placeholder' },
  { id: 'matches', label: 'Partidas', icon: 'P', routeName: 'matches', path: '/partidas', status: 'placeholder' },
  { id: 'stats', label: 'Estatisticas', icon: 'E', routeName: 'stats', path: '/estatisticas', status: 'placeholder' },
  { id: 'settings', label: 'Configuracoes', icon: '*', routeName: 'settings', path: '/configuracoes', status: 'placeholder' },
]

const user: TopbarUserSummary = {
  displayName: 'Perfil',
  subtitle: 'Organizador',
  initials: 'RL',
  menuItems: [
    { id: 'profile', label: 'Meu perfil', action: 'profile' },
    { id: 'settings', label: 'Preferencias', action: 'settings' },
    { id: 'logout', label: 'Sair', action: 'logout' },
  ],
}

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
