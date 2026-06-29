<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'

import { getDiscordConfiguration, saveDiscordConfiguration } from '@/services/discordConfiguration'

const { t } = useI18n()
const loading = ref(true)
const saving = ref(false)
const message = ref('')
const form = reactive({
  id: null as string | null,
  guildId: '',
  presenceChannelId: '',
  newsChannelId: '',
  adminChannelId: '',
  draftChannelId: '',
  matchResultChannelId: '',
  botEnabled: true,
})

onMounted(async () => {
  const configuration = await getDiscordConfiguration()
  if (configuration) {
    Object.assign(form, configuration)
  }
  loading.value = false
})

async function submit() {
  saving.value = true
  message.value = ''
  try {
    const saved = await saveDiscordConfiguration(form)
    Object.assign(form, saved)
    message.value = t('settings.discordAdmin.messages.saved')
  } catch {
    message.value = t('settings.discordAdmin.messages.error')
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <section class="panel-card integration-card" aria-labelledby="discord-admin-title">
    <div class="integration-card__header">
      <span class="integration-card__mark">{{ t('settings.discordAdmin.mark') }}</span>
      <div>
        <h2 id="discord-admin-title">{{ t('settings.discordAdmin.title') }}</h2>
        <p>{{ t('settings.discordAdmin.description') }}</p>
      </div>
    </div>
    <p v-if="loading">{{ t('settings.discordAdmin.loading') }}</p>
    <form v-else class="player-form" @submit.prevent="submit">
      <label class="player-form__field">
        {{ t('settings.discordAdmin.fields.guildId') }}
        <input v-model="form.guildId" required />
      </label>
      <label class="player-form__field">
        {{ t('settings.discordAdmin.fields.presenceChannelId') }}
        <input v-model="form.presenceChannelId" required />
      </label>
      <label class="player-form__field">
        {{ t('settings.discordAdmin.fields.newsChannelId') }}
        <input v-model="form.newsChannelId" required />
      </label>
      <label class="player-form__field">
        {{ t('settings.discordAdmin.fields.adminChannelId') }}
        <input v-model="form.adminChannelId" required />
      </label>
      <label class="player-form__field">
        {{ t('settings.discordAdmin.fields.draftChannelId') }}
        <input v-model="form.draftChannelId" required />
      </label>
      <label class="player-form__field">
        {{ t('settings.discordAdmin.fields.matchResultChannelId') }}
        <input v-model="form.matchResultChannelId" required />
      </label>
      <label class="checkbox-line">
        <input v-model="form.botEnabled" type="checkbox" />
        {{ t('settings.discordAdmin.fields.botEnabled') }}
      </label>
      <p v-if="message" class="profile-inline-message">{{ message }}</p>
      <button type="submit" :disabled="saving">{{ saving ? t('common.saving') : t('common.save') }}</button>
    </form>
  </section>
</template>
