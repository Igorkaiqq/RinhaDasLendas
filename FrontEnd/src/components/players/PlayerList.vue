<script setup lang="ts">
import PlayerCard from '@/components/players/PlayerCard.vue'
import PlayerListStates from '@/components/players/PlayerListStates.vue'
import type { Player } from '@/services/players'

defineProps<{
  players: Player[]
  loading: boolean
  errors: string[]
}>()

defineEmits<{
  create: []
  edit: [player: Player]
  delete: [player: Player]
}>()
</script>

<template>
  <PlayerListStates v-if="loading" state="loading" />
  <PlayerListStates v-else-if="errors.length" state="error" :errors="errors" />
  <PlayerListStates v-else-if="players.length === 0" state="empty">
    <button type="button" @click="$emit('create')">Cadastrar Jogador</button>
  </PlayerListStates>

  <section v-else class="player-grid" aria-label="Lista de jogadores">
    <PlayerCard
      v-for="player in players"
      :key="player.id"
      :player="player"
      @edit="$emit('edit', $event)"
      @delete="$emit('delete', $event)"
    />
  </section>
</template>
