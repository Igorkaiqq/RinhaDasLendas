<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

import DraftBoard from '@/components/drafts/DraftBoard.vue'
import DraftCancelDialog from '@/components/drafts/DraftCancelDialog.vue'
import DraftCreateModal from '@/components/drafts/DraftCreateModal.vue'
import DraftPickHistory from '@/components/drafts/DraftPickHistory.vue'
import DraftStatusBadge from '@/components/drafts/DraftStatusBadge.vue'
import { DRAFT_STATUS_OPTIONS } from '@/constants/draftStatus'
import { listPlayers, type Player } from '@/services/players'
import { cancelDraft, createDraft, DraftServiceError, listDrafts, registerDraftPick } from '@/services/drafts'
import type { Draft, DraftPayload, DraftPlayer, DraftStatusValue } from '@/types/draft'

const drafts = ref<Draft[]>([])
const players = ref<Player[]>([])
const selectedDraftId = ref<string | null>(null)
const loading = ref(true)
const saving = ref(false)
const picking = ref(false)
const errors = ref<string[]>([])
const serviceErrors = ref<string[]>([])
const notification = ref<string | null>(null)
const createModalOpen = ref(false)
const cancelingDraft = ref<Draft | null>(null)
const searchTerm = ref('')
const selectedStatus = ref<DraftStatusValue | ''>('')

const selectedDraft = computed(() => drafts.value.find((draft) => draft.id === selectedDraftId.value) ?? drafts.value[0] ?? null)

const filteredDrafts = computed(() => {
  const search = searchTerm.value.trim().toLowerCase()
  return drafts.value.filter((draft) => {
    const matchesStatus = !selectedStatus.value || draft.status === selectedStatus.value
    const matchesSearch = !search || draft.nome.toLowerCase().includes(search)
    return matchesStatus && matchesSearch
  })
})

onMounted(async () => {
  await Promise.all([loadDrafts(), loadPlayers()])
})

async function loadDrafts() {
  loading.value = true
  errors.value = []
  try {
    drafts.value = await listDrafts()
    selectedDraftId.value = drafts.value[0]?.id ?? null
  } catch (error) {
    captureError(error)
  } finally {
    loading.value = false
  }
}

async function loadPlayers() {
  try {
    players.value = await listPlayers(true)
  } catch {
    players.value = []
  }
}

async function saveDraft(payload: DraftPayload) {
  saving.value = true
  serviceErrors.value = []
  try {
    const created = await createDraft(payload)
    drafts.value = [created, ...drafts.value]
    selectedDraftId.value = created.id
    notification.value = `Draft ${created.nome} criado.`
    createModalOpen.value = false
  } catch (error) {
    serviceErrors.value = error instanceof DraftServiceError ? error.errors : ['Nao foi possivel criar o draft.']
  } finally {
    saving.value = false
  }
}

async function pickPlayer(player: DraftPlayer) {
  if (!selectedDraft.value) {
    return
  }

  picking.value = true
  errors.value = []
  try {
    const updated = await registerDraftPick(selectedDraft.value.id, player.id)
    replaceDraft(updated)
    notification.value = `${player.nomeExibicao} escolhido.`
  } catch (error) {
    captureError(error)
  } finally {
    picking.value = false
  }
}

async function confirmCancel(motivo: string) {
  if (!cancelingDraft.value) {
    return
  }

  try {
    const updated = await cancelDraft(cancelingDraft.value.id, motivo)
    replaceDraft(updated)
    notification.value = `Draft ${updated.nome} cancelado.`
    cancelingDraft.value = null
  } catch (error) {
    captureError(error)
  }
}

function replaceDraft(updated: Draft) {
  drafts.value = drafts.value.map((draft) => (draft.id === updated.id ? updated : draft))
}

function resetFilters() {
  searchTerm.value = ''
  selectedStatus.value = ''
}

function captureError(error: unknown) {
  errors.value = error instanceof DraftServiceError ? error.errors : ['Nao foi possivel concluir a acao.']
}
</script>

<template>
  <section class="players-page drafts-page">
    <div v-if="notification" class="app-toast app-toast--success" role="status" aria-live="polite">
      <span class="app-toast__indicator" aria-hidden="true" />
      <p>{{ notification }}</p>
      <button type="button" aria-label="Fechar notificacao" @click="notification = null">x</button>
    </div>

    <header class="players-hero drafts-hero">
      <div>
        <h1>Draft de Jogadores</h1>
        <p>Monte os times com capitaes, ordem de picks e historico transparente.</p>
      </div>
      <button type="button" @click="createModalOpen = true">+ Criar Draft</button>
    </header>

    <section class="filter-bar" aria-label="Filtros de drafts">
      <label class="filter-field filter-field--wide">
        Buscar draft
        <span>
          <span aria-hidden="true">S</span>
          <input v-model="searchTerm" type="search" placeholder="Nome do draft" />
        </span>
      </label>
      <label class="filter-field">
        Status
        <select v-model="selectedStatus">
          <option value="">Todos</option>
          <option v-for="status in DRAFT_STATUS_OPTIONS" :key="status" :value="status">{{ status }}</option>
        </select>
      </label>
      <button class="filter-reset" type="button" aria-label="Limpar filtros" @click="resetFilters">=</button>
    </section>

    <div v-if="errors.length" class="form-errors" role="alert">
      <p v-for="error in errors" :key="error">{{ error }}</p>
    </div>

    <section class="draft-layout">
      <aside class="draft-list" aria-label="Lista de drafts">
        <button
          v-for="draft in filteredDrafts"
          :key="draft.id"
          type="button"
          :class="{ 'is-selected': selectedDraft?.id === draft.id }"
          @click="selectedDraftId = draft.id"
        >
          <strong>{{ draft.nome }}</strong>
          <DraftStatusBadge :status="draft.status" />
          <span>{{ draft.escolhas.length }} picks</span>
        </button>
        <div v-if="!loading && !filteredDrafts.length" class="draft-empty-card">
          <h2>Nenhum draft encontrado</h2>
          <p>Crie o primeiro draft ou ajuste os filtros.</p>
        </div>
      </aside>

      <main class="draft-main">
        <header v-if="selectedDraft" class="draft-summary">
          <div>
            <DraftStatusBadge :status="selectedDraft.status" />
            <h2>{{ selectedDraft.nome }}</h2>
            <p>{{ selectedDraft.observacoes || 'Sem observacoes.' }}</p>
          </div>
          <button v-if="selectedDraft.status === 'Aberto'" type="button" class="button-secondary" @click="cancelingDraft = selectedDraft">
            Cancelar
          </button>
        </header>

        <DraftBoard :draft="selectedDraft" :picking="picking" @pick="pickPlayer" />
        <DraftPickHistory v-if="selectedDraft" :picks="selectedDraft.escolhas" />
      </main>
    </section>

    <DraftCreateModal
      :open="createModalOpen"
      :players="players"
      :saving="saving"
      :service-errors="serviceErrors"
      @close="createModalOpen = false"
      @submit="saveDraft"
    />

    <DraftCancelDialog :open="Boolean(cancelingDraft)" :draft="cancelingDraft" @cancel="cancelingDraft = null" @confirm="confirmCancel" />
  </section>
</template>
