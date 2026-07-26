<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'

import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { DRAFT_MONTAGEM_STATUS_OPTIONS } from '@/constants/draftMontagemStatus'
import type { DraftMontagemResumo, DraftMontagemStatus } from '@/types/draftMontagem'

export type DraftNavigatorItem = Omit<DraftMontagemResumo, 'status'> & { status: string }

defineProps<{
  drafts: readonly DraftNavigatorItem[]
  selectedDraftId: string | null
  searchTerm: string
  selectedStatus: DraftMontagemStatus | ''
  statusOptions: readonly DraftMontagemStatus[]
  loading: boolean
  loadFailed: boolean
  hasKnownDrafts: boolean
  canCreate: boolean
}>()

const emit = defineEmits<{
  'update:searchTerm': [value: string]
  'update:selectedStatus': [value: DraftMontagemStatus | '']
  select: [draftId: string]
  reset: []
  retry: []
  create: []
}>()

const { locale, t } = useI18n()
const compactExpanded = ref(false)
const knownStatuses = new Set<string>(DRAFT_MONTAGEM_STATUS_OPTIONS)
const compactToggleLabel = computed(() => t(compactExpanded.value ? 'drafts.navigator.collapse' : 'drafts.navigator.expand'))
type DraftStatusVariant = 'neutral' | 'info' | 'warning' | 'success' | 'danger'
const statusVariants: Record<DraftMontagemStatus, DraftStatusVariant> = {
  PresencaAberta: 'info',
  PresencaEncerrada: 'warning',
  CapitaesDefinidos: 'warning',
  OrdemDefinida: 'info',
  Aberta: 'info',
  Finalizada: 'success',
  Cancelada: 'danger',
}

type ControlEvent = InstanceType<typeof globalThis.Event>

function updateSearch(event: ControlEvent) {
  emit('update:searchTerm', (event.target as unknown as { value: string }).value)
}

function updateStatus(event: ControlEvent) {
  emit('update:selectedStatus', (event.target as unknown as { value: DraftMontagemStatus | '' }).value)
}

function statusLabel(status: string) {
  return t(knownStatuses.has(status) ? `drafts.status.${status}` : 'drafts.status.unknown')
}

function statusVariant(status: string): DraftStatusVariant {
  return knownStatuses.has(status) ? statusVariants[status as DraftMontagemStatus] : 'neutral'
}

function formatDate(draft: DraftNavigatorItem) {
  const value = draft.dataRinha ?? draft.horarioEncerramentoPresenca
  if (!value) return t('drafts.noRinhaDate')
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return t('drafts.noRinhaDate')
  return date.toLocaleDateString(locale.value, { day: '2-digit', month: '2-digit', year: 'numeric', timeZone: 'UTC' })
}
</script>

