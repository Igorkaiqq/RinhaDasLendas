<script setup lang="ts">
import { computed, reactive, watch } from 'vue'

import RoutePreferenceEditor from '@/components/RoutePreferenceEditor.vue'
import { Divisao, Elo, type Player, type PlayerPayload, type RoutePreference } from '@/services/players'
import type { PlayerFormMode } from '@/types/players'

const props = defineProps<{
  open: boolean
  mode: PlayerFormMode
  player: Player | null
  saving: boolean
  serviceErrors: string[]
}>()

const emit = defineEmits<{
  close: []
  submit: [payload: PlayerPayload & { id?: string }]
}>()

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

const form = reactive({
  nomeExibicao: '',
  discord: '',
  riotId: '',
  opGgUrl: '',
  deepLolUrl: '',
  elo: '' as Elo | '',
  divisao: '' as Divisao | '',
  preferencias: defaultPreferences(),
  submitted: false,
})

const title = computed(() => (props.mode === 'edit' ? 'Editar jogador' : 'Cadastrar jogador'))
const selectedEloRequiresDivision = computed(() => form.elo !== '' && elosComDivisao.has(form.elo))

const validationErrors = computed(() => {
  const messages: string[] = []
  const priorities = form.preferencias.map((preference) => preference.prioridade)

  if (!form.nomeExibicao.trim()) {
    messages.push('Nome e obrigatorio.')
  }

  if (!form.discord.trim()) {
    messages.push('Informe o Discord do jogador.')
  }

  if (!form.elo) {
    messages.push('Selecione um Elo.')
  }

  if (selectedEloRequiresDivision.value && !form.divisao) {
    messages.push('Selecione uma divisao.')
  }

  if (new Set(priorities).size !== 5) {
    messages.push('Cada prioridade deve ser unica.')
  }

  if (form.preferencias.filter((preference) => preference.naoJogoNemLascando).length > 1) {
    messages.push('Marque no maximo uma rota bloqueada.')
  }

  if (form.opGgUrl && !form.opGgUrl.startsWith('https://')) {
    messages.push('Informe um link OP.GG iniciado por https://.')
  }

  return messages
})

const visibleErrors = computed(() => [...new Set([...(form.submitted ? validationErrors.value : []), ...props.serviceErrors])])

watch(
  () => [props.open, props.player, props.mode] as const,
  () => {
    if (!props.open) {
      return
    }

    form.submitted = false
    form.nomeExibicao = props.player?.nomeExibicao ?? ''
    form.discord = props.player?.discord ?? ''
    form.riotId = props.player?.riotId ?? ''
    form.opGgUrl = props.player?.opGgUrl ?? ''
    form.deepLolUrl = props.player?.deepLolUrl ?? ''
    form.elo = props.player?.elo ?? ''
    form.divisao = props.player?.divisao ?? ''
    form.preferencias = props.player ? props.player.preferencias.map((preference) => ({ ...preference })) : defaultPreferences()
  },
  { immediate: true },
)

function submit() {
  form.submitted = true

  if (validationErrors.value.length > 0) {
    return
  }

  emit('submit', {
    id: props.player?.id,
    nomeExibicao: form.nomeExibicao,
    discord: form.discord,
    riotId: form.riotId,
    opGgUrl: form.opGgUrl,
    deepLolUrl: form.deepLolUrl,
    elo: form.elo || null,
    divisao: selectedEloRequiresDivision.value ? form.divisao || null : null,
    preferencias: form.preferencias,
  })
}
</script>

<template>
  <div v-if="open" class="drawer-backdrop" role="presentation">
    <aside class="player-drawer" aria-labelledby="player-form-title">
      <header class="player-drawer__header">
        <div>
          <p class="page-kicker">Jogadores</p>
          <h2 id="player-form-title">{{ title }}</h2>
        </div>
        <button type="button" aria-label="Fechar formulario" @click="$emit('close')">x</button>
      </header>

      <form class="player-form" @submit.prevent="submit">
        <label class="player-form__field player-form__field--wide">
          Nome de exibicao
          <input v-model="form.nomeExibicao" autocomplete="off" placeholder="Hide on bush" />
        </label>
        <label class="player-form__field">
          Discord
          <input v-model="form.discord" autocomplete="off" placeholder="usuario#1234" />
        </label>
        <label class="player-form__field">
          Riot ID
          <input v-model="form.riotId" autocomplete="off" placeholder="Nome#BR1" />
        </label>
        <label class="player-form__field">
          Elo
          <select v-model="form.elo" @change="form.divisao = ''">
            <option value="" disabled>Selecione</option>
            <option v-for="elo in eloOptions" :key="elo" :value="elo">{{ elo }}</option>
          </select>
        </label>
        <label v-if="selectedEloRequiresDivision" class="player-form__field">
          Divisao
          <select v-model="form.divisao">
            <option value="" disabled>Selecione</option>
            <option v-for="division in divisionOptions" :key="division" :value="division">{{ division }}</option>
          </select>
        </label>
        <label class="player-form__field player-form__field--wide">
          OP.GG
          <input v-model="form.opGgUrl" autocomplete="off" placeholder="https://www.op.gg/..." />
        </label>
        <label class="player-form__field player-form__field--wide">
          Deeplol
          <input v-model="form.deepLolUrl" autocomplete="off" placeholder="https://www.deeplol.gg/..." />
        </label>

        <RoutePreferenceEditor v-model="form.preferencias" />

        <div v-if="visibleErrors.length" class="player-form__errors">
          <p v-for="error in visibleErrors" :key="error">{{ error }}</p>
        </div>

        <div class="player-drawer__actions">
          <button type="button" @click="$emit('close')">Cancelar</button>
          <button type="submit" :disabled="saving">{{ saving ? 'Salvando...' : 'Salvar' }}</button>
        </div>
      </form>
    </aside>
  </div>
</template>
