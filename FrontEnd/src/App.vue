<script setup lang="ts">
import { ref } from 'vue'
import { RouterLink, RouterView } from 'vue-router'

type NavigationItem = {
  label: string
  icon: string
  route?: string
}

const navigationItems: NavigationItem[] = [
  { label: 'Home', icon: 'H', route: '/' },
  { label: 'Jogadores', icon: 'J', route: '/jogadores' },
  { label: 'Partidas', icon: 'P' },
  { label: 'Draft', icon: 'D' },
  { label: 'Times', icon: 'T' },
  { label: 'Relatórios', icon: 'R' },
  { label: 'Estatísticas', icon: 'E' },
  { label: 'Configurações', icon: 'C' },
]

const sidebarExpanded = ref(true)
const notification = ref<string | null>(null)
let notificationTimer: ReturnType<typeof globalThis.setTimeout> | null = null

function toggleSidebar() {
  sidebarExpanded.value = !sidebarExpanded.value
}

function showUnderConstruction() {
  notification.value = 'Tela em desenvolvimento.'

  if (notificationTimer) {
    globalThis.clearTimeout(notificationTimer)
  }

  notificationTimer = globalThis.setTimeout(() => {
    notification.value = null
    notificationTimer = null
  }, 3200)
}
</script>

<template>
  <div class="app-shell" :class="{ 'app-shell--collapsed': !sidebarExpanded }">
    <aside class="app-sidebar" aria-label="Navegacao principal">
      <div class="app-sidebar__brand">
        <span class="app-sidebar__mark" aria-hidden="true">RL</span>
        <div v-if="sidebarExpanded" class="app-sidebar__brand-text">
          <strong>Rinha</strong>
          <span>das Lendas</span>
        </div>
      </div>

      <button
        class="app-sidebar__toggle"
        type="button"
        :aria-label="sidebarExpanded ? 'Recolher menu lateral' : 'Expandir menu lateral'"
        @click="toggleSidebar"
      >
        <span aria-hidden="true">{{ sidebarExpanded ? '<' : '>' }}</span>
      </button>

      <nav class="app-sidebar__nav">
        <template v-for="item in navigationItems" :key="item.label">
          <RouterLink v-if="item.route" class="app-sidebar__item" :to="item.route" :title="item.label">
            <span class="app-sidebar__icon" aria-hidden="true">{{ item.icon }}</span>
            <span v-if="sidebarExpanded" class="app-sidebar__label">{{ item.label }}</span>
          </RouterLink>

          <button v-else class="app-sidebar__item" type="button" :title="item.label" @click="showUnderConstruction">
            <span class="app-sidebar__icon" aria-hidden="true">{{ item.icon }}</span>
            <span v-if="sidebarExpanded" class="app-sidebar__label">{{ item.label }}</span>
          </button>
        </template>
      </nav>
    </aside>

    <div v-if="notification" class="app-toast app-toast--info" role="status" aria-live="polite">
      <span class="app-toast__indicator" aria-hidden="true" />
      <p>{{ notification }}</p>
      <button type="button" aria-label="Fechar notificacao" @click="notification = null">x</button>
    </div>

    <main class="app-content">
      <RouterView />
    </main>
  </div>
</template>
