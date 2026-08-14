<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'

import PageFrame from '@/components/layout/PageFrame.vue'
import PageHeader from '@/components/layout/PageHeader.vue'
import SeasonScopeSelector from '@/components/competitive/SeasonScopeSelector.vue'
import * as seasonService from '@/services/seasons'
import * as seriesService from '@/services/series'
import type { SeasonScope, SeasonSummary } from '@/types/season'
import type {
  SeriesDetail,
  SeriesPage,
  SeriesState,
  SeriesType,
} from '@/types/series'

const { t, locale } = useI18n()
const page = ref<SeriesPage | null>(null)
const loading = ref(true)
const failed = ref(false)
const mobile = ref(false)
const typeFilter = ref<SeriesType | ''>('')
const stateFilter = ref<SeriesState | ''>('')
const seasons = ref<SeasonSummary[]>([])
const scope = ref<SeasonScope>(scopeFromUrl())
const seriesTypes: SeriesType[] = [
  'DiariaTemporaria',
  'ConfrontoOficial',
  'Amistoso',
]
const seriesStates: SeriesState[] = [
  'Agendada',
  'EmAndamento',
  'Concluida',
  'Cancelada',
  'Anulada',
]
let media: InstanceType<typeof globalThis.MediaQueryList> | null = null

onMounted(() => {
  media = globalThis.matchMedia?.('(max-width: 767px)') ?? null
  mobile.value = media?.matches ?? false
  media?.addEventListener('change', onMediaChange)
  globalThis.addEventListener('resize', syncViewport)
  void loadPage()
})

onBeforeUnmount(() => {
  media?.removeEventListener('change', onMediaChange)
  globalThis.removeEventListener('resize', syncViewport)
})

function onMediaChange(
  event: InstanceType<typeof globalThis.MediaQueryListEvent>,
) {
  mobile.value = event.matches
}

function syncViewport() {
  if (media) mobile.value = media.matches
}

async function load() {
  loading.value = true
  failed.value = false
  try {
    page.value = await seriesService.listSeries({
      ...(typeFilter.value ? { tipo: typeFilter.value } : {}),
      ...(stateFilter.value ? { estado: stateFilter.value } : {}),
      scope: scope.value,
    })
  } catch {
    failed.value = true
  } finally {
    loading.value = false
  }
}

async function loadPage() {
  loading.value = true
  try {
    const [catalog] = await Promise.all([
      seasonService.listSeasons({ pageSize: 100 }).catch(() => null),
      load(),
    ])
    seasons.value = catalog?.data.items ?? page.value?.seasonsIncluidas ?? []
  } catch {
    failed.value = true
    loading.value = false
  }
}

function scopeFromUrl(): SeasonScope {
  const params = new globalThis.URL(globalThis.location.href).searchParams
  if (params.get('todas') === 'true') return { mode: 'all' }
  const seasonIds = params.getAll('temporadaIds')
  if (seasonIds.length > 0)
    return { mode: 'selected', seasonIds: seasonIds as [string, ...string[]] }
  return { mode: 'current' }
}

function changeScope(value: SeasonScope) {
  scope.value = value
  const url = new globalThis.URL(globalThis.location.href)
  url.searchParams.delete('todas')
  url.searchParams.delete('temporadaIds')
  if (value.mode === 'all') url.searchParams.set('todas', 'true')
  if (value.mode === 'selected')
    value.seasonIds.forEach((id) => url.searchParams.append('temporadaIds', id))
  globalThis.history.replaceState({}, '', `${url.pathname}${url.search}`)
  void load()
}

function typeLabel(value: string) {
  return t(`competitive.series.types.${value}`)
}

function modeLabel(value: string) {
  return t(`competitive.series.modes.${value}`)
}

function formatLabel(value: SeriesDetail['formato']) {
  return t(`competitive.series.formats.${value}`)
}

function stateLabel(value: string) {
  return t(`competitive.series.states.${value}`)
}

