// @vitest-environment happy-dom
import { mount } from '@vue/test-utils'
import { createMemoryHistory, createRouter } from 'vue-router'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { AppRouteNames, AppRoutes } from '@/constants/appRoutes'
import { i18n, setLocale } from '@/i18n'
import {
  LAST_SEEN_SYSTEM_UPDATE_KEY,
  markLatestSystemUpdateSeen,
} from '@/services/systemUpdates'
import type { SidebarNavigationItem } from '@/types/layout'
import AppShell from './AppShell.vue'
import SidebarNav from './SidebarNav.vue'

vi.mock('@/services/authState', async () => {
  const { ref } = await import('vue')

  return {
    useAuthState: () => ({
      hasPermission: () => false,
      isAuthenticated: ref(true),
      user: ref({
        id: 'user-id',
        nome: 'Jogador',
        email: 'jogador@example.com',
        roles: ['Jogador'],
        jogadorId: 'player-id',
      }),
    }),
  }
})

describe('AppShell update badge', () => {
  beforeEach(() => {
    localStorage.clear()
    setLocale('pt')
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  function createAppRouter() {
    type RouteKey = keyof typeof AppRoutes

    return createRouter({
      history: createMemoryHistory(),
      routes: (Object.keys(AppRoutes) as RouteKey[]).map((key) => ({
        path: AppRoutes[key],
        name: AppRouteNames[key],
        component: { template: '<div />' },
      })),
    })
  }

  async function mountShell() {
    const router = createAppRouter()
    await router.push(AppRoutes.Home)
    await router.isReady()

    const wrapper = mount(AppShell, {
      global: {
        plugins: [i18n, router],
      },
    })
    const updateItem = () =>
      wrapper
        .getComponent(SidebarNav)
        .props('items')
        .find((item: SidebarNavigationItem) => item.id === 'updates')

    return { router, updateItem, wrapper }
  }

  it('shows the badge to any authenticated user and removes it reactively after opening updates', async () => {
    const { router, updateItem, wrapper } = await mountShell()
    const mountedShell = wrapper.vm

    expect(updateItem()).toMatchObject({
      routeName: AppRouteNames.Updates,
      status: 'available',
      badge: 'new',
    })

    await router.push(AppRoutes.Updates)

    expect(localStorage.getItem(LAST_SEEN_SYSTEM_UPDATE_KEY)).toBe('2026.07.1')
    expect(updateItem()?.badge).toBeUndefined()
    expect(wrapper.vm).toBe(mountedShell)
  })

  it('removes the badge in the current session when persistent storage fails', async () => {
    markLatestSystemUpdateSeen('2026.06.7')
    const storageWrite = vi
      .spyOn(localStorage, 'setItem')
      .mockImplementation(() => {
        throw new Error('storage unavailable')
      })
    const { router, updateItem, wrapper } = await mountShell()

    expect(updateItem()?.badge).toBe('new')

    await router.push(AppRoutes.Updates)

    expect(storageWrite).toHaveBeenCalledWith(
      LAST_SEEN_SYSTEM_UPDATE_KEY,
      '2026.07.1',
    )
    expect(updateItem()?.badge).toBeUndefined()
    expect(wrapper.exists()).toBe(true)
  })
})
