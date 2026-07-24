export type PresenceScheduleStatus = 'Ativo' | 'Pausado'

export type PresenceScheduleOccurrenceStatus =
  | 'Processando'
  | 'Bloqueada'
  | 'Criada'
  | 'Perdida'
  | 'Falha'

export type IsoWeekday =
  | 'Segunda'
  | 'Terca'
  | 'Quarta'
  | 'Quinta'
  | 'Sexta'
  | 'Sabado'
  | 'Domingo'

export interface SavePresenceScheduleRequest {
  nome: string
  observacao: string | null
  diasSemana: IsoWeekday[]
  horarioPublicacao: string
  horarioEncerramento: string
}

export interface PresenceScheduleOccurrenceSummary {
  id: string
  dataLocal: string
  publicacaoPrevistaEm: string
  encerramentoPrevistoEm: string
  status: PresenceScheduleOccurrenceStatus
  draftMontagemId: string | null
  messageCode: string | null
}

export interface PresenceScheduleSummary {
  id: string
  nome: string
  observacao: string | null
  status: PresenceScheduleStatus
  diasSemana: IsoWeekday[]
  horarioPublicacao: string
  horarioEncerramento: string
  proximaExecucaoEm: string | null
  ultimaOcorrencia: PresenceScheduleOccurrenceSummary | null
}

export interface PaginatedResponse<T> {
  page: number
  pageSize: number
  items: T[]
  totalItems: number
  totalPages: number
}
