<script setup lang="ts">
/* global document, window, HTMLElement, HTMLCanvasElement */
import { computed, nextTick, onMounted, onUnmounted, ref, useTemplateRef, watch } from 'vue'
import { useI18n } from 'vue-i18n'

import { DraftMontagemEstadoValues, DraftMontagemModoValues, DraftMontagemStatusValues } from '@/constants/draftMontagem'
import {
  DRAFT_MONTAGEM_ROUTE_BY_FILTER,
  DRAFT_ROUTE_FILTER_OPTIONS,
  DraftRouteFilterValues,
  type DraftRouteFilterValue,
} from '@/constants/draftRouteFilters'
import type {
  DraftMontagem,
  DraftMontagemLayoutPayload,
  DraftMontagemParticipante,
  DraftMontagemRota,
  DraftMontagemSubstituicaoPayload,
} from '@/types/draftMontagem'

import DraftSubstitutionDialog from './DraftSubstitutionDialog.vue'
import PlayerDetailsDrawer from './PlayerDetailsDrawer.vue'

type BoardEvent = InstanceType<typeof globalThis.Event>

const props = withDefaults(defineProps<{ montagem: DraftMontagem; saving: boolean; canManage: boolean; currentPlayerId?: string | null; canCurrentUserPick?: boolean | null; serverClockOffsetMs?: number; eligibleCaptainIds?: string[] }>(), {
  eligibleCaptainIds: () => [],
})
const { t } = useI18n()
const emit = defineEmits<{
  save: [payload: DraftMontagemLayoutPayload]
  startRealtime: []
  pick: [jogadorId: string]
  substituteReserve: [payload: DraftMontagemSubstituicaoPayload, complete: (success: boolean) => void]
  drawCaptains: []
  finalize: []
  cancel: []
}>()

const localMontagem = ref<DraftMontagem>(cloneMontagem(props.montagem))
const dragged = ref<{ jogadorId: string } | null>(null)
const detailsPlayer = ref<DraftMontagemParticipante | null>(null)
const dirty = ref(false)
const playerSearch = ref('')
const selectedRoute = ref<DraftRouteFilterValue>(DraftRouteFilterValues.All)
const now = ref(Date.now())
const soundEnabled = ref(false)
const pickLocked = ref(false)
const substituteLocked = ref(false)
const substitutionContext = ref<{
  timeId: string
  jogadorSaiuId: string
  openedVersion: number
  teamMemberIds: string[]
  reserveIds: string[]
} | null>(null)
const substitutionTrigger = ref<InstanceType<typeof globalThis.HTMLElement> | null>(null)
const moveAnnouncement = ref('')
const boardShell = useTemplateRef<InstanceType<typeof globalThis.HTMLElement>>('boardShell')
let timerInterval: ReturnType<typeof globalThis.setInterval> | null = null
let audioContext: AudioContext | null = null
let lastTickSecond: number | null = null
const routeFilters = DRAFT_ROUTE_FILTER_OPTIONS
const routeByFilter = DRAFT_MONTAGEM_ROUTE_BY_FILTER
const routeFilterI18nKeys: Record<DraftRouteFilterValue, string> = {
  [DraftRouteFilterValues.All]: 'drafts.visualBoard.routeFilters.all',
  [DraftRouteFilterValues.Top]: 'drafts.visualBoard.routeFilters.top',
  [DraftRouteFilterValues.Jungle]: 'drafts.visualBoard.routeFilters.jungle',
  [DraftRouteFilterValues.Mid]: 'drafts.visualBoard.routeFilters.mid',
  [DraftRouteFilterValues.Adc]: 'drafts.visualBoard.routeFilters.adc',
  [DraftRouteFilterValues.Support]: 'drafts.visualBoard.routeFilters.support',
}

