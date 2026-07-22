import { describe, expect, it } from 'vitest'

import { SYSTEM_UPDATES } from './systemUpdates'

const releaseIds = [
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
  it('contains the eight stable releases in descending chronological order', () => {
    expect(SYSTEM_UPDATES.map(({ id }) => id)).toEqual(releaseIds)
    expect(SYSTEM_UPDATES.map(({ version }) => version)).toEqual([
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

  it('contains exactly the fifteen stable details in the latest release', () => {
    expect(SYSTEM_UPDATES[0]).toMatchObject({
      id: 'drafts-discord-reliability',
      version: '2026.07.1',
      featured: true,
    })
    expect(SYSTEM_UPDATES[0].details.map(({ id }) => id)).toEqual(
      latestDetailIds,
    )
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
})
