<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'

import DraftVisualBoard from '@/components/drafts/visual/DraftVisualBoard.vue'
import DraftVisualSetup from '@/components/drafts/visual/DraftVisualSetup.vue'
import PendingPlayerProfileNotice from '@/components/users/PendingPlayerProfileNotice.vue'
import { DRAFT_MONTAGEM_STATUS_OPTIONS } from '@/constants/draftMontagemStatus'
import { Permissions } from '@/constants/permissions'
import { useAuthState } from '@/services/authState'
import { listEligibleCaptains, listPlayers, type Player } from '@/services/players'
import {
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
  getDraftMontagemRealtimeState,
  listDraftMontagens,
  registerDraftMontagemPick,
  saveDraftMontagemLayout,
  startDraftMontagemRealtime,
  substituteDraftMontagemReserve,
} from '@/services/draftMontagens'
import { DraftMontagemRealtimeConnection } from '@/services/draftMontagemRealtime'
import { DraftMontagemOrdemEscolhaModoValues, DraftMontagemPresencaStatusValues, DraftMontagemStatusValues } from '@/constants/draftMontagem'
import type { DraftMontagem, DraftMontagemLayoutPayload, DraftMontagemPayload, DraftMontagemResumo, DraftMontagemStatus } from '@/types/draftMontagem'

const players = ref<Player[]>([])
const { t } = useI18n()
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

const captainSelection = ref<string[]>([])
const statusOptions = DRAFT_MONTAGEM_STATUS_OPTIONS
const canManageDrafts = computed(() => auth.hasPermission(Permissions.CanManageDrafts))
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
    visualMontagens.value = await listDraftMontagens()
    if (!selectedMontagem.value && visualMontagens.value[0]) {
      await openMontagem(visualMontagens.value[0].id)
    }
  } catch (error) {
    captureError(error)
  } finally {
    loading.value = false
  }
}

async function openMontagem(id: string) {
  saving.value = true
  errors.value = []
  try {
    selectedMontagem.value = await getDraftMontagemById(id)
    captainSelection.value = []
    await connectRealtime(id)
  } catch (error) {
    captureError(error)
  } finally {
    saving.value = false
  }
}

async function confirmPresence() {
  if (!selectedMontagem.value) return
  saving.value = true
  try {
    selectedMontagem.value = await confirmDraftMontagemPresence(selectedMontagem.value.id)
    notification.value = t('drafts.presence.confirmed')
  } catch (error) {
    captureError(error)
  } finally {
    saving.value = false
  }
}

async function cancelPresence() {
  if (!selectedMontagem.value) return
  saving.value = true
  try {
    selectedMontagem.value = await cancelDraftMontagemPresence(selectedMontagem.value.id)
    notification.value = t('drafts.presence.cancelled')
  } catch (error) {
    captureError(error)
  } finally {
    saving.value = false
  }
}

