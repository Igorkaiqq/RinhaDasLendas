import { beforeEach, describe, expect, it, vi } from 'vitest'

import type { DraftMontagem } from '@/types/draftMontagem'

import { api } from './api'
import { cancelDraftMontagem, getDraftMontagemById, listDraftMontagens } from './draftMontagens'

vi.mock('./api', () => ({
  api: {
    get: vi.fn(),
    patch: vi.fn(),
  },
}))

const montagem: DraftMontagem = {
  id: 'montagem-1',
  nome: 'Rinha visual',
  observacoes: null,
  status: 'Aberta',
  modo: 'Manual',
  tamanhoEquipe: 5,
  quantidadeTimes: 2,
  quantidadeReservas: 1,
  criterioCapitaes: 'Sorteio',
  turnoAtualTimeId: null,
  turnoAtualCapitaoId: null,
  turnoSequencia: null,
  turnoIniciadoEm: null,
  turnoExpiraEm: null,
  duracaoTurnoSegundos: 30,
  presencaContinuadaManualmente: false,
  presencas: [],
  times: [],
  livres: [],
  reservas: [],
  escolhas: [],
  substituicoes: [],
  motivoCancelamento: null,
  dataCadastro: '2026-06-20T00:00:00Z',
  dataAtualizacao: '2026-06-20T00:00:00Z',
}

describe('draftMontagens service', () => {
  beforeEach(() => {
    vi.mocked(api.get).mockReset()
    vi.mocked(api.patch).mockReset()
  })

  it('lists visual draft assemblies', async () => {
    vi.mocked(api.get).mockResolvedValue({ data: { page: 1, pageSize: 100, totalItems: 1, totalPages: 1, items: [montagem] } })

    const result = await listDraftMontagens()

    expect(api.get).toHaveBeenCalledWith('/api/v1/draft-montagens', { params: { page: 1, pageSize: 100 } })
    expect(result).toEqual([montagem])
  })

  it('opens an existing visual draft assembly by id', async () => {
    vi.mocked(api.get).mockResolvedValue({ data: montagem })

    const result = await getDraftMontagemById('montagem-1')

    expect(api.get).toHaveBeenCalledWith('/api/v1/draft-montagens/montagem-1')
    expect(result).toBe(montagem)
  })

  it('cancels visual draft assembly with reason', async () => {
    vi.mocked(api.patch).mockResolvedValue({ data: { ...montagem, status: 'Cancelada' } })

    await cancelDraftMontagem('montagem-1', 'motivo')

    expect(api.patch).toHaveBeenCalledWith('/api/v1/draft-montagens/montagem-1/cancelar', { motivo: 'motivo' })
  })
})
