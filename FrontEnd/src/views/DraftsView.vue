<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute } from 'vue-router'

import DraftReasonDialog, { type DraftReasonDialogAction } from '@/components/drafts/DraftReasonDialog.vue'
import DraftWorkspaceHeader from '@/components/drafts/DraftWorkspaceHeader.vue'
import DraftVisualBoard from '@/components/drafts/visual/DraftVisualBoard.vue'
import DraftVisualSetup from '@/components/drafts/visual/DraftVisualSetup.vue'
import PageFrame from '@/components/layout/PageFrame.vue'
import PageHeader from '@/components/layout/PageHeader.vue'
import PendingPlayerProfileNotice from '@/components/users/PendingPlayerProfileNotice.vue'
import { Button } from '@/components/ui/button'
import { DRAFT_MONTAGEM_STATUS_OPTIONS } from '@/constants/draftMontagemStatus'
import { Permissions } from '@/constants/permissions'
import { useAuthState } from '@/services/authState'
import { listEligibleCaptains, listPlayers, type Player } from '@/services/players'
import {
  addManualDraftMontagemPresence,
  cancelDraftMontagem,
  cancelDraftMontagemPresence,
  closeDraftMontagemPresence,
  confirmDraftMontagemPresence,
  createDraftMontagem,
  defineDraftMontagemCaptains,
  defineDraftMontagemPickOrder,
  DraftMontagemServiceError,
  drawDraftMontagemCaptains,
  finalizeDraftMontagem,
  getDraftMontagemById,
  getDraftMontagemAdminById,
  getDraftMontagemRealtimeState,
  listEligibleManualPresencePlayers,
  listDraftMontagens,
  registerDraftMontagemPick,
  removeManualDraftMontagemPresence,
  republishDraftMontagemDiscordPublication,
  saveDraftMontagemLayout,
  startDraftMontagemRealtime,
  substituteDraftMontagemReserve,
} from '@/services/draftMontagens'
import { DraftMontagemRealtimeConnection } from '@/services/draftMontagemRealtime'
import { resolveInitialDraftId } from '@/services/draftRoute'
import { DraftMontagemOrdemEscolhaModoValues, DraftMontagemPresencaStatusValues, DraftMontagemStatusValues } from '@/constants/draftMontagem'
import type { DraftMontagem, DraftMontagemAdmin, DraftMontagemLayoutPayload, DraftMontagemPayload, DraftMontagemResumo, DraftMontagemStatus } from '@/types/draftMontagem'

const players = ref<Player[]>([])
const { t, locale } = useI18n()
const route = useRoute()
const auth = useAuthState()
const captains = ref<Player[]>([])
const loading = ref(true)
const saving = ref(false)
const errors = ref<string[]>([])
const serviceErrors = ref<string[]>([])
const notification = ref<string | null>(null)
const visualSetupOpen = ref(false)
const searchTerm = ref('')
const selectedStatus = ref<DraftMontagemStatus | ''>('')
const selectedMontagem = ref<DraftMontagem | null>(null)
const visualMontagens = ref<DraftMontagemResumo[]>([])
const realtimeConnection = ref<DraftMontagemRealtimeConnection | null>(null)
const selectedManualPresencePlayerId = ref('')
const manualPresenceSearch = ref('')
const manualPresencePlayers = ref<Pick<Player, 'id' | 'nomeExibicao'>[]>([])
const pendingReasonAction = ref<DraftReasonDialogAction | null>(null)
const adminAccessDenied = ref(false)
let detailRequestVersion = 0
let manualPresenceRequestVersion = 0
let manualPresenceAbortController: AbortController | null = null
let activeDraftId: string | null = null
let activeDraftGeneration = 0

interface DraftUpdateContext {
  draftId: string
  generation: number
  requestVersion: number
}