<template>
  <nav
    class="draft-navigator panel-card"
    data-testid="draft-navigator"
    :data-compact-expanded="compactExpanded"
    :aria-label="t('drafts.listLabel')"
  >
    <div class="draft-navigator__heading">
      <h2>{{ t('drafts.listLabel') }}</h2>
      <button
        type="button"
        class="draft-navigator__toggle"
        data-navigator-toggle
        :aria-expanded="compactExpanded"
        aria-controls="draft-navigator-list"
        @click="compactExpanded = !compactExpanded"
      >
        {{ compactToggleLabel }}
      </button>
    </div>

    <section class="draft-navigator__filters" :aria-label="t('drafts.filtersLabel')">
      <label>
        <span>{{ t('drafts.searchLabel') }}</span>
        <input
          type="search"
          name="draft-search"
          autocomplete="off"
          :value="searchTerm"
          :placeholder="t('drafts.searchPlaceholder')"
          @input="updateSearch"
        />
      </label>
      <label>
        <span>{{ t('common.status') }}</span>
        <select name="draft-status" :value="selectedStatus" @change="updateStatus">
          <option value="">{{ t('common.all') }}</option>
          <option v-for="status in statusOptions" :key="status" :value="status">
            {{ t(`drafts.status.${status}`) }}
          </option>
        </select>
      </label>
      <Button type="button" variant="ghost" data-navigator-reset @click="emit('reset')">
        {{ t('drafts.actions.clearFilters') }}
      </Button>
    </section>

    <div id="draft-navigator-list" class="draft-navigator__list">
      <div v-if="loading && !hasKnownDrafts" class="draft-navigator__loading" data-navigator-loading role="status" :aria-label="t('drafts.navigator.loading')">
        <Skeleton v-for="index in 3" :key="index" class="draft-navigator__skeleton" />
      </div>

      <div v-else-if="loadFailed && !hasKnownDrafts" class="draft-navigator__state" data-navigator-load-failure role="alert">
        <h3>{{ t('drafts.navigator.loadFailedTitle') }}</h3>
        <p>{{ t('drafts.navigator.loadFailedDescription') }}</p>
        <Button type="button" variant="outline" data-navigator-retry @click="emit('retry')">
          {{ t('drafts.actions.retry') }}
        </Button>
      </div>

      <template v-else>
        <div v-if="loading" class="draft-navigator__feedback" data-navigator-feedback="loading" role="status">
          {{ t('drafts.navigator.refreshing') }}
        </div>
        <div v-else-if="loadFailed" class="draft-navigator__feedback" data-navigator-feedback="error" role="alert">
          <div>
            <strong>{{ t('drafts.navigator.refreshFailedTitle') }}</strong>
            <p>{{ t('drafts.navigator.refreshFailedDescription') }}</p>
          </div>
          <Button type="button" variant="outline" data-navigator-retry @click="emit('retry')">
            {{ t('drafts.actions.retry') }}
          </Button>
        </div>

        <div v-if="!loading && !loadFailed && !drafts.length && hasKnownDrafts" class="draft-navigator__state" data-navigator-no-results>
          <h3>{{ t('drafts.navigator.noResultsTitle') }}</h3>
          <p>{{ t('drafts.navigator.noResultsDescription') }}</p>
          <Button type="button" variant="outline" data-navigator-clear-results @click="emit('reset')">
            {{ t('drafts.actions.clearFilters') }}
          </Button>
        </div>

        <div v-else-if="!loading && !loadFailed && !drafts.length" class="draft-navigator__state" data-navigator-empty>
          <h3>{{ t('drafts.emptyTitle') }}</h3>
          <p>{{ t(canCreate ? 'drafts.navigator.emptyCreateDescription' : 'drafts.navigator.emptyUnauthorizedDescription') }}</p>
          <Button v-if="canCreate" type="button" data-navigator-create @click="emit('create')">
            {{ t('drafts.create') }}
          </Button>
        </div>

        <button
          v-for="draft in drafts"
          v-else
          :key="draft.id"
          type="button"
          class="draft-navigator__item"
          :class="{ 'is-selected': selectedDraftId === draft.id }"
          :data-draft-id="draft.id"
          :aria-current="selectedDraftId === draft.id ? 'true' : undefined"
          @click="emit('select', draft.id)"
        >
          <strong data-draft-name>{{ draft.nome }}</strong>
          <span
            class="draft-navigator__status team-status"
            :class="`draft-navigator__status--${statusVariant(draft.status)}`"
            data-draft-status
            :data-status="knownStatuses.has(draft.status) ? draft.status : 'unknown'"
            :data-variant="statusVariant(draft.status)"
          >
            {{ statusLabel(draft.status) }}
          </span>
          <span class="draft-navigator__date" data-draft-date>{{ t('drafts.rinhaDate', { date: formatDate(draft) }) }}</span>
        </button>
      </template>
    </div>
  </nav>
</template>

<style scoped>
.draft-navigator {
  display: grid;
  gap: 1rem;
  min-width: 0;
  padding: 1rem;
}

.draft-navigator__heading,
.draft-navigator__filters {
  display: flex;
  gap: 0.75rem;
}

.draft-navigator__heading {
  align-items: center;
  justify-content: space-between;
}

.draft-navigator__heading h2 {
  margin: 0;
  font-size: 1.25rem;
}

.draft-navigator__toggle {
  display: none;
  min-height: 2.75rem;
  color: var(--color-ink-muted);
}

