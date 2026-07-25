<script setup lang="ts">
defineProps<{
  rail?: boolean
}>()
</script>

<template>
  <div class="mesa-page-frame" :data-has-rail="rail || undefined">
    <slot />
  </div>
</template>
