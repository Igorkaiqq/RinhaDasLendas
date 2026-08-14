<script setup lang="ts">
import { useI18n } from 'vue-i18n'

import type { SeriesDetail } from '@/types/series'

defineProps<{ series: SeriesDetail }>()
const { t } = useI18n()
</script>

<template>
  <section
    class="fearless-panel"
    data-fearless-panel
    :aria-label="t('competitive.series.fearless.label')"
  >
    <header>
      <div>
        <p>{{ t('competitive.series.fearless.eyebrow') }}</p>
        <h2>{{ t('competitive.series.fearless.label') }}</h2>
      </div>
      <span>{{ t(`competitive.series.modes.${series.modoDraft}`) }}</span>
    </header>

    <p v-if="series.revisaoNecessaria" role="alert" data-review-required>
      {{ t('competitive.series.fearless.reviewRequired') }}
    </p>

    <div class="fearless-panel__sides">
      <section
        v-for="side in series.lados"
        :key="side.id"
        data-side-fearless
        :aria-label="t('competitive.series.fearless.side', { name: side.nome })"
      >
        <h3>{{ side.nome }}</h3>
        <ul v-if="series.bloqueiosFearless.length > 0">
          <li
            v-for="championId in series.bloqueiosFearless"
            :key="championId"
            data-blocked-champion
            :data-champion-id="championId"
          >
            {{ championId }}
          </li>
        </ul>
        <p v-else>{{ t('competitive.series.fearless.empty') }}</p>
      </section>
    </div>
  </section>
</template>

<style scoped>
.fearless-panel {
  display: grid;
  gap: var(--space-md);
  padding: var(--space-lg);
  border: 1px solid var(--color-hairline);
  border-radius: var(--radius-xl);
  background: var(--color-canvas-raised);
}

.fearless-panel > header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-md);
}

.fearless-panel > header p,
.fearless-panel > header span {
  color: var(--color-primary-hover);
  font-family: var(--font-data);
  font-size: 12px;
  text-transform: uppercase;
}

.fearless-panel > header h2 {
  margin-top: var(--space-xxs);
}

.fearless-panel > [role='alert'] {
  padding: var(--space-sm);
  border: 1px solid var(--color-warning);
  border-radius: var(--radius-md);
  color: var(--color-warning);
  background: color-mix(
    in srgb,
    var(--color-warning) 10%,
    var(--color-surface-1)
  );
}

.fearless-panel__sides {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: var(--space-md);
}

.fearless-panel__sides section {
  min-width: 0;
  padding: var(--space-md);
  border: 1px solid var(--color-hairline-strong);
  border-radius: var(--radius-lg);
  background: var(--color-surface-1);
}

.fearless-panel__sides section:first-child {
  box-shadow: inset 3px 0 0 var(--color-secondary);
}

.fearless-panel__sides section:last-child {
  box-shadow: inset -3px 0 0 var(--color-primary);
}

.fearless-panel ul {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-xs);
  margin: var(--space-sm) 0 0;
  padding: 0;
  list-style: none;
}

.fearless-panel li {
  min-width: 38px;
  padding: var(--space-xs);
  border: 1px solid var(--color-hairline-strong);
  border-radius: var(--radius-md);
  color: var(--color-ink-muted);
  background: var(--color-surface-2);
  font-family: var(--font-data);
  text-align: center;
}

.fearless-panel section > p {
  margin-top: var(--space-sm);
  color: var(--color-ink-subtle);
}

@media (max-width: 767px) {
  .fearless-panel__sides {
    grid-template-columns: 1fr;
  }
}
</style>
