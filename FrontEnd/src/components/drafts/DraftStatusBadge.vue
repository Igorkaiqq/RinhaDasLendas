<script setup lang="ts">
import type { DraftStatusValue } from '@/types/draft'

defineProps<{ status: DraftStatusValue }>()
</script>

<template>
  <span class="team-status" :class="`team-status--${status.toLowerCase()}`">{{ status }}</span>
</template>
