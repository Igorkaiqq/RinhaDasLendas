<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'

import FearlessPanel from '@/components/competitive/FearlessPanel.vue'
import MatchOperationPanel from '@/components/competitive/MatchOperationPanel.vue'
import SeriesScoreboard from '@/components/competitive/SeriesScoreboard.vue'
import { useModalFocus } from '@/components/competitive/useModalFocus'
import PageFrame from '@/components/layout/PageFrame.vue'
import * as matchService from '@/services/matches'
import * as seriesService from '@/services/series'
import type {
  AnnulMatchRequest,
  ConfirmMatchResultRequest,
  CorrectMatchRequest,
  MatchDetail,
  MatchMutationResult,
  RegisterMatchPicksRequest,
  RegisterMatchRemakeRequest,
} from '@/types/match'
import type { CompetitiveMutationResult, SeriesDetail } from '@/types/series'

type Operation = 'picks' | 'result' | 'remake' | 'annul' | 'correction'
type ReconciliationRetry = 'load' | 'mutation' | 'conflict'
type OperationPayload =
  | RegisterMatchPicksRequest
  | ConfirmMatchResultRequest
  | RegisterMatchRemakeRequest
  | AnnulMatchRequest
  | CorrectMatchRequest

interface PendingOperation {
  kind: Operation
  payload: OperationPayload
}

interface Difference {
  field: string
  before: string
  after: string
}

const props = defineProps<{ matchId: string }>()
const { t } = useI18n()
const match = ref<MatchDetail | null>(null)
const series = ref<SeriesDetail | null>(null)
const etag = ref<string | null>(null)
const loading = ref(true)
const busy = ref(false)
const reconciling = ref(false)
const error = ref('')
const status = ref('')
const fieldErrors = ref<Record<string, string>>({})
const pending = ref<PendingOperation | null>(null)
const difference = ref<Difference | null>(null)
const preservedInput = ref('')
const reconciliationRetry = ref<ReconciliationRetry | null>(null)
const conflictBaseline = ref<MatchDetail | null>(null)
const operationPanel = ref<InstanceType<typeof MatchOperationPanel> | null>(
  null,
)
const conflictPanel = ref<InstanceType<typeof globalThis.HTMLElement> | null>(
  null,
)

onMounted(() => void loadInitial())

useModalFocus(
  () => pending.value !== null && difference.value !== null,
  conflictPanel,
  dismissConflict,
  () =>
    conflictPanel.value?.querySelector<
      InstanceType<typeof globalThis.HTMLElement>
    >('button') ?? null,
)

async function loadInitial() {
  loading.value = true
  error.value = ''
  try {
    const converged = await loadPair(true)
    if (!converged) setReconciliationFailure('load')
  } catch (caught) {
    if (reconciling.value) setReconciliationFailure('load')
    else error.value = readError(caught)
  } finally {
    loading.value = false
  }
}

async function loadPair(reconcile: boolean) {
  const matchObserved = await matchService.getMatch(props.matchId)
  const [seriesObserved] = await Promise.all([
    seriesService.getSeries(matchObserved.data.serieId),
    seriesService.getSeriesResult(matchObserved.data.serieId),
  ])
  if (reconcile && matchObserved.etag !== seriesObserved.etag) {
    etag.value = null
    reconciling.value = true
    const nextMatch = await matchService.getMatch(props.matchId)
    const [nextSeries] = await Promise.all([
      seriesService.getSeries(matchObserved.data.serieId),
      seriesService.getSeriesResult(matchObserved.data.serieId),
    ])
    if (nextMatch.etag !== nextSeries.etag) {
      etag.value = null
      error.value = t('competitive.series.match.reconciliationPending')
      return false
    }
    match.value = nextMatch.data
    series.value = nextSeries.data
    etag.value = nextSeries.etag
    reconciling.value = false
  } else {
    match.value = matchObserved.data
    series.value = seriesObserved.data
    etag.value = seriesObserved.etag
    reconciling.value = false
  }
  return true
}

function readError(caught: unknown) {
  if (
    caught instanceof matchService.MatchServiceError ||
    caught instanceof seriesService.SeriesServiceError
  ) {
    if (caught.status === 401)
      return t('competitive.series.errors.unauthorized')
    if (caught.status === 403) return t('competitive.series.errors.forbidden')
    if (caught.status === 404)
      return t('competitive.series.errors.matchNotFound')
  }
  return t('competitive.series.errors.loadMatch')
}

