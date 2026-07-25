<script setup lang="ts">
defineProps<{
  eyebrow?: string
  title: string
  description?: string
}>()
</script>

<template>
  <header class="mesa-page-header">
    <div class="mesa-page-header__copy">
      <p v-if="eyebrow" class="mesa-eyebrow">{{ eyebrow }}</p>
      <h1>{{ title }}</h1>
      <p v-if="description">{{ description }}</p>
    </div>
    <div v-if="$slots.actions" class="mesa-page-header__actions">
      <slot name="actions" />
    </div>
  </header>
</template>