const captainSelection = ref<string[]>([])
const statusOptions = DRAFT_MONTAGEM_STATUS_OPTIONS
const hasDraftManagementPermission = computed(() => auth.hasPermission(Permissions.CanManageDrafts))
const canManageDrafts = computed(() => hasDraftManagementPermission.value && !adminAccessDenied.value)
const currentUserId = computed(() => auth.user.value?.id ?? null)
const currentAuthPlayerId = computed(() => auth.user.value?.jogadorId ?? null)
const myPresence = computed(
  () =>
    selectedMontagem.value?.presencas.find(
      (presence) => presence.status === DraftMontagemPresencaStatusValues.Confirmada && (presence.usuarioId === currentUserId.value || presence.jogadorId === currentAuthPlayerId.value),
    ) ?? null,
)
const currentPlayerId = computed(() => currentAuthPlayerId.value ?? myPresence.value?.jogadorId ?? null)
const hasPlayerProfile = computed(() => Boolean(currentPlayerId.value))
const confirmedPresences = computed(() => selectedMontagem.value?.presencas.filter((presence) => presence.status === DraftMontagemPresencaStatusValues.Confirmada) ?? [])
const availableManualPresencePlayers = computed(() => {
  const confirmed = new Set(confirmedPresences.value.map((presence) => presence.jogadorId))
  return manualPresencePlayers.value.filter((player) => !confirmed.has(player.id))
})
const finalTeamsPublicationStatus = computed(() => selectedMontagem.value?.publicacoesDiscord?.find((publication) => publication.tipo === 'TimesDefinidos')?.status ?? null)

const filteredDrafts = computed(() => {
  const search = searchTerm.value.trim().toLowerCase()
  return visualMontagens.value.filter((draft) => {
    const matchesStatus = !selectedStatus.value || draft.status === selectedStatus.value
    const matchesSearch = !search || draft.nome.toLowerCase().includes(search)
    return matchesStatus && matchesSearch
  })
})

onMounted(async () => {
  await Promise.all([loadPlayers(), loadCaptains(), loadVisualMontagens()])
})

onUnmounted(async () => {
  activeDraftId = null
  activeDraftGeneration++
  detailRequestVersion = 0
  manualPresenceAbortController?.abort()
  await disconnectRealtime()
})

async function loadPlayers() {
  try {
    players.value = await listPlayers(true)
  } catch {
    players.value = []
  }
}

async function loadCaptains() {
  captains.value = await listEligibleCaptains()
}

async function loadVisualMontagens() {
  loading.value = true
  try {
    visualMontagens.value = await listDraftMontagens({ status: selectedStatus.value })
    if (!selectedMontagem.value) {
      const initialDraftId = resolveInitialDraftId(route.query.draftId)
      if (initialDraftId) {
        await openMontagemFromLink(initialDraftId)
        return
      }

      if (visualMontagens.value[0]) {
        await openMontagem(visualMontagens.value[0].id)
      }
    }
  } catch (error) {
    captureError(error)
  } finally {
    loading.value = false
  }
}

async function openMontagemFromLink(id: string) {
  await openMontagem(id)
  if (!selectedMontagem.value) {
    errors.value = [t('drafts.errors.linkNotFound')]
  }
}

async function openMontagem(id: string, publicProjection?: DraftMontagem) {
  const generation = ++activeDraftGeneration
  activeDraftId = id
  detailRequestVersion = 0
  manualPresenceAbortController?.abort()
  manualPresenceAbortController = null
  manualPresenceRequestVersion++
  selectedMontagem.value = null
  pendingReasonAction.value = null
  captainSelection.value = []
  manualPresencePlayers.value = []
  selectedManualPresencePlayerId.value = ''
  const disconnecting = disconnectRealtime()
  saving.value = true
  errors.value = []
  try {
    await disconnecting
    if (!isActiveDraft(id, generation)) return
    if (!(await refreshMontagemDetail(id, generation, publicProjection))) return
    captainSelection.value = []
    await loadEligibleManualPresencePlayers()
    if (!isActiveDraft(id, generation)) return
    await connectRealtime(id, generation)
  } catch (error) {
    if (isActiveDraft(id, generation)) captureError(error)
  } finally {
    if (isActiveDraft(id, generation)) saving.value = false
  }
}

function isActiveDraft(id: string, generation: number) {
  return activeDraftId === id && activeDraftGeneration === generation
}

function beginDraftUpdate(id: string, generation = activeDraftGeneration): DraftUpdateContext | null {
  if (!isActiveDraft(id, generation)) return null
  return { draftId: id, generation, requestVersion: ++detailRequestVersion }
}

function isCurrentUpdate(context: DraftUpdateContext) {
  return isActiveDraft(context.draftId, context.generation) && detailRequestVersion === context.requestVersion
}

function beginSelectedDraftUpdate() {
  return selectedMontagem.value ? beginDraftUpdate(selectedMontagem.value.id) : null
}

async function applyMutationProjection(context: DraftUpdateContext, montagem: DraftMontagem) {
  if (!isCurrentUpdate(context) || montagem.id !== context.draftId) return false

  void refreshMontagemDetail(context.draftId, context.generation, montagem, context).catch(() => {
    // The public mutation response is authoritative; administrative enrichment is best-effort.
  })
  return true
}

