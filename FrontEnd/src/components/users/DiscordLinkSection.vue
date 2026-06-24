<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'

import { loadDiscordStatus, startDiscordLink, unlinkDiscord } from '@/services/auth'
import type { DiscordLinkStatus } from '@/types/auth'

const status = ref<DiscordLinkStatus>({ vinculado: false })
const loading = ref(true)
const actionLoading = ref(false)
const error = ref('')
const message = ref('')
const { t } = useI18n()

onMounted(loadStatus)

async function loadStatus() {
  loading.value = true
  error.value = ''
  try {
    status.value = await loadDiscordStatus()
  } catch {
    error.value = t('settings.integrations.discord.errors.load')
  } finally {
    loading.value = false
  }
}

async function linkDiscord() {
  actionLoading.value = true
  error.value = ''
  try {
    const response = await startDiscordLink()
    window.location.href = response.authorizationUrl
  } catch {
    error.value = t('settings.integrations.discord.errors.link')
    actionLoading.value = false
  }
}

async function removeDiscord() {
  actionLoading.value = true
  error.value = ''
  message.value = ''
  try {
    await unlinkDiscord()
    await loadStatus()
    message.value = t('settings.integrations.discord.messages.unlinked')
  } catch {
    error.value = t('settings.integrations.discord.errors.unlink')
  } finally {
    actionLoading.value = false
  }
}
</script>

<template>
  <section class="panel-card integration-card">
    <div class="integration-provider">
      <span class="integration-provider__mark">{{ t('settings.integrations.discord.mark') }}</span>
      <div>
        <h2>{{ t('settings.integrations.discord.title') }}</h2>
        <p>{{ t('settings.integrations.discord.description') }}</p>
      </div>
    </div>
    <p v-if="loading" class="integration-status">{{ t('settings.integrations.discord.loading') }}</p>
    <template v-else>
      <p v-if="status.vinculado" class="status-ok">{{ t('settings.integrations.discord.linkedAs', { username: status.username }) }}</p>
      <p v-else class="integration-status">{{ t('settings.integrations.discord.notLinkedDescription') }}</p>
      <p v-if="message" class="status-ok">{{ message }}</p>
      <p v-if="error" class="form-error">{{ error }}</p>
      <div class="integration-actions">
        <button v-if="!status.vinculado" class="button button--primary" type="button" :disabled="actionLoading" @click="linkDiscord">
          {{ actionLoading ? t('settings.integrations.discord.linking') : t('settings.integrations.discord.link') }}
        </button>
        <button v-else class="button button--danger" type="button" :disabled="actionLoading" @click="removeDiscord">
          {{ actionLoading ? t('settings.integrations.discord.unlinking') : t('settings.integrations.discord.unlink') }}
        </button>
      </div>
    </template>
  </section>
</template>
