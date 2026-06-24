<script setup lang="ts">
import { computed, reactive, watch } from 'vue'

import { PlayerStatus } from '@/constants/playerStatus'
import type { Team, TeamFormMode, TeamFormPayload, TeamPlayerOption } from '@/types/team'

const props = defineProps<{
  open: boolean
  mode: TeamFormMode
  team: Team | null
  players: TeamPlayerOption[]
  saving: boolean
  serviceErrors: string[]
}>()

const emit = defineEmits<{
  close: []
  submit: [payload: TeamFormPayload]
}>()

const form = reactive({
  nome: '',
  tag: '',
  observacoes: '',
  capitaoId: '',
  jogadoresIds: [] as string[],
  submitted: false,
})

const activePlayers = computed(() => props.players.filter((player) => player.status === PlayerStatus.Active))
const title = computed(() => (props.mode === 'edit' ? 'Editar time' : 'Cadastrar time'))

const validationErrors = computed(() => {
  const messages: string[] = []

  if (!form.nome.trim()) {
    messages.push('Nome do time e obrigatorio.')
  }

  if (!form.tag.trim()) {
    messages.push('Tag do time e obrigatoria.')
  }

  if (form.jogadoresIds.length === 0) {
    messages.push('Selecione pelo menos um jogador.')
  }

  if (form.jogadoresIds.length > 5) {
    messages.push('Selecione no maximo cinco jogadores.')
  }

  if (form.capitaoId && !form.jogadoresIds.includes(form.capitaoId)) {
    messages.push('Capitao deve fazer parte do time.')
  }

  return messages
})

const visibleErrors = computed(() => [...new Set([...(form.submitted ? validationErrors.value : []), ...props.serviceErrors])])

watch(
  () => [props.open, props.team, props.mode] as const,
  () => {
    if (!props.open) {
      return
    }

    form.nome = props.team?.nome ?? ''
    form.tag = props.team?.tag ?? ''
    form.observacoes = props.team?.observacoes ?? ''
    form.capitaoId = props.team?.capitao?.id ?? ''
    form.jogadoresIds = props.team?.membros.map((member) => member.jogadorId) ?? []
    form.submitted = false
  },
  { immediate: true },
)

watch(
  () => form.jogadoresIds.slice(),
  () => {
    if (form.capitaoId && !form.jogadoresIds.includes(form.capitaoId)) {
      form.capitaoId = ''
    }
  },
)

function togglePlayer(playerId: string) {
  if (form.jogadoresIds.includes(playerId)) {
    form.jogadoresIds = form.jogadoresIds.filter((id) => id !== playerId)
    return
  }

  if (form.jogadoresIds.length >= 5) {
    form.submitted = true
    return
  }

  form.jogadoresIds = [...form.jogadoresIds, playerId]
}

function submit() {
  form.submitted = true
  if (validationErrors.value.length > 0) {
    return
  }

  emit('submit', {
    id: props.team?.id,
    nome: form.nome,
    tag: form.tag,
    observacoes: form.observacoes || null,
    capitaoId: form.capitaoId || null,
    jogadoresIds: form.jogadoresIds,
  })
}
</script>

<template>
  <Teleport to="body">
    <Transition name="player-modal">
      <div v-if="open" class="player-modal-backdrop" role="presentation">
        <section class="player-modal" role="dialog" aria-modal="true" aria-labelledby="team-form-title">
          <header class="player-modal__header">
            <div>
              <p class="page-kicker">Times</p>
              <h2 id="team-form-title">{{ title }}</h2>
            </div>
            <button type="button" aria-label="Fechar formulario" @click="$emit('close')">x</button>
          </header>

          <form class="player-form player-form--modal" @submit.prevent="submit">
            <label class="player-form__field player-form__field--wide">
              Nome
              <input v-model="form.nome" autocomplete="off" placeholder="Os Sem Baron" />
            </label>
            <label class="player-form__field">
              Tag
              <input v-model="form.tag" autocomplete="off" maxlength="10" placeholder="OSB" />
            </label>
            <label class="player-form__field player-form__field--wide">
              Observacoes
              <textarea v-model="form.observacoes" rows="3" placeholder="Notas internas do time" />
            </label>
            <label class="player-form__field">
              Capitao
              <select v-model="form.capitaoId">
                <option value="">Sem capitao</option>
                <option v-for="player in activePlayers.filter((player) => form.jogadoresIds.includes(player.id))" :key="player.id" :value="player.id">
                  {{ player.nomeExibicao }}
                </option>
              </select>
            </label>

            <fieldset class="player-form__field player-form__field--wide">
              <legend>Jogadores ativos</legend>
              <div class="route-preferences-panel">
                <button
                  v-for="player in activePlayers"
                  :key="player.id"
                  type="button"
                  class="route-pill"
                  :class="{ 'is-selected': form.jogadoresIds.includes(player.id) }"
                  @click="togglePlayer(player.id)"
                >
                  {{ player.nomeExibicao }}
                </button>
              </div>
            </fieldset>

            <div v-if="visibleErrors.length" class="player-form__errors">
              <p v-for="error in visibleErrors" :key="error">{{ error }}</p>
            </div>

            <div class="player-modal__actions">
              <button type="button" class="button-secondary" @click="$emit('close')">Cancelar</button>
              <button type="submit" :disabled="saving">{{ saving ? 'Salvando...' : 'Salvar' }}</button>
            </div>
          </form>
        </section>
      </div>
    </Transition>
  </Teleport>
</template>
