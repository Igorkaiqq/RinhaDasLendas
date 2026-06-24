<script setup lang="ts">
/* global document, window, HTMLElement, HTMLCanvasElement */
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'

import type {
  DraftMontagem,
  DraftMontagemLayoutPayload,
  DraftMontagemParticipante,
  DraftMontagemRota,
} from '@/types/draftMontagem'

import PlayerDetailsDrawer from './PlayerDetailsDrawer.vue'

const props = defineProps<{ montagem: DraftMontagem; saving: boolean; canManage: boolean; currentPlayerId?: string | null }>()
const { t } = useI18n()
const emit = defineEmits<{
  save: [payload: DraftMontagemLayoutPayload]
  startRealtime: []
  pick: [jogadorId: string]
  substituteReserve: [payload: { timeId: string; jogadorSaiuId: string; reservaEntrouId: string; motivo?: string | null }]
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
const now = ref(Date.now())
const soundEnabled = ref(false)
let timerInterval: ReturnType<typeof globalThis.setInterval> | null = null
let audioContext: AudioContext | null = null
let lastTickSecond: number | null = null
const routeFilters = ['TODAS AS ROTAS', 'TOP', 'JUNGLE', 'MID', 'ADC', 'SUPORTE']
const routeByFilter: Record<string, DraftMontagemRota | null> = {
  'TODAS AS ROTAS': null,
  TOP: 'Top',
  JUNGLE: 'Jungle',
  MID: 'Mid',
  ADC: 'Adc',
  SUPORTE: 'Support',
}

const isRealtime = computed(() => localMontagem.value.modo === 'TempoReal')
const isReadOnly = computed(() => !props.canManage || localMontagem.value.status !== 'Aberta' || isRealtime.value)
const isOpen = computed(() => localMontagem.value.status === 'Aberta')
const hasActiveTurn = computed(() => isRealtime.value && isOpen.value && Boolean(localMontagem.value.turnoAtualTimeId && localMontagem.value.turnoAtualCapitaoId && localMontagem.value.turnoExpiraEm))
const canPick = computed(() => isRealtime.value && localMontagem.value.status === 'Aberta' && localMontagem.value.turnoAtualCapitaoId === props.currentPlayerId)
const currentTurnTeam = computed(() => localMontagem.value.times.find((time) => time.id === localMontagem.value.turnoAtualTimeId) ?? null)
const currentTurnCaptain = computed(() => allPlayers().find((player) => player.jogadorId === localMontagem.value.turnoAtualCapitaoId) ?? null)
const remainingSeconds = computed(() => {
  if (!localMontagem.value.turnoExpiraEm) {
    return 0
  }

  return Math.max(0, Math.ceil((new Date(localMontagem.value.turnoExpiraEm).getTime() - now.value) / 1000))
})
const turnProgress = computed(() => {
  if (!hasActiveTurn.value || !localMontagem.value.turnoExpiraEm) {
    return 0
  }

  return Math.max(0, Math.min(100, (remainingSeconds.value / localMontagem.value.duracaoTurnoSegundos) * 100))
})
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

watch(
  () => localMontagem.value.turnoSequencia,
  () => {
    lastTickSecond = null
  },
)

watch(remainingSeconds, (seconds) => {
  if (!hasActiveTurn.value || seconds <= 0 || seconds === lastTickSecond) {
    return
  }

  lastTickSecond = seconds
  playTimerSound(seconds)
})

onMounted(() => {
  timerInterval = globalThis.setInterval(() => {
    now.value = Date.now()
  }, 500)
})

onUnmounted(() => {
  if (timerInterval) {
    globalThis.clearInterval(timerInterval)
  }
  void audioContext?.close()
})

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

function canPickPlayer(player: DraftMontagemParticipante) {
  return canPick.value && player.estado === 'Livre' && !props.saving
}

async function toggleTimerSound() {
  if (soundEnabled.value) {
    soundEnabled.value = false
    return
  }

  audioContext = getAudioContext()
  if (!audioContext) {
    return
  }

  await audioContext.resume()
  soundEnabled.value = true
  playBeep(760, 0.08, 0.04)
}

function getAudioContext() {
  if (audioContext) {
    return audioContext
  }

  const AudioContextConstructor = window.AudioContext ?? (window as unknown as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext
  return AudioContextConstructor ? new AudioContextConstructor() : null
}

function playTimerSound(seconds: number) {
  if (!soundEnabled.value) {
    return
  }

  if (seconds <= 5) {
    playBeep(1040, 0.09, 0.07)
    playBeep(1320, 0.07, 0.045, 0.1)
    return
  }

  if (seconds <= 10) {
    playBeep(880, 0.08, 0.055)
    return
  }

  playBeep(520, 0.045, 0.035)
}

function playBeep(frequency: number, duration: number, volume: number, delay = 0) {
  const context = getAudioContext()
  if (!context) {
    return
  }

  const oscillator = context.createOscillator()
  const gain = context.createGain()
  const start = context.currentTime + delay
  oscillator.type = 'sine'
  oscillator.frequency.setValueAtTime(frequency, start)
  gain.gain.setValueAtTime(0.0001, start)
  gain.gain.exponentialRampToValueAtTime(volume, start + 0.01)
  gain.gain.exponentialRampToValueAtTime(0.0001, start + duration)
  oscillator.connect(gain)
  gain.connect(context.destination)
  oscillator.start(start)
  oscillator.stop(start + duration + 0.02)
}

function isTurnTeam(timeId: string) {
  return hasActiveTurn.value && localMontagem.value.turnoAtualTimeId === timeId
}

function substituteWithFirstReserve(timeId: string, jogadorSaiuId: string) {
  const reserve = localMontagem.value.reservas[0]
  if (!reserve) {
    return
  }

  emit('substituteReserve', { timeId, jogadorSaiuId, reservaEntrouId: reserve.jogadorId, motivo: null })
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

function eloSummary(player: DraftMontagemParticipante) {
  return [player.elo, player.divisao].filter(Boolean).join(' ') || t('common.eloNotInformed')
}

function participantRoleLabel(isCaptain: boolean) {
  return isCaptain ? t('drafts.roles.captain') : t('drafts.roles.player')
}

function captainName(time: DraftMontagem['times'][number]) {
  return time.jogadores.find((player) => player.jogadorId === time.capitaoId)?.nomeExibicao || t('drafts.visualBoard.pending')
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
        <p>{{ t('drafts.visualBoard.summary', { teams: localMontagem.quantidadeTimes, reserves: localMontagem.quantidadeReservas }) }}</p>
      </div>
      <div class="draft-visual-actions">
        <button v-if="canManage && !isRealtime && isOpen" type="button" class="button-secondary" :disabled="isReadOnly || saving" @click="emit('drawCaptains')">{{ t('drafts.visualBoard.drawCaptains') }}</button>
        <button v-if="canManage && !isRealtime && isOpen" type="button" class="button-secondary" :disabled="isReadOnly || saving" @click="emit('startRealtime')">{{ t('drafts.realtime.start') }}</button>
        <button v-if="canManage && isOpen" type="button" class="button-secondary" :disabled="!dirty || saving" @click="save">{{ saving ? t('common.saving') : t('drafts.visualBoard.saveLayout') }}</button>
        <button type="button" class="button-secondary" @click="exportImage">{{ t('drafts.visualBoard.exportImage') }}</button>
        <button v-if="canManage && isOpen" type="button" class="button-secondary" :disabled="isReadOnly || saving" @click="emit('cancel')">{{ t('common.cancel') }}</button>
        <button v-if="canManage && isOpen && !isRealtime" type="button" :disabled="dirty || saving" @click="emit('finalize')">{{ t('drafts.visualBoard.finalize') }}</button>
      </div>
    </header>

    <section v-if="hasActiveTurn" class="draft-turn-clock" :aria-label="t('drafts.realtime.turnClock')">
      <div class="draft-turn-clock__pulse" aria-hidden="true" />
      <div class="draft-turn-clock__copy">
        <span class="eyebrow">{{ t('drafts.realtime.onTheClock') }}</span>
        <strong>{{ currentTurnCaptain?.nomeExibicao ?? t('drafts.visualBoard.pending') }}</strong>
        <small>{{ currentTurnTeam?.nome ?? t('drafts.visualBoard.pending') }}</small>
      </div>
      <div class="draft-turn-clock__timer">
        <strong>{{ remainingSeconds }}</strong>
        <span>{{ t('drafts.realtime.seconds') }}</span>
        <button type="button" class="draft-turn-clock__sound" :class="{ 'is-active': soundEnabled }" @click="toggleTimerSound">
          {{ soundEnabled ? t('drafts.realtime.soundOn') : t('drafts.realtime.soundOff') }}
        </button>
      </div>
      <div class="draft-turn-clock__bar" aria-hidden="true">
        <span :style="{ width: `${turnProgress}%` }" />
      </div>
    </section>

    <div id="draft-visual-capture" class="draft-visual-board draft-board">
      <aside class="draft-visual-team-column" :aria-label="t('drafts.visualBoard.oddTeams')">
        <article v-for="time in leftTeams" :key="time.id" class="draft-team draft-visual-team" :class="[teamColorClass(time.cor), { 'draft-visual-team--turn': isTurnTeam(time.id) }]" @dragover.prevent @drop="movePlayer(time.id)">
          <header class="draft-team__header">
            <input v-model="time.nome" :disabled="isReadOnly" @input="dirty = true" />
            <span>{{ time.jogadores.length }} / {{ localMontagem.tamanhoEquipe }}<br />{{ t('drafts.board.captain', { name: captainName(time) }) }}</span>
          </header>
          <ul class="draft-slots">
            <li v-for="player in time.jogadores" :key="player.jogadorId" class="draft-slot draft-visual-slot" :class="{ 'is-captain': player.jogadorId === time.capitaoId }" :draggable="!isReadOnly" role="button" tabindex="0" @dragstart="dragged = { jogadorId: player.jogadorId }" @dragend="dragged = null" @click="detailsPlayer = player" @keydown.enter="detailsPlayer = player">
              <span class="draft-slot__avatar">{{ player.nomeExibicao.charAt(0) }}</span>
              <span class="draft-slot__copy">
                <strong>{{ player.nomeExibicao }}</strong>
                <small>{{ participantRoleLabel(player.jogadorId === time.capitaoId) }}</small>
                <small>{{ eloSummary(player) }}</small>
                <span class="draft-visual-routes">
                  <strong>{{ primaryRoute(player) }}</strong>
                  <small v-if="secondaryRoute(player)">{{ secondaryRoute(player) }}</small>
                </span>
              </span>
              <span v-if="player.jogadorId === time.capitaoId" class="draft-slot__captain">C</span>
              <button v-else-if="canManage && localMontagem.reservas.length" type="button" class="button-secondary draft-substitute-action" @click.stop="substituteWithFirstReserve(time.id, player.jogadorId)">{{ t('drafts.realtime.substitute') }}</button>
            </li>
            <li v-for="index in Math.max(localMontagem.tamanhoEquipe - time.jogadores.length, 0)" :key="`${time.id}-empty-${index}`" class="draft-slot draft-slot--empty">
              <span>{{ t('drafts.board.emptySlot') }}</span>
            </li>
          </ul>
        </article>
      </aside>

      <article class="draft-available draft-visual-pool" @dragover.prevent @drop="movePlayer('livres')">
        <span class="draft-available__glow" aria-hidden="true" />
        <header class="draft-available__filters">
          <label class="draft-search-field">
            <span aria-hidden="true">S</span>
            <input v-model="playerSearch" type="search" :placeholder="t('drafts.visualBoard.searchPlaceholder')" />
          </label>
          <div class="draft-route-filters" :aria-label="t('drafts.visualBoard.displayedRoutes')">
            <button v-for="route in routeFilters" :key="route" type="button" :class="{ 'is-active': selectedRoute === route }" @click="selectedRoute = route">
              {{ route }}
            </button>
          </div>
        </header>
        <header class="draft-available__status">
          <div>
            <span class="eyebrow">{{ t('drafts.visualBoard.available') }}</span>
            <h2>{{ t('drafts.visualBoard.availablePlayers') }}</h2>
          </div>
          <p>{{ t('drafts.visualBoard.reservesHint') }}</p>
        </header>
        <div class="draft-player-grid" role="list" :aria-label="t('drafts.board.availablePlayers')">
          <div class="draft-player-row draft-player-row--head" role="row">
            <span>{{ t('drafts.board.player') }}</span>
          </div>
          <article v-for="player in filteredAvailablePlayers" :key="player.jogadorId" class="draft-player-row draft-visual-player-row" :draggable="!isReadOnly && player.estado === 'Livre'" role="button" tabindex="0" @dragstart="dragged = { jogadorId: player.jogadorId }" @dragend="dragged = null" @click="detailsPlayer = player" @keydown.enter="detailsPlayer = player">
            <span class="draft-player-row__identity">
              <span class="draft-slot__avatar">{{ player.nomeExibicao.charAt(0) }}</span>
              <span class="draft-slot__copy">
                <strong>
                  {{ player.nomeExibicao }}
                  <span v-if="player.estado === 'Reserva'" class="draft-visual-reserve-badge">{{ t('drafts.visualBoard.reserve') }}</span>
                </strong>
                <small>{{ eloSummary(player) }}</small>
                <span class="draft-visual-routes">
                  <strong>{{ primaryRoute(player) }}</strong>
                  <small v-if="secondaryRoute(player)">{{ secondaryRoute(player) }}</small>
                </span>
              </span>
            </span>
            <button v-if="canPickPlayer(player)" type="button" class="draft-pick-action" @click.stop="emit('pick', player.jogadorId)">{{ t('drafts.realtime.pick') }}</button>
            <span v-else-if="isRealtime && player.estado === 'Reserva'" class="draft-visual-reserve-badge">{{ t('drafts.realtime.emergencyReserve') }}</span>
          </article>
        </div>
        <p v-if="!filteredAvailablePlayers.length" class="empty-copy">{{ t('drafts.visualBoard.noPlayersForFilter') }}</p>
      </article>

      <aside class="draft-visual-team-column" :aria-label="t('drafts.visualBoard.evenTeams')">
        <article v-for="time in rightTeams" :key="time.id" class="draft-team draft-visual-team" :class="[teamColorClass(time.cor), { 'draft-visual-team--turn': isTurnTeam(time.id) }]" @dragover.prevent @drop="movePlayer(time.id)">
          <header class="draft-team__header">
            <input v-model="time.nome" :disabled="isReadOnly" @input="dirty = true" />
            <span>{{ time.jogadores.length }} / {{ localMontagem.tamanhoEquipe }}<br />{{ t('drafts.board.captain', { name: captainName(time) }) }}</span>
          </header>
          <ul class="draft-slots">
            <li v-for="player in time.jogadores" :key="player.jogadorId" class="draft-slot draft-visual-slot" :class="{ 'is-captain': player.jogadorId === time.capitaoId }" :draggable="!isReadOnly" role="button" tabindex="0" @dragstart="dragged = { jogadorId: player.jogadorId }" @dragend="dragged = null" @click="detailsPlayer = player" @keydown.enter="detailsPlayer = player">
              <span class="draft-slot__avatar">{{ player.nomeExibicao.charAt(0) }}</span>
              <span class="draft-slot__copy">
                <strong>{{ player.nomeExibicao }}</strong>
                <small>{{ participantRoleLabel(player.jogadorId === time.capitaoId) }}</small>
                <small>{{ eloSummary(player) }}</small>
                <span class="draft-visual-routes">
                  <strong>{{ primaryRoute(player) }}</strong>
                  <small v-if="secondaryRoute(player)">{{ secondaryRoute(player) }}</small>
                </span>
              </span>
              <span v-if="player.jogadorId === time.capitaoId" class="draft-slot__captain">C</span>
              <button v-else-if="canManage && localMontagem.reservas.length" type="button" class="button-secondary draft-substitute-action" @click.stop="substituteWithFirstReserve(time.id, player.jogadorId)">{{ t('drafts.realtime.substitute') }}</button>
            </li>
            <li v-for="index in Math.max(localMontagem.tamanhoEquipe - time.jogadores.length, 0)" :key="`${time.id}-empty-${index}`" class="draft-slot draft-slot--empty">
              <span>{{ t('drafts.board.emptySlot') }}</span>
            </li>
          </ul>
        </article>
      </aside>
    </div>

    <PlayerDetailsDrawer :player="detailsPlayer" @close="detailsPlayer = null" />
  </section>
</template>
