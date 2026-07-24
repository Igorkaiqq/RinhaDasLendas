<script setup lang="ts">
import { computed, nextTick, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'

import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import {
  Empty,
  EmptyDescription,
  EmptyHeader,
  EmptyTitle,
} from '@/components/ui/empty'
import { Skeleton } from '@/components/ui/skeleton'
import { listPresenceScheduleOccurrences } from '@/services/presenceSchedules'
import type {
  PresenceScheduleOccurrenceStatus,
  PresenceScheduleOccurrenceSummary,
} from '@/types/presenceSchedule'

const pageSize = 10

interface FocusTarget {
  focus: () => void
}

const props = defineProps<{
  open: boolean
  scheduleId: string
  scheduleName: string
  returnFocusTo?: FocusTarget
}>()

const emit = defineEmits<{ 'update:open': [value: boolean] }>()
const { d, locale, t, te } = useI18n()
const items = ref<PresenceScheduleOccurrenceSummary[]>([])
const page = ref(1)
const totalPages = ref(0)
const totalItems = ref(0)
const loading = ref(false)
const error = ref(false)

const liveMessage = computed(() => totalPages.value > 0
  ? t('settings.presenceSchedules.history.livePage', { page: page.value, total: totalPages.value })
  : '')

watch(
  [() => props.open, () => props.scheduleId],
  ([open]) => {
    if (open) void loadPage(1)
    else restoreFocus()
  },
  { immediate: true },
)

async function loadPage(targetPage: number) {
  loading.value = true
  error.value = false
  try {
    const response = await listPresenceScheduleOccurrences(props.scheduleId, targetPage, pageSize)
    items.value = response.items
    page.value = response.page
    totalPages.value = response.totalPages
    totalItems.value = response.totalItems
  } catch {
    error.value = true
  } finally {
    loading.value = false
  }
}

function formatInstant(value: string) {
  return d(new Date(value), { dateStyle: 'medium', timeStyle: 'short' })
}

function formatLocalDate(value: string) {
  return new Intl.DateTimeFormat(locale.value, { dateStyle: 'long', timeZone: 'UTC' }).format(new Date(`${value}T00:00:00Z`))
}

function statusVariant(status: PresenceScheduleOccurrenceStatus) {
  if (status === 'Falha' || status === 'Perdida') return 'destructive'
  if (status === 'Criada') return 'default'
  return 'secondary'
}

function messageCodeLabel(messageCode: string) {
  const key = `settings.presenceSchedules.messageCodes.${messageCode}`
  return te(key) ? t(key) : t('settings.presenceSchedules.messageCodes.unknown')
}

function setOpen(open: boolean) {
  emit('update:open', open)
}

async function restoreFocus() {
  await nextTick()
  props.returnFocusTo?.focus()
}
</script>

<template>
  <Dialog :open="open" @update:open="setOpen">
    <DialogContent class="presence-schedule-history-dialog sm:max-w-2xl" @keydown.esc.stop="setOpen(false)">
      <DialogHeader>
        <DialogTitle>{{ t('settings.presenceSchedules.history.title', { name: scheduleName }) }}</DialogTitle>
        <DialogDescription>{{ t('settings.presenceSchedules.history.description') }}</DialogDescription>
      </DialogHeader>

      <div class="presence-schedule-history__body">
        <div v-if="loading" class="presence-schedule-history__skeletons" :aria-label="t('settings.presenceSchedules.history.loading')">
          <Skeleton v-for="index in 3" :key="index" data-history-skeleton class="h-24 w-full" />
        </div>

        <div v-else-if="error" class="presence-schedule-state" role="alert">
          <p>{{ t('settings.presenceSchedules.history.error') }}</p>
          <Button type="button" variant="outline" data-history-retry @click="loadPage(page)">
            {{ t('settings.presenceSchedules.actions.retry') }}
          </Button>
        </div>

        <Empty v-else-if="items.length === 0" class="presence-schedule-state">
          <EmptyHeader>
            <EmptyTitle>{{ t('settings.presenceSchedules.history.empty.title') }}</EmptyTitle>
            <EmptyDescription>{{ t('settings.presenceSchedules.history.empty.description') }}</EmptyDescription>
          </EmptyHeader>
        </Empty>

        <ol v-else class="presence-schedule-history" role="list" :aria-label="t('settings.presenceSchedules.accessibility.historyList')">
          <li v-for="occurrence in items" :key="occurrence.id" data-occurrence class="presence-schedule-history__item">
            <div class="presence-schedule-history__heading">
              <time :datetime="occurrence.dataLocal">{{ formatLocalDate(occurrence.dataLocal) }}</time>
              <Badge :variant="statusVariant(occurrence.status)" :data-status="occurrence.status">
                {{ t(`settings.presenceSchedules.statuses.occurrence.${occurrence.status}`) }}
              </Badge>
            </div>
            <p>{{ t('settings.presenceSchedules.history.window', {
              publication: formatInstant(occurrence.publicacaoPrevistaEm),
              closing: formatInstant(occurrence.encerramentoPrevistoEm),
            }) }}</p>
            <p v-if="occurrence.draftMontagemId" class="presence-schedule-history__draft">
              {{ t('settings.presenceSchedules.history.draft', { id: occurrence.draftMontagemId }) }}
            </p>
            <p v-if="occurrence.messageCode" class="form-error">
              {{ messageCodeLabel(occurrence.messageCode) }}
            </p>
          </li>
        </ol>
      </div>

      <p class="sr-only" aria-live="polite">{{ liveMessage }}</p>
      <DialogFooter class="presence-schedule-history__pagination">
        <span v-if="totalItems > 0">{{ t('settings.presenceSchedules.history.total', { total: totalItems }) }}</span>
        <Button type="button" variant="outline" data-history-previous :disabled="loading || page <= 1" @click="loadPage(page - 1)">
          {{ t('settings.presenceSchedules.actions.previous') }}
        </Button>
        <Button type="button" variant="outline" data-history-next :disabled="loading || page >= totalPages" @click="loadPage(page + 1)">
          {{ t('settings.presenceSchedules.actions.next') }}
        </Button>
      </DialogFooter>
    </DialogContent>
  </Dialog>
</template>
