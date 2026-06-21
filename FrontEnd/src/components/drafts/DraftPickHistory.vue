<script setup lang="ts">
import type { DraftPick } from '@/types/draft'

defineProps<{ picks: DraftPick[] }>()
</script>

<template>
  <section class="draft-panel">
    <h2>Ordem de picks</h2>
    <ol v-if="picks.length" class="draft-picks">
      <li v-for="pick in picks" :key="pick.sequencia">
        <strong>#{{ pick.sequencia }}</strong>
        <span>{{ pick.time === 'TimeA' ? 'Time A' : 'Time B' }}</span>
        <p>{{ pick.jogadorNome }}</p>
      </li>
    </ol>
    <p v-else class="empty-copy">Nenhuma escolha registrada ainda.</p>
  </section>
</template>
