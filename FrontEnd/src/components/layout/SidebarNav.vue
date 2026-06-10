<script setup lang="ts">
import { RouterLink, useRoute } from 'vue-router'

import type { SidebarNavigationItem } from '@/types/layout'

defineProps<{
  items: SidebarNavigationItem[]
}>()

const route = useRoute()
</script>

<template>
  <aside class="sidebar" aria-label="Navegacao principal">
    <RouterLink class="sidebar__brand" to="/" aria-label="RinhaDasLendas">
      <span class="sidebar__mark" aria-hidden="true">RL</span>
      <span>
        <strong>RinhaDasLendas</strong>
        <small>Liga de Elite</small>
      </span>
    </RouterLink>

    <nav class="sidebar__nav">
      <RouterLink
        v-for="item in items"
        :key="item.id"
        class="sidebar__item"
        :class="{ 'sidebar__item--active': route.name === item.routeName }"
        :to="item.path"
        :title="item.label"
      >
        <span class="sidebar__icon" aria-hidden="true">{{ item.icon }}</span>
        <span>{{ item.label }}</span>
      </RouterLink>
    </nav>

    <div class="sidebar__footer">
      <button type="button" class="sidebar__tournament">Entrar no Torneio</button>
      <RouterLink class="sidebar__item" to="/configuracoes">
        <span class="sidebar__icon" aria-hidden="true">?</span>
        <span>Suporte</span>
      </RouterLink>
      <button type="button" class="sidebar__item">
        <span class="sidebar__icon" aria-hidden="true">-&gt;</span>
        <span>Sair</span>
      </button>
    </div>
  </aside>
</template>