async function refreshMontagemDetail(id: string, generation: number, publicProjection?: DraftMontagem, existingContext?: DraftUpdateContext) {
  const context = existingContext ?? beginDraftUpdate(id, generation)
  if (!context || !isCurrentUpdate(context)) return false
  let detail = publicProjection

  if (publicProjection) {
    applyPublicMontagemState(publicProjection)
  }

  if (hasDraftManagementPermission.value && !adminAccessDenied.value) {
    try {
      detail = await getDraftMontagemAdminById(id)
    } catch (error) {
      if (!isCurrentUpdate(context)) return false
      if (!(error instanceof DraftMontagemServiceError) || error.status !== 403) {
        if (publicProjection) return true
        throw error
      }

      adminAccessDenied.value = true
      detail = publicProjection ?? (await getDraftMontagemById(id))
      if (!isCurrentUpdate(context)) return false
      applyMontagemState(detail)
      return true
    }
  } else if (!detail) {
    detail = await getDraftMontagemById(id)
  }

  if (!isCurrentUpdate(context) || detail.id !== id) return false
  applyMontagemState(detail)
  return true
}

function applyPublicMontagemState(montagem: DraftMontagem) {
  const current = selectedMontagem.value
  if (!current || current.id !== montagem.id) {
    applyMontagemState(montagem)
    return
  }

  const currentAdmin = current as DraftMontagemAdmin
  const merged = {
    ...current,
    ...montagem,
    presencas: montagem.presencas.map((presence) => ({ ...currentAdmin.presencas?.find((item) => item.id === presence.id), ...presence })),
    substituicoes: montagem.substituicoes.map((substitution) => ({
      ...currentAdmin.substituicoes?.find((item) => item.timeId === substitution.timeId && item.jogadorSaiuId === substitution.jogadorSaiuId && item.reservaEntrouId === substitution.reservaEntrouId),
      ...substitution,
    })),
    publicacoesDiscord: montagem.publicacoesDiscord?.map((publication) => ({ ...currentAdmin.publicacoesDiscord?.find((item) => item.tipo === publication.tipo), ...publication })),
  }
  applyMontagemState(merged)
}

async function loadEligibleManualPresencePlayers() {
  const draftId = selectedMontagem.value?.id
  const generation = activeDraftGeneration
  const search = manualPresenceSearch.value
  const requestVersion = ++manualPresenceRequestVersion
  manualPresenceAbortController?.abort()
  if (!draftId || !canManageDrafts.value) {
    manualPresenceAbortController = null
    manualPresencePlayers.value = []
    return
  }

  const controller = new AbortController()
  manualPresenceAbortController = controller
  try {
    const players = await listEligibleManualPresencePlayers(draftId, search, 1, 20, controller.signal)
    if (isActiveDraft(draftId, generation)
      && manualPresenceRequestVersion === requestVersion
      && manualPresenceSearch.value === search) {
      manualPresencePlayers.value = players
    }
  } catch (error) {
    if (!controller.signal.aborted) throw error
  } finally {
    if (manualPresenceAbortController === controller) manualPresenceAbortController = null
  }
}

async function confirmPresence() {
  const context = beginSelectedDraftUpdate()
  if (!context) return
  saving.value = true
  try {
    const montagem = await confirmDraftMontagemPresence(context.draftId)
    if (await applyMutationProjection(context, montagem)) notification.value = t('drafts.presence.confirmed')
  } catch (error) {
    if (isActiveDraft(context.draftId, context.generation)) captureError(error)
  } finally {
    if (isActiveDraft(context.draftId, context.generation)) saving.value = false
  }
}

async function cancelPresence() {
  const context = beginSelectedDraftUpdate()
  if (!context) return
  saving.value = true
  try {
    const montagem = await cancelDraftMontagemPresence(context.draftId)
    if (await applyMutationProjection(context, montagem)) notification.value = t('drafts.presence.cancelled')
  } catch (error) {
    if (isActiveDraft(context.draftId, context.generation)) captureError(error)
  } finally {
    if (isActiveDraft(context.draftId, context.generation)) saving.value = false
  }
}

async function addManualPresence() {
  if (!selectedMontagem.value || !canManageDrafts.value || !selectedManualPresencePlayerId.value) return
  const player = availableManualPresencePlayers.value.find((item) => item.id === selectedManualPresencePlayerId.value)
  if (!player) return

  pendingReasonAction.value = { type: 'addManualPresence', jogadorId: player.id, jogadorNome: player.nomeExibicao }
}