async function run(kind: Operation, payload: OperationPayload) {
  if (!etag.value) return
  busy.value = true
  error.value = ''
  fieldErrors.value = {}
  const beforeMatch = match.value
  try {
    const response = await invoke(kind, payload, etag.value)
    const hadReview = series.value?.revisaoNecessaria ?? false
    try {
      await reloadAfterMutation(response.invalidates)
    } catch {
      setReconciliationFailure('mutation', beforeMatch)
      return
    }
    reconciliationRetry.value = null
    pending.value = null
    difference.value = null
    operationPanel.value?.completeOperation()
    if (hadReview && series.value && !series.value.revisaoNecessaria)
      status.value = t('competitive.series.match.reviewResolved')
  } catch (caught) {
    if (
      caught instanceof matchService.MatchServiceError &&
      caught.status === 409 &&
      caught.messageCode === 'MV110'
    ) {
      pending.value = { kind, payload }
      conflictBaseline.value = beforeMatch
      preservedInput.value = summarizePayload(kind, payload)
      difference.value = null
      try {
        await recoverConflict(beforeMatch, kind)
      } catch {
        setReconciliationFailure('conflict', beforeMatch)
      }
    } else if (
      caught instanceof matchService.MatchServiceError &&
      caught.status === 409 &&
      caught.messageCode === 'MV126'
    ) {
      error.value = t('competitive.series.errors.idempotencyConflict')
    } else if (
      caught instanceof matchService.MatchServiceError &&
      caught.status === 403
    ) {
      removeAttemptedAction(kind)
      error.value = readError(caught)
    } else if (
      caught instanceof matchService.MatchServiceError &&
      caught.status === 400
    ) {
      fieldErrors.value = mapFieldErrors(caught, kind)
      error.value =
        caught.errors[0] ?? t('competitive.series.errors.validation')
    } else {
      error.value = readError(caught)
    }
  } finally {
    busy.value = false
  }
}

function invoke(
  kind: Operation,
  payload: OperationPayload,
  currentEtag: string,
) {
  if (kind === 'picks')
    return matchService.registerMatchPicks(
      props.matchId,
      payload as RegisterMatchPicksRequest,
      currentEtag,
    )
  if (kind === 'result')
    return matchService.confirmMatchResult(
      props.matchId,
      payload as ConfirmMatchResultRequest,
      currentEtag,
    )
  if (kind === 'remake')
    return matchService.registerMatchRemake(
      props.matchId,
      payload as RegisterMatchRemakeRequest,
      currentEtag,
    )
  if (kind === 'annul')
    return matchService.annulMatch(
      props.matchId,
      payload as AnnulMatchRequest,
      currentEtag,
    )
  return matchService.correctMatch(
    props.matchId,
    payload as CorrectMatchRequest,
    currentEtag,
  )
}

async function reloadAfterMutation(
  invalidates: CompetitiveMutationResult<MatchMutationResult>['invalidates'],
) {
  const required = [
    'match-detail',
    'series-detail',
    'series-scoreboard',
    'series-fearless',
  ] as const
  if (!required.every((resource) => invalidates.includes(resource))) {
    throw new Error('incomplete-competitive-invalidation')
  }
  if (!(await loadPair(true))) throw new Error('competitive-etag-diverged')
}

async function recoverConflict(before: MatchDetail | null, kind: Operation) {
  if (!before) return
  if (!(await loadPair(true))) throw new Error('competitive-etag-diverged')
  if (!match.value || !series.value) return
  difference.value = buildDifference(kind, before, match.value, series.value)
  reconciliationRetry.value = null
}

function removeAttemptedAction(kind: Operation) {
  if (!match.value) return
  const action =
    kind === 'picks'
      ? 'register-picks'
      : kind === 'result'
        ? 'confirm'
        : kind === 'correction'
          ? 'correct'
          : kind
  match.value = {
    ...match.value,
    acoesPermitidas: match.value.acoesPermitidas.filter(
      (candidate) => candidate !== action,
    ),
  }
}

function buildDifference(
  kind: Operation,
  before: MatchDetail,
  after: MatchDetail,
  currentSeries: SeriesDetail,
): Difference {
  if (kind === 'picks') {
    return {
      field: 'picks',
      before: picksSummary(before),
      after: picksSummary(after),
    }
  }
  if (kind === 'remake') {
    return {
      field: 'remake',
      before: t(`competitive.series.match.states.${before.estado}`),
      after: after.decisaoPicksRemake
        ? t(
            `competitive.series.match.remake.decisions.${after.decisaoPicksRemake}`,
          )
        : t(`competitive.series.match.states.${after.estado}`),
    }
  }
  if (kind === 'annul') {
    return {
      field: 'annul',
      before: t(`competitive.series.match.states.${before.estado}`),
      after: t(`competitive.series.match.states.${after.estado}`),
    }
  }
  return {
    field: kind,
    before: resultSummary(before, currentSeries),
    after: resultSummary(after, currentSeries),
  }
}

