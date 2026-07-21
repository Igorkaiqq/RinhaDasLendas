<script setup lang="ts">
import { Card, CardContent } from '@/components/ui/card'

defineProps<{
  title: string
  description: string
}>()
</script>

<template>
  <Card class="mesa-empty-state">
    <CardContent class="mesa-empty-state__content">
      <div class="mesa-empty-state__mark" aria-hidden="true">◇</div>
      <h2>{{ title }}</h2>
      <p>{{ description }}</p>
      <div v-if="$slots.action" class="mesa-empty-state__action">
        <slot name="action" />
      </div>
    </CardContent>
  </Card>
</template>
