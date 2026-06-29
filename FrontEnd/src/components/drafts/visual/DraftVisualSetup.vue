<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'

import type { Player } from '@/services/players'
import type { DraftMontagemPayload } from '@/types/draftMontagem'

const props = defineProps<{ open: boolean; players: Player[]; captains: Player[]; saving: boolean; errors: string[] }>()
const { t } = useI18n()

const emit = defineEmits<{ close: []; submit: [payload: DraftMontagemPayload] }>()

const search = ref('')
const form = reactive({
  nome: '',
  observacoes: '',
  tamanhoEquipe: 5,
  horarioEncerramentoPresenca: '',
  sortearCapitaes: true,
  jogadoresIds: [] as string[],
  capitaesIds: [] as string[],
})

const filteredPlayers = computed(() => {
  const term = search.value.trim().toLowerCase()
  return props.players.filter((player) => !term || player.nomeExibicao.toLowerCase().includes(term) || player.discord?.toLowerCase().includes(term) || player.riotId?.toLowerCase().includes(term))
})

const quantidadeTimes = computed(() => Math.floor(form.jogadoresIds.length / Math.max(form.tamanhoEquipe, 1)))
const quantidadeReservas = computed(() => form.jogadoresIds.length % Math.max(form.tamanhoEquipe, 1))
const canSubmit = computed(() => Boolean(form.nome.trim()))
const selectedPlayers = computed(() => props.players.filter((player) => form.jogadoresIds.includes(player.id)))
const selectedCaptains = computed(() => props.captains.filter((player) => form.jogadoresIds.includes(player.id)))

function togglePlayer(playerId: string) {
  if (form.jogadoresIds.includes(playerId)) {
    form.jogadoresIds = form.jogadoresIds.filter((id) => id !== playerId)
    form.capitaesIds = form.capitaesIds.filter((id) => id !== playerId)
    return
  }
  form.jogadoresIds = [...form.jogadoresIds, playerId]
}

function toggleCaptain(playerId: string) {
  if (form.capitaesIds.includes(playerId)) {
    form.capitaesIds = form.capitaesIds.filter((id) => id !== playerId)
    return
  }
  if (form.capitaesIds.length >= quantidadeTimes.value) {
    return
  }
  form.capitaesIds = [...form.capitaesIds, playerId]
}

function submit() {
  if (!canSubmit.value) {
    return
  }
  emit('submit', {
    nome: form.nome,
    observacoes: form.observacoes || null,
    tamanhoEquipe: form.tamanhoEquipe,
    horarioEncerramentoPresenca: form.horarioEncerramentoPresenca || null,
    sortearCapitaes: form.sortearCapitaes,
    capitaesIds: form.sortearCapitaes ? [] : form.capitaesIds,
    jogadoresIds: form.jogadoresIds,
  })
}
</script>

<template>
  <div v-if="open" class="player-modal-backdrop" role="presentation">
    <section class="player-modal draft-create-modal" role="dialog" aria-modal="true" aria-labelledby="draft-visual-title">
      <header class="player-modal__header">
        <div>
          <span class="eyebrow">{{ t('drafts.visualSetup.eyebrow') }}</span>
          <h2 id="draft-visual-title">{{ t('drafts.visualSetup.title') }}</h2>
        </div>
        <button type="button" :aria-label="t('common.close')" @click="emit('close')">x</button>
      </header>

      <form class="player-form draft-create-form" @submit.prevent="submit">
        <div v-if="errors.length" class="form-errors" role="alert">
          <p v-for="error in errors" :key="error">{{ error }}</p>
        </div>
        <label class="player-form__field">
          {{ t('drafts.createModal.name') }}
          <input v-model="form.nome" required :placeholder="t('drafts.visualSetup.namePlaceholder')" />
        </label>
        <label class="player-form__field">
          {{ t('drafts.visualSetup.teamSize') }}
          <input v-model.number="form.tamanhoEquipe" type="number" min="1" max="5" />
        </label>
        <label class="player-form__field">
          {{ t('drafts.presence.closeAt') }}
          <input v-model="form.horarioEncerramentoPresenca" type="datetime-local" />
        </label>
        <label class="player-form__field player-form__field--wide">
          {{ t('drafts.createModal.notes') }}
          <textarea v-model="form.observacoes" rows="2" />
        </label>

        <section class="draft-player-picker player-form__field--wide">
          <div class="draft-player-picker__header">
            <div>
              <span class="eyebrow">{{ t('drafts.visualSetup.players') }}</span>
              <h3>{{ t('drafts.createModal.selectedCount', { count: form.jogadoresIds.length }) }}</h3>
            </div>
            <span>{{ t('drafts.visualSetup.summary', { teams: quantidadeTimes, captains: quantidadeTimes, reserves: quantidadeReservas }) }}</span>
          </div>
          <label class="draft-search-field">
            <span aria-hidden="true">S</span>
            <input v-model="search" type="search" :placeholder="t('drafts.visualSetup.searchPlayer')" />
          </label>
          <div class="draft-player-picker__grid">
            <button v-for="player in filteredPlayers" :key="player.id" type="button" class="draft-player-option" :class="{ 'is-selected': form.jogadoresIds.includes(player.id) }" @click="togglePlayer(player.id)">
              <span class="draft-slot__avatar">{{ player.nomeExibicao.charAt(0) }}</span>
              <span>
                <strong>{{ player.nomeExibicao }}</strong>
                <small>{{ player.elo ? `${player.elo} ${player.divisao ?? ''}` : t('common.eloNotInformed') }}</small>
              </span>
            </button>
          </div>
        </section>

        <label class="checkbox-line">
          <input v-model="form.sortearCapitaes" type="checkbox" />
          {{ t('drafts.visualSetup.drawCaptainsAutomatically') }}
        </label>

        <section v-if="!form.sortearCapitaes" class="draft-player-picker player-form__field--wide">
          <div class="draft-player-picker__header">
            <h3>{{ t('drafts.visualSetup.captainsCount', { selected: form.capitaesIds.length, total: quantidadeTimes }) }}</h3>
          </div>
          <div class="draft-player-picker__grid">
            <button v-for="player in selectedCaptains" :key="player.id" type="button" class="draft-player-option" :class="{ 'is-selected': form.capitaesIds.includes(player.id) }" @click="toggleCaptain(player.id)">
              <span class="draft-slot__avatar">{{ player.nomeExibicao.charAt(0) }}</span>
              <span><strong>{{ player.nomeExibicao }}</strong><small>{{ t('drafts.roles.captain') }}</small></span>
            </button>
            <p v-if="selectedPlayers.length && !selectedCaptains.length">{{ t('drafts.visualSetup.noCaptainRole') }}</p>
          </div>
        </section>

        <footer class="player-modal__actions">
          <button type="button" class="button-secondary" @click="emit('close')">{{ t('common.cancel') }}</button>
          <button type="submit" :disabled="saving || !canSubmit">{{ saving ? t('drafts.createModal.creating') : t('drafts.createModal.submit') }}</button>
        </footer>
      </form>
    </section>
  </div>
</template>
