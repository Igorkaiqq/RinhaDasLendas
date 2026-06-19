<script setup lang="ts">
import TeamCard from '@/components/teams/TeamCard.vue'
import type { Team } from '@/types/team'

defineProps<{
  teams: Team[]
  loading: boolean
  errors: string[]
}>()

defineEmits<{
  create: []
  edit: [team: Team]
  inactivate: [team: Team]
  reactivate: [team: Team]
}>()
</script>

<template>
  <section v-if="loading" class="player-grid player-grid--skeleton" aria-label="Carregando times">
    <span v-for="index in 4" :key="index" />
  </section>
  <section v-else-if="errors.length" class="players-empty">
    <p class="page-kicker">Erro</p>
    <h2>Nao foi possivel carregar times</h2>
    <p v-for="error in errors" :key="error">{{ error }}</p>
  </section>
  <section v-else-if="teams.length === 0" class="players-empty">
    <p class="page-kicker">Lista vazia</p>
    <h2>Nenhum time encontrado</h2>
    <p>Cadastre o primeiro time ou ajuste os filtros para visualizar a base da rinha.</p>
    <button type="button" @click="$emit('create')">Cadastrar Time</button>
  </section>

  <section v-else class="player-grid" aria-label="Lista de times">
    <TeamCard
      v-for="team in teams"
      :key="team.id"
      :team="team"
      @edit="$emit('edit', $event)"
      @inactivate="$emit('inactivate', $event)"
      @reactivate="$emit('reactivate', $event)"
    />
  </section>
</template>
