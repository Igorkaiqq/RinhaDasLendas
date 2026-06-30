import { LeagueRole, type LeagueRoleValue } from '@/constants/leagueRoles'
import type { DraftMontagemRota } from '@/types/draftMontagem'

export const DraftRouteFilterValues = {
  All: 'TODAS AS ROTAS',
  Top: 'TOP',
  Jungle: 'JUNGLE',
  Mid: 'MID',
  Adc: 'ADC',
  Support: 'SUPORTE',
} as const

export type DraftRouteFilterValue = (typeof DraftRouteFilterValues)[keyof typeof DraftRouteFilterValues]

export const DRAFT_ROUTE_FILTER_OPTIONS: DraftRouteFilterValue[] = [
  DraftRouteFilterValues.All,
  DraftRouteFilterValues.Top,
  DraftRouteFilterValues.Jungle,
  DraftRouteFilterValues.Mid,
  DraftRouteFilterValues.Adc,
  DraftRouteFilterValues.Support,
]

export const LEAGUE_ROLE_BY_DRAFT_FILTER: Record<DraftRouteFilterValue, LeagueRoleValue | null> = {
  [DraftRouteFilterValues.All]: null,
  [DraftRouteFilterValues.Top]: LeagueRole.Top,
  [DraftRouteFilterValues.Jungle]: LeagueRole.Jungle,
  [DraftRouteFilterValues.Mid]: LeagueRole.Mid,
  [DraftRouteFilterValues.Adc]: LeagueRole.Adc,
  [DraftRouteFilterValues.Support]: LeagueRole.Support,
}

export const DRAFT_ROUTE_LABEL_BY_LEAGUE_ROLE: Record<LeagueRoleValue, DraftRouteFilterValue> = {
  [LeagueRole.Top]: DraftRouteFilterValues.Top,
  [LeagueRole.Jungle]: DraftRouteFilterValues.Jungle,
  [LeagueRole.Mid]: DraftRouteFilterValues.Mid,
  [LeagueRole.Adc]: DraftRouteFilterValues.Adc,
  [LeagueRole.Support]: DraftRouteFilterValues.Support,
}

export const DRAFT_MONTAGEM_ROUTE_BY_FILTER: Record<DraftRouteFilterValue, DraftMontagemRota | null> = {
  [DraftRouteFilterValues.All]: null,
  [DraftRouteFilterValues.Top]: 'Top',
  [DraftRouteFilterValues.Jungle]: 'Jungle',
  [DraftRouteFilterValues.Mid]: 'Mid',
  [DraftRouteFilterValues.Adc]: 'Adc',
  [DraftRouteFilterValues.Support]: 'Support',
}
