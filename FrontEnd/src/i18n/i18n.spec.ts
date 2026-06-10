import { describe, expect, it } from 'vitest'

import { i18n, setLocale } from './index'

describe('i18n', () => {
  it('loads Portuguese translations by default', () => {
    setLocale('pt-BR')

    expect(i18n.global.t('navigation.players')).toBe('Jogadores')
  })

  it('loads English translations when locale changes', () => {
    setLocale('en-US')

    expect(i18n.global.t('navigation.players')).toBe('Players')
  })
})
