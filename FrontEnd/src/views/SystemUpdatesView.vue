<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'

import PageFrame from '@/components/layout/PageFrame.vue'
import PageHeader from '@/components/layout/PageHeader.vue'
import SystemUpdateCard from '@/components/updates/SystemUpdateCard.vue'
import { Badge } from '@/components/ui/badge'
import { SYSTEM_UPDATES } from '@/constants/systemUpdates'
import {
  filterSystemUpdates,
  getLatestSystemUpdate,
} from '@/services/systemUpdates'
import type {
  SystemUpdateCategory,
  SystemUpdateRelease,
} from '@/types/systemUpdate'

const { locale, t } = useI18n()

const categories: readonly (SystemUpdateCategory | 'all')[] = [
  'all',
  'feature',
  'improvement',
  'fix',
  'security',
  'infrastructure',
]
const query = ref('')
const activeCategory = ref<SystemUpdateCategory | 'all'>('all')
const latest = getLatestSystemUpdate()

const filteredUpdates = computed(() =>
  filterSystemUpdates(SYSTEM_UPDATES, query.value, activeCategory.value, t),
)
const hasFilters = computed(
  () => query.value.length > 0 || activeCategory.value !== 'all',
)
const groupedUpdates = computed(() =>
  Object.entries(
    filteredUpdates.value.reduce<Record<string, SystemUpdateRelease[]>>(
      (groups, release) => {
        const month = release.publishedAt.slice(0, 7)
        groups[month] ??= []
        groups[month].push(release)
        return groups
      },
      {},
    ),
  ),
)
const latestDate = computed(() => formatDate(latest.publishedAt))

function formatDate(date: string): string {
  return new Intl.DateTimeFormat(locale.value, {
    day: '2-digit',
    month: 'long',
    year: 'numeric',
    timeZone: 'UTC',
  }).format(new Date(`${date}T00:00:00Z`))
}

function formatMonth(month: string): string {
  return new Intl.DateTimeFormat(locale.value, {
    month: 'long',
    year: 'numeric',
    timeZone: 'UTC',
  }).format(new Date(`${month}-01T00:00:00Z`))
}

function categoryLabel(category: SystemUpdateCategory | 'all'): string {
  return category === 'all'
    ? t('updates.allCategories')
    : t(`updates.categories.${category}`)
}

function clearFilters() {
  query.value = ''
  activeCategory.value = 'all'
}
</script>

