<script setup lang="ts">
import { computed, ref, watch } from 'vue'

import type {
  DraftMontagem,
  DraftMontagemLayoutPayload,
  DraftMontagemParticipante,
  DraftMontagemRota,
} from '@/types/draftMontagem'

import PlayerDetailsDrawer from './PlayerDetailsDrawer.vue'

const props = defineProps<{ montagem: DraftMontagem; saving: boolean }>()
const emit = defineEmits<{
  save: [payload: DraftMontagemLayoutPayload]
  drawCaptains: []
  finalize: []
  cancel: []
}>()

const localMontagem = ref<DraftMontagem>(cloneMontagem(props.montagem))
const dragged = ref<{ jogadorId: string } | null>(null)
const detailsPlayer = ref<DraftMontagemParticipante | null>(null)
const dirty = ref(false)
const playerSearch = ref('')
const selectedRoute = ref('TODAS AS ROTAS')
const routeFilters = ['TODAS AS ROTAS', 'TOP', 'JUNGLE', 'MID', 'ADC', 'SUPORTE']
const routeByFilter: Record<string, DraftMontagemRota | null> = {
  'TODAS AS ROTAS': null,
  TOP: 'Top',
  JUNGLE: 'Jungle',
  MID: 'Mid',
  ADC: 'Adc',
  SUPORTE: 'Support',
}

const isReadOnly = computed(() => localMontagem.value.status !== 'Aberta')
const availablePlayers = computed(() => [...localMontagem.value.livres, ...localMontagem.value.reservas])
const filteredAvailablePlayers = computed(() => {
  const search = playerSearch.value.trim().toLowerCase()
  const route = routeByFilter[selectedRoute.value]

  return availablePlayers.value.filter((player) => {
    const routes = preferredRoutes(player)
    const matchesSearch =
      !search ||
      player.nomeExibicao.toLowerCase().includes(search) ||
      eloSummary(player).toLowerCase().includes(search) ||
      routes.some((item) => item.toLowerCase().includes(search))
    const matchesRoute = !route || routes.includes(route)

    return matchesSearch && matchesRoute
  })
})
const leftTeams = computed(() => localMontagem.value.times.filter((_, index) => index % 2 === 0))
const rightTeams = computed(() => localMontagem.value.times.filter((_, index) => index % 2 === 1))

watch(
  () => props.montagem,
  (montagem) => {
    localMontagem.value = cloneMontagem(montagem)
    dirty.value = false
  },
)

function cloneMontagem(montagem: DraftMontagem): DraftMontagem {
  return JSON.parse(JSON.stringify(montagem)) as DraftMontagem
}

function allPlayers() {
  return [
    ...localMontagem.value.livres,
    ...localMontagem.value.reservas,
    ...localMontagem.value.times.flatMap((time) => time.jogadores),
  ]
}

function removePlayer(jogadorId: string) {
  localMontagem.value.livres = localMontagem.value.livres.filter((player) => player.jogadorId !== jogadorId)
  localMontagem.value.reservas = localMontagem.value.reservas.filter((player) => player.jogadorId !== jogadorId)
  localMontagem.value.times.forEach((time) => {
    time.jogadores = time.jogadores.filter((player) => player.jogadorId !== jogadorId)
    if (time.capitaoId === jogadorId) {
      time.capitaoId = time.jogadores.find((player) => player.capitao)?.jogadorId ?? null
    }
  })
}

function movePlayer(target: 'livres' | 'reservas' | string) {
  if (isReadOnly.value || !dragged.value) {
    return
  }
  const player = allPlayers().find((item) => item.jogadorId === dragged.value?.jogadorId)
  if (!player) {
    return
  }

  removePlayer(player.jogadorId)
  const moved = { ...player, capitao: false }
  if (target === 'livres') {
    moved.estado = 'Livre'
    moved.ordem = localMontagem.value.livres.length + 1
    localMontagem.value.livres.push(moved)
  } else if (target === 'reservas') {
    moved.estado = 'Reserva'
    moved.ordem = localMontagem.value.reservas.length + 1
    localMontagem.value.reservas.push(moved)
  } else {
    const time = localMontagem.value.times.find((item) => item.id === target)
    if (!time || time.jogadores.length >= localMontagem.value.tamanhoEquipe) {
      return
    }
    moved.estado = 'Time'
    moved.ordem = time.jogadores.length + 1
    moved.capitao = time.capitaoId === moved.jogadorId
    time.jogadores.push(moved)
  }
  dirty.value = true
}

function teamColorClass(cor: string) {
  return ['blue', 'red'].includes(cor) ? `draft-team--${cor}` : `draft-visual-team--${cor}`
}