function requestManualPresenceRemoval(jogadorId: string, jogadorNome: string) {
  if (selectedMontagem.value && canManageDrafts.value) {
    pendingReasonAction.value = { type: 'removeManualPresence', jogadorId, jogadorNome }
  }
}

async function closePresence(continueWithLess = false) {
  const montagemAtual = selectedMontagem.value
  const context = beginSelectedDraftUpdate()
  if (!montagemAtual || !context || !canManageDrafts.value) return
  saving.value = true
  try {
    const montagem = await closeDraftMontagemPresence(context.draftId, continueWithLess, montagemAtual.tamanhoEquipe)
    if (await applyMutationProjection(context, montagem)) notification.value = t('drafts.presence.closed')
  } catch (error) {
    if (isActiveDraft(context.draftId, context.generation)) captureError(error)
  } finally {
    if (isActiveDraft(context.draftId, context.generation)) saving.value = false
  }
}

function toggleCaptainSelection(jogadorId: string) {
  if (captainSelection.value.includes(jogadorId)) {
    captainSelection.value = captainSelection.value.filter((id) => id !== jogadorId)
    return
  }
  if (!selectedMontagem.value || captainSelection.value.length >= selectedMontagem.value.quantidadeTimes) return
  captainSelection.value = [...captainSelection.value, jogadorId]
}

async function defineCaptains() {
  const context = beginSelectedDraftUpdate()
  if (!context || !canManageDrafts.value) return
  saving.value = true
  try {
    const montagem = await defineDraftMontagemCaptains(context.draftId, captainSelection.value)
    if (await applyMutationProjection(context, montagem)) notification.value = t('drafts.presence.captainsDefined')
  } catch (error) {
    if (isActiveDraft(context.draftId, context.generation)) captureError(error)
  } finally {
    if (isActiveDraft(context.draftId, context.generation)) saving.value = false
  }
}

async function drawPickOrder() {
  const context = beginSelectedDraftUpdate()
  if (!context || !canManageDrafts.value) return
  saving.value = true
  try {
    const montagem = await defineDraftMontagemPickOrder(context.draftId, DraftMontagemOrdemEscolhaModoValues.Sorteado)
    if (await applyMutationProjection(context, montagem)) notification.value = t('drafts.presence.orderDefined')
  } catch (error) {
    if (isActiveDraft(context.draftId, context.generation)) captureError(error)
  } finally {
    if (isActiveDraft(context.draftId, context.generation)) saving.value = false
  }
}

async function connectRealtime(id: string, generation: number) {
  if (!isActiveDraft(id, generation)) return
  try {
    const state = await getDraftMontagemRealtimeState(id)
    if (!isActiveDraft(id, generation)) return
    await refreshMontagemDetail(id, generation, state.montagem)
  } catch {
    // The regular detail endpoint already loaded the board; realtime state errors are shown by later actions.
  }

  if (!isActiveDraft(id, generation)) return
  const connection = new DraftMontagemRealtimeConnection(id)
  realtimeConnection.value = connection
  await connection.connect(
    async (state) => {
      if (!isActiveDraft(id, generation)) return
      try {
        await refreshMontagemDetail(id, generation, state.montagem)
      } catch {
        // Keep the last complete administrative projection if its refresh fails.
      }
    },
    async () => {
      if (!isActiveDraft(id, generation)) return
      const state = await getDraftMontagemRealtimeState(id)
      await refreshMontagemDetail(id, generation, state.montagem)
    },
  )
  if (!isActiveDraft(id, generation)) {
    if (realtimeConnection.value === connection) realtimeConnection.value = null
    await connection.disconnect()
  }
}

function applyMontagemState(montagem: DraftMontagem) {
  selectedMontagem.value = montagem
  visualMontagens.value = visualMontagens.value.map((item) =>
    item.id === montagem.id
      ? {
          ...item,
          status: montagem.status,
          modo: montagem.modo,
          quantidadeTimes: montagem.quantidadeTimes,
          quantidadeReservas: montagem.quantidadeReservas,
          dataAtualizacao: montagem.dataAtualizacao,
        }
      : item,
  )
}

async function disconnectRealtime() {
  const connection = realtimeConnection.value
  realtimeConnection.value = null
  await connection?.disconnect()
}

