<script setup lang="ts">
import { computed, reactive, watch } from 'vue'

import RoutePreferenceEditor from '@/components/RoutePreferenceEditor.vue'
import { LeagueRole } from '@/constants/leagueRoles'
import { Divisao, Elo, type RoutePreference } from '@/services/players'
import type { MeuJogadorProfile, MeuJogadorProfilePayload } from '@/types/meuJogador'

const props = defineProps<{
  jogador: MeuJogadorProfile | null
  saving: boolean
  errors: string[]
}>()

const emit = defineEmits<{
  submit: [payload: MeuJogadorProfilePayload]
}>()

const defaultPreferences = (): RoutePreference[] => [
  { rota: LeagueRole.Top, prioridade: 1, naoJogoNemLascando: false },
  { rota: LeagueRole.Jungle, prioridade: 2, naoJogoNemLascando: false },
  { rota: LeagueRole.Mid, prioridade: 3, naoJogoNemLascando: false },
  { rota: LeagueRole.Adc, prioridade: 4, naoJogoNemLascando: false },
  { rota: LeagueRole.Support, prioridade: 5, naoJogoNemLascando: false },
]

const eloOptions: Elo[] = [Elo.Ferro, Elo.Bronze, Elo.Prata, Elo.Ouro, Elo.Platina, Elo.Esmeralda, Elo.Diamante, Elo.Mestre, Elo.GraoMestre, Elo.Desafiante]
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

const title = computed(() => (props.jogador ? 'Meu perfil de jogador' : 'Complete seu perfil de jogador'))
const selectedEloRequiresDivision = computed(() => form.elo !== '' && elosComDivisao.has(form.elo))

const validationErrors = computed(() => {
  const messages: string[] = []
  const requiredFields: Array<[string, string]> = [
    [form.nomeExibicao, 'Informe o nome de exibição.'],
    [form.discord, 'Informe o Discord.'],
    [form.riotId, 'Informe o Riot ID.'],
    [form.opGgUrl, 'Informe o link de OP.GG.'],
    [form.deepLolUrl, 'Informe o link de DeepLOL.'],
  ]

  requiredFields.forEach(([value, message]) => {
    if (!String(value).trim()) {
      messages.push(message)
    }
  })

  if (!form.elo) {
    messages.push('Selecione um elo.')
  }

  if (selectedEloRequiresDivision.value && !form.divisao) {
    messages.push('Selecione uma divisão.')
  }

  if (form.opGgUrl && !form.opGgUrl.startsWith('https://')) {
    messages.push('OP.GG deve iniciar com https://')
  }

  if (form.deepLolUrl && !form.deepLolUrl.startsWith('https://')) {
    messages.push('DeepLOL deve iniciar com https://')
  }

  if (new Set(form.preferencias.map((preference) => preference.prioridade)).size !== 5) {
    messages.push('Cada prioridade de rota deve ser única.')
  }

  if (form.preferencias.filter((preference) => preference.naoJogoNemLascando).length > 1) {
    messages.push('Marque no máximo uma rota como não jogo nem lascando.')
  }

  return messages
})

const visibleErrors = computed(() => [...new Set([...(form.submitted ? validationErrors.value : []), ...props.errors])])

watch(
  () => props.jogador,
  (jogador) => {
    form.submitted = false
    form.nomeExibicao = jogador?.nomeExibicao ?? ''
    form.discord = jogador?.discord ?? ''
    form.riotId = jogador?.riotId ?? ''
    form.opGgUrl = jogador?.opGgUrl ?? ''
    form.deepLolUrl = jogador?.deepLolUrl ?? ''
    form.elo = jogador?.elo ?? ''
    form.divisao = jogador?.divisao ?? ''
    form.preferencias = jogador ? jogador.preferencias.map((preference) => ({ ...preference })) : defaultPreferences()
  },
  { immediate: true },
)

function submit() {
  form.submitted = true
  if (validationErrors.value.length > 0 || !form.elo) {
    return
  }

  emit('submit', {
    nomeExibicao: form.nomeExibicao,
    discord: form.discord,
    riotId: form.riotId,
    opGgUrl: form.opGgUrl,
    deepLolUrl: form.deepLolUrl,
    elo: form.elo,
    divisao: selectedEloRequiresDivision.value ? form.divisao || null : null,
    preferencias: form.preferencias,
  })
}
</script>

<template>
  <form class="panel-card complete-player-card" @submit.prevent="submit">
    <header>
      <span class="eyebrow">Jogador</span>
      <h2>{{ title }}</h2>
      <p>
        Preencha todos os dados para aparecer na lista de jogadores e ficar disponível para drafts.
      </p>
    </header>

    <label>
      Nome de exibição
      <input v-model="form.nomeExibicao" required maxlength="100" placeholder="Nome no campeonato" />
    </label>
    <label>
      Discord
      <input v-model="form.discord" required maxlength="120" placeholder="usuario#1234" />
    </label>
    <label>
      Riot ID
      <input v-model="form.riotId" required maxlength="120" placeholder="Nome#BR1" />
    </label>
    <label>
      Elo
      <select v-model="form.elo" required @change="form.divisao = ''">
        <option value="" disabled>Selecione</option>
        <option v-for="elo in eloOptions" :key="elo" :value="elo">{{ elo }}</option>
      </select>
    </label>
    <label v-if="selectedEloRequiresDivision">
      Divisão
      <select v-model="form.divisao" required>
        <option value="" disabled>Selecione</option>
        <option v-for="division in divisionOptions" :key="division" :value="division">{{ division }}</option>
      </select>
    </label>
    <label>
      OP.GG
      <input v-model="form.opGgUrl" required placeholder="https://www.op.gg/..." />
    </label>
    <label>
      DeepLOL
      <input v-model="form.deepLolUrl" required placeholder="https://www.deeplol.gg/..." />
    </label>

    <RoutePreferenceEditor v-model="form.preferencias" />

    <div v-if="visibleErrors.length" class="form-errors complete-player-card__errors" role="alert">
      <p v-for="error in visibleErrors" :key="error">{{ error }}</p>
    </div>

    <button class="button button--primary" type="submit" :disabled="saving">
      {{ saving ? 'Salvando...' : jogador ? 'Salvar perfil de jogador' : 'Completar perfil de jogador' }}
    </button>
  </form>
</template>
