<script setup lang="ts">
import { computed, nextTick, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'

import type {
  AnnulMatchRequest,
  ConfirmMatchResultRequest,
  CorrectMatchRequest,
  MatchDetail,
  MatchEndReason,
  RegisterMatchPicksRequest,
  RegisterMatchRemakeRequest,
  RemakePickDecision,
} from '@/types/match'
import type { SeriesDetail } from '@/types/series'

import { useModalFocus } from './useModalFocus'

const props = withDefaults(
  defineProps<{
    match: MatchDetail
    series: SeriesDetail
    busy: boolean
    disabled?: boolean
    fieldErrors?: Record<string, string>
  }>(),
  { disabled: false, fieldErrors: () => ({}) },
)

const emit = defineEmits<{
  savePicks: [payload: RegisterMatchPicksRequest]
  confirmResult: [payload: ConfirmMatchResultRequest]
  remake: [payload: RegisterMatchRemakeRequest]
  annul: [payload: AnnulMatchRequest]
  correct: [payload: CorrectMatchRequest]
}>()

const { t } = useI18n()
const dialog = ref<'remake' | 'annul' | 'correction' | null>(null)
const dialogPanel = ref<InstanceType<typeof globalThis.HTMLElement> | null>(
  null,
)
const localAlert = ref('')
const pickValues = ref<string[]>(Array.from({ length: 10 }, () => ''))
const winner = ref('')
const endReason = ref<MatchEndReason>('Normal')
const remakeDecision = ref<RemakePickDecision | ''>('')
const remakeReason = ref('')
const annulReason = ref('')
const annulSeries = ref(false)
const correctedWinner = ref('')
const correctedEndReason = ref<MatchEndReason>('Normal')
const correctionReason = ref('')
const correctionPicks = ref<string[]>(Array.from({ length: 10 }, () => ''))
const initialCorrectionPicks = ref<string[]>([])
const validation = ref<Record<string, string>>({})

const can = (action: string) => props.match.acoesPermitidas.includes(action)
const reviewBlocked = computed(() => props.series.revisaoNecessaria)
const controlsDisabled = computed(() => props.busy || props.disabled)
const correctionImpact = computed(() => {
  if (
    props.series.estado !== 'Concluida' ||
    !props.match.ladoVencedorId ||
    !correctedWinner.value ||
    correctedWinner.value === props.match.ladoVencedorId
  ) {
    return null
  }
  const score: [number, number] = [...props.series.placar]
  const previous = props.series.lados.findIndex(
    (side) => side.id === props.match.ladoVencedorId,
  )
  const next = props.series.lados.findIndex(
    (side) => side.id === correctedWinner.value,
  )
  if (previous < 0 || next < 0) return null
  score[previous] = Math.max(0, score[previous]! - 1)
  score[next] = score[next]! + 1
  const requiredWins = props.series.formato === 'Md5' ? 3 : 2
  if (score.some((value) => value >= requiredWins)) return null
  return { before: props.series.placar, after: score }
})

watch(
  () => props.match,
  (match) => {
    const grouped = props.series.lados.flatMap((side) =>
      match.picks
        .filter((pick) => pick.ladoSerieId === side.id)
        .sort((left, right) => left.ordem - right.ordem)
        .map((pick) => String(pick.championId)),
    )
    if (grouped.length === 10) pickValues.value = grouped
  },
  { immediate: true },
)

watch(
  () => props.fieldErrors,
  async (errors) => {
    validation.value = { ...validation.value, ...errors }
    const field = ['remakeReason', 'annulReason', 'correctionReason'].find(
      (candidate) => errors[candidate],
    )
    if (field) {
      await nextTick()
      const ids: Record<string, string> = {
        remakeReason: 'remake-reason',
        annulReason: 'annul-reason',
        correctionReason: 'correction-reason',
      }
      globalThis.document.getElementById(ids[field]!)?.focus()
    }
  },
  { deep: true },
)

useModalFocus(
  () => dialog.value !== null,
  dialogPanel,
  closeDialog,
  () =>
    dialogPanel.value?.querySelector<
      InstanceType<typeof globalThis.HTMLElement>
    >('input, textarea, select, button') ?? null,
)

function openDialog(kind: 'remake' | 'annul' | 'correction') {
  resetDialogState()
  dialog.value = kind
  if (kind === 'correction') {
    correctedWinner.value = ''
    correctedEndReason.value = props.match.motivoTermino ?? 'Normal'
    correctionPicks.value = props.series.lados.flatMap((side) => {
      const values = props.match.picks
        .filter((pick) => pick.ladoSerieId === side.id)
        .sort((left, right) => left.ordem - right.ordem)
        .map((pick) => String(pick.championId))
      return values.length === 5 ? values : Array.from({ length: 5 }, () => '')
    })
    initialCorrectionPicks.value = [...correctionPicks.value]
  }
}

function closeDialog() {
  if (!props.busy) {
    dialog.value = null
    resetDialogState()
  }
}

function completeOperation() {
  dialog.value = null
  resetDialogState()
}

function resetDialogState() {
  localAlert.value = ''
  validation.value = {}
  remakeDecision.value = ''
  remakeReason.value = ''
  annulReason.value = ''
  annulSeries.value = false
  correctedWinner.value = ''
  correctionReason.value = ''
  initialCorrectionPicks.value = []
}

function picksPayload(values: string[]): RegisterMatchPicksRequest | null {
  const ids = values.map(Number)
  const invalidIndex = ids.findIndex((id) => !Number.isInteger(id) || id <= 0)
  if (invalidIndex >= 0) {
    localAlert.value = t('competitive.series.match.picks.required')
    void focusPick(invalidIndex, values === correctionPicks.value)
    return null
  }
  const duplicateIndex = ids.findIndex((id, index) => ids.indexOf(id) !== index)
  if (duplicateIndex >= 0) {
    localAlert.value = t('competitive.series.match.picks.unique')
    void focusPick(duplicateIndex, values === correctionPicks.value)
    return null
  }
  return {
    lados: [
      {
        ladoSerieId: props.series.lados[0]!.id,
        championIds: ids.slice(0, 5) as [
          number,
          number,
          number,
          number,
          number,
        ],
      },
      {
        ladoSerieId: props.series.lados[1]!.id,
        championIds: ids.slice(5, 10) as [
          number,
          number,
          number,
          number,
          number,
        ],
      },
    ],
  }
}

async function focusPick(index: number, correction: boolean) {
  await nextTick()
  const selector = correction
    ? '[data-correction-pick]'
    : 'input[data-champion-pick]:not([data-correction-pick])'
  globalThis.document
    .querySelectorAll<
      InstanceType<typeof globalThis.HTMLInputElement>
    >(selector)
    [index]?.focus()
}

function savePicks() {
  localAlert.value = ''
  const payload = picksPayload(pickValues.value)
  if (payload) {
    emit('savePicks', payload)
  }
}

async function confirmResult() {
  validation.value = {}
  if (!winner.value) {
    validation.value.winner = t(
      'competitive.series.match.result.winnerRequired',
    )
    await nextTick()
    globalThis.document
      .querySelector<
        InstanceType<typeof globalThis.HTMLInputElement>
      >('input[name="winner"]')
      ?.focus()
    return
  }
  const payload = {
    ladoVencedorId: winner.value,
    motivoTermino: endReason.value,
  }
  emit('confirmResult', payload)
}

async function confirmRemake() {
  validation.value = {}
  if (!remakeDecision.value)
    validation.value.remakeDecision = t(
      'competitive.series.match.remake.decisionRequired',
    )
  if (!remakeReason.value.trim())
    validation.value.remakeReason = t(
      'competitive.series.match.remake.reasonRequired',
    )
  if (Object.keys(validation.value).length > 0) {
    await nextTick()
    const target = validation.value.remakeDecision
      ? 'input[name="remakePickDecision"]'
      : 'textarea[name="remakeReason"]'
    globalThis.document
      .querySelector<InstanceType<typeof globalThis.HTMLInputElement>>(target)
      ?.focus()
    return
  }
  emit('remake', {
    decisaoPicks: remakeDecision.value as RemakePickDecision,
    justificativa: remakeReason.value.trim(),
  })
}

async function confirmAnnul() {
  validation.value = {}
  if (!annulReason.value.trim()) {
    validation.value.annulReason = t(
      'competitive.series.match.annul.reasonRequired',
    )
    await nextTick()
    globalThis.document.getElementById('annul-reason')?.focus()
    return
  }
  emit('annul', {
    justificativa: annulReason.value.trim(),
    anularSerieSeInconclusiva: annulSeries.value,
  })
}

async function confirmCorrection() {
  validation.value = {}
  if (!correctionReason.value.trim()) {
    validation.value.correctionReason = t(
      'competitive.series.match.correction.reasonRequired',
    )
    await nextTick()
    globalThis.document.getElementById('correction-reason')?.focus()
  }
  if (correctionImpact.value && !annulSeries.value)
    validation.value.correctionAnnulSeries = t(
      'competitive.series.match.correction.annulSeriesRequired',
    )
  if (Object.keys(validation.value).length > 0) {
    await nextTick()
    const target = validation.value.correctionReason
      ? '#correction-reason'
      : '#correction-annul-series'
    globalThis.document
      .querySelector<InstanceType<typeof globalThis.HTMLElement>>(target)
      ?.focus()
    return
  }
  const payload: Record<string, unknown> = {
    justificativa: correctionReason.value.trim(),
  }
  if (
    correctionPicks.value.some(Boolean) &&
    correctionPicks.value.some(
      (value, index) => value !== initialCorrectionPicks.value[index],
    )
  ) {
    const picks = picksPayload(correctionPicks.value)
    if (!picks) return
    payload.lados = picks.lados
  }
  if (correctedWinner.value) {
    payload.ladoVencedorId = correctedWinner.value
    payload.motivoTermino = correctedEndReason.value
  }
  if (annulSeries.value) payload.anularSerieSeInconclusiva = true
  emit('correct', payload as unknown as CorrectMatchRequest)
}

defineExpose({ completeOperation })
</script>

<template>
  <section class="match-operation" :aria-busy="busy">
    <div data-operation-feedback aria-live="polite">
      <p v-if="localAlert" role="alert">{{ localAlert }}</p>
      <p v-if="busy" role="status">{{ t('common.saving') }}</p>
    </div>

    <form
      v-if="can('register-picks')"
      class="match-operation__section"
      @submit.prevent="savePicks"
    >
      <fieldset>
        <legend>{{ t('competitive.series.match.picks.title') }}</legend>
        <p>{{ t('competitive.series.match.picks.description') }}</p>
        <div
          v-for="(side, sideIndex) in series.lados"
          :key="side.id"
          class="match-operation__side"
        >
          <h3>{{ side.nome }}</h3>
          <label
            v-for="index in 5"
            :key="index"
            :for="`pick-${sideIndex}-${index}`"
          >
            <span>{{
              t('competitive.series.match.picks.position', { position: index })
            }}</span>
            <input
              :id="`pick-${sideIndex}-${index}`"
              v-model="pickValues[sideIndex * 5 + index - 1]"
              data-champion-pick
              :name="`pick-${sideIndex}-${index}`"
              type="number"
              min="1"
              inputmode="numeric"
              autocomplete="off"
              :disabled="controlsDisabled"
            />
          </label>
        </div>
      </fieldset>
      <button type="button" :disabled="controlsDisabled" @click="savePicks">
        {{ t('competitive.series.match.picks.save') }}
      </button>
    </form>

    <form
      v-if="can('confirm')"
      class="match-operation__section"
      @submit.prevent="confirmResult"
    >
      <fieldset>
        <legend>{{ t('competitive.series.match.result.title') }}</legend>
        <label
          v-for="(side, index) in series.lados"
          :key="side.id"
          :for="`winner-${index}`"
        >
          <input
            :id="`winner-${index}`"
            v-model="winner"
            type="radio"
            name="winner"
            :value="side.id"
            autocomplete="off"
            :disabled="controlsDisabled || reviewBlocked"
            :aria-invalid="Boolean(validation.winner)"
            aria-errormessage="winner-error"
          />
          <span>{{ side.nome }}</span>
        </label>
      </fieldset>
      <p v-if="validation.winner" id="winner-error" role="alert">
        {{ validation.winner }}
      </p>
      <label for="match-end-reason">
        <span>{{ t('competitive.series.match.result.reason') }}</span>
        <select
          id="match-end-reason"
          v-model="endReason"
          name="endReason"
          autocomplete="off"
          :disabled="controlsDisabled || reviewBlocked"
        >
          <option value="Normal">
            {{ t('competitive.series.match.result.normal') }}
          </option>
          <option value="Surrender">
            {{ t('competitive.series.match.result.surrender') }}
          </option>
        </select>
      </label>
      <button
        type="button"
        :disabled="controlsDisabled || reviewBlocked"
        @click="confirmResult"
      >
        {{ t('competitive.series.match.result.confirm') }}
      </button>
    </form>

    <div class="match-operation__actions">
      <button
        v-if="can('remake')"
        type="button"
        :disabled="controlsDisabled || reviewBlocked"
        @click="openDialog('remake')"
      >
        {{ t('competitive.series.match.remake.open') }}
      </button>
      <button
        v-if="can('annul')"
        type="button"
        :disabled="controlsDisabled"
        @click="openDialog('annul')"
      >
        {{ t('competitive.series.match.annul.open') }}
      </button>
      <button
        v-if="can('correct')"
        type="button"
        :disabled="controlsDisabled"
        @click="openDialog('correction')"
      >
        {{ t('competitive.series.match.correction.open') }}
      </button>
    </div>

    <Teleport to="body">
      <div v-if="dialog" class="operation-dialog">
        <div class="operation-dialog__backdrop" aria-hidden="true" />
        <section
          ref="dialogPanel"
          class="operation-dialog__panel"
          role="alertdialog"
          aria-modal="true"
          :aria-labelledby="`${dialog}-dialog-title`"
          :aria-describedby="`${dialog}-dialog-description`"
          :aria-busy="busy"
          tabindex="-1"
        >
          <template v-if="dialog === 'remake'">
            <h2 id="remake-dialog-title">
              {{ t('competitive.series.match.remake.open') }}
            </h2>
            <p id="remake-dialog-description">
              {{ t('competitive.series.match.remake.description') }}
            </p>
            <fieldset>
              <legend>
                {{ t('competitive.series.match.remake.decision') }}
              </legend>
              <label for="remake-preserve">
                <input
                  id="remake-preserve"
                  v-model="remakeDecision"
                  type="radio"
                  name="remakePickDecision"
                  value="PreservarPicks"
                  autocomplete="off"
                  :disabled="busy"
                  :aria-invalid="Boolean(validation.remakeDecision)"
                  aria-errormessage="remake-pick-decision-error"
                />
                {{ t('competitive.series.match.remake.preserve') }}
              </label>
              <label for="remake-discard">
                <input
                  id="remake-discard"
                  v-model="remakeDecision"
                  type="radio"
                  name="remakePickDecision"
                  value="DesconsiderarPicks"
                  autocomplete="off"
                  :disabled="busy"
                  :aria-invalid="Boolean(validation.remakeDecision)"
                  aria-errormessage="remake-pick-decision-error"
                />
                {{ t('competitive.series.match.remake.discard') }}
              </label>
              <p
                v-if="validation.remakeDecision"
                id="remake-pick-decision-error"
                role="alert"
              >
                {{ validation.remakeDecision }}
              </p>
            </fieldset>
            <label for="remake-reason">
              <span>{{ t('competitive.series.match.reason') }}</span>
              <textarea
                id="remake-reason"
                v-model="remakeReason"
                name="remakeReason"
                autocomplete="off"
                :disabled="busy"
                :aria-invalid="Boolean(validation.remakeReason)"
                aria-errormessage="remake-reason-error"
              />
            </label>
            <p
              v-if="validation.remakeReason"
              id="remake-reason-error"
              role="alert"
            >
              {{ validation.remakeReason }}
            </p>
          </template>

          <template v-else-if="dialog === 'annul'">
            <h2 id="annul-dialog-title">
              {{ t('competitive.series.match.annul.open') }}
            </h2>
            <p id="annul-dialog-description">
              {{ t('competitive.series.match.annul.description') }}
            </p>
            <label for="annul-reason">
              <span>{{ t('competitive.series.match.reason') }}</span>
              <textarea
                id="annul-reason"
                v-model="annulReason"
                name="annulReason"
                autocomplete="off"
                :disabled="busy"
                :aria-invalid="Boolean(validation.annulReason)"
                aria-errormessage="annul-reason-error"
              />
            </label>
            <p
              v-if="validation.annulReason"
              id="annul-reason-error"
              role="alert"
            >
              {{ validation.annulReason }}
            </p>
            <label for="annul-series">
              <input
                id="annul-series"
                v-model="annulSeries"
                type="checkbox"
                name="annulSeries"
                autocomplete="off"
                :disabled="busy"
              />
              {{ t('competitive.series.match.annul.seriesIfNeeded') }}
            </label>
          </template>

          <template v-else>
            <h2 id="correction-dialog-title">
              {{ t('competitive.series.match.correction.open') }}
            </h2>
            <p id="correction-dialog-description">
              {{ t('competitive.series.match.correction.description') }}
            </p>
            <fieldset>
              <legend>{{ t('competitive.series.match.result.winner') }}</legend>
              <label
                v-for="(side, index) in series.lados"
                :key="side.id"
                :for="`corrected-winner-${index}`"
              >
                <input
                  :id="`corrected-winner-${index}`"
                  v-model="correctedWinner"
                  type="radio"
                  name="correctedWinner"
                  :value="side.id"
                  autocomplete="off"
                  :disabled="busy"
                />
                {{ side.nome }}
              </label>
            </fieldset>
            <label for="corrected-end-reason">
              <span>{{ t('competitive.series.match.result.reason') }}</span>
              <select
                id="corrected-end-reason"
                v-model="correctedEndReason"
                name="correctedEndReason"
                autocomplete="off"
                :disabled="busy"
              >
                <option value="Normal">
                  {{ t('competitive.series.match.result.normal') }}
                </option>
                <option value="Surrender">
                  {{ t('competitive.series.match.result.surrender') }}
                </option>
              </select>
            </label>
            <fieldset>
              <legend>
                {{ t('competitive.series.match.correction.picks') }}
              </legend>
              <div
                v-for="(side, sideIndex) in series.lados"
                :key="side.id"
                class="match-operation__side"
              >
                <h3>{{ side.nome }}</h3>
                <label
                  v-for="index in 5"
                  :key="index"
                  :for="`corrected-pick-${sideIndex}-${index}`"
                >
                  <span>{{
                    t('competitive.series.match.picks.position', {
                      position: index,
                    })
                  }}</span>
                  <input
                    :id="`corrected-pick-${sideIndex}-${index}`"
                    v-model="correctionPicks[sideIndex * 5 + index - 1]"
                    data-champion-pick
                    data-correction-pick
                    :name="`correctedPick-${sideIndex}-${index}`"
                    type="number"
                    min="1"
                    inputmode="numeric"
                    autocomplete="off"
                    :disabled="busy"
                  />
                </label>
              </div>
            </fieldset>
            <label for="correction-reason">
              <span>{{ t('competitive.series.match.reason') }}</span>
              <textarea
                id="correction-reason"
                v-model="correctionReason"
                name="correctionReason"
                autocomplete="off"
                :disabled="busy"
                :aria-invalid="Boolean(validation.correctionReason)"
                aria-errormessage="correction-reason-error"
              />
            </label>
            <p
              v-if="validation.correctionReason"
              id="correction-reason-error"
              role="alert"
            >
              {{ validation.correctionReason }}
            </p>
            <section
              v-if="correctionImpact"
              data-correction-series-impact
              aria-labelledby="correction-impact-title"
            >
              <h3 id="correction-impact-title">
                {{ t('competitive.series.match.correction.seriesImpact') }}
              </h3>
              <dl>
                <div>
                  <dt>{{ t('competitive.series.match.conflict.before') }}</dt>
                  <dd>{{ correctionImpact.before.join(' — ') }}</dd>
                </div>
                <div>
                  <dt>{{ t('competitive.series.match.conflict.after') }}</dt>
                  <dd>{{ correctionImpact.after.join(' — ') }}</dd>
                </div>
              </dl>
              <label for="correction-annul-series">
                <input
                  id="correction-annul-series"
                  v-model="annulSeries"
                  name="correctionAnnulSeries"
                  type="checkbox"
                  :disabled="busy"
                  :aria-invalid="Boolean(validation.correctionAnnulSeries)"
                  aria-errormessage="correction-annul-series-error"
                />
                {{
                  t('competitive.series.match.correction.confirmSeriesAnnul')
                }}
              </label>
              <p
                v-if="validation.correctionAnnulSeries"
                id="correction-annul-series-error"
                role="alert"
              >
                {{ validation.correctionAnnulSeries }}
              </p>
            </section>
          </template>

          <p v-if="busy" role="status" aria-live="polite">
            {{ t('common.saving') }}
          </p>
          <footer>
            <button type="button" :disabled="busy" @click="closeDialog">
              {{ t('common.cancel') }}
            </button>
            <button
              v-if="dialog === 'remake'"
              type="button"
              :disabled="busy"
              @click="confirmRemake"
            >
              {{ t('competitive.series.match.remake.confirm') }}
            </button>
            <button
              v-else-if="dialog === 'annul'"
              type="button"
              :disabled="busy"
              @click="confirmAnnul"
            >
              {{ t('competitive.series.match.annul.confirm') }}
            </button>
            <button
              v-else
              type="button"
              :disabled="busy"
              @click="confirmCorrection"
            >
              {{ t('competitive.series.match.correction.confirm') }}
            </button>
          </footer>
        </section>
      </div>
    </Teleport>
  </section>
</template>

<style scoped>
.match-operation,
.match-operation__section,
.match-operation__section fieldset,
.operation-dialog__panel {
  display: grid;
  gap: var(--space-md);
}

.match-operation__section,
.match-operation__actions {
  padding: var(--space-md);
  border: 1px solid var(--color-hairline);
  border-radius: var(--radius-lg);
  background: var(--color-surface-1);
}

.match-operation fieldset {
  min-width: 0;
  margin: 0;
  padding: 0;
  border: 0;
}

.match-operation legend,
.match-operation h3 {
  color: var(--color-ink);
  font-weight: 700;
}

.match-operation label {
  display: grid;
  gap: var(--space-xs);
  color: var(--color-ink-muted);
}

.match-operation__side {
  display: grid;
  grid-template-columns: repeat(5, minmax(64px, 1fr));
  gap: var(--space-xs);
}

.match-operation__side h3 {
  grid-column: 1 / -1;
}

.match-operation__actions,
.operation-dialog footer {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-sm);
}