async function saveMontagem(payload: DraftMontagemPayload) {
  if (!canManageDrafts.value) {
    return
  }

  saving.value = true
  serviceErrors.value = []
  try {
    const montagem = await createDraftMontagem(payload)
    await openMontagem(montagem.id, montagem)
    await loadVisualMontagens()
    notification.value = t('drafts.created', { name: montagem.nome })
    visualSetupOpen.value = false
  } catch (error) {
    serviceErrors.value = error instanceof DraftMontagemServiceError ? error.errors : [t('drafts.errors.create')]
  } finally {
    saving.value = false
  }
}

async function saveMontagemLayout(payload: DraftMontagemLayoutPayload) {
  const context = beginSelectedDraftUpdate()
  if (!context || !canManageDrafts.value) return
  saving.value = true
  errors.value = []
  try {
    const montagem = await saveDraftMontagemLayout(context.draftId, payload)
    if (!(await applyMutationProjection(context, montagem))) return
    await loadVisualMontagens()
    notification.value = t('drafts.messages.layoutSaved')
  } catch (error) {
    if (isActiveDraft(context.draftId, context.generation)) captureError(error)
  } finally {
    if (isActiveDraft(context.draftId, context.generation)) saving.value = false
  }
}

async function startRealtime() {
  const context = beginSelectedDraftUpdate()
  if (!context || !canManageDrafts.value) return

  saving.value = true
  errors.value = []
  try {
    const state = await startDraftMontagemRealtime(context.draftId)
    if (await applyMutationProjection(context, state.montagem)) notification.value = t('drafts.realtime.started')
  } catch (error) {
    if (isActiveDraft(context.draftId, context.generation)) captureError(error)
  } finally {
    if (isActiveDraft(context.draftId, context.generation)) saving.value = false
  }
}

async function pickRealtime(jogadorId: string) {
  const context = beginSelectedDraftUpdate()
  if (!context) return

  saving.value = true
  errors.value = []
  try {
    const state = await registerDraftMontagemPick(context.draftId, jogadorId)
    await applyMutationProjection(context, state.montagem)
  } catch (error) {
    if (isActiveDraft(context.draftId, context.generation)) captureError(error)
  } finally {
    if (isActiveDraft(context.draftId, context.generation)) saving.value = false
  }
}

async function substituteReserve(payload: { timeId: string; jogadorSaiuId: string; reservaEntrouId: string; motivo?: string | null }) {
  const context = beginSelectedDraftUpdate()
  if (!context || !canManageDrafts.value) return

  saving.value = true
  errors.value = []
  try {
    const state = await substituteDraftMontagemReserve(context.draftId, payload)
    if (await applyMutationProjection(context, state.montagem)) notification.value = t('drafts.realtime.reserveSubstituted')
  } catch (error) {
    if (isActiveDraft(context.draftId, context.generation)) captureError(error)
  } finally {
    if (isActiveDraft(context.draftId, context.generation)) saving.value = false
  }
}

async function drawMontagemCaptains() {
  const context = beginSelectedDraftUpdate()
  if (!context || !canManageDrafts.value) return
  saving.value = true
  try {
    const montagem = await drawDraftMontagemCaptains(context.draftId)
    if (await applyMutationProjection(context, montagem)) notification.value = t('drafts.messages.captainsDrawn')
  } catch (error) {
    if (isActiveDraft(context.draftId, context.generation)) captureError(error)
  } finally {
    if (isActiveDraft(context.draftId, context.generation)) saving.value = false
  }
}

async function finalizeMontagem() {
  const context = beginSelectedDraftUpdate()
  if (!context || !canManageDrafts.value) return
  saving.value = true
  try {
    const montagem = await finalizeDraftMontagem(context.draftId)
    if (!(await applyMutationProjection(context, montagem))) return
    await loadVisualMontagens()
    notification.value = t('drafts.messages.finished')
  } catch (error) {
    if (isActiveDraft(context.draftId, context.generation)) captureError(error)
  } finally {
    if (isActiveDraft(context.draftId, context.generation)) saving.value = false
  }
}

function requestDraftCancellation() {
  if (selectedMontagem.value && canManageDrafts.value) {
    pendingReasonAction.value = { type: 'cancelDraft' }
  }
}

function resetFilters() {
  searchTerm.value = ''
  selectedStatus.value = ''
  void loadVisualMontagens()
}

function formatRinhaDate(value?: string | null) {
  if (!value) return t('drafts.noRinhaDate')
  return new Date(value).toLocaleDateString(locale.value, { day: '2-digit', month: '2-digit', year: 'numeric' })
}

