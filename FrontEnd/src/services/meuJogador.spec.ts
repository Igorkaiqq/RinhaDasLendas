import { beforeEach, describe, expect, it, vi } from 'vitest'

import { api } from './api'
import { completeMeuJogadorProfile, getMeuJogadorProfile, updateMeuJogadorProfile } from './meuJogador'

import { Divisao, Elo, type Player } from './players'
import type { MeuJogadorProfilePayload } from '@/types/meuJogador'

vi.mock('./api', () => ({
  api: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
  },
}))

const payload: MeuJogadorProfilePayload = {
  nomeExibicao: 'Novo Player',
  discord: 'novo#1234',
  riotId: 'Novo#BR1',
  opGgUrl: 'https://www.op.gg/summoners/br/Novo-BR1',
  deepLolUrl: 'https://www.deeplol.gg/summoner/br/Novo-BR1',
  elo: Elo.Ouro,
  divisao: Divisao.IV,
  preferencias: [
    { rota: 'Top', prioridade: 1, naoJogoNemLascando: false },
    { rota: 'Jungle', prioridade: 2, naoJogoNemLascando: false },
    { rota: 'Mid', prioridade: 3, naoJogoNemLascando: false },
    { rota: 'Adc', prioridade: 4, naoJogoNemLascando: false },
    { rota: 'Support', prioridade: 5, naoJogoNemLascando: true },
  ],
}

const jogador: Player = {
  id: 'jogador-1',
  nomeExibicao: payload.nomeExibicao,
  discord: payload.discord,
  riotId: payload.riotId,
  opGgUrl: payload.opGgUrl,
  deepLolUrl: payload.deepLolUrl,
  elo: payload.elo,
  divisao: payload.divisao,
  status: 'Ativo',
  dataCadastro: '2026-06-21T00:00:00Z',
  dataAtualizacao: '2026-06-21T00:00:00Z',
  preferencias: payload.preferencias,
}

describe('meuJogador service', () => {
  beforeEach(() => {
    vi.mocked(api.get).mockReset()
    vi.mocked(api.post).mockReset()
    vi.mocked(api.put).mockReset()
  })

  it('returns null when current user has no linked player profile', async () => {
    vi.mocked(api.get).mockRejectedValue({ response: { status: 404 } })

    await expect(getMeuJogadorProfile()).resolves.toBeNull()
  })

  it('completes current user player profile', async () => {
    vi.mocked(api.post).mockResolvedValue({ data: jogador })

    const result = await completeMeuJogadorProfile(payload)

    expect(api.post).toHaveBeenCalledWith('/api/v1/auth/me/jogador', payload)
    expect(result).toBe(jogador)
  })

  it('updates current user player profile', async () => {
    vi.mocked(api.put).mockResolvedValue({ data: jogador })

    const result = await updateMeuJogadorProfile(payload)

    expect(api.put).toHaveBeenCalledWith('/api/v1/auth/me/jogador', payload)
    expect(result).toBe(jogador)
  })
})
