export type SeasonState = 'Planejada' | 'Ativa' | 'Encerrada'

export type AtLeastOne<T, Keys extends keyof T = keyof T> = Keys extends keyof T
  ? Required<Pick<T, Keys>> & Partial<Omit<T, Keys>>
  : never

export type SeasonScope =
  | { mode: 'current' }
  | { mode: 'selected'; seasonIds: [string, ...string[]] }
  | { mode: 'all' }

export interface SeasonSummary {
  id: string
  nome: string
  ano: number
  ordemNoAno: number
  dataInicio: string
  dataFimExclusiva: string
  estado: SeasonState
  versao: number
}

export interface SeasonDetail extends SeasonSummary {
  quantidadeCompeticoes: number
  ativadaEm?: string | null
  encerradaEm?: string | null
  acoesPermitidas?: string[]
}

export interface SeasonPage {
  page: number
  pageSize: number
  items: SeasonSummary[]
  totalItems: number
  totalPages: number
  calendarioConfigurado: boolean
  temporadaAtual: SeasonSummary | null
  versaoCalendario: number
}

export interface SeasonListFilters {
  page?: number
  pageSize?: number
  estado?: SeasonState
}

export interface CreateSeasonRequest {
  nome: string
  ano: number
  ordemNoAno: number
  dataInicio: string
  dataFimExclusiva: string
}

export type UpdateSeasonRequest = AtLeastOne<CreateSeasonRequest>

export interface ObservedResponse<T> {
  data: T
  etag: string | null
}
