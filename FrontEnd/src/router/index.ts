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
      meta: { titleKey: 'routes.home.title', requiresAuth: true },
    },
    {
      path: AppRoutes.Players,
      name: AppRouteNames.Players,
      component: PlayersView,
      meta: { titleKey: 'routes.players.title', requiresAuth: true },
    },
    {
      path: AppRoutes.Teams,
      name: AppRouteNames.Teams,
      component: TeamsView,
      meta: { titleKey: 'routes.teams.title', requiresAuth: true },
    },
    {
      path: AppRoutes.Draft,
      name: AppRouteNames.Draft,
      component: DraftsView,
      meta: { titleKey: 'routes.draft.title', requiresAuth: true },
    },
    {
      path: AppRoutes.Matches,
      name: AppRouteNames.Matches,
      component: PlaceholderView,
      meta: { titleKey: 'routes.matches.title', requiresAuth: true },
    },
    {
      path: AppRoutes.Stats,
      name: AppRouteNames.Stats,
      component: PlaceholderView,
      meta: { titleKey: 'routes.stats.title', requiresAuth: true },
    },
    {
      path: AppRoutes.Settings,
      name: AppRouteNames.Settings,
      component: PlaceholderView,
      meta: { titleKey: 'routes.settings.title', requiresAuth: true },
    },
    {
      path: AppRoutes.Login,
      name: AppRouteNames.Login,
      component: LoginView,
      meta: { titleKey: 'routes.login.title', publicOnly: true },
    },
    {
      path: AppRoutes.Register,
      name: AppRouteNames.Register,
      component: RegisterView,
      meta: { titleKey: 'routes.register.title', publicOnly: true },
    },
    {
      path: AppRoutes.ForgotPassword,
      name: AppRouteNames.ForgotPassword,
      component: ForgotPasswordView,
      meta: { titleKey: 'routes.forgotPassword.title', publicOnly: true },
    },
    {
      path: AppRoutes.ResetPassword,
      name: AppRouteNames.ResetPassword,
      component: ResetPasswordView,
      meta: { titleKey: 'routes.resetPassword.title', publicOnly: true },
    },
    {
      path: AppRoutes.Profile,
      name: AppRouteNames.Profile,
      component: ProfileView,
      meta: { titleKey: 'routes.profile.title', requiresAuth: true },
    },
    {
      path: AppRoutes.UsersAdmin,
      name: AppRouteNames.UsersAdmin,
      component: UsersAdminView,
      meta: { titleKey: 'routes.usersAdmin.title', requiresAuth: true, permissions: [Permissions.CanViewUsers] },
    },
    {
      path: AppRoutes.Forbidden,
      name: AppRouteNames.Forbidden,
      component: ForbiddenView,
      meta: { titleKey: 'routes.forbidden.title', requiresAuth: true },
    },
  ],
})

router.beforeEach(authGuard)

export default router
