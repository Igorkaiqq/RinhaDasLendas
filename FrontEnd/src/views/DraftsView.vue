<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'

import DraftVisualBoard from '@/components/drafts/visual/DraftVisualBoard.vue'
import DraftVisualSetup from '@/components/drafts/visual/DraftVisualSetup.vue'
import PendingPlayerProfileNotice from '@/components/users/PendingPlayerProfileNotice.vue'
import { Permissions } from '@/constants/permissions'
import { useAuthState } from '@/services/authState'
import { listEligibleCaptains, listPlayers, type Player } from '@/services/players'
import {
  cancelDraftMontagem,
  createDraftMontagem,
  DraftMontagemServiceError,
  drawDraftMontagemCaptains,
  finalizeDraftMontagem,
  getDraftMontagemById,
  listDraftMontagens,
  saveDraftMontagemLayout,
} from '@/services/draftMontagens'
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

const statusOptions: DraftMontagemStatus[] = ['Aberta', 'Finalizada', 'Cancelada']
const canManageDrafts = computed(() => auth.hasPermission(Permissions.CanManageDrafts))
const hasPlayerProfile = computed(() => Boolean(auth.user.value?.jogadorId))

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
  } catch (error) {
    captureError(error)
  } finally {
    saving.value = false
  }
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
      <button type="button" :aria-label="t('common.closeNotification')" @click="notification = null">x</button>
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
          <span aria-hidden="true">SR</span>
          <input v-model="searchTerm" type="search" :placeholder="t('drafts.searchPlaceholder')" />
        </span>
      </label>
      <label class="filter-field">
        {{ t('common.status') }}
        <select v-model="selectedStatus">
          <option value="">{{ t('common.all') }}</option>
          <option v-for="status in statusOptions" :key="status" :value="status">{{ status }}</option>
        </select>
      </label>
      <button class="filter-reset" type="button" :aria-label="t('common.clearFilters')" @click="resetFilters">=</button>
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
          <span class="team-status" :class="`team-status--${draft.status.toLowerCase()}`">{{ draft.status }}</span>
          <span>{{ t('drafts.cardSummary', { teams: draft.quantidadeTimes, reserves: draft.quantidadeReservas }) }}</span>
        </button>
        <div v-if="!loading && !filteredDrafts.length" class="draft-empty-card">
          <h2>{{ t('drafts.emptyTitle') }}</h2>
          <p>{{ t('drafts.emptyDescription') }}</p>
        </div>
      </aside>

      <main class="draft-main">
        <DraftVisualBoard
          v-if="selectedMontagem"
          :montagem="selectedMontagem"
          :saving="saving"
          :can-manage="canManageDrafts"
          @save="saveMontagemLayout"
          @draw-captains="drawMontagemCaptains"
          @finalize="finalizeMontagem"
          @cancel="cancelMontagem"
        />
        <section v-else class="draft-empty-card">
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
