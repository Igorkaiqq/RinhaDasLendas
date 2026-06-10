<script setup lang="ts">
import { RouterLink, useRoute } from 'vue-router'

import type { SidebarNavigationItem } from '@/types/layout'

defineProps<{
  items: SidebarNavigationItem[]
  collapsed: boolean
}>()

defineEmits<{
  toggle: []
}>()

const route = useRoute()
</script>

<template>
  <aside class="sidebar" aria-label="Navegacao principal">
    <div class="sidebar__brand-row">
      <RouterLink class="sidebar__brand" to="/" aria-label="RinhaDasLendas">
        <span class="sidebar__mark" aria-hidden="true">RL</span>
        <span class="sidebar__brand-copy">
          <strong>RinhaDasLendas</strong>
          <small>Liga de Elite</small>
        </span>
      </RouterLink>

      <button
        class="sidebar__toggle"
        type="button"
        :aria-label="collapsed ? 'Expandir menu lateral' : 'Minimizar menu lateral'"
        :title="collapsed ? 'Expandir menu' : 'Minimizar menu'"
        @click="$emit('toggle')"
      >
        <span aria-hidden="true">{{ collapsed ? '&gt;' : '&lt;' }}</span>
      </button>
    </div>

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
        <span class="sidebar__label">{{ item.label }}</span>
      </RouterLink>
    </nav>

    <div class="sidebar__footer">
      <button type="button" class="sidebar__tournament">Entrar no Torneio</button>
      <RouterLink class="sidebar__item" to="/configuracoes" title="Suporte">
        <span class="sidebar__icon" aria-hidden="true">?</span>
        <span class="sidebar__label">Suporte</span>
      </RouterLink>
      <button type="button" class="sidebar__item" title="Sair">
        <span class="sidebar__icon" aria-hidden="true">-&gt;</span>
        <span class="sidebar__label">Sair</span>
      </button>
    </div>
  </aside>
</template>