function picksSummary(value: MatchDetail) {
  if (value.picks.length === 0)
    return t('competitive.series.match.conflict.noPicks')
  const firstSide = series.value?.lados[0]?.id
  return value.picks
    .filter((pick) => pick.ladoSerieId === firstSide)
    .sort((a, b) => a.ordem - b.ordem)
    .map((pick) => pick.championId)
    .join(', ')
}

function resultSummary(value: MatchDetail, currentSeries: SeriesDetail) {
  const side =
    currentSeries.lados.find((item) => item.id === value.ladoVencedorId)
      ?.nome ?? t('common.notInformed')
  const reason = value.motivoTermino
    ? t(`competitive.series.match.result.values.${value.motivoTermino}`)
    : t('common.notInformed')
  return `${side} · ${reason}`
}

function summarizePayload(kind: Operation, payload: OperationPayload) {
  if (kind === 'picks')
    return (payload as RegisterMatchPicksRequest).lados
      .flatMap((side) => side.championIds)
      .join(', ')
  if (kind === 'result')
    return t(
      `competitive.series.match.result.values.${(payload as ConfirmMatchResultRequest).motivoTermino}`,
    )
  return (
    payload as
      | RegisterMatchRemakeRequest
      | AnnulMatchRequest
      | CorrectMatchRequest
  ).justificativa
}

function mapFieldErrors(
  caught: matchService.MatchServiceError,
  kind: Operation,
) {
  const mapped: Record<string, string> = {}
  const reasonField =
    kind === 'remake'
      ? 'remakeReason'
      : kind === 'annul'
        ? 'annulReason'
        : 'correctionReason'
  for (const item of caught.fieldErrors) {
    if (item.field.toLocaleLowerCase('en-US').includes('justificativa'))
      mapped[reasonField] = item.message
  }
  return mapped
}

function setReconciliationFailure(
  retry: ReconciliationRetry,
  baseline: MatchDetail | null = null,
) {
  reconciling.value = false
  etag.value = null
  difference.value = null
  reconciliationRetry.value = retry
  if (baseline) conflictBaseline.value = baseline
  error.value = t('competitive.series.match.reconciliationFailed')
}

async function retryReconciliation() {
  if (!reconciliationRetry.value) return
  busy.value = true
  reconciling.value = true
  error.value = ''
  difference.value = null
  const retry = reconciliationRetry.value
  try {
    if (!(await loadPair(true))) throw new Error('competitive-etag-diverged')
    if (retry === 'conflict' && pending.value && conflictBaseline.value) {
      difference.value = buildDifference(
        pending.value.kind,
        conflictBaseline.value,
        match.value!,
        series.value!,
      )
    } else if (retry === 'mutation') {
      operationPanel.value?.completeOperation()
      pending.value = null
    }
    reconciliationRetry.value = null
  } catch {
    setReconciliationFailure(retry, conflictBaseline.value)
  } finally {
    busy.value = false
  }
}

function reconfirm() {
  const operation = pending.value
  if (operation) void run(operation.kind, operation.payload)
}

function dismissConflict() {
  pending.value = null
  difference.value = null
}
</script>

