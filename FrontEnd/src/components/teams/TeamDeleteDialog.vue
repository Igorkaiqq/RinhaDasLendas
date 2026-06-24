<script setup lang="ts">
import { useI18n } from 'vue-i18n'

import type { Team } from '@/types/team'

const { t } = useI18n()

defineProps<{
  team: Team | null
  open: boolean
}>()

defineEmits<{
  cancel: []
  confirm: []
}>()
</script>

<template>
  <div v-if="open && team" class="dialog-backdrop" role="presentation">
    <section class="confirm-dialog" role="dialog" aria-modal="true" aria-labelledby="inactivate-team-title">
      <p class="page-kicker">{{ t('common.confirmation') }}</p>
      <h2 id="inactivate-team-title">{{ t('teams.delete.title', { name: team.nome }) }}</h2>
      <p>{{ t('teams.delete.description') }}</p>
      <div class="confirm-dialog__actions">
        <button type="button" class="button-secondary" @click="$emit('cancel')">{{ t('common.cancel') }}</button>
        <button type="button" class="button-danger" @click="$emit('confirm')">{{ t('common.confirm') }}</button>
      </div>
    </section>
  </div>
</template>
