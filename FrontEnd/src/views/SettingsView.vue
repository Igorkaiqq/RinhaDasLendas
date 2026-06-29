<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute } from 'vue-router'

import DiscordLinkSection from '@/components/users/DiscordLinkSection.vue'
import DiscordAdminConfigurationSection from '@/components/users/DiscordAdminConfigurationSection.vue'
import { Permissions } from '@/constants/permissions'
import { useAuthState } from '@/services/authState'

const { t } = useI18n()
const route = useRoute()
const auth = useAuthState()

const discordMessage = computed(() => {
  if (route.query.discord === 'success') {
    return t('settings.integrations.discord.messages.success')
  }

  if (route.query.discord === 'error') {
    return t('settings.integrations.discord.messages.error')
  }

  return ''
})
</script>

<template>
  <section class="page-stack">
    <header class="page-header-card">
      <span>{{ t('settings.eyebrow') }}</span>
      <h1>{{ t('settings.title') }}</h1>
      <p>{{ t('settings.description') }}</p>
    </header>
    <p v-if="discordMessage" class="panel-card status-ok profile-inline-message">{{ discordMessage }}</p>
    <section class="integration-grid" aria-labelledby="integrations-title">
      <header class="panel-card integration-intro">
        <span class="eyebrow">{{ t('settings.integrations.eyebrow') }}</span>
        <h2 id="integrations-title">{{ t('settings.integrations.title') }}</h2>
        <p>{{ t('settings.integrations.description') }}</p>
      </header>
      <DiscordLinkSection />
      <DiscordAdminConfigurationSection v-if="auth.hasPermission(Permissions.CanManageUsers)" />
    </section>
  </section>
</template>
