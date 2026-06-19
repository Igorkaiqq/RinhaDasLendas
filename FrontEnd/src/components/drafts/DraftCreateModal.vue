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
</script>

<template>
  <div v-if="open" class="modal-backdrop" role="presentation">
    <section class="player-form-modal" role="dialog" aria-modal="true" aria-labelledby="draft-create-title">
      <header>
        <div>
          <span class="eyebrow">Novo draft</span>
          <h2 id="draft-create-title">Criar Draft</h2>
        </div>
        <button type="button" aria-label="Fechar" @click="emit('close')">x</button>
      </header>

      <form class="player-form" @submit.prevent="submit">
        <div v-if="serviceErrors.length" class="form-errors" role="alert">
          <p v-for="error in serviceErrors" :key="error">{{ error }}</p>
        </div>

        <label>
          Nome
          <input v-model="form.nome" required maxlength="120" placeholder="Rinha de sexta" />
        </label>

        <label>
          Tamanho do time
          <input v-model.number="form.tamanhoTime" type="number" min="1" max="5" />
        </label>

        <label class="player-form__wide">
          Observacoes
          <textarea v-model="form.observacoes" rows="3" maxlength="500" />
        </label>

        <label class="player-form__wide">
          Jogadores elegiveis
          <select v-model="form.jogadoresIds" multiple size="8" required>
            <option v-for="player in players" :key="player.id" :value="player.id">{{ player.nomeExibicao }}</option>
          </select>
        </label>

        <label class="checkbox-line">
          <input v-model="form.sortearCapitaes" type="checkbox" />
          Sortear capitaes
        </label>

        <template v-if="!form.sortearCapitaes">
          <label>
            Capitao Time A
            <select v-model="form.capitaoTimeAId" required>
              <option value="">Selecione</option>
              <option v-for="player in selectedPlayers" :key="player.id" :value="player.id">{{ player.nomeExibicao }}</option>
            </select>
          </label>
          <label>
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

        <label v-if="!form.sortearPrimeiroPick">
          Primeiro pick
          <select v-model="form.primeiroTime">
            <option value="TimeA">Time A</option>
            <option value="TimeB">Time B</option>
          </select>
        </label>

        <footer>
          <button type="button" class="button-secondary" @click="emit('close')">Cancelar</button>
          <button type="submit" :disabled="saving">{{ saving ? 'Criando...' : 'Criar draft' }}</button>
        </footer>
      </form>
    </section>
  </div>
</template>
