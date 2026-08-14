import type { DraftMode, SeriesFormat } from './competition'
import type { MatchDetail } from './match'
import type { SeasonScope, SeasonSummary } from './season'

export type { DraftMode, SeriesFormat } from './competition'

export type SeriesType = 'DiariaTemporaria' | 'ConfrontoOficial' | 'Amistoso'
export type SeriesState =
  | 'Agendada'
  | 'EmAndamento'
  | 'Concluida'
  | 'Cancelada'
  | 'Anulada'
export type SeriesSideType = 'Temporario' | 'TimeOficial'

export interface SeriesParticipantSnapshot {
  jogadorId: string
  nomeExibicao: string
  tag?: string | null
}

export interface SeriesSideSnapshot {
  id: string
  tipo: SeriesSideType
  origemId: string
  nome: string
  tag?: string | null
  capitaoJogadorId?: string | null
  capitaoNomeSnapshot?: string | null
  capitaoTagSnapshot?: string | null
  participantes?: SeriesParticipantSnapshot[]
}

export interface SeriesDetail {
  id: string
  seasonId: string
  competicaoId?: string | null
  rodadaId?: string | null
  versaoRegrasId: string
  eventoId?: string | null
  tipo: SeriesType
  formato: SeriesFormat
  modoDraft: DraftMode
  estado: SeriesState
  agendadaPara: string
  lados: SeriesSideSnapshot[]
  placar: [number, number]
  partidas: MatchDetail[]
  bloqueiosFearless: number[]
  elegivelOficial: boolean
  revisaoNecessaria: boolean
  versao: number
  acoesPermitidas: string[]
}

export interface SeriesPage {
  page: number
  pageSize: number
  items: SeriesDetail[]
  totalItems: number
  totalPages: number
  calendarioConfigurado: boolean
  temporadaAtual?: SeasonSummary | null
  seasonsIncluidas: SeasonSummary[]
}

export interface SeriesResult {
  serieId: string
  placar: [number, number]
  ladoVencedorId?: string | null
  concluida: boolean
  elegivelOficial: boolean
}

export interface SeriesListFilters {
  scope?: SeasonScope
  page?: number
  pageSize?: number
  tipo?: SeriesType
  estado?: SeriesState
}

export interface CreateSeriesRequest {
  seasonId: string
  competicaoId?: string | null
  rodadaId?: string | null
  versaoRegrasId: string
  eventoId?: string | null
  tipo: SeriesType
  agendadaPara: string
  dataLocal?: string | null
  draftMontagemId?: string | null
  ladoOrigemIds: [string, string]
}

export interface SeriesReasonRequest {
  justificativa: string
}

export type CompetitiveMutationInvalidations = readonly [
  'series-detail',
  'series-scoreboard',
  'series-fearless',
  'match-detail',
]

export interface CompetitiveMutationResult<T> {
  data: T
  etag: string | null
  idempotencyReplayed: boolean
  invalidates: CompetitiveMutationInvalidations
}
