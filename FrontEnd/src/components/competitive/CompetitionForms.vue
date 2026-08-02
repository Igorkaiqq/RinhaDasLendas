<script setup lang="ts">
import { nextTick, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'

import type {
  CompetitionDetail,
  CreateCompetitionRequest,
  CreateRoundRequest,
  DraftMode,
  SeriesFormat,
} from '@/types/competition'
import type { SeasonSummary } from '@/types/season'

const props = defineProps<{
  season: SeasonSummary
  saving: boolean
  fieldErrors: Record<string, string>
  mutationVersion: number
}>()

const emit = defineEmits<{
  createCompetition: [payload: CreateCompetitionRequest]
  updateCompetition: [competition: CompetitionDetail, payload: CreateCompetitionRequest]
  createRound: [competition: CompetitionDetail, payload: CreateRoundRequest]
  publishCompetitionRules: [
    competition: CompetitionDetail,
    payload: { formato: SeriesFormat; modoDraft: DraftMode },
  ]
  publishSeasonRules: [payload: { formato: SeriesFormat; modoDraft: DraftMode }]
}>()

const { t } = useI18n()
const competitionFormOpen = ref(false)
const roundFormCompetition = ref<CompetitionDetail | null>(null)
const editingCompetition = ref<CompetitionDetail | null>(null)
const rulesTarget = ref<CompetitionDetail | 'season' | null>(null)
const competitionForm = reactive({ nome: '', codigo: '', circuitoDiario: false })
const roundForm = reactive({ nome: '', ordem: '' })
const rulesForm = reactive<{ formato: SeriesFormat; modoDraft: DraftMode }>({
  formato: 'Md3',
  modoDraft: 'Padrao',
})
const competitionErrors = reactive({ nome: '', codigo: '' })
const roundErrors = reactive({ nome: '', ordem: '' })

watch(
  () => props.fieldErrors,
  async (fieldErrors) => {
    competitionErrors.nome = fieldErrors.nome ?? ''
    competitionErrors.codigo = fieldErrors.codigo ?? ''
    roundErrors.nome = fieldErrors.roundNome ?? ''
    roundErrors.ordem = fieldErrors.roundOrdem ?? ''
    await nextTick()
    const firstInvalid = competitionErrors.nome
      ? 'competition-name'
      : competitionErrors.codigo
        ? 'competition-code'
        : roundErrors.nome
          ? 'round-name'
          : roundErrors.ordem
            ? 'round-order'
            : null
    if (firstInvalid) globalThis.document.getElementById(firstInvalid)?.focus()
  },
  { deep: true },
)

watch(
  () => props.mutationVersion,
  (version, previousVersion) => {
    if (version !== previousVersion) closeForms()
  },
)

function openCompetition(competition: CompetitionDetail | null = null) {
  if (props.saving) return
  editingCompetition.value = competition
  competitionForm.nome = competition?.nome ?? ''
  competitionForm.codigo = competition?.codigo ?? ''
  competitionForm.circuitoDiario = competition?.circuitoDiario ?? false
  competitionErrors.nome = ''
  competitionErrors.codigo = ''
  competitionFormOpen.value = true
  void nextTick(() => globalThis.document.getElementById('competition-name')?.focus())
}

function openRound(competition: CompetitionDetail) {
  if (props.saving) return
  roundFormCompetition.value = competition
  roundForm.nome = ''
  roundForm.ordem = String(competition.rodadas.length + 1)
  roundErrors.nome = ''
  roundErrors.ordem = ''
  void nextTick(() => globalThis.document.getElementById('round-name')?.focus())
}

function openRules(target: CompetitionDetail | 'season') {
  if (props.saving) return
  rulesTarget.value = target
}

function submitCompetition() {
  if (props.saving) return
  competitionErrors.nome = competitionForm.nome.trim()
    ? ''
    : t('seasons.competitions.validation.name')
  competitionErrors.codigo = competitionForm.codigo.trim()
    ? ''
    : t('seasons.competitions.validation.code')
  if (competitionErrors.nome || competitionErrors.codigo) {
    void nextTick(() =>
      globalThis.document
        .getElementById(competitionErrors.nome ? 'competition-name' : 'competition-code')
        ?.focus(),
    )
    return
  }
  const payload = {
    nome: competitionForm.nome.trim(),
    codigo: competitionForm.codigo.trim(),
    circuitoDiario: competitionForm.circuitoDiario,
  }
  if (editingCompetition.value) emit('updateCompetition', editingCompetition.value, payload)
  else emit('createCompetition', payload)
}

function submitRound() {
  if (props.saving) return
  const order = Number(roundForm.ordem)
  roundErrors.nome = roundForm.nome.trim() ? '' : t('seasons.rounds.validation.name')
  roundErrors.ordem =
    Number.isInteger(order) && order > 0 ? '' : t('seasons.rounds.validation.order')
  if (roundErrors.nome || roundErrors.ordem) {
    void nextTick(() =>
      globalThis.document
        .getElementById(roundErrors.nome ? 'round-name' : 'round-order')
        ?.focus(),
    )
    return
  }
  if (roundFormCompetition.value) {
    emit('createRound', roundFormCompetition.value, {
      nome: roundForm.nome.trim(),
      ordem: order,
    })
  }
}

function closeForms() {
  competitionFormOpen.value = false
  roundFormCompetition.value = null
  editingCompetition.value = null
  rulesTarget.value = null
  competitionForm.nome = ''
  competitionForm.codigo = ''
  competitionForm.circuitoDiario = false
  roundForm.nome = ''
  roundForm.ordem = ''
  rulesForm.formato = 'Md3'
  rulesForm.modoDraft = 'Padrao'
  competitionErrors.nome = ''
  competitionErrors.codigo = ''
  roundErrors.nome = ''
  roundErrors.ordem = ''
}

function requestClose() {
  if (!props.saving) closeForms()
}

function submitRules() {
  if (props.saving || !rulesTarget.value) return
  if (rulesTarget.value === 'season') emit('publishSeasonRules', { ...rulesForm })
  else emit('publishCompetitionRules', rulesTarget.value, { ...rulesForm })
}

defineExpose({ openCompetition, openRound, openRules })
</script>

<template>
  <section v-if="competitionFormOpen" class="inline-form-card">
    <h3>{{ t(`seasons.competitions.${editingCompetition ? 'editTitle' : 'createTitle'}`) }}</h3>
    <form id="competition-form" novalidate @submit.prevent="submitCompetition">
      <label for="competition-name">{{ t('seasons.competitions.name') }}</label>
      <input id="competition-name" v-model="competitionForm.nome" name="competitionName" autocomplete="off" :disabled="saving" :aria-invalid="Boolean(competitionErrors.nome)" :aria-errormessage="competitionErrors.nome ? 'competition-name-error' : undefined" />
      <p v-if="competitionErrors.nome" id="competition-name-error" role="alert">{{ competitionErrors.nome }}</p>
      <label for="competition-code">{{ t('seasons.competitions.code') }}</label>
      <input id="competition-code" v-model="competitionForm.codigo" name="competitionCode" autocomplete="off" :disabled="saving" :aria-invalid="Boolean(competitionErrors.codigo)" :aria-errormessage="competitionErrors.codigo ? 'competition-code-error' : undefined" />
      <p v-if="competitionErrors.codigo" id="competition-code-error" role="alert">{{ competitionErrors.codigo }}</p>
      <label class="inline-form-card__check" for="competition-daily-circuit">
        <input id="competition-daily-circuit" v-model="competitionForm.circuitoDiario" type="checkbox" name="competitionDailyCircuit" autocomplete="off" :disabled="saving" />
        {{ t('seasons.competitions.dailyCircuit') }}
      </label>
      <footer>
        <button type="button" :disabled="saving" @click="requestClose">{{ t('common.cancel') }}</button>
        <button class="primary" type="submit" :disabled="saving">{{ t('common.save') }}</button>
      </footer>
    </form>
  </section>

  <section v-if="roundFormCompetition" class="inline-form-card">
    <h3>{{ t('seasons.rounds.createTitle', { name: roundFormCompetition.nome }) }}</h3>
    <form id="round-form" novalidate @submit.prevent="submitRound">
      <label for="round-name">{{ t('seasons.rounds.name') }}</label>
      <input id="round-name" v-model="roundForm.nome" name="roundName" autocomplete="off" :disabled="saving" :aria-invalid="Boolean(roundErrors.nome)" :aria-errormessage="roundErrors.nome ? 'round-name-error' : undefined" />
      <p v-if="roundErrors.nome" id="round-name-error" role="alert">{{ roundErrors.nome }}</p>
      <label for="round-order">{{ t('seasons.rounds.order') }}</label>
      <input id="round-order" v-model="roundForm.ordem" type="number" name="roundOrder" autocomplete="off" :disabled="saving" :aria-invalid="Boolean(roundErrors.ordem)" :aria-errormessage="roundErrors.ordem ? 'round-order-error' : undefined" />
      <p v-if="roundErrors.ordem" id="round-order-error" role="alert">{{ roundErrors.ordem }}</p>
      <footer>
        <button type="button" :disabled="saving" @click="requestClose">{{ t('common.cancel') }}</button>
        <button class="primary" type="submit" :disabled="saving">{{ t('common.save') }}</button>
      </footer>
    </form>
  </section>

  <section v-if="rulesTarget" class="inline-form-card">
    <h3>{{ t('seasons.rules.publishTitle', { name: rulesTarget === 'season' ? season.nome : rulesTarget.nome }) }}</h3>
    <form @submit.prevent="submitRules">
      <label for="rules-format">{{ t('seasons.rules.format') }}</label>
      <select id="rules-format" v-model="rulesForm.formato" name="rulesFormat" autocomplete="off" :disabled="saving">
        <option value="Md3">{{ t('seasons.rules.formats.Md3') }}</option>
        <option value="Md5">{{ t('seasons.rules.formats.Md5') }}</option>
      </select>
      <label for="rules-mode">{{ t('seasons.rules.mode') }}</label>
      <select id="rules-mode" v-model="rulesForm.modoDraft" name="rulesMode" autocomplete="off" :disabled="saving">
        <option value="Padrao">{{ t('seasons.rules.modes.Padrao') }}</option>
        <option value="Fearless">{{ t('seasons.rules.modes.Fearless') }}</option>
      </select>
      <footer>
        <button type="button" :disabled="saving" @click="requestClose">{{ t('common.cancel') }}</button>
        <button class="primary" type="submit" :disabled="saving">{{ t('seasons.rules.publish') }}</button>
      </footer>
    </form>
  </section>
</template>

<style scoped>
.inline-form-card {
  min-width: 0;
  padding: var(--space-lg);
  border: 1px solid var(--color-hairline);
  border-radius: var(--radius-lg);
  background: var(--color-surface-1);
  overflow-wrap: anywhere;
  overscroll-behavior: contain;
}

.inline-form-card form {
  display: grid;
  gap: var(--space-xs);
  margin-top: var(--space-md);
}

.inline-form-card input,
.inline-form-card select,
.inline-form-card button {
  width: 100%;
  min-width: 0;
  min-height: 44px;
  padding-inline: var(--space-sm);
  border: 1px solid var(--color-hairline-strong);
  border-radius: var(--radius-md);
  color: var(--color-ink);
  background: var(--color-surface-2);
  transition:
    background-color var(--duration-fast) var(--ease-standard),
    border-color var(--duration-fast) var(--ease-standard),
    transform var(--duration-fast) var(--ease-standard);
}

.inline-form-card input:focus-visible,
.inline-form-card select:focus-visible,
.inline-form-card button:focus-visible {
  outline: 2px solid var(--color-focus-ring);
  outline-offset: 2px;
}

.inline-form-card input:hover,
.inline-form-card select:hover,
.inline-form-card button:not(:disabled):hover {
  border-color: var(--color-primary-hover);
}

.inline-form-card button:not(:disabled):active {
  transform: translateY(1px);
}

.inline-form-card .primary {
  border-color: var(--color-primary);
  background: var(--color-primary);
}

.inline-form-card p {
  margin: 0;
  color: var(--color-danger);
  font-size: 14px;
}

.inline-form-card__check {
  display: flex;
  align-items: center;
  gap: var(--space-xs);
  min-height: 44px;
}

.inline-form-card__check input {
  width: auto;
  min-height: auto;
}

.inline-form-card footer {
  display: flex;
  justify-content: flex-end;
  gap: var(--space-sm);
  margin-top: var(--space-sm);
}

@media (max-width: 767px) {
  .inline-form-card footer {
    align-items: stretch;
    flex-direction: column;
  }
}

@media (prefers-reduced-motion: reduce) {
  .inline-form-card input,
  .inline-form-card select,
  .inline-form-card button {
    transition: none;
  }

  .inline-form-card button:active {
    transform: none;
  }
}
</style>