const isRealtime = computed(() => localMontagem.value.modo === DraftMontagemModoValues.TempoReal)
const isTerminal = computed(() => localMontagem.value.status === DraftMontagemStatusValues.Finalizada || localMontagem.value.status === DraftMontagemStatusValues.Cancelada)
const isReadOnly = computed(() => !props.canManage || localMontagem.value.status !== DraftMontagemStatusValues.Aberta || isRealtime.value)
const isOpen = computed(() => localMontagem.value.status === DraftMontagemStatusValues.Aberta)
const canSubstitute = computed(() => props.canManage && !isTerminal.value && (
  isOpen.value
  || (localMontagem.value.cicloVersao === 'ModoPosPresenca'
    && isRealtime.value
    && (localMontagem.value.status === DraftMontagemStatusValues.CapitaesDefinidos
      || localMontagem.value.status === DraftMontagemStatusValues.OrdemDefinida))
))
const isManualV2 = computed(() => localMontagem.value.modo === DraftMontagemModoValues.Manual && localMontagem.value.cicloVersao === 'ModoPosPresenca')
const isManualV2Open = computed(() => isManualV2.value && isOpen.value)
const canStartRealtime = computed(() => (
  isRealtime.value && localMontagem.value.status === DraftMontagemStatusValues.OrdemDefinida
) || (
  localMontagem.value.cicloVersao === 'Legado' && !isRealtime.value && isOpen.value
))
const manualLayoutComplete = computed(() => (
  isManualV2Open.value
  && localMontagem.value.times.length === localMontagem.value.quantidadeTimes
  && localMontagem.value.times.every((time) => time.jogadores.length === localMontagem.value.tamanhoEquipe)
  && localMontagem.value.livres.length === 0
))
const currentTurnTeam = computed(() => localMontagem.value.times.find((time) => time.id === localMontagem.value.turnoAtualTimeId) ?? null)
const currentTurnCaptain = computed(() => {
  const team = currentTurnTeam.value
  if (!team || team.capitaoId !== localMontagem.value.turnoAtualCapitaoId) return null
  return team.jogadores.find((player) => player.jogadorId === team.capitaoId && player.capitao) ?? null
})
const remainingSeconds = computed(() => {
  if (!localMontagem.value.turnoExpiraEm) {
    return 0
  }

  return Math.max(0, Math.ceil((new Date(localMontagem.value.turnoExpiraEm).getTime() - (now.value + (props.serverClockOffsetMs ?? 0))) / 1000))
})
const hasActiveTurn = computed(() => isRealtime.value && isOpen.value && Boolean(currentTurnTeam.value && currentTurnCaptain.value && remainingSeconds.value > 0))
const canPick = computed(() => props.canCurrentUserPick === true && hasActiveTurn.value && localMontagem.value.turnoAtualCapitaoId === props.currentPlayerId)
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
const orderedTeams = computed(() => [...localMontagem.value.times].sort((current, next) => current.ordem - next.ordem))
const leftTeams = computed(() => orderedTeams.value.filter((_, index) => index % 2 === 0))
const rightTeams = computed(() => orderedTeams.value.filter((_, index) => index % 2 === 1))
const presentedChoices = computed(() => {
  const teamsById = new Map(localMontagem.value.times.map((team) => [team.id, team]))
  const picksByTeam = new Map<string, number>()

  return localMontagem.value.escolhas
    .map((choice, originalIndex) => ({ choice, originalIndex }))
    .sort((current, next) => current.choice.sequencia - next.choice.sequencia || current.originalIndex - next.originalIndex)
    .map(({ choice, originalIndex }) => {
      const teamPickOrder = (picksByTeam.get(choice.timeId) ?? 0) + 1
      picksByTeam.set(choice.timeId, teamPickOrder)

      return {
        key: `${choice.sequencia}-${choice.timeId}-${originalIndex}`,
        choice,
        teamName: teamsById.get(choice.timeId)?.nome,
        teamPickOrder,
      }
    })
})
const completedPicks = computed(() => localMontagem.value.escolhas.filter((choice) => Boolean(choice.jogadorId)).length)
const totalPicks = computed(() => localMontagem.value.quantidadeTimes * Math.max(localMontagem.value.tamanhoEquipe - 1, 0))
const realtimeAnnouncement = computed(() => hasActiveTurn.value
  ? t('drafts.realtime.liveStatus', {
      captain: currentTurnCaptain.value?.nomeExibicao ?? t('drafts.visualBoard.pending'),
      team: currentTurnTeam.value?.nome ?? t('drafts.visualBoard.pending'),
      current: completedPicks.value,
      total: totalPicks.value,
    })
  : t('drafts.realtime.liveProgress', { current: completedPicks.value, total: totalPicks.value }))
