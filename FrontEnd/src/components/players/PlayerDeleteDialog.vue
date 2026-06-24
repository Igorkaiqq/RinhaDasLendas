<script setup lang="ts">
import { useI18n } from 'vue-i18n'

import type { Player } from '@/services/players'

defineProps<{
  player: Player | null
  open: boolean
}>()

defineEmits<{
  cancel: []
  confirm: []
}>()

const { t } = useI18n()
</script>

<template>
  <div v-if="open && player" class="dialog-backdrop" role="presentation">
    <section class="confirm-dialog" role="dialog" aria-modal="true" aria-labelledby="delete-player-title">
      <p class="page-kicker">{{ t('playerDelete.kicker') }}</p>
      <h2 id="delete-player-title">{{ t('playerDelete.title', { name: player.nomeExibicao }) }}</h2>
      <p>{{ t('playerDelete.description') }}</p>
      <div class="confirm-dialog__actions">
        <button type="button" class="button-secondary" @click="$emit('cancel')">{{ t('playerForm.cancel') }}</button>
        <button type="button" class="button-danger" @click="$emit('confirm')">{{ t('playerDelete.confirm') }}</button>
      </div>
    </section>
  </div>
</template>
