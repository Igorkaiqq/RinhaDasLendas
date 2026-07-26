<script setup lang="ts">
import { computed, nextTick, onMounted, onUnmounted, ref, useTemplateRef } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute } from 'vue-router'

import DraftNavigator from '@/components/drafts/DraftNavigator.vue'
import DraftReasonDialog, { type DraftReasonDialogAction } from '@/components/drafts/DraftReasonDialog.vue'
import DraftPreparationPanel from '@/components/drafts/DraftPreparationPanel.vue'
import DraftDiscordPublicationPanel from '@/components/drafts/DraftDiscordPublicationPanel.vue'
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
import { DraftMontagemEstadoValues, DraftMontagemOrdemEscolhaModoValues, DraftMontagemPresencaStatusValues, DraftMontagemStatusValues } from '@/constants/draftMontagem'
import type { DraftMontagem, DraftMontagemAdmin, DraftMontagemLayoutPayload, DraftMontagemPayload, DraftMontagemPublicacaoDiscordStatus, DraftMontagemPublicacaoDiscordTipo, DraftMontagemRealtimeState, DraftMontagemResumo, DraftMontagemStatus } from '@/types/draftMontagem'

const players = ref<Player[]>([])
const { t } = useI18n()
const route = useRoute()
const auth = useAuthState()
const captains = ref<Player[]>([])
const loading = ref(true)
const listLoadFailed = ref(false)
const saving = ref(false)
const errors = ref<string[]>([])
const serviceErrors = ref<string[]>([])
const notification = ref<string | null>(null)
const visualSetupOpen = ref(false)
const searchTerm = ref('')
const selectedStatus = ref<DraftMontagemStatus | ''>('')
const selectedMontagem = ref<DraftMontagem | null>(null)
const selectedDraftId = ref<string | null>(null)
const canCurrentUserPick = ref<boolean | null>(null)
const serverClockOffsetMs = ref(0)
const visualMontagens = ref<DraftMontagemResumo[]>([])
const hasKnownDrafts = ref(false)
const realtimeConnection = ref<DraftMontagemRealtimeConnection | null>(null)
const selectedManualPresencePlayerId = ref('')
const manualPresenceSearch = ref('')
const manualPresencePlayers = ref<Pick<Player, 'id' | 'nomeExibicao'>[]>([])
const pendingReasonAction = ref<DraftReasonDialogAction | null>(null)
const workspaceHeader = useTemplateRef<InstanceType<typeof DraftWorkspaceHeader>>('workspaceHeader')
const adminAccessDenied = ref(false)
let detailRequestVersion = 0
let manualPresenceRequestVersion = 0
let manualPresenceAbortController: AbortController | null = null
let activeDraftId: string | null = null
let activeDraftGeneration = 0
let listRequestVersion = 0

interface DraftUpdateContext {
  draftId: string
  generation: number
  requestVersion: number
}

