export const AppRouteNames = {
  Home: 'home',
  Players: 'players',
  Teams: 'teams',
  Draft: 'draft',
  Matches: 'matches',
  Stats: 'stats',
  Settings: 'settings',
  Login: 'login',
  Register: 'register',
  ForgotPassword: 'forgot-password',
  ResetPassword: 'reset-password',
  Profile: 'profile',
  UsersAdmin: 'users-admin',
  Forbidden: 'forbidden',
} as const

export const AppRoutes = {
  Home: '/',
  Players: '/jogadores',
  Teams: '/times',
  Draft: '/drafts',
  Matches: '/partidas',
  Stats: '/estatisticas',
  Settings: '/configuracoes',
  Login: '/login',
  Register: '/cadastro',
  ForgotPassword: '/esqueci-senha',
  ResetPassword: '/redefinir-senha',
  Profile: '/perfil',
  UsersAdmin: '/usuarios',
  Forbidden: '/403',
} as const

export type AppRouteName = (typeof AppRouteNames)[keyof typeof AppRouteNames]
export type AppRoutePath = (typeof AppRoutes)[keyof typeof AppRoutes]
