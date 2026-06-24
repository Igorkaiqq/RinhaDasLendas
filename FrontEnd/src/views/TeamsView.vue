<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'

import TeamDeleteDialog from '@/components/teams/TeamDeleteDialog.vue'
import TeamFormModal from '@/components/teams/TeamFormModal.vue'
import TeamList from '@/components/teams/TeamList.vue'
import { TEAM_STATUS_OPTIONS } from '@/constants/teamStatus'
import { listPlayers, type Player } from '@/services/players'
import {
  createTeam,
  inactivateTeam,
  listTeams,
  reactivateTeam,
  TeamServiceError,
  updateTeam,
} from '@/services/teams'
import type { Team, TeamFormMode, TeamFormPayload, TeamStatusValue } from '@/types/team'

const teams = ref<Team[]>([])
const { t } = useI18n()
const players = ref<Player[]>([])
const loading = ref(true)
const saving = ref(false)
const errors = ref<string[]>([])
const serviceErrors = ref<string[]>([])
const searchTerm = ref('')
const selectedStatus = ref<TeamStatusValue | ''>('')
const teamModalOpen = ref(false)
const formMode = ref<TeamFormMode>('create')
const editingTeam = ref<Team | null>(null)
const inactivatingTeam = ref<Team | null>(null)
const notification = ref<string | null>(null)

const filteredTeams = computed(() => {
  const search = searchTerm.value.trim().toLowerCase()
  return teams.value.filter((team) => {
    const matchesStatus = !selectedStatus.value || team.status === selectedStatus.value
    const matchesSearch =
      !search ||
      team.nome.toLowerCase().includes(search) ||
      team.tag.toLowerCase().includes(search) ||
      team.membros.some((member) => member.nomeExibicao.toLowerCase().includes(search))

    return matchesStatus && matchesSearch
  })
})

onMounted(async () => {
  await Promise.all([loadTeams(), loadPlayers()])
})

async function loadTeams() {
  loading.value = true
  errors.value = []
  try {
    teams.value = await listTeams()
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

function openCreateModal() {
  formMode.value = 'create'
  editingTeam.value = null
  serviceErrors.value = []
  teamModalOpen.value = true
}

function openEditModal(team: Team) {
  formMode.value = 'edit'
  editingTeam.value = team
  serviceErrors.value = []
  teamModalOpen.value = true
}

function closeModal() {
  teamModalOpen.value = false
  editingTeam.value = null
  serviceErrors.value = []
}

async function saveTeam(payload: TeamFormPayload) {
  saving.value = true
  serviceErrors.value = []

  try {
    if (formMode.value === 'edit' && payload.id) {
      const updated = await updateTeam(payload.id, payload)
      teams.value = teams.value.map((team) => (team.id === updated.id ? updated : team))
      notification.value = t('teams.updated', { name: updated.nome })
    } else {
      const created = await createTeam(payload)
      teams.value = [created, ...teams.value]
      notification.value = t('teams.created', { name: created.nome })
    }
    closeModal()
  } catch (error) {
    serviceErrors.value = error instanceof TeamServiceError ? error.errors : [t('teams.errors.save')]
  } finally {
    saving.value = false
  }
}

async function confirmInactivate() {
  if (!inactivatingTeam.value) {
    return
  }

  try {
    const updated = await inactivateTeam(inactivatingTeam.value.id)
    teams.value = teams.value.map((team) => (team.id === updated.id ? updated : team))
    notification.value = t('teams.inactivated', { name: updated.nome })
    inactivatingTeam.value = null
  } catch (error) {
    captureError(error)
  }
}

async function reactivate(team: Team) {
  try {
    const updated = await reactivateTeam(team.id)
    teams.value = teams.value.map((current) => (current.id === updated.id ? updated : current))
    notification.value = t('teams.reactivated', { name: updated.nome })
  } catch (error) {
    captureError(error)
  }
}

function resetFilters() {
  searchTerm.value = ''
  selectedStatus.value = ''
}

function captureError(error: unknown) {
  errors.value = error instanceof TeamServiceError ? error.errors : [t('teams.errors.action')]
}
</script>

<template>
  <section class="players-page">
    <div v-if="notification" class="app-toast app-toast--success" role="status" aria-live="polite">
      <span class="app-toast__indicator" aria-hidden="true" />
      <p>{{ notification }}</p>
      <button type="button" :aria-label="t('common.closeNotification')" @click="notification = null">x</button>
    </div>

    <header class="players-hero">
      <div>
        <p class="page-kicker">{{ t('teams.kicker') }}</p>
        <h1>{{ t('teams.title') }}</h1>
        <p>{{ t('teams.subtitle') }}</p>
      </div>
      <div class="page-hero__actions">
        <span class="page-hero__metric">{{ t('teams.metrics.visible', { visible: filteredTeams.length, total: teams.length }) }}</span>
        <button type="button" @click="openCreateModal">{{ t('teams.createWithIcon') }}</button>
      </div>
    </header>

    <section class="filter-bar" :aria-label="t('teams.filtersLabel')">
      <label class="filter-field filter-field--wide">
        {{ t('teams.searchLabel') }}
        <span>
          <span aria-hidden="true">SR</span>
          <input v-model="searchTerm" type="search" :placeholder="t('teams.searchPlaceholder')" />
        </span>
      </label>
      <label class="filter-field">
        {{ t('common.status') }}
        <select v-model="selectedStatus">
          <option value="">{{ t('common.all') }}</option>
          <option v-for="status in TEAM_STATUS_OPTIONS" :key="status" :value="status">{{ status }}</option>
        </select>
      </label>
      <button class="filter-reset" type="button" :aria-label="t('common.clearFilters')" @click="resetFilters">=</button>
    </section>

    <TeamList
      :teams="filteredTeams"
      :loading="loading"
      :errors="errors"
      @create="openCreateModal"
      @edit="openEditModal"
      @inactivate="inactivatingTeam = $event"
      @reactivate="reactivate"
    />

    <footer class="players-pagination" :aria-label="t('teams.paginationLabel')">
      <span>{{ t('teams.paginationSummary', { start: filteredTeams.length ? 1 : 0, end: filteredTeams.length, total: teams.length }) }}</span>
    </footer>

    <TeamFormModal
      :open="teamModalOpen"
      :mode="formMode"
      :team="editingTeam"
      :players="players"
      :saving="saving"
      :service-errors="serviceErrors"
      @close="closeModal"
      @submit="saveTeam"
    />

    <TeamDeleteDialog
      :open="Boolean(inactivatingTeam)"
      :team="inactivatingTeam"
      @cancel="inactivatingTeam = null"
      @confirm="confirmInactivate"
    />
  </section>
</template>
