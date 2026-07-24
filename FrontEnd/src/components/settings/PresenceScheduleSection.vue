<script setup lang="ts">
import { CalendarClockIcon, HistoryIcon, PlusIcon } from '@lucide/vue'
import { computed, nextTick, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { toast } from 'vue-sonner'

import PresenceScheduleConfirmDialog from '@/components/settings/PresenceScheduleConfirmDialog.vue'
import PresenceScheduleFormDialog from '@/components/settings/PresenceScheduleFormDialog.vue'
import PresenceScheduleOccurrenceHistoryDialog from '@/components/settings/PresenceScheduleOccurrenceHistoryDialog.vue'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import {
  Empty,
  EmptyContent,
  EmptyDescription,
  EmptyHeader,
  EmptyMedia,
  EmptyTitle,
} from '@/components/ui/empty'
import { Skeleton } from '@/components/ui/skeleton'
import {
  archivePresenceSchedule,
  createPresenceSchedule,
  listPresenceSchedules,
  pausePresenceSchedule,
  PresenceScheduleServiceError,
  reactivatePresenceSchedule,
  updatePresenceSchedule,
} from '@/services/presenceSchedules'
import type {
  PresenceScheduleOccurrenceStatus,
  PresenceScheduleSummary,
  SavePresenceScheduleRequest,
} from '@/types/presenceSchedule'

const pageSize = 6
type ConfirmAction = 'pause' | 'reactivate' | 'archive'

interface FocusTarget {
  focus: () => void
}

interface ActionEvent {
  currentTarget: unknown
}

const { d, t, te } = useI18n()
const schedules = ref<PresenceScheduleSummary[]>([])
const page = ref(1)
const totalPages = ref(0)
const totalItems = ref(0)
const loading = ref(true)
const loadingMore = ref(false)
const loadError = ref(false)
const refreshError = ref(false)
const formOpen = ref(false)
const formMode = ref<'create' | 'edit'>('create')
const selectedSchedule = ref<PresenceScheduleSummary | null>(null)
const saving = ref(false)
const formMessageCode = ref<string | null>(null)
const confirmOpen = ref(false)
const confirmAction = ref<ConfirmAction>('pause')
const confirming = ref(false)
const historyOpen = ref(false)
const focusKey = ref('create')
let listGeneration = 0

const returnFocusTarget: FocusTarget = {
  focus: () => { void focusStableAction() },
}

const activeCount = computed(() => schedules.value.filter(({ status }) => status === 'Ativo').length)
const nextSchedule = computed(() => schedules.value.find(({ proximaExecucaoEm }) => proximaExecucaoEm))
const canLoadMore = computed(() => page.value < totalPages.value)

onMounted(loadInitial)

async function loadInitial() {
  const generation = ++listGeneration
  loading.value = true
  loadError.value = false
  refreshError.value = false
  try {
    const response = await listPresenceSchedules(1, pageSize)
    if (generation !== listGeneration) return
    schedules.value = response.items
    page.value = response.page
    totalPages.value = response.totalPages
    totalItems.value = response.totalItems
  } catch {
    if (generation !== listGeneration) return
    loadError.value = true
  } finally {
    if (generation === listGeneration) loading.value = false
  }
}

async function loadMore() {
  if (loadingMore.value || saving.value || confirming.value || !canLoadMore.value) return
  const generation = listGeneration
  loadingMore.value = true
  try {
    const response = await listPresenceSchedules(page.value + 1, pageSize)
    if (generation !== listGeneration) return
    const knownIds = new Set(schedules.value.map(({ id }) => id))
    schedules.value.push(...response.items.filter(({ id }) => !knownIds.has(id)))
    page.value = response.page
    totalPages.value = response.totalPages
    totalItems.value = response.totalItems
  } catch {
    if (generation !== listGeneration) return
    toast.error(t('settings.presenceSchedules.toasts.loadMoreError'))
  } finally {
    if (generation === listGeneration) loadingMore.value = false
  }
}

async function reloadLoadedPages(generation: number) {
  const targetPage = Math.max(page.value, 1)
  const refreshed: PresenceScheduleSummary[] = []
  const knownIds = new Set<string>()
  let lastResponse = await listPresenceSchedules(1, pageSize)
  if (generation !== listGeneration) return false

  for (let currentPage = 1; currentPage <= Math.min(targetPage, lastResponse.totalPages || 1); currentPage += 1) {
    if (currentPage > 1) lastResponse = await listPresenceSchedules(currentPage, pageSize)
    if (generation !== listGeneration) return false
    for (const schedule of lastResponse.items) {
      if (!knownIds.has(schedule.id)) {
        knownIds.add(schedule.id)
        refreshed.push(schedule)
      }
    }
  }

  schedules.value = refreshed
  page.value = lastResponse.page
  totalPages.value = lastResponse.totalPages
  totalItems.value = lastResponse.totalItems
  return true
}

function rememberFocus(key: string, event: ActionEvent) {
  const target = event.currentTarget as { dataset?: { focusKey?: string } } | null
  focusKey.value = target?.dataset?.focusKey ?? key
}

function openCreate(event: ActionEvent) {
  rememberFocus('create', event)
  selectedSchedule.value = null
  formMode.value = 'create'
  formMessageCode.value = null
  formOpen.value = true
}

function openEdit(schedule: PresenceScheduleSummary, event: ActionEvent) {
  rememberFocus(`edit:${schedule.id}`, event)
  selectedSchedule.value = schedule
  formMode.value = 'edit'
  formMessageCode.value = null
  formOpen.value = true
}

function openConfirmation(action: ConfirmAction, schedule: PresenceScheduleSummary, event: ActionEvent) {
  rememberFocus(`${action}:${schedule.id}`, event)
  selectedSchedule.value = schedule
  confirmAction.value = action
  confirmOpen.value = true
}

function openHistory(schedule: PresenceScheduleSummary, event: ActionEvent) {
  rememberFocus(`history:${schedule.id}`, event)
  selectedSchedule.value = schedule
  historyOpen.value = true
}

async function save(payload: SavePresenceScheduleRequest) {
  if (saving.value) return
  const generation = ++listGeneration
  loadingMore.value = false
  saving.value = true
  formMessageCode.value = null
  let successKey = 'settings.presenceSchedules.toasts.created'
  try {
    if (formMode.value === 'create') {
      await createPresenceSchedule(payload)
    } else if (selectedSchedule.value) {
      await updatePresenceSchedule(selectedSchedule.value.id, payload)
      successKey = 'settings.presenceSchedules.toasts.updated'
    } else {
      saving.value = false
      return
    }
  } catch (error) {
    formMessageCode.value = serviceMessageCode(error)
    toast.error(serviceErrorLabel(error, 'settings.presenceSchedules.toasts.saveError'))
    saving.value = false
    return
  }

  formOpen.value = false
  toast.success(t(successKey))
  saving.value = false
  await refreshAfterSuccessfulMutation(generation, focusKey.value)
}

async function confirmMutation() {
  if (confirming.value || !selectedSchedule.value) return
  const generation = ++listGeneration
  loadingMore.value = false
  const scheduleId = selectedSchedule.value.id
  const archiveFocusKey = confirmAction.value === 'archive' ? nextArchiveFocusKey(scheduleId) : ''
  const action = confirmAction.value
  confirming.value = true
  try {
    if (action === 'pause') {
      await pausePresenceSchedule(selectedSchedule.value.id)
    } else if (action === 'reactivate') {
      await reactivatePresenceSchedule(selectedSchedule.value.id)
    } else {
      await archivePresenceSchedule(selectedSchedule.value.id)
    }
  } catch (error) {
    toast.error(serviceErrorLabel(error, 'settings.presenceSchedules.toasts.actionError'))
    confirming.value = false
    return
  }

  const nextFocusKey = action === 'pause'
    ? `reactivate:${scheduleId}`
    : action === 'reactivate'
      ? `pause:${scheduleId}`
      : archiveFocusKey
  confirmOpen.value = false
  toast.success(t(`settings.presenceSchedules.toasts.${action}d`))
  confirming.value = false
  await refreshAfterSuccessfulMutation(generation, nextFocusKey)
}

async function refreshAfterSuccessfulMutation(generation: number, nextFocusKey: string) {
  try {
    if (!await reloadLoadedPages(generation)) return
    refreshError.value = false
    loadError.value = false
    focusKey.value = nextFocusKey
    await focusStableAction()
  } catch {
    if (generation !== listGeneration) return
    refreshError.value = true
    loadError.value = true
    await focusListRetry()
  }
}

function serviceMessageCode(error: unknown) {
  return error instanceof PresenceScheduleServiceError ? error.messageCode ?? null : null
}

function serviceErrorLabel(error: unknown, fallbackKey: string) {
  const code = serviceMessageCode(error)
  const key = code ? `settings.presenceSchedules.messageCodes.${code}` : ''
  return key && te(key) ? t(key) : t(fallbackKey)
}

function formatInstant(value: string) {
  return d(new Date(value), {
    dateStyle: 'medium',
    timeStyle: 'short',
    timeZone: 'America/Sao_Paulo',
  })
}

function occurrenceVariant(status: PresenceScheduleOccurrenceStatus) {
  if (status === 'Falha' || status === 'Perdida') return 'destructive'
  if (status === 'Criada') return 'default'
  return 'secondary'
}

function nextArchiveFocusKey(scheduleId: string) {
  const index = schedules.value.findIndex(({ id }) => id === scheduleId)
  const nextSchedule = schedules.value[index + 1] ?? schedules.value[index - 1]
  return nextSchedule ? `archive:${nextSchedule.id}` : 'create'
}

async function focusStableAction() {
  await nextTick()
  await nextTick()
  const elements = globalThis.document?.querySelectorAll('[data-focus-key]') ?? []
  const target = Array.from(elements).find((element) => element.getAttribute('data-focus-key') === focusKey.value)
    ?? Array.from(elements).find((element) => element.getAttribute('data-focus-key') === 'create')
  const focusableTarget = target as unknown as FocusTarget | undefined
  focusableTarget?.focus()
}

async function focusListRetry() {
  await nextTick()
  const retry = globalThis.document?.querySelector('[data-schedule-retry]') as FocusTarget | null
  retry?.focus()
}
</script>

<template>
  <section class="presence-schedule-section" aria-labelledby="presence-schedule-title">
    <header class="presence-schedule-section__header">
      <div>
        <span class="eyebrow">{{ t('settings.presenceSchedules.eyebrow') }}</span>
        <h2 id="presence-schedule-title">{{ t('settings.presenceSchedules.title') }}</h2>
        <p>{{ t('settings.presenceSchedules.description') }}</p>
      </div>
      <Button type="button" data-create-schedule data-focus-key="create" @click="openCreate">
        <PlusIcon data-icon="inline-start" />
        {{ t('settings.presenceSchedules.actions.create') }}
      </Button>
    </header>

    <div class="presence-schedule-summary" :aria-label="t('settings.presenceSchedules.accessibility.summary')">
      <Card>
        <CardHeader><CardDescription>{{ t('settings.presenceSchedules.summary.active') }}</CardDescription></CardHeader>
        <CardContent><strong>{{ activeCount }}</strong></CardContent>
      </Card>
      <Card>
        <CardHeader><CardDescription>{{ t('settings.presenceSchedules.summary.next') }}</CardDescription></CardHeader>
        <CardContent><strong>{{ nextSchedule?.proximaExecucaoEm ? formatInstant(nextSchedule.proximaExecucaoEm) : t('settings.presenceSchedules.summary.none') }}</strong></CardContent>
      </Card>
      <Card>
        <CardHeader><CardDescription>{{ t('settings.presenceSchedules.summary.timezoneLabel') }}</CardDescription></CardHeader>
        <CardContent><strong>{{ t('settings.presenceSchedules.summary.timezone') }}</strong></CardContent>
      </Card>
    </div>

    <div
      v-if="loading"
      data-schedule-loading
      class="presence-schedule-grid"
      role="status"
      aria-live="polite"
      aria-busy="true"
      :aria-label="t('settings.presenceSchedules.loading')"
    >
      <span class="sr-only">{{ t('settings.presenceSchedules.loading') }}</span>
      <Skeleton v-for="index in 3" :key="index" data-schedule-skeleton class="h-72 w-full" />
    </div>

    <div v-else-if="loadError" class="presence-schedule-state" role="alert">
      <p>{{ t(refreshError ? 'settings.presenceSchedules.refreshError' : 'settings.presenceSchedules.error') }}</p>
      <Button type="button" variant="outline" data-schedule-retry @click="loadInitial">
        {{ t('settings.presenceSchedules.actions.retry') }}
      </Button>
    </div>

    <Empty v-else-if="schedules.length === 0" class="presence-schedule-state">
      <EmptyHeader>
        <EmptyMedia variant="icon"><CalendarClockIcon /></EmptyMedia>
        <EmptyTitle>{{ t('settings.presenceSchedules.empty.title') }}</EmptyTitle>
        <EmptyDescription>{{ t('settings.presenceSchedules.empty.description') }}</EmptyDescription>
      </EmptyHeader>
      <EmptyContent>
        <Button type="button" data-empty-create data-focus-key="create" @click="openCreate">
          <PlusIcon data-icon="inline-start" />
          {{ t('settings.presenceSchedules.empty.action') }}
        </Button>
      </EmptyContent>
    </Empty>

    <div v-else>
      <ul
        class="presence-schedule-grid"
        data-schedule-list
        :aria-busy="loadingMore"
        :aria-label="t('settings.presenceSchedules.accessibility.scheduleList')"
      >
        <li
          v-for="schedule in schedules"
          :key="schedule.id"
          class="presence-schedule-card-item"
          :data-schedule-id="schedule.id"
        >
          <Card class="presence-schedule-card" data-schedule-card>
          <CardHeader>
            <div class="presence-schedule-card__title">
              <CardTitle class="presence-schedule-card__name">{{ schedule.nome }}</CardTitle>
              <Badge :variant="schedule.status === 'Ativo' ? 'default' : 'secondary'">
                {{ t(`settings.presenceSchedules.statuses.schedule.${schedule.status}`) }}
              </Badge>
            </div>
            <CardDescription v-if="schedule.observacao" class="presence-schedule-card__description">{{ schedule.observacao }}</CardDescription>
          </CardHeader>
          <CardContent class="presence-schedule-card__content">
            <dl>
              <div>
                <dt>{{ t('settings.presenceSchedules.card.days') }}</dt>
                <dd class="presence-schedule-card__days">
                  <Badge v-for="day in schedule.diasSemana" :key="day" variant="outline">{{ t(`settings.presenceSchedules.weekdays.${day}`) }}</Badge>
                </dd>
              </div>
              <div>
                <dt>{{ t('settings.presenceSchedules.card.window') }}</dt>
                <dd>{{ t('settings.presenceSchedules.card.windowValue', { publication: schedule.horarioPublicacao.slice(0, 5), closing: schedule.horarioEncerramento.slice(0, 5) }) }}</dd>
              </div>
              <div>
                <dt>{{ t('settings.presenceSchedules.card.next') }}</dt>
                <dd>{{ schedule.proximaExecucaoEm ? formatInstant(schedule.proximaExecucaoEm) : t('settings.presenceSchedules.card.noNext') }}</dd>
              </div>
            </dl>
            <div v-if="schedule.ultimaOcorrencia" class="presence-schedule-card__occurrence" data-occurrence-status>
              <span>{{ t('settings.presenceSchedules.card.latest') }}</span>
              <Badge :variant="occurrenceVariant(schedule.ultimaOcorrencia.status)" :data-status="schedule.ultimaOcorrencia.status">
                {{ t(`settings.presenceSchedules.statuses.occurrence.${schedule.ultimaOcorrencia.status}`) }}
              </Badge>
              <small v-if="schedule.ultimaOcorrencia.messageCode">
                {{ serviceErrorLabel(new PresenceScheduleServiceError(undefined, schedule.ultimaOcorrencia.messageCode), 'settings.presenceSchedules.messageCodes.unknown') }}
              </small>
            </div>
            <p v-else class="presence-schedule-card__occurrence">{{ t('settings.presenceSchedules.card.noOccurrence') }}</p>
          </CardContent>
          <CardFooter class="presence-schedule-card__actions" data-card-actions>
            <Button type="button" variant="outline" data-view-history :data-focus-key="`history:${schedule.id}`" @click="openHistory(schedule, $event)">
              <HistoryIcon data-icon="inline-start" />
              {{ t('settings.presenceSchedules.actions.viewHistory') }}
            </Button>
            <Button type="button" variant="outline" data-edit-schedule :data-focus-key="`edit:${schedule.id}`" @click="openEdit(schedule, $event)">{{ t('settings.presenceSchedules.actions.edit') }}</Button>
            <Button v-if="schedule.status === 'Ativo'" type="button" variant="secondary" data-pause-schedule :data-focus-key="`pause:${schedule.id}`" @click="openConfirmation('pause', schedule, $event)">{{ t('settings.presenceSchedules.actions.pause') }}</Button>
            <Button v-else type="button" variant="secondary" data-reactivate-schedule :data-focus-key="`reactivate:${schedule.id}`" @click="openConfirmation('reactivate', schedule, $event)">{{ t('settings.presenceSchedules.actions.reactivate') }}</Button>
            <Button type="button" variant="destructive" data-archive-schedule :data-focus-key="`archive:${schedule.id}`" @click="openConfirmation('archive', schedule, $event)">{{ t('settings.presenceSchedules.actions.archive') }}</Button>
          </CardFooter>
          </Card>
        </li>
      </ul>
      <div v-if="canLoadMore" class="presence-schedule-load-more">
        <Button type="button" variant="outline" data-load-more :disabled="loadingMore" @click="loadMore">
          {{ loadingMore ? t('settings.presenceSchedules.actions.loadingMore') : t('settings.presenceSchedules.actions.loadMore') }}
        </Button>
        <p v-if="loadingMore" data-load-more-status class="sr-only" role="status" aria-live="polite">
          {{ t('settings.presenceSchedules.actions.loadingMore') }}
        </p>
      </div>
    </div>

    <PresenceScheduleFormDialog
      v-model:open="formOpen"
      :mode="formMode"
      :schedule="selectedSchedule"
      :saving="saving"
      :service-message-code="formMessageCode"
      :return-focus-to="returnFocusTarget"
      @submit="save"
    />
    <PresenceScheduleConfirmDialog
      v-model:open="confirmOpen"
      :action="confirmAction"
      :schedule-name="selectedSchedule?.nome ?? ''"
      :submitting="confirming"
      :return-focus-to="returnFocusTarget"
      @confirm="confirmMutation"
    />
    <PresenceScheduleOccurrenceHistoryDialog
      v-model:open="historyOpen"
      :schedule-id="selectedSchedule?.id ?? ''"
      :schedule-name="selectedSchedule?.nome ?? ''"
      :return-focus-to="returnFocusTarget"
    />
  </section>
</template>
