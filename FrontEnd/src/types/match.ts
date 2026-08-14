import type { SeriesResult } from './series'

export type MatchState = 'Rascunho' | 'Confirmada' | 'Remake' | 'Anulada'
export type MatchEndReason = 'Normal' | 'Surrender'
export type RemakePickDecision = 'PreservarPicks' | 'DesconsiderarPicks'
export type ChampionIds = readonly [number, number, number, number, number]

export interface MatchPick {
  ladoSerieId: string
  championId: number
  ordem: number
}

export interface MatchDetail {
  id: string
  serieId: string
  ordem: number
  estado: MatchState
  ladoVencedorId?: string | null
  motivoTermino?: MatchEndReason | null
  decisaoPicksRemake?: RemakePickDecision | null
  picks: MatchPick[]
  conflitoFearless: boolean
  acoesPermitidas: string[]
}

export interface MatchPage {
  page: number
  pageSize: number
  items: MatchDetail[]
  totalItems: number
  totalPages: number
}

export interface MatchSidePicks {
  ladoSerieId: string
  championIds: ChampionIds
}

export interface RegisterMatchPicksRequest {
  lados: readonly [MatchSidePicks, MatchSidePicks]
}

export interface ConfirmMatchResultRequest {
  ladoVencedorId: string
  motivoTermino: MatchEndReason
}

export interface RegisterMatchRemakeRequest {
  decisaoPicks: RemakePickDecision
  justificativa: string
}

export interface AnnulMatchRequest {
  justificativa: string
  anularSerieSeInconclusiva: boolean
}

interface CorrectMatchBase {
  justificativa: string
  anularSerieSeInconclusiva?: boolean
}

export type CorrectMatchRequest = CorrectMatchBase &
  (
    | RegisterMatchPicksRequest
    | ConfirmMatchResultRequest
    | (RegisterMatchPicksRequest & ConfirmMatchResultRequest)
  )

export interface MatchMutationResult {
  partida: MatchDetail
  resultadoSerie: SeriesResult
  bloqueiosFearless: number[]
  revisaoNecessaria: boolean
}