.draft-navigator__filters {
  align-items: end;
  flex-wrap: wrap;
}

.draft-navigator__filters label {
  display: grid;
  flex: 1 1 10rem;
  gap: 0.25rem;
  min-width: 0;
  color: var(--color-ink-muted);
  font-size: 0.875rem;
}

.draft-navigator__filters input,
.draft-navigator__filters select {
  min-width: 0;
  min-height: 2.75rem;
  padding: 0.5rem 0.75rem;
  color: var(--color-ink);
  background: var(--color-canvas-raised);
  border: 1px solid var(--color-hairline-strong);
  border-radius: 0.5rem;
}

.draft-navigator__list,
.draft-navigator__loading {
  display: grid;
  gap: 0.75rem;
}

.draft-navigator__item {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto;
  gap: 0.375rem 0.75rem;
  width: 100%;
  min-height: 4.5rem;
  padding: 0.75rem;
  text-align: left;
  color: var(--color-ink);
  background: var(--color-canvas-raised);
  border: 1px solid var(--color-hairline);
  border-radius: 0.75rem;
}

.draft-navigator__item.is-selected {
  background: var(--color-primary-soft);
  border-color: var(--color-primary-hover);
  box-shadow: inset 3px 0 0 var(--color-primary-hover);
}

.draft-navigator__item strong {
  overflow: hidden;
  display: -webkit-box;
  min-width: 0;
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 2;
}

.draft-navigator__status,
.draft-navigator__date {
  font-family: var(--font-mono);
  font-size: 0.75rem;
}

.draft-navigator__status {
  align-self: start;
}

.draft-navigator__status--neutral {
  border-color: var(--color-hairline-soft);
  color: var(--color-ink-muted);
  background: var(--color-canvas-raised);
}

.draft-navigator__status--info {
  border-color: color-mix(in srgb, var(--color-info) 35%, transparent);
  color: var(--color-info);
  background: color-mix(in srgb, var(--color-info) 10%, transparent);
}

.draft-navigator__status--warning {
  border-color: color-mix(in srgb, var(--color-warning) 35%, transparent);
  color: var(--color-warning);
  background: color-mix(in srgb, var(--color-warning) 10%, transparent);
}

.draft-navigator__status--success {
  border-color: color-mix(in srgb, var(--color-success) 35%, transparent);
  color: var(--color-success);
  background: color-mix(in srgb, var(--color-success) 10%, transparent);
}

.draft-navigator__status--danger {
  border-color: color-mix(in srgb, var(--color-danger) 35%, transparent);
  color: var(--color-danger);
  background: color-mix(in srgb, var(--color-danger) 10%, transparent);
}

.draft-navigator__date {
  grid-column: 1 / -1;
  color: var(--color-ink-subtle);
}

.draft-navigator__state {
  display: grid;
  gap: 0.75rem;
  padding: 1rem;
  background: var(--color-canvas-raised);
  border: 1px solid var(--color-hairline);
  border-radius: 0.75rem;
}

.draft-navigator__state h3,
.draft-navigator__state p {
  margin: 0;
}

.draft-navigator__state p {
  color: var(--color-ink-muted);
}

.draft-navigator__feedback {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  padding: 0.75rem;
  color: var(--color-ink-muted);
  background: var(--color-canvas-raised);
  border: 1px solid var(--color-hairline-soft);
  border-radius: 0.5rem;
}

.draft-navigator__feedback p {
  margin: 0.25rem 0 0;
}

.draft-navigator__skeleton {
  height: 4.5rem;
}

@media (max-width: 768px) {
  .draft-navigator__toggle {
    display: inline-flex;
    align-items: center;
  }

  .draft-navigator[data-compact-expanded='false'] .draft-navigator__filters,
  .draft-navigator[data-compact-expanded='false'] .draft-navigator__item:not(.is-selected) {
    display: none;
  }

  .draft-navigator__item {
    grid-template-columns: minmax(0, 1fr);
  }

  .draft-navigator__status,
  .draft-navigator__date {
    grid-column: 1;
  }
}
</style>
