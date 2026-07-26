<script setup lang="ts">
import { useI18n } from 'vue-i18n'

import type { DraftMontagemPublicacaoDiscordStatus, DraftMontagemPublicacaoDiscordTipo } from '@/types/draftMontagem'

interface DraftPublicationPresentation {
  tipo: DraftMontagemPublicacaoDiscordTipo | string
  status: DraftMontagemPublicacaoDiscordStatus | string | null
}

const props = defineProps<{
  publications: readonly DraftPublicationPresentation[]
  republishableTypes: readonly DraftMontagemPublicacaoDiscordTipo[]
  archived: boolean
  saving: boolean
}>()

const emit = defineEmits<{
  republish: [action: { publicationType: DraftMontagemPublicacaoDiscordTipo; publicationStatus: DraftMontagemPublicacaoDiscordStatus | string | null }]
}>()

const { t, te } = useI18n()
const knownTypes: readonly DraftMontagemPublicacaoDiscordTipo[] = ['Presenca', 'ChamadaPresenca', 'TimesDefinidos', 'Cancelamento']

function isKnownType(tipo: string): tipo is DraftMontagemPublicacaoDiscordTipo {
  return knownTypes.includes(tipo as DraftMontagemPublicacaoDiscordTipo)
}

function statusKey(status: string | null) {
  const key = status ? `drafts.publication.status.${status}` : ''
  return key && te(key) ? key : 'drafts.publication.status.unknown'
}

function publicationStatusKey(publication: DraftPublicationPresentation) {
  return isKnownType(publication.tipo) ? statusKey(publication.status) : 'drafts.publication.status.unknown'
}

function publicationText(publication: DraftPublicationPresentation) {
  if (!isKnownType(publication.tipo)) return t('drafts.publication.unknownType', { status: t(publicationStatusKey(publication)) })
  const keys: Record<DraftMontagemPublicacaoDiscordTipo, string> = {
    Presenca: 'drafts.publication.presence',
    ChamadaPresenca: 'drafts.publication.presenceCta',
    TimesDefinidos: 'drafts.publication.finalTeams',
    Cancelamento: 'drafts.publication.cancellation',
  }
  return t(keys[publication.tipo], { status: t(statusKey(publication.status)) })
}

function canRepublish(publication: DraftPublicationPresentation) {
  if (!isKnownType(publication.tipo) || !props.republishableTypes.includes(publication.tipo)) return false
  if (!props.archived) return publication.tipo !== 'Cancelamento'
  return publication.tipo === 'Cancelamento' && ['Falha', 'RequerReconciliacao'].includes(publication.status ?? '')
}

function republish(publication: DraftPublicationPresentation) {
  if (!props.saving && canRepublish(publication) && isKnownType(publication.tipo)) {
    emit('republish', { publicationType: publication.tipo, publicationStatus: publication.status })
  }
}

function actionKey(tipo: DraftMontagemPublicacaoDiscordTipo) {
  if (tipo === 'Cancelamento') return 'drafts.publication.republishCancellation'
  return tipo === 'Presenca'
    ? 'drafts.publication.republishPresence'
    : tipo === 'ChamadaPresenca'
      ? 'drafts.publication.republishPresenceCta'
      : 'drafts.publication.republishFinalTeams'
}

function actionTestId(tipo: DraftMontagemPublicacaoDiscordTipo) {
  if (tipo === 'Cancelamento') return 'republish-cancellation'
  return tipo === 'Presenca' ? 'republish-presence' : tipo === 'ChamadaPresenca' ? 'republish-presence-cta' : 'republish-final-teams'
}
</script>

<template>
  <section data-discord-publications class="draft-publications draft-publications--subordinate" :aria-labelledby="'draft-publications-title'">
    <header>
      <p class="page-kicker">{{ t('drafts.publication.kicker') }}</p>
      <h2 id="draft-publications-title">{{ t('drafts.publication.title') }}</h2>
      <p>{{ t('drafts.publication.description') }}</p>
    </header>
    <ul>
      <li v-for="publication in publications" :key="publication.tipo" :data-publication-type="publication.tipo">
        <span
          class="team-status"
          :data-publication-status="publicationStatusKey(publication).endsWith('.unknown') ? 'unknown' : publication.status"
        >
          {{ publicationText(publication) }}
        </span>
        <button
          v-if="canRepublish(publication)"
          :data-testid="actionTestId(publication.tipo as DraftMontagemPublicacaoDiscordTipo)"
          type="button"
          class="button-secondary"
          :disabled="saving"
          @click="republish(publication)"
        >
          {{ t(actionKey(publication.tipo as DraftMontagemPublicacaoDiscordTipo)) }}
        </button>
      </li>
    </ul>
  </section>
</template>

<style scoped>
.draft-publications {
  display: grid;
  gap: var(--space-sm);
  min-width: 0;
  padding: var(--space-sm) var(--space-md);
  border: 1px solid var(--color-hairline-soft);
  border-radius: var(--radius-lg);
  background: var(--color-surface-1);
}

.draft-publications header,
.draft-publications h2,
.draft-publications p {
  margin: 0;
}

.draft-publications header > p:last-child {
  color: var(--color-ink-muted);
  font-size: 14px;
}

.draft-publications ul {
  display: grid;
  gap: var(--space-xs);
  margin: 0;
  padding: 0;
  list-style: none;
}

.draft-publications li {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-sm);
  min-width: 0;
}

[data-publication-status='unknown'] {
  border-color: var(--color-hairline-strong);
  color: var(--color-ink-muted);
  background: var(--color-surface-2);
}
</style>
