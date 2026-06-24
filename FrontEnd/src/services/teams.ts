import { AxiosError } from 'axios'

import { MessageCode } from '@/constants/messageCode'
import type { Team, TeamFilters, TeamPayload, TeamStatusValue } from '@/types/team'

import { api } from './api'
import { getMessage } from './messageService'

interface PaginatedTeams {
  page: number
  pageSize: number
  items: Team[]
  totalItems: number
  totalPages: number
}

interface ApiErrorResponse {
  message?: string
  errors?: string[]
}

export class TeamServiceError extends Error {
  constructor(public readonly errors: string[]) {
    super(errors[0] ?? getMessage(MessageCode.TeamSaveFailed))
  }
}

const fakeTeams: Team[] = []

export async function listTeams(filters: TeamFilters = {}): Promise<Team[]> {
  try {
    const response = await api.get<PaginatedTeams>('/api/v1/times', {
      params: { ...filters, page: 1, pageSize: 100 },
    })
    return response.data.items
  } catch (error) {
    if (isConnectionFailure(error)) {
      return filterFakeTeams(filters)
    }

    throw toTeamServiceError(error)
  }
}

export async function createTeam(payload: TeamPayload): Promise<Team> {
  try {
    const response = await api.post<Team>('/api/v1/times', normalizePayload(payload))
    return response.data
  } catch (error) {
    if (isConnectionFailure(error)) {
      const created = createFakeTeam(payload)
      fakeTeams.unshift(created)
      return created
    }

    throw toTeamServiceError(error)
  }
}

export async function updateTeam(id: string, payload: TeamPayload): Promise<Team> {
  try {
    const response = await api.put<Team>(`/api/v1/times/${id}`, normalizePayload(payload))
    return response.data
  } catch (error) {
    if (isConnectionFailure(error)) {
      const updated = createFakeTeam(payload, id)
      const index = fakeTeams.findIndex((team) => team.id === id)
      if (index >= 0) {
        fakeTeams[index] = updated
      }
      return updated
    }

    throw toTeamServiceError(error)
  }
}

export async function inactivateTeam(id: string): Promise<Team> {
  try {
    const response = await api.patch<Team>(`/api/v1/times/${id}/inativar`)
    return response.data
  } catch (error) {
    if (isConnectionFailure(error)) {
      return updateFakeStatus(id, 'Inativo')
    }

    throw toTeamServiceError(error)
  }
}

export async function reactivateTeam(id: string): Promise<Team> {
  try {
    const response = await api.patch<Team>(`/api/v1/times/${id}/reativar`)
    return response.data
  } catch (error) {
    if (isConnectionFailure(error)) {
      return updateFakeStatus(id, 'Ativo')
    }

    throw toTeamServiceError(error)
  }
}

function filterFakeTeams(filters: TeamFilters): Team[] {
  const search = filters.search?.trim().toLowerCase()
  return fakeTeams.filter((team) => {
    const matchesStatus = !filters.status || team.status === filters.status
    const matchesSearch =
      !search ||
      team.nome.toLowerCase().includes(search) ||
      team.tag.toLowerCase().includes(search) ||
      team.membros.some((member) => member.nomeExibicao.toLowerCase().includes(search))

    return matchesStatus && matchesSearch
  })
}

function createFakeTeam(payload: TeamPayload, id: string = crypto.randomUUID()): Team {
  const now = new Date().toISOString()
  const membros = payload.jogadoresIds.map((jogadorId) => ({
    jogadorId,
    nomeExibicao: `Jogador ${jogadorId.slice(0, 4)}`,
    principal: true,
    capitao: payload.capitaoId === jogadorId,
  }))
  const capitao = membros.find((member) => member.capitao)

  return {
    id,
    nome: payload.nome,
    tag: payload.tag.toUpperCase(),
    observacoes: payload.observacoes,
    status: 'Ativo',
    capitao: capitao ? { id: capitao.jogadorId, nomeExibicao: capitao.nomeExibicao } : null,
    quantidadeJogadores: membros.length,
    membros,
    dataCadastro: now,
    dataAtualizacao: now,
  }
}

function updateFakeStatus(id: string, status: TeamStatusValue): Team {
  const team = fakeTeams.find((current) => current.id === id)
  if (!team) {
    throw new TeamServiceError([getMessage(MessageCode.TeamNotFound)])
  }

  team.status = status
  team.dataAtualizacao = new Date().toISOString()
  return team
}

function normalizePayload<T extends object>(payload: T): T {
  return Object.fromEntries(Object.entries(payload).map(([key, value]) => [key, value === '' ? null : value])) as T
}

function toTeamServiceError(error: unknown): TeamServiceError {
  if (error instanceof AxiosError) {
    const data = error.response?.data as ApiErrorResponse | undefined
    if (Array.isArray(data?.errors) && data.errors.length > 0) {
      return new TeamServiceError(data.errors)
    }

    if (data?.message) {
      return new TeamServiceError([data.message])
    }
  }

  return new TeamServiceError([getMessage(MessageCode.ServerConnectionFailed)])
}

function isConnectionFailure(error: unknown): boolean {
  return error instanceof AxiosError && !error.response
}
