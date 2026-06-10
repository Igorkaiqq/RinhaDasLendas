<script setup lang="ts">
defineOptions({ name: 'AppTopbar' })

import ProfileMenu from '@/components/layout/ProfileMenu.vue'
import type { TopbarUserSummary } from '@/types/layout'

defineProps<{
  user: TopbarUserSummary
}>()
</script>

<template>
  <header class="topbar">
    <label class="topbar__search" aria-label="Busca global">
      <span aria-hidden="true">S</span>
      <input type="search" placeholder="Buscar jogadores, partidas..." />
    </label>

    <div class="topbar__actions">
      <a class="topbar__link" href="https://discord.com" target="_blank" rel="noreferrer">Discord</a>
      <span class="topbar__divider" aria-hidden="true" />
      <button class="topbar__icon" type="button" aria-label="Notificacoes">!</button>
      <button class="topbar__icon" type="button" aria-label="Configuracoes">*</button>
      <div class="topbar__profile">
        <span class="topbar__avatar" aria-hidden="true">{{ user.initials }}</span>
        <div class="topbar__user-copy">
          <strong>{{ user.displayName }}</strong>
          <span>{{ user.subtitle }}</span>
        </div>
        <ProfileMenu :items="user.menuItems" />
      </div>
    </div>
  </header>
</template>
