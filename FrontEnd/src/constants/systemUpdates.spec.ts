import { describe, expect, it } from 'vitest'

import { AppRoutes } from './appRoutes'
import { SYSTEM_UPDATES } from './systemUpdates'

const releaseIds = [
  'administrative-draft-archiving',
  'clearer-draft-operation',
  'presence-schedule-weekday-selection-fix',
  'presence-scheduling-2026-07',
  'drafts-discord-reliability',
  'security-deploy-identity',
  'discord-presence',
  'realtime-draft',
  'users-auth-rbac',
  'visual-draft',
  'players-teams',
  'platform-foundation',
]

const latestDetailIds = [
  'operational-hierarchy',
  'presence-roster',
  'stage-accessibility-clarity',
  'responsive-mobile-operation',
]

const presenceSchedulingDetailIds = [
  'weekly-presence-scheduling',
  'publication-closing-times',
  'moderator-management',
  'window-recovery',
  'duplicate-draft-protection',
]

const previousDetailIds = [
  'discord-draft-deep-link',
  'invalid-draft-link-feedback',
  'contextual-admin-confirmations',
  'discord-publication-statuses',
  'individual-publication-recovery',
  'duplicate-message-protection',
  'realtime-presence',
  'consistent-presence-operations',
  'eligible-player-search',
  'admin-action-audit',
  'clear-bot-messages',
  'discord-permission-diagnostics',
  'independent-presence-call',
  'resilient-publication-queue',
  'security-stability-hardening',
]

describe('system update registry', () => {
  it('contains the twelve stable releases in descending chronological order', () => {
    expect(SYSTEM_UPDATES.map(({ id }) => id)).toEqual(releaseIds)
    expect(SYSTEM_UPDATES.map(({ version }) => version)).toEqual([
      '2026.07.5',
      '2026.07.4',
      '2026.07.3',
      '2026.07.2',
      '2026.07.1',
      '2026.06.7',
      '2026.06.6',
      '2026.06.5',
      '2026.06.4',
      '2026.06.3',
      '2026.06.2',
      '2026.06.1',
    ])
    expect(SYSTEM_UPDATES.map(({ publishedAt }) => publishedAt)).toEqual([
      '2026-07-26',
      '2026-07-26',
      '2026-07-25',
      '2026-07-23',
      '2026-07-22',
      '2026-06-30',
      '2026-06-29',
      '2026-06-24',
      '2026-06-21',
      '2026-06-20',
      '2026-06-19',
      '2026-06-10',
    ])
  })

  it('publishes draft archiving as the only featured latest release', () => {
    expect(SYSTEM_UPDATES[0]).toMatchObject({
      id: 'administrative-draft-archiving',
      version: '2026.07.5',
      publishedAt: '2026-07-26',
      featured: true,
      categories: ['feature'],
      areas: ['drafts'],
    })
    expect(SYSTEM_UPDATES[0].details.map(({ id }) => id)).toEqual(['archive-and-restore'])
    expect(SYSTEM_UPDATES[0].details[0]).toMatchObject({ category: 'feature', link: AppRoutes.Draft })
    expect(SYSTEM_UPDATES.filter(({ featured }) => featured)).toHaveLength(1)
    expect(SYSTEM_UPDATES[1].details.map(({ id }) => id)).toEqual(latestDetailIds)
  })

  it('preserves the selected weekday feedback fix without featuring it', () => {
    expect(SYSTEM_UPDATES[2]).toEqual({
      id: 'presence-schedule-weekday-selection-fix',
      version: '2026.07.3',
      publishedAt: '2026-07-25',
      featured: false,
      categories: ['fix'],
      areas: ['drafts'],
      titleKey: 'updates.releases.2026_07_3.title',
      summaryKey: 'updates.releases.2026_07_3.summary',
      details: [
        {
          id: 'selected-weekday-feedback',
          category: 'fix',
          link: AppRoutes.Settings,
          titleKey:
            'updates.releases.2026_07_3.details.selected-weekday-feedback.title',
          descriptionKey:
            'updates.releases.2026_07_3.details.selected-weekday-feedback.description',
        },
      ],
    })
  })

  it('freezes the complete presence scheduling release metadata without featuring it', () => {
    const previous = SYSTEM_UPDATES.find(({ version }) => version === '2026.07.2')

    expect(previous).toEqual({
      id: 'presence-scheduling-2026-07',
      version: '2026.07.2',
      publishedAt: '2026-07-23',
      featured: false,
      categories: ['feature', 'improvement'],
      areas: ['drafts', 'discord'],
      titleKey: 'updates.releases.2026_07_2.title',
      summaryKey: 'updates.releases.2026_07_2.summary',
      details: [
        {
          id: 'weekly-presence-scheduling',
          category: 'feature',
          link: AppRoutes.Settings,
          titleKey:
            'updates.releases.2026_07_2.details.weekly-presence-scheduling.title',
          descriptionKey:
            'updates.releases.2026_07_2.details.weekly-presence-scheduling.description',
        },
        {
          id: 'publication-closing-times',
          category: 'improvement',
          titleKey:
            'updates.releases.2026_07_2.details.publication-closing-times.title',
          descriptionKey:
            'updates.releases.2026_07_2.details.publication-closing-times.description',
        },
        {
          id: 'moderator-management',
          category: 'improvement',
          link: AppRoutes.Settings,
          titleKey:
            'updates.releases.2026_07_2.details.moderator-management.title',
          descriptionKey:
            'updates.releases.2026_07_2.details.moderator-management.description',
        },
        {
          id: 'window-recovery',
          category: 'improvement',
          titleKey: 'updates.releases.2026_07_2.details.window-recovery.title',
          descriptionKey:
            'updates.releases.2026_07_2.details.window-recovery.description',
        },
        {
          id: 'duplicate-draft-protection',
          category: 'improvement',
          titleKey:
            'updates.releases.2026_07_2.details.duplicate-draft-protection.title',
          descriptionKey:
            'updates.releases.2026_07_2.details.duplicate-draft-protection.description',
        },
      ],
    })
    expect(previous?.details.map(({ id }) => id)).toEqual(presenceSchedulingDetailIds)
  })

  it('preserves exactly the fifteen details from release 2026.07.1', () => {
    const previous = SYSTEM_UPDATES.find(({ version }) => version === '2026.07.1')

    expect(previous?.featured).toBe(false)
    expect(previous?.details.map(({ id }) => id)).toEqual(previousDetailIds)
  })

  it('uses every controlled category and area in a non-empty registry', () => {
    expect(
      new Set(SYSTEM_UPDATES.flatMap(({ categories }) => categories)),
    ).toEqual(
      new Set(['feature', 'improvement', 'fix', 'security', 'infrastructure']),
    )
    expect(new Set(SYSTEM_UPDATES.flatMap(({ areas }) => areas))).toEqual(
      new Set([
        'platform',
        'players',
        'teams',
        'users',
        'drafts',
        'discord',
        'security',
        'infrastructure',
      ]),
    )
    expect(
      SYSTEM_UPDATES.every(
        ({ categories, areas, details }) =>
          categories.length && areas.length && details.length,
      ),
    ).toBe(true)
  })

  it('does not expose restricted routes through editorial links', () => {
    const editorialLinks = SYSTEM_UPDATES.flatMap(({ details }) =>
      details.flatMap(({ link }) => (link ? [link] : [])),
    )

    expect(editorialLinks).not.toContain(AppRoutes.UsersAdmin)
  })
})
