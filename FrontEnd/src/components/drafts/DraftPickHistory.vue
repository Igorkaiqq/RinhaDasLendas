<script setup lang="ts">
import { useI18n } from 'vue-i18n'

import type { DraftPick } from '@/types/draft'

defineProps<{ picks: DraftPick[] }>()
const { t } = useI18n()
</script>

<template>
  <section class="draft-panel">
    <h2>{{ t('drafts.pickHistory.title') }}</h2>
    <ol v-if="picks.length" class="draft-picks">
      <li v-for="pick in picks" :key="pick.sequencia">
        <strong>#{{ pick.sequencia }}</strong>
        <span>{{ pick.time === 'TimeA' ? t('drafts.teams.teamA') : t('drafts.teams.teamB') }}</span>
        <p>{{ pick.jogadorNome }}</p>
      </li>
    </ol>
    <p v-else class="empty-copy">{{ t('drafts.pickHistory.empty') }}</p>
  </section>
</template>
