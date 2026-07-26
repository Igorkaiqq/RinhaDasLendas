<script setup lang="ts">
export interface DraftRailStep {
  id: string
  label: string
  state: 'done' | 'active' | 'pending' | 'attention' | 'terminal' | 'unknown'
  stateLabel: string
  ariaLabel: string
  current?: boolean
}

defineProps<{ steps: DraftRailStep[] }>()
</script>

<template>
  <ol class="draft-rail" :aria-label="$attrs['aria-label'] as string">
    <li
      v-for="step in steps"
      :key="step.id"
      class="draft-rail__step"
      :data-step-id="step.id"
      :data-state="step.state"
      :aria-current="step.current ? 'step' : undefined"
      :aria-label="step.ariaLabel"
    >
      <span class="draft-rail__node" aria-hidden="true" />
      <span class="draft-rail__copy">
        <span>{{ step.label }}</span>
        <small data-step-state-label>{{ step.stateLabel }}</small>
      </span>
    </li>
  </ol>
</template>
