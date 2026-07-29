import { AxiosError } from 'axios'
import { beforeEach, describe, expect, it, vi } from 'vitest'

import type { DraftMontagem, DraftMontagemAdmin } from '@/types/draftMontagem'

import { api } from './api'
import { addManualDraftMontagemPresence, archiveDraftMontagem, cancelDraftMontagem, chooseDraftMontagemMode, createDraftMontagem, getDraftMontagemAdminById, getDraftMontagemArchivingById, getDraftMontagemById, listDraftMontagens, listEligibleManualPresencePlayers, removeManualDraftMontagemPresence, reopenDraftMontagemPresence, republishArchivedDraftCancellation, republishDraftMontagemDiscordPublication, restoreDraftMontagem } from './draftMontagens'
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
  cicloVersao: 'Legado',
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
  arquivado: false,
  versaoEstado: 7,
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

    expect(api.get).toHaveBeenCalledWith('/api/v1/draft-montagens', { params: { includeArchived: false, page: 1, pageSize: 100 } })
    expect(result).toEqual([montagem])
  })

  it('opens an existing visual draft assembly by id', async () => {
    vi.mocked(api.get).mockResolvedValue({ data: montagem })

    const result = await getDraftMontagemById('montagem-1')

    expect(api.get).toHaveBeenCalledWith('/api/v1/draft-montagens/montagem-1')
    expect(result).toBe(montagem)
  })

  it('loads the explicit administrative projection by id', async () => {
    const adminMontagem: DraftMontagemAdmin = { ...montagem, presencas: [], substituicoes: [], capitaesElegiveisIds: [], acoesAdministrativas: [], publicacoesDiscord: [] }
    vi.mocked(api.get).mockResolvedValue({ data: adminMontagem })

    const result = await getDraftMontagemAdminById('montagem-1')

    expect(api.get).toHaveBeenCalledWith('/api/v1/draft-montagens/montagem-1/administracao')
    expect(result).toBe(adminMontagem)
  })

  it('cancels visual draft assembly with reason', async () => {
    vi.mocked(api.patch).mockResolvedValue({ data: { ...montagem, status: 'Cancelada' } })

    await cancelDraftMontagem('montagem-1', 'motivo')

    expect(api.patch).toHaveBeenCalledWith('/api/v1/draft-montagens/montagem-1/cancelar', { motivo: 'motivo' })
  })

  it('chooses the operational mode through the dedicated endpoint', async () => {
    vi.mocked(api.patch).mockResolvedValue({ data: { ...montagem, modo: 'TempoReal' } })

    const result = await chooseDraftMontagemMode('draft/id', 'TempoReal')

    expect(api.patch).toHaveBeenCalledWith('/api/v1/draft-montagens/draft%2Fid/modo', { modo: 'TempoReal' })
    expect(result.modo).toBe('TempoReal')
  })

  it('keeps the compatibility captain fields neutral when creating a direct manual draft', async () => {
    vi.mocked(api.post).mockResolvedValue({ data: montagem })

    await createDraftMontagem({
      nome: 'Manual direto',
      tamanhoEquipe: 5,
      horarioEncerramentoPresenca: null,
      sortearCapitaes: false,
      capitaesIds: [],
      jogadoresIds: ['jogador-1'],
    })

    expect(api.post).toHaveBeenCalledWith('/api/v1/draft-montagens', {
      nome: 'Manual direto',
      tamanhoEquipe: 5,
      horarioEncerramentoPresenca: null,
      sortearCapitaes: false,
      capitaesIds: [],
      jogadoresIds: ['jogador-1'],
    })
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

  it('lists archived drafts only when explicitly requested', async () => {
    vi.mocked(api.get).mockResolvedValue({ data: { page: 1, pageSize: 100, totalItems: 0, totalPages: 0, items: [] } })

    await listDraftMontagens({ includeArchived: true })

    expect(api.get).toHaveBeenCalledWith('/api/v1/draft-montagens', {
      params: { includeArchived: true, page: 1, pageSize: 100 },
    })
  })

  it('loads every draft page sequentially with the same discovery filters', async () => {
    const firstPageItems = Array.from({ length: 100 }, (_, index) => ({ ...montagem, id: `montagem-${index + 1}` }))
    const montagemB = { ...montagem, id: 'montagem-101', nome: 'Rinha encontrada na pagina 2' }
    vi.mocked(api.get)
      .mockResolvedValueOnce({ data: { page: 1, pageSize: 100, totalItems: 101, totalPages: 2, items: firstPageItems } })
      .mockResolvedValueOnce({ data: { page: 2, pageSize: 100, totalItems: 101, totalPages: 2, items: [montagemB] } })

    const result = await listDraftMontagens({ search: 'Rinha', status: 'Cancelada', includeArchived: true })

    expect(api.get).toHaveBeenNthCalledWith(1, '/api/v1/draft-montagens', {
      params: { search: 'Rinha', status: 'Cancelada', includeArchived: true, page: 1, pageSize: 100 },
    })
    expect(api.get).toHaveBeenNthCalledWith(2, '/api/v1/draft-montagens', {
      params: { search: 'Rinha', status: 'Cancelada', includeArchived: true, page: 2, pageSize: 100 },
    })
    expect(result).toHaveLength(101)
    expect(result[result.length - 1]).toEqual(montagemB)
  })

  it('preserves a forbidden response raised while loading a later draft page', async () => {
    const forbidden = new AxiosError('Forbidden')
    Object.defineProperty(forbidden, 'response', { value: { status: 403, data: { errors: ['forbidden'] } } })
    vi.mocked(api.get)
      .mockResolvedValueOnce({ data: { page: 1, pageSize: 100, totalItems: 101, totalPages: 2, items: [montagem] } })
      .mockRejectedValueOnce(forbidden)

    await expect(listDraftMontagens({ includeArchived: true })).rejects.toMatchObject({
      status: 403,
      errors: ['forbidden'],
    })
    expect(api.get).toHaveBeenCalledTimes(2)
  })

  it('archives with a trimmed reason and observed state version', async () => {
    vi.mocked(api.patch).mockResolvedValue({ data: { id: montagem.id, status: 'Cancelada', arquivado: true, versaoEstado: 8 } })

    await archiveDraftMontagem('draft/id', '  organização concluída  ', 7)

    expect(api.patch).toHaveBeenCalledWith('/api/v1/draft-montagens/draft%2Fid/arquivar', {
      motivo: 'organização concluída',
      versaoEstado: 7,
    })
  })

  it('restores without a reason and retains the observed state version contract', async () => {
    vi.mocked(api.patch).mockResolvedValue({ data: { id: montagem.id, status: 'Cancelada', arquivado: false, versaoEstado: 9 } })

    await restoreDraftMontagem('draft/id', 8)

    expect(api.patch).toHaveBeenCalledWith('/api/v1/draft-montagens/draft%2Fid/restaurar', { versaoEstado: 8 })
  })

  it('reopens presence with an encoded id and no request body', async () => {
    const reopened = { ...montagem, status: 'PresencaAberta' as const }
    vi.mocked(api.patch).mockResolvedValue({ data: reopened })

    const result = await reopenDraftMontagemPresence('draft/id')

    expect(api.patch).toHaveBeenCalledWith('/api/v1/draft-montagens/draft%2Fid/reabrir-presenca')
    expect(result).toBe(reopened)
  })

  it('loads dedicated archive details and republishes archived cancellation', async () => {
    vi.mocked(api.get).mockResolvedValueOnce({ data: { draft: montagem, arquivadoEm: null, arquivadoPorUsuarioId: null, motivoArquivamento: null, acoes: [] } })
    vi.mocked(api.post).mockResolvedValueOnce({ data: { id: montagem.id, status: 'Cancelada', arquivado: true, versaoEstado: 9 } })

    await getDraftMontagemArchivingById('draft/id')
    await republishArchivedDraftCancellation('draft/id')

    expect(api.get).toHaveBeenCalledWith('/api/v1/draft-montagens/draft%2Fid/arquivamento')
    expect(api.post).toHaveBeenCalledWith('/api/v1/draft-montagens/draft%2Fid/discord/publicacoes/cancelamento/republicar')
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
