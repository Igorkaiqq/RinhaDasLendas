import type { Player } from '@/services/players'

export type DraftStatusValue = 'Aberto' | 'Concluido' | 'Cancelado'
export type DraftTeamValue = 'TimeA' | 'TimeB'
export type DraftCriteriaValue = 'Manual' | 'Sorteio'

export interface DraftPlayer {
  id: string
  nomeExibicao: string
}

export interface DraftParticipant {
  jogadorId: string
  nomeExibicao: string
  capitao: boolean
}

export interface DraftPick {
  sequencia: number
  time: DraftTeamValue
  capitaoId: string
  jogadorId: string
  jogadorNome: string
  dataEscolha: string
}

export interface Draft {
  id: string
  nome: string
  observacoes?: string | null
  status: DraftStatusValue
  tamanhoTime: number
  criterioCapitaes: DraftCriteriaValue
  criterioPrimeiroPick: DraftCriteriaValue
  proximoTime?: DraftTeamValue | null
  capitaoTimeA: DraftPlayer
  capitaoTimeB: DraftPlayer
  timeA: DraftParticipant[]
  timeB: DraftParticipant[]
  disponiveis: DraftPlayer[]
  escolhas: DraftPick[]
  motivoCancelamento?: string | null
  dataCadastro: string
  dataAtualizacao: string
}

export interface DraftPayload {
  nome: string
  observacoes?: string | null
  tamanhoTime: number
  sortearCapitaes: boolean
  capitaoTimeAId?: string | null
  capitaoTimeBId?: string | null
  sortearPrimeiroPick: boolean
  primeiroTime?: DraftTeamValue | null
  jogadoresIds: string[]
}

export interface DraftCreateFormState extends DraftPayload {
  selectedPlayers: Player[]
}
