<script setup lang="ts">
import { useI18n } from 'vue-i18n'

import TeamCard from '@/components/teams/TeamCard.vue'
import type { Team } from '@/types/team'

const { t } = useI18n()

defineProps<{
  teams: Team[]
  loading: boolean
  errors: string[]
  canManage: boolean
}>()

defineEmits<{
  create: []
  edit: [team: Team]
  inactivate: [team: Team]
  reactivate: [team: Team]
}>()
</script>

<template>
  <section v-if="loading" class="player-grid player-grid--skeleton" :aria-label="t('teams.states.loading')">
    <span v-for="index in 4" :key="index" />
  </section>
  <section v-else-if="errors.length" class="players-empty">
    <p class="page-kicker">{{ t('playerStates.errorKicker') }}</p>
    <h2>{{ t('teams.states.errorTitle') }}</h2>
    <p v-for="error in errors" :key="error">{{ error }}</p>
  </section>
  <section v-else-if="teams.length === 0" class="players-empty">
    <p class="page-kicker">{{ t('playerStates.emptyKicker') }}</p>
    <h2>{{ t('teams.emptyTitle') }}</h2>
    <p>{{ t('teams.emptyDescription') }}</p>
    <button v-if="canManage" type="button" @click="$emit('create')">{{ t('teams.create') }}</button>
  </section>

  <section v-else class="player-grid" :aria-label="t('teams.listLabel')">
    <TeamCard
      v-for="team in teams"
      :key="team.id"
      :team="team"
      :can-manage="canManage"
      @edit="$emit('edit', $event)"
      @inactivate="$emit('inactivate', $event)"
      @reactivate="$emit('reactivate', $event)"
    />
  </section>
</template>
