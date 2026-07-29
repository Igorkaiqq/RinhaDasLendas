<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'

import DraftRail, { type DraftRailStep } from '@/components/layout/DraftRail.vue'
import { DRAFT_MONTAGEM_STATUS_OPTIONS } from '@/constants/draftMontagemStatus'
import type { DraftMontagemCicloVersao, DraftMontagemModo } from '@/types/draftMontagem'

const props = defineProps<{
  status: string
  modo: DraftMontagemModo | null
  cicloVersao: DraftMontagemCicloVersao
  publicationStatus?: string | null
}>()

const { t } = useI18n()

const operationalStatuses = DRAFT_MONTAGEM_STATUS_OPTIONS.filter((status) => status !== 'Cancelada')
const labels: Record<string, string> = {
  PresencaAberta: 'drafts.rail.presenceOpen',
  PresencaEncerrada: 'drafts.rail.presenceClosed',
  Modo: 'drafts.rail.mode',
  CapitaesDefinidos: 'drafts.rail.captains',
  OrdemDefinida: 'drafts.rail.order',
  Aberta: 'drafts.rail.picking',
  Finalizada: 'drafts.rail.finished',
}
const stateKeys: Record<DraftRailStep['state'], string> = {
  done: 'drafts.progress.completed',
  active: 'drafts.progress.current',
  pending: 'drafts.progress.pending',
  attention: 'drafts.progress.attention',
  terminal: 'drafts.progress.terminal',
  unknown: 'drafts.progress.unknown',
}

function step(id: string, label: string, state: DraftRailStep['state'], current = false): DraftRailStep {
  const stateLabel = t(stateKeys[state])
  return {
    id,
    label,
    state,
    stateLabel,
    ariaLabel: t('drafts.accessibility.stateLabel', { label, state: stateLabel }),
    current,
  }
}

const steps = computed<DraftRailStep[]>(() => {
  if (props.status === 'Cancelada') {
    return [step('cancelled', t('drafts.rail.cancelled'), 'terminal')]
  }

  const v2 = props.cicloVersao === 'ModoPosPresenca'
  const sequence = !v2
    ? operationalStatuses
    : props.modo === 'Manual'
      ? ['PresencaAberta', 'PresencaEncerrada', 'Aberta', 'Finalizada']
      : ['PresencaAberta', 'PresencaEncerrada', 'Modo', 'CapitaesDefinidos', 'OrdemDefinida', 'Aberta', 'Finalizada']
  const currentId = v2 && props.modo === null && props.status === 'PresencaEncerrada' ? 'Modo' : props.status
  const activeIndex = sequence.findIndex((status) => status === currentId)
  if (activeIndex === -1) {
    return [step('unknown', t('drafts.rail.unknown'), 'unknown')]
  }

  return sequence.map((status, index) => {
    const state = (index < activeIndex ? 'done' : index === activeIndex ? status === 'Finalizada' ? 'terminal' : 'active' : 'pending') as DraftRailStep['state']
    return step(status, t(labels[status] ?? status), state, index === activeIndex)
  })
})

const discordState = computed<DraftRailStep['state']>(() => (
  ['RequerReconciliacao', 'Falha', 'Pendente', 'EmAndamento'].includes(props.publicationStatus ?? '')
    ? 'attention'
    : props.publicationStatus === 'Publicada'
      ? 'done'
      : 'pending'
))
const discordStateLabel = computed(() => t(stateKeys[discordState.value]))
const discordLabel = computed(() => t('drafts.rail.discord'))
</script>

<template>
  <div class="draft-state-progress">
    <DraftRail :steps="steps" :aria-label="t('drafts.rail.label')" />
    <div
      class="draft-integration-status"
      data-discord-integration
      :data-state="discordState"
      role="status"
      aria-live="polite"
      :aria-label="t('drafts.accessibility.stateLabel', { label: discordLabel, state: discordStateLabel })"
    >
      <span class="draft-integration-status__node" aria-hidden="true" />
      <span class="draft-rail__copy">
        <span>{{ discordLabel }}</span>
        <small data-integration-state-label>{{ discordStateLabel }}</small>
      </span>
    </div>
  </div>
</template>
