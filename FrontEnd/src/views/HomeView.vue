<script setup lang="ts">
import { AppRoutes } from '@/constants/appRoutes'
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'

const notification = ref<string | null>(null)
const { t } = useI18n()
let notificationTimer: ReturnType<typeof globalThis.setTimeout> | null = null

function showUnderConstruction() {
  notification.value = t('home.notifications.underConstruction')

  if (notificationTimer) {
    globalThis.clearTimeout(notificationTimer)
  }

  notificationTimer = globalThis.setTimeout(() => {
    notification.value = null
    notificationTimer = null
  }, 3200)
}
</script>

<template>
  <section class="home-page">
    <div v-if="notification" class="app-toast app-toast--info" role="status" aria-live="polite">
      <span class="app-toast__indicator" aria-hidden="true" />
      <p>{{ notification }}</p>
      <button type="button" :aria-label="t('common.closeNotification')" @click="notification = null">x</button>
    </div>

    <header class="home-page__header">
      <div>
        <p class="home-page__eyebrow">{{ t('home.hero.eyebrow') }}</p>
        <h1>{{ t('home.hero.title') }}</h1>
        <p>
          {{ t('home.hero.description') }}
        </p>
      </div>
      <div class="home-page__signal" :aria-label="t('home.platformStatusLabel')">
        <span>{{ t('home.signal.label') }}</span>
        <strong>{{ t('home.signal.status') }}</strong>
      </div>
    </header>

    <section class="home-page__grid" :aria-label="t('home.systemSummaryLabel')">
      <article class="home-card home-card--highlight">
        <span class="home-card__icon" aria-hidden="true">DC</span>
        <div>
          <h2>{{ t('home.cards.discord.title') }}</h2>
          <p>{{ t('home.cards.discord.description') }}</p>
        </div>
      </article>

      <RouterLink class="home-card home-card--link" :to="AppRoutes.Players">
        <span class="home-card__icon" aria-hidden="true">JG</span>
        <div>
          <h2>{{ t('home.cards.players.title') }}</h2>
          <p>{{ t('home.cards.players.description') }}</p>
        </div>
      </RouterLink>

      <button class="home-card home-card--link" type="button" @click="showUnderConstruction">
        <span class="home-card__icon" aria-hidden="true">DR</span>
        <div>
          <h2>{{ t('home.cards.draft.title') }}</h2>
          <p>{{ t('home.cards.draft.description') }}</p>
        </div>
      </button>

      <button class="home-card home-card--link" type="button" @click="showUnderConstruction">
        <span class="home-card__icon" aria-hidden="true">MD</span>
        <div>
          <h2>{{ t('home.cards.matches.title') }}</h2>
          <p>{{ t('home.cards.matches.description') }}</p>
        </div>
      </button>
    </section>
  </section>
</template>
