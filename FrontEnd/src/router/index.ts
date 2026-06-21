import { createRouter, createWebHistory } from 'vue-router'

import { AppRouteNames, AppRoutes } from '@/constants/appRoutes'
import { Permissions } from '@/constants/permissions'
import HomeView from '@/views/HomeView.vue'
import PlaceholderView from '@/views/PlaceholderView.vue'
import PlayersView from '@/views/PlayersView.vue'
import TeamsView from '@/views/TeamsView.vue'
import DraftsView from '@/views/DraftsView.vue'
import LoginView from '@/views/LoginView.vue'
import RegisterView from '@/views/RegisterView.vue'
import ForgotPasswordView from '@/views/ForgotPasswordView.vue'
import ResetPasswordView from '@/views/ResetPasswordView.vue'
import ProfileView from '@/views/ProfileView.vue'
import UsersAdminView from '@/views/UsersAdminView.vue'
import ForbiddenView from '@/views/ForbiddenView.vue'
import { authGuard } from './guards'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: AppRoutes.Home,
      name: AppRouteNames.Home,
      component: HomeView,
      meta: { title: 'Painel', requiresAuth: true },
    },
    {
      path: AppRoutes.Players,
      name: AppRouteNames.Players,
      component: PlayersView,
      meta: { title: 'Jogadores', requiresAuth: true },
    },
    {
      path: AppRoutes.Teams,
      name: AppRouteNames.Teams,
      component: TeamsView,
      meta: { title: 'Times', requiresAuth: true },
    },
    {
      path: AppRoutes.Draft,
      name: AppRouteNames.Draft,
      component: DraftsView,
      meta: { title: 'Draft', requiresAuth: true },
    },
    {
      path: AppRoutes.Matches,
      name: AppRouteNames.Matches,
      component: PlaceholderView,
      meta: { title: 'Partidas', requiresAuth: true },
    },
    {
      path: AppRoutes.Stats,
      name: AppRouteNames.Stats,
      component: PlaceholderView,
      meta: { title: 'Estatisticas', requiresAuth: true },
    },
    {
      path: AppRoutes.Settings,
      name: AppRouteNames.Settings,
      component: PlaceholderView,
      meta: { title: 'Configuracoes', requiresAuth: true },
    },
    {
      path: AppRoutes.Login,
      name: AppRouteNames.Login,
      component: LoginView,
      meta: { title: 'Entrar', publicOnly: true },
    },
    {
      path: AppRoutes.Register,
      name: AppRouteNames.Register,
      component: RegisterView,
      meta: { title: 'Cadastro', publicOnly: true },
    },
    {
      path: AppRoutes.ForgotPassword,
      name: AppRouteNames.ForgotPassword,
      component: ForgotPasswordView,
      meta: { title: 'Esqueci minha senha', publicOnly: true },
    },
    {
      path: AppRoutes.ResetPassword,
      name: AppRouteNames.ResetPassword,
      component: ResetPasswordView,
      meta: { title: 'Redefinir senha', publicOnly: true },
    },
    {
      path: AppRoutes.Profile,
      name: AppRouteNames.Profile,
      component: ProfileView,
      meta: { title: 'Perfil', requiresAuth: true },
    },
    {
      path: AppRoutes.UsersAdmin,
      name: AppRouteNames.UsersAdmin,
      component: UsersAdminView,
      meta: { title: 'Usuários', requiresAuth: true, permissions: [Permissions.CanViewUsers] },
    },
    {
      path: AppRoutes.Forbidden,
      name: AppRouteNames.Forbidden,
      component: ForbiddenView,
      meta: { title: 'Acesso negado', requiresAuth: true },
    },
  ],
})

router.beforeEach(authGuard)

export default router
