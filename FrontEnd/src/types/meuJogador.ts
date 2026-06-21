import type { Divisao, Elo, Player, RoutePreference } from '@/services/players'

export type MeuJogadorProfile = Player

export interface MeuJogadorProfilePayload {
  nomeExibicao: string
  discord: string
  riotId: string
  opGgUrl: string
  deepLolUrl: string
  elo: Elo
  divisao?: Divisao | null
  preferencias: RoutePreference[]
}