const substitutionTeam = computed(() => {
  const context = substitutionContext.value
  return context ? localMontagem.value.times.find((team) => team.id === context.timeId) ?? null : null
})
const substitutionPlayer = computed(() => {
  const context = substitutionContext.value
  return context ? substitutionTeam.value?.jogadores.find((player) => player.jogadorId === context.jogadorSaiuId) ?? null : null
})
const substitutionContextValid = computed(() => Boolean(
  substitutionContext.value
  && localMontagem.value.versaoEstado === substitutionContext.value.openedVersion
  && canSubstitute.value
  && substitutionTeam.value
  && substitutionPlayer.value
  && idsEqual(substitutionContext.value.teamMemberIds, substitutionTeam.value.jogadores.map((player) => player.jogadorId))
  && idsEqual(substitutionContext.value.reserveIds, localMontagem.value.reservas.map((reserve) => reserve.jogadorId))
  && localMontagem.value.reservas.some((reserve) => reserve.estado === DraftMontagemEstadoValues.Reserva),
))

watch(
  () => props.montagem,
  (montagem) => {
    localMontagem.value = cloneMontagem(montagem)
    dirty.value = false
    pickLocked.value = false
    substituteLocked.value = false
    if (substitutionContext.value && !substitutionContextValid.value) closeSubstitution()
  },
)

