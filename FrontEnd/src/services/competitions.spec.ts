import { AxiosError, AxiosHeaders } from 'axios'
import { beforeEach, describe, expect, expectTypeOf, it, vi } from 'vitest'

import type { UpdateCompetitionRequest } from '@/types/competition'

import { api } from './api'
import {
  createCompetition,
  createRound,
  getCompetition,
  listCompetitions,
  listCompetitionsForSeason,
  listRounds,
  publishCompetitionRules,
  publishSeasonRules,
  reorderRounds,
  updateCompetition,
} from './competitions'

vi.mock('./api', () => ({
  api: {
    get: vi.fn(),
    post: vi.fn(),
    patch: vi.fn(),
  },
}))

const seasonId = '00000000-0000-4000-8000-000000000001'
const competitionId = '00000000-0000-4000-8000-000000000002'
const competition = {
  id: competitionId,
  seasonId,
  nome: 'Circuito',
  codigo: 'CIRCUITO',
  circuitoDiario: true,
  rodadas: [],
  regrasPublicadas: [],
  versao: 1,
}
const competitionPage = {
  page: 1,
  pageSize: 20,
  items: [competition],
  totalItems: 1,
  totalPages: 1,
  calendarioConfigurado: true,
  seasonsIncluidas: [],
}
const payload = {
  nome: 'Circuito',
  codigo: 'CIRCUITO',
  circuitoDiario: true,
}