function formatPresenceOrigin(origin: string) {
  return t(`drafts.presenceOrigin.${origin}`)
}

function publicationStatus(tipo: 'Presenca' | 'ChamadaPresenca' | 'TimesDefinidos') {
  return selectedMontagem.value?.publicacoesDiscord?.find((publication) => publication.tipo === tipo)?.status ?? 'Pendente'
}

function requestDiscordRepublish(tipo: 'Presenca' | 'ChamadaPresenca' | 'TimesDefinidos') {
  if (!selectedMontagem.value || !canManageDrafts.value) return
  const actions: Record<typeof tipo, DraftReasonDialogAction> = {
    Presenca: { type: 'republishPresence', publicationStatus: publicationStatus('Presenca') },
    ChamadaPresenca: { type: 'republishPresenceCta', publicationStatus: publicationStatus('ChamadaPresenca') },
    TimesDefinidos: { type: 'republishTeams', publicationStatus: publicationStatus('TimesDefinidos') },
  }
  pendingReasonAction.value = actions[tipo]
}

async function confirmReasonAction(reason: string) {
  if (saving.value) return

  const action = pendingReasonAction.value
  if (!action || !selectedMontagem.value || !canManageDrafts.value) return
  const context = beginSelectedDraftUpdate()
  if (!context) return

  saving.value = true
  try {
    if (action.type === 'cancelDraft') {
      const montagem = await cancelDraftMontagem(context.draftId, reason)
      if (!(await applyMutationProjection(context, montagem))) return
      await loadVisualMontagens()
      notification.value = t('drafts.canceled', { name: montagem.nome })
    } else if (action.type === 'addManualPresence') {
      const montagem = await addManualDraftMontagemPresence(context.draftId, action.jogadorId, reason)
      if (!(await applyMutationProjection(context, montagem))) return
      selectedManualPresencePlayerId.value = ''
      await loadEligibleManualPresencePlayers()
      notification.value = t('drafts.presence.manualAdded')
    } else if (action.type === 'removeManualPresence') {
      const montagem = await removeManualDraftMontagemPresence(context.draftId, action.jogadorId, reason)
      if (!(await applyMutationProjection(context, montagem))) return
      await loadEligibleManualPresencePlayers()
      notification.value = t('drafts.presence.manualRemoved')
    } else {
      const tipo = action.type === 'republishPresence'
        ? 'Presenca'
        : action.type === 'republishPresenceCta'
          ? 'ChamadaPresenca'
          : 'TimesDefinidos'
      const montagem = await republishDraftMontagemDiscordPublication(context.draftId, tipo, reason)
      if (!(await applyMutationProjection(context, montagem))) return
      notification.value = t('drafts.publication.republishRequested')
    }
    pendingReasonAction.value = null
  } catch (error) {
    if (isActiveDraft(context.draftId, context.generation)) captureError(error)
  } finally {
    if (isActiveDraft(context.draftId, context.generation)) saving.value = false
  }
}

function captureError(error: unknown) {
  errors.value = error instanceof DraftMontagemServiceError ? error.errors : [t('drafts.errors.action')]
}
</script>

