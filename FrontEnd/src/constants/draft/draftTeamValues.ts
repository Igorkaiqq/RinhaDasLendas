import type { DraftTeamValue } from '@/types/draft'

export const DraftTeamValues = {
  TimeA: 'TimeA',
  TimeB: 'TimeB',
} as const satisfies Record<string, DraftTeamValue>
