<script setup lang="ts">
import { useI18n } from 'vue-i18n'

import PlayerCard from '@/components/players/PlayerCard.vue'
import PlayerListStates from '@/components/players/PlayerListStates.vue'
import type { Player } from '@/services/players'

defineProps<{
  players: Player[]
  loading: boolean
  errors: string[]
  canManage: boolean
}>()

defineEmits<{
  create: []
  edit: [player: Player]
  delete: [player: Player]
}>()

const { t } = useI18n()
</script>

<template>
  <PlayerListStates v-if="loading" state="loading" />
  <PlayerListStates v-else-if="errors.length" state="error" :errors="errors" />
  <PlayerListStates v-else-if="players.length === 0" state="empty">
    <button v-if="canManage" type="button" @click="$emit('create')">{{ t('players.create') }}</button>
  </PlayerListStates>

  <section v-else class="player-grid" :aria-label="t('players.listLabel')">
    <PlayerCard
      v-for="player in players"
      :key="player.id"
      :player="player"
      :can-manage="canManage"
      @edit="$emit('edit', $event)"
      @delete="$emit('delete', $event)"
    />
  </section>
</template>
