<script setup lang="ts">
import { computed, onMounted, onUnmounted, reactive, ref } from 'vue'

import PlayerStatusBadge from '@/components/PlayerStatusBadge.vue'
import RoutePreferenceEditor from '@/components/RoutePreferenceEditor.vue'
import RoutePreferencesPanel from '@/components/RoutePreferencesPanel.vue'
import {
  createPlayer,
  inactivatePlayer,
  listPlayers,
  PlayerServiceError,
  updateRoutePreferences,
  Elo,
  Divisao,
  type Player,
  type PlayerPayload,
  type RoutePreference,
} from '@/services/players'

const defaultPreferences = (): RoutePreference[] => [
  { rota: 'Top', prioridade: 1, naoJogoNemLascando: false },
  { rota: 'Jungle', prioridade: 2, naoJogoNemLascando: false },
  { rota: 'Mid', prioridade: 3, naoJogoNemLascando: false },
  { rota: 'Adc', prioridade: 4, naoJogoNemLascando: false },
  { rota: 'Support', prioridade: 5, naoJogoNemLascando: false },
]

const eloOptions: Elo[] = [
  Elo.Ferro,
  Elo.Bronze,
  Elo.Prata,
  Elo.Ouro,
  Elo.Platina,
  Elo.Esmeralda,
  Elo.Diamante,
  Elo.Mestre,
  Elo.GraoMestre,
  Elo.Desafiante,
]
const divisionOptions: Divisao[] = [Divisao.IV, Divisao.III, Divisao.II, Divisao.I]
const elosComDivisao = new Set<Elo>([Elo.Ferro, Elo.Bronze, Elo.Prata, Elo.Ouro, Elo.Platina, Elo.Esmeralda, Elo.Diamante])

const players = ref<Player[]>([])
const selectedPlayerId = ref<string | null>(null)
const loading = ref(true)
const saving = ref(false)
const onlyActive = ref(false)
const errors = ref<string[]>([])
const formSubmitted = ref(false)
const selectedElo = ref<Elo | ''>('')
const selectedDivision = ref<Divisao | ''>('')
type NotificationTone = "success" | "danger" | "info"

const notification = ref<{ tone: NotificationTone; message: string } | null>(null)
let notificationTimer: ReturnType<typeof globalThis.setTimeout> | null = null

const form = reactive<PlayerPayload>({
  nomeExibicao: '',
  discord: '',
  riotId: '',
  opGgUrl: '',
  deepLolUrl: '',
  preferencias: defaultPreferences(),
})

const selectedPlayer = computed(() => players.value.find((player) => player.id === selectedPlayerId.value))
const selectedEloRequiresDivision = computed(() => selectedElo.value !== "" && elosComDivisao.has(selectedElo.value))

const formErrors = computed(() => {
  const messages: string[] = []
  const priorities = form.preferencias.map((preference) => preference.prioridade)

  if (!form.nomeExibicao.trim()) {
    messages.push('Nome e obrigatorio.')
  }

  if (!form.discord?.trim()) {
    messages.push('Informe o Discord do jogador.')
  }

  if (!selectedElo.value) {
    messages.push('Selecione um Elo.')
  }

  if (selectedEloRequiresDivision.value && !selectedDivision.value) {
    messages.push('Selecione uma Divisão.')
  }

  if (new Set(priorities).size !== 5) {
    messages.push('Cada prioridade deve ser unica.')
  }

  if (form.preferencias.filter((preference) => preference.naoJogoNemLascando).length > 1) {
    messages.push('Marque no maximo uma rota bloqueada.')
  }

  return messages
})

const visibleFormErrors = computed(() => (formSubmitted.value ? formErrors.value : []))

onMounted(async () => {
  await loadPlayers()
})

onUnmounted(() => {
  clearNotificationTimer()
})

