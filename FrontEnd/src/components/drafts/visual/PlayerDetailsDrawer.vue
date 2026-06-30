<script setup lang="ts">
import { useI18n } from 'vue-i18n'

import type { DraftMontagemParticipante } from '@/types/draftMontagem'

defineProps<{ player: DraftMontagemParticipante | null }>()
defineEmits<{ close: [] }>()
const { t, locale } = useI18n()

function formatDate(value: string) {
  return new Date(value).toLocaleString(locale.value === 'pt' ? 'pt-BR' : 'en-US')
}

function eloSummary(player: DraftMontagemParticipante) {
  return [player.elo, player.divisao].filter(Boolean).join(' ') || t('common.notInformed')
}

function routeLabel(player: DraftMontagemParticipante) {
  return player.rotaContextual || player.preferencias.find((preference) => !preference.naoJogoNemLascando)?.rota || t('drafts.playerDetails.noRoute')
}
</script>

<template>
  <div v-if="player" class="dialog-backdrop" role="presentation" @click.self="$emit('close')">
    <section class="player-details-modal" role="dialog" aria-modal="true" :aria-label="t('drafts.playerDetails.dialogLabel', { name: player.nomeExibicao })">
      <header class="player-details-modal__hero">
        <div>
          <span class="eyebrow">{{ t('drafts.board.player') }}</span>
          <h2>{{ player.nomeExibicao }}</h2>
          <p>{{ eloSummary(player) }} · {{ routeLabel(player) }} · {{ player.status }}</p>
        </div>
        <span class="player-details-modal__avatar" aria-hidden="true">{{ player.nomeExibicao.charAt(0) }}</span>
        <button type="button" :aria-label="t('common.close')" @click="$emit('close')">×</button>
      </header>

      <div class="player-details-modal__grid">
        <article class="player-details-card player-details-card--highlight">
          <span class="eyebrow">{{ t('drafts.playerDetails.competitiveSummary') }}</span>
          <dl>
            <dt>{{ t('common.elo') }}</dt><dd>{{ eloSummary(player) }}</dd>
            <dt>{{ t('drafts.playerDetails.draftRoute') }}</dt><dd>{{ routeLabel(player) }}</dd>
            <dt>{{ t('drafts.playerDetails.state') }}</dt><dd>{{ player.estado }}{{ player.capitao ? ` · ${t('drafts.roles.captain')}` : '' }}</dd>
            <dt>{{ t('drafts.playerDetails.order') }}</dt><dd>{{ player.ordem }}</dd>
          </dl>
        </article>

        <article class="player-details-card">
          <span class="eyebrow">{{ t('drafts.playerDetails.identity') }}</span>
          <dl>
            <dt>{{ t('common.discord') }}</dt><dd>{{ player.discord || t('common.notInformed') }}</dd>
            <dt>{{ t('common.riotId') }}</dt><dd>{{ player.riotId || t('common.notInformed') }}</dd>
            <dt>{{ t('common.status') }}</dt><dd>{{ player.status }}</dd>
          </dl>
        </article>

        <article class="player-details-card player-details-card--wide">
          <span class="eyebrow">{{ t('drafts.playerDetails.routePreferences') }}</span>
          <div class="player-details-routes">
            <span v-for="preference in player.preferencias" :key="preference.rota" class="route-pill" :class="{ 'is-selected': preference.prioridade === 1, 'is-blocked': preference.naoJogoNemLascando }">
              <strong>{{ preference.prioridade }}. {{ preference.rota }}</strong>
              <small>{{ preference.naoJogoNemLascando ? t('drafts.playerDetails.doesNotPlay') : preference.prioridade === 1 ? t('drafts.playerDetails.priority') : t('drafts.playerDetails.preference') }}</small>
            </span>
          </div>
        </article>

        <article class="player-details-card player-details-card--wide">
          <span class="eyebrow">{{ t('drafts.playerDetails.linksAndAudit') }}</span>
          <dl>
            <dt>{{ t('common.opgg') }}</dt>
            <dd><a v-if="player.opGgUrl" :href="player.opGgUrl" target="_blank" rel="noreferrer">{{ t('drafts.playerDetails.openOpGg') }}</a><span v-else>{{ t('common.notInformed') }}</span></dd>
            <dt>{{ t('common.deepLol') }}</dt>
            <dd><a v-if="player.deepLolUrl" :href="player.deepLolUrl" target="_blank" rel="noreferrer">{{ t('drafts.playerDetails.openDeepLol') }}</a><span v-else>{{ t('common.notInformed') }}</span></dd>
            <dt>{{ t('drafts.playerDetails.createdAt') }}</dt><dd>{{ formatDate(player.dataCadastro) }}</dd>
            <dt>{{ t('drafts.playerDetails.updatedAt') }}</dt><dd>{{ formatDate(player.dataAtualizacao) }}</dd>
          </dl>
        </article>
      </div>
    </section>
  </div>
</template>
