import { describe, expect, it } from 'vitest'

import en from './locales/en.json'
import pt from './locales/pt.json'
import { i18n, setLocale } from './index'

const settingsComponents = import.meta.glob('../components/settings/*.vue', {
  eager: true,
  import: 'default',
  query: '?raw',
}) as Record<string, string>

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
        'updates.releases.2026_07_2.details.weekly-presence-scheduling.title',
      ),
    ).not.toContain('updates.')
  })

  it('provides equivalent and product-safe presence scheduling release content', () => {
    const detailIds = [
      'weekly-presence-scheduling',
      'publication-closing-times',
      'moderator-management',
      'window-recovery',
      'duplicate-draft-protection',
    ] as const

    for (const locale of [pt, en]) {
      const release = locale.updates.releases['2026_07_2']
      expect(release.title).toBeTruthy()
      expect(release.summary).toBeTruthy()
      expect(Object.keys(release.details)).toEqual(detailIds)
      for (const detail of Object.values(release.details)) {
        expect(detail.title).toBeTruthy()
        expect(detail.description).toBeTruthy()
        expect(detail.description).not.toMatch(/claims?|locks?|tokens?|endpoints?|\/api\//i)
      }
    }
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
      'settings.presenceSchedules.messageCodes.requestFailed',
      'settings.presenceSchedules.refreshError',
      'settings.presenceSchedules.actions.refreshing',
    ]

    for (const path of requiredPaths) {
      expect(leafPaths(pt)).toContain(path)
      expect(leafPaths(en)).toContain(path)
    }
    expect(pt.settings.presenceSchedules.weekdays.Sabado).toBe('Sábado')
    expect(pt.settings.presenceSchedules.fields.observation.label).toBe('Observação')
    expect(pt.settings.presenceSchedules.loading).toMatch(/…$/)
    expect(en.settings.presenceSchedules.loading).toMatch(/…$/)
    expect(pt.settings.presenceSchedules.history.loading).toMatch(/…$/)
    expect(en.settings.presenceSchedules.history.loading).toMatch(/…$/)
    expect(pt.settings.presenceSchedules.fields.name.placeholder).toMatch(/^Ex\.: .+…$/)
    expect(en.settings.presenceSchedules.fields.name.placeholder).toMatch(/^E\.g\. .+…$/)
    expect(pt.settings.presenceSchedules.fields.observation.placeholder).toMatch(/…$/)
    expect(en.settings.presenceSchedules.fields.observation.placeholder).toMatch(/…$/)
  })

  it('keeps presence schedule visible text and accessible names in i18n', () => {
    for (const [path, source] of Object.entries(settingsComponents)) {
      const template = source.match(/<template>([\s\S]*?)<\/template>/)?.[1] ?? ''
      expect(template, path).not.toMatch(/>\s*[A-Za-zÀ-ÿ][^<{]*</)
      expect(source, path).not.toMatch(/(?<![:@])\b(?:aria-label|placeholder|title)="[^"{]*[A-Za-zÀ-ÿ][^"]*"/)
      expect(source, path).not.toMatch(/toast\.(?:success|error)\(\s*['"`]/)
    }
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
