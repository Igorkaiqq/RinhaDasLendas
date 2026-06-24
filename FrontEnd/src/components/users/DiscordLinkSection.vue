<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'

import { loadDiscordStatus } from '@/services/auth'
import type { DiscordLinkStatus } from '@/types/auth'

const status = ref<DiscordLinkStatus>({ vinculado: false })
const { t } = useI18n()

onMounted(async () => {
  status.value = await loadDiscordStatus()
})
</script>

<template>
  <section class="panel-card">
    <h2>{{ t('playerForm.discord') }}</h2>
    <p v-if="status.vinculado">{{ t('usersAdmin.discord.linkedAs', { username: status.username }) }}</p>
    <p v-else>{{ t('usersAdmin.discord.notLinkedDescription') }}</p>
    <button class="button" type="button" disabled>{{ t('usersAdmin.discord.linkSoon') }}</button>
  </section>
</template>
