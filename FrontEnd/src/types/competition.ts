import type { AtLeastOne, SeasonSummary } from './season'

export type SeriesFormat = 'Md3' | 'Md5'
export type DraftMode = 'Padrao' | 'Fearless'

export interface Round {
  id: string
  competicaoId: string
  nome: string
  ordem: number
  versao: number
}

export interface RulesVersion {
  id: string
  seasonId: string
  competicaoId?: string | null
  numero: number
  formato: SeriesFormat
  modoDraft: DraftMode
  publicadaEm: string
}

export interface CompetitionDetail {
  id: string
  seasonId: string
  nome: string
  codigo: string
  circuitoDiario: boolean
  rodadas: Round[]
  regrasPublicadas: RulesVersion[]
  versao: number
  acoesPermitidas?: string[]
}

export interface CompetitionPage {
  page: number
  pageSize: number
  items: CompetitionDetail[]
  totalItems: number
  totalPages: number
  calendarioConfigurado: boolean
  temporadaAtual?: SeasonSummary | null
  seasonsIncluidas: SeasonSummary[]
}

export interface CreateCompetitionRequest {
  nome: string
  codigo: string
  circuitoDiario: boolean
}

export type UpdateCompetitionRequest = AtLeastOne<CreateCompetitionRequest>

export interface CreateRoundRequest {
  nome: string
  ordem: number
}

export interface ReorderRoundsRequest {
  rodadaIds: string[]
}

export interface PublishRulesRequest {
  formato: SeriesFormat
  modoDraft: DraftMode
}
