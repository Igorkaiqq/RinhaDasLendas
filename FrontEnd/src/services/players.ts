import { AxiosError } from 'axios'

import { api } from './api'

export type RouteName = 'Top' | 'Jungle' | 'Mid' | 'Adc' | 'Support'

export enum Elo {
  Ferro = 'Ferro',
  Bronze = 'Bronze',
  Prata = 'Prata',
  Ouro = 'Ouro',
  Platina = 'Platina',
  Esmeralda = 'Esmeralda',
  Diamante = 'Diamante',
  Mestre = 'Mestre',
  GraoMestre = 'Grão-Mestre',
  Desafiante = 'Desafiante',
}

export enum Divisao {
  IV = 'IV',
  III = 'III',
  II = 'II',
  I = 'I',
}

export interface RoutePreference {
  rota: RouteName
  prioridade: number
  naoJogoNemLascando: boolean
}

export interface PlayerPayload {
  nomeExibicao: string
  discord?: string | null
  riotId?: string | null
  opGgUrl?: string | null
  deepLolUrl?: string | null
  elo?: Elo | null
  divisao?: Divisao | null
  preferencias: RoutePreference[]
}

export interface PlayerUpdatePayload {
  nomeExibicao: string
  nomeReal?: string | null
  discord?: string | null
  riotId?: string | null
  opGgUrl?: string | null
  deepLolUrl?: string | null
  elo?: Elo | null
  divisao?: Divisao | null
}

export interface Player {
  id: string
  nomeExibicao: string
  nomeReal?: string | null
  discord?: string | null
  riotId?: string | null
  opGgUrl?: string | null
  deepLolUrl?: string | null
  elo?: Elo | null
  divisao?: Divisao | null
  status: 'Ativo' | 'Inativo'
  dataCadastro: string
  dataAtualizacao: string
  preferencias: RoutePreference[]
}

export interface PaginatedPlayers {
  page: number
  pageSize: number
  items: Player[]
}

export class PlayerServiceError extends Error {
  constructor(public readonly errors: string[]) {
    super(errors[0] ?? 'Nao foi possivel concluir a acao.')
  }
}

export async function listPlayers(somenteAtivos = false): Promise<Player[]> {
  const response = await api.get<PaginatedPlayers>('/api/v1/jogadores', {
    params: { somenteAtivos, page: 1, pageSize: 100 },
  })
  return response.data.items
}

export async function createPlayer(payload: PlayerPayload): Promise<Player> {
  try {
    const response = await api.post<Player>('/api/v1/jogadores', normalizePayload(payload))
    return response.data
  } catch (error) {
    throw toPlayerServiceError(error)
  }
}

export async function updatePlayerBasics(id: string, payload: PlayerUpdatePayload): Promise<Player> {
  try {
    const response = await api.put<Player>(`/api/v1/jogadores/${id}/dados-basicos`, normalizePayload(payload))
    return response.data
  } catch (error) {
    throw toPlayerServiceError(error)
  }
}

export async function updateRoutePreferences(id: string, preferencias: RoutePreference[]): Promise<Player> {
  try {
    const response = await api.put<Player>(`/api/v1/jogadores/${id}/preferencias-rotas`, { preferencias })
    return response.data
  } catch (error) {
    throw toPlayerServiceError(error)
  }
}

export async function inactivatePlayer(id: string): Promise<void> {
  try {
    await api.patch(`/api/v1/jogadores/${id}/inativar`)
  } catch (error) {
    throw toPlayerServiceError(error)
  }
}

function normalizePayload<T extends object>(payload: T): T {
  return Object.fromEntries(
    Object.entries(payload).map(([key, value]) => [key, value === '' ? null : value]),
  ) as T
}

function toPlayerServiceError(error: unknown): PlayerServiceError {
  if (error instanceof AxiosError) {
    const errors = error.response?.data?.errors
    if (Array.isArray(errors) && errors.length > 0) {
      return new PlayerServiceError(errors)
    }
  }

  return new PlayerServiceError(['Nao foi possivel conectar com a API de jogadores.'])
}
