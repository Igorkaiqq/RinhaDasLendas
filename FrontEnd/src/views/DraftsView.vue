<script setup lang="ts">
/* global document, window, HTMLElement, BeforeUnloadEvent */
import { computed, nextTick, onMounted, onUnmounted, ref, useTemplateRef, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { onBeforeRouteLeave, useRoute } from 'vue-router'

import DraftNavigator from '@/components/drafts/DraftNavigator.vue'
import DraftReasonDialog, { type DraftReasonDialogAction } from '@/components/drafts/DraftReasonDialog.vue'
import DraftPreparationPanel from '@/components/drafts/DraftPreparationPanel.vue'
import DraftDiscordPublicationPanel from '@/components/drafts/DraftDiscordPublicationPanel.vue'
import DraftWorkspaceHeader from '@/components/drafts/DraftWorkspaceHeader.vue'
import DraftUnsavedLayoutDialog, { type DraftUnsavedLayoutIntent } from '@/components/drafts/DraftUnsavedLayoutDialog.vue'
import DraftVisualBoard from '@/components/drafts/visual/DraftVisualBoard.vue'
import DraftVisualSetup from '@/components/drafts/visual/DraftVisualSetup.vue'
import PageFrame from '@/components/layout/PageFrame.vue'
import PageHeader from '@/components/layout/PageHeader.vue'
import PendingPlayerProfileNotice from '@/components/users/PendingPlayerProfileNotice.vue'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { AuthRoles } from '@/constants/authRoles'
import { DRAFT_MONTAGEM_STATUS_OPTIONS } from '@/constants/draftMontagemStatus'
import { Permissions } from '@/constants/permissions'
import { useAuthState } from '@/services/authState'
import { listPlayers, type Player } from '@/services/players'
import {
  addManualDraftMontagemPresence,
  archiveDraftMontagem,
  cancelDraftMontagem,
  cancelDraftMontagemPresence,
  closeDraftMontagemPresence,
  chooseDraftMontagemMode,
  confirmDraftMontagemPresence,
  createDraftMontagem,
  defineDraftMontagemCaptains,
  defineDraftMontagemPickOrder,
  DraftMontagemServiceError,
  drawDraftMontagemCaptains,
  finalizeDraftMontagem,
  getDraftMontagemAdminById,
  getDraftMontagemArchivingById,
  getDraftMontagemRealtimeState,
  listEligibleManualPresencePlayers,
  listDraftMontagens,
  registerDraftMontagemPick,
  removeManualDraftMontagemPresence,
  reopenDraftMontagemPresence,
  republishDraftMontagemDiscordPublication,
  republishArchivedDraftCancellation,
  restoreDraftMontagem,
  saveDraftMontagemLayout,
  startDraftMontagemRealtime,
  substituteDraftMontagemReserve,
} from '@/services/draftMontagens'
import { DraftMontagemRealtimeConnection } from '@/services/draftMontagemRealtime'
import { resolveInitialDraftId } from '@/services/draftRoute'
import { DraftMontagemEstadoValues, DraftMontagemOrdemEscolhaModoValues, DraftMontagemPresencaStatusValues, DraftMontagemStatusValues } from '@/constants/draftMontagem'
import type { DraftConnectionStatus, DraftMontagem, DraftMontagemAcaoAdministrativa, DraftMontagemAdmin, DraftMontagemArquivamento, DraftMontagemLayoutPayload, DraftMontagemModo, DraftMontagemPayload, DraftMontagemPublicacaoDiscordStatus, DraftMontagemPublicacaoDiscordTipo, DraftMontagemRealtimeState, DraftMontagemResumo, DraftMontagemStatus, DraftMontagemSubstituicaoPayload } from '@/types/draftMontagem'

const players = ref<Player[]>([])
const { locale, t, te } = useI18n()
const route = useRoute()
const auth = useAuthState()
const loading = ref(true)
const listLoadFailed = ref(false)
const saving = ref(false)
const errors = ref<string[]>([])
const serviceErrors = ref<string[]>([])
const notification = ref<string | null>(null)
const visualSetupOpen = ref(false)
const searchTerm = ref('')
const selectedStatus = ref<DraftMontagemStatus | ''>('')
const includeArchived = ref(false)
const selectedMontagem = ref<DraftMontagem | null>(null)
const selectedArchiving = ref<DraftMontagemArquivamento | null>(null)
const selectedDraftId = ref<string | null>(null)
const selectedDataRinha = ref<string | null>(null)
const canCurrentUserPick = ref<boolean | null>(null)
const serverClockOffsetMs = ref(0)
const connectionStatus = ref<DraftConnectionStatus>('disconnected')
const visualMontagens = ref<DraftMontagemResumo[]>([])
const hasKnownDrafts = ref(false)
const realtimeConnection = ref<DraftMontagemRealtimeConnection | null>(null)
const selectedManualPresencePlayerId = ref('')
const manualPresenceSearch = ref('')
const manualPresencePlayers = ref<Pick<Player, 'id' | 'nomeExibicao'>[]>([])
const pendingReasonAction = ref<DraftReasonDialogAction | null>(null)
const boardDirty = ref(false)
const boardBaseVersion = ref(0)
const pendingCanonicalSnapshot = ref<DraftMontagem | null>(null)
const requiresLayoutReconciliation = ref(false)
const canonicalResetToken = ref(0)
const acceptedSaveVersion = ref<number | null>(null)
const pendingLayoutIntent = ref<DraftUnsavedLayoutIntent | null>(null)
const workspaceHeader = useTemplateRef<InstanceType<typeof DraftWorkspaceHeader>>('workspaceHeader')
const emptyWorkspace = useTemplateRef<InstanceType<typeof globalThis.HTMLElement>>('emptyWorkspace')
const adminAccessDenied = ref(false)
const archiveAccessDenied = ref(false)
let detailRequestVersion = 0
let passiveRequestId = 0
let mutationRequestId = 0
let personalizedSequence = 0
let lastPersonalizedSequence = 0
let highestSharedVersion = 0
let manualPresenceRequestVersion = 0
let manualPresenceAbortController: AbortController | null = null
let activeDraftId: string | null = null
let activeDraftGeneration = 0
let listRequestVersion = 0
let fallbackTimer: ReturnType<typeof globalThis.setInterval> | null = null
let fallbackRequestController: AbortController | null = null
let fallbackRequestInFlight = false
let canonicalRequestController: AbortController | null = null
let adminRequestController: AbortController | null = null
let conflictRequestController: AbortController | null = null
let canonicalAcceptanceId = 0
let acceptedLayoutSaveInProgress: number | null = null
let outstandingLayoutSave: { draftId: string; generation: number; baseVersion: number } | null = null
let pendingLayoutIntentAction: (() => void | Promise<void>) | null = null
let pendingRouteResolution: ((allow: boolean) => void) | null = null
let layoutIntentFocusTarget: HTMLElement | null = null
let deferredArchivedDraftId: string | null = null

type DraftUpdateLane = 'passive' | 'mutation'

interface DraftUpdateContext {
  draftId: string
  generation: number
  lane: DraftUpdateLane
  requestId: number
  personalizedSequence?: number
  holdSaving?: boolean
}

const captainSelection = ref<string[]>([])
const statusOptions = DRAFT_MONTAGEM_STATUS_OPTIONS
const preparationStatuses: readonly DraftMontagemStatus[] = [
  DraftMontagemStatusValues.PresencaAberta,
  DraftMontagemStatusValues.PresencaEncerrada,
  DraftMontagemStatusValues.CapitaesDefinidos,
]
const operationalDiscordPublicationTypes: readonly DraftMontagemPublicacaoDiscordTipo[] = ['Presenca', 'ChamadaPresenca', 'TimesDefinidos']
const hasDraftManagementPermission = computed(() => auth.hasPermission(Permissions.CanManageDrafts))
const canManageDrafts = computed(() => hasDraftManagementPermission.value && !adminAccessDenied.value)
const isAdminPlus = computed(() => auth.hasRole(AuthRoles.Admin) || auth.hasRole(AuthRoles.SuperAdmin))
const canManageDraftCycle = computed(() => canManageDrafts.value && isAdminPlus.value)
const hasDraftArchivePermission = computed(() => auth.hasPermission(Permissions.CanArchiveDrafts))
const canArchiveDrafts = computed(() => hasDraftArchivePermission.value && !archiveAccessDenied.value)
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
const eligibleCaptainIds = computed(() => (selectedMontagem.value as DraftMontagemAdmin | null)?.capitaesElegiveisIds ?? [])
const substitutionEligibleCaptainIds = computed(() => (selectedMontagem.value as DraftMontagemAdmin | null)?.capitaesElegiveisSubstituicaoIds ?? [])
const selectableCaptainIds = computed(() => selectedMontagem.value?.cicloVersao === 'ModoPosPresenca'
  ? eligibleCaptainIds.value
  : confirmedPresences.value.map((presence) => presence.jogadorId))
const availableManualPresencePlayers = computed(() => {
  const confirmed = new Set(confirmedPresences.value.map((presence) => presence.jogadorId))
  return manualPresencePlayers.value.filter((player) => !confirmed.has(player.id))
})
const discordPublicationMatrix = computed(() => {
  const publications = selectedMontagem.value?.publicacoesDiscord ?? []
  const canonicalTypes: readonly DraftMontagemPublicacaoDiscordTipo[] = selectedMontagem.value?.arquivado
    ? ['Cancelamento']
    : operationalDiscordPublicationTypes
  const canonical = canonicalTypes.map((tipo) => ({
    tipo,
    status: publications.find((publication) => publication.tipo === tipo)?.status ?? null,
  }))
  const seenTypes = new Set<string>(canonicalTypes)
  const noncanonical = publications.filter((publication) => {
    const tipo = publication.tipo as string
    if (seenTypes.has(tipo)) return false
    seenTypes.add(tipo)
    return true
  })
  return [...canonical, ...noncanonical]
})
const finalTeamsPublicationStatus = computed(() => discordPublicationStatus('TimesDefinidos'))
const preparationCapabilities = computed(() => {
  const status = selectedMontagem.value?.status
  const operational = !selectedMontagem.value?.arquivado
  const presenceOpen = status === DraftMontagemStatusValues.PresencaAberta
  const presenceClosed = status === DraftMontagemStatusValues.PresencaEncerrada
  const canManageOpenPresence = operational && canManageDrafts.value && presenceOpen
  const canSelectCaptains = operational
    && presenceClosed
    && canManageDraftCycle.value
    && (selectedMontagem.value?.cicloVersao !== 'ModoPosPresenca' || selectedMontagem.value.modo === 'TempoReal')

  return {
    canConfirmPresence: operational && presenceOpen && !myPresence.value,
    canCancelPresence: operational && presenceOpen && Boolean(myPresence.value),
    canClosePresence: canManageOpenPresence,
    canContinueManualPresence: canManageOpenPresence && confirmedPresences.value.length < 10,
    canManageManualPresence: canManageOpenPresence,
    canChooseMode: operational
      && canManageDraftCycle.value
      && presenceClosed
      && selectedMontagem.value?.cicloVersao === 'ModoPosPresenca'
      && selectedMontagem.value.modo === null,
    canSelectCaptains,
    canReopenPresence: operational && canManageDraftCycle.value && presenceClosed,
    canDefineCaptains: canSelectCaptains
      && captainSelection.value.length === selectedMontagem.value?.quantidadeTimes
      && captainSelection.value.every((id) => selectableCaptainIds.value.includes(id)),
    canDrawOrder: operational
      && status === DraftMontagemStatusValues.CapitaesDefinidos
      && canManageDraftCycle.value
      && (selectedMontagem.value?.cicloVersao !== 'ModoPosPresenca' || selectedMontagem.value.modo === 'TempoReal'),
  }
})

watch(
  [
    selectableCaptainIds,
    canManageDraftCycle,
    () => selectedMontagem.value?.status,
    () => selectedMontagem.value?.modo,
    () => selectedMontagem.value?.cicloVersao,
  ],
  ([selectableIds, canManageCycle, status, modo, cicloVersao]) => {
    const canSelect = canManageCycle
      && status === DraftMontagemStatusValues.PresencaEncerrada
      && (cicloVersao !== 'ModoPosPresenca' || modo === 'TempoReal')
    const reconciled = canSelect
      ? captainSelection.value.filter((id) => selectableIds.includes(id))
      : []
    if (reconciled.length !== captainSelection.value.length) captainSelection.value = reconciled
  },
  { flush: 'sync' },
)
const discordRepublishableTypes = computed<readonly DraftMontagemPublicacaoDiscordTipo[]>(() => {
  if (selectedMontagem.value?.arquivado) {
    const status = discordPublicationStatus('Cancelamento')
    return canArchiveDrafts.value && (status === 'Falha' || status === 'RequerReconciliacao') ? ['Cancelamento'] : []
  }
  if (!canManageDrafts.value) return []
  return operationalDiscordPublicationTypes.filter((tipo) => tipo !== 'ChamadaPresenca'
    || ['Falha', 'RequerReconciliacao'].includes(discordPublicationStatus(tipo) ?? ''))
})

watch(canArchiveDrafts, async (allowed) => {
  if (allowed || !includeArchived.value) return
  includeArchived.value = false
  if (selectedMontagem.value?.arquivado) {
    await removeArchivedAndReconcile(selectedMontagem.value.id, visualMontagens.value.findIndex((draft) => draft.id === selectedMontagem.value?.id))
  } else {
    await loadVisualMontagens()
  }
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
  window.addEventListener('beforeunload', handleBeforeUnload)
  await Promise.all([loadPlayers(), loadVisualMontagens()])
})

onUnmounted(async () => {
  window.removeEventListener('beforeunload', handleBeforeUnload)
  pendingRouteResolution?.(false)
  pendingRouteResolution = null
  pendingLayoutIntentAction = null
  deferredArchivedDraftId = null
  activeDraftId = null
  selectedDraftId.value = null
  invalidateOutstandingLayoutSave()
  activeDraftGeneration++
  detailRequestVersion = 0
  abortGenerationRequests()
  stopFallback()
  manualPresenceAbortController?.abort()
  await disconnectRealtime()
})

onBeforeRouteLeave(() => {
  if (!boardDirty.value) return true
  if (pendingLayoutIntent.value) return false

  return new Promise<boolean>((resolve) => {
    requestLayoutIntent('route-leave', null, resolve)
  })
})

function handleBeforeUnload(event: BeforeUnloadEvent) {
  if (!boardDirty.value) return
  event.preventDefault()
  event.returnValue = ''
}

function handleBoardDirtyChange(dirty: boolean, baseVersion: number) {
  boardDirty.value = dirty
  boardBaseVersion.value = baseVersion
  if (!dirty) {
    pendingCanonicalSnapshot.value = null
    requiresLayoutReconciliation.value = false
    cancelPendingRemoteUpdate()
  }
}

function cancelPendingRemoteUpdate() {
  if (pendingLayoutIntent.value !== 'remote-update' || pendingLayoutIntentAction !== null) return
  pendingLayoutIntent.value = null
  pendingRouteResolution = null
  void reconcileDeferredArchivedDraft()
}

function requestLayoutIntent(
  intent: DraftUnsavedLayoutIntent,
  action: (() => void | Promise<void>) | null = null,
  routeResolution: ((allow: boolean) => void) | null = null,
) {
  if (!boardDirty.value) {
    routeResolution?.(true)
    if (action) void action()
    return true
  }
  if (pendingLayoutIntent.value) {
    routeResolution?.(false)
    return false
  }

  pendingLayoutIntent.value = intent
  pendingLayoutIntentAction = action
  pendingRouteResolution = routeResolution
  layoutIntentFocusTarget = document.activeElement instanceof HTMLElement ? document.activeElement : null
  return false
}

function keepEditingLayout() {
  const routeResolution = pendingRouteResolution
  pendingLayoutIntent.value = null
  pendingLayoutIntentAction = null
  pendingRouteResolution = null
  routeResolution?.(false)
  void nextTick(() => layoutIntentFocusTarget?.isConnected && layoutIntentFocusTarget.focus())
}

async function discardLayout() {
  const action = pendingLayoutIntentAction
  const routeResolution = pendingRouteResolution
  const canonical = pendingCanonicalSnapshot.value
  if (canonical && canonical.id === selectedMontagem.value?.id && canonical.versaoEstado >= selectedMontagem.value.versaoEstado) {
    applyPublicMontagemState(canonical)
  }
  canonicalResetToken.value++
  boardDirty.value = false
  boardBaseVersion.value = selectedMontagem.value?.versaoEstado ?? 0
  pendingCanonicalSnapshot.value = null
  requiresLayoutReconciliation.value = false
  pendingLayoutIntent.value = null
  pendingLayoutIntentAction = null
  pendingRouteResolution = null
  await nextTick()
  routeResolution?.(true)
  if (action) await action()
  await reconcileDeferredArchivedDraft()
}

async function reconcileDeferredArchivedDraft(expectedDraftId = deferredArchivedDraftId) {
  const draftId = expectedDraftId
  if (!draftId) return
  await loadVisualMontagens()
  const archived = visualMontagens.value.find((draft) => draft.id === draftId)?.arquivado === true
  if (!archived) return
  if (selectedDraftId.value === draftId) {
    await openMontagem(draftId)
    if (selectedMontagem.value?.id !== draftId || !selectedMontagem.value.arquivado) return
  }
  if (deferredArchivedDraftId === draftId) deferredArchivedDraftId = null
}

function reviewLayoutUpdate() {
  const draftId = deferredArchivedDraftId
  requestLayoutIntent('remote-update', draftId ? () => reconcileDeferredArchivedDraft(draftId) : null)
}

function queueCanonicalSnapshot(montagem: DraftMontagem) {
  if (!pendingCanonicalSnapshot.value || montagem.versaoEstado > pendingCanonicalSnapshot.value.versaoEstado) {
    pendingCanonicalSnapshot.value = montagem
  }
  if (outstandingLayoutSave?.draftId === montagem.id && outstandingLayoutSave.generation === activeDraftGeneration) return
  requiresLayoutReconciliation.value = true
  requestLayoutIntent('remote-update')
}

async function loadPlayers() {
  try {
    players.value = await listPlayers(true)
  } catch {
    players.value = []
  }
}

async function loadVisualMontagens() {
  const requestVersion = ++listRequestVersion
  loading.value = true
  listLoadFailed.value = false
  try {
    const montagens = await listDraftMontagens({ status: selectedStatus.value, includeArchived: canArchiveDrafts.value && includeArchived.value })
    if (requestVersion !== listRequestVersion) return
    visualMontagens.value = montagens
    const selectedSummary = montagens.find((draft) => draft.id === selectedDraftId.value)
    if (selectedSummary) selectedDataRinha.value = selectedSummary.dataRinha ?? null
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
  } catch (error) {
    if (requestVersion !== listRequestVersion) return
    if (error instanceof DraftMontagemServiceError && error.status === 403 && includeArchived.value) {
      await handleArchiveAccessDenied()
      errors.value = [t('drafts.archive.errors.forbidden')]
    } else if (error instanceof DraftMontagemServiceError && error.status === 401) {
      errors.value = [t('drafts.archive.errors.unauthorized')]
    } else {
      listLoadFailed.value = true
    }
  } finally {
    if (requestVersion === listRequestVersion) loading.value = false
  }
}

function openVisualSetup() {
  if (canManageDraftCycle.value) visualSetupOpen.value = true
}

async function openMontagemFromLink(id: string) {
  await openMontagem(id)
  if (!selectedMontagem.value) {
    errors.value = [t('drafts.errors.linkNotFound')]
  }
}

async function openMontagem(id: string, publicProjection?: DraftMontagem) {
  invalidateOutstandingLayoutSave()
  const generation = ++activeDraftGeneration
  abortGenerationRequests()
  activeDraftId = id
  selectedDraftId.value = id
  selectedDataRinha.value = visualMontagens.value.find((draft) => draft.id === id)?.dataRinha ?? null
  detailRequestVersion = 0
  passiveRequestId = 0
  mutationRequestId = 0
  personalizedSequence = 0
  lastPersonalizedSequence = 0
  canonicalAcceptanceId = 0
  highestSharedVersion = 0
  connectionStatus.value = 'disconnected'
  stopFallback()
  manualPresenceAbortController?.abort()
  manualPresenceAbortController = null
  manualPresenceRequestVersion++
  selectedMontagem.value = null
  selectedArchiving.value = null
  canCurrentUserPick.value = null
  serverClockOffsetMs.value = 0
  pendingReasonAction.value = null
  captainSelection.value = []
  manualPresencePlayers.value = []
  selectedManualPresencePlayerId.value = ''
  const disconnecting = disconnectRealtime(false)
  saving.value = true
  errors.value = []
  try {
    await disconnecting
    if (!isActiveDraft(id, generation)) return
    initializeGenerationRequests()
    const archived = publicProjection?.arquivado || visualMontagens.value.find((draft) => draft.id === id)?.arquivado
    if (archived) {
      if (!(await refreshMontagemDetail(id, generation, publicProjection))) return
    } else {
      await connectRealtime(id, generation)
    }
    if (selectedMontagem.value) {
      captainSelection.value = []
      await loadEligibleManualPresencePlayers()
    }
  } catch (error) {
    if (isActiveDraft(id, generation)) captureError(error)
  } finally {
    if (isActiveDraft(id, generation)) saving.value = false
  }
}

function isActiveDraft(id: string, generation: number) {
  return activeDraftId === id && activeDraftGeneration === generation
}

function beginDraftUpdate(
  id: string,
  generation = activeDraftGeneration,
  lane: DraftUpdateLane = 'mutation',
  personalized = false,
): DraftUpdateContext | null {
  if (!isActiveDraft(id, generation)) return null
  detailRequestVersion++
  const requestId = lane === 'passive' ? ++passiveRequestId : ++mutationRequestId
  return {
    draftId: id,
    generation,
    lane,
    requestId,
    ...(personalized ? { personalizedSequence: ++personalizedSequence } : {}),
  }
}

function isCurrentUpdate(context: DraftUpdateContext) {
  void detailRequestVersion
  const currentRequestId = context.lane === 'passive' ? passiveRequestId : mutationRequestId
  return isActiveDraft(context.draftId, context.generation) && currentRequestId === context.requestId
}

function beginSelectedDraftUpdate(personalized = false) {
  return selectedMontagem.value ? beginDraftUpdate(selectedMontagem.value.id, activeDraftGeneration, 'mutation', personalized) : null
}

async function applyMutationProjection(context: DraftUpdateContext, montagem: DraftMontagem) {
  if (!isCurrentUpdate(context) || montagem.id !== context.draftId) return false
  applySharedProjection(context, montagem)
  scheduleAdministrativeDetail(context.draftId, context.generation)
  return true
}

async function applyMutationRealtimeState(context: DraftUpdateContext, state: DraftMontagemRealtimeState) {
  if (!applyPersonalizedRealtimeState(context, state)) return false
  scheduleAdministrativeDetail(context.draftId, context.generation)
  return true
}

function applyPersonalizedRealtimeMetadata(context: DraftUpdateContext, state: DraftMontagemRealtimeState) {
  const sequence = context.personalizedSequence
  if (sequence === undefined || sequence <= lastPersonalizedSequence || state.montagem.versaoEstado < highestSharedVersion) return false
  lastPersonalizedSequence = sequence
  canCurrentUserPick.value = state.canCurrentUserPick
  const serverNow = Date.parse(state.serverNow)
  if (Number.isFinite(serverNow)) serverClockOffsetMs.value = serverNow - Date.now()
  return true
}

function applyPersonalizedRealtimeState(context: DraftUpdateContext, state: DraftMontagemRealtimeState) {
  if (!isCurrentUpdate(context) || state.montagem.id !== context.draftId) return false
  const sharedApplied = applySharedProjection(context, state.montagem)
  const metadataApplied = applyPersonalizedRealtimeMetadata(context, state)
  return sharedApplied || metadataApplied
}

async function loadPersonalizedRealtimeState(id: string, generation: number, signal = canonicalRequestController?.signal) {
  if (!signal || signal.aborted) return false
  const context = beginDraftUpdate(id, generation, 'passive', true)
  if (!context) return false
  const state = await getDraftMontagemRealtimeState(id, signal)
  if (!isCurrentUpdate(context)) return false
  if (!applyPersonalizedRealtimeState(context, state)) return false
  canonicalAcceptanceId++
  return isActiveDraft(id, generation)
}

function scheduleAdministrativeDetail(id: string, generation: number) {
  if (!canManageDraftCycle.value || !isActiveDraft(id, generation)) return
  void refreshMontagemDetail(id, generation).catch(() => {
    // Identity-scoped enrichment is best-effort and never gates canonical health.
  })
}

async function refreshMontagemDetail(id: string, generation: number, publicProjection?: DraftMontagem) {
  const administrative = !publicProjection && canManageDraftCycle.value
  const context = beginDraftUpdate(id, generation, 'passive', administrative)
  if (!context || !isCurrentUpdate(context)) return false
  let detail = publicProjection
  const summary = visualMontagens.value.find((draft) => draft.id === id)

  if ((summary?.arquivado || publicProjection?.arquivado) && canArchiveDrafts.value) {
    try {
      const archiving = await getDraftMontagemArchivingById(id)
      if (!isCurrentUpdate(context) || archiving.draft.id !== id) return false
      selectedArchiving.value = archiving
      applySharedProjection(context, archiving.draft)
      return true
    } catch (error) {
      if (!isCurrentUpdate(context)) return false
      if (error instanceof DraftMontagemServiceError && error.status === 403) {
        await handleArchiveAccessDenied(id, visualMontagens.value.findIndex((draft) => draft.id === id))
        return false
      }
      throw error
    }
  }

  if (publicProjection) {
    applySharedProjection(context, publicProjection)
  }

  if (canManageDraftCycle.value) {
    adminRequestController?.abort()
    const controller = new AbortController()
    adminRequestController = controller
    const timeout = globalThis.setTimeout(() => controller.abort(), 2000)
    try {
      detail = await getDraftMontagemAdminById(id, controller.signal)
    } catch (error) {
      if (!isCurrentUpdate(context)) return false
      if (!(error instanceof DraftMontagemServiceError) || error.status !== 403) {
        if (publicProjection) return true
        throw error
      }

      adminAccessDenied.value = true
      dropAdministrativeState()
      return true
    } finally {
      globalThis.clearTimeout(timeout)
      if (adminRequestController === controller) adminRequestController = null
    }
  } else if (!detail) {
    return Boolean(selectedMontagem.value?.id === id)
  }

  if (!isCurrentUpdate(context) || detail.id !== id) return false
  if (canArchiveDrafts.value && typeof detail.versaoEstado !== 'number') {
    const archiving = await getDraftMontagemArchivingById(id)
    if (!isCurrentUpdate(context) || archiving.draft.id !== id) return false
    selectedArchiving.value = archiving
    detail = { ...detail, arquivado: archiving.draft.arquivado, versaoEstado: archiving.draft.versaoEstado }
  }
  if (summary) detail = { ...detail, arquivado: summary.arquivado }
  if (!selectedArchiving.value) selectedArchiving.value = null
  if (administrative) {
    applySharedProjection(context, toSharedDraftMontagem(detail as DraftMontagemAdmin))
    applyAdministrativeMetadata(context, detail as DraftMontagemAdmin)
  } else {
    applySharedProjection(context, detail)
  }
  return true
}

function applySharedProjection(context: DraftUpdateContext, montagem: DraftMontagem) {
  if (!isCurrentUpdate(context) || montagem.id !== context.draftId) return false
  if (montagem.versaoEstado <= highestSharedVersion) return false

  highestSharedVersion = montagem.versaoEstado
  applyPublicMontagemState(montagem)
  if (
    boardDirty.value
    && montagem.versaoEstado > boardBaseVersion.value
    && montagem.versaoEstado !== acceptedLayoutSaveInProgress
  ) queueCanonicalSnapshot(montagem)
  return true
}

function applyAdministrativeMetadata(context: DraftUpdateContext, detail: DraftMontagemAdmin) {
  const sequence = context.personalizedSequence
  const current = selectedMontagem.value
  if (
    sequence === undefined
    || sequence <= lastPersonalizedSequence
    || !isActiveDraft(context.draftId, context.generation)
    || !current
    || current.id !== detail.id
    || detail.versaoEstado < highestSharedVersion
  ) return false

  lastPersonalizedSequence = sequence
  const shared = toSharedDraftMontagem(current as DraftMontagemAdmin)
  selectedMontagem.value = {
    ...shared,
    discordGuildId: detail.discordGuildId,
    discordPresenceMessageId: detail.discordPresenceMessageId,
    presencas: shared.presencas.map((presence) => ({
      ...detail.presencas.find((item) => item.id === presence.id),
      ...presence,
    })),
    substituicoes: shared.substituicoes.map((substitution) => ({
      ...detail.substituicoes.find((item) => item.timeId === substitution.timeId
        && item.jogadorSaiuId === substitution.jogadorSaiuId
        && item.reservaEntrouId === substitution.reservaEntrouId),
      ...substitution,
    })),
    publicacoesDiscord: shared.publicacoesDiscord?.map((publication) => ({
      ...detail.publicacoesDiscord.find((item) => item.tipo === publication.tipo),
      ...publication,
    })) ?? [],
    acoesAdministrativas: detail.acoesAdministrativas,
    capitaesElegiveisIds: detail.capitaesElegiveisIds,
    capitaesElegiveisSubstituicaoIds: detail.capitaesElegiveisSubstituicaoIds,
    motivoCancelamento: detail.motivoCancelamento,
  } as DraftMontagemAdmin
  return true
}

function toSharedDraftMontagem(detail: DraftMontagemAdmin): DraftMontagem {
  const shared = {
    ...detail,
    presencas: detail.presencas.map((presence) => omitAdministrativeFields(presence, ['discordUserId'])),
    substituicoes: detail.substituicoes.map((substitution) => omitAdministrativeFields(substitution, ['motivo', 'responsavelUsuarioId'])),
    publicacoesDiscord: (detail.publicacoesDiscord ?? []).map((publication) => omitAdministrativeFields(publication, [
      'id', 'guildId', 'channelId', 'messageId', 'ultimoErroCodigo', 'claimId', 'claimExpiraEm', 'publicadaEm', 'ultimaTentativaEm',
    ])),
  } as Record<string, unknown>
  for (const key of ['discordGuildId', 'discordPresenceMessageId', 'acoesAdministrativas', 'capitaesElegiveisIds', 'capitaesElegiveisSubstituicaoIds', 'motivoCancelamento']) {
    delete shared[key]
  }
  return shared as unknown as DraftMontagem
}

function omitAdministrativeFields(value: object, keys: string[]) {
  const shared = { ...value } as Record<string, unknown>
  for (const key of keys) delete shared[key]
  return shared
}

function dropAdministrativeState() {
  const current = selectedMontagem.value
  if (!current) return
  selectedMontagem.value = toSharedDraftMontagem(current as DraftMontagemAdmin)
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
    await captureMutationError(error, context)
  } finally {
    if (canReleaseMutation(context)) {
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
    await captureMutationError(error, context)
  } finally {
    if (canReleaseMutation(context)) saving.value = false
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

function requestPresenceReopen() {
  const draft = selectedMontagem.value
  if (saving.value || !draft || !preparationCapabilities.value.canReopenPresence) return
  pendingReasonAction.value = { type: 'reopenPresence', draftName: draft.nome }
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
    await captureMutationError(error, context)
  } finally {
    if (canReleaseMutation(context)) {
      saving.value = false
      if (completed) await restoreStageFocus()
    }
  }
}

async function chooseMode(modo: DraftMontagemModo) {
  if (saving.value || !preparationCapabilities.value.canChooseMode) return
  const context = beginSelectedDraftUpdate()
  if (!context) return

  let completed = false
  saving.value = true
  errors.value = []
  try {
    const montagem = await chooseDraftMontagemMode(context.draftId, modo)
    if (await applyMutationProjection(context, montagem)) {
      notification.value = t('drafts.messages.modeSelected')
      completed = true
    }
  } catch (error) {
    await captureMutationError(error, context)
  } finally {
    if (canReleaseMutation(context)) {
      saving.value = false
      if (completed) await restoreStageFocus()
    }
  }
}

function toggleCaptainSelection(jogadorId: string) {
  if (
    saving.value
    || !canManageDraftCycle.value
    || selectedMontagem.value?.status !== DraftMontagemStatusValues.PresencaEncerrada
    || (selectedMontagem.value.cicloVersao === 'ModoPosPresenca' && selectedMontagem.value.modo !== 'TempoReal')
    || !selectableCaptainIds.value.includes(jogadorId)
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
    || !canManageDraftCycle.value
    || selectedMontagem.value?.status !== DraftMontagemStatusValues.PresencaEncerrada
    || (selectedMontagem.value.cicloVersao === 'ModoPosPresenca' && selectedMontagem.value.modo !== 'TempoReal')
    || captainSelection.value.length !== selectedMontagem.value.quantidadeTimes
    || captainSelection.value.some((id) => !selectableCaptainIds.value.includes(id))
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
    await captureMutationError(error, context)
  } finally {
    if (canReleaseMutation(context)) {
      saving.value = false
      if (completed) await restoreStageFocus()
    }
  }
}

async function drawPickOrder() {
  if (
    saving.value
    || !selectedMontagem.value
    || selectedMontagem.value.status !== DraftMontagemStatusValues.CapitaesDefinidos
    || !canManageDraftCycle.value
    || (selectedMontagem.value.cicloVersao === 'ModoPosPresenca' && selectedMontagem.value.modo !== 'TempoReal')
  ) return
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
    await captureMutationError(error, context)
  } finally {
    if (canReleaseMutation(context)) {
      saving.value = false
      if (completed) await restoreStageFocus()
    }
  }
}

function initializeGenerationRequests() {
  canonicalRequestController = new AbortController()
  conflictRequestController = new AbortController()
}

function abortGenerationRequests() {
  canonicalRequestController?.abort()
  adminRequestController?.abort()
  conflictRequestController?.abort()
  canonicalRequestController = null
  adminRequestController = null
  conflictRequestController = null
}

async function connectRealtime(id: string, generation: number) {
  if (!isActiveDraft(id, generation)) return
  connectionStatus.value = 'reconnecting'
  const connection = new DraftMontagemRealtimeConnection(id)
  realtimeConnection.value = connection
  await connection.connect(
    async (snapshot) => {
      if (!isActiveDraft(id, generation)) return
      const eventContext = beginDraftUpdate(id, generation, 'passive')
      if (eventContext) applySharedProjection(eventContext, snapshot.montagem)
      try {
        if (await loadPersonalizedRealtimeState(id, generation)) scheduleAdministrativeDetail(id, generation)
      } catch {
        // Keep the last personalized projection if its refresh fails.
      }
    },
    async () => {
      if (!isActiveDraft(id, generation)) return
      try {
        const loaded = await loadPersonalizedRealtimeState(id, generation)
        if (!loaded || !isActiveDraft(id, generation)) {
          startFallback(id, generation)
          return
        }
        connectionStatus.value = 'connected'
        stopFallback()
        scheduleAdministrativeDetail(id, generation)
      } catch {
        if (isActiveDraft(id, generation)) startFallback(id, generation)
      }
    },
    async (archivedId) => {
      await handleDraftArchived(archivedId)
    },
    (status) => {
      if (!isActiveDraft(id, generation)) return
      connectionStatus.value = status
      startFallback(id, generation, status === 'fallback')
    },
  )
  if (!isActiveDraft(id, generation)) {
    if (realtimeConnection.value === connection) realtimeConnection.value = null
    await connection.disconnect()
  }
}

function startFallback(id: string, generation: number, markFallback = true) {
  if (!isActiveDraft(id, generation) || connectionStatus.value === 'connected') return
  if (markFallback) connectionStatus.value = 'fallback'
  if (fallbackTimer !== null) return
  fallbackTimer = globalThis.setInterval(() => {
    if (isActiveDraft(id, generation) && connectionStatus.value !== 'connected') connectionStatus.value = 'fallback'
    void runFallbackRequest(id, generation)
  }, 3000)
}

async function runFallbackRequest(id: string, generation: number) {
  if (!isActiveDraft(id, generation) || connectionStatus.value === 'connected' || fallbackRequestInFlight) return
  fallbackRequestInFlight = true
  const controller = new AbortController()
  fallbackRequestController = controller
  controller.signal.addEventListener('abort', () => releaseFallbackRequest(controller), { once: true })
  const timeout = globalThis.setTimeout(() => controller.abort(), 2000)
  try {
    if (await loadPersonalizedRealtimeState(id, generation, controller.signal)) scheduleAdministrativeDetail(id, generation)
  } catch {
    // A failed fallback GET preserves the last accepted state and degraded health.
  } finally {
    globalThis.clearTimeout(timeout)
    releaseFallbackRequest(controller)
  }
}

function releaseFallbackRequest(controller: AbortController) {
  if (fallbackRequestController !== controller) return
  fallbackRequestController = null
  fallbackRequestInFlight = false
}

function stopFallback() {
  if (fallbackTimer !== null) {
    globalThis.clearInterval(fallbackTimer)
    fallbackTimer = null
  }
  fallbackRequestController?.abort()
  fallbackRequestController = null
  fallbackRequestInFlight = false
}

function applyMontagemState(montagem: DraftMontagem) {
  selectedMontagem.value = montagem
  visualMontagens.value = visualMontagens.value.map((item) =>
    item.id === montagem.id
      ? {
          ...item,
          status: montagem.status,
          modo: montagem.modo,
          cicloVersao: montagem.cicloVersao,
          quantidadeTimes: montagem.quantidadeTimes,
          quantidadeReservas: montagem.quantidadeReservas,
          arquivado: montagem.arquivado,
          versaoEstado: montagem.versaoEstado,
          dataAtualizacao: montagem.dataAtualizacao,
        }
      : item,
  )
}

async function disconnectRealtime(abortRequests = true) {
  if (abortRequests) abortGenerationRequests()
  stopFallback()
  const connection = realtimeConnection.value
  realtimeConnection.value = null
  await connection?.disconnect()
}

async function saveMontagem(payload: DraftMontagemPayload) {
  if (!canManageDraftCycle.value) {
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
  if (saving.value || requiresLayoutReconciliation.value || !canManageDraftCycle.value || selectedMontagem.value?.status !== DraftMontagemStatusValues.Aberta || selectedMontagem.value.modo !== 'Manual') return
  const context = beginSelectedDraftUpdate()
  if (!context) return
  const layoutSave = { draftId: context.draftId, generation: context.generation, baseVersion: payload.versaoEstado }
  saving.value = true
  outstandingLayoutSave = layoutSave
  acceptedSaveVersion.value = null
  errors.value = []
  try {
    const montagem = await saveDraftMontagemLayout(context.draftId, payload)
    if (outstandingLayoutSave !== layoutSave || !isCurrentUpdate(context)) return
    acceptedLayoutSaveInProgress = montagem.versaoEstado
    const projectionApplied = await applyMutationProjection(context, montagem)
    if (!projectionApplied && (
      !isCurrentUpdate(context)
      || selectedMontagem.value?.id !== montagem.id
      || selectedMontagem.value.versaoEstado !== montagem.versaoEstado
    )) return
    if (!projectionApplied) scheduleAdministrativeDetail(context.draftId, context.generation)
    if (pendingCanonicalSnapshot.value && pendingCanonicalSnapshot.value.versaoEstado <= montagem.versaoEstado) {
      pendingCanonicalSnapshot.value = null
      requiresLayoutReconciliation.value = false
    }
    acceptedSaveVersion.value = montagem.versaoEstado
    await nextTick()
    await loadVisualMontagens()
    notification.value = t('drafts.messages.layoutSaved')
  } catch (error) {
    await captureMutationError(error, context)
  } finally {
    if (outstandingLayoutSave === layoutSave) {
      acceptedLayoutSaveInProgress = null
      outstandingLayoutSave = null
      await nextTick()
      if (boardDirty.value && pendingCanonicalSnapshot.value) {
        requiresLayoutReconciliation.value = true
        requestLayoutIntent('remote-update')
      }
    }
    if (canReleaseMutation(context)) saving.value = false
  }
}

function invalidateOutstandingLayoutSave() {
  outstandingLayoutSave = null
  acceptedLayoutSaveInProgress = null
}

async function startRealtime() {
  if (
    saving.value
    || !canManageDraftCycle.value
    || !selectedMontagem.value
    || (selectedMontagem.value.cicloVersao === 'ModoPosPresenca'
      ? selectedMontagem.value.status !== DraftMontagemStatusValues.OrdemDefinida || selectedMontagem.value.modo !== 'TempoReal'
      : selectedMontagem.value.status !== DraftMontagemStatusValues.Aberta || selectedMontagem.value.modo !== 'Manual')
  ) return
  const context = beginSelectedDraftUpdate(true)
  if (!context) return

  saving.value = true
  errors.value = []
  try {
    const state = await startDraftMontagemRealtime(context.draftId)
    if (await applyMutationRealtimeState(context, state)) notification.value = t('drafts.realtime.started')
  } catch (error) {
    await captureMutationError(error, context)
  } finally {
    if (canReleaseMutation(context)) saving.value = false
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
  const context = beginSelectedDraftUpdate(true)
  if (!context) return

  let completed = false
  saving.value = true
  errors.value = []
  try {
    const state = await registerDraftMontagemPick(context.draftId, jogadorId)
    completed = await applyMutationRealtimeState(context, state)
  } catch (error) {
    await captureMutationError(error, context)
  } finally {
    if (canReleaseMutation(context)) {
      saving.value = false
      if (completed) await restoreStageFocus()
    }
  }
}

async function substituteReserve(payload: DraftMontagemSubstituicaoPayload, complete?: (success: boolean) => void) {
  const current = selectedMontagem.value
  const team = current?.times.find((item) => item.id === payload.timeId)
  const outgoingPlayer = team?.jogadores.find((player) => player.jogadorId === payload.jogadorSaiuId)
  const captainLeaving = current?.cicloVersao === 'ModoPosPresenca'
    && Boolean(outgoingPlayer?.capitao || team?.capitaoId === payload.jogadorSaiuId)
  const resultingCaptainIds = new Set([
    ...(team?.jogadores.filter((player) => player.jogadorId !== payload.jogadorSaiuId).map((player) => player.jogadorId) ?? []),
    payload.reservaEntrouId,
  ])
  const substitutionStatusAllowed = current?.status === DraftMontagemStatusValues.Aberta
    || (current?.cicloVersao === 'ModoPosPresenca'
      && current.modo === 'TempoReal'
      && (current.status === DraftMontagemStatusValues.CapitaesDefinidos
        || current.status === DraftMontagemStatusValues.OrdemDefinida))
  if (
    saving.value
    || !canManageDraftCycle.value
    || !current
    || !substitutionStatusAllowed
    || !team?.jogadores.some((player) => player.jogadorId === payload.jogadorSaiuId)
    || !current.reservas.some((player) => player.jogadorId === payload.reservaEntrouId && player.estado === DraftMontagemEstadoValues.Reserva)
    || (captainLeaving && (!payload.novoCapitaoId || !substitutionEligibleCaptainIds.value.includes(payload.novoCapitaoId) || !resultingCaptainIds.has(payload.novoCapitaoId)))
    || (!captainLeaving && current.cicloVersao === 'ModoPosPresenca' && Boolean(payload.novoCapitaoId))
  ) {
    complete?.(false)
    return
  }
  const context = beginSelectedDraftUpdate(true)
  if (!context) {
    complete?.(false)
    return
  }

  let completed = false
  saving.value = true
  errors.value = []
  try {
    const state = await substituteDraftMontagemReserve(context.draftId, payload)
    if (await applyMutationRealtimeState(context, state)) {
      completed = state.montagem.versaoEstado > current.versaoEstado
      if (completed) notification.value = t('drafts.realtime.reserveSubstituted')
    }
  } catch (error) {
    await captureMutationError(error, context)
  } finally {
    if (canReleaseMutation(context)) saving.value = false
    complete?.(completed)
  }
}

async function drawMontagemCaptains() {
  if (saving.value || !canManageDraftCycle.value || selectedMontagem.value?.status !== DraftMontagemStatusValues.Aberta || selectedMontagem.value.modo !== 'Manual') return
  const context = beginSelectedDraftUpdate()
  if (!context) return
  saving.value = true
  try {
    const montagem = await drawDraftMontagemCaptains(context.draftId)
    if (await applyMutationProjection(context, montagem)) notification.value = t('drafts.messages.captainsDrawn')
  } catch (error) {
    await captureMutationError(error, context)
  } finally {
    if (canReleaseMutation(context)) saving.value = false
  }
}

async function finalizeMontagem() {
  const current = selectedMontagem.value
  if (
    saving.value
    || !canManageDraftCycle.value
    || current?.status !== DraftMontagemStatusValues.Aberta
    || current.modo !== 'Manual'
    || (current.cicloVersao === 'ModoPosPresenca' && !(
      current.times.length === current.quantidadeTimes
      && current.times.every((team) => team.jogadores.length === current.tamanhoEquipe)
      && current.livres.length === 0
    ))
  ) return
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
    await captureMutationError(error, context)
  } finally {
    if (canReleaseMutation(context)) {
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

async function resetFilters() {
  searchTerm.value = ''
  selectedStatus.value = ''
  if (canArchiveDrafts.value) await updateArchivedFilter(false)
  else await loadVisualMontagens()
}

function updateStatusFilter(value: DraftMontagemStatus | '') {
  if (selectedMontagem.value && value && selectedMontagem.value.status !== value) {
    requestLayoutIntent('draft-removed', () => {
      selectedStatus.value = value
      return loadVisualMontagens()
    })
    return
  }
  selectedStatus.value = value
  void loadVisualMontagens()
}

function updateSearchFilter(value: string) {
  const currentName = visualMontagens.value.find((draft) => draft.id === selectedDraftId.value)?.nome ?? selectedMontagem.value?.nome
  if (currentName && value.trim() && !currentName.toLowerCase().includes(value.trim().toLowerCase())) {
    requestLayoutIntent('draft-removed', () => { searchTerm.value = value })
    return
  }
  searchTerm.value = value
}

async function updateArchivedFilter(value: boolean) {
  if (!canArchiveDrafts.value) return
  includeArchived.value = value
  if (!value && selectedMontagem.value?.arquivado) {
    await removeArchivedAndReconcile(selectedMontagem.value.id, visualMontagens.value.findIndex((draft) => draft.id === selectedMontagem.value?.id))
    return
  }
  await loadVisualMontagens()
}

function requestDraftArchive() {
  const draft = selectedMontagem.value
  if (saving.value || !draft || draft.arquivado || !canArchiveDrafts.value) return
  requestLayoutIntent('archive', () => {
    pendingReasonAction.value = {
      type: 'archiveDraft',
      draftName: draft.nome,
      cancelsActiveDraft: draft.status !== DraftMontagemStatusValues.Finalizada && draft.status !== DraftMontagemStatusValues.Cancelada,
    }
  })
}

function requestDraftRestore() {
  const draft = selectedMontagem.value
  if (saving.value || !draft?.arquivado || !canArchiveDrafts.value) return
  pendingReasonAction.value = { type: 'restoreDraft', draftName: draft.nome }
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
  if (action.publicationType === 'Cancelamento') {
    void republishArchivedCancellation(action.publicationStatus)
    return
  }
  pendingReasonAction.value = {
    type: 'republishDiscord',
    publicationType: action.publicationType,
    publicationStatus: currentStatus,
  }
}

async function republishArchivedCancellation(publicationStatus: DraftMontagemPublicacaoDiscordStatus | string | null) {
  if (
    saving.value
    || !canArchiveDrafts.value
    || !selectedMontagem.value?.arquivado
    || publicationStatus !== discordPublicationStatus('Cancelamento')
    || !discordRepublishableTypes.value.includes('Cancelamento')
  ) return
  const context = beginSelectedDraftUpdate()
  if (!context) return

  saving.value = true
  errors.value = []
  try {
    await republishArchivedDraftCancellation(context.draftId)
    if (!isCurrentUpdate(context)) return
    await openMontagem(context.draftId)
    notification.value = t('drafts.publication.republishRequested')
    await restoreStageFocus()
  } catch (error) {
    if (isActiveDraft(context.draftId, context.generation)) {
      if (error instanceof DraftMontagemServiceError) await handleArchiveError(error, context.draftId)
      else captureError(error)
    }
  } finally {
    if (canReleaseMutation(context)) saving.value = false
  }
}

function isReasonActionAvailable(action: DraftReasonDialogAction) {
  if (action.type === 'archiveDraft') return canArchiveDrafts.value && Boolean(selectedMontagem.value && !selectedMontagem.value.arquivado)
  if (action.type === 'restoreDraft') return canArchiveDrafts.value && selectedMontagem.value?.arquivado === true
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
  if (action.type === 'reopenPresence') return preparationCapabilities.value.canReopenPresence
  return discordRepublishableTypes.value.includes(action.publicationType)
    && discordPublicationStatus(action.publicationType) === action.publicationStatus
}

async function confirmReasonAction(reason: string | null) {
  if (saving.value) return

  const action = pendingReasonAction.value
  if (!action || !selectedMontagem.value) return
  const archiveAction = action.type === 'archiveDraft' || action.type === 'restoreDraft'
    || (action.type === 'republishDiscord' && action.publicationType === 'Cancelamento')
  if ((archiveAction ? !canArchiveDrafts.value : !canManageDrafts.value) || !isReasonActionAvailable(action)) {
    pendingReasonAction.value = null
    return
  }
  const context = beginSelectedDraftUpdate()
  if (!context) return

  saving.value = true
  try {
    if (action.type === 'archiveDraft') {
      if (!reason) return
      const currentIndex = visualMontagens.value.findIndex((draft) => draft.id === context.draftId)
      const draftName = selectedMontagem.value.nome
      await archiveDraftMontagem(context.draftId, reason, selectedMontagem.value.versaoEstado)
      notification.value = t('drafts.archive.archived', { name: draftName })
      if (includeArchived.value) {
        await loadVisualMontagens()
        if (selectedDraftId.value === context.draftId) await openMontagem(context.draftId)
      } else {
        await removeArchivedAndReconcile(context.draftId, currentIndex, true)
      }
    } else if (action.type === 'restoreDraft') {
      const currentIndex = visualMontagens.value.findIndex((draft) => draft.id === context.draftId)
      const draftName = selectedMontagem.value.nome
      await restoreDraftMontagem(context.draftId, selectedMontagem.value.versaoEstado)
      await loadVisualMontagens()
      if (selectedDraftId.value === context.draftId) {
        if (visualMontagens.value.some((draft) => draft.id === context.draftId)) await openMontagem(context.draftId)
        else await removeArchivedAndReconcile(context.draftId, currentIndex, false, false)
      }
      notification.value = t('drafts.archive.restored', { name: draftName })
    } else if (action.type === 'cancelDraft') {
      if (!reason) return
      const montagem = await cancelDraftMontagem(context.draftId, reason)
      if (!(await applyMutationProjection(context, montagem))) return
      await loadVisualMontagens()
      notification.value = t('drafts.canceled', { name: montagem.nome })
    } else if (action.type === 'addManualPresence') {
      if (!reason) return
      const montagem = await addManualDraftMontagemPresence(context.draftId, action.jogadorId, reason)
      if (!(await applyMutationProjection(context, montagem))) return
      selectedManualPresencePlayerId.value = ''
      await loadEligibleManualPresencePlayers()
      notification.value = t('drafts.presence.manualAdded')
    } else if (action.type === 'removeManualPresence') {
      if (!reason) return
      const montagem = await removeManualDraftMontagemPresence(context.draftId, action.jogadorId, reason)
      if (!(await applyMutationProjection(context, montagem))) return
      await loadEligibleManualPresencePlayers()
      notification.value = t('drafts.presence.manualRemoved')
    } else if (action.type === 'reopenPresence') {
      const montagem = await reopenDraftMontagemPresence(context.draftId)
      if (!(await applyMutationProjection(context, montagem))) return
      captainSelection.value = []
      notification.value = t('drafts.presence.reopened')
    } else {
      if (!reason) return
      const montagem = await republishDraftMontagemDiscordPublication(context.draftId, action.publicationType, reason)
      if (!(await applyMutationProjection(context, montagem))) return
      notification.value = t('drafts.publication.republishRequested')
    }
    if (pendingReasonAction.value === action) pendingReasonAction.value = null
  } catch (error) {
    if (isActiveDraft(context.draftId, context.generation)) {
      if (archiveAction && error instanceof DraftMontagemServiceError) {
        await handleArchiveError(error, context.draftId)
      } else {
        await captureMutationError(error, context)
      }
    }
  } finally {
    if (canReleaseMutation(context)) saving.value = false
  }
}

async function removeArchivedAndReconcile(draftId: string, previousIndex: number, restoreFocus = false, reload = true) {
  if (selectedDraftId.value === draftId && boardDirty.value) {
    requestLayoutIntent('draft-removed', () => removeArchivedAndReconcile(draftId, previousIndex, restoreFocus, reload))
    return
  }
  visualMontagens.value = visualMontagens.value.filter((draft) => draft.id !== draftId)
  if (selectedDraftId.value !== draftId) {
    await loadVisualMontagens()
    return
  }
  activeDraftId = null
  selectedDraftId.value = '__reconciling__'
  selectedMontagem.value = null
  selectedArchiving.value = null
  invalidateOutstandingLayoutSave()
  activeDraftGeneration++
  detailRequestVersion = 0
  await disconnectRealtime()
  if (reload) await loadVisualMontagens()
  selectedDraftId.value = null
  if (visualMontagens.value.length === 0) {
    saving.value = false
    if (restoreFocus) await restoreStageFocus()
    return
  }
  const next = visualMontagens.value[Math.min(Math.max(previousIndex, 0), visualMontagens.value.length - 1)]
  if (next) await openMontagem(next.id)
  if (restoreFocus) await restoreStageFocus()
}

function archiveActionLabel(type: string) {
  const key = `drafts.archive.actionTypes.${type}`
  return t(te(key) ? key : 'drafts.archive.actionTypes.unknown')
}

function administrativeActorLabel(action: DraftMontagemAcaoAdministrativa) {
  return action.responsavelTipo === 'System'
    ? t('drafts.audit.actor.system')
    : t('drafts.archive.responsible', { id: action.responsavelUsuarioId })
}

function formatArchiveDate(value: string) {
  return new Date(value).toLocaleString(locale.value)
}

async function handleDraftArchived(draftId: string) {
  const index = visualMontagens.value.findIndex((draft) => draft.id === draftId)
  if (index < 0 && selectedDraftId.value !== draftId) return
  if (includeArchived.value && canArchiveDrafts.value) {
    if (selectedDraftId.value === draftId && boardDirty.value) {
      deferredArchivedDraftId = draftId
      if (pendingLayoutIntent.value) {
        return
      }
      const current = selectedMontagem.value
      if (current && (!pendingCanonicalSnapshot.value || current.versaoEstado > pendingCanonicalSnapshot.value.versaoEstado)) {
        pendingCanonicalSnapshot.value = current
      }
      requiresLayoutReconciliation.value = true
      requestLayoutIntent('remote-update', () => reconcileDeferredArchivedDraft(draftId))
      return
    }
    deferredArchivedDraftId = draftId
    await reconcileDeferredArchivedDraft(draftId)
    return
  }
  await removeArchivedAndReconcile(draftId, index)
}

async function handleArchiveAccessDenied(inaccessibleId?: string | null, inaccessibleIndex?: number) {
  const archivedId = inaccessibleId ?? (selectedMontagem.value?.arquivado ? selectedMontagem.value.id : null)
  const archivedIndex = inaccessibleIndex ?? (archivedId ? visualMontagens.value.findIndex((draft) => draft.id === archivedId) : -1)
  archiveAccessDenied.value = true
  includeArchived.value = false
  selectedArchiving.value = null
  if (archivedId) await removeArchivedAndReconcile(archivedId, archivedIndex)
  else await loadVisualMontagens()
}

async function handleArchiveError(error: DraftMontagemServiceError, draftId: string) {
  if (error.status === 401) {
    errors.value = [t('drafts.archive.errors.unauthorized')]
    return
  }
  if (error.status === 403) {
    await handleArchiveAccessDenied()
    errors.value = [t('drafts.archive.errors.forbidden')]
    pendingReasonAction.value = null
    return
  }
  if (error.status === 409) {
    pendingReasonAction.value = null
    await loadVisualMontagens()
    if (visualMontagens.value.some((draft) => draft.id === draftId)) await openMontagem(draftId)
    errors.value = [t('drafts.archive.errors.conflict')]
    return
  }
  captureError(error)
}

async function restoreStageFocus() {
  await nextTick()
  if (workspaceHeader.value) await workspaceHeader.value.focusStage()
  else emptyWorkspace.value?.focus()
}

async function captureMutationError(error: unknown, context: DraftUpdateContext) {
  if (!isActiveDraft(context.draftId, context.generation)) return
  if (error instanceof DraftMontagemServiceError && error.status === 409) {
    context.holdSaving = true
    try {
      if (await reconcileMutationConflict(context)) {
        context.holdSaving = false
        scheduleAdministrativeDetail(context.draftId, context.generation)
      }
    } catch {
      // The mutation remains failed; the original conflict is still surfaced after reconciliation fails.
    }
    if (boardDirty.value && selectedMontagem.value?.id === context.draftId) {
      queueCanonicalSnapshot(selectedMontagem.value)
    }
  }
  if (isActiveDraft(context.draftId, context.generation)) captureError(error)
}

async function reconcileMutationConflict(mutationContext: DraftUpdateContext) {
  const signal = conflictRequestController?.signal
  if (!signal || signal.aborted) return false
  const acceptanceAtStart = canonicalAcceptanceId
  const context = beginDraftUpdate(mutationContext.draftId, mutationContext.generation, 'mutation', true)
  if (!context) return false
  const state = await getDraftMontagemRealtimeState(context.draftId, signal)
  if (!isCurrentUpdate(context) || state.montagem.id !== context.draftId) return false
  if (applyPersonalizedRealtimeState(context, state)) {
    canonicalAcceptanceId++
    return true
  }
  return canonicalAcceptanceId > acceptanceAtStart
}

function canReleaseMutation(context: DraftUpdateContext) {
  return isActiveDraft(context.draftId, context.generation) && !context.holdSaving
}

function captureError(error: unknown) {
  errors.value = error instanceof DraftMontagemServiceError ? error.errors : [t('drafts.errors.action')]
}

function requestOpenMontagem(id: string) {
  if (id === selectedDraftId.value) return
  requestLayoutIntent('switch-draft', () => openMontagem(id))
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
        <Button v-if="canManageDraftCycle" type="button" @click="openVisualSetup">{{ t('drafts.createWithIcon') }}</Button>
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
        :can-create="canManageDraftCycle"
        :can-include-archived="canArchiveDrafts"
        :include-archived="includeArchived"
        @update:search-term="updateSearchFilter"
        @update:selected-status="updateStatusFilter"
        @update:include-archived="updateArchivedFilter"
        @select="requestOpenMontagem"
        @reset="resetFilters"
        @retry="loadVisualMontagens"
        @create="openVisualSetup"
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
            <Button v-if="selectedMontagem.arquivado && canArchiveDrafts" data-testid="restore-draft" type="button" :disabled="saving" @click="requestDraftRestore">
              {{ t('drafts.archive.restore') }}
            </Button>
          </template>
          <template #secondary-actions>
          </template>
          <template #danger-action>
            <Button v-if="canManageDrafts && !selectedMontagem.arquivado && selectedMontagem.status !== DraftMontagemStatusValues.Finalizada && selectedMontagem.status !== DraftMontagemStatusValues.Cancelada" type="button" variant="destructive" :disabled="saving" @click="requestDraftCancellation">{{ t('common.cancel') }}</Button>
            <Button v-if="canArchiveDrafts && !selectedMontagem.arquivado" data-testid="archive-draft" type="button" variant="destructive" :disabled="saving" @click="requestDraftArchive">
              {{ t('drafts.archive.action') }}
            </Button>
          </template>
        </DraftWorkspaceHeader>

        <DraftPreparationPanel
          v-if="selectedMontagem && !selectedMontagem.arquivado && preparationStatuses.includes(selectedMontagem.status)"
          :draft="selectedMontagem"
          :confirmed-presences="confirmedPresences"
          :saving="saving"
          :can-confirm-presence="preparationCapabilities.canConfirmPresence"
          :can-cancel-presence="preparationCapabilities.canCancelPresence"
          :can-close-presence="preparationCapabilities.canClosePresence"
          :can-continue-manual-presence="preparationCapabilities.canContinueManualPresence"
          :can-manage-manual-presence="preparationCapabilities.canManageManualPresence"
          :can-choose-mode="preparationCapabilities.canChooseMode"
          :can-select-captains="preparationCapabilities.canSelectCaptains"
          :can-reopen-presence="preparationCapabilities.canReopenPresence"
          :can-define-captains="preparationCapabilities.canDefineCaptains"
          :can-draw-order="preparationCapabilities.canDrawOrder"
          :captain-selection="captainSelection"
          :eligible-captain-ids="selectableCaptainIds"
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
          @choose-mode="chooseMode"
          @toggle-captain="toggleCaptainSelection"
          @reopen-presence="requestPresenceReopen"
          @define-captains="defineCaptains"
          @draw-order="drawPickOrder"
        />
        <DraftDiscordPublicationPanel
          v-if="selectedMontagem && (canManageDrafts || (selectedMontagem.arquivado && canArchiveDrafts))"
          :publications="discordPublicationMatrix"
          :republishable-types="discordRepublishableTypes"
          :archived="selectedMontagem.arquivado"
          :saving="saving"
          @republish="requestDiscordRepublish"
        />
        <Alert v-if="boardDirty && requiresLayoutReconciliation" data-layout-reconciliation role="status">
          <AlertDescription>{{ t('drafts.unsavedLayout.reconciliationRequired') }}</AlertDescription>
          <Button data-testid="review-layout-update" type="button" variant="outline" @click="reviewLayoutUpdate">
            {{ t('drafts.unsavedLayout.reviewUpdate') }}
          </Button>
        </Alert>
        <DraftVisualBoard
          v-if="selectedMontagem && !selectedMontagem.arquivado && selectedMontagem.status !== DraftMontagemStatusValues.PresencaAberta && selectedMontagem.status !== DraftMontagemStatusValues.PresencaEncerrada"
          :montagem="selectedMontagem"
          :saving="saving"
          :can-manage="canManageDraftCycle"
          :current-player-id="currentPlayerId"
          :can-current-user-pick="canCurrentUserPick"
          :server-clock-offset-ms="serverClockOffsetMs"
          :eligible-captain-ids="substitutionEligibleCaptainIds"
          :canonical-reset-token="canonicalResetToken"
          :accepted-save-version="acceptedSaveVersion"
          @dirty-change="handleBoardDirtyChange"
          @save="saveMontagemLayout"
          @start-realtime="startRealtime"
          @pick="pickRealtime"
          @substitute-reserve="substituteReserve"
          @draw-captains="drawMontagemCaptains"
          @finalize="finalizeMontagem"
          @cancel="requestDraftCancellation"
        />
        <section v-if="selectedMontagem?.arquivado" class="draft-empty-card draft-archive-audit" data-archived-workspace>
          <h2>{{ t('drafts.archive.historyTitle') }}</h2>
          <p>{{ t('drafts.archive.readOnly') }}</p>
          <p v-if="selectedArchiving?.arquivadoEm">{{ t('drafts.archive.archivedAt', { date: formatArchiveDate(selectedArchiving.arquivadoEm) }) }}</p>
          <p v-if="selectedArchiving?.arquivadoPorUsuarioId" class="draft-archive-audit__value" data-archive-value>{{ t('drafts.archive.responsible', { id: selectedArchiving.arquivadoPorUsuarioId }) }}</p>
          <p v-if="selectedArchiving?.motivoArquivamento" class="draft-archive-audit__value" data-archive-value>{{ t('drafts.archive.reason', { reason: selectedArchiving.motivoArquivamento }) }}</p>
          <ol v-if="selectedArchiving?.acoes.length">
            <li v-for="action in selectedArchiving.acoes" :key="action.id">
              <strong>{{ archiveActionLabel(action.tipo) }}</strong>
              <span>{{ t('drafts.archive.eventAt', { date: formatArchiveDate(action.registradoEm) }) }}</span>
              <span class="draft-archive-audit__value" data-archive-value>
                {{ administrativeActorLabel(action) }}
              </span>
              <span v-if="action.motivo" class="draft-archive-audit__value" data-archive-value>{{ t('drafts.archive.eventReason', { reason: action.motivo }) }}</span>
            </li>
          </ol>
        </section>
        <section v-else-if="!selectedMontagem" ref="emptyWorkspace" class="draft-empty-card" data-empty-workspace tabindex="-1">
          <h2>{{ t('drafts.noSelectionTitle') }}</h2>
          <p>{{ t('drafts.noSelectionDescription') }}</p>
        </section>
      </div>
    </section>

    <DraftVisualSetup
      :open="visualSetupOpen"
      :players="players"
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
    <DraftUnsavedLayoutDialog
      :open="pendingLayoutIntent !== null"
      :intent="pendingLayoutIntent"
      @keep-editing="keepEditingLayout"
      @discard="discardLayout"
    />
  </PageFrame>
</template>

<style scoped>
.draft-archive-audit,
.draft-archive-audit li,
.draft-archive-audit__value {
  min-width: 0;
}

.draft-archive-audit ol,
.draft-archive-audit li {
  display: grid;
  gap: var(--space-xs);
}

.draft-archive-audit__value {
  overflow-wrap: anywhere;
}
</style>