function score(series: SeriesDetail) {
  return `${series.placar[0]} — ${series.placar[1]}`
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat(
    locale.value.startsWith('en') ? 'en-US' : 'pt-BR',
    {
      dateStyle: 'medium',
      timeStyle: 'short',
      timeZone: 'America/Sao_Paulo',
    },
  ).format(new Date(value))
}
</script>

<template>
  <PageFrame>
    <main class="series-list" :aria-busy="loading">
      <PageHeader
        :eyebrow="t('competitive.series.eyebrow')"
        :title="t('competitive.series.title')"
        :description="t('competitive.series.description')"
      />

      <SeasonScopeSelector
        :seasons="seasons"
        :model-value="scope"
        :calendar-configured="page?.calendarioConfigurado ?? true"
        :loading="loading"
        @update:model-value="changeScope"
      />

      <form
        class="series-list__filters"
        :aria-label="t('competitive.series.filters.label')"
        @submit.prevent="load"
      >
        <label for="series-type-filter">{{
          t('competitive.series.filters.type')
        }}</label>
        <select
          id="series-type-filter"
          v-model="typeFilter"
          name="seriesType"
          autocomplete="off"
        >
          <option value="">{{ t('common.all') }}</option>
          <option v-for="type in seriesTypes" :key="type" :value="type">
            {{ typeLabel(type) }}
          </option>
        </select>
        <label for="series-state-filter">{{ t('common.status') }}</label>
        <select
          id="series-state-filter"
          v-model="stateFilter"
          name="seriesState"
          autocomplete="off"
        >
          <option value="">{{ t('common.all') }}</option>
          <option v-for="state in seriesStates" :key="state" :value="state">
            {{ stateLabel(state) }}
          </option>
        </select>
        <button type="submit">
          {{ t('competitive.series.filters.apply') }}
        </button>
      </form>

      <div
        v-if="loading"
        class="series-list__skeleton"
        data-series-skeleton
        aria-hidden="true"
      >
        <span v-for="item in 3" :key="item" />
      </div>
      <section v-else-if="failed" class="series-list__state" role="alert">
        <h2>{{ t('competitive.series.errors.load') }}</h2>
        <button type="button" @click="load">
          {{ t('competitive.series.retry') }}
        </button>
      </section>
      <section
        v-else-if="!page?.calendarioConfigurado"
        class="series-list__state"
        role="status"
      >
        {{ t('competitive.series.calendarEmpty') }}
      </section>
      <section
        v-else-if="page.items.length === 0"
        class="series-list__state"
        role="status"
      >
        {{ t('competitive.series.empty') }}
      </section>

      <table
        v-else-if="!mobile && page"
        :aria-label="t('competitive.series.accessibility.table')"
      >
        <thead>
          <tr>
            <th>{{ t('competitive.series.columns.series') }}</th>
            <th>{{ t('competitive.series.columns.sides') }}</th>
            <th>{{ t('competitive.series.columns.score') }}</th>
            <th>{{ t('competitive.series.columns.schedule') }}</th>
            <th>{{ t('common.status') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="series in page.items" :key="series.id">
            <td>
              <RouterLink
                :to="`/series/${series.id}`"
                :data-series-id="series.id"
              >
                {{ typeLabel(series.tipo) }}
              </RouterLink>
              <small
                >{{ formatLabel(series.formato) }} ·
                {{ modeLabel(series.modoDraft) }}</small
              >
              <small class="series-list__context">
                {{ t('competitive.series.context.season') }}:
                {{ series.seasonId }} ·
                <template v-if="series.competicaoId"
                  >{{ t('competitive.series.context.competition') }}:
                  {{ series.competicaoId }} ·
                </template>
                <template v-if="series.rodadaId"
                  >{{ t('competitive.series.context.round') }}:
                  {{ series.rodadaId }} ·
                </template>
                {{ t('competitive.series.context.rules') }}:
                {{ series.versaoRegrasId }}
              </small>
            </td>
            <td>{{ series.lados[0]?.nome }} × {{ series.lados[1]?.nome }}</td>
            <td class="series-list__score">{{ score(series) }}</td>
            <td>{{ formatDate(series.agendadaPara) }}</td>
            <td>{{ stateLabel(series.estado) }}</td>
          </tr>
        </tbody>
      </table>

      <ul
        v-else-if="page"
        class="series-list__cards"
        :aria-label="t('competitive.series.accessibility.cards')"
      >
        <li v-for="series in page.items" :key="series.id">
          <RouterLink :to="`/series/${series.id}`" :data-series-id="series.id">
            <strong>{{ typeLabel(series.tipo) }}</strong>
            <span
              >{{ formatLabel(series.formato) }} ·
              {{ modeLabel(series.modoDraft) }}</span
            >
            <small class="series-list__context">
              {{ t('competitive.series.context.season') }}: {{ series.seasonId
              }}<br />
              <template v-if="series.competicaoId"
                >{{ t('competitive.series.context.competition') }}:
                {{ series.competicaoId }}<br
              /></template>
              <template v-if="series.rodadaId"
                >{{ t('competitive.series.context.round') }}:
                {{ series.rodadaId }}<br
              /></template>
              {{ t('competitive.series.context.rules') }}:
              {{ series.versaoRegrasId }}
            </small>
            <span
              >{{ series.lados[0]?.nome }} × {{ series.lados[1]?.nome }}</span
            >
            <b>{{ score(series) }}</b>
            <time :datetime="series.agendadaPara">{{
              formatDate(series.agendadaPara)
            }}</time>
            <span>{{ stateLabel(series.estado) }}</span>
          </RouterLink>
        </li>
      </ul>
    </main>
  </PageFrame>
</template>

<style scoped>
.series-list {
  display: grid;
  gap: var(--space-xl);
  min-width: 0;
}

.series-list__filters {
  display: grid;
  grid-template-columns: auto minmax(160px, 1fr) auto minmax(160px, 1fr) auto;
  align-items: center;
  gap: var(--space-sm);
  padding: var(--space-md);
  border: 1px solid var(--color-hairline);
  border-radius: var(--radius-xl);
  background: var(--color-surface-1);
}

.series-list__filters select,
.series-list__filters button {
  min-height: 44px;
}

.series-list__skeleton {
  display: grid;
  gap: var(--space-sm);
}

.series-list__skeleton span {
  min-height: 72px;
  border-radius: var(--radius-lg);
  background: var(--color-surface-2);
}

.series-list__state {
  padding: var(--space-xl);
  border: 1px solid var(--color-hairline);
  border-radius: var(--radius-xl);
  background: var(--color-surface-1);
  text-align: center;
}

.series-list table {
  width: 100%;
  border-collapse: collapse;
  border: 1px solid var(--color-hairline);
  border-radius: var(--radius-xl);
  background: var(--color-surface-1);
  overflow: hidden;
}

.series-list th,
.series-list td {
  padding: var(--space-md);
  border-bottom: 1px solid var(--color-hairline);
  text-align: left;
}

.series-list th,
.series-list small {
  color: var(--color-ink-subtle);
  font-family: var(--font-data);
  font-size: 12px;
}

.series-list td:first-child {
  display: grid;
  gap: var(--space-xxs);
}

.series-list__context {
  overflow-wrap: anywhere;
}

.series-list a {
  min-height: 44px;
  color: var(--color-ink);
}

.series-list__score,
.series-list__cards b {
  font-family: var(--font-data);
}

.series-list__cards {
  display: grid;
  gap: var(--space-md);
  margin: 0;
  padding: 0;
  list-style: none;
}

.series-list__cards a {
  display: grid;
  gap: var(--space-xs);
  padding: var(--space-md);
  border: 1px solid var(--color-hairline);
  border-radius: var(--radius-xl);
  background: var(--color-surface-1);
  text-decoration: none;
}

@media (max-width: 767px) {
  .series-list__filters {
    grid-template-columns: 1fr;
  }
}

@media (prefers-reduced-motion: reduce) {
  .series-list button,
  .series-list a,
  .series-list select {
    transition: none;
  }
}
</style>
