import { AppRoutes } from '@/constants/appRoutes'
import type { AppRoutePath } from '@/constants/appRoutes'
import type {
  SystemUpdateCategory,
  SystemUpdateDetail,
  SystemUpdateRelease,
} from '@/types/systemUpdate'

const releaseKeys = (version: string) =>
  `updates.releases.${version.split('.').join('_')}`
const detailKeys = (version: string, id: string) =>
  `${releaseKeys(version)}.details.${id}`

function releaseDetails(
  version: string,
  details: readonly {
    readonly id: string
    readonly category: SystemUpdateCategory
    readonly link?: AppRoutePath
  }[],
): readonly SystemUpdateDetail[] {
  return details.map((detail) => ({
    ...detail,
    titleKey: `${detailKeys(version, detail.id)}.title`,
    descriptionKey: `${detailKeys(version, detail.id)}.description`,
  }))
}

export const SYSTEM_UPDATES = [
  {
    id: 'drafts-discord-reliability',
    version: '2026.07.1',
    publishedAt: '2026-07-22',
    featured: true,
    categories: ['feature', 'improvement', 'fix', 'security', 'infrastructure'],
    areas: ['players', 'drafts', 'discord', 'security', 'infrastructure'],
    titleKey: `${releaseKeys('2026.07.1')}.title`,
    summaryKey: `${releaseKeys('2026.07.1')}.summary`,
    details: releaseDetails('2026.07.1', [
      {
        id: 'discord-draft-deep-link',
        category: 'improvement',
        link: AppRoutes.Draft,
      },
      {
        id: 'invalid-draft-link-feedback',
        category: 'fix',
        link: AppRoutes.Draft,
      },
      {
        id: 'contextual-admin-confirmations',
        category: 'improvement',
        link: AppRoutes.Draft,
      },
      {
        id: 'discord-publication-statuses',
        category: 'improvement',
        link: AppRoutes.Draft,
      },
      {
        id: 'individual-publication-recovery',
        category: 'improvement',
        link: AppRoutes.Draft,
      },
      { id: 'duplicate-message-protection', category: 'fix' },
      { id: 'realtime-presence', category: 'feature', link: AppRoutes.Draft },
      {
        id: 'consistent-presence-operations',
        category: 'fix',
        link: AppRoutes.Draft,
      },
      {
        id: 'eligible-player-search',
        category: 'improvement',
        link: AppRoutes.Draft,
      },
      { id: 'admin-action-audit', category: 'security', link: AppRoutes.Draft },
      { id: 'clear-bot-messages', category: 'improvement' },
      {
        id: 'discord-permission-diagnostics',
        category: 'improvement',
        link: AppRoutes.Settings,
      },
      {
        id: 'independent-presence-call',
        category: 'improvement',
        link: AppRoutes.Draft,
      },
      { id: 'resilient-publication-queue', category: 'infrastructure' },
      { id: 'security-stability-hardening', category: 'security' },
    ]),
  },
  {
    id: 'security-deploy-identity',
    version: '2026.06.7',
    publishedAt: '2026-06-30',
    featured: false,
    categories: ['improvement', 'security', 'infrastructure'],
    areas: ['platform', 'security', 'infrastructure'],
    titleKey: `${releaseKeys('2026.06.7')}.title`,
    summaryKey: `${releaseKeys('2026.06.7')}.summary`,
    details: releaseDetails('2026.06.7', [
      { id: 'security-baseline', category: 'security' },
      { id: 'deployment-readiness', category: 'infrastructure' },
      { id: 'operational-observability', category: 'infrastructure' },
      { id: 'visual-identity', category: 'improvement' },
    ]),
  },
  {
    id: 'discord-presence',
    version: '2026.06.6',
    publishedAt: '2026-06-29',
    featured: false,
    categories: ['feature', 'improvement'],
    areas: ['users', 'drafts', 'discord'],
    titleKey: `${releaseKeys('2026.06.6')}.title`,
    summaryKey: `${releaseKeys('2026.06.6')}.summary`,
    details: releaseDetails('2026.06.6', [
      {
        id: 'discord-account-link',
        category: 'feature',
        link: AppRoutes.Settings,
      },
      {
        id: 'shared-presence-list',
        category: 'feature',
        link: AppRoutes.Draft,
      },
      {
        id: 'presence-closing',
        category: 'improvement',
        link: AppRoutes.Draft,
      },
      {
        id: 'final-teams-publication',
        category: 'feature',
        link: AppRoutes.Draft,
      },
    ]),
  },
  {
    id: 'realtime-draft',
    version: '2026.06.5',
    publishedAt: '2026-06-24',
    featured: false,
    categories: ['feature', 'improvement'],
    areas: ['players', 'teams', 'drafts'],
    titleKey: `${releaseKeys('2026.06.5')}.title`,
    summaryKey: `${releaseKeys('2026.06.5')}.summary`,
    details: releaseDetails('2026.06.5', [
      { id: 'synchronized-picks', category: 'feature', link: AppRoutes.Draft },
      { id: 'captain-turns', category: 'feature', link: AppRoutes.Draft },
      { id: 'turn-timer', category: 'improvement', link: AppRoutes.Draft },
      {
        id: 'reconnection-state',
        category: 'improvement',
        link: AppRoutes.Draft,
      },
    ]),
  },
  {
    id: 'users-auth-rbac',
    version: '2026.06.4',
    publishedAt: '2026-06-21',
    featured: false,
    categories: ['feature', 'security', 'improvement'],
    areas: ['users', 'players', 'security'],
    titleKey: `${releaseKeys('2026.06.4')}.title`,
    summaryKey: `${releaseKeys('2026.06.4')}.summary`,
    details: releaseDetails('2026.06.4', [
      { id: 'account-access', category: 'feature', link: AppRoutes.Profile },
      {
        id: 'role-permissions',
        category: 'security',
      },
      {
        id: 'player-profile-link',
        category: 'feature',
        link: AppRoutes.Profile,
      },
      {
        id: 'account-recovery',
        category: 'improvement',
        link: AppRoutes.ForgotPassword,
      },
    ]),
  },
  {
    id: 'visual-draft',
    version: '2026.06.3',
    publishedAt: '2026-06-20',
    featured: false,
    categories: ['feature', 'improvement'],
    areas: ['players', 'teams', 'drafts'],
    titleKey: `${releaseKeys('2026.06.3')}.title`,
    summaryKey: `${releaseKeys('2026.06.3')}.summary`,
    details: releaseDetails('2026.06.3', [
      {
        id: 'registered-player-pool',
        category: 'improvement',
        link: AppRoutes.Draft,
      },
      {
        id: 'visual-team-building',
        category: 'feature',
        link: AppRoutes.Draft,
      },
      {
        id: 'dynamic-teams-reserves',
        category: 'feature',
        link: AppRoutes.Draft,
      },
      { id: 'player-details', category: 'improvement', link: AppRoutes.Draft },
    ]),
  },
  {
    id: 'players-teams',
    version: '2026.06.2',
    publishedAt: '2026-06-19',
    featured: false,
    categories: ['feature', 'improvement'],
    areas: ['players', 'teams'],
    titleKey: `${releaseKeys('2026.06.2')}.title`,
    summaryKey: `${releaseKeys('2026.06.2')}.summary`,
    details: releaseDetails('2026.06.2', [
      { id: 'player-directory', category: 'feature', link: AppRoutes.Players },
      { id: 'route-preferences', category: 'feature', link: AppRoutes.Players },
      {
        id: 'player-availability',
        category: 'improvement',
        link: AppRoutes.Players,
      },
      { id: 'reusable-teams', category: 'feature', link: AppRoutes.Teams },
    ]),
  },
  {
    id: 'platform-foundation',
    version: '2026.06.1',
    publishedAt: '2026-06-10',
    featured: false,
    categories: ['feature', 'improvement', 'infrastructure'],
    areas: ['platform', 'infrastructure'],
    titleKey: `${releaseKeys('2026.06.1')}.title`,
    summaryKey: `${releaseKeys('2026.06.1')}.summary`,
    details: releaseDetails('2026.06.1', [
      { id: 'architecture-foundation', category: 'infrastructure' },
      { id: 'localized-interface', category: 'feature' },
      { id: 'shared-standards', category: 'improvement' },
    ]),
  },
] as const satisfies readonly SystemUpdateRelease[]
