<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'

import PlayerDeleteDialog from '@/components/players/PlayerDeleteDialog.vue'
import PlayerFormModal from '@/components/players/PlayerFormModal.vue'
import PlayerList from '@/components/players/PlayerList.vue'
import { LEAGUE_ROLES } from '@/constants/leagueRoles'
import { Permissions } from '@/constants/permissions'
import { PlayerStatus } from '@/constants/playerStatus'
import {
  createPlayer,
  deletePlayer,
  listPlayers,
  PlayerServiceError,
  updatePlayerBasics,
  updateRoutePreferences,
  type Player,
  type PlayerPayload,
} from '@/services/players'
import { useAuthState } from '@/services/authState'
import type { FeedbackState, PlayerFormMode } from '@/types/players'

const { t } = useI18n()
const auth = useAuthState()
const players = ref<Player[]>([])
const loading = ref(true)
const saving = ref(false)
const onlyActive = ref(false)
const searchTerm = ref('')
const selectedRank = ref('')
const selectedRoute = ref('')
const errors = ref<string[]>([])
const serviceErrors = ref<string[]>([])
const playerModalOpen = ref(false)
const formMode = ref<PlayerFormMode>('create')
const editingPlayer = ref<Player | null>(null)
const deletingPlayer = ref<Player | null>(null)
const notification = ref<FeedbackState | null>(null)
let notificationTimer: ReturnType<typeof globalThis.setTimeout> | null = null

const rankOptions = computed(() => [...new Set(players.value.map((player) => player.elo).filter(Boolean))] as string[])
const routeOptions = LEAGUE_ROLES
const paginationStart = computed(() => (filteredPlayers.value.length ? 1 : 0))
const canManagePlayers = computed(() => auth.hasPermission(Permissions.CanManageUsers))

const filteredPlayers = computed(() => {
  const normalizedSearch = searchTerm.value.trim().toLowerCase()

  return players.value.filter((player) => {
    const primaryRoute = player.preferencias.find((preference) => preference.prioridade === 1)?.rota
    const matchesSearch =
      !normalizedSearch ||
      player.nomeExibicao.toLowerCase().includes(normalizedSearch) ||
      player.discord?.toLowerCase().includes(normalizedSearch) ||
      player.riotId?.toLowerCase().includes(normalizedSearch)
    const matchesRank = !selectedRank.value || player.elo === selectedRank.value
    const matchesRoute = !selectedRoute.value || primaryRoute === selectedRoute.value
    const matchesStatus = !onlyActive.value || player.status === PlayerStatus.Active

    return matchesSearch && matchesRank && matchesRoute && matchesStatus
  })
})

onMounted(async () => {
  await loadPlayers()
})

onUnmounted(() => {
  clearNotificationTimer()
})

async function loadPlayers() {
  loading.value = true
  errors.value = []

  try {
    players.value = await listPlayers(false)
  } catch (error) {
    captureError(error)
  } finally {
    loading.value = false
  }
}

function openCreateModal() {
  if (!canManagePlayers.value) {
    return
  }

  formMode.value = 'create'
  editingPlayer.value = null
  serviceErrors.value = []
  playerModalOpen.value = true
}

function openEditModal(player: Player) {
  if (!canManagePlayers.value) {
    return
  }

  formMode.value = 'edit'
  editingPlayer.value = player
  serviceErrors.value = []
  playerModalOpen.value = true
}

function closeModal() {
  playerModalOpen.value = false
  editingPlayer.value = null
  serviceErrors.value = []
}

async function savePlayer(payload: PlayerPayload & { id?: string }) {
  saving.value = true
  serviceErrors.value = []

  try {
    if (formMode.value === 'edit' && payload.id) {
      const updatedBasics = await updatePlayerBasics(payload.id, payload)
      const updatedPreferences = await updateRoutePreferences(payload.id, payload.preferencias)
      const updated = { ...updatedBasics, preferencias: updatedPreferences.preferencias }
      players.value = players.value.map((player) => (player.id === updated.id ? updated : player))
      showNotification('success', t('players.updated', { name: updated.nomeExibicao }))
    } else {
      const created = await createPlayer(payload)
      players.value = [created, ...players.value.filter((player) => player.id !== created.id)]
      showNotification('success', t('players.created', { name: created.nomeExibicao }))
    }

    closeModal()
  } catch (error) {
    serviceErrors.value = error instanceof PlayerServiceError ? error.errors : [t('players.saveError')]
    showNotification('danger', serviceErrors.value[0] ?? t('players.saveError'))
  } finally {
    saving.value = false
  }
}

