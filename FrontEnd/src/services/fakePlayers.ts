import { LeagueRole } from '@/constants/leagueRoles'
import { MessageCode } from '@/constants/messageCode'
import { PlayerStatus } from '@/constants/playerStatus'

import type { Divisao, Elo, Player, PlayerPayload, PlayerUpdatePayload, RoutePreference } from './players'
import { getMessage } from './messageService'

const defaultPreferences = (primary: RoutePreference['rota']): RoutePreference[] => {
  const order: RoutePreference['rota'][] = [primary, LeagueRole.Jungle, LeagueRole.Mid, LeagueRole.Adc, LeagueRole.Support, LeagueRole.Top]
  const uniqueOrder = [...new Set(order)].slice(0, 5)

  return uniqueOrder.map((rota, index) => ({
    rota,
    prioridade: index + 1,
    naoJogoNemLascando: index === 4,
  }))
}

const now = '2026-06-09T00:00:00.000Z'

let fakePlayers: Player[] = [
  {
    id: 'fake-hide-on-bush',
    nomeExibicao: 'Hide on bush',
    discord: 'faker#0001',
    riotId: 'Hide on bush#KR1',
    opGgUrl: 'https://www.op.gg/summoners/kr/Hide%20on%20bush-KR1',
    deepLolUrl: null,
    elo: 'Desafiante' as Elo,
    divisao: null,
    status: PlayerStatus.Active,
    dataCadastro: now,
    dataAtualizacao: now,
    preferencias: defaultPreferences(LeagueRole.Mid),
  },
  {
    id: 'fake-canyon',
    nomeExibicao: 'Canyon',
    discord: 'canyon#7777',
    riotId: 'JUGKING#KR1',
    opGgUrl: 'https://www.op.gg/',
    deepLolUrl: null,
    elo: 'Mestre' as Elo,
    divisao: null,
    status: PlayerStatus.Active,
    dataCadastro: now,
    dataAtualizacao: now,
    preferencias: defaultPreferences(LeagueRole.Jungle),
  },
  {
    id: 'fake-zeus',
    nomeExibicao: 'Zeus',
    discord: 'zeus#1004',
    riotId: 'Zeus#KR1',
    opGgUrl: null,
    deepLolUrl: null,
    elo: 'Diamante' as Elo,
    divisao: 'I' as Divisao,
    status: PlayerStatus.Active,
    dataCadastro: now,
    dataAtualizacao: now,
    preferencias: defaultPreferences(LeagueRole.Top),
  },
  {
    id: 'fake-ruler',
    nomeExibicao: 'Ruler',
    discord: 'ruler#2026',
    riotId: 'Ruler#KR1',
    opGgUrl: null,
    deepLolUrl: null,
    elo: 'Desafiante' as Elo,
    divisao: null,
    status: PlayerStatus.Inactive,
    dataCadastro: now,
    dataAtualizacao: now,
    preferencias: defaultPreferences(LeagueRole.Adc),
  },
]

export function listFakePlayers(somenteAtivos = false): Player[] {
  return fakePlayers.filter((player) => !somenteAtivos || player.status === PlayerStatus.Active)
}

export function createFakePlayer(payload: PlayerPayload): Player {
  validateFakePlayer(payload.nomeExibicao, payload.discord, payload.preferencias)

  const player: Player = {
    id: `fake-${Date.now()}`,
    nomeExibicao: payload.nomeExibicao.trim(),
    discord: payload.discord?.trim() || null,
    riotId: payload.riotId?.trim() || null,
    opGgUrl: payload.opGgUrl?.trim() || null,
    deepLolUrl: payload.deepLolUrl?.trim() || null,
    elo: payload.elo ?? null,
    divisao: payload.divisao ?? null,
    status: PlayerStatus.Active,
    dataCadastro: new Date().toISOString(),
    dataAtualizacao: new Date().toISOString(),
    preferencias: [...payload.preferencias],
  }

  fakePlayers = [player, ...fakePlayers]
  return player
}

export function updateFakePlayerBasics(id: string, payload: PlayerUpdatePayload): Player {
  const current = getFakePlayer(id)
  validateFakePlayer(payload.nomeExibicao, payload.discord, current.preferencias)

  const updated: Player = {
    ...current,
    nomeExibicao: payload.nomeExibicao.trim(),
    nomeReal: payload.nomeReal?.trim() || null,
    discord: payload.discord?.trim() || null,
    riotId: payload.riotId?.trim() || null,
    opGgUrl: payload.opGgUrl?.trim() || null,
    deepLolUrl: payload.deepLolUrl?.trim() || null,
    elo: payload.elo ?? null,
    divisao: payload.divisao ?? null,
    dataAtualizacao: new Date().toISOString(),
  }

  fakePlayers = fakePlayers.map((player) => (player.id === id ? updated : player))
  return updated
}

export function updateFakeRoutePreferences(id: string, preferencias: RoutePreference[]): Player {
  const current = getFakePlayer(id)
  validatePreferences(preferencias)

  const updated = {
    ...current,
    preferencias: [...preferencias],
    dataAtualizacao: new Date().toISOString(),
  }

  fakePlayers = fakePlayers.map((player) => (player.id === id ? updated : player))
  return updated
}

export function inactivateFakePlayer(id: string): void {
  const current = getFakePlayer(id)
  fakePlayers = fakePlayers.map((player) =>
    player.id === id ? { ...current, status: PlayerStatus.Inactive, dataAtualizacao: new Date().toISOString() } : player,
  )
}

export function deleteFakePlayer(id: string): void {
  getFakePlayer(id)
  fakePlayers = fakePlayers.filter((player) => player.id !== id)
}

function getFakePlayer(id: string): Player {
  const player = fakePlayers.find((current) => current.id === id)

  if (!player) {
    throw new Error(getMessage(MessageCode.PlayerNotFound))
  }

  return player
}

function validateFakePlayer(nomeExibicao: string, discord: string | null | undefined, preferencias: RoutePreference[]) {
  const errors: string[] = []

  if (!nomeExibicao.trim()) {
    errors.push(getMessage(MessageCode.PlayerNameRequired))
  }

  if (!discord?.trim()) {
    errors.push(getMessage(MessageCode.FieldRequired))
  }

  errors.push(...validatePreferences(preferencias, false))

  if (errors.length > 0) {
    throw new Error(errors[0] ?? getMessage(MessageCode.RequestProcessingFailed))
  }
}

function validatePreferences(preferencias: RoutePreference[], shouldThrow = true): string[] {
  const errors: string[] = []
  const priorities = preferencias.map((preference) => preference.prioridade)

  if (preferencias.length !== 5) {
    errors.push(getMessage(MessageCode.IncompleteRoutePreferences))
  }

  if (new Set(priorities).size !== 5) {
    errors.push(getMessage(MessageCode.RoutePrioritiesMustBeUnique))
  }

  if (preferencias.filter((preference) => preference.naoJogoNemLascando).length > 1) {
    errors.push(getMessage(MessageCode.OnlyOneNeverPlayRole))
  }

  if (errors.length > 0 && shouldThrow) {
    throw new Error(errors[0] ?? getMessage(MessageCode.RequestProcessingFailed))
  }

  return errors
}
