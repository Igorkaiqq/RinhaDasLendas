<script setup lang="ts">
import type { Draft, DraftPlayer } from '@/types/draft'

defineProps<{ draft: Draft | null; picking: boolean }>()

defineEmits<{
  pick: [player: DraftPlayer]
}>()
</script>

<template>
  <section v-if="draft" class="draft-board" aria-label="Tabuleiro do draft">
    <article class="draft-team draft-team--a">
      <span class="eyebrow">Time A</span>
      <h2>{{ draft.capitaoTimeA.nomeExibicao }}</h2>
      <p>Capitao</p>
      <ul>
        <li v-for="player in draft.timeA" :key="player.jogadorId">
          {{ player.nomeExibicao }} <span v-if="player.capitao">C</span>
        </li>
      </ul>
    </article>

    <article class="draft-available">
      <span class="eyebrow">Proximo pick</span>
      <h2>{{ draft.proximoTime === 'TimeA' ? 'Time A' : draft.proximoTime === 'TimeB' ? 'Time B' : 'Finalizado' }}</h2>
      <p>Capitaes: {{ draft.criterioCapitaes }} · Primeiro pick: {{ draft.criterioPrimeiroPick }}</p>
      <div class="draft-player-grid">
        <button
          v-for="player in draft.disponiveis"
          :key="player.id"
          type="button"
          :disabled="picking || draft.status !== 'Aberto'"
          @click="$emit('pick', player)"
        >
          {{ player.nomeExibicao }}
        </button>
      </div>
      <p v-if="!draft.disponiveis.length" class="empty-copy">Sem jogadores disponiveis.</p>
    </article>

    <article class="draft-team draft-team--b">
      <span class="eyebrow">Time B</span>
      <h2>{{ draft.capitaoTimeB.nomeExibicao }}</h2>
      <p>Capitao</p>
      <ul>
        <li v-for="player in draft.timeB" :key="player.jogadorId">
          {{ player.nomeExibicao }} <span v-if="player.capitao">C</span>
        </li>
      </ul>
    </article>
  </section>
  <section v-else class="draft-empty-card">
    <h2>Nenhum draft selecionado</h2>
    <p>Crie ou selecione um draft para visualizar capitães, picks e jogadores disponiveis.</p>
  </section>
</template>