function showNotification(tone: NotificationTone, message: string) {
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

async function loadPlayers() {
  loading.value = true
  errors.value = []

  try {
    players.value = await listPlayers(onlyActive.value)
  } catch (error) {
    captureError(error)
  } finally {
    loading.value = false
  }
}

async function applyActiveFilter() {
  await loadPlayers()
  showNotification("info", onlyActive.value ? "Exibindo somente jogadores ativos." : "Exibindo todos os jogadores.")
}

function handleEloChange() {
  selectedDivision.value = ''
}

async function submitPlayer() {
  formSubmitted.value = true

  if (formErrors.value.length > 0) {
    showNotification("danger", "Revise os campos destacados antes de cadastrar.")
    return
  }

  saving.value = true
  errors.value = []

  try {
    const player = await createPlayer({
      ...form,
      elo: selectedElo.value as Elo,
      divisao: selectedEloRequiresDivision.value ? (selectedDivision.value as Divisao) : null,
      preferencias: [...form.preferencias],
    })
    players.value = [player, ...players.value.filter((current) => current.id !== player.id)]
    selectedPlayerId.value = player.id
    showNotification("success", "Jogador " + player.nomeExibicao + " cadastrado.")
    resetForm()
  } catch (error) {
    captureError(error)
  } finally {
    saving.value = false
  }
}

async function savePreferences(player: Player) {
  errors.value = []

  try {
    const updated = await updateRoutePreferences(player.id, player.preferencias)
    players.value = players.value.map((current) => (current.id === updated.id ? updated : current))
    showNotification("success", "Rotas de " + updated.nomeExibicao + " atualizadas.")
  } catch (error) {
    captureError(error)
  }
}

async function inactivate(player: Player) {
  errors.value = []

  try {
    await inactivatePlayer(player.id)
    players.value = players.value.map((current) =>
      current.id === player.id ? { ...current, status: 'Inativo' } : current,
    )
    showNotification("success", player.nomeExibicao + " foi inativado.")
  } catch (error) {
    captureError(error)
  }
}

function formatPlayerElo(player: Player) {
  if (!player.elo) {
    return null
  }

  return player.divisao ? player.elo + " " + player.divisao : player.elo
}

function resetForm() {
  formSubmitted.value = false
  form.nomeExibicao = ''
  form.discord = ''
  form.riotId = ''
  form.opGgUrl = ''
  form.deepLolUrl = ''
  selectedElo.value = ''
  selectedDivision.value = ''
  form.preferencias = defaultPreferences()
}

function setPlayerPreferences(player: Player, preferences: RoutePreference[]) {
  players.value = players.value.map((current) =>
    current.id === player.id ? { ...current, preferencias: preferences } : current,
  )
}

function captureError(error: unknown) {
  errors.value = error instanceof PlayerServiceError ? error.errors : ['Nao foi possivel concluir a acao.']
  showNotification("danger", errors.value[0] ?? 'Nao foi possivel concluir a acao.')
}
</script>

<template>
  <main class="players-page">
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
    <section class="players-page__header">
      <div>
        <p class="players-page__eyebrow">Jogadores</p>
        <h1>Cadastro de jogadores</h1>
        <p>Gerencie participantes, rotas preferidas, rota bloqueada e status para as proximas rinhas.</p>
      </div>
      <label class="players-page__filter">
        <input v-model="onlyActive" type="checkbox" @change="applyActiveFilter" />
        Somente ativos
      </label>
    </section>

    <section class="players-page__layout">
      <form class="player-form" @submit.prevent="submitPlayer">
        <div class="player-form__header">
          <h2>Novo jogador</h2>
          <button type="submit" :disabled="saving">{{ saving ? 'Salvando...' : 'Cadastrar' }}</button>
        </div>

        <div class="player-form__grid">
          <label class="player-form__field player-form__field--wide">
            Nome de exibicao
            <span>Nome pelo qual o jogador e conhecido no grupo, Discord ou League of Legends.</span>
            <input v-model="form.nomeExibicao" autocomplete="off" placeholder="Rona, Antena, ChuZS..." />
          </label>
          <label>
            Discord
            <input v-model="form.discord" autocomplete="off" placeholder="usuario#1234" />
          </label>
          <label>
            Elo
            <select v-model="selectedElo" @change="handleEloChange">
              <option value="" disabled>Selecione</option>
              <option v-for="elo in eloOptions" :key="elo" :value="elo">{{ elo }}</option>
            </select>
          </label>
          <label v-if="selectedEloRequiresDivision">
            Divisao
            <select v-model="selectedDivision">
              <option value="" disabled>Selecione</option>
              <option v-for="division in divisionOptions" :key="division" :value="division">{{ division }}</option>
            </select>
          </label>
          <label>
            Riot ID
            <input v-model="form.riotId" autocomplete="off" placeholder="Nome#BR1" />
          </label>
          <label>
            OP.GG
            <input v-model="form.opGgUrl" autocomplete="off" placeholder="https://www.op.gg/..." />
          </label>
          <label>
            Deeplol
            <input v-model="form.deepLolUrl" autocomplete="off" placeholder="https://www.deeplol.gg/..." />
          </label>
        </div>

        <RoutePreferenceEditor v-model="form.preferencias" />

        <div v-if="errors.length || visibleFormErrors.length" class="player-form__errors">
          <p v-for="error in [...new Set([...errors, ...visibleFormErrors])]" :key="error">{{ error }}</p>
        </div>
      </form>

      <section class="players-list" aria-label="Lista de jogadores">
        <div v-if="loading" class="players-list__skeleton">
          <span v-for="item in 4" :key="item" />
        </div>

        <div v-else-if="players.length === 0" class="players-list__empty">
          <h2>Nenhum jogador cadastrado</h2>
          <p>Cadastre o primeiro participante para liberar a base das filas e drafts.</p>
          <button type="button" @click="resetForm">Preparar formulario</button>
        </div>

        <article
          v-for="player in players"
          v-else
          :key="player.id"
          class="player-card"
          :class="{ 'player-card--selected': selectedPlayerId === player.id }"
          @click="selectedPlayerId = player.id"
        >
          <div class="player-card__avatar" aria-hidden="true">{{ player.nomeExibicao.slice(0, 2).toUpperCase() }}</div>
          <div class="player-card__body">
            <div class="player-card__topline">
              <div>
                <h2>{{ player.nomeExibicao }}</h2>
                <p>{{ formatPlayerElo(player) || 'Elo nao informado' }}</p>
              </div>
              <PlayerStatusBadge :status="player.status" />
            </div>
            <RoutePreferencesPanel :preferences="player.preferencias" />
            <div class="player-card__actions">
              <button type="button" @click.stop="savePreferences(player)">Salvar rotas</button>
              <button
                type="button"
                class="player-card__danger"
                :disabled="player.status === 'Inativo'"
                @click.stop="inactivate(player)"
              >
                Inativar
              </button>
            </div>
            <RoutePreferenceEditor
              v-if="selectedPlayer?.id === player.id"
              :model-value="player.preferencias"
              @update:model-value="setPlayerPreferences(player, $event)"
            />
          </div>
        </article>
      </section>
    </section>
  </main>
</template>
