<script setup lang="ts">
import type { DraftMontagemParticipante } from '@/types/draftMontagem'

defineProps<{ player: DraftMontagemParticipante | null }>()
defineEmits<{ close: [] }>()

function formatDate(value: string) {
  return new Date(value).toLocaleString('pt-BR')
}

function eloSummary(player: DraftMontagemParticipante) {
  return [player.elo, player.divisao].filter(Boolean).join(' ') || 'Nao informado'
}

function routeLabel(player: DraftMontagemParticipante) {
  return player.rotaContextual || player.preferencias.find((preference) => !preference.naoJogoNemLascando)?.rota || 'Sem rota'
}
</script>

<template>
  <div v-if="player" class="dialog-backdrop" role="presentation" @click.self="$emit('close')">
    <section class="player-details-modal" role="dialog" aria-modal="true" :aria-label="`Detalhes de ${player.nomeExibicao}`">
      <header class="player-details-modal__hero">
        <div>
          <span class="eyebrow">Jogador</span>
          <h2>{{ player.nomeExibicao }}</h2>
          <p>{{ eloSummary(player) }} · {{ routeLabel(player) }} · {{ player.status }}</p>
        </div>
        <span class="player-details-modal__avatar" aria-hidden="true">{{ player.nomeExibicao.charAt(0) }}</span>
        <button type="button" aria-label="Fechar" @click="$emit('close')">x</button>
      </header>

      <div class="player-details-modal__grid">
        <article class="player-details-card player-details-card--highlight">
          <span class="eyebrow">Resumo competitivo</span>
          <dl>
            <dt>Elo</dt><dd>{{ eloSummary(player) }}</dd>
            <dt>Rota no draft</dt><dd>{{ routeLabel(player) }}</dd>
            <dt>Estado</dt><dd>{{ player.estado }}{{ player.capitao ? ' · Capitao' : '' }}</dd>
            <dt>Ordem</dt><dd>{{ player.ordem }}</dd>
          </dl>
        </article>

        <article class="player-details-card">
          <span class="eyebrow">Identidade</span>
          <dl>
            <dt>Discord</dt><dd>{{ player.discord || 'Nao informado' }}</dd>
            <dt>Riot ID</dt><dd>{{ player.riotId || 'Nao informado' }}</dd>
            <dt>Status</dt><dd>{{ player.status }}</dd>
          </dl>
        </article>

        <article class="player-details-card player-details-card--wide">
          <span class="eyebrow">Preferencias de rota</span>
          <div class="player-details-routes">
            <span v-for="preference in player.preferencias" :key="preference.rota" class="route-pill" :class="{ 'is-selected': preference.prioridade === 1, 'is-blocked': preference.naoJogoNemLascando }">
              <strong>{{ preference.prioridade }}. {{ preference.rota }}</strong>
              <small>{{ preference.naoJogoNemLascando ? 'Nao joga' : preference.prioridade === 1 ? 'Prioritaria' : 'Preferencia' }}</small>
            </span>
          </div>
        </article>

        <article class="player-details-card player-details-card--wide">
          <span class="eyebrow">Links e auditoria</span>
          <dl>
            <dt>OP.GG</dt>
            <dd><a v-if="player.opGgUrl" :href="player.opGgUrl" target="_blank" rel="noreferrer">Abrir OP.GG</a><span v-else>Nao informado</span></dd>
            <dt>DeepLoL</dt>
            <dd><a v-if="player.deepLolUrl" :href="player.deepLolUrl" target="_blank" rel="noreferrer">Abrir DeepLoL</a><span v-else>Nao informado</span></dd>
            <dt>Cadastrado</dt><dd>{{ formatDate(player.dataCadastro) }}</dd>
            <dt>Atualizado</dt><dd>{{ formatDate(player.dataAtualizacao) }}</dd>
          </dl>
        </article>
      </div>
    </section>
  </div>
</template>
