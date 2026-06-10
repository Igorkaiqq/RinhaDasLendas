<script setup lang="ts">
import { useI18n } from 'vue-i18n'

defineProps<{
  state: 'loading' | 'empty' | 'error'
  errors?: string[]
}>()

const skeletonItems = [1, 2, 3, 4]
const { t } = useI18n()
</script>

<template>
  <div v-if="state === 'loading'" class="player-grid player-grid--skeleton" :aria-label="t('playerStates.loading')">
    <span v-for="item in skeletonItems" :key="item" />
  </div>

  <section v-else class="players-empty">
    <p class="page-kicker">{{ state === 'error' ? t('playerStates.errorKicker') : t('playerStates.emptyKicker') }}</p>
    <h2>{{ state === 'error' ? t('playerStates.errorTitle') : t('playerStates.emptyTitle') }}</h2>
    <p v-if="state === 'error'">{{ errors?.[0] ?? t('playerStates.tryAgain') }}</p>
    <p v-else>{{ t('playerStates.emptyDescription') }}</p>
    <slot />
  </section>
</template>