function preferredRoutes(player: DraftMontagemParticipante) {
  const routes = player.preferencias
    .filter((preference) => !preference.naoJogoNemLascando)
    .sort((current, next) => current.prioridade - next.prioridade)
    .map((preference) => preference.rota as DraftMontagemRota)

  return [player.rotaContextual, ...routes]
    .filter((route): route is DraftMontagemRota => Boolean(route))
    .filter((route, index, allRoutes) => allRoutes.indexOf(route) === index)
    .slice(0, 2)
}

function routeSummary(player: DraftMontagemParticipante) {
  const routes = preferredRoutes(player)
  return routes.length ? routes.join(' / ') : '--'
}

function eloSummary(player: DraftMontagemParticipante) {
  return [player.elo, player.divisao].filter(Boolean).join(' ') || 'Elo nao informado'
}

function primaryRoute(player: DraftMontagemParticipante) {
  return preferredRoutes(player)[0] ?? '--'
}

function secondaryRoute(player: DraftMontagemParticipante) {
  return preferredRoutes(player)[1] ?? null
}

function toParticipantPayload(player: DraftMontagemParticipante, index: number) {
  return { jogadorId: player.jogadorId, ordem: index + 1, rotaContextual: preferredRoutes(player)[0] ?? null }
}

function save() {
  emit('save', {
    times: localMontagem.value.times.map((time) => ({
      timeId: time.id,
      nome: time.nome,
      capitaoId: time.capitaoId,
      jogadores: time.jogadores.map(toParticipantPayload),
    })),
    livres: localMontagem.value.livres.map(toParticipantPayload),
    reservas: localMontagem.value.reservas.map(toParticipantPayload),
  })
}

async function exportImage() {
  const element = document.getElementById('draft-visual-capture')
  const html2canvas = (window as unknown as { html2canvas?: (element: HTMLElement, options: object) => Promise<HTMLCanvasElement> }).html2canvas
  if (!element || !html2canvas) {
    window.print()
    return
  }
  const canvas = await html2canvas(element, { backgroundColor: '#0c1320', scale: 2 })
  const link = document.createElement('a')
  link.download = `${localMontagem.value.nome || 'montagem'}-times.png`
  link.href = canvas.toDataURL('image/png')
  link.click()
}
</script>

