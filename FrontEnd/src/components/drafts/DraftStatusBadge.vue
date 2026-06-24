<script setup lang="ts">
import type { DraftStatusValue } from '@/types/draft'
import { useI18n } from 'vue-i18n'

defineProps<{ status: DraftStatusValue }>()
const { t } = useI18n()
</script>

<template>
  <span class="team-status" :class="`team-status--${status.toLowerCase()}`">{{ t(`drafts.status.${status}`) }}</span>
</template>
