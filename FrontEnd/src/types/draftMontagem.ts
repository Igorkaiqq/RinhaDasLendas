import {
  DraftMontagemEscolhaTipoValues,
  DraftMontagemEstadoValues,
  DraftMontagemModoValues,
  DraftMontagemOrdemEscolhaModoValues,
  DraftMontagemPresencaOrigemValues,
  DraftMontagemPresencaStatusValues,
  DraftMontagemStatusValues,
} from '@/constants/draftMontagem'
import type { RoutePreference } from '@/services/players'
import type { DraftCriteriaValue } from '@/types/draft'

export type DraftMontagemStatus = (typeof DraftMontagemStatusValues)[keyof typeof DraftMontagemStatusValues]
export type DraftMontagemEstado = (typeof DraftMontagemEstadoValues)[keyof typeof DraftMontagemEstadoValues]
export type DraftMontagemRota = 'Top' | 'Jungle' | 'Mid' | 'Adc' | 'Support'
export type DraftMontagemModo = (typeof DraftMontagemModoValues)[keyof typeof DraftMontagemModoValues]
export type DraftMontagemEscolhaTipo = (typeof DraftMontagemEscolhaTipoValues)[keyof typeof DraftMontagemEscolhaTipoValues]
export type DraftMontagemOrdemEscolhaModo = (typeof DraftMontagemOrdemEscolhaModoValues)[keyof typeof DraftMontagemOrdemEscolhaModoValues]
export type DraftMontagemPresencaStatus = (typeof DraftMontagemPresencaStatusValues)[keyof typeof DraftMontagemPresencaStatusValues]
export type DraftMontagemPresencaOrigem = (typeof DraftMontagemPresencaOrigemValues)[keyof typeof DraftMontagemPresencaOrigemValues]

export interface DraftMontagemParticipante {
  jogadorId: string
  nomeExibicao: string
  discord?: string | null
  riotId?: string | null
  opGgUrl?: string | null
  deepLolUrl?: string | null
  elo?: string | null
  divisao?: string | null
  status: string
  preferencias: RoutePreference[]
  estado: DraftMontagemEstado
  capitao: boolean
  rotaContextual?: DraftMontagemRota | null
  ordem: number
  dataCadastro: string
  dataAtualizacao: string
}

export interface DraftMontagemTime {
  id: string
  nome: string
  ordem: number
  cor: string
  capitaoId?: string | null
  jogadores: DraftMontagemParticipante[]
}

export interface DraftMontagem {
  id: string
  nome: string
  observacoes?: string | null
  status: DraftMontagemStatus
  modo: DraftMontagemModo
  tamanhoEquipe: number
  quantidadeTimes: number
  quantidadeReservas: number
  criterioCapitaes: DraftCriteriaValue
  turnoAtualTimeId?: string | null
  turnoAtualCapitaoId?: string | null
  turnoSequencia?: number | null
  turnoIniciadoEm?: string | null
  turnoExpiraEm?: string | null
  duracaoTurnoSegundos: number
  horarioEncerramentoPresenca?: string | null
  discordGuildId?: string | null
  discordPresenceMessageId?: string | null
  ordemEscolhaModo?: DraftMontagemOrdemEscolhaModo | null
  presencaContinuadaManualmente: boolean
  presencas: DraftMontagemPresenca[]
  times: DraftMontagemTime[]
  livres: DraftMontagemParticipante[]
  reservas: DraftMontagemParticipante[]
  escolhas: DraftMontagemEscolha[]
  substituicoes: DraftMontagemSubstituicao[]
  motivoCancelamento?: string | null
  dataCadastro: string
  dataAtualizacao: string
}

export interface DraftMontagemResumo {
  id: string
  nome: string
  status: DraftMontagemStatus
  modo: DraftMontagemModo
  tamanhoEquipe: number
  quantidadeTimes: number
  quantidadeReservas: number
  horarioEncerramentoPresenca?: string | null
  discordGuildId?: string | null
  discordPresenceMessageId?: string | null
  ordemEscolhaModo?: DraftMontagemOrdemEscolhaModo | null
  presencaContinuadaManualmente: boolean
  dataCadastro: string
  dataAtualizacao: string
}

export interface DraftMontagemPresenca {
  id: string
  usuarioId: string
  jogadorId: string
  nomeExibicao: string
  discordUserId?: string | null
  origemConfirmacao: DraftMontagemPresencaOrigem
  status: DraftMontagemPresencaStatus
  confirmadoEm: string
  canceladoEm?: string | null
  ordemConfirmacao: number
  ordemManual?: number | null
  ordemFinal?: number | null
}

export interface DraftMontagemEscolha {
  sequencia: number
  timeId: string
  capitaoId: string
  jogadorId?: string | null
  tipo: DraftMontagemEscolhaTipo
  jogadorNome?: string | null
  registradoEm: string
}

export interface DraftMontagemSubstituicao {
  timeId: string
  jogadorSaiuId: string
  reservaEntrouId: string
  jogadorSaiuNome?: string | null
  reservaEntrouNome?: string | null
  motivo?: string | null
  responsavelUsuarioId: string
  registradoEm: string
}

export interface DraftMontagemRealtimeState {
  montagem: DraftMontagem
  serverNow: string
  canCurrentUserPick: boolean
}

export interface DraftMontagemPayload {
  nome: string
  observacoes?: string | null
  tamanhoEquipe: number
  sortearCapitaes: boolean
  horarioEncerramentoPresenca?: string | null
  discordGuildId?: string | null
  capitaesIds: string[]
  jogadoresIds: string[]
}

export interface DraftMontagemLayoutParticipantePayload {
  jogadorId: string
  ordem: number
  rotaContextual?: DraftMontagemRota | null
}

export interface DraftMontagemLayoutTimePayload {
  timeId: string
  nome: string
  capitaoId?: string | null
  jogadores: DraftMontagemLayoutParticipantePayload[]
}

export interface DraftMontagemLayoutPayload {
  times: DraftMontagemLayoutTimePayload[]
  livres: DraftMontagemLayoutParticipantePayload[]
  reservas: DraftMontagemLayoutParticipantePayload[]
}
