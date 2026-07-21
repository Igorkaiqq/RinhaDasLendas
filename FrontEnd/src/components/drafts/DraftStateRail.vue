<script setup lang="ts">
import { computed, getCurrentInstance } from 'vue'

import DraftRail, { type DraftRailStep } from '@/components/layout/DraftRail.vue'

const props = defineProps<{
  status: string
  publicationStatus?: string | null
}>()

const instance = getCurrentInstance()
const t = (key: string) => instance?.proxy?.$t?.(key) ?? key

const order = ['PresencaAberta', 'PresencaEncerrada', 'CapitaesDefinidos', 'OrdemDefinida', 'Aberta', 'Finalizada']
const labels: Record<string, string> = {
  PresencaAberta: 'drafts.rail.presenceOpen',
  PresencaEncerrada: 'drafts.rail.presenceClosed',
  CapitaesDefinidos: 'drafts.rail.captains',
  OrdemDefinida: 'drafts.rail.order',
  Aberta: 'drafts.rail.picking',
  Finalizada: 'drafts.rail.finished',
}

const steps = computed<DraftRailStep[]>(() => {
  const activeIndex = Math.max(order.indexOf(props.status), 0)
  const base = order.map((status, index) => ({
    id: status,
    label: t(labels[status] ?? status),
    state: (index < activeIndex ? 'done' : index === activeIndex ? 'active' : 'pending') as DraftRailStep['state'],
  }) satisfies DraftRailStep)

  base.push({
    id: 'discord',
    label: t('drafts.rail.discord'),
    state: (
      props.publicationStatus === 'Falha' || props.publicationStatus === 'Pendente'
        ? 'attention'
        : props.publicationStatus === 'Publicada'
          ? 'done'
          : 'pending'
    ) as DraftRailStep['state'],
  })

  return base
})
</script>

<template>
  <DraftRail :steps="steps" :aria-label="t('drafts.rail.label')" />
</template>
