export const AppRouteNames = {
  Home: 'home',
  Players: 'players',
  Teams: 'teams',
  Draft: 'draft',
  Matches: 'matches',
  Stats: 'stats',
  Settings: 'settings',
} as const

export const AppRoutes = {
  Home: '/',
  Players: '/jogadores',
  Teams: '/times',
  Draft: '/draft',
  Matches: '/partidas',
  Stats: '/estatisticas',
  Settings: '/configuracoes',
} as const

export type AppRouteName = (typeof AppRouteNames)[keyof typeof AppRouteNames]
export type AppRoutePath = (typeof AppRoutes)[keyof typeof AppRoutes]
