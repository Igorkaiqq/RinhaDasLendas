<script setup lang="ts">
import { PlayerStatus, type PlayerStatusValue } from '@/constants/playerStatus'

defineProps<{
  status: PlayerStatusValue
}>()
</script>

<template>
  <span class="status-badge" :class="status === PlayerStatus.Active ? 'status-badge--active' : 'status-badge--inactive'">
    {{ status }}
  </span>
</template>

<style scoped>
.status-badge {
  display: inline-flex;
  align-items: center;
  width: fit-content;
  min-height: 28px;
  padding: 4px 10px;
  border: 1px solid var(--color-hairline);
  border-radius: var(--radius-pill);
  font-size: 0.75rem;
  font-weight: 700;
}

.status-badge--active {
  color: var(--color-success);
  background: rgb(34 197 94 / 12%);
}

.status-badge--inactive {
  color: var(--color-ink-subtle);
  background: var(--color-surface-3);
}
</style>
