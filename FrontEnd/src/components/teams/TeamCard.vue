<script setup lang="ts">
import TeamStatusBadge from '@/components/teams/TeamStatusBadge.vue'
import { TeamStatus } from '@/constants/teamStatus'
import type { Team } from '@/types/team'

defineProps<{
  team: Team
}>()

defineEmits<{
  edit: [team: Team]
  inactivate: [team: Team]
  reactivate: [team: Team]
}>()

function initials(name: string) {
  return name
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0])
    .join('')
    .toUpperCase()
}
</script>

<template>
  <article class="player-card">
    <div class="player-card__topline">
      <div class="player-card__avatar" aria-hidden="true">{{ initials(team.tag) }}</div>
      <div class="player-card__badges">
        <span class="elo-badge">{{ team.tag }}</span>
        <TeamStatusBadge :status="team.status" />
      </div>
    </div>

    <div class="player-card__identity">
      <h2>{{ team.nome }}</h2>
      <p>{{ team.observacoes || 'Sem observacoes' }}</p>
    </div>

    <div class="player-card__meta">
      <span>Capitao</span>
      <strong>{{ team.capitao?.nomeExibicao || 'Nao definido' }}</strong>
      <span>Jogadores</span>
      <strong>{{ team.quantidadeJogadores }}/5</strong>
    </div>

    <div class="route-preferences-panel" aria-label="Membros do time">
      <span v-for="member in team.membros" :key="member.jogadorId" class="route-pill">
        {{ member.capitao ? '*' : '' }}{{ member.nomeExibicao }}
      </span>
    </div>

    <div class="player-card__actions">
      <button type="button" @click="$emit('edit', team)">Editar</button>
      <button v-if="team.status === TeamStatus.Active" type="button" class="button-danger" @click="$emit('inactivate', team)">
        Inativar
      </button>
      <button v-else type="button" @click="$emit('reactivate', team)">Reativar</button>
    </div>
  </article>
</template>
