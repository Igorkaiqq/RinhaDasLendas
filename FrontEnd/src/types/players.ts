import type { Divisao, Elo, Player, RoutePreference } from '@/services/players'

export type PlayerFormMode = 'create' | 'edit'
export type FeedbackTone = 'success' | 'danger' | 'info'

export interface PlayerFormDraft {
  mode: PlayerFormMode
  id?: string
  nomeExibicao: string
  discord: string
  riotId: string
  opGgUrl: string
  deepLolUrl: string
  elo: Elo | ''
  divisao: Divisao | ''
  preferencias: RoutePreference[]
}

export interface FeedbackState {
  tone: FeedbackTone
  message: string
}

export type PlayerListItem = Player