describe('competitions service', () => {
  beforeEach(() => vi.clearAllMocks())

  it('serializes repeated selected IDs and pagination in the collection URL params', async () => {
    vi.mocked(api.get).mockResolvedValue({ data: competitionPage, headers: {} })

    await listCompetitions(
      { mode: 'selected', seasonIds: [seasonId, competitionId] },
      2,
      10,
    )

    const params = vi.mocked(api.get).mock.calls[0]![1]!
      .params as URLSearchParams
    expect([...params]).toEqual([
      ['temporadaIds', seasonId],
      ['temporadaIds', competitionId],
      ['page', '2'],
      ['pageSize', '10'],
    ])
    expect(api.get).toHaveBeenCalledWith('/api/v1/competicoes', { params })
  })

  it('uses only todas=true for all Seasons', async () => {
    vi.mocked(api.get).mockResolvedValue({ data: competitionPage, headers: {} })

    await listCompetitions({ mode: 'all' })

    const params = vi.mocked(api.get).mock.calls[0]![1]!
      .params as URLSearchParams
    expect([...params]).toEqual([
      ['todas', 'true'],
      ['page', '1'],
      ['pageSize', '20'],
    ])
  })

  it('uses exact nested, detail and rounds URLs', async () => {
    vi.mocked(api.get)
      .mockResolvedValueOnce({ data: competitionPage, headers: {} })
      .mockResolvedValueOnce({
        data: competition,
        headers: { etag: '"competition"' },
      })
      .mockResolvedValueOnce({ data: [], headers: {} })

    await listCompetitionsForSeason('season/id', 2, 10)
    await expect(getCompetition('competition/id')).resolves.toEqual({
      data: competition,
      etag: '"competition"',
    })
    await listRounds('competition/id')

    expect(api.get).toHaveBeenNthCalledWith(
      1,
      '/api/v1/temporadas/season%2Fid/competicoes',
      { params: { page: 2, pageSize: 10 } },
    )
    expect(api.get).toHaveBeenNthCalledWith(
      2,
      '/api/v1/competicoes/competition%2Fid',
    )
    expect(api.get).toHaveBeenNthCalledWith(
      3,
      '/api/v1/competicoes/competition%2Fid/rodadas',
    )
  })

  it('preserves ETags from declared competition mutation responses', async () => {
    const round = {
      id: 'round-1',
      competicaoId: competitionId,
      nome: 'Rodada 1',
      ordem: 1,
      versao: 1,
    }
    const rules = {
      id: 'rules-1',
      seasonId,
      competicaoId: competitionId,
      numero: 1,
      formato: 'Md3' as const,
      modoDraft: 'Fearless' as const,
      publicadaEm: '2026-01-01T00:00:00Z',
    }
    vi.mocked(api.post)
      .mockResolvedValueOnce({
        data: competition,
        headers: { etag: '"created"' },
      })
      .mockResolvedValueOnce({ data: round, headers: { etag: '"round-created"' } })
      .mockResolvedValueOnce({ data: [], headers: { etag: '"reordered"' } })
      .mockResolvedValueOnce({ data: rules, headers: { etag: '"rules-published"' } })
    vi.mocked(api.patch).mockResolvedValue({
      data: competition,
      headers: { etag: '"updated"' },
    })

    await expect(
      createCompetition('season/id', payload, 'create-key'),
    ).resolves.toEqual({ data: competition, etag: '"created"' })
    await expect(
      updateCompetition(
        'competition/id',
        { nome: 'Novo nome' },
        '"old"',
        'update-key',
      ),
    ).resolves.toEqual({ data: competition, etag: '"updated"' })
    await expect(
      createRound(
        'competition/id',
        { nome: 'Rodada 1', ordem: 1 },
        '"old"',
        'round-key',
      ),
    ).resolves.toEqual({ data: round, etag: '"round-created"' })
    await expect(
      reorderRounds(
        'competition/id',
        { rodadaIds: ['round-1'] },
        '"old"',
        'reorder-key',
      ),
    ).resolves.toEqual({ data: [], etag: '"reordered"' })
    await expect(
      publishCompetitionRules(
        'competition/id',
        { formato: 'Md3', modoDraft: 'Fearless' },
        '"old"',
        'rules-key',
      ),
    ).resolves.toEqual({ data: rules, etag: '"rules-published"' })

    expect(api.post).toHaveBeenNthCalledWith(
      1,
      '/api/v1/temporadas/season%2Fid/competicoes',
      payload,
      { headers: { 'Idempotency-Key': 'create-key' } },
    )
    expect(api.patch).toHaveBeenCalledWith(
      '/api/v1/competicoes/competition%2Fid',
      { nome: 'Novo nome' },
      {
        headers: { 'Idempotency-Key': 'update-key', 'If-Match': '"old"' },
      },
    )
    expect(api.post).toHaveBeenNthCalledWith(
      3,
      '/api/v1/competicoes/competition%2Fid/ordenacoes-rodadas',
      { rodadaIds: ['round-1'] },
      {
        headers: { 'Idempotency-Key': 'reorder-key', 'If-Match': '"old"' },
      },
    )
    expect(api.post).toHaveBeenNthCalledWith(
      2,
      '/api/v1/competicoes/competition%2Fid/rodadas',
      { nome: 'Rodada 1', ordem: 1 },
      {
        headers: { 'Idempotency-Key': 'round-key', 'If-Match': '"old"' },
      },
    )
    expect(api.post).toHaveBeenNthCalledWith(
      4,
      '/api/v1/competicoes/competition%2Fid/regras-publicadas',
      { formato: 'Md3', modoDraft: 'Fearless' },
      {
        headers: { 'Idempotency-Key': 'rules-key', 'If-Match': '"old"' },
      },
    )
  })

  it('sends exact bodies and headers for Season rules', async () => {
    const rules = {
      id: 'rules-1',
      seasonId,
      numero: 1,
      formato: 'Md3' as const,
      modoDraft: 'Fearless' as const,
      publicadaEm: '2026-01-01T00:00:00Z',
    }
    const rulesPayload = {
      formato: 'Md3' as const,
      modoDraft: 'Fearless' as const,
    }
    vi.mocked(api.post)
      .mockResolvedValueOnce({
        data: rules,
        headers: { etag: '"season-rules-next"' },
      })

    await expect(
      publishSeasonRules(
        'season/id',
        rulesPayload,
        '"season"',
        'season-rules-key',
      ),
    ).resolves.toEqual({ data: rules, etag: '"season-rules-next"' })

    expect(api.post).toHaveBeenNthCalledWith(
      1,
      '/api/v1/temporadas/season%2Fid/regras-publicadas',
      rulesPayload,
      {
        headers: {
          'Idempotency-Key': 'season-rules-key',
          'If-Match': '"season"',
        },
      },
    )
  })

  it('maps the complete OpenAPI Axios error envelope', async () => {
    vi.mocked(api.get).mockRejectedValue(apiError(403))

    await expect(listCompetitions()).rejects.toMatchObject({
      status: 403,
      message: 'Forbidden',
      messageCode: 'MA002',
      errors: ['Capability missing'],
      fieldErrors: [],
    })
  })

  it('does not invent a user-visible fallback for an unknown Axios error', async () => {
    vi.mocked(api.get).mockRejectedValue(new AxiosError('network failure'))

    await expect(listCompetitions()).rejects.toMatchObject({
      status: undefined,
      message: '',
      messageCode: undefined,
      errors: [],
    })
  })

  it('requires at least one Competition PATCH field at compile time', () => {
    expectTypeOf<
      Record<PropertyKey, never>
    >().not.toMatchTypeOf<UpdateCompetitionRequest>()
    expectTypeOf<{ codigo: string }>().toMatchTypeOf<UpdateCompetitionRequest>()
    expectTypeOf<{ nome: null }>().not.toMatchTypeOf<UpdateCompetitionRequest>()
  })
})

function apiError(status: number) {
  return new AxiosError(
    'request failed',
    'ERR_BAD_RESPONSE',
    undefined,
    undefined,
    {
      status,
      statusText: 'Error',
      headers: new AxiosHeaders(),
      config: { headers: new AxiosHeaders() },
      data: {
        message: 'Forbidden',
        messageCode: 'MA002',
        errors: ['Capability missing'],
      },
    },
  )
}
