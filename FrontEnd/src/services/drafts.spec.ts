import { beforeEach, describe, expect, it, vi } from 'vitest'

import type { Draft } from '@/types/draft'

import { api } from './api'
import { createDraft, listDrafts, registerDraftPick } from './drafts'

vi.mock('./api', () => ({
  api: {
    get: vi.fn(),
    post: vi.fn(),
    patch: vi.fn(),
  },
}))

const draft: Draft = {
  id: 'draft-1',
  nome: 'Rinha',
  observacoes: null,
  status: 'Aberto',
  tamanhoTime: 5,
  criterioCapitaes: 'Manual',
  criterioPrimeiroPick: 'Manual',
  proximoTime: 'TimeA',
  capitaoTimeA: { id: 'player-1', nomeExibicao: 'Capitao A' },
  capitaoTimeB: { id: 'player-2', nomeExibicao: 'Capitao B' },
  timeA: [{ jogadorId: 'player-1', nomeExibicao: 'Capitao A', capitao: true }],
  timeB: [{ jogadorId: 'player-2', nomeExibicao: 'Capitao B', capitao: true }],
  disponiveis: [{ id: 'player-3', nomeExibicao: 'Jogador' }],
  escolhas: [],
  dataCadastro: '2026-06-19T00:00:00Z',
  dataAtualizacao: '2026-06-19T00:00:00Z',
}

describe('drafts service', () => {
  beforeEach(() => {
    vi.mocked(api.get).mockReset()
    vi.mocked(api.post).mockReset()
  })

  it('lists drafts using API filters', async () => {
    vi.mocked(api.get).mockResolvedValue({ data: { page: 1, pageSize: 100, totalItems: 1, totalPages: 1, items: [draft] } })

    const result = await listDrafts({ search: 'rinha', status: 'Aberto' })

    expect(api.get).toHaveBeenCalledWith('/api/v1/drafts', { params: { search: 'rinha', status: 'Aberto', page: 1, pageSize: 100 } })
    expect(result).toEqual([draft])
  })

  it('creates drafts and normalizes empty optional values', async () => {
    vi.mocked(api.post).mockResolvedValue({ data: draft })

    const result = await createDraft({
      nome: 'Rinha',
      observacoes: '',
      tamanhoTime: 5,
      sortearCapitaes: false,
      capitaoTimeAId: 'player-1',
      capitaoTimeBId: 'player-2',
      sortearPrimeiroPick: false,
      primeiroTime: 'TimeA',
      jogadoresIds: ['player-1', 'player-2', 'player-3'],
    })

    expect(api.post).toHaveBeenCalledWith('/api/v1/drafts', {
      nome: 'Rinha',
      observacoes: null,
      tamanhoTime: 5,
      sortearCapitaes: false,
      capitaoTimeAId: 'player-1',
      capitaoTimeBId: 'player-2',
      sortearPrimeiroPick: false,
      primeiroTime: 'TimeA',
      jogadoresIds: ['player-1', 'player-2', 'player-3'],
    })
    expect(result).toBe(draft)
  })

  it('registers draft pick', async () => {
    vi.mocked(api.post).mockResolvedValue({ data: draft })

    await registerDraftPick('draft-1', 'player-3')

    expect(api.post).toHaveBeenCalledWith('/api/v1/drafts/draft-1/picks', { jogadorId: 'player-3' })
  })
})
