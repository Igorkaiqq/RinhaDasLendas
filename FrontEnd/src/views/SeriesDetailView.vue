<script setup lang="ts">
import { computed, nextTick, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'

import FearlessPanel from '@/components/competitive/FearlessPanel.vue'
import SeriesScoreboard from '@/components/competitive/SeriesScoreboard.vue'
import PageFrame from '@/components/layout/PageFrame.vue'
import { useModalFocus } from '@/components/competitive/useModalFocus'
import * as seriesService from '@/services/series'
import type {
  CompetitiveMutationInvalidations,
  SeriesDetail,
  SeriesSideSnapshot,
} from '@/types/series'

type SeriesAction = 'start' | 'cancel' | 'annul' | 'create-match'

interface PendingSeriesAction {
  action: SeriesAction
  reason: string
}

const props = defineProps<{ seriesId: string }>()
const { t, locale } = useI18n()
const series = ref<SeriesDetail | null>(null)
const etag = ref<string | null>(null)
const loading = ref(true)
const busy = ref(false)
const error = ref('')
const dialog = ref<'start' | 'cancel' | 'annul' | null>(null)
const reason = ref('')
const dialogPanel = ref<InstanceType<typeof globalThis.HTMLElement> | null>(
  null,
)
const conflictPanel = ref<InstanceType<typeof globalThis.HTMLElement> | null>(
  null,
)
const pending = ref<PendingSeriesAction | null>(null)
const difference = ref<{ before: string; after: string } | null>(null)

const titleId = computed(() => `series-${props.seriesId}-title`)

onMounted(load)

useModalFocus(
  () => dialog.value !== null,
  dialogPanel,
  closeDialog,
  () =>
    dialogPanel.value?.querySelector<
      InstanceType<typeof globalThis.HTMLElement>
    >('textarea, button') ?? null,
)
useModalFocus(
  () => pending.value !== null && difference.value !== null,
  conflictPanel,
  dismissConflict,
  () =>
    conflictPanel.value?.querySelector<
      InstanceType<typeof globalThis.HTMLElement>
    >('button') ?? null,
)

async function load() {
  loading.value = true
  error.value = ''
  try {
    const observed = await loadSnapshot()
    series.value = observed.data
    etag.value = observed.etag
  } catch (caught) {
    error.value = readError(caught)
  } finally {
    loading.value = false
  }
}

async function loadSnapshot() {
  const [observed] = await Promise.all([
    seriesService.getSeries(props.seriesId),
    seriesService.getSeriesResult(props.seriesId),
  ])
  return observed
}

function readError(caught: unknown) {
  if (caught instanceof seriesService.SeriesServiceError) {
    if (caught.status === 401)
      return t('competitive.series.errors.unauthorized')
    if (caught.status === 403) return t('competitive.series.errors.forbidden')
    if (caught.status === 404) return t('competitive.series.errors.notFound')
  }
  return t('competitive.series.errors.loadDetail')
}

function hasAction(action: string) {
  return series.value?.acoesPermitidas.includes(action) ?? false
}

async function openDialog(action: 'start' | 'cancel' | 'annul') {
  reason.value = ''
  dialog.value = action
  await nextTick()
  dialogPanel.value
    ?.querySelector<
      InstanceType<typeof globalThis.HTMLElement>
    >('textarea, button')
    ?.focus()
}

function closeDialog() {
  if (!busy.value) {
    dialog.value = null
    reason.value = ''
  }
}

async function confirmAction() {
  if (!series.value || !etag.value || !dialog.value) return
  if (dialog.value !== 'start' && !reason.value.trim()) return
  const action = dialog.value
  const submittedReason = reason.value.trim()
  closeDialog()
  await executeAction({ action, reason: submittedReason })
}

async function createMatch() {
  await executeAction({ action: 'create-match', reason: '' })
}

async function executeAction(operation: PendingSeriesAction) {
  if (!series.value || !etag.value) return
  busy.value = true
  error.value = ''
  const before = series.value
  try {
    const response =
      operation.action === 'start'
        ? await seriesService.startSeries(series.value.id, etag.value)
        : operation.action === 'cancel'
          ? await seriesService.cancelSeries(
              series.value.id,
              { justificativa: operation.reason },
              etag.value,
            )
          : operation.action === 'annul'
            ? await seriesService.annulSeries(
                series.value.id,
                { justificativa: operation.reason },
                etag.value,
              )
            : await seriesService.createMatch(series.value.id, etag.value)
    await reloadInvalidated(response.invalidates)
    pending.value = null
    difference.value = null
  } catch (caught) {
    if (
      caught instanceof seriesService.SeriesServiceError &&
      caught.status === 409
    ) {
      await recoverConflict(before, operation)
    } else if (
      caught instanceof seriesService.SeriesServiceError &&
      caught.status === 403
    ) {
      removeAttemptedAction(operation.action)
      error.value = readError(caught)
    } else {
      error.value = readError(caught)
    }
  } finally {
    busy.value = false
  }
}

async function reloadInvalidated(
  invalidates: CompetitiveMutationInvalidations,
) {
  if (!invalidates.includes('series-detail')) return
  const observed = await loadSnapshot()
  series.value = observed.data
  etag.value = observed.etag
}

async function recoverConflict(
  before: SeriesDetail,
  operation: PendingSeriesAction,
) {
  const observed = await loadSnapshot()
  series.value = observed.data
  etag.value = observed.etag
  pending.value = operation
  difference.value = {
    before: conflictSummary(before, operation.action),
    after: conflictSummary(observed.data, operation.action),
  }
}

function conflictSummary(value: SeriesDetail, action: SeriesAction) {
  if (action === 'create-match') return String(value.partidas.length)
  return `${stateLabel(value.estado)} · ${value.placar.join(' — ')}`
}

function removeAttemptedAction(action: SeriesAction) {
  if (!series.value) return
  series.value = {
    ...series.value,
    acoesPermitidas: series.value.acoesPermitidas.filter(
      (candidate) => candidate !== action,
    ),
  }
}

function reconfirm() {
  const operation = pending.value
  if (operation) void executeAction(operation)
}

function dismissConflict() {
  pending.value = null
  difference.value = null
}

function stateLabel(value: string) {
  return t(`competitive.series.states.${value}`)
}

function captainIdentity(side: SeriesSideSnapshot) {
  if (!side.capitaoJogadorId) return ''
  const participant = side.participantes?.find(
    (item) => item.jogadorId === side.capitaoJogadorId,
  )
  const name = participant?.nomeExibicao ?? side.capitaoNomeSnapshot?.trim()
  if (!name) return t('competitive.series.captainUnknown')
  const tag = participant?.tag?.trim() || side.capitaoTagSnapshot?.trim()
  return tag ? `${name} · ${tag}` : name
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat(
    locale.value.startsWith('en') ? 'en-US' : 'pt-BR',
    {
      dateStyle: 'long',
      timeStyle: 'short',
      timeZone: 'America/Sao_Paulo',
    },
  ).format(new Date(value))
}
</script>

<template>
  <PageFrame>
    <main
      class="series-detail"
      :aria-labelledby="titleId"
      :aria-busy="loading || busy"
    >
      <div v-if="loading" class="series-detail__loading" aria-hidden="true" />
      <section v-else-if="error && !series" role="alert">
        <p>{{ error }}</p>
        <button type="button" @click="load">
          {{ t('competitive.series.retry') }}
        </button>
      </section>
      <template v-else-if="series">
        <header>
          <div>
            <p>
              {{ t(`competitive.series.types.${series.tipo}`) }} ·
              {{ formatDate(series.agendadaPara) }}
            </p>
            <h1 :id="titleId">
              {{ series.lados[0]?.nome }} × {{ series.lados[1]?.nome }}
            </h1>
            <p>
              {{ t(`competitive.series.formats.${series.formato}`) }} ·
              {{ t(`competitive.series.modes.${series.modoDraft}`) }} ·
              <span data-series-state>{{ stateLabel(series.estado) }}</span>
            </p>
          </div>
          <div class="series-detail__actions">
            <button
              v-if="hasAction('start')"
              type="button"
              :disabled="busy"
              @click="openDialog('start')"
            >
              {{ t('competitive.series.actions.start') }}
            </button>
            <button
              v-if="hasAction('cancel')"
              type="button"
              :disabled="busy"
              @click="openDialog('cancel')"
            >
              {{ t('competitive.series.actions.cancel') }}
            </button>
            <button
              v-if="hasAction('annul')"
              type="button"
              :disabled="busy"
              @click="openDialog('annul')"
            >
              {{ t('competitive.series.actions.annul') }}
            </button>
            <button
              v-if="hasAction('create-match')"
              type="button"
              :disabled="busy || series.revisaoNecessaria"
              @click="createMatch"
            >
              {{ t('competitive.series.actions.createMatch') }}
            </button>
          </div>
        </header>

        <p v-if="error" role="alert">{{ error }}</p>
        <p v-if="series.revisaoNecessaria" role="alert" data-review-required>
          {{ t('competitive.series.fearless.reviewRequired') }}
        </p>
        <dl class="series-detail__context">
          <div>
            <dt>{{ t('competitive.series.context.season') }}</dt>
            <dd>{{ series.seasonId }}</dd>
          </div>
          <div v-if="series.competicaoId">
            <dt>{{ t('competitive.series.context.competition') }}</dt>
            <dd>{{ series.competicaoId }}</dd>
          </div>
          <div v-if="series.rodadaId">
            <dt>{{ t('competitive.series.context.round') }}</dt>
            <dd>{{ series.rodadaId }}</dd>
          </div>
          <div>
            <dt>{{ t('competitive.series.context.rules') }}</dt>
            <dd>{{ series.versaoRegrasId }}</dd>
          </div>
        </dl>
        <SeriesScoreboard :series="series" />

        <section
          class="series-detail__sides"
          :aria-label="t('competitive.series.sides')"
        >
          <article v-for="side in series.lados" :key="side.id">
            <p>{{ side.tag || t('competitive.series.side') }}</p>
            <h2>{{ side.nome }}</h2>
            <small v-if="side.capitaoJogadorId">
              {{
                t('competitive.series.captainIdentity', {
                  identity: captainIdentity(side),
                })
              }}
            </small>
          </article>
        </section>

        <FearlessPanel
          v-if="series.modoDraft === 'Fearless'"
          :series="series"
        />

        <section
          class="series-detail__matches"
          aria-labelledby="series-matches-title"
        >
          <h2 id="series-matches-title">
            {{ t('competitive.series.matches.title') }}
          </h2>
          <ol :aria-label="t('competitive.series.matches.label')">
            <li v-for="match in series.partidas" :key="match.id">
              <RouterLink :to="`/partidas/${match.id}`">{{
                t('competitive.series.matches.item', { order: match.ordem })
              }}</RouterLink>
              <span>{{
                t(`competitive.series.match.states.${match.estado}`)
              }}</span>
            </li>
          </ol>
        </section>
      </template>

      <Teleport to="body">
        <div v-if="dialog" class="series-action-dialog">
          <section
            ref="dialogPanel"
            role="alertdialog"
            aria-modal="true"
            aria-labelledby="series-action-title"
            aria-describedby="series-action-description"
            :aria-busy="busy"
            tabindex="-1"
          >
            <h2 id="series-action-title">
              {{ t(`competitive.series.dialogs.${dialog}.title`) }}
            </h2>
            <p id="series-action-description">
              {{ t(`competitive.series.dialogs.${dialog}.description`) }}
            </p>
            <label v-if="dialog !== 'start'" for="series-action-reason">
              <span>{{ t('competitive.series.match.reason') }}</span>
              <textarea
                id="series-action-reason"
                v-model="reason"
                :name="
                  dialog === 'cancel'
                    ? 'seriesCancelReason'
                    : 'seriesAnnulReason'
                "
                autocomplete="off"
                :disabled="busy"
              />
            </label>
            <footer>
              <button type="button" :disabled="busy" @click="closeDialog">
                {{ t('common.cancel') }}
              </button>
              <button
                type="button"
                :disabled="busy || (dialog !== 'start' && !reason.trim())"
                @click="confirmAction"
              >
                {{ t(`competitive.series.dialogs.${dialog}.confirm`) }}
              </button>
            </footer>
          </section>
        </div>
        <div v-if="pending && difference" class="series-action-dialog">
          <section
            ref="conflictPanel"
            role="alertdialog"
            aria-modal="true"
            aria-labelledby="series-conflict-title"
            aria-describedby="series-conflict-description"
            data-series-version-conflict
            tabindex="-1"
          >
            <h2 id="series-conflict-title">
              {{ t('competitive.series.match.conflict.title') }}
            </h2>
            <p id="series-conflict-description">
              {{ t('competitive.series.match.conflict.description') }}
            </p>
            <dl>
              <div>
                <dt>{{ t('competitive.series.match.conflict.before') }}</dt>
                <dd>{{ difference.before }}</dd>
              </div>
              <div>
                <dt>{{ t('competitive.series.match.conflict.after') }}</dt>
                <dd>{{ difference.after }}</dd>
              </div>
            </dl>
            <footer>
              <button type="button" :disabled="busy" @click="dismissConflict">
                {{ t('common.cancel') }}
              </button>
              <button type="button" :disabled="busy" @click="reconfirm">
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
.series-detail {
  display: grid;
  gap: var(--space-xl);
}

.series-detail > header,
.series-detail__actions,
.series-action-dialog footer {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-sm);
}

