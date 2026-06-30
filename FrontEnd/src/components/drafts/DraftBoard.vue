<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'

import { DraftTeamValues } from '@/constants/draft'
import {
  DRAFT_ROUTE_FILTER_OPTIONS,
  DRAFT_ROUTE_LABEL_BY_LEAGUE_ROLE,
  DraftRouteFilterValues,
  LEAGUE_ROLE_BY_DRAFT_FILTER,
  type DraftRouteFilterValue,
} from '@/constants/draftRouteFilters'
import { DraftStatusValues } from '@/constants/draftStatus'
import type { Player } from '@/services/players'
import type { Draft, DraftPlayer, DraftTeamValue } from '@/types/draft'

const props = defineProps<{ draft: Draft | null; players: Player[]; picking: boolean }>()
const { t } = useI18n()

defineEmits<{
  pick: [player: DraftPlayer]
}>()

const playerSearch = ref('')
const selectedRoute = ref<DraftRouteFilterValue>(DraftRouteFilterValues.All)
const routeFilters = DRAFT_ROUTE_FILTER_OPTIONS

const routeByFilter = LEAGUE_ROLE_BY_DRAFT_FILTER

const playersById = computed(() => new Map(props.players.map((player) => [player.id, player])))

const availablePlayers = computed(() => {
  const search = playerSearch.value.trim().toLowerCase()
  const route = routeByFilter[selectedRoute.value]
  const players = props.draft?.disponiveis ?? []

  return players.filter((player) => {
    const details = playerDetails(player)
    const routes = details?.preferencias.map((preference) => DRAFT_ROUTE_LABEL_BY_LEAGUE_ROLE[preference.rota]) ?? []
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

  return preferred ? DRAFT_ROUTE_LABEL_BY_LEAGUE_ROLE[preferred.rota] : '--'
}

function playerElo(player: DraftPlayer) {
  const details = playerDetails(player)
  return details?.elo ? [details.elo, details.divisao].filter(Boolean).join(' ') : '--'
}

function teamSlots(team: DraftTeamValue) {
  if (!props.draft) {
    return []
  }

  const players = team === DraftTeamValues.TimeA ? props.draft.timeA : props.draft.timeB
  return Array.from({ length: props.draft.tamanhoTime }, (_, index) => players[index] ?? null)
}

function teamName(team: DraftTeamValue) {
  return team === DraftTeamValues.TimeA ? t('drafts.teams.blue') : t('drafts.teams.red')
}

function pickRoleLabel(captain: boolean) {
  return captain ? t('drafts.roles.captain') : t('drafts.roles.player')
}

function nextPickLabel() {
  if (props.draft?.proximoTime === DraftTeamValues.TimeA) {
    return t('drafts.teams.blue')
  }
  if (props.draft?.proximoTime === DraftTeamValues.TimeB) {
    return t('drafts.teams.red')
  }
  return t('drafts.status.finished')
}

function isActivePick(team: DraftTeamValue, index: number) {
  const slots = teamSlots(team)
  const firstEmptyIndex = slots.findIndex((player) => !player)
  return props.draft?.status === DraftStatusValues.Aberto && props.draft.proximoTime === team && firstEmptyIndex === index
}
</script>

<template>
  <section v-if="draft" class="draft-board" :aria-label="t('drafts.board.label')">
    <article class="draft-team draft-team--blue">
      <header class="draft-team__header">
        <h2>{{ teamName(DraftTeamValues.TimeA) }}</h2>
        <span>{{ t('drafts.board.captain', { name: draft.capitaoTimeA.nomeExibicao }) }}</span>
      </header>

      <ul class="draft-slots">
        <li
          v-for="(player, index) in teamSlots(DraftTeamValues.TimeA)"
          :key="player?.jogadorId ?? `time-a-empty-${index}`"
          class="draft-slot"
          :class="{ 'draft-slot--empty': !player, 'draft-slot--active': isActivePick(DraftTeamValues.TimeA, index) }"
        >
          <template v-if="player">
            <span class="draft-slot__avatar">{{ player.nomeExibicao.charAt(0) }}</span>
            <span class="draft-slot__copy">
              <strong>{{ player.nomeExibicao }}</strong>
              <small>{{ pickRoleLabel(player.capitao) }}</small>
            </span>
            <span v-if="player.capitao" class="draft-slot__captain">C</span>
          </template>
          <template v-else>
            <span>{{ isActivePick(DraftTeamValues.TimeA, index) ? t('drafts.board.selectPlayer') : t('drafts.board.emptySlot') }}</span>
            <small v-if="isActivePick(DraftTeamValues.TimeA, index)">{{ t('drafts.board.choosing') }}</small>
          </template>
        </li>
      </ul>
    </article>

    <article class="draft-available">
      <span class="draft-available__glow" aria-hidden="true" />
      <header class="draft-available__filters">
        <label class="draft-search-field">
            <span aria-hidden="true">⌕</span>
          <input v-model="playerSearch" type="search" :placeholder="t('drafts.board.searchPlaceholder')" />
        </label>
        <div class="draft-route-filters" :aria-label="t('drafts.board.routeFilters')">
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
          <span class="eyebrow">{{ t('drafts.board.nextPick') }}</span>
          <h2>{{ nextPickLabel() }}</h2>
        </div>
        <p>{{ t('drafts.board.criteria', { captains: draft.criterioCapitaes, firstPick: draft.criterioPrimeiroPick }) }}</p>
      </div>

      <div class="draft-player-grid" role="list" :aria-label="t('drafts.board.availablePlayers')">
        <div class="draft-player-row draft-player-row--head" role="row">
          <span>{{ t('drafts.board.player') }}</span>
          <span>{{ t('drafts.board.route') }}</span>
          <span>{{ t('common.elo') }}</span>
          <span>{{ t('drafts.board.action') }}</span>
        </div>
        <button
          v-for="player in availablePlayers"
          :key="player.id"
          type="button"
          class="draft-player-row"
          :disabled="picking || draft.status !== DraftStatusValues.Aberto"
          @click="$emit('pick', player)"
        >
          <span class="draft-player-row__identity">
            <span class="draft-slot__avatar">{{ player.nomeExibicao.charAt(0) }}</span>
            <strong>{{ player.nomeExibicao }}</strong>
          </span>
          <span class="draft-route-badge">{{ preferredRoute(player) }}</span>
          <span>{{ playerElo(player) }}</span>
          <span class="draft-pick-action">{{ t('drafts.board.pick') }}</span>
        </button>
      </div>
      <p v-if="!availablePlayers.length" class="empty-copy">{{ t('drafts.board.noAvailablePlayers') }}</p>
    </article>

    <article class="draft-team draft-team--red">
      <header class="draft-team__header">
        <h2>{{ teamName(DraftTeamValues.TimeB) }}</h2>
        <span>{{ t('drafts.board.captain', { name: draft.capitaoTimeB.nomeExibicao }) }}</span>
      </header>

      <ul class="draft-slots">
        <li
          v-for="(player, index) in teamSlots(DraftTeamValues.TimeB)"
          :key="player?.jogadorId ?? `time-b-empty-${index}`"
          class="draft-slot"
          :class="{ 'draft-slot--empty': !player, 'draft-slot--active': isActivePick(DraftTeamValues.TimeB, index) }"
        >
          <template v-if="player">
            <span class="draft-slot__avatar">{{ player.nomeExibicao.charAt(0) }}</span>
            <span class="draft-slot__copy">
              <strong>{{ player.nomeExibicao }}</strong>
              <small>{{ pickRoleLabel(player.capitao) }}</small>
            </span>
            <span v-if="player.capitao" class="draft-slot__captain">C</span>
          </template>
          <template v-else>
            <span>{{ isActivePick(DraftTeamValues.TimeB, index) ? t('drafts.board.selectPlayer') : t('drafts.board.emptySlot') }}</span>
            <small v-if="isActivePick(DraftTeamValues.TimeB, index)">{{ t('drafts.board.choosing') }}</small>
          </template>
        </li>
      </ul>
    </article>
  </section>
  <section v-else class="draft-empty-card">
    <h2>{{ t('drafts.noSelectionTitle') }}</h2>
    <p>{{ t('drafts.board.noSelectionDescription') }}</p>
  </section>
</template>
