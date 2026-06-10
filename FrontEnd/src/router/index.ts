import { createRouter, createWebHistory } from 'vue-router'

import HomeView from '@/views/HomeView.vue'
import PlaceholderView from '@/views/PlaceholderView.vue'
import PlayersView from '@/views/PlayersView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      component: HomeView,
      meta: { title: 'Painel' },
    },
    {
      path: '/jogadores',
      name: 'players',
      component: PlayersView,
      meta: { title: 'Jogadores' },
    },
    {
      path: '/times',
      name: 'teams',
      component: PlaceholderView,
      meta: { title: 'Times' },
    },
    {
      path: '/draft',
      name: 'draft',
      component: PlaceholderView,
      meta: { title: 'Draft' },
    },
    {
      path: '/partidas',
      name: 'matches',
      component: PlaceholderView,
      meta: { title: 'Partidas' },
    },
    {
      path: '/estatisticas',
      name: 'stats',
      component: PlaceholderView,
      meta: { title: 'Estatisticas' },
    },
    {
      path: '/configuracoes',
      name: 'settings',
      component: PlaceholderView,
      meta: { title: 'Configuracoes' },
    },
  ],
})

export default router
