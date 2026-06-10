<script setup lang="ts">
defineProps<{
  state: 'loading' | 'empty' | 'error'
  errors?: string[]
}>()

const skeletonItems = [1, 2, 3, 4]
</script>

<template>
  <div v-if="state === 'loading'" class="player-grid player-grid--skeleton" aria-label="Carregando jogadores">
    <span v-for="item in skeletonItems" :key="item" />
  </div>

  <section v-else class="players-empty">
    <p class="page-kicker">{{ state === 'error' ? 'Erro' : 'Lista vazia' }}</p>
    <h2>{{ state === 'error' ? 'Nao foi possivel carregar jogadores' : 'Nenhum jogador encontrado' }}</h2>
    <p v-if="state === 'error'">{{ errors?.[0] ?? 'Tente novamente em instantes.' }}</p>
    <p v-else>Cadastre o primeiro participante ou ajuste os filtros para visualizar a base da rinha.</p>
    <slot />
  </section>
</template>
