<script setup lang="ts">
import { Badge } from '@/components/ui/badge'

defineProps<{
  tone?: 'neutral' | 'success' | 'warning' | 'danger' | 'info' | 'gold'
}>()
</script>

<template>
  <Badge class="mesa-status-badge" :data-tone="tone || 'neutral'">
    <slot />
  </Badge>
</template>
