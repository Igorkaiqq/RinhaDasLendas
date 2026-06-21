<script setup lang="ts">
import { computed, ref } from 'vue'

import { LeagueRole, type LeagueRoleValue } from '@/constants/leagueRoles'
import type { Player } from '@/services/players'
import type { Draft, DraftPlayer } from '@/types/draft'

const props = defineProps<{ draft: Draft | null; players: Player[]; picking: boolean }>()

defineEmits<{
  pick: [player: DraftPlayer]
}>()

const playerSearch = ref('')
const selectedRoute = ref('TODAS AS ROTAS')
const routeFilters = ['TODAS AS ROTAS', 'TOP', 'JUNGLE', 'MID', 'ADC', 'SUPORTE']

const routeLabelByValue: Record<LeagueRoleValue, string> = {
  [LeagueRole.Top]: 'TOP',
  [LeagueRole.Jungle]: 'JUNGLE',
  [LeagueRole.Mid]: 'MID',
  [LeagueRole.Adc]: 'ADC',
  [LeagueRole.Support]: 'SUPORTE',
}

const routeByFilter: Record<string, LeagueRoleValue | null> = {
  'TODAS AS ROTAS': null,
  TOP: LeagueRole.Top,
  JUNGLE: LeagueRole.Jungle,
  MID: LeagueRole.Mid,
  ADC: LeagueRole.Adc,
  SUPORTE: LeagueRole.Support,
}

const playersById = computed(() => new Map(props.players.map((player) => [player.id, player])))

const availablePlayers = computed(() => {
  const search = playerSearch.value.trim().toLowerCase()
  const route = routeByFilter[selectedRoute.value]
  const players = props.draft?.disponiveis ?? []

  return players.filter((player) => {
    const details = playerDetails(player)
    const routes = details?.preferencias.map((preference) => routeLabelByValue[preference.rota]) ?? []
    const matchesSearch =
      !search ||
      player.nomeExibicao.toLowerCase().includes(search) ||
      routes.some((label) => label.toLowerCase().includes(search))
    const matchesRoute = Boolean(
      !route || details?.preferencias.some((preference) => preference.rota === route && !preference.naoJogoNemLascando),
    )

    return matchesSearch && matchesRoute
  })
})

function playerDetails(player: DraftPlayer) {
  return playersById.value.get(player.id)
}

function preferredRoute(player: DraftPlayer) {
  const preferences = playerDetails(player)?.preferencias ?? []
  const preferred =
    preferences.find((preference) => preference.prioridade === 1 && !preference.naoJogoNemLascando) ??
    preferences.find((preference) => !preference.naoJogoNemLascando)

  return preferred ? routeLabelByValue[preferred.rota] : '--'
}

function playerElo(player: DraftPlayer) {
  const details = playerDetails(player)
  return details?.elo ? [details.elo, details.divisao].filter(Boolean).join(' ') : '--'
}

function teamSlots(team: 'TimeA' | 'TimeB') {
  if (!props.draft) {
    return []
  }

  const players = team === 'TimeA' ? props.draft.timeA : props.draft.timeB
  return Array.from({ length: props.draft.tamanhoTime }, (_, index) => players[index] ?? null)
}

function teamName(team: 'TimeA' | 'TimeB') {
  return team === 'TimeA' ? 'Time Azul' : 'Time Vermelho'
}

function isActivePick(team: 'TimeA' | 'TimeB', index: number) {
  const slots = teamSlots(team)
  const firstEmptyIndex = slots.findIndex((player) => !player)
  return props.draft?.status === 'Aberto' && props.draft.proximoTime === team && firstEmptyIndex === index
}
</script>

