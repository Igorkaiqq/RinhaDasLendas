export const LeagueRole = {
  Top: 'Top',
  Jungle: 'Jungle',
  Mid: 'Mid',
  Adc: 'Adc',
  Support: 'Support',
} as const

export type LeagueRoleValue = (typeof LeagueRole)[keyof typeof LeagueRole]

export const LEAGUE_ROLES: LeagueRoleValue[] = [
  LeagueRole.Top,
  LeagueRole.Jungle,
  LeagueRole.Mid,
  LeagueRole.Adc,
  LeagueRole.Support,
]
