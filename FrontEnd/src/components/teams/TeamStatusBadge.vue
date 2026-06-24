<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'

import { TeamStatus } from '@/constants/teamStatus'
import type { TeamStatusValue } from '@/types/team'

const props = defineProps<{
  status: TeamStatusValue
}>()

const { t } = useI18n()

const label = computed(() => (props.status === TeamStatus.Active ? t('teams.status.active') : t('teams.status.inactive')))
</script>

<template>
  <span class="player-status" :class="{ 'team-status--inactive': status === TeamStatus.Inactive }">{{ label }}</span>
</template>