.match-operation button,
.match-operation input,
.match-operation select,
.match-operation textarea,
.operation-dialog button,
.operation-dialog input,
.operation-dialog select,
.operation-dialog textarea {
  min-height: 44px;
}

.match-operation button:focus-visible,
.match-operation input:focus-visible,
.match-operation select:focus-visible,
.match-operation textarea:focus-visible,
.operation-dialog button:focus-visible,
.operation-dialog input:focus-visible,
.operation-dialog select:focus-visible,
.operation-dialog textarea:focus-visible {
  outline: 2px solid var(--color-focus-ring);
  outline-offset: 2px;
}

[role='alert'] {
  color: var(--color-danger);
}

.operation-dialog {
  position: fixed;
  inset: 0;
  display: grid;
  place-items: center;
  padding: var(--space-md);
}

.operation-dialog__backdrop {
  position: absolute;
  inset: 0;
  background: var(--color-overlay);
}

.operation-dialog__panel {
  position: relative;
  width: min(720px, 100%);
  max-height: calc(100vh - 32px);
  padding: var(--space-lg);
  border: 1px solid var(--color-hairline-strong);
  border-radius: var(--radius-xl);
  color: var(--color-ink);
  background: var(--color-surface-1);
  box-shadow: var(--shadow-lg);
  overflow: auto;
  overscroll-behavior: contain;
}

.operation-dialog footer {
  justify-content: flex-end;
}

@media (max-width: 767px) {
  .match-operation__side {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .operation-dialog__panel {
    padding: var(--space-md);
  }
}

@media (prefers-reduced-motion: reduce) {
  .match-operation button,
  .match-operation input,
  .match-operation select,
  .match-operation textarea,
  .operation-dialog__panel {
    transition: none;
    animation: none;
  }
}
</style>