.series-detail > header {
  padding: var(--space-lg);
  border: 1px solid var(--color-hairline);
  border-radius: var(--radius-xl);
  background: var(--color-canvas-raised);
}

.series-detail > header p,
.series-detail__matches span {
  color: var(--color-ink-subtle);
  font-family: var(--font-data);
  font-size: 12px;
}

.series-detail__sides {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: var(--space-md);
}

.series-detail__sides article,
.series-detail__matches,
.series-detail__context {
  padding: var(--space-lg);
  border: 1px solid var(--color-hairline);
  border-radius: var(--radius-xl);
  background: var(--color-surface-1);
}

.series-detail__context {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: var(--space-sm);
  margin: 0;
}

.series-detail__context dt {
  color: var(--color-ink-subtle);
  font-size: 12px;
}

.series-detail__context dd {
  margin: var(--space-xxs) 0 0;
  overflow-wrap: anywhere;
}

.series-detail__matches ol {
  display: grid;
  gap: var(--space-xs);
  padding-left: var(--space-lg);
}

.series-detail__matches li {
  padding: var(--space-sm);
}

.series-detail__matches a {
  display: inline-flex;
  align-items: center;
  min-height: 44px;
  color: var(--color-ink);
}

.series-detail__loading {
  min-height: 420px;
  border-radius: var(--radius-xl);
  background: var(--color-surface-2);
}

.series-action-dialog {
  position: fixed;
  inset: 0;
  display: grid;
  place-items: center;
  padding: var(--space-md);
  background: var(--color-overlay);
}

.series-action-dialog section {
  display: grid;
  gap: var(--space-md);
  width: min(520px, 100%);
  max-height: calc(100vh - 32px);
  padding: var(--space-lg);
  border: 1px solid var(--color-hairline-strong);
  border-radius: var(--radius-xl);
  background: var(--color-surface-1);
  overflow: auto;
  overscroll-behavior: contain;
}

.series-action-dialog label {
  display: grid;
  gap: var(--space-xs);
}

.series-action-dialog footer {
  justify-content: flex-end;
}

@media (max-width: 767px) {
  .series-detail > header,
  .series-detail__sides {
    grid-template-columns: 1fr;
  }

  .series-detail > header {
    align-items: stretch;
    flex-direction: column;
  }
}

@media (prefers-reduced-motion: reduce) {
  .series-detail button,
  .series-detail a,
  .series-action-dialog section {
    transition: none;
    animation: none;
  }
}
</style>
