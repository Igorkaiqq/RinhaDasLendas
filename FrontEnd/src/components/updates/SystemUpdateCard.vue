<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { RouterLink } from 'vue-router'

import { Badge } from '@/components/ui/badge'
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import { cn } from '@/lib/utils'
import type { SystemUpdateRelease } from '@/types/systemUpdate'

const props = defineProps<{
  release: SystemUpdateRelease
  latest: boolean
}>()

const { locale, t } = useI18n()

const groupedDetails = computed(() =>
  props.release.categories
    .map((category) => ({
      category,
      details: props.release.details.filter((detail) => detail.category === category),
    }))
    .filter((group) => group.details.length > 0),
)

const formattedDate = computed(() =>
  new Intl.DateTimeFormat(locale.value, {
    day: '2-digit',
    month: 'long',
    year: 'numeric',
    timeZone: 'UTC',
  }).format(new Date(`${props.release.publishedAt}T00:00:00Z`)),
)
</script>

<template>
  <article
    :class="cn('system-update-card', latest && 'system-update-card--latest')"
    :data-update-id="release.id"
  >
    <Card :class="cn('w-full', latest && 'ring-primary/40')">
      <CardHeader class="gap-4">
        <div class="flex flex-wrap items-center gap-2 text-muted-foreground">
          <Badge v-if="latest" variant="secondary">{{ t('updates.latest') }}</Badge>
          <span class="font-mono text-xs text-foreground" translate="no">{{ release.version }}</span>
          <time class="text-xs" :datetime="release.publishedAt">{{ formattedDate }}</time>
        </div>

        <div class="flex flex-col gap-2">
          <CardTitle>
            <h2 class="text-balance text-xl">{{ t(release.titleKey) }}</h2>
          </CardTitle>
          <CardDescription>
            <p class="text-pretty leading-relaxed">{{ t(release.summaryKey) }}</p>
          </CardDescription>
        </div>

        <div class="flex flex-wrap gap-2">
          <Badge v-for="category in release.categories" :key="category">
            {{ t(`updates.categories.${category}`) }}
          </Badge>
          <Badge v-for="area in release.areas" :key="area" variant="outline">
            {{ t(`updates.areas.${area}`) }}
          </Badge>
        </div>
      </CardHeader>

      <CardContent class="flex flex-col gap-3">
        <details
          v-for="group in groupedDetails"
          :key="group.category"
          class="group rounded-lg border border-border bg-muted/20 open:bg-muted/30"
        >
          <summary
            class="cursor-pointer rounded-lg px-4 py-3 outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background"
          >
            <Badge variant="secondary">
              {{ t(`updates.categories.${group.category}`) }}
            </Badge>
          </summary>

          <ul class="flex list-none flex-col gap-4 border-t border-border px-4 py-4">
            <li
              v-for="detail in group.details"
              :key="detail.id"
              class="flex min-w-0 flex-col gap-1"
              data-update-detail
            >
              <h3 class="break-words font-medium text-foreground">
                <RouterLink
                  v-if="detail.link"
                  :to="detail.link"
                  class="rounded-sm underline-offset-4 hover:text-primary hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                >
                  {{ t(detail.titleKey) }}
                </RouterLink>
                <template v-else>{{ t(detail.titleKey) }}</template>
              </h3>
              <p class="break-words text-sm leading-relaxed text-muted-foreground">
                {{ t(detail.descriptionKey) }}
              </p>
            </li>
          </ul>
        </details>
      </CardContent>
    </Card>
  </article>
</template>
