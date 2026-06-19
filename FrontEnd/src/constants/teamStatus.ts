import type { TeamStatusValue } from '@/types/team'

export const TeamStatus = {
  Active: 'Ativo',
  Inactive: 'Inativo',
} as const satisfies Record<string, TeamStatusValue>

export const TEAM_STATUS_OPTIONS = [TeamStatus.Active, TeamStatus.Inactive] as const
