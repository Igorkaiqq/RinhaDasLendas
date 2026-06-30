<script setup lang="ts">
defineOptions({ name: 'AppTopbar' })

import { useI18n } from 'vue-i18n'

import ProfileMenu from '@/components/layout/ProfileMenu.vue'
import type { TopbarUserSummary } from '@/types/layout'

defineProps<{
  user: TopbarUserSummary
  pageTitle: string
}>()

defineEmits<{
  menuAction: [string]
}>()

const { t } = useI18n()
</script>

<template>
  <header class="topbar">
    <div class="topbar__context">
      <span>{{ t('app.name') }}</span>
      <strong>{{ pageTitle }}</strong>
    </div>

    <div class="topbar__center">
      <label class="topbar__search" :aria-label="t('topbar.globalSearch')">
        <span aria-hidden="true">⌕</span>
        <input type="search" :placeholder="t('topbar.searchPlaceholder')" />
      </label>
    </div>

    <div class="topbar__actions">
      <a class="topbar__link" href="https://discord.gg/UAu64Zhnd8" target="_blank" rel="noreferrer">{{ t('topbar.discord') }}</a>
      <span class="topbar__divider" aria-hidden="true" />
      <button class="topbar__icon" type="button" :aria-label="t('topbar.notifications')" disabled>•</button>
      <button class="topbar__icon" type="button" :aria-label="t('topbar.settings')" disabled>⚙</button>
      <div class="topbar__profile">
        <span class="topbar__avatar" aria-hidden="true">{{ user.initials }}</span>
        <div class="topbar__user-copy">
          <strong>{{ user.displayName }}</strong>
          <span>{{ user.subtitle }}</span>
        </div>
        <ProfileMenu :items="user.menuItems" @action="$emit('menuAction', $event)" />
      </div>
    </div>
  </header>
</template>
