import { AxiosError } from 'axios'

import type { LeagueRoleValue } from '@/constants/leagueRoles'
import { MessageCode } from '@/constants/messageCode'
import type { PlayerStatusValue } from '@/constants/playerStatus'

import { api } from './api'
import { getMessage } from './messageService'
import {
  createFakePlayer,
  deleteFakePlayer,
  inactivateFakePlayer,
  listFakePlayers,
  updateFakePlayerBasics,
  updateFakeRoutePreferences,
} from './fakePlayers'

export type RouteName = LeagueRoleValue

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
  status: PlayerStatusValue
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
    super(errors[0] ?? getMessage(MessageCode.RequestProcessingFailed))
  }
}

interface ApiMessageErrorResponse {
  messageCode?: string
  message?: string
  errors?: string[]
}

export async function listPlayers(somenteAtivos = false): Promise<Player[]> {
  try {
    const response = await api.get<PaginatedPlayers>('/api/v1/jogadores', {
      params: { somenteAtivos, page: 1, pageSize: 100 },
    })
    return response.data.items
  } catch {
    return listFakePlayers(somenteAtivos)
  }
}

export async function listEligibleCaptains(): Promise<Player[]> {
  try {
    const response = await api.get<PaginatedPlayers>('/api/v1/jogadores/capitaes-elegiveis', {
      params: { page: 1, pageSize: 100 },
    })
    return response.data.items
  } catch {
    return []
  }
}

export async function createPlayer(payload: PlayerPayload): Promise<Player> {
  try {
    const response = await api.post<Player>('/api/v1/jogadores', normalizePayload(payload))
    return response.data
  } catch (error) {
    if (isConnectionFailure(error)) {
      return createFakePlayer(payload)
    }

    throw toPlayerServiceError(error)
  }
}

export async function updatePlayerBasics(id: string, payload: PlayerUpdatePayload): Promise<Player> {
  try {
    const response = await api.put<Player>(`/api/v1/jogadores/${id}/dados-basicos`, normalizePayload(payload))
    return response.data
  } catch (error) {
    if (isConnectionFailure(error)) {
      return updateFakePlayerBasics(id, payload)
    }

    throw toPlayerServiceError(error)
  }
}

export async function updateRoutePreferences(id: string, preferencias: RoutePreference[]): Promise<Player> {
  try {
    const response = await api.put<Player>(`/api/v1/jogadores/${id}/preferencias-rotas`, { preferencias })
    return response.data
  } catch (error) {
    if (isConnectionFailure(error)) {
      return updateFakeRoutePreferences(id, preferencias)
    }

    throw toPlayerServiceError(error)
  }
}

export async function inactivatePlayer(id: string): Promise<void> {
  try {
    await api.patch(`/api/v1/jogadores/${id}/inativar`)
  } catch (error) {
    if (isConnectionFailure(error)) {
      inactivateFakePlayer(id)
      return
    }

    throw toPlayerServiceError(error)
  }
}

export async function deletePlayer(id: string): Promise<void> {
  try {
    await api.delete(`/api/v1/jogadores/${id}`)
  } catch (error) {
    if (isConnectionFailure(error)) {
      deleteFakePlayer(id)
      return
    }

    await inactivatePlayer(id)
  }
}

function normalizePayload<T extends object>(payload: T): T {
  return Object.fromEntries(
    Object.entries(payload).map(([key, value]) => [key, value === '' ? null : value]),
  ) as T
}

function toPlayerServiceError(error: unknown): PlayerServiceError {
  if (error instanceof AxiosError) {
    const data = error.response?.data as ApiMessageErrorResponse | undefined
    const errors = data?.errors
    if (Array.isArray(errors) && errors.length > 0) {
      return new PlayerServiceError(errors)
    }

    if (data?.messageCode) {
      return new PlayerServiceError([getMessage(data.messageCode)])
    }
  }

  return new PlayerServiceError([getMessage(MessageCode.ServerConnectionFailed)])
}

function isConnectionFailure(error: unknown): boolean {
  return error instanceof AxiosError && !error.response
}
