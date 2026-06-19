import type { Player } from '@/services/players'

export interface TeamMember {
  jogadorId: string
  nomeExibicao: string
  principal: boolean
  capitao: boolean
}

export interface TeamCaptain {
  id: string
  nomeExibicao: string
}

export interface Team {
  id: string
  nome: string
  tag: string
  observacoes?: string | null
  status: TeamStatusValue
  capitao?: TeamCaptain | null
  quantidadeJogadores: number
  membros: TeamMember[]
  dataCadastro: string
  dataAtualizacao: string
}

export interface TeamPayload {
  nome: string
  tag: string
  observacoes?: string | null
  capitaoId?: string | null
  jogadoresIds: string[]
}

export interface TeamFilters {
  search?: string
  status?: TeamStatusValue | ''
}

export interface TeamFormPayload extends TeamPayload {
  id?: string
}

export type TeamFormMode = 'create' | 'edit'
export type TeamPlayerOption = Pick<Player, 'id' | 'nomeExibicao' | 'status'>
export type TeamStatusValue = 'Ativo' | 'Inativo'
