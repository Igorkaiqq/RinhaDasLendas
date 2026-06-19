import { createRouter, createWebHistory } from 'vue-router'

import { AppRouteNames, AppRoutes } from '@/constants/appRoutes'
import HomeView from '@/views/HomeView.vue'
import PlaceholderView from '@/views/PlaceholderView.vue'
import PlayersView from '@/views/PlayersView.vue'
import TeamsView from '@/views/TeamsView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: AppRoutes.Home,
      name: AppRouteNames.Home,
      component: HomeView,
      meta: { title: 'Painel' },
    },
    {
      path: AppRoutes.Players,
      name: AppRouteNames.Players,
      component: PlayersView,
      meta: { title: 'Jogadores' },
    },
    {
      path: AppRoutes.Teams,
      name: AppRouteNames.Teams,
      component: TeamsView,
      meta: { title: 'Times' },
    },
    {
      path: AppRoutes.Draft,
      name: AppRouteNames.Draft,
      component: PlaceholderView,
      meta: { title: 'Draft' },
    },
    {
      path: AppRoutes.Matches,
      name: AppRouteNames.Matches,
      component: PlaceholderView,
      meta: { title: 'Partidas' },
    },
    {
      path: AppRoutes.Stats,
      name: AppRouteNames.Stats,
      component: PlaceholderView,
      meta: { title: 'Estatisticas' },
    },
    {
      path: AppRoutes.Settings,
      name: AppRouteNames.Settings,
      component: PlaceholderView,
      meta: { title: 'Configuracoes' },
    },
  ],
})

export default router
