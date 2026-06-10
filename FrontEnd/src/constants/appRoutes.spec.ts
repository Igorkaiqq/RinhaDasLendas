import { describe, expect, it } from 'vitest'

import { AppRouteNames, AppRoutes } from './appRoutes'

describe('app route constants', () => {
  it('defines stable route paths for primary navigation', () => {
    expect(AppRoutes.Home).toBe('/')
    expect(AppRoutes.Players).toBe('/jogadores')
    expect(AppRoutes.Settings).toBe('/configuracoes')
  })

  it('defines stable route names for router matching', () => {
    expect(AppRouteNames.Home).toBe('home')
    expect(AppRouteNames.Players).toBe('players')
    expect(AppRouteNames.Settings).toBe('settings')
  })

  it('keeps route paths absolute', () => {
    expect(Object.values(AppRoutes).every((path) => path.startsWith('/'))).toBe(true)
  })
})
