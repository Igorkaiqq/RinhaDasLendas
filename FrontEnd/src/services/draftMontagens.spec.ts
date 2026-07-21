import { beforeEach, describe, expect, it, vi } from 'vitest'

import type { DraftMontagem } from '@/types/draftMontagem'

import { api } from './api'
import { addManualDraftMontagemPresence, cancelDraftMontagem, getDraftMontagemById, listDraftMontagens, listEligibleManualPresencePlayers, removeManualDraftMontagemPresence, republishDraftMontagemDiscordPublication } from './draftMontagens'
import { resolveInitialDraftId } from './draftRoute'

vi.mock('./api', () => ({
  api: {
    get: vi.fn(),
    post: vi.fn(),
    delete: vi.fn(),
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
    vi.mocked(api.post).mockReset()
    vi.mocked(api.delete).mockReset()
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

  it('adds manual presence to a visual draft assembly with reason', async () => {
    vi.mocked(api.post).mockResolvedValue({ data: montagem })

    await addManualDraftMontagemPresence('montagem-1', 'jogador-1', 'convidado pelo organizador')

    expect(api.post).toHaveBeenCalledWith('/api/v1/draft-montagens/montagem-1/presencas/manual', {
      jogadorId: 'jogador-1',
      motivo: 'convidado pelo organizador',
    })
  })

  it('removes manual presence from a visual draft assembly', async () => {
    vi.mocked(api.delete).mockResolvedValue({ data: montagem })

    await removeManualDraftMontagemPresence('montagem-1', 'jogador-1', 'não poderá jogar')

    expect(api.delete).toHaveBeenCalledWith('/api/v1/draft-montagens/montagem-1/presencas/jogador-1', { data: { motivo: 'não poderá jogar' } })
  })

  it('searches players eligible for manual presence without loading all players', async () => {
    vi.mocked(api.get).mockResolvedValue({ data: { page: 1, pageSize: 20, totalItems: 1, totalPages: 1, items: [{ id: 'jogador-1', nomeExibicao: 'Player 1' }] } })

    const result = await listEligibleManualPresencePlayers('montagem-1', 'Player')

    expect(api.get).toHaveBeenCalledWith('/api/v1/draft-montagens/montagem-1/presencas/elegiveis', { params: { search: 'Player', page: 1, pageSize: 20 } })
    expect(result).toEqual([{ id: 'jogador-1', nomeExibicao: 'Player 1' }])
  })

  it('requests Discord publication republish with type and reason', async () => {
    vi.mocked(api.post).mockResolvedValue({ data: montagem })

    await republishDraftMontagemDiscordPublication('montagem-1', 'TimesDefinidos', 'permissão corrigida')

    expect(api.post).toHaveBeenCalledWith('/api/v1/draft-montagens/montagem-1/discord/publicacoes/republicar', { tipo: 'TimesDefinidos', motivo: 'permissão corrigida' })
  })
})

describe('draftRoute helpers', () => {
  it('resolves draftId from a route query string value', () => {
    expect(resolveInitialDraftId('montagem-1')).toBe('montagem-1')
  })

  it('uses the first draftId when route query contains an array', () => {
    expect(resolveInitialDraftId(['montagem-1', 'montagem-2'])).toBe('montagem-1')
  })

  it('ignores empty draftId values', () => {
    expect(resolveInitialDraftId('   ')).toBeNull()
  })
})
