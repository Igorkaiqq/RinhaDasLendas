<script setup lang="ts">
import { computed, getCurrentInstance } from 'vue'

import DraftRail, { type DraftRailStep } from '@/components/layout/DraftRail.vue'
import { DRAFT_MONTAGEM_STATUS_OPTIONS } from '@/constants/draftMontagemStatus'

const props = defineProps<{
  status: string
  publicationStatus?: string | null
}>()

const instance = getCurrentInstance()
const t = (key: string) => instance?.proxy?.$t?.(key) ?? key

const operationalStatuses = DRAFT_MONTAGEM_STATUS_OPTIONS.filter((status) => status !== 'Cancelada')
const labels: Record<string, string> = {
  PresencaAberta: 'drafts.rail.presenceOpen',
  PresencaEncerrada: 'drafts.rail.presenceClosed',
  CapitaesDefinidos: 'drafts.rail.captains',
  OrdemDefinida: 'drafts.rail.order',
  Aberta: 'drafts.rail.picking',
  Finalizada: 'drafts.rail.finished',
}

const steps = computed<DraftRailStep[]>(() => {
  if (props.status === 'Cancelada') {
    return [
      { id: 'cancelled', label: t('drafts.rail.cancelled'), state: 'terminal' },
      discordStep.value,
    ]
  }

  const activeIndex = operationalStatuses.findIndex((status) => status === props.status)
  if (activeIndex === -1) {
    return [
      { id: 'unknown', label: t('drafts.rail.unknown'), state: 'unknown' },
      discordStep.value,
    ]
  }

  const base: DraftRailStep[] = operationalStatuses.map((status, index) => ({
    id: status,
    label: t(labels[status] ?? status),
    state: (index < activeIndex ? 'done' : index === activeIndex ? status === 'Finalizada' ? 'terminal' : 'active' : 'pending') as DraftRailStep['state'],
    current: index === activeIndex,
  }))

  base.push(discordStep.value)
  return base
})

const discordStep = computed<DraftRailStep>(() => ({
  id: 'discord',
  label: t('drafts.rail.discord'),
  state: ['RequerReconciliacao', 'Falha', 'Pendente', 'EmAndamento'].includes(props.publicationStatus ?? '')
    ? 'attention'
    : props.publicationStatus === 'Publicada'
      ? 'done'
      : 'pending',
}))
</script>

<template>
  <DraftRail :steps="steps" :aria-label="t('drafts.rail.label')" />
</template>
