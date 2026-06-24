<script setup lang="ts">
import { AppRoutes } from '@/constants/appRoutes'
import { ref } from 'vue'

const notification = ref<string | null>(null)
let notificationTimer: ReturnType<typeof globalThis.setTimeout> | null = null

function showUnderConstruction() {
  notification.value = 'Tela em desenvolvimento.'

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
      <button type="button" aria-label="Fechar notificacao" @click="notification = null">x</button>
    </div>

    <header class="home-page__header">
      <div>
        <p class="home-page__eyebrow">Plataforma competitiva interna</p>
        <h1>Organize a próxima rinha com ritmo de campeonato.</h1>
        <p>
          Centralize jogadores, times, drafts e histórico da comunidade em uma experiência preparada para Discord, Riot e noites de confronto.
        </p>
      </div>
      <div class="home-page__signal" aria-label="Status da plataforma">
        <span>Live ops</span>
        <strong>Drafts e jogadores prontos</strong>
      </div>
    </header>

    <section class="home-page__grid" aria-label="Resumo do sistema">
      <article class="home-card home-card--highlight">
        <span class="home-card__icon" aria-hidden="true">DC</span>
        <div>
          <h2>Discord do grupo</h2>
          <p>Centralize jogadores, chamadas e organização das rinhas com dados consistentes para o grupo.</p>
        </div>
      </article>

      <RouterLink class="home-card home-card--link" :to="AppRoutes.Players">
        <span class="home-card__icon" aria-hidden="true">JG</span>
        <div>
          <h2>Jogadores</h2>
          <p>Cadastre participantes, elos e preferências de rota.</p>
        </div>
      </RouterLink>

      <button class="home-card home-card--link" type="button" @click="showUnderConstruction">
        <span class="home-card__icon" aria-hidden="true">DR</span>
        <div>
          <h2>Draft</h2>
          <p>Estrutura preparada para capitães, ordem de picks e times balanceados.</p>
        </div>
      </button>

      <button class="home-card home-card--link" type="button" @click="showUnderConstruction">
        <span class="home-card__icon" aria-hidden="true">MD</span>
        <div>
          <h2>Partidas</h2>
          <p>Base visual pronta para registrar jogos, placares e histórico.</p>
        </div>
      </button>
    </section>
  </section>
</template>
