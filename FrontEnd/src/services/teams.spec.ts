import { beforeEach, describe, expect, it, vi } from 'vitest'

import type { Team } from '@/types/team'

import { api } from './api'
import { createTeam, listTeams } from './teams'

vi.mock('./api', () => ({
  api: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    patch: vi.fn(),
  },
}))

const team: Team = {
  id: 'team-1',
  nome: 'Os Sem Baron',
  tag: 'OSB',
  observacoes: null,
  status: 'Ativo',
  capitao: null,
  quantidadeJogadores: 1,
  membros: [{ jogadorId: 'player-1', nomeExibicao: 'Kaique', principal: true, capitao: false }],
  dataCadastro: '2026-06-19T00:00:00Z',
  dataAtualizacao: '2026-06-19T00:00:00Z',
}

describe('teams service', () => {
  beforeEach(() => {
    vi.mocked(api.get).mockReset()
    vi.mocked(api.post).mockReset()
  })

  it('lists teams using API filters', async () => {
    vi.mocked(api.get).mockResolvedValue({ data: { page: 1, pageSize: 100, totalItems: 1, totalPages: 1, items: [team] } })

    const result = await listTeams({ search: 'baron', status: 'Ativo' })

    expect(api.get).toHaveBeenCalledWith('/api/v1/times', { params: { search: 'baron', status: 'Ativo', page: 1, pageSize: 100 } })
    expect(result).toEqual([team])
  })

  it('creates teams and normalizes empty optional values', async () => {
    vi.mocked(api.post).mockResolvedValue({ data: team })

    const result = await createTeam({ nome: 'Os Sem Baron', tag: 'OSB', observacoes: '', capitaoId: '', jogadoresIds: ['player-1'] })

    expect(api.post).toHaveBeenCalledWith('/api/v1/times', {
      nome: 'Os Sem Baron',
      tag: 'OSB',
      observacoes: null,
      capitaoId: null,
      jogadoresIds: ['player-1'],
    })
    expect(result).toBe(team)
  })
})
