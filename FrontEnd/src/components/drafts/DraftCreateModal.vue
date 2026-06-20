<script setup lang="ts">
import { computed, reactive, watch } from 'vue'

import type { Player } from '@/services/players'
import type { DraftPayload, DraftTeamValue } from '@/types/draft'

const props = defineProps<{ open: boolean; players: Player[]; saving: boolean; serviceErrors: string[] }>()

const emit = defineEmits<{
  close: []
  submit: [payload: DraftPayload]
}>()

const form = reactive({
  nome: '',
  observacoes: '',
  tamanhoTime: 5,
  sortearCapitaes: false,
  capitaoTimeAId: '',
  capitaoTimeBId: '',
  sortearPrimeiroPick: false,
  primeiroTime: 'TimeA' as DraftTeamValue,
  jogadoresIds: [] as string[],
})

const selectedPlayers = computed(() => props.players.filter((player) => form.jogadoresIds.includes(player.id)))
const totalPlayersNeeded = computed(() => form.tamanhoTime * 2)
const hasEnoughPlayers = computed(() => form.jogadoresIds.length >= totalPlayersNeeded.value)
const hasValidCaptains = computed(
  () =>
    form.sortearCapitaes ||
    (Boolean(form.capitaoTimeAId) &&
      Boolean(form.capitaoTimeBId) &&
      form.capitaoTimeAId !== form.capitaoTimeBId &&
      form.jogadoresIds.includes(form.capitaoTimeAId) &&
      form.jogadoresIds.includes(form.capitaoTimeBId)),
)
const canSubmit = computed(() => Boolean(form.nome.trim()) && hasEnoughPlayers.value && hasValidCaptains.value)

watch(
  () => props.open,
  (open) => {
    if (open) {
      form.nome = ''
      form.observacoes = ''
      form.tamanhoTime = 5
      form.sortearCapitaes = false
      form.capitaoTimeAId = ''
      form.capitaoTimeBId = ''
      form.sortearPrimeiroPick = false
      form.primeiroTime = 'TimeA'
      form.jogadoresIds = []
    }
  },
)

function submit() {
  if (!canSubmit.value) {
    return
  }

  emit('submit', {
    nome: form.nome,
    observacoes: form.observacoes || null,
    tamanhoTime: form.tamanhoTime,
    sortearCapitaes: form.sortearCapitaes,
    capitaoTimeAId: form.sortearCapitaes ? null : form.capitaoTimeAId,
    capitaoTimeBId: form.sortearCapitaes ? null : form.capitaoTimeBId,
    sortearPrimeiroPick: form.sortearPrimeiroPick,
    primeiroTime: form.sortearPrimeiroPick ? null : form.primeiroTime,
    jogadoresIds: form.jogadoresIds,
  })
}

function togglePlayer(playerId: string) {
  if (form.jogadoresIds.includes(playerId)) {
    form.jogadoresIds = form.jogadoresIds.filter((id) => id !== playerId)
    if (form.capitaoTimeAId === playerId) {
      form.capitaoTimeAId = ''
    }
    if (form.capitaoTimeBId === playerId) {
      form.capitaoTimeBId = ''
    }
    return
  }

  form.jogadoresIds = [...form.jogadoresIds, playerId]
}
</script>

<template>
  <div v-if="open" class="player-modal-backdrop" role="presentation">
    <section class="player-modal draft-create-modal" role="dialog" aria-modal="true" aria-labelledby="draft-create-title">
      <header class="player-modal__header">
        <div>
          <span class="eyebrow">Novo draft</span>
          <h2 id="draft-create-title">Criar Draft</h2>
        </div>
        <button type="button" aria-label="Fechar" @click="emit('close')">x</button>
      </header>

      <form class="player-form draft-create-form" @submit.prevent="submit">
        <div v-if="serviceErrors.length" class="form-errors" role="alert">
          <p v-for="error in serviceErrors" :key="error">{{ error }}</p>
        </div>

        <label class="player-form__field">
          Nome
          <input v-model="form.nome" required maxlength="120" placeholder="Rinha de sexta" />
        </label>

        <label class="player-form__field">
          Tamanho do time
          <input v-model.number="form.tamanhoTime" type="number" min="1" max="5" />
        </label>

        <label class="player-form__field player-form__field--wide">
          Observacoes
          <textarea v-model="form.observacoes" rows="3" maxlength="500" />
        </label>

        <section class="draft-player-picker player-form__field--wide" aria-label="Jogadores elegiveis">
          <div class="draft-player-picker__header">
            <div>
              <span class="eyebrow">Jogadores elegiveis</span>
              <h3>{{ form.jogadoresIds.length }} selecionados</h3>
            </div>
            <span :class="{ 'is-danger': !hasEnoughPlayers }">Minimo: {{ totalPlayersNeeded }}</span>
          </div>

          <div class="draft-player-picker__grid">
            <button
              v-for="player in players"
              :key="player.id"
              type="button"
              class="draft-player-option"
              :class="{ 'is-selected': form.jogadoresIds.includes(player.id) }"
              @click="togglePlayer(player.id)"
            >
              <span class="draft-slot__avatar">{{ player.nomeExibicao.charAt(0) }}</span>
              <span>
                <strong>{{ player.nomeExibicao }}</strong>
                <small>{{ player.elo ? `${player.elo} ${player.divisao ?? ''}` : 'Elo nao informado' }}</small>
              </span>
            </button>
          </div>
        </section>

        <label class="checkbox-line">
          <input v-model="form.sortearCapitaes" type="checkbox" />
          Sortear capitaes
        </label>

        <template v-if="!form.sortearCapitaes">
          <label class="player-form__field">
            Capitao Time A
            <select v-model="form.capitaoTimeAId" required>
              <option value="">Selecione</option>
              <option v-for="player in selectedPlayers" :key="player.id" :value="player.id">{{ player.nomeExibicao }}</option>
            </select>
          </label>
          <label class="player-form__field">
            Capitao Time B
            <select v-model="form.capitaoTimeBId" required>
              <option value="">Selecione</option>
              <option v-for="player in selectedPlayers" :key="player.id" :value="player.id">{{ player.nomeExibicao }}</option>
            </select>
          </label>
        </template>

        <label class="checkbox-line">
          <input v-model="form.sortearPrimeiroPick" type="checkbox" />
          Sortear primeiro pick
        </label>

        <label v-if="!form.sortearPrimeiroPick" class="player-form__field">
          Primeiro pick
          <select v-model="form.primeiroTime">
            <option value="TimeA">Time A</option>
            <option value="TimeB">Time B</option>
          </select>
        </label>

        <footer class="player-modal__actions">
          <button type="button" class="button-secondary" @click="emit('close')">Cancelar</button>
          <button type="submit" :disabled="saving || !canSubmit">{{ saving ? 'Criando...' : 'Criar draft' }}</button>
        </footer>
      </form>
    </section>
  </div>
</template>
