import { describe, expect, it } from 'vitest'

import { SYSTEM_UPDATES } from '@/constants/systemUpdates'
import en from '@/i18n/locales/en.json'
import pt from '@/i18n/locales/pt.json'
import type { SystemUpdateRelease } from '@/types/systemUpdate'

import {
  filterSystemUpdates,
  getLatestSystemUpdate,
  getSystemUpdateValidationErrors,
  markLatestSystemUpdateSeen,
  readLastSeenSystemUpdate,
} from './systemUpdates'

function hasPath(source: object, path: string): boolean {
  let current: unknown = source
  for (const part of path.split('.')) {
    if (!current || typeof current !== 'object' || !(part in current))
      return false
    current = (current as Record<string, unknown>)[part]
  }
  return current !== undefined
}

describe('system updates', () => {
  it('returns the latest valid release from the registry', () => {
    expect(getLatestSystemUpdate()).toBe(SYSTEM_UPDATES[0])
    expect(getLatestSystemUpdate().version).toBe('2026.07.1')
    expect(() => getLatestSystemUpdate([])).toThrow(
      'System update registry cannot be empty',
    )
  })

  it('validates the complete localized registry', () => {
    const errors = getSystemUpdateValidationErrors(
      SYSTEM_UPDATES,
      (key) => hasPath(pt, key) && hasPath(en, key),
    )

    expect(errors).toEqual([])
  })

  it('reports malformed registry data and unknown links', () => {
    const invalid = [
      {
        ...SYSTEM_UPDATES[0],
        publishedAt: '2026-02-30',
        categories: [],
        areas: [],
        titleKey: 'missing.title',
        details: [
          { ...SYSTEM_UPDATES[0].details[0], link: '/unknown' },
          SYSTEM_UPDATES[0].details[0],
        ],
      },
      {
        ...SYSTEM_UPDATES[0],
        version: '2026-7-1',
        publishedAt: '2026-08-01',
        featured: true,
        details: [],
      },
    ] as unknown as readonly SystemUpdateRelease[]

    const errors = getSystemUpdateValidationErrors(
      invalid,
      (key) => !key.startsWith('missing'),
    )

    expect(errors).toEqual(
      expect.arrayContaining([
        'Exactly one release must be featured',
        'Duplicate release id: drafts-discord-reliability',
        'Invalid version: 2026-7-1',
        'Invalid date: 2026-02-30',
        'Releases must be newest first',
        'Missing categories: drafts-discord-reliability',
        'Missing areas: drafts-discord-reliability',
        'Missing details: drafts-discord-reliability',
        'Missing translation: missing.title',
        'Duplicate detail id: drafts-discord-reliability:discord-draft-deep-link',
        'Unknown internal link: /unknown',
      ]),
    )
  })

  it('filters localized content without case or accent distinctions and combines category', () => {
    const messages = pt as Record<string, unknown>
    const translate = (key: string) => {
      let current: unknown = messages
      for (const part of key.split('.'))
        current = (current as Record<string, unknown>)[part]
      return String(current)
    }

    expect(
      filterSystemUpdates(
        SYSTEM_UPDATES,
        'seguranca',
        'security',
        translate,
      ).map(({ version }) => version),
    ).toContain('2026.07.1')
    expect(
      filterSystemUpdates(
        SYSTEM_UPDATES,
        'DISCORD',
        'improvement',
        translate,
      )[0]?.version,
    ).toBe('2026.07.1')
    expect(filterSystemUpdates(SYSTEM_UPDATES, '', 'all', translate)).toEqual(
      SYSTEM_UPDATES,
    )
    expect(
      filterSystemUpdates(
        SYSTEM_UPDATES,
        'termo inexistente',
        'all',
        translate,
      ),
    ).toEqual([])
  })

  it('reads and writes the latest seen version', () => {
    const values = new Map<string, string>()
    const storage = {
      getItem: (key: string) => values.get(key) ?? null,
      setItem: (key: string, value: string) => values.set(key, value),
    }

    expect(readLastSeenSystemUpdate(storage)).toBeNull()
    expect(markLatestSystemUpdateSeen('2026.07.1', storage)).toBe('2026.07.1')
    expect(readLastSeenSystemUpdate(storage)).toBe('2026.07.1')
  })

  it('uses an in-memory fallback when local storage throws or is unavailable', () => {
    const blockedStorage = {
      getItem: () => {
        throw new Error('blocked')
      },
      setItem: () => {
        throw new Error('blocked')
      },
    }

    expect(markLatestSystemUpdateSeen('2026.07.2', blockedStorage)).toBe(
      '2026.07.2',
    )
    expect(readLastSeenSystemUpdate(blockedStorage)).toBe('2026.07.2')
    expect(markLatestSystemUpdateSeen('2026.07.3', undefined)).toBe('2026.07.3')
    expect(readLastSeenSystemUpdate(undefined)).toBe('2026.07.3')
  })
})