<template>
  <section class="draft-visual-shell draft-panel">
    <header class="draft-summary draft-visual-summary">
      <div>
        <span class="draft-status draft-status--aberto">{{ localMontagem.status }}</span>
        <h2>{{ localMontagem.nome }}</h2>
        <p>{{ localMontagem.quantidadeTimes }} times · {{ localMontagem.quantidadeReservas }} reservas.</p>
      </div>
      <div class="draft-visual-actions">
        <button type="button" class="button-secondary" :disabled="isReadOnly || saving" @click="emit('drawCaptains')">Sortear capitaes</button>
        <button type="button" class="button-secondary" :disabled="!dirty || saving" @click="save">{{ saving ? 'Salvando...' : 'Salvar layout' }}</button>
        <button type="button" class="button-secondary" @click="exportImage">Exportar imagem</button>
        <button type="button" class="button-secondary" :disabled="isReadOnly || saving" @click="emit('cancel')">Cancelar</button>
        <button type="button" :disabled="isReadOnly || dirty || saving" @click="emit('finalize')">Finalizar</button>
      </div>
    </header>

    <div id="draft-visual-capture" class="draft-visual-board draft-board">
      <aside class="draft-visual-team-column" aria-label="Times impares">
        <article v-for="time in leftTeams" :key="time.id" class="draft-team draft-visual-team" :class="teamColorClass(time.cor)" @dragover.prevent @drop="movePlayer(time.id)">
          <header class="draft-team__header">
            <input v-model="time.nome" :disabled="isReadOnly" @input="dirty = true" />
            <span>{{ time.jogadores.length }} / {{ localMontagem.tamanhoEquipe }}<br />Capitao: {{ time.jogadores.find((player) => player.jogadorId === time.capitaoId)?.nomeExibicao || 'pendente' }}</span>
          </header>
          <ul class="draft-slots">
            <li v-for="player in time.jogadores" :key="player.jogadorId" class="draft-slot draft-visual-slot" :class="{ 'is-captain': player.jogadorId === time.capitaoId }" draggable="true" role="button" tabindex="0" @dragstart="dragged = { jogadorId: player.jogadorId }" @dragend="dragged = null" @click="detailsPlayer = player" @keydown.enter="detailsPlayer = player">
              <span class="draft-slot__avatar">{{ player.nomeExibicao.charAt(0) }}</span>
              <span class="draft-slot__copy">
                <strong>{{ player.nomeExibicao }}</strong>
                <small>{{ player.jogadorId === time.capitaoId ? 'CAPITAO' : 'JOGADOR' }}</small>
                <small>{{ eloSummary(player) }}</small>
                <span class="draft-visual-routes">
                  <strong>{{ primaryRoute(player) }}</strong>
                  <small v-if="secondaryRoute(player)">{{ secondaryRoute(player) }}</small>
                </span>
              </span>
              <span v-if="player.jogadorId === time.capitaoId" class="draft-slot__captain">C</span>
            </li>
            <li v-for="index in Math.max(localMontagem.tamanhoEquipe - time.jogadores.length, 0)" :key="`${time.id}-empty-${index}`" class="draft-slot draft-slot--empty">
              <span>Slot vazio</span>
            </li>
          </ul>
        </article>
      </aside>

      <article class="draft-available draft-visual-pool" @dragover.prevent @drop="movePlayer('livres')">
        <span class="draft-available__glow" aria-hidden="true" />
        <header class="draft-available__filters">
          <label class="draft-search-field">
            <span aria-hidden="true">S</span>
            <input v-model="playerSearch" type="search" placeholder="Buscar jogadores por nome, elo ou rota..." />
          </label>
          <div class="draft-route-filters" aria-label="Rotas exibidas">
            <button v-for="route in routeFilters" :key="route" type="button" :class="{ 'is-active': selectedRoute === route }" @click="selectedRoute = route">
              {{ route }}
            </button>
          </div>
        </header>
        <header class="draft-available__status">
          <div>
            <span class="eyebrow">Disponiveis</span>
            <h2>Jogadores Disponiveis</h2>
          </div>
          <p>Reservas ficam marcados aqui</p>
        </header>
        <div class="draft-player-grid" role="list" aria-label="Jogadores disponiveis">
          <div class="draft-player-row draft-player-row--head" role="row">
            <span>Jogador</span>
          </div>
          <article v-for="player in filteredAvailablePlayers" :key="player.jogadorId" class="draft-player-row draft-visual-player-row" draggable="true" role="button" tabindex="0" @dragstart="dragged = { jogadorId: player.jogadorId }" @dragend="dragged = null" @click="detailsPlayer = player" @keydown.enter="detailsPlayer = player">
            <span class="draft-player-row__identity">
              <span class="draft-slot__avatar">{{ player.nomeExibicao.charAt(0) }}</span>
              <span class="draft-slot__copy">
                <strong>
                  {{ player.nomeExibicao }}
                  <span v-if="player.estado === 'Reserva'" class="draft-visual-reserve-badge">Reserva</span>
                </strong>
                <small>{{ eloSummary(player) }}</small>
                <span class="draft-visual-routes">
                  <strong>{{ primaryRoute(player) }}</strong>
                  <small v-if="secondaryRoute(player)">{{ secondaryRoute(player) }}</small>
                </span>
              </span>
            </span>
          </article>
        </div>
        <p v-if="!filteredAvailablePlayers.length" class="empty-copy">Sem jogadores disponiveis para o filtro atual.</p>
      </article>

      <aside class="draft-visual-team-column" aria-label="Times pares">
        <article v-for="time in rightTeams" :key="time.id" class="draft-team draft-visual-team" :class="teamColorClass(time.cor)" @dragover.prevent @drop="movePlayer(time.id)">
          <header class="draft-team__header">
            <input v-model="time.nome" :disabled="isReadOnly" @input="dirty = true" />
            <span>{{ time.jogadores.length }} / {{ localMontagem.tamanhoEquipe }}<br />Capitao: {{ time.jogadores.find((player) => player.jogadorId === time.capitaoId)?.nomeExibicao || 'pendente' }}</span>
          </header>
          <ul class="draft-slots">
            <li v-for="player in time.jogadores" :key="player.jogadorId" class="draft-slot draft-visual-slot" :class="{ 'is-captain': player.jogadorId === time.capitaoId }" draggable="true" role="button" tabindex="0" @dragstart="dragged = { jogadorId: player.jogadorId }" @dragend="dragged = null" @click="detailsPlayer = player" @keydown.enter="detailsPlayer = player">
              <span class="draft-slot__avatar">{{ player.nomeExibicao.charAt(0) }}</span>
              <span class="draft-slot__copy">
                <strong>{{ player.nomeExibicao }}</strong>
                <small>{{ player.jogadorId === time.capitaoId ? 'CAPITAO' : 'JOGADOR' }}</small>
                <small>{{ eloSummary(player) }}</small>
                <span class="draft-visual-routes">
                  <strong>{{ primaryRoute(player) }}</strong>
                  <small v-if="secondaryRoute(player)">{{ secondaryRoute(player) }}</small>
                </span>
              </span>
              <span v-if="player.jogadorId === time.capitaoId" class="draft-slot__captain">C</span>
            </li>
            <li v-for="index in Math.max(localMontagem.tamanhoEquipe - time.jogadores.length, 0)" :key="`${time.id}-empty-${index}`" class="draft-slot draft-slot--empty">
              <span>Slot vazio</span>
            </li>
          </ul>
        </article>
      </aside>
    </div>

    <PlayerDetailsDrawer :player="detailsPlayer" @close="detailsPlayer = null" />
  </section>
</template>
