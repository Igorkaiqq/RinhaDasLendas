<script setup lang="ts">
import { RouterLink, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'

import brandLogo from '@/assets/rinhadaslendas_brand_logo.png'
import { AppRoutes } from '@/constants/appRoutes'
import type { SidebarNavigationItem } from '@/types/layout'

const props = defineProps<{
  items: SidebarNavigationItem[]
  collapsed: boolean
}>()

defineEmits<{
  toggle: []
  menuAction: [action: 'logout']
}>()

const route = useRoute()
const { t } = useI18n()
</script>

<template>
  <aside class="sidebar" :aria-label="t('navigation.main')">
    <button
      class="sidebar__toggle"
      type="button"
      :aria-label="props.collapsed ? t('navigation.expandSidebar') : t('navigation.collapseSidebar')"
      :title="props.collapsed ? t('navigation.expandMenu') : t('navigation.collapseMenu')"
      @click="$emit('toggle')"
    >
      <span aria-hidden="true">{{ props.collapsed ? '&gt;' : '&lt;' }}</span>
    </button>
    <div class="sidebar__brand-row">
      <RouterLink class="sidebar__brand" :to="AppRoutes.Home" :aria-label="t('app.name')">
        <span class="sidebar__mark" aria-hidden="true">
          <img :src="brandLogo" alt="" />
        </span>
        <span class="sidebar__brand-copy">
          <strong>{{ t('app.name') }}</strong>
          <small>{{ t('app.league') }}</small>
        </span>
      </RouterLink>
    </div>

    <nav class="sidebar__nav">
      <span class="sidebar__section-label">{{ t('navigation.arena') }}</span>
      <RouterLink
        v-for="item in props.items"
        :key="item.id"
        class="sidebar__item"
        :class="{ 'sidebar__item--active': route.name === item.routeName }"
        :to="item.path"
        :title="item.label"
      >
        <span class="sidebar__icon" aria-hidden="true">{{ item.icon }}</span>
        <span class="sidebar__label">{{ item.label }}</span>
        <span v-if="item.status === 'placeholder'" class="sidebar__status">{{ t('navigation.soon') }}</span>
      </RouterLink>
    </nav>

    <div class="sidebar__footer">
      <button type="button" class="sidebar__tournament" disabled>{{ t('navigation.joinTournamentSoon') }}</button>
      <RouterLink class="sidebar__item" :to="AppRoutes.Settings" :title="t('navigation.support')">
        <span class="sidebar__icon" aria-hidden="true">?</span>
        <span class="sidebar__label">{{ t('navigation.support') }}</span>
      </RouterLink>
      <button type="button" class="sidebar__item" :title="t('navigation.logout')" @click="$emit('menuAction', 'logout')">
        <span class="sidebar__icon" aria-hidden="true">↩</span>
        <span class="sidebar__label">{{ t('navigation.logout') }}</span>
      </button>
    </div>
  </aside>
</template>
