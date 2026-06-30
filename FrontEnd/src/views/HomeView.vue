<script setup lang="ts">
import { AppRoutes } from '@/constants/appRoutes'
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'

import brandLogo from '@/assets/rinhadaslendas_brand_logo.png'

const notification = ref<string | null>(null)
const { t } = useI18n()
let notificationTimer: ReturnType<typeof globalThis.setTimeout> | null = null

const workflowSteps = ['players', 'presence', 'draft', 'history'] as const
const roadmapItems = ['discord', 'riot', 'stats'] as const

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
      <button type="button" :aria-label="t('common.closeNotification')" @click="notification = null">×</button>
    </div>

    <header class="home-page__header">
      <div>
        <img class="home-page__logo" :src="brandLogo" :alt="t('app.name')" />
        <p class="home-page__eyebrow">{{ t('home.hero.eyebrow') }}</p>
        <h1>{{ t('home.hero.title') }}</h1>
        <p>
          {{ t('home.hero.description') }}
        </p>
        <div class="home-page__actions" :aria-label="t('home.hero.actionsLabel')">
          <RouterLink class="home-action home-action--primary" :to="AppRoutes.Draft">
            {{ t('home.hero.primaryAction') }}
          </RouterLink>
          <RouterLink class="home-action home-action--secondary" :to="AppRoutes.Players">
            {{ t('home.hero.secondaryAction') }}
          </RouterLink>
        </div>
      </div>
      <div class="home-page__signal" :aria-label="t('home.platformStatusLabel')">
        <span>{{ t('home.signal.label') }}</span>
        <strong>{{ t('home.signal.status') }}</strong>
        <p>{{ t('home.signal.description') }}</p>
      </div>
    </header>

    <section class="home-section home-section--split" :aria-label="t('home.about.label')">
      <div class="home-section__intro">
        <p class="home-page__eyebrow">{{ t('home.about.eyebrow') }}</p>
        <h2>{{ t('home.about.title') }}</h2>
      </div>
      <p>{{ t('home.about.description') }}</p>
    </section>

    <section class="home-workflow" :aria-label="t('home.workflow.label')">
      <div class="home-section__intro">
        <p class="home-page__eyebrow">{{ t('home.workflow.eyebrow') }}</p>
        <h2>{{ t('home.workflow.title') }}</h2>
      </div>
      <ol class="home-workflow__list">
        <li v-for="(step, index) in workflowSteps" :key="step" class="home-workflow__item">
          <span class="home-workflow__number">{{ String(index + 1).padStart(2, '0') }}</span>
          <div>
            <h3>{{ t(`home.workflow.steps.${step}.title`) }}</h3>
            <p>{{ t(`home.workflow.steps.${step}.description`) }}</p>
          </div>
        </li>
      </ol>
    </section>

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

    <section class="home-roadmap" :aria-label="t('home.roadmap.label')">
      <div class="home-section__intro">
        <p class="home-page__eyebrow">{{ t('home.roadmap.eyebrow') }}</p>
        <h2>{{ t('home.roadmap.title') }}</h2>
      </div>
      <div class="home-roadmap__grid">
        <article v-for="item in roadmapItems" :key="item" class="home-roadmap__item">
          <span aria-hidden="true">{{ t(`home.roadmap.items.${item}.tag`) }}</span>
          <h3>{{ t(`home.roadmap.items.${item}.title`) }}</h3>
          <p>{{ t(`home.roadmap.items.${item}.description`) }}</p>
        </article>
      </div>
    </section>
  </section>
</template>
