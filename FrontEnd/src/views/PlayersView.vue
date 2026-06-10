<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'

import PlayerDeleteDialog from '@/components/players/PlayerDeleteDialog.vue'
import PlayerFormDrawer from '@/components/players/PlayerFormDrawer.vue'
import PlayerList from '@/components/players/PlayerList.vue'
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
import type { FeedbackState, PlayerFormMode } from '@/types/players'

const players = ref<Player[]>([])
const loading = ref(true)
const saving = ref(false)
const onlyActive = ref(false)
const searchTerm = ref('')
const selectedRank = ref('')
const selectedRoute = ref('')
const errors = ref<string[]>([])
const serviceErrors = ref<string[]>([])
const drawerOpen = ref(false)
const formMode = ref<PlayerFormMode>('create')
const editingPlayer = ref<Player | null>(null)
const deletingPlayer = ref<Player | null>(null)
const notification = ref<FeedbackState | null>(null)
let notificationTimer: ReturnType<typeof globalThis.setTimeout> | null = null

const rankOptions = computed(() => [...new Set(players.value.map((player) => player.elo).filter(Boolean))] as string[])
const routeOptions = ['Top', 'Jungle', 'Mid', 'Adc', 'Support']

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
    const matchesStatus = !onlyActive.value || player.status === 'Ativo'

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

function openCreateDrawer() {
  formMode.value = 'create'
  editingPlayer.value = null
  serviceErrors.value = []
  drawerOpen.value = true
}

function openEditDrawer(player: Player) {
  formMode.value = 'edit'
  editingPlayer.value = player
  serviceErrors.value = []
  drawerOpen.value = true
}

function closeDrawer() {
  drawerOpen.value = false
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
      showNotification('success', `Jogador ${updated.nomeExibicao} atualizado.`)
    } else {
      const created = await createPlayer(payload)
      players.value = [created, ...players.value.filter((player) => player.id !== created.id)]
      showNotification('success', `Jogador ${created.nomeExibicao} cadastrado.`)
    }

    closeDrawer()
  } catch (error) {
    serviceErrors.value = error instanceof PlayerServiceError ? error.errors : ['Nao foi possivel salvar o jogador.']
    showNotification('danger', serviceErrors.value[0] ?? 'Nao foi possivel salvar o jogador.')
  } finally {
    saving.value = false
  }
}

function requestDelete(player: Player) {
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
    showNotification('success', `Jogador ${player.nomeExibicao} removido.`)
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
  errors.value = error instanceof PlayerServiceError ? error.errors : ['Nao foi possivel concluir a acao.']
  showNotification('danger', errors.value[0] ?? 'Nao foi possivel concluir a acao.')
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
      <button type="button" aria-label="Fechar notificacao" @click="dismissNotification">x</button>
    </div>

    <header class="players-hero">
      <div>
        <h1>Banco de Dados de Jogadores</h1>
        <p>Explore, filtre e recrute os melhores talentos para o seu time.</p>
      </div>
      <button type="button" @click="openCreateDrawer">+ Cadastrar Jogador</button>
    </header>

    <section class="filter-bar" aria-label="Filtros de jogadores">
      <label class="filter-field filter-field--wide">
        Buscar Nome
        <span>
          <span aria-hidden="true">S</span>
          <input v-model="searchTerm" type="search" placeholder="e.g. Faker, Chovy..." />
        </span>
      </label>

      <label class="filter-field">
        Rank / Elo
        <select v-model="selectedRank">
          <option value="">Todos os Ranks</option>
          <option v-for="rank in rankOptions" :key="rank" :value="rank">{{ rank }}</option>
        </select>
      </label>

      <label class="filter-field">
        Rota Principal
        <select v-model="selectedRoute">
          <option value="">Qualquer Rota</option>
          <option v-for="route in routeOptions" :key="route" :value="route">{{ route }}</option>
        </select>
      </label>

      <label class="switch-field">
        <input v-model="onlyActive" type="checkbox" />
        <span aria-hidden="true" />
        LFA (Procurando Time)
      </label>

      <button class="filter-reset" type="button" aria-label="Limpar filtros" @click="resetFilters">=</button>
    </section>

    <PlayerList
      :players="filteredPlayers"
      :loading="loading"
      :errors="errors"
      @create="openCreateDrawer"
      @edit="openEditDrawer"
      @delete="requestDelete"
    />

    <footer class="players-pagination" aria-label="Paginacao de jogadores">
      <span>Mostrando {{ filteredPlayers.length ? 1 : 0 }}-{{ filteredPlayers.length }} de {{ players.length }} jogadores</span>
      <div>
        <button type="button" disabled>&lt;</button>
        <button type="button" class="is-active">1</button>
        <button type="button">2</button>
        <button type="button">3</button>
        <button type="button">&gt;</button>
      </div>
    </footer>

    <PlayerFormDrawer
      :open="drawerOpen"
      :mode="formMode"
      :player="editingPlayer"
      :saving="saving"
      :service-errors="serviceErrors"
      @close="closeDrawer"
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
