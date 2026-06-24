<script setup lang="ts">
import { useI18n } from 'vue-i18n'

import PlayerStatusBadge from '@/components/PlayerStatusBadge.vue'
import RoutePreferencesPanel from '@/components/RoutePreferencesPanel.vue'
import type { Player } from '@/services/players'

const props = defineProps<{
  player: Player
  canManage: boolean
}>()

const { t } = useI18n()

defineEmits<{
  edit: [player: Player]
  delete: [player: Player]
}>()

function formatPlayerElo(player: Player) {
  if (!player.elo) {
    return t('playerCard.noElo')
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
      <p>{{ player.discord || t('playerCard.discordMissing') }}</p>
    </div>

    <div class="player-card__meta">
      <span>{{ t('playerCard.primaryRoute') }}</span>
      <strong>{{ primaryRoute }}</strong>
    </div>

    <RoutePreferencesPanel :preferences="player.preferencias" />

    <div class="player-card__links">
      <a v-if="player.opGgUrl" :href="player.opGgUrl" target="_blank" rel="noreferrer">{{ t('playerForm.opgg') }}</a>
      <span v-else>{{ t('playerCard.opggMissing') }}</span>
      <span>{{ player.riotId || t('playerCard.riotIdMissing') }}</span>
    </div>

    <div v-if="canManage" class="player-card__actions">
      <button type="button" class="button-secondary" @click="$emit('edit', player)">{{ t('playerCard.edit') }}</button>
      <button type="button" class="button-danger" @click="$emit('delete', player)">{{ t('playerCard.delete') }}</button>
    </div>
  </article>
</template>
