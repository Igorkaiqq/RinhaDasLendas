<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'

import type { SeriesDetail } from '@/types/series'

const props = defineProps<{ series: SeriesDetail }>()
const { t } = useI18n()
const winsRequired = computed(() => (props.series.formato === 'Md5' ? 3 : 2))
</script>

<template>
  <output
    class="series-scoreboard"
    data-series-scoreboard
    aria-live="polite"
    :aria-label="t('competitive.series.scoreboard.label')"
  >
    <span>{{ series.lados[0]?.nome }}</span>
    <strong>{{ series.placar[0] }} — {{ series.placar[1] }}</strong>
    <span>{{ series.lados[1]?.nome }}</span>
    <small>{{
      t('competitive.series.scoreboard.required', { count: winsRequired })
    }}</small>
  </output>
</template>

<style scoped>
.series-scoreboard {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto minmax(0, 1fr);
  align-items: center;
  gap: var(--space-md);
  width: 100%;
  padding: var(--space-lg);
  border: 1px solid var(--color-hairline-strong);
  border-radius: var(--radius-xl);
  color: var(--color-ink-muted);
  background: var(--color-surface-1);
  text-align: center;
}

.series-scoreboard strong {
  color: var(--color-ink);
  font-family: var(--font-data);
  font-size: clamp(28px, 5vw, 48px);
}

.series-scoreboard small {
  grid-column: 1 / -1;
  color: var(--color-ink-subtle);
  font-family: var(--font-data);
}

@media (max-width: 479px) {
  .series-scoreboard {
    grid-template-columns: 1fr;
  }

  .series-scoreboard small {
    grid-column: auto;
  }
}
</style>
