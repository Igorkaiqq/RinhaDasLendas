<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'

import { Badge } from '@/components/ui/badge'
import { DRAFT_MONTAGEM_STATUS_OPTIONS } from '@/constants/draftMontagemStatus'
import type { DraftMontagem, DraftMontagemPublicacaoDiscordStatus } from '@/types/draftMontagem'

import DraftStateRail from './DraftStateRail.vue'

type DraftWorkspacePresentation = Omit<DraftMontagem, 'status'> & { status: string }

const props = defineProps<{
  draft: DraftWorkspacePresentation
  confirmedCount: number
  finalTeamsPublicationStatus: DraftMontagemPublicacaoDiscordStatus | null
}>()

const { locale, t } = useI18n()
const knownStatuses = new Set<string>(DRAFT_MONTAGEM_STATUS_OPTIONS)
const statusLabel = computed(() => t(knownStatuses.has(props.draft.status) ? `drafts.status.${props.draft.status}` : 'drafts.status.unknown'))
const draftDate = computed(() => {
  if (!props.draft.horarioEncerramentoPresenca) return t('drafts.noRinhaDate')
  return new Date(props.draft.horarioEncerramentoPresenca).toLocaleDateString(locale.value, {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    timeZone: 'UTC',
  })
})
</script>

<template>
  <header class="panel-card presence-panel" data-testid="draft-workspace-header">
    <div class="draft-summary">
      <div>
        <span class="eyebrow">{{ t('drafts.kicker') }}</span>
        <h1>{{ draft.nome }}</h1>
        <p data-workspace-date>{{ t('drafts.rinhaDate', { date: draftDate }) }}</p>
      </div>
      <div class="draft-hero-actions">
        <Badge variant="outline" data-workspace-status>{{ statusLabel }}</Badge>
        <span data-workspace-counts>{{ t('drafts.presence.summary', { count: confirmedCount, teams: draft.quantidadeTimes, reserves: draft.quantidadeReservas }) }}</span>
      </div>
    </div>

    <DraftStateRail :status="draft.status" :publication-status="finalTeamsPublicationStatus" />

    <div class="draft-hero-actions">
      <div data-action-group="primary"><slot name="primary-action" /></div>
      <div data-action-group="secondary"><slot name="secondary-actions" /></div>
      <div data-action-group="danger"><slot name="danger-action" /></div>
    </div>
  </header>
</template>