function requestDelete(player: Player) {
  if (!canManagePlayers.value) {
    return
  }

  deletingPlayer.value = player
}

async function confirmDelete() {
  if (!deletingPlayer.value) {
    return
  }

  try {
    const player = deletingPlayer.value
    await deletePlayer(player.id)
    players.value = players.value.filter((current) => current.id !== player.id)
    deletingPlayer.value = null
    showNotification('success', t('players.removed', { name: player.nomeExibicao }))
  } catch (error) {
    captureError(error)
  }
}

function resetFilters() {
  searchTerm.value = ''
  selectedRank.value = ''
  selectedRoute.value = ''
  onlyActive.value = false
}

function showNotification(tone: FeedbackState['tone'], message: string) {
  notification.value = { tone, message }
  clearNotificationTimer()
  notificationTimer = globalThis.setTimeout(() => {
    notification.value = null
    notificationTimer = null
  }, 4200)
}

function dismissNotification() {
  notification.value = null
  clearNotificationTimer()
}

function clearNotificationTimer() {
  if (notificationTimer) {
    globalThis.clearTimeout(notificationTimer)
    notificationTimer = null
  }
}

function captureError(error: unknown) {
  errors.value = error instanceof PlayerServiceError ? error.errors : [t('players.actionError')]
  showNotification('danger', errors.value[0] ?? t('players.actionError'))
}
</script>

<template>
  <section class="players-page">
    <div
      v-if="notification"
      class="app-toast"
      :class="'app-toast--' + notification.tone"
      role="status"
      aria-live="polite"
    >
      <span class="app-toast__indicator" aria-hidden="true" />
      <p>{{ notification.message }}</p>
      <button type="button" :aria-label="t('players.closeNotification')" @click="dismissNotification">x</button>
    </div>

    <header class="players-hero">
      <div>
        <h1>{{ t('players.title') }}</h1>
        <p>{{ t('players.subtitle') }}</p>
      </div>
      <button v-if="canManagePlayers" type="button" @click="openCreateModal">+ {{ t('players.create') }}</button>
    </header>

    <section class="filter-bar" :aria-label="t('players.filtersLabel')">
      <label class="filter-field filter-field--wide">
        {{ t('players.searchName') }}
        <span>
          <span aria-hidden="true">S</span>
          <input v-model="searchTerm" type="search" :placeholder="t('players.searchPlaceholder')" />
        </span>
      </label>

      <label class="filter-field">
        {{ t('players.rank') }}
        <select v-model="selectedRank">
          <option value="">{{ t('players.allRanks') }}</option>
          <option v-for="rank in rankOptions" :key="rank" :value="rank">{{ rank }}</option>
        </select>
      </label>

      <label class="filter-field">
        {{ t('players.primaryRoute') }}
        <select v-model="selectedRoute">
          <option value="">{{ t('players.anyRoute') }}</option>
          <option v-for="route in routeOptions" :key="route" :value="route">{{ route }}</option>
        </select>
      </label>

      <label class="switch-field">
        <input v-model="onlyActive" type="checkbox" />
        <span aria-hidden="true" />
        {{ t('players.onlyActive') }}
      </label>

      <button class="filter-reset" type="button" :aria-label="t('players.clearFilters')" @click="resetFilters">=</button>
    </section>

    <PlayerList
      :players="filteredPlayers"
      :loading="loading"
      :errors="errors"
      :can-manage="canManagePlayers"
      @create="openCreateModal"
      @edit="openEditModal"
      @delete="requestDelete"
    />

    <footer class="players-pagination" :aria-label="t('players.paginationLabel')">
      <span>
        {{ t('players.paginationSummary', { start: paginationStart, end: filteredPlayers.length, total: players.length }) }}
      </span>
      <div>
        <button type="button" disabled>&lt;</button>
        <button type="button" class="is-active">1</button>
        <button type="button">2</button>
        <button type="button">3</button>
        <button type="button">&gt;</button>
      </div>
    </footer>

    <PlayerFormModal
      :open="playerModalOpen"
      :mode="formMode"
      :player="editingPlayer"
      :saving="saving"
      :service-errors="serviceErrors"
      @close="closeModal"
      @submit="savePlayer"
    />

    <PlayerDeleteDialog
      :open="Boolean(deletingPlayer)"
      :player="deletingPlayer"
      @cancel="deletingPlayer = null"
      @confirm="confirmDelete"
    />
  </section>
</template>