<template>
  <section v-if="draft" class="draft-board" aria-label="Tabuleiro do draft">
    <article class="draft-team draft-team--blue">
      <header class="draft-team__header">
        <h2>{{ teamName('TimeA') }}</h2>
        <span>Capitao: {{ draft.capitaoTimeA.nomeExibicao }}</span>
      </header>

      <ul class="draft-slots">
        <li
          v-for="(player, index) in teamSlots('TimeA')"
          :key="player?.jogadorId ?? `time-a-empty-${index}`"
          class="draft-slot"
          :class="{ 'draft-slot--empty': !player, 'draft-slot--active': isActivePick('TimeA', index) }"
        >
          <template v-if="player">
            <span class="draft-slot__avatar">{{ player.nomeExibicao.charAt(0) }}</span>
            <span class="draft-slot__copy">
              <strong>{{ player.nomeExibicao }}</strong>
              <small>{{ player.capitao ? 'CAPITAO' : 'JOGADOR' }}</small>
            </span>
            <span v-if="player.capitao" class="draft-slot__captain">C</span>
          </template>
          <template v-else>
            <span>{{ isActivePick('TimeA', index) ? 'Selecionar Jogador' : 'Slot vazio' }}</span>
            <small v-if="isActivePick('TimeA', index)">ESCOLHENDO</small>
          </template>
        </li>
      </ul>
    </article>

    <article class="draft-available">
      <span class="draft-available__glow" aria-hidden="true" />
      <header class="draft-available__filters">
        <label class="draft-search-field">
          <span aria-hidden="true">S</span>
          <input v-model="playerSearch" type="search" placeholder="Buscar jogadores por nome ou rota..." />
        </label>
        <div class="draft-route-filters" aria-label="Filtros de rota">
          <button
            v-for="route in routeFilters"
            :key="route"
            type="button"
            :class="{ 'is-active': selectedRoute === route }"
            @click="selectedRoute = route"
          >
            {{ route }}
          </button>
        </div>
      </header>

      <div class="draft-available__status">
        <div>
          <span class="eyebrow">Proximo pick</span>
          <h2>{{ draft.proximoTime === 'TimeA' ? 'Time Azul' : draft.proximoTime === 'TimeB' ? 'Time Vermelho' : 'Finalizado' }}</h2>
        </div>
        <p>Capitaes: {{ draft.criterioCapitaes }} · Primeiro pick: {{ draft.criterioPrimeiroPick }}</p>
      </div>

      <div class="draft-player-grid" role="list" aria-label="Jogadores disponiveis">
        <div class="draft-player-row draft-player-row--head" role="row">
          <span>Jogador</span>
          <span>Rota</span>
          <span>Elo</span>
          <span>Ação</span>
        </div>
        <button
          v-for="player in availablePlayers"
          :key="player.id"
          type="button"
          class="draft-player-row"
          :disabled="picking || draft.status !== 'Aberto'"
          @click="$emit('pick', player)"
        >
          <span class="draft-player-row__identity">
            <span class="draft-slot__avatar">{{ player.nomeExibicao.charAt(0) }}</span>
            <strong>{{ player.nomeExibicao }}</strong>
          </span>
          <span class="draft-route-badge">{{ preferredRoute(player) }}</span>
          <span>{{ playerElo(player) }}</span>
          <span class="draft-pick-action">Pick</span>
        </button>
      </div>
      <p v-if="!availablePlayers.length" class="empty-copy">Sem jogadores disponiveis.</p>
    </article>

    <article class="draft-team draft-team--red">
      <header class="draft-team__header">
        <h2>{{ teamName('TimeB') }}</h2>
        <span>Capitao: {{ draft.capitaoTimeB.nomeExibicao }}</span>
      </header>

      <ul class="draft-slots">
        <li
          v-for="(player, index) in teamSlots('TimeB')"
          :key="player?.jogadorId ?? `time-b-empty-${index}`"
          class="draft-slot"
          :class="{ 'draft-slot--empty': !player, 'draft-slot--active': isActivePick('TimeB', index) }"
        >
          <template v-if="player">
            <span class="draft-slot__avatar">{{ player.nomeExibicao.charAt(0) }}</span>
            <span class="draft-slot__copy">
              <strong>{{ player.nomeExibicao }}</strong>
              <small>{{ player.capitao ? 'CAPITAO' : 'JOGADOR' }}</small>
            </span>
            <span v-if="player.capitao" class="draft-slot__captain">C</span>
          </template>
          <template v-else>
            <span>{{ isActivePick('TimeB', index) ? 'Selecionar Jogador' : 'Slot vazio' }}</span>
            <small v-if="isActivePick('TimeB', index)">ESCOLHENDO</small>
          </template>
        </li>
      </ul>
    </article>
  </section>
  <section v-else class="draft-empty-card">
    <h2>Nenhum draft selecionado</h2>
    <p>Crie ou selecione um draft para visualizar capitães, picks e jogadores disponiveis.</p>
  </section>
</template>