<template>
  <PageFrame>
    <PageHeader
      :eyebrow="t('updates.eyebrow')"
      :title="t('updates.title')"
      :description="t('updates.description')"
    />

    <section
      data-latest-update
      class="system-updates-hero relative overflow-hidden rounded-2xl border border-primary/30 bg-card p-6 shadow-sm sm:p-8"
    >
      <div class="relative flex max-w-4xl flex-col gap-5">
        <div class="flex flex-wrap items-center gap-3 text-muted-foreground">
          <Badge variant="secondary">{{ t('updates.latest') }}</Badge>
          <span class="font-mono text-sm text-foreground" translate="no">{{
            latest.version
          }}</span>
          <time class="text-sm" :datetime="latest.publishedAt">{{
            latestDate
          }}</time>
        </div>

        <div class="flex flex-col gap-2">
          <h2
            class="text-balance text-2xl font-semibold text-foreground sm:text-3xl"
          >
            {{ t(latest.titleKey) }}
          </h2>
          <p
            data-latest-summary
            class="max-w-3xl text-pretty leading-relaxed text-muted-foreground"
          >
            {{ t(latest.summaryKey) }}
          </p>
        </div>

        <div class="flex flex-wrap gap-2">
          <div data-latest-categories class="contents">
            <Badge v-for="category in latest.categories" :key="category">
              {{ t(`updates.categories.${category}`) }}
            </Badge>
          </div>
          <div data-latest-areas class="contents">
            <Badge v-for="area in latest.areas" :key="area" variant="outline">
              {{ t(`updates.areas.${area}`) }}
            </Badge>
          </div>
        </div>
      </div>
    </section>

    <section
      class="system-updates-filters flex flex-col gap-4 rounded-xl border border-border bg-card p-4 sm:p-5"
      :aria-label="t('updates.filterLabel')"
    >
      <div class="flex flex-col gap-2">
        <label
          class="text-sm font-medium text-foreground"
          for="system-updates-search"
        >
          {{ t('updates.searchLabel') }}
        </label>
        <input
          id="system-updates-search"
          v-model="query"
          type="search"
          class="h-11 w-full rounded-lg border border-input bg-background px-3 text-foreground outline-none transition-colors placeholder:text-muted-foreground focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background"
          :placeholder="t('updates.searchPlaceholder')"
        />
      </div>

      <div
        class="flex max-w-full gap-2 overflow-x-auto pb-1"
        role="group"
        :aria-label="t('updates.filterLabel')"
      >
        <button
          v-for="category in categories"
          :key="category"
          type="button"
          class="h-9 shrink-0 rounded-full border px-4 text-sm font-medium outline-none transition-colors focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background"
          :class="
            activeCategory === category
              ? 'border-primary bg-primary text-primary-foreground'
              : 'border-border bg-background text-muted-foreground hover:border-primary/50 hover:text-foreground'
          "
          :aria-pressed="activeCategory === category"
          :data-category="category"
          @click="activeCategory = category"
        >
          {{ categoryLabel(category) }}
        </button>
      </div>

      <div class="flex flex-wrap items-center justify-between gap-3">
        <p
          data-result-count
          class="font-mono text-sm text-muted-foreground"
          aria-live="polite"
        >
          {{ t('updates.resultCount', filteredUpdates.length) }}
        </p>
        <button
          v-if="hasFilters && filteredUpdates.length"
          type="button"
          data-clear-filters
          class="rounded-md px-3 py-2 text-sm font-medium text-primary outline-none hover:bg-primary/10 focus-visible:ring-2 focus-visible:ring-ring"
          @click="clearFilters"
        >
          {{ t('updates.clearFilters') }}
        </button>
      </div>
    </section>

    <div
      v-if="filteredUpdates.length"
      class="system-updates-layout grid items-start gap-8 lg:grid-cols-[10rem_minmax(0,1fr)]"
    >
      <nav
        class="system-updates-index hidden rounded-xl border border-border bg-card p-4 lg:sticky lg:top-6 lg:block"
        :aria-label="t('updates.versionIndexLabel')"
      >
        <ul class="flex list-none flex-col gap-1">
          <li v-for="release in filteredUpdates" :key="release.id">
            <a
              class="block rounded-md px-2 py-2 font-mono text-xs text-muted-foreground outline-none hover:bg-muted hover:text-foreground focus-visible:ring-2 focus-visible:ring-ring"
              :href="`#update-${release.id}`"
            >
              {{ release.version }}
            </a>
          </li>
        </ul>
      </nav>

      <ol
        class="system-updates-timeline flex min-w-0 list-none flex-col gap-10"
        role="list"
        :aria-label="t('updates.timelineLabel')"
      >
        <li
          v-for="([month, releases], groupIndex) in groupedUpdates"
          :key="month"
          :data-update-group="month"
          class="flex min-w-0 flex-col gap-5"
        >
          <h2
            class="font-mono text-sm font-medium capitalize text-muted-foreground"
          >
            {{ formatMonth(month) }}
          </h2>
          <ol
            class="flex min-w-0 list-none flex-col gap-6 border-l border-border pl-4 sm:pl-6"
            role="list"
          >
            <li
              v-for="release in releases"
              :id="`update-${release.id}`"
              :key="release.id"
              data-system-update
              :data-categories="release.categories.join(' ')"
              class="relative min-w-0 scroll-mt-6 before:absolute before:-left-[1.29rem] before:top-6 before:size-2 before:rounded-full before:bg-primary sm:before:-left-[1.79rem]"
            >
              <SystemUpdateCard
                :release="release"
                :latest="groupIndex === 0 && release.id === latest.id"
              />
            </li>
          </ol>
        </li>
      </ol>
    </div>

    <section
      v-else
      class="flex flex-col items-start gap-3 rounded-xl border border-dashed border-border bg-card p-6"
    >
      <h2 class="text-xl font-semibold text-foreground">
        {{ t('updates.emptyTitle') }}
      </h2>
      <p class="text-muted-foreground">{{ t('updates.emptyDescription') }}</p>
      <button
        type="button"
        data-clear-filters
        class="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground outline-none hover:bg-primary/90 focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background"
        @click="clearFilters"
      >
        {{ t('updates.clearFilters') }}
      </button>
    </section>
  </PageFrame>
</template>
