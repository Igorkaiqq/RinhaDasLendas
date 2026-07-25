<script setup lang="ts">
export interface DraftRailStep {
  id: string
  label: string
  state: 'done' | 'active' | 'pending' | 'attention' | 'terminal' | 'unknown'
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
    >
      <span class="draft-rail__node" aria-hidden="true" />
      <span>{{ step.label }}</span>
    </li>
  </ol>
</template>
