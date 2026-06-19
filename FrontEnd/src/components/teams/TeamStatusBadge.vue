<script setup lang="ts">
import { computed } from 'vue'

import { TeamStatus } from '@/constants/teamStatus'
import type { TeamStatusValue } from '@/types/team'

const props = defineProps<{
  status: TeamStatusValue
}>()

const label = computed(() => (props.status === TeamStatus.Active ? 'Ativo' : 'Inativo'))
</script>

<template>
  <span class="player-status" :class="{ 'team-status--inactive': status === TeamStatus.Inactive }">{{ label }}</span>
</template>
