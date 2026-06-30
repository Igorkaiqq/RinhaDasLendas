<script setup lang="ts">
import { useI18n } from 'vue-i18n'

import type { RoutePreference } from '@/services/players'

const props = defineProps<{
  preferences: RoutePreference[]
}>()

const { t } = useI18n()

function orderedPreferences() {
  return [...props.preferences].sort((first, second) => first.prioridade - second.prioridade)
}
</script>

<template>
  <div class="route-panel" :aria-label="t('routePreferences.title')">
    <span
      v-for="preference in orderedPreferences()"
      :key="preference.rota"
      class="route-panel__badge"
      :class="{
        'route-panel__badge--primary': preference.prioridade === 1,
        'route-panel__badge--blocked': preference.naoJogoNemLascando,
      }"
    >
      {{ preference.prioridade }}. {{ preference.rota }}
      <strong v-if="preference.naoJogoNemLascando">{{ t('routePreferences.blocked') }}</strong>
    </span>
  </div>
</template>

<style scoped>
.route-panel {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-xs);
}

.route-panel__badge {
  display: inline-flex;
  align-items: center;
  gap: var(--space-xxs);
  min-height: 28px;
  padding: 4px 10px;
  border: 1px solid var(--color-hairline-soft);
  border-radius: var(--radius-pill);
  background: var(--color-canvas-raised);
  color: var(--color-ink-muted);
  font-size: 12px;
  font-weight: 700;
  transition:
    background var(--duration-base) var(--ease-standard),
    border-color var(--duration-base) var(--ease-standard),
    color var(--duration-base) var(--ease-standard);
}

.route-panel__badge strong {
  color: var(--color-danger);
  font-size: 11px;
}

.route-panel__badge--primary {
  border-color: rgb(139 92 246 / 42%);
  color: var(--color-ink);
  background: rgb(124 58 237 / 10%);
}

.route-panel__badge--blocked {
  border-color: rgb(239 68 68 / 34%);
  color: var(--color-ink-muted);
  background: rgb(239 68 68 / 7%);
}
</style>