<template>
  <PageFrame class="players-page drafts-page" rail>
    <div v-if="notification" class="app-toast app-toast--success" role="status" aria-live="polite">
      <span class="app-toast__indicator" aria-hidden="true" />
      <p>{{ notification }}</p>
      <button type="button" :aria-label="t('common.closeNotification')" @click="notification = null">×</button>
    </div>

    <PageHeader :eyebrow="t('drafts.kicker')" :title="t('drafts.title')" :description="t('drafts.visualSubtitle')">
      <template #actions>
        <span class="page-hero__metric">{{ t('drafts.metrics.visible', { total: filteredDrafts.length }) }}</span>
        <Button v-if="canManageDrafts" type="button" @click="visualSetupOpen = true">{{ t('drafts.createWithIcon') }}</Button>
      </template>
    </PageHeader>

    <PendingPlayerProfileNotice v-if="!hasPlayerProfile" />

    <section class="filter-bar" :aria-label="t('drafts.filtersLabel')">
      <label class="filter-field filter-field--wide">
        {{ t('drafts.searchLabel') }}
        <span>
          <span aria-hidden="true">⌕</span>
          <input v-model="searchTerm" type="search" :placeholder="t('drafts.searchPlaceholder')" />
        </span>
      </label>
      <label class="filter-field">
        {{ t('common.status') }}
        <select v-model="selectedStatus" @change="loadVisualMontagens">
          <option value="">{{ t('common.all') }}</option>
          <option v-for="status in statusOptions" :key="status" :value="status">{{ t(`drafts.status.${status}`) }}</option>
        </select>
      </label>
      <button class="filter-reset" type="button" :aria-label="t('common.clearFilters')" @click="resetFilters">↺</button>
    </section>

    <div v-if="errors.length" class="form-errors" role="alert">
      <p v-for="error in errors" :key="error">{{ error }}</p>
    </div>

    <section class="draft-layout">
      <aside class="draft-list" :aria-label="t('drafts.listLabel')">
        <button
          v-for="draft in filteredDrafts"
          :key="draft.id"
          type="button"
          :class="{ 'is-selected': selectedMontagem?.id === draft.id }"
          @click="openMontagem(draft.id)"
        >
          <strong>{{ draft.nome }}</strong>
          <span class="team-status" :class="`team-status--${draft.status.toLowerCase()}`">{{ t(`drafts.status.${draft.status}`) }}</span>
          <span>{{ t('drafts.rinhaDate', { date: formatRinhaDate(draft.dataRinha ?? draft.horarioEncerramentoPresenca) }) }}</span>
        </button>
        <div v-if="!loading && !filteredDrafts.length" class="draft-empty-card">
          <h2>{{ t('drafts.emptyTitle') }}</h2>
          <p>{{ t('drafts.emptyDescription') }}</p>
        </div>
      </aside>

      <div class="draft-main">
        <DraftWorkspaceHeader
          v-if="selectedMontagem"
          :draft="selectedMontagem"
          :confirmed-count="confirmedPresences.length"
          :final-teams-publication-status="finalTeamsPublicationStatus"
        >
          <template #primary-action>
            <button v-if="selectedMontagem.status === DraftMontagemStatusValues.PresencaAberta && !myPresence" type="button" :disabled="saving" @click="confirmPresence">{{ t('drafts.presence.confirm') }}</button>
            <button v-else-if="canManageDrafts && selectedMontagem.status === DraftMontagemStatusValues.PresencaEncerrada" type="button" :disabled="saving || captainSelection.length !== selectedMontagem.quantidadeTimes" @click="defineCaptains">{{ t('drafts.presence.defineCaptains') }}</button>
            <button v-else-if="canManageDrafts && selectedMontagem.status === DraftMontagemStatusValues.CapitaesDefinidos" type="button" :disabled="saving" @click="drawPickOrder">{{ t('drafts.presence.drawOrder') }}</button>
          </template>
          <template #secondary-actions>
            <button v-if="selectedMontagem.status === DraftMontagemStatusValues.PresencaAberta && myPresence" type="button" class="button-secondary" :disabled="saving" @click="cancelPresence">{{ t('drafts.presence.cancel') }}</button>
            <button v-if="canManageDrafts && selectedMontagem.status === DraftMontagemStatusValues.PresencaAberta" type="button" class="button-secondary" :disabled="saving" @click="closePresence(false)">{{ t('drafts.presence.close') }}</button>
            <button v-if="canManageDrafts && selectedMontagem.status === DraftMontagemStatusValues.PresencaAberta && confirmedPresences.length < 10" type="button" class="button-secondary" :disabled="saving" @click="closePresence(true)">{{ t('drafts.presence.continueManual') }}</button>
          </template>
          <template #danger-action>
            <Button v-if="canManageDrafts && selectedMontagem.status !== DraftMontagemStatusValues.Finalizada && selectedMontagem.status !== DraftMontagemStatusValues.Cancelada" type="button" variant="destructive" :disabled="saving" @click="requestDraftCancellation">{{ t('common.cancel') }}</Button>
          </template>
        </DraftWorkspaceHeader>

        <section v-if="selectedMontagem" class="panel-card presence-panel">
          <p v-if="selectedMontagem.status === DraftMontagemStatusValues.PresencaAberta && confirmedPresences.length < 10" class="profile-inline-message">{{ t('drafts.presence.lessThanTen') }}</p>
          <div v-if="canManageDrafts" class="draft-hero-actions" :aria-label="t('drafts.publication.statusLabel')">
            <span class="team-status">{{ t('drafts.publication.presence', { status: t(`drafts.publication.status.${publicationStatus('Presenca')}`) }) }}</span>
            <span class="team-status">{{ t('drafts.publication.presenceCta', { status: t(`drafts.publication.status.${publicationStatus('ChamadaPresenca')}`) }) }}</span>
            <span class="team-status">{{ t('drafts.publication.finalTeams', { status: t(`drafts.publication.status.${publicationStatus('TimesDefinidos')}`) }) }}</span>
            <button type="button" class="button-secondary" :disabled="saving" @click="requestDiscordRepublish('Presenca')">{{ t('drafts.publication.republishPresence') }}</button>
            <button v-if="['Falha', 'RequerReconciliacao'].includes(publicationStatus('ChamadaPresenca'))" type="button" class="button-secondary" :disabled="saving" @click="requestDiscordRepublish('ChamadaPresenca')">{{ t('drafts.publication.republishPresenceCta') }}</button>
            <button type="button" class="button-secondary" :disabled="saving" @click="requestDiscordRepublish('TimesDefinidos')">{{ t('drafts.publication.republishFinalTeams') }}</button>
          </div>
          <div v-if="canManageDrafts && selectedMontagem.status === DraftMontagemStatusValues.PresencaAberta" class="draft-hero-actions">
            <label class="filter-field">
              {{ t('drafts.presence.manualPlayer') }}
              <input v-model="manualPresenceSearch" type="search" :placeholder="t('drafts.presence.searchPlayer')" :disabled="saving" @input="loadEligibleManualPresencePlayers" />
              <select v-model="selectedManualPresencePlayerId" :disabled="saving">
                <option value="">{{ t('drafts.presence.selectPlayer') }}</option>
                <option v-for="player in availableManualPresencePlayers" :key="player.id" :value="player.id">{{ player.nomeExibicao }}</option>
              </select>
            </label>
            <button type="button" class="button-secondary" :disabled="saving || !selectedManualPresencePlayerId" @click="addManualPresence">{{ t('drafts.presence.addManual') }}</button>
          </div>
          <div class="draft-player-picker__grid">
            <div v-for="presence in confirmedPresences" :key="presence.id" class="draft-player-option" :class="{ 'is-selected': captainSelection.includes(presence.jogadorId) }">
              <button type="button" :disabled="selectedMontagem.status !== DraftMontagemStatusValues.PresencaEncerrada || !canManageDrafts" @click="toggleCaptainSelection(presence.jogadorId)">
                <span class="draft-slot__avatar">{{ presence.nomeExibicao.charAt(0) }}</span>
                <span><strong>{{ presence.nomeExibicao }}</strong><small>{{ formatPresenceOrigin(presence.origemConfirmacao) }}</small></span>
              </button>
              <button v-if="canManageDrafts && selectedMontagem.status === DraftMontagemStatusValues.PresencaAberta" type="button" class="button-secondary" :disabled="saving" @click.stop="requestManualPresenceRemoval(presence.jogadorId, presence.nomeExibicao)">{{ t('drafts.presence.removeManual') }}</button>
            </div>
          </div>
        </section>
        <DraftVisualBoard
          v-if="selectedMontagem && selectedMontagem.status !== DraftMontagemStatusValues.PresencaAberta && selectedMontagem.status !== DraftMontagemStatusValues.PresencaEncerrada && selectedMontagem.status !== DraftMontagemStatusValues.CapitaesDefinidos"
          :montagem="selectedMontagem"
          :saving="saving"
          :can-manage="canManageDrafts"
          :current-player-id="currentPlayerId"
          @save="saveMontagemLayout"
          @start-realtime="startRealtime"
          @pick="pickRealtime"
          @substitute-reserve="substituteReserve"
          @draw-captains="drawMontagemCaptains"
          @finalize="finalizeMontagem"
          @cancel="requestDraftCancellation"
        />
        <section v-else-if="!selectedMontagem" class="draft-empty-card">
          <h2>{{ t('drafts.noSelectionTitle') }}</h2>
          <p>{{ t('drafts.noSelectionDescription') }}</p>
        </section>
      </div>
    </section>

    <DraftVisualSetup
      :open="visualSetupOpen"
      :players="players"
      :captains="captains"
      :saving="saving"
      :errors="serviceErrors"
      @close="visualSetupOpen = false"
      @submit="saveMontagem"
    />
    <DraftReasonDialog
      :open="pendingReasonAction !== null"
      :action="pendingReasonAction"
      :saving="saving"
      @cancel="pendingReasonAction = null"
      @confirm="confirmReasonAction"
    />
  </PageFrame>
</template>