<template>
  <PageFrame>
    <main class="match-detail" :aria-busy="loading || busy || reconciling">
      <p v-if="reconciling" role="status" data-etag-reconciliation>
        {{ t('competitive.series.match.reconciling') }}
      </p>
      <section
        v-if="reconciliationRetry"
        role="alert"
        data-reconciliation-error
      >
        <p>{{ error }}</p>
        <button type="button" :disabled="busy" @click="retryReconciliation">
          {{ t('competitive.series.match.retryReconciliation') }}
        </button>
      </section>
      <div v-if="loading" class="match-detail__skeleton" aria-hidden="true" />
      <section v-else-if="error && !match && !reconciliationRetry" role="alert">
        <p>{{ error }}</p>
        <button type="button" @click="loadInitial">
          {{ t('competitive.series.retry') }}
        </button>
      </section>
      <template v-else-if="match && series">
        <header>
          <div>
            <p>
              {{
                t('competitive.series.match.eyebrow', { order: match.ordem })
              }}
            </p>
            <h1>{{ series.lados[0]?.nome }} × {{ series.lados[1]?.nome }}</h1>
          </div>
          <span>{{
            t(`competitive.series.match.states.${match.estado}`)
          }}</span>
        </header>

        <p v-if="error && !reconciliationRetry" role="alert">{{ error }}</p>
        <p v-if="status" role="status" aria-live="polite">{{ status }}</p>
        <SeriesScoreboard :series="series" />
        <FearlessPanel
          v-if="series.modoDraft === 'Fearless'"
          :series="series"
        />
        <MatchOperationPanel
          ref="operationPanel"
          :match="match"
          :series="series"
          :busy="busy"
          :disabled="reconciling"
          :field-errors="fieldErrors"
          @savePicks="run('picks', $event)"
          @confirmResult="run('result', $event)"
          @remake="run('remake', $event)"
          @annul="run('annul', $event)"
          @correct="run('correction', $event)"
        />
      </template>

      <Teleport to="body">
        <div v-if="pending && difference" class="version-conflict">
          <section
            ref="conflictPanel"
            role="alertdialog"
            data-version-conflict
            aria-modal="true"
            aria-labelledby="version-conflict-title"
            aria-describedby="version-conflict-description"
          >
            <h2 id="version-conflict-title">
              {{ t('competitive.series.match.conflict.title') }}
            </h2>
            <p id="version-conflict-description">
              {{ t('competitive.series.match.conflict.description') }}
            </p>
            <dl :data-conflict-field="difference.field">
              <div data-before>
                <dt>{{ t('competitive.series.match.conflict.before') }}</dt>
                <dd>{{ difference.before }}</dd>
              </div>
              <div data-after>
                <dt>{{ t('competitive.series.match.conflict.after') }}</dt>
                <dd>{{ difference.after }}</dd>
              </div>
            </dl>
            <p data-preserved-input>
              {{
                t('competitive.series.match.conflict.preserved', {
                  value: preservedInput,
                })
              }}
            </p>
            <footer>
              <button type="button" :disabled="busy" @click="dismissConflict">
                {{ t('common.cancel') }}
              </button>
              <button
                type="button"
                :disabled="busy || reconciling || !etag"
                @click="reconfirm"
              >
                {{ t('competitive.series.match.conflict.reconfirm') }}
              </button>
            </footer>
          </section>
        </div>
      </Teleport>
    </main>
  </PageFrame>
</template>

<style scoped>
.match-detail {
  display: grid;
  gap: var(--space-xl);
}

.match-detail > header,
.version-conflict footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-md);
}

.match-detail > header {
  padding: var(--space-lg);
  border: 1px solid var(--color-hairline);
  border-radius: var(--radius-xl);
  background: var(--color-canvas-raised);
}

.match-detail > header p,
.match-detail > header span {
  color: var(--color-primary-hover);
  font-family: var(--font-data);
  font-size: 12px;
  text-transform: uppercase;
}

.match-detail__skeleton {
  min-height: 480px;
  border-radius: var(--radius-xl);
  background: var(--color-surface-2);
}

.version-conflict {
  position: fixed;
  inset: 0;
  display: grid;
  place-items: center;
  padding: var(--space-md);
  background: var(--color-overlay);
}

.version-conflict section {
  display: grid;
  gap: var(--space-md);
  width: min(600px, 100%);
  padding: var(--space-lg);
  border: 1px solid var(--color-warning);
  border-radius: var(--radius-xl);
  background: var(--color-surface-1);
  box-shadow: var(--shadow-lg);
  max-height: calc(100vh - 32px);
  overflow: auto;
  overscroll-behavior: contain;
}

.version-conflict dl {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: var(--space-sm);
  margin: 0;
}

.version-conflict dl div {
  padding: var(--space-md);
  border: 1px solid var(--color-hairline);
  border-radius: var(--radius-md);
  background: var(--color-canvas-raised);
}

.version-conflict dt {
  color: var(--color-ink-subtle);
  font-family: var(--font-data);
  font-size: 12px;
}

.version-conflict dd {
  margin: var(--space-xs) 0 0;
}

.version-conflict footer {
  justify-content: flex-end;
}

@media (max-width: 479px) {
  .version-conflict dl {
    grid-template-columns: 1fr;
  }
}

@media (prefers-reduced-motion: reduce) {
  .match-detail button,
  .version-conflict section {
    transition: none;
    animation: none;
  }
}
</style>
