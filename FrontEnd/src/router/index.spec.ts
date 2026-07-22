// @vitest-environment happy-dom
import { describe, expect, it } from 'vitest'

import { AppRouteNames } from '@/constants/appRoutes'
import router from './index'

describe('updates route', () => {
  it('requires authentication without role restrictions and uses a localized title', () => {
    const route = router
      .getRoutes()
      .find((candidate) => candidate.name === AppRouteNames.Updates)

    expect(route?.path).toBe('/atualizacoes')
    expect(route?.meta).toEqual({
      requiresAuth: true,
      titleKey: 'routes.updates.title',
    })
  })
})