watch(
  () => props.saving,
  (saving, wasSaving) => {
    if (wasSaving && !saving) {
      pickLocked.value = false
      substituteLocked.value = false
    }
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
  movePlayerById(dragged.value.jogadorId, target)
}

function movePlayerById(jogadorId: string, target: 'livres' | 'reservas' | string) {
  if (isReadOnly.value) return false
  const player = allPlayers().find((item) => item.jogadorId === jogadorId)
  if (!player) {
    return false
  }

  const isPoolTarget = target === 'livres' || target === 'reservas'
  const targetTeam = isPoolTarget
    ? null
    : localMontagem.value.times.find((item) => item.id === target)
  if (!isPoolTarget && !targetTeam) return false
  if (targetTeam && targetTeam.jogadores.length >= localMontagem.value.tamanhoEquipe) return false

  removePlayer(player.jogadorId)
  const moved = { ...player, capitao: false }
  if (target === 'livres') {
    moved.estado = DraftMontagemEstadoValues.Livre
    moved.ordem = localMontagem.value.livres.length + 1
    localMontagem.value.livres.push(moved)
  } else if (target === 'reservas') {
    moved.estado = DraftMontagemEstadoValues.Reserva
    moved.ordem = localMontagem.value.reservas.length + 1
    localMontagem.value.reservas.push(moved)
  } else {
    const time = targetTeam
    if (!time) return
    moved.estado = DraftMontagemEstadoValues.Time
    moved.ordem = time.jogadores.length + 1
    moved.capitao = time.capitaoId === moved.jogadorId
    time.jogadores.push(moved)
  }
  dirty.value = true
  return true
}

function canPickPlayer(player: DraftMontagemParticipante) {
  return canPick.value && player.estado === DraftMontagemEstadoValues.Livre && !props.saving && !pickLocked.value
}

function pickPlayer(player: DraftMontagemParticipante) {
  if (!canPickPlayer(player)) return
  pickLocked.value = true
  emit('pick', player.jogadorId)
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

function requestSubstitution(timeId: string, jogadorSaiuId: string, event: BoardEvent) {
  const team = localMontagem.value.times.find((item) => item.id === timeId)
  const player = team?.jogadores.find((item) => item.jogadorId === jogadorSaiuId)
  if (
    substituteLocked.value
    || props.saving
    || !canSubstitute.value
    || !team
    || !player
    || !localMontagem.value.reservas.some((reserve) => reserve.estado === DraftMontagemEstadoValues.Reserva)
  ) return

  substitutionTrigger.value = event.currentTarget as InstanceType<typeof globalThis.HTMLElement>
  substitutionContext.value = {
    timeId,
    jogadorSaiuId,
    openedVersion: localMontagem.value.versaoEstado,
    teamMemberIds: team.jogadores.map((item) => item.jogadorId).sort(),
    reserveIds: localMontagem.value.reservas.map((reserve) => reserve.jogadorId).sort(),
  }
}

function confirmSubstitution(payload: DraftMontagemSubstituicaoPayload) {
  const context = substitutionContext.value
  if (substituteLocked.value || props.saving) return
  if (
    !context
    || !substitutionContextValid.value
    || payload.timeId !== context.timeId
    || payload.jogadorSaiuId !== context.jogadorSaiuId
    || !localMontagem.value.reservas.some((reserve) => reserve.jogadorId === payload.reservaEntrouId && reserve.estado === DraftMontagemEstadoValues.Reserva)
  ) {
    if (context) closeSubstitution()
    return
  }

  substituteLocked.value = true
  const submittedContext = { ...context }
  emit('substituteReserve', payload, (success) => {
    if (
      !substitutionContext.value
      || substitutionContext.value.timeId !== submittedContext.timeId
      || substitutionContext.value.jogadorSaiuId !== submittedContext.jogadorSaiuId
      || substitutionContext.value.openedVersion !== submittedContext.openedVersion
    ) return

    substituteLocked.value = false
    if (success && localMontagem.value.versaoEstado > submittedContext.openedVersion) closeSubstitution()
  })
}

function cancelSubstitution() {
  if (props.saving) return
  closeSubstitution()
}

function closeSubstitution() {
  substitutionContext.value = null
  substituteLocked.value = false
  restoreSubstitutionFocus()
}

function restoreSubstitutionFocus() {
  void nextTick(() => {
    const trigger = substitutionTrigger.value
    ;(trigger?.isConnected ? trigger : boardShell.value)?.focus()
  })
}

function idsEqual(expected: string[], current: string[]) {
  const sortedCurrent = [...current].sort()
  return expected.length === sortedCurrent.length && expected.every((id, index) => id === sortedCurrent[index])
}

type MoveControlEvent = InstanceType<typeof globalThis.Event>

async function moveFromControl(player: DraftMontagemParticipante, event: MoveControlEvent) {
  const target = (event.target as unknown as { value: string }).value
  if (!target || !movePlayerById(player.jogadorId, target)) return

  const destination = target === 'livres'
    ? t('drafts.visualBoard.moveToFree')
    : target === 'reservas'
      ? t('drafts.visualBoard.moveToReserves')
      : localMontagem.value.times.find((team) => team.id === target)?.nome
  if (!destination) return

  moveAnnouncement.value = ''
  await nextTick()
  moveAnnouncement.value = t('drafts.visualBoard.moveAnnouncement', { name: player.nomeExibicao, destination })
  await nextTick()
  const movedRow = [...(boardShell.value?.querySelectorAll<InstanceType<typeof globalThis.HTMLElement>>('[data-player-id]') ?? [])]
    .find((row) => row.dataset.playerId === player.jogadorId)
  const focusTarget = movedRow?.querySelector<InstanceType<typeof globalThis.HTMLElement>>('[data-move-destination], [data-player-details]')
    ?? boardShell.value?.querySelector<InstanceType<typeof globalThis.HTMLElement>>('[name="draft-player-search"]')
  focusTarget?.focus()
}

function detailsLabel(player: DraftMontagemParticipante) {
  return t('drafts.visualBoard.detailsFor', { name: player.nomeExibicao })
}

function moveDestinationLabel(player: DraftMontagemParticipante) {
  return t('drafts.visualBoard.moveDestination', { name: player.nomeExibicao })
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

function routeFilterLabel(route: DraftRouteFilterValue) {
  return t(routeFilterI18nKeys[route])
}

function captainName(time: DraftMontagem['times'][number]) {
  return time.jogadores.find((player) => player.jogadorId === time.capitaoId)?.nomeExibicao || t('drafts.visualBoard.pending')
}

function choiceName(choice: DraftMontagem['escolhas'][number]) {
  return choice.jogadorNome || (choice.tipo === 'Timeout' ? t('drafts.visualBoard.timeout') : t('drafts.visualBoard.pending'))
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
      capitaoId: isManualV2.value ? null : time.capitaoId,
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
  <section ref="boardShell" class="draft-visual-shell draft-panel" :aria-label="t('drafts.board.label')" tabindex="-1">
    <div class="draft-visual-actions">
      <button v-if="canManage && localMontagem.cicloVersao === 'Legado' && !isRealtime && isOpen" type="button" class="button-secondary" :disabled="isReadOnly || saving" @click="emit('drawCaptains')">{{ t('drafts.visualBoard.drawCaptains') }}</button>
      <button v-if="canManage && canStartRealtime" data-testid="start-realtime" :data-stage-primary-action="localMontagem.cicloVersao === 'ModoPosPresenca' ? '' : undefined" type="button" :disabled="saving" @click="emit('startRealtime')">{{ t('drafts.realtime.start') }}</button>
      <button v-if="canManage && !isRealtime && isOpen" type="button" class="button-secondary" :disabled="!dirty || saving" @click="save">{{ saving ? t('common.saving') : t('drafts.visualBoard.saveLayout') }}</button>
      <button type="button" class="button-secondary" @click="exportImage">{{ t('drafts.visualBoard.exportImage') }}</button>
      <button v-if="canManage && !isRealtime && isOpen" data-stage-primary-action type="button" :disabled="dirty || saving || (isManualV2Open && !manualLayoutComplete)" @click="emit('finalize')">{{ t('drafts.visualBoard.finalize') }}</button>
    </div>
    <p v-if="canManage && isManualV2Open && !manualLayoutComplete" data-manual-incomplete class="profile-inline-message">{{ t('drafts.visualBoard.manualIncomplete') }}</p>

    <section v-if="hasActiveTurn" class="draft-turn-clock" data-active-turn :aria-label="t('drafts.realtime.turnClock')">
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
        <span :style="{ transform: `scaleX(${turnProgress / 100})` }" />
      </div>
    </section>
    <p v-if="isRealtime" class="sr-only" data-realtime-announcement role="status" aria-live="polite" aria-atomic="true">{{ realtimeAnnouncement }}</p>
    <p class="sr-only" data-move-announcement role="status" aria-live="polite" aria-atomic="true">{{ moveAnnouncement }}</p>

    <section v-if="!isManualV2" class="draft-pick-overview" :aria-label="t('drafts.visualBoard.pickSequence')">
      <header>
        <span class="eyebrow">{{ t('drafts.pickHistory.title') }}</span>
        <strong data-pick-progress>{{ t('drafts.visualBoard.pickProgress', { current: completedPicks, total: totalPicks }) }}</strong>
      </header>
      <ol v-if="presentedChoices.length" data-pick-sequence-list :aria-label="t('drafts.visualBoard.pickSequence')">
        <li v-for="item in presentedChoices" :key="item.key" data-pick-sequence class="draft-pick-card">
          <strong data-pick-sequence-number class="draft-pick-card__number">#{{ item.choice.sequencia }}</strong>
          <span class="draft-pick-card__copy">
            <strong data-pick-player>{{ choiceName(item.choice) }}</strong>
            <small data-pick-team-order>
              {{ t('drafts.visualBoard.teamPickOrder', {
                team: item.teamName ?? t('drafts.visualBoard.unknownTeam'),
                order: item.teamPickOrder,
              }) }}
            </small>
          </span>
        </li>
      </ol>
      <p v-else>{{ t('drafts.pickHistory.empty') }}</p>
    </section>

    <div id="draft-visual-capture" class="draft-visual-board draft-board">
      <aside class="draft-visual-team-column" :aria-label="t('drafts.visualBoard.oddTeams')">
        <article v-for="time in leftTeams" :key="time.id" class="draft-team draft-visual-team" :class="[teamColorClass(time.cor), { 'draft-visual-team--turn': isTurnTeam(time.id) }]" :data-team-id="time.id" @dragover.prevent @drop="movePlayer(time.id)">
          <header class="draft-team__header">
            <input
              v-if="!isTerminal"
              v-model="time.nome"
              :name="`draft-team-${time.id}`"
              autocomplete="off"
              :aria-label="t('drafts.visualBoard.teamNameLabel', { name: time.nome })"
              :disabled="isReadOnly"
              @input="dirty = true"
            />
            <strong v-else>{{ time.nome }}</strong>
            <span v-if="!isManualV2" data-team-order>{{ t('drafts.visualBoard.teamOrder', { order: time.ordem }) }}</span>
            <span>{{ time.jogadores.length }} / {{ localMontagem.tamanhoEquipe }}<template v-if="!isManualV2"><br /><span data-team-captain>{{ t('drafts.board.captain', { name: captainName(time) }) }}</span></template></span>
          </header>
          <ul class="draft-slots">
            <li v-for="player in time.jogadores" :key="player.jogadorId" class="draft-slot draft-visual-slot" :class="{ 'is-captain': !isManualV2 && player.jogadorId === time.capitaoId }" :data-player-id="player.jogadorId" :draggable="!isReadOnly" @dragstart="dragged = { jogadorId: player.jogadorId }" @dragend="dragged = null">
              <button type="button" class="draft-player-details" data-player-details :aria-label="detailsLabel(player)" @click="detailsPlayer = player" @keydown.stop>
                <span class="draft-slot__avatar" aria-hidden="true">{{ player.nomeExibicao.charAt(0) }}</span>
                <span class="draft-slot__copy">
                  <strong>{{ player.nomeExibicao }}</strong>
                  <small v-if="!isManualV2">{{ participantRoleLabel(player.jogadorId === time.capitaoId) }}</small>
                  <small>{{ eloSummary(player) }}</small>
                  <span class="draft-visual-routes">
                    <strong>{{ primaryRoute(player) }}</strong>
                    <small v-if="secondaryRoute(player)">{{ secondaryRoute(player) }}</small>
                  </span>
                </span>
              </button>
              <span v-if="!isManualV2 && player.jogadorId === time.capitaoId" class="draft-slot__captain">{{ t('drafts.roles.captainShort') }}</span>
              <button v-if="canSubstitute && localMontagem.reservas.length" type="button" class="button-secondary draft-substitute-action" :disabled="saving || substituteLocked" @click.stop="requestSubstitution(time.id, player.jogadorId, $event)" @keydown.stop>{{ t('drafts.realtime.substitute') }}</button>
              <select v-if="!isReadOnly" data-move-destination :name="`draft-move-${player.jogadorId}`" autocomplete="off" :aria-label="moveDestinationLabel(player)" @change="moveFromControl(player, $event)" @keydown.stop>
                <option value="">{{ t('drafts.visualBoard.moveDestinationOption') }}</option>
                <option value="livres">{{ t('drafts.visualBoard.moveToFree') }}</option>
                <option value="reservas">{{ t('drafts.visualBoard.moveToReserves') }}</option>
                <option v-for="destination in orderedTeams" :key="destination.id" :value="destination.id">{{ destination.nome }}</option>
              </select>
            </li>
            <li v-for="index in Math.max(localMontagem.tamanhoEquipe - time.jogadores.length, 0)" :key="`${time.id}-empty-${index}`" class="draft-slot draft-slot--empty">
              <span>{{ t('drafts.board.emptySlot') }}</span>
            </li>
          </ul>
        </article>
      </aside>

      <article class="draft-available draft-visual-pool" data-available-pool @dragover.prevent @drop="movePlayer('livres')">
        <span class="draft-available__glow" aria-hidden="true" />
        <header class="draft-available__filters">
          <label class="draft-search-field">
            <span aria-hidden="true">⌕</span>
            <input
              v-model="playerSearch"
              type="search"
              name="draft-player-search"
              autocomplete="off"
              :aria-label="t('drafts.visualBoard.playerSearchLabel')"
              :placeholder="t('drafts.visualBoard.searchPlaceholder')"
            />
          </label>
          <div class="draft-route-filters" :aria-label="t('drafts.visualBoard.displayedRoutes')">
            <button v-for="route in routeFilters" :key="route" type="button" :class="{ 'is-active': selectedRoute === route }" :aria-pressed="selectedRoute === route" @click="selectedRoute = route">
              {{ routeFilterLabel(route) }}
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
        <div class="draft-player-grid">
          <div class="draft-player-row draft-player-row--head">
            <span>{{ t('drafts.board.player') }}</span>
          </div>
          <ul data-available-player-list class="draft-player-list" role="list" :aria-label="t('drafts.board.availablePlayers')">
            <li v-for="player in filteredAvailablePlayers" :key="player.jogadorId" class="draft-player-row draft-visual-player-row" :data-player-id="player.jogadorId" :draggable="!isReadOnly && player.estado === DraftMontagemEstadoValues.Livre" @dragstart="dragged = { jogadorId: player.jogadorId }" @dragend="dragged = null">
              <button type="button" class="draft-player-row__identity draft-player-details" data-player-details :aria-label="detailsLabel(player)" @click="detailsPlayer = player" @keydown.stop>
                <span class="draft-slot__avatar" aria-hidden="true">{{ player.nomeExibicao.charAt(0) }}</span>
                <span class="draft-slot__copy">
                  <strong>
                    {{ player.nomeExibicao }}
                    <span v-if="player.estado === DraftMontagemEstadoValues.Reserva" class="draft-visual-reserve-badge">{{ t('drafts.visualBoard.reserve') }}</span>
                  </strong>
                  <small>{{ eloSummary(player) }}</small>
                  <span class="draft-visual-routes">
                    <strong>{{ primaryRoute(player) }}</strong>
                    <small v-if="secondaryRoute(player)">{{ secondaryRoute(player) }}</small>
                  </span>
                </span>
              </button>
              <button v-if="canPickPlayer(player)" data-stage-primary-action type="button" class="draft-pick-action" @click.stop="pickPlayer(player)" @keydown.stop>{{ t('drafts.realtime.pick') }}</button>
              <span v-else-if="isRealtime && player.estado === DraftMontagemEstadoValues.Reserva" class="draft-visual-reserve-badge">{{ t('drafts.realtime.emergencyReserve') }}</span>
              <select v-if="!isReadOnly" data-move-destination :name="`draft-move-${player.jogadorId}`" autocomplete="off" :aria-label="moveDestinationLabel(player)" @change="moveFromControl(player, $event)" @keydown.stop>
                <option value="">{{ t('drafts.visualBoard.moveDestinationOption') }}</option>
                <option value="livres">{{ t('drafts.visualBoard.moveToFree') }}</option>
                <option value="reservas">{{ t('drafts.visualBoard.moveToReserves') }}</option>
                <option v-for="destination in orderedTeams" :key="destination.id" :value="destination.id">{{ destination.nome }}</option>
              </select>
            </li>
          </ul>
        </div>
        <p v-if="!filteredAvailablePlayers.length" class="empty-copy">{{ t('drafts.visualBoard.noPlayersForFilter') }}</p>
      </article>

      <aside class="draft-visual-team-column" :aria-label="t('drafts.visualBoard.evenTeams')">
        <article v-for="time in rightTeams" :key="time.id" class="draft-team draft-visual-team" :class="[teamColorClass(time.cor), { 'draft-visual-team--turn': isTurnTeam(time.id) }]" :data-team-id="time.id" @dragover.prevent @drop="movePlayer(time.id)">
          <header class="draft-team__header">
            <input
              v-if="!isTerminal"
              v-model="time.nome"
              :name="`draft-team-${time.id}`"
              autocomplete="off"
              :aria-label="t('drafts.visualBoard.teamNameLabel', { name: time.nome })"
              :disabled="isReadOnly"
              @input="dirty = true"
            />
            <strong v-else>{{ time.nome }}</strong>
            <span v-if="!isManualV2" data-team-order>{{ t('drafts.visualBoard.teamOrder', { order: time.ordem }) }}</span>
            <span>{{ time.jogadores.length }} / {{ localMontagem.tamanhoEquipe }}<template v-if="!isManualV2"><br /><span data-team-captain>{{ t('drafts.board.captain', { name: captainName(time) }) }}</span></template></span>
          </header>
          <ul class="draft-slots">
            <li v-for="player in time.jogadores" :key="player.jogadorId" class="draft-slot draft-visual-slot" :class="{ 'is-captain': !isManualV2 && player.jogadorId === time.capitaoId }" :data-player-id="player.jogadorId" :draggable="!isReadOnly" @dragstart="dragged = { jogadorId: player.jogadorId }" @dragend="dragged = null">
              <button type="button" class="draft-player-details" data-player-details :aria-label="detailsLabel(player)" @click="detailsPlayer = player" @keydown.stop>
                <span class="draft-slot__avatar" aria-hidden="true">{{ player.nomeExibicao.charAt(0) }}</span>
                <span class="draft-slot__copy">
                  <strong>{{ player.nomeExibicao }}</strong>
                  <small v-if="!isManualV2">{{ participantRoleLabel(player.jogadorId === time.capitaoId) }}</small>
                  <small>{{ eloSummary(player) }}</small>
                  <span class="draft-visual-routes">
                    <strong>{{ primaryRoute(player) }}</strong>
                    <small v-if="secondaryRoute(player)">{{ secondaryRoute(player) }}</small>
                  </span>
                </span>
              </button>
              <span v-if="!isManualV2 && player.jogadorId === time.capitaoId" class="draft-slot__captain">{{ t('drafts.roles.captainShort') }}</span>
              <button v-if="canSubstitute && localMontagem.reservas.length" type="button" class="button-secondary draft-substitute-action" :disabled="saving || substituteLocked" @click.stop="requestSubstitution(time.id, player.jogadorId, $event)" @keydown.stop>{{ t('drafts.realtime.substitute') }}</button>
              <select v-if="!isReadOnly" data-move-destination :name="`draft-move-${player.jogadorId}`" autocomplete="off" :aria-label="moveDestinationLabel(player)" @change="moveFromControl(player, $event)" @keydown.stop>
                <option value="">{{ t('drafts.visualBoard.moveDestinationOption') }}</option>
                <option value="livres">{{ t('drafts.visualBoard.moveToFree') }}</option>
                <option value="reservas">{{ t('drafts.visualBoard.moveToReserves') }}</option>
                <option v-for="destination in orderedTeams" :key="destination.id" :value="destination.id">{{ destination.nome }}</option>
              </select>
            </li>
            <li v-for="index in Math.max(localMontagem.tamanhoEquipe - time.jogadores.length, 0)" :key="`${time.id}-empty-${index}`" class="draft-slot draft-slot--empty">
              <span>{{ t('drafts.board.emptySlot') }}</span>
            </li>
          </ul>
        </article>
      </aside>
    </div>

    <PlayerDetailsDrawer :player="detailsPlayer" @close="detailsPlayer = null" />
    <DraftSubstitutionDialog
      v-if="substitutionContext && substitutionTeam && substitutionPlayer"
      :open="true"
      :team="substitutionTeam"
      :outgoing-player="substitutionPlayer"
      :reserves="localMontagem.reservas"
      :eligible-captain-ids="eligibleCaptainIds"
      :requires-new-captain="localMontagem.cicloVersao === 'ModoPosPresenca' && (substitutionPlayer.capitao || substitutionTeam.capitaoId === substitutionPlayer.jogadorId)"
      :saving="saving"
      @confirm="confirmSubstitution"
      @cancel="cancelSubstitution"
      @restore-focus="restoreSubstitutionFocus"
    />
  </section>
</template>
