import { beforeEach, describe, expect, it, vi } from 'vitest'

import { api } from './api'
import * as matchService from './matches'
import * as seriesService from './series'

vi.mock('./api', () => ({
  api: {
    get: vi.fn(),
    post: vi.fn(),
  },
}))

const seriesId = 'series/id'
const matchId = 'match/id'
const etag = '"series:opaque"'
const responseEtag = '"series:next:opaque"'
const reason = { justificativa: 'Revisão competitiva' }
const invalidates = [
  'series-detail',
  'series-scoreboard',
  'series-fearless',
  'match-detail',
] as const
const picks = {
  lados: [
    { ladoSerieId: 'side-1', championIds: [1, 2, 3, 4, 5] },
    { ladoSerieId: 'side-2', championIds: [6, 7, 8, 9, 10] },
  ],
} as const

describe('Series and Match services', () => {
  beforeEach(() => vi.clearAllMocks())

  it('covers every daily Series read route and observes only the detail ETag', async () => {
    const detail = { id: 'series-1' }
    const matches = { items: [] }
    const result = { serieId: 'series-1' }
    vi.mocked(api.get)
      .mockResolvedValueOnce({ data: detail, headers: { etag } })
      .mockResolvedValueOnce({ data: matches, headers: {} })
      .mockResolvedValueOnce({ data: result, headers: {} })

    await expect(seriesService.getSeries(seriesId)).resolves.toEqual({
      data: detail,
      etag,
    })
    await expect(seriesService.listMatches(seriesId, 2, 10)).resolves.toBe(
      matches,
    )
    await expect(seriesService.getSeriesResult(seriesId)).resolves.toBe(result)

    expect(api.get).toHaveBeenNthCalledWith(1, '/api/v1/series/series%2Fid')
    expect(api.get).toHaveBeenNthCalledWith(
      2,
      '/api/v1/series/series%2Fid/partidas',
      { params: { page: 2, pageSize: 10 } },
    )
    expect(api.get).toHaveBeenNthCalledWith(
      3,
      '/api/v1/series/series%2Fid/resultado',
    )
  })

  it('serializes current, selected and all Season scopes without bracketed keys', async () => {
    const page = { items: [] }
    const seasonOne = '00000000-0000-4000-8000-000000000001'
    const seasonTwo = '00000000-0000-4000-8000-000000000002'
    vi.mocked(api.get).mockResolvedValue({ data: page, headers: {} })

    await seriesService.listSeries({ page: 2, pageSize: 10 })
    await seriesService.listSeries({
      scope: { mode: 'selected', seasonIds: [seasonOne, seasonTwo] },
    })
    await seriesService.listSeries({ scope: { mode: 'all' } })

    expect([
      ...(vi.mocked(api.get).mock.calls[0]?.[1]?.params as URLSearchParams),
    ]).toEqual([
      ['page', '2'],
      ['pageSize', '10'],
    ])
    expect([
      ...(vi.mocked(api.get).mock.calls[1]?.[1]?.params as URLSearchParams),
    ]).toEqual([
      ['temporadaIds', seasonOne],
      ['temporadaIds', seasonTwo],
    ])
    expect([
      ...(vi.mocked(api.get).mock.calls[2]?.[1]?.params as URLSearchParams),
    ]).toEqual([['todas', 'true']])
  })

  it('uses a generated key and shared Series ETag for every Series mutation', async () => {
    vi.spyOn(globalThis.crypto, 'randomUUID').mockReturnValue(
      '11111111-1111-4111-8111-111111111111',
    )
    vi.mocked(api.post).mockResolvedValue({
      data: {},
      headers: { etag: responseEtag, 'idempotency-replayed': 'true' },
    })

    const results = [
      await seriesService.createSeries({
        seasonId: 'season-1',
        versaoRegrasId: 'rules-1',
        tipo: 'DiariaTemporaria',
        agendadaPara: '2026-06-01T22:00:00Z',
        ladoOrigemIds: ['side-1', 'side-2'],
      }),
      await seriesService.startSeries(seriesId, etag),
      await seriesService.cancelSeries(seriesId, reason, etag),
      await seriesService.annulSeries(seriesId, reason, etag),
      await seriesService.createMatch(seriesId, etag),
    ]

    for (const result of results) {
      expect(result).toEqual({
        data: {},
        etag: responseEtag,
        idempotencyReplayed: true,
        invalidates,
      })
    }

    expect(api.post).toHaveBeenNthCalledWith(
      1,
      '/api/v1/series',
      expect.any(Object),
      {
        headers: { 'Idempotency-Key': '11111111-1111-4111-8111-111111111111' },
      },
    )
    for (const call of vi.mocked(api.post).mock.calls.slice(1)) {
      expect(call[2]?.headers).toEqual({
        'If-Match': etag,
        'Idempotency-Key': '11111111-1111-4111-8111-111111111111',
      })
    }
    expect(vi.mocked(api.post).mock.calls.map(([path]) => path)).toEqual([
      '/api/v1/series',
      '/api/v1/series/series%2Fid/inicios',
      '/api/v1/series/series%2Fid/cancelamentos',
      '/api/v1/series/series%2Fid/anulacoes',
      '/api/v1/series/series%2Fid/partidas',
    ])
  })

  it('uses every Match route with the shared Series ETag and joint invalidation metadata', async () => {
    vi.mocked(api.get).mockResolvedValue({ data: {}, headers: { etag } })
    vi.mocked(api.post).mockResolvedValue({
      data: {},
      headers: { etag: responseEtag, 'idempotency-replayed': 'true' },
    })

    await expect(matchService.getMatch(matchId)).resolves.toEqual({
      data: {},
      etag,
    })
    const results = [
      await matchService.registerMatchPicks(
        matchId,
        picks,
        etag,
        'operation-key',
      ),
      await matchService.confirmMatchResult(
        matchId,
        { ladoVencedorId: 'side-1', motivoTermino: 'Normal' },
        etag,
        'operation-key',
      ),
      await matchService.registerMatchRemake(
        matchId,
        { decisaoPicks: 'PreservarPicks', justificativa: 'Falha técnica' },
        etag,
        'operation-key',
      ),
      await matchService.annulMatch(
        matchId,
        {
          justificativa: reason.justificativa,
          anularSerieSeInconclusiva: false,
        },
        etag,
        'operation-key',
      ),
      await matchService.correctMatch(
        matchId,
        { ...picks, justificativa: reason.justificativa },
        etag,
        'operation-key',
      ),
    ]

    for (const result of results) {
      expect(result).toEqual({
        data: {},
        etag: responseEtag,
        idempotencyReplayed: true,
        invalidates,
      })
    }

    expect(api.get).toHaveBeenCalledWith('/api/v1/partidas/match%2Fid')
    expect(vi.mocked(api.post).mock.calls.map(([path]) => path)).toEqual([
      '/api/v1/partidas/match%2Fid/picks',
      '/api/v1/partidas/match%2Fid/resultados',
      '/api/v1/partidas/match%2Fid/remakes',
      '/api/v1/partidas/match%2Fid/anulacoes',
      '/api/v1/partidas/match%2Fid/correcoes',
    ])
    for (const call of vi.mocked(api.post).mock.calls) {
      expect(call[2]?.headers).toEqual({
        'If-Match': etag,
        'Idempotency-Key': 'operation-key',
      })
    }
  })
})
