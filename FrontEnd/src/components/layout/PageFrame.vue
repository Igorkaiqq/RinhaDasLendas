<script setup lang="ts">
defineProps<{
  rail?: boolean
}>()
</script>

<template>
  <main class="mesa-page-frame" :data-has-rail="rail || undefined">
    <slot />
  </main>
</template>
