<script setup lang="ts">
import PlayerStatusBadge from '@/components/PlayerStatusBadge.vue'
import RoutePreferencesPanel from '@/components/RoutePreferencesPanel.vue'
import type { Player } from '@/services/players'

const props = defineProps<{
  player: Player
  canManage: boolean
}>()

defineEmits<{
  edit: [player: Player]
  delete: [player: Player]
}>()

function formatPlayerElo(player: Player) {
  if (!player.elo) {
    return 'Sem elo'
  }

  return player.divisao ? `${player.elo} ${player.divisao}` : player.elo
}

function initials(name: string) {
  return name
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0])
    .join('')
    .toUpperCase()
}

const primaryRoute = props.player.preferencias.find((preference) => preference.prioridade === 1)?.rota ?? 'Flex'
</script>

<template>
  <article class="player-card">
    <div class="player-card__topline">
      <div class="player-card__avatar" aria-hidden="true">{{ initials(player.nomeExibicao) }}</div>
      <div class="player-card__badges">
        <span class="elo-badge">{{ formatPlayerElo(player) }}</span>
        <PlayerStatusBadge :status="player.status" />
      </div>
    </div>

    <div class="player-card__identity">
      <h2>{{ player.nomeExibicao }}</h2>
      <p>{{ player.discord || 'Discord nao informado' }}</p>
    </div>

    <div class="player-card__meta">
      <span>Rota principal</span>
      <strong>{{ primaryRoute }}</strong>
    </div>

    <RoutePreferencesPanel :preferences="player.preferencias" />

    <div class="player-card__links">
      <a v-if="player.opGgUrl" :href="player.opGgUrl" target="_blank" rel="noreferrer">OP.GG</a>
      <span v-else>OP.GG ausente</span>
      <span>{{ player.riotId || 'Riot ID ausente' }}</span>
    </div>

    <div v-if="canManage" class="player-card__actions">
      <button type="button" @click="$emit('edit', player)">Editar</button>
      <button type="button" class="button-danger" @click="$emit('delete', player)">Excluir</button>
    </div>
  </article>
</template>