const captainSelection = ref<string[]>([])
const statusOptions = DRAFT_MONTAGEM_STATUS_OPTIONS
const preparationStatuses: readonly DraftMontagemStatus[] = [
  DraftMontagemStatusValues.PresencaAberta,
  DraftMontagemStatusValues.PresencaEncerrada,
  DraftMontagemStatusValues.CapitaesDefinidos,
]
const discordPublicationTypes: readonly DraftMontagemPublicacaoDiscordTipo[] = ['Presenca', 'ChamadaPresenca', 'TimesDefinidos']
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
const discordPublicationMatrix = computed(() => {
  const publications = selectedMontagem.value?.publicacoesDiscord ?? []
  const canonical = discordPublicationTypes.map((tipo) => ({
    tipo,
    status: publications.find((publication) => publication.tipo === tipo)?.status ?? null,
  }))
  const seenTypes = new Set<string>(discordPublicationTypes)
  const noncanonical = publications.filter((publication) => {
    const tipo = publication.tipo as string
    if (seenTypes.has(tipo)) return false
    seenTypes.add(tipo)
    return true
  })
  return [...canonical, ...noncanonical]
})
const finalTeamsPublicationStatus = computed(() => discordPublicationStatus('TimesDefinidos'))
const selectedDataRinha = computed(() => visualMontagens.value.find((draft) => draft.id === selectedMontagem.value?.id)?.dataRinha ?? null)
const preparationCapabilities = computed(() => {
  const status = selectedMontagem.value?.status
  const presenceOpen = status === DraftMontagemStatusValues.PresencaAberta
  const presenceClosed = status === DraftMontagemStatusValues.PresencaEncerrada
  const canManageOpenPresence = canManageDrafts.value && presenceOpen
  const canSelectCaptains = canManageDrafts.value && presenceClosed

  return {
    canConfirmPresence: presenceOpen && !myPresence.value,
    canCancelPresence: presenceOpen && Boolean(myPresence.value),
    canClosePresence: canManageOpenPresence,
    canContinueManualPresence: canManageOpenPresence && confirmedPresences.value.length < 10,
    canManageManualPresence: canManageOpenPresence,
    canSelectCaptains,
    canDefineCaptains: canSelectCaptains
      && captainSelection.value.length === selectedMontagem.value?.quantidadeTimes
      && captainSelection.value.every((id) => confirmedPresences.value.some((presence) => presence.jogadorId === id)),
    canDrawOrder: canManageDrafts.value && status === DraftMontagemStatusValues.CapitaesDefinidos,
  }
})
const discordRepublishableTypes = computed<readonly DraftMontagemPublicacaoDiscordTipo[]>(() => {
  if (!canManageDrafts.value) return []
  return discordPublicationTypes.filter((tipo) => tipo !== 'ChamadaPresenca'
    || ['Falha', 'RequerReconciliacao'].includes(discordPublicationStatus(tipo) ?? ''))
})

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
  selectedDraftId.value = null
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
  const requestVersion = ++listRequestVersion
  loading.value = true
  listLoadFailed.value = false
  try {
    const montagens = await listDraftMontagens({ status: selectedStatus.value })
    if (requestVersion !== listRequestVersion) return
    visualMontagens.value = montagens
    if (!selectedStatus.value) hasKnownDrafts.value = montagens.length > 0
    else if (montagens.length > 0) hasKnownDrafts.value = true
    if (!selectedDraftId.value) {
      const initialDraftId = resolveInitialDraftId(route.query.draftId)
      if (initialDraftId) {
        await openMontagemFromLink(initialDraftId)
        return
      }

      if (visualMontagens.value[0]) {
        await openMontagem(visualMontagens.value[0].id)
      }
    }
  } catch {
    if (requestVersion === listRequestVersion) listLoadFailed.value = true
  } finally {
    if (requestVersion === listRequestVersion) loading.value = false
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
  selectedDraftId.value = id
  detailRequestVersion = 0
  manualPresenceAbortController?.abort()
  manualPresenceAbortController = null
  manualPresenceRequestVersion++
  selectedMontagem.value = null
  canCurrentUserPick.value = null
  serverClockOffsetMs.value = 0
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

async function applyMutationRealtimeState(context: DraftUpdateContext, state: DraftMontagemRealtimeState) {
  if (!(await applyMutationProjection(context, state.montagem))) return false
  if (!isCurrentUpdate(context)) return false
  applyPersonalizedRealtimeMetadata(state)
  return true
}

function applyPersonalizedRealtimeMetadata(state: DraftMontagemRealtimeState) {
  canCurrentUserPick.value = state.canCurrentUserPick
  const serverNow = Date.parse(state.serverNow)
  if (Number.isFinite(serverNow)) serverClockOffsetMs.value = serverNow - Date.now()
}

async function applyPersonalizedRealtimeState(context: DraftUpdateContext, state: DraftMontagemRealtimeState) {
  if (!isCurrentUpdate(context) || state.montagem.id !== context.draftId) return false
  if (!(await refreshMontagemDetail(context.draftId, context.generation, state.montagem, context))) return false
  if (!isCurrentUpdate(context)) return false
  applyPersonalizedRealtimeMetadata(state)
  return true
}

async function loadPersonalizedRealtimeState(id: string, generation: number) {
  const context = beginDraftUpdate(id, generation)
  if (!context) return false
  const state = await getDraftMontagemRealtimeState(id)
  if (!isCurrentUpdate(context)) return false
  return applyPersonalizedRealtimeState(context, state)
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
  if (saving.value || !preparationCapabilities.value.canConfirmPresence) return
  const context = beginSelectedDraftUpdate()
  if (!context) return
  let completed = false
  saving.value = true
  try {
    const montagem = await confirmDraftMontagemPresence(context.draftId)
    if (await applyMutationProjection(context, montagem)) {
      notification.value = t('drafts.presence.confirmed')
      completed = true
    }
  } catch (error) {
    if (isActiveDraft(context.draftId, context.generation)) captureError(error)
  } finally {
    if (isActiveDraft(context.draftId, context.generation)) {
      saving.value = false
      if (completed) await restoreStageFocus()
    }
  }
}

async function cancelPresence() {
  if (saving.value || !preparationCapabilities.value.canCancelPresence) return
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
  if (saving.value || !selectedMontagem.value || !preparationCapabilities.value.canManageManualPresence || !selectedManualPresencePlayerId.value) return
  const player = availableManualPresencePlayers.value.find((item) => item.id === selectedManualPresencePlayerId.value)
  if (!player) return

  pendingReasonAction.value = { type: 'addManualPresence', jogadorId: player.id, jogadorNome: player.nomeExibicao }
}

function requestManualPresenceRemoval(jogadorId: string, jogadorNome: string) {
  if (
    !saving.value
    && selectedMontagem.value
    && preparationCapabilities.value.canManageManualPresence
    && confirmedPresences.value.some((presence) => presence.jogadorId === jogadorId)
  ) {
    pendingReasonAction.value = { type: 'removeManualPresence', jogadorId, jogadorNome }
  }
}

async function closePresence(continueWithLess = false) {
  if (saving.value) return
  const montagemAtual = selectedMontagem.value
  const canClose = continueWithLess
    ? preparationCapabilities.value.canContinueManualPresence
    : preparationCapabilities.value.canClosePresence
  if (!montagemAtual || !canClose) return
  const context = beginSelectedDraftUpdate()
  if (!context) return
  let completed = false
  saving.value = true
  try {
    const montagem = await closeDraftMontagemPresence(context.draftId, continueWithLess, montagemAtual.tamanhoEquipe)
    if (await applyMutationProjection(context, montagem)) {
      notification.value = t('drafts.presence.closed')
      completed = true
    }
  } catch (error) {
    if (isActiveDraft(context.draftId, context.generation)) captureError(error)
  } finally {
    if (isActiveDraft(context.draftId, context.generation)) {
      saving.value = false
      if (completed) await restoreStageFocus()
    }
  }
}

function toggleCaptainSelection(jogadorId: string) {
  if (
    saving.value
    || !canManageDrafts.value
    || selectedMontagem.value?.status !== DraftMontagemStatusValues.PresencaEncerrada
    || !confirmedPresences.value.some((presence) => presence.jogadorId === jogadorId)
  ) return
  if (captainSelection.value.includes(jogadorId)) {
    captainSelection.value = captainSelection.value.filter((id) => id !== jogadorId)
    return
  }
  if (!selectedMontagem.value || captainSelection.value.length >= selectedMontagem.value.quantidadeTimes) return
  captainSelection.value = [...captainSelection.value, jogadorId]
}

async function defineCaptains() {
  if (
    saving.value
    || !canManageDrafts.value
    || selectedMontagem.value?.status !== DraftMontagemStatusValues.PresencaEncerrada
    || captainSelection.value.length !== selectedMontagem.value.quantidadeTimes
    || captainSelection.value.some((id) => !confirmedPresences.value.some((presence) => presence.jogadorId === id))
  ) return
  const context = beginSelectedDraftUpdate()
  if (!context) return
  let completed = false
  saving.value = true
  try {
    const montagem = await defineDraftMontagemCaptains(context.draftId, captainSelection.value)
    if (await applyMutationProjection(context, montagem)) {
      notification.value = t('drafts.presence.captainsDefined')
      completed = true
    }
  } catch (error) {
    if (isActiveDraft(context.draftId, context.generation)) captureError(error)
  } finally {
    if (isActiveDraft(context.draftId, context.generation)) {
      saving.value = false
      if (completed) await restoreStageFocus()
    }
  }
}

async function drawPickOrder() {
  if (saving.value || !canManageDrafts.value || selectedMontagem.value?.status !== DraftMontagemStatusValues.CapitaesDefinidos) return
  const context = beginSelectedDraftUpdate()
  if (!context) return
  let completed = false
  saving.value = true
  try {
    const montagem = await defineDraftMontagemPickOrder(context.draftId, DraftMontagemOrdemEscolhaModoValues.Sorteado)
    if (await applyMutationProjection(context, montagem)) {
      notification.value = t('drafts.presence.orderDefined')
      completed = true
    }
  } catch (error) {
    if (isActiveDraft(context.draftId, context.generation)) captureError(error)
  } finally {
    if (isActiveDraft(context.draftId, context.generation)) {
      saving.value = false
      if (completed) await restoreStageFocus()
    }
  }
}

async function connectRealtime(id: string, generation: number) {
  if (!isActiveDraft(id, generation)) return
  try {
    await loadPersonalizedRealtimeState(id, generation)
  } catch {
    // The regular detail endpoint already loaded the board; realtime state errors are shown by later actions.
  }

  if (!isActiveDraft(id, generation)) return
  const connection = new DraftMontagemRealtimeConnection(id)
  realtimeConnection.value = connection
  await connection.connect(
    async () => {
      if (!isActiveDraft(id, generation)) return
      try {
        await loadPersonalizedRealtimeState(id, generation)
      } catch {
        // Keep the last personalized projection if its refresh fails.
      }
    },
    async () => {
      if (!isActiveDraft(id, generation)) return
      await loadPersonalizedRealtimeState(id, generation)
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
  if (saving.value || !canManageDrafts.value || selectedMontagem.value?.status !== DraftMontagemStatusValues.Aberta || selectedMontagem.value.modo !== 'Manual') return
  const context = beginSelectedDraftUpdate()
  if (!context) return
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
  if (saving.value || !canManageDrafts.value || selectedMontagem.value?.status !== DraftMontagemStatusValues.Aberta || selectedMontagem.value.modo !== 'Manual') return
  const context = beginSelectedDraftUpdate()
  if (!context) return

  saving.value = true
  errors.value = []
  try {
    const state = await startDraftMontagemRealtime(context.draftId)
    if (await applyMutationRealtimeState(context, state)) notification.value = t('drafts.realtime.started')
  } catch (error) {
    if (isActiveDraft(context.draftId, context.generation)) captureError(error)
  } finally {
    if (isActiveDraft(context.draftId, context.generation)) saving.value = false
  }
}

async function pickRealtime(jogadorId: string) {
  const current = selectedMontagem.value
  const activeTeam = current?.times.find((team) => team.id === current.turnoAtualTimeId)
  const activeCaptainId = current?.turnoAtualCapitaoId
  const turnExpiresAt = current?.turnoExpiraEm ? Date.parse(current.turnoExpiraEm) : Number.NaN
  if (
    saving.value
    || !current
    || current.status !== DraftMontagemStatusValues.Aberta
    || current.modo !== 'TempoReal'
    || canCurrentUserPick.value !== true
    || !activeTeam
    || !activeCaptainId
    || activeTeam.capitaoId !== activeCaptainId
    || activeCaptainId !== currentPlayerId.value
    || !activeTeam.jogadores.some((player) => player.jogadorId === activeCaptainId && player.capitao)
    || !current.livres.some((player) => player.jogadorId === jogadorId && player.estado === DraftMontagemEstadoValues.Livre)
    || !Number.isFinite(turnExpiresAt)
    || turnExpiresAt <= Date.now() + serverClockOffsetMs.value
  ) return
  const context = beginSelectedDraftUpdate()
  if (!context) return

  let completed = false
  saving.value = true
  errors.value = []
  try {
    const state = await registerDraftMontagemPick(context.draftId, jogadorId)
    completed = await applyMutationRealtimeState(context, state)
  } catch (error) {
    if (isActiveDraft(context.draftId, context.generation)) captureError(error)
  } finally {
    if (isActiveDraft(context.draftId, context.generation)) {
      saving.value = false
      if (completed) await restoreStageFocus()
    }
  }
}

async function substituteReserve(payload: { timeId: string; jogadorSaiuId: string; reservaEntrouId: string; motivo?: string | null }) {
  const current = selectedMontagem.value
  const team = current?.times.find((item) => item.id === payload.timeId)
  if (
    saving.value
    || !canManageDrafts.value
    || !current
    || current.status !== DraftMontagemStatusValues.Aberta
    || !team?.jogadores.some((player) => player.jogadorId === payload.jogadorSaiuId)
    || !current.reservas.some((player) => player.jogadorId === payload.reservaEntrouId && player.estado === DraftMontagemEstadoValues.Reserva)
  ) return
  const context = beginSelectedDraftUpdate()
  if (!context) return

  saving.value = true
  errors.value = []
  try {
    const state = await substituteDraftMontagemReserve(context.draftId, payload)
    if (await applyMutationRealtimeState(context, state)) notification.value = t('drafts.realtime.reserveSubstituted')
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
  if (saving.value || !canManageDrafts.value || selectedMontagem.value?.status !== DraftMontagemStatusValues.Aberta || selectedMontagem.value.modo !== 'Manual') return
  const context = beginSelectedDraftUpdate()
  if (!context) return
  let completed = false
  saving.value = true
  try {
    const montagem = await finalizeDraftMontagem(context.draftId)
    if (!(await applyMutationProjection(context, montagem))) return
    await loadVisualMontagens()
    notification.value = t('drafts.messages.finished')
    completed = true
  } catch (error) {
    if (isActiveDraft(context.draftId, context.generation)) captureError(error)
  } finally {
    if (isActiveDraft(context.draftId, context.generation)) {
      saving.value = false
      if (completed) await restoreStageFocus()
    }
  }
}

function requestDraftCancellation() {
  if (
    !saving.value
    && selectedMontagem.value
    && canManageDrafts.value
    && selectedMontagem.value.status !== DraftMontagemStatusValues.Finalizada
    && selectedMontagem.value.status !== DraftMontagemStatusValues.Cancelada
  ) {
    pendingReasonAction.value = { type: 'cancelDraft' }
  }
}

function resetFilters() {
  searchTerm.value = ''
  selectedStatus.value = ''
  void loadVisualMontagens()
}

function updateStatusFilter(value: DraftMontagemStatus | '') {
  selectedStatus.value = value
  void loadVisualMontagens()
}

function discordPublicationStatus(tipo: DraftMontagemPublicacaoDiscordTipo): DraftMontagemPublicacaoDiscordStatus | null {
  return discordPublicationMatrix.value.find((publication) => publication.tipo === tipo)?.status ?? null
}

function requestDiscordRepublish(action: { publicationType: DraftMontagemPublicacaoDiscordTipo; publicationStatus: DraftMontagemPublicacaoDiscordStatus | string | null }) {
  const currentStatus = discordPublicationStatus(action.publicationType)
  if (
    saving.value
    || !selectedMontagem.value
    || !discordRepublishableTypes.value.includes(action.publicationType)
    || currentStatus !== action.publicationStatus
  ) return
  pendingReasonAction.value = {
    type: 'republishDiscord',
    publicationType: action.publicationType,
    publicationStatus: currentStatus,
  }
}

function isReasonActionAvailable(action: DraftReasonDialogAction) {
  if (action.type === 'cancelDraft') {
    return selectedMontagem.value?.status !== DraftMontagemStatusValues.Finalizada
      && selectedMontagem.value?.status !== DraftMontagemStatusValues.Cancelada
  }
  if (action.type === 'addManualPresence') {
    return preparationCapabilities.value.canManageManualPresence
      && availableManualPresencePlayers.value.some((player) => player.id === action.jogadorId)
  }
  if (action.type === 'removeManualPresence') {
    return preparationCapabilities.value.canManageManualPresence
      && confirmedPresences.value.some((presence) => presence.jogadorId === action.jogadorId)
  }
  return discordRepublishableTypes.value.includes(action.publicationType)
    && discordPublicationStatus(action.publicationType) === action.publicationStatus
}

async function confirmReasonAction(reason: string) {
  if (saving.value) return

  const action = pendingReasonAction.value
  if (!action || !selectedMontagem.value) return
  if (!canManageDrafts.value || !isReasonActionAvailable(action)) {
    pendingReasonAction.value = null
    return
  }
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
      const montagem = await republishDraftMontagemDiscordPublication(context.draftId, action.publicationType, reason)
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

async function restoreStageFocus() {
  await nextTick()
  await workspaceHeader.value?.focusStage()
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

    <div v-if="errors.length" class="form-errors" role="alert">
      <p v-for="error in errors" :key="error">{{ error }}</p>
    </div>

    <section class="draft-layout" data-draft-shell :aria-label="t('drafts.title')">
      <DraftNavigator
        :drafts="filteredDrafts"
        :selected-draft-id="selectedDraftId"
        :search-term="searchTerm"
        :selected-status="selectedStatus"
        :status-options="statusOptions"
        :loading="loading"
        :load-failed="listLoadFailed"
        :has-known-drafts="hasKnownDrafts"
        :can-create="canManageDrafts"
        @update:search-term="searchTerm = $event"
        @update:selected-status="updateStatusFilter"
        @select="openMontagem"
        @reset="resetFilters"
        @retry="loadVisualMontagens"
        @create="visualSetupOpen = true"
      />

      <div class="draft-main" data-draft-workspace>
        <DraftWorkspaceHeader
          v-if="selectedMontagem"
          ref="workspaceHeader"
          :draft="selectedMontagem"
          :data-rinha="selectedDataRinha"
          :confirmed-count="confirmedPresences.length"
          :final-teams-publication-status="finalTeamsPublicationStatus"
        >
          <template #primary-action>
          </template>
          <template #secondary-actions>
          </template>
          <template #danger-action>
            <Button v-if="canManageDrafts && selectedMontagem.status !== DraftMontagemStatusValues.Finalizada && selectedMontagem.status !== DraftMontagemStatusValues.Cancelada" type="button" variant="destructive" :disabled="saving" @click="requestDraftCancellation">{{ t('common.cancel') }}</Button>
          </template>
        </DraftWorkspaceHeader>

        <DraftPreparationPanel
          v-if="selectedMontagem && preparationStatuses.includes(selectedMontagem.status)"
          :draft="selectedMontagem"
          :confirmed-presences="confirmedPresences"
          :saving="saving"
          :can-confirm-presence="preparationCapabilities.canConfirmPresence"
          :can-cancel-presence="preparationCapabilities.canCancelPresence"
          :can-close-presence="preparationCapabilities.canClosePresence"
          :can-continue-manual-presence="preparationCapabilities.canContinueManualPresence"
          :can-manage-manual-presence="preparationCapabilities.canManageManualPresence"
          :can-select-captains="preparationCapabilities.canSelectCaptains"
          :can-define-captains="preparationCapabilities.canDefineCaptains"
          :can-draw-order="preparationCapabilities.canDrawOrder"
          :captain-selection="captainSelection"
          :manual-presence-search="manualPresenceSearch"
          :selected-manual-presence-player-id="selectedManualPresencePlayerId"
          :available-manual-presence-players="availableManualPresencePlayers"
          @confirm-presence="confirmPresence"
          @cancel-presence="cancelPresence"
          @close-presence="closePresence"
          @update:manual-presence-search="manualPresenceSearch = $event"
          @search-manual-presence="loadEligibleManualPresencePlayers"
          @update:selected-manual-presence-player-id="selectedManualPresencePlayerId = $event"
          @add-manual-presence="addManualPresence"
          @remove-manual-presence="requestManualPresenceRemoval"
          @toggle-captain="toggleCaptainSelection"
          @define-captains="defineCaptains"
          @draw-order="drawPickOrder"
        />
        <DraftDiscordPublicationPanel
          v-if="selectedMontagem && canManageDrafts"
          :publications="discordPublicationMatrix"
          :republishable-types="discordRepublishableTypes"
          :saving="saving"
          @republish="requestDiscordRepublish"
        />
        <DraftVisualBoard
          v-if="selectedMontagem && selectedMontagem.status !== DraftMontagemStatusValues.PresencaAberta && selectedMontagem.status !== DraftMontagemStatusValues.PresencaEncerrada && selectedMontagem.status !== DraftMontagemStatusValues.CapitaesDefinidos"
          :montagem="selectedMontagem"
          :saving="saving"
          :can-manage="canManageDrafts"
          :current-player-id="currentPlayerId"
          :can-current-user-pick="canCurrentUserPick"
          :server-clock-offset-ms="serverClockOffsetMs"
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
      @restore-focus="restoreStageFocus"
    />
  </PageFrame>
</template>
