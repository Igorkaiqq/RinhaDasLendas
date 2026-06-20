import { AxiosError } from 'axios'

import type { Draft, DraftPayload, DraftStatusValue } from '@/types/draft'

import { api } from './api'

interface PaginatedDrafts {
  page: number
  pageSize: number
  items: Draft[]
  totalItems: number
  totalPages: number
}

interface ApiErrorResponse {
  message?: string
  errors?: string[]
}

interface DraftFilters {
  search?: string
  status?: DraftStatusValue | ''
}

export class DraftServiceError extends Error {
  constructor(public readonly errors: string[]) {
    super(errors[0] ?? 'Nao foi possivel processar o draft.')
  }
}

const fakeDrafts: Draft[] = []

export async function listDrafts(filters: DraftFilters = {}): Promise<Draft[]> {
  try {
    const response = await api.get<PaginatedDrafts>('/api/v1/drafts', {
      params: { ...filters, page: 1, pageSize: 100 },
    })
    return response.data.items
  } catch (error) {
    if (isConnectionFailure(error)) {
      return filterFakeDrafts(filters)
    }

    throw toDraftServiceError(error)
  }
}

export async function createDraft(payload: DraftPayload): Promise<Draft> {
  try {
    const response = await api.post<Draft>('/api/v1/drafts', normalizePayload(payload))
    return response.data
  } catch (error) {
    if (isConnectionFailure(error)) {
      const created = createFakeDraft(payload)
      fakeDrafts.unshift(created)
      return created
    }

    throw toDraftServiceError(error)
  }
}

export async function registerDraftPick(id: string, jogadorId: string): Promise<Draft> {
  try {
    const response = await api.post<Draft>(`/api/v1/drafts/${id}/picks`, { jogadorId })
    return response.data
  } catch (error) {
    if (isConnectionFailure(error)) {
      return pickFakeDraft(id, jogadorId)
    }

    throw toDraftServiceError(error)
  }
}

export async function cancelDraft(id: string, motivo?: string | null): Promise<Draft> {
  try {
    const response = await api.patch<Draft>(`/api/v1/drafts/${id}/cancelar`, { motivo })
    return response.data
  } catch (error) {
    if (isConnectionFailure(error)) {
      const draft = requireFakeDraft(id)
      draft.status = 'Cancelado'
      draft.proximoTime = null
      draft.motivoCancelamento = motivo
      return draft
    }

    throw toDraftServiceError(error)
  }
}

function filterFakeDrafts(filters: DraftFilters): Draft[] {
  const search = filters.search?.trim().toLowerCase()
  return fakeDrafts.filter((draft) => {
    const matchesStatus = !filters.status || draft.status === filters.status
    const matchesSearch = !search || draft.nome.toLowerCase().includes(search)
    return matchesStatus && matchesSearch
  })
}

function createFakeDraft(payload: DraftPayload): Draft {
  const players = payload.jogadoresIds.map((id) => ({ id, nomeExibicao: `Jogador ${id.slice(0, 4)}` }))
  const capitaoA = players.find((player) => player.id === payload.capitaoTimeAId) ?? players[0]
  const capitaoB = players.find((player) => player.id === payload.capitaoTimeBId) ?? players[1]
  const now = new Date().toISOString()

  if (!capitaoA || !capitaoB) {
    throw new DraftServiceError(['Selecione jogadores suficientes para criar o draft.'])
  }

  return {
    id: crypto.randomUUID(),
    nome: payload.nome,
    observacoes: payload.observacoes,
    status: 'Aberto',
    tamanhoTime: payload.tamanhoTime,
    criterioCapitaes: payload.sortearCapitaes ? 'Sorteio' : 'Manual',
    criterioPrimeiroPick: payload.sortearPrimeiroPick ? 'Sorteio' : 'Manual',
    proximoTime: payload.primeiroTime ?? 'TimeA',
    capitaoTimeA: capitaoA,
    capitaoTimeB: capitaoB,
    timeA: [{ jogadorId: capitaoA.id, nomeExibicao: capitaoA.nomeExibicao, capitao: true }],
    timeB: [{ jogadorId: capitaoB.id, nomeExibicao: capitaoB.nomeExibicao, capitao: true }],
    disponiveis: players.filter((player) => player.id !== capitaoA.id && player.id !== capitaoB.id),
    escolhas: [],
    dataCadastro: now,
    dataAtualizacao: now,
  }
}

function pickFakeDraft(id: string, jogadorId: string): Draft {
  const draft = requireFakeDraft(id)
  const player = draft.disponiveis.find((available) => available.id === jogadorId)
  if (!player || draft.status !== 'Aberto' || !draft.proximoTime) {
    throw new DraftServiceError(['Jogador indisponivel para este draft.'])
  }

  const participant = { jogadorId: player.id, nomeExibicao: player.nomeExibicao, capitao: false }
  const pickedTeam = draft.proximoTime
  const captainId = pickedTeam === 'TimeA' ? draft.capitaoTimeA.id : draft.capitaoTimeB.id
  if (pickedTeam === 'TimeA') {
    draft.timeA.push(participant)
    draft.proximoTime = 'TimeB'
  } else {
    draft.timeB.push(participant)
    draft.proximoTime = 'TimeA'
  }
  draft.disponiveis = draft.disponiveis.filter((available) => available.id !== jogadorId)
  draft.escolhas.push({
    sequencia: draft.escolhas.length + 1,
    time: pickedTeam,
    capitaoId: captainId,
    jogadorId: player.id,
    jogadorNome: player.nomeExibicao,
    dataEscolha: new Date().toISOString(),
  })
  if (draft.timeA.length >= draft.tamanhoTime && draft.timeB.length >= draft.tamanhoTime) {
    draft.status = 'Concluido'
    draft.proximoTime = null
  }
  return draft
}

function requireFakeDraft(id: string): Draft {
  const draft = fakeDrafts.find((current) => current.id === id)
  if (!draft) {
    throw new DraftServiceError(['Draft nao encontrado.'])
  }

  return draft
}

function normalizePayload<T extends object>(payload: T): T {
  return Object.fromEntries(Object.entries(payload).map(([key, value]) => [key, value === '' ? null : value])) as T
}

function toDraftServiceError(error: unknown): DraftServiceError {
  if (error instanceof AxiosError) {
    const data = error.response?.data as ApiErrorResponse | undefined
    if (Array.isArray(data?.errors) && data.errors.length > 0) {
      return new DraftServiceError(data.errors)
    }

    if (data?.message) {
      return new DraftServiceError([data.message])
    }
  }

  return new DraftServiceError(['Falha ao conectar ao servidor.'])
}

function isConnectionFailure(error: unknown): boolean {
  return error instanceof AxiosError && !error.response
}
