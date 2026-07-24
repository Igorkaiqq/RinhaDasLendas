import { describe, expect, it } from 'vitest'

import en from './locales/en.json'
import pt from './locales/pt.json'
import { i18n, setLocale } from './index'

function leafPaths(source: object, prefix = ''): string[] {
  return Object.entries(source).flatMap(([key, value]) => {
    const path = prefix ? `${prefix}.${key}` : key
    return value && typeof value === 'object' ? leafPaths(value, path) : [path]
  })
}

describe('i18n', () => {
  it('loads Portuguese translations by default', () => {
    setLocale('pt')

    expect(i18n.global.t('navigation.players')).toBe('Jogadores')
  })

  it('loads English translations when locale changes', () => {
    setLocale('en')

    expect(i18n.global.t('navigation.players')).toBe('Players')
  })

  it('keeps Portuguese and English translation leaf paths synchronized', () => {
    expect(leafPaths(pt).sort()).toEqual(leafPaths(en).sort())
    expect(
      i18n.global.t(
        'updates.releases.2026_07_1.details.discord-draft-deep-link.title',
      ),
    ).not.toContain('updates.')
  })

  it('provides the complete localized presence schedule interface', () => {
    const requiredPaths = [
      'settings.presenceSchedules.title',
      'settings.presenceSchedules.actions.viewHistory',
      'settings.presenceSchedules.fields.name.placeholder',
      'settings.presenceSchedules.weekdays.Sabado',
      'settings.presenceSchedules.statuses.occurrence.Bloqueada',
      'settings.presenceSchedules.confirm.archive.description',
      'settings.presenceSchedules.history.livePage',
      'settings.presenceSchedules.toasts.created',
      'settings.presenceSchedules.validation.closingAfterPublication',
      'settings.presenceSchedules.accessibility.scheduleList',
    ]

    for (const path of requiredPaths) {
      expect(leafPaths(pt)).toContain(path)
      expect(leafPaths(en)).toContain(path)
    }
    expect(pt.settings.presenceSchedules.weekdays.Sabado).toBe('Sábado')
    expect(pt.settings.presenceSchedules.fields.observation.label).toBe('Observação')
  })

  it('describes security hardening without exposing sensitive implementation details', () => {
    const descriptions = [
      pt.updates.releases['2026_07_1'].details['security-stability-hardening']
        .description,
      en.updates.releases['2026_07_1'].details['security-stability-hardening']
        .description,
    ]

    for (const description of descriptions) {
      expect(description).not.toMatch(/tokens?|endpoints?|https?:\/\/|\/api\//i)
    }
  })
})