async function closePresence(continueWithLess = false) {
  if (!selectedMontagem.value || !canManageDrafts.value) return
  saving.value = true
  try {
    selectedMontagem.value = await closeDraftMontagemPresence(selectedMontagem.value.id, continueWithLess, selectedMontagem.value.tamanhoEquipe)
    notification.value = t('drafts.presence.closed')
  } catch (error) {
    captureError(error)
  } finally {
    saving.value = false
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
  if (!selectedMontagem.value || !canManageDrafts.value) return
  saving.value = true
  try {
    selectedMontagem.value = await defineDraftMontagemCaptains(selectedMontagem.value.id, captainSelection.value)
    notification.value = t('drafts.presence.captainsDefined')
  } catch (error) {
    captureError(error)
  } finally {
    saving.value = false
  }
}

async function drawPickOrder() {
  if (!selectedMontagem.value || !canManageDrafts.value) return
  saving.value = true
  try {
    selectedMontagem.value = await defineDraftMontagemPickOrder(selectedMontagem.value.id, DraftMontagemOrdemEscolhaModoValues.Sorteado)
    notification.value = t('drafts.presence.orderDefined')
  } catch (error) {
    captureError(error)
  } finally {
    saving.value = false
  }
}

async function connectRealtime(id: string) {
  await disconnectRealtime()
  try {
    const state = await getDraftMontagemRealtimeState(id)
    applyRealtimeState(state.montagem)
  } catch {
    // The regular detail endpoint already loaded the board; realtime state errors are shown by later actions.
  }

  realtimeConnection.value = new DraftMontagemRealtimeConnection(id)
  await realtimeConnection.value.connect((state) => {
    applyRealtimeState(state.montagem)
  })
}

function applyRealtimeState(montagem: DraftMontagem) {
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
  if (!realtimeConnection.value) {
    return
  }

  await realtimeConnection.value.disconnect()
  realtimeConnection.value = null
}

async function saveMontagem(payload: DraftMontagemPayload) {
  if (!canManageDrafts.value) {
    return
  }

  saving.value = true
  serviceErrors.value = []
  try {
    selectedMontagem.value = await createDraftMontagem(payload)
    await loadVisualMontagens()
    notification.value = t('drafts.created', { name: selectedMontagem.value.nome })
    visualSetupOpen.value = false
  } catch (error) {
    serviceErrors.value = error instanceof DraftMontagemServiceError ? error.errors : [t('drafts.errors.create')]
  } finally {
    saving.value = false
  }
}

async function saveMontagemLayout(payload: DraftMontagemLayoutPayload) {
  if (!selectedMontagem.value || !canManageDrafts.value) {
    return
  }
  saving.value = true
  errors.value = []
  try {
    selectedMontagem.value = await saveDraftMontagemLayout(selectedMontagem.value.id, payload)
    await loadVisualMontagens()
    notification.value = t('drafts.messages.layoutSaved')
  } catch (error) {
    captureError(error)
  } finally {
    saving.value = false
  }
}

async function startRealtime() {
  if (!selectedMontagem.value || !canManageDrafts.value) {
    return
  }

  saving.value = true
  errors.value = []
  try {
    const state = await startDraftMontagemRealtime(selectedMontagem.value.id)
    applyRealtimeState(state.montagem)
    notification.value = t('drafts.realtime.started')
  } catch (error) {
    captureError(error)
  } finally {
    saving.value = false
  }
}

async function pickRealtime(jogadorId: string) {
  if (!selectedMontagem.value) {
    return
  }

  saving.value = true
  errors.value = []
  try {
    const state = await registerDraftMontagemPick(selectedMontagem.value.id, jogadorId)
    applyRealtimeState(state.montagem)
  } catch (error) {
    captureError(error)
  } finally {
    saving.value = false
  }
}

async function substituteReserve(payload: { timeId: string; jogadorSaiuId: string; reservaEntrouId: string; motivo?: string | null }) {
  if (!selectedMontagem.value || !canManageDrafts.value) {
    return
  }

  saving.value = true
  errors.value = []
  try {
    const state = await substituteDraftMontagemReserve(selectedMontagem.value.id, payload)
    applyRealtimeState(state.montagem)
    notification.value = t('drafts.realtime.reserveSubstituted')
  } catch (error) {
    captureError(error)
  } finally {
    saving.value = false
  }
}

async function drawMontagemCaptains() {
  if (!selectedMontagem.value || !canManageDrafts.value) {
    return
  }
  saving.value = true
  try {
    selectedMontagem.value = await drawDraftMontagemCaptains(selectedMontagem.value.id)
    notification.value = t('drafts.messages.captainsDrawn')
  } catch (error) {
    captureError(error)
  } finally {
    saving.value = false
  }
}

async function finalizeMontagem() {
  if (!selectedMontagem.value || !canManageDrafts.value) {
    return
  }
  saving.value = true
  try {
    selectedMontagem.value = await finalizeDraftMontagem(selectedMontagem.value.id)
    await loadVisualMontagens()
    notification.value = t('drafts.messages.finished')
  } catch (error) {
    captureError(error)
  } finally {
    saving.value = false
  }
}

async function cancelMontagem() {
  if (!selectedMontagem.value || !canManageDrafts.value) {
    return
  }

  saving.value = true
  try {
    selectedMontagem.value = await cancelDraftMontagem(selectedMontagem.value.id, t('drafts.cancelReason'))
    await loadVisualMontagens()
    notification.value = t('drafts.canceled', { name: selectedMontagem.value.nome })
  } catch (error) {
    captureError(error)
  } finally {
    saving.value = false
  }
}

function resetFilters() {
  searchTerm.value = ''
  selectedStatus.value = ''
}

function captureError(error: unknown) {
  errors.value = error instanceof DraftMontagemServiceError ? error.errors : [t('drafts.errors.action')]
}
</script>

<template>
  <section class="players-page drafts-page">
    <div v-if="notification" class="app-toast app-toast--success" role="status" aria-live="polite">
      <span class="app-toast__indicator" aria-hidden="true" />
      <p>{{ notification }}</p>
      <button type="button" :aria-label="t('common.closeNotification')" @click="notification = null">×</button>
    </div>

    <header class="players-hero drafts-hero">
      <div>
        <p class="page-kicker">{{ t('drafts.kicker') }}</p>
        <h1>{{ t('drafts.title') }}</h1>
        <p>{{ t('drafts.visualSubtitle') }}</p>
      </div>
      <div class="draft-hero-actions">
        <span class="page-hero__metric">{{ t('drafts.metrics.visible', { total: filteredDrafts.length }) }}</span>
        <button v-if="canManageDrafts" type="button" @click="visualSetupOpen = true">{{ t('drafts.createWithIcon') }}</button>
      </div>
    </header>

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
        <select v-model="selectedStatus">
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
          <span>{{ t('drafts.cardSummary', { teams: draft.quantidadeTimes, reserves: draft.quantidadeReservas }) }}</span>
        </button>
        <div v-if="!loading && !filteredDrafts.length" class="draft-empty-card">
          <h2>{{ t('drafts.emptyTitle') }}</h2>
          <p>{{ t('drafts.emptyDescription') }}</p>
        </div>
      </aside>

      <main class="draft-main">
        <section v-if="selectedMontagem" class="panel-card presence-panel">
          <div>
            <span class="eyebrow">{{ t('drafts.presence.eyebrow') }}</span>
            <h2>{{ t('drafts.presence.title') }}</h2>
            <p>{{ t('drafts.presence.summary', { count: confirmedPresences.length, teams: selectedMontagem.quantidadeTimes, reserves: selectedMontagem.quantidadeReservas }) }}</p>
          </div>
          <div class="draft-hero-actions">
            <button v-if="selectedMontagem.status === DraftMontagemStatusValues.PresencaAberta && !myPresence" type="button" :disabled="saving" @click="confirmPresence">{{ t('drafts.presence.confirm') }}</button>
            <button v-if="selectedMontagem.status === DraftMontagemStatusValues.PresencaAberta && myPresence" type="button" class="button-secondary" :disabled="saving" @click="cancelPresence">{{ t('drafts.presence.cancel') }}</button>
            <button v-if="canManageDrafts && selectedMontagem.status === DraftMontagemStatusValues.PresencaAberta" type="button" class="button-secondary" :disabled="saving" @click="closePresence(false)">{{ t('drafts.presence.close') }}</button>
            <button v-if="canManageDrafts && selectedMontagem.status === DraftMontagemStatusValues.PresencaAberta && confirmedPresences.length < 10" type="button" class="button-secondary" :disabled="saving" @click="closePresence(true)">{{ t('drafts.presence.continueManual') }}</button>
            <button v-if="canManageDrafts && selectedMontagem.status !== DraftMontagemStatusValues.Finalizada && selectedMontagem.status !== DraftMontagemStatusValues.Cancelada" type="button" class="button-secondary" :disabled="saving" @click="cancelMontagem">{{ t('common.cancel') }}</button>
          </div>
          <p v-if="selectedMontagem.status === DraftMontagemStatusValues.PresencaAberta && confirmedPresences.length < 10" class="profile-inline-message">{{ t('drafts.presence.lessThanTen') }}</p>
          <div class="draft-player-picker__grid">
            <button v-for="presence in confirmedPresences" :key="presence.id" type="button" class="draft-player-option" :class="{ 'is-selected': captainSelection.includes(presence.jogadorId) }" :disabled="selectedMontagem.status !== DraftMontagemStatusValues.PresencaEncerrada || !canManageDrafts" @click="toggleCaptainSelection(presence.jogadorId)">
              <span class="draft-slot__avatar">{{ presence.nomeExibicao.charAt(0) }}</span>
              <span><strong>{{ presence.nomeExibicao }}</strong><small>{{ presence.origemConfirmacao }}</small></span>
            </button>
          </div>
          <div v-if="canManageDrafts && selectedMontagem.status === DraftMontagemStatusValues.PresencaEncerrada" class="draft-hero-actions">
            <button type="button" :disabled="saving || captainSelection.length !== selectedMontagem.quantidadeTimes" @click="defineCaptains">{{ t('drafts.presence.defineCaptains') }}</button>
          </div>
          <div v-if="canManageDrafts && selectedMontagem.status === DraftMontagemStatusValues.CapitaesDefinidos" class="draft-hero-actions">
            <button type="button" :disabled="saving" @click="drawPickOrder">{{ t('drafts.presence.drawOrder') }}</button>
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
          @cancel="cancelMontagem"
        />
        <section v-else-if="!selectedMontagem" class="draft-empty-card">
          <h2>{{ t('drafts.noSelectionTitle') }}</h2>
          <p>{{ t('drafts.noSelectionDescription') }}</p>
        </section>
      </main>
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
  </section>
</template>
