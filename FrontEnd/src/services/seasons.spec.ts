import { AxiosError, AxiosHeaders } from 'axios'
import { beforeEach, describe, expect, expectTypeOf, it, vi } from 'vitest'

import type { UpdateSeasonRequest } from '@/types/season'

import { api } from './api'
import {
  activateSeason,
  closeSeason,
  createSeason,
  getSeason,
  listSeasons,
  SeasonScopeValidationError,
  serializeSeasonScope,
  updateSeason,
} from './seasons'

vi.mock('./api', () => ({
  api: {
    get: vi.fn(),
    post: vi.fn(),
    patch: vi.fn(),
  },
}))

const seasonId = '00000000-0000-4000-8000-000000000001'
const otherSeasonId = '00000000-0000-4000-8000-000000000002'
const season = {
  id: seasonId,
  nome: 'Rinha 2026',
  ano: 2026,
  ordemNoAno: 1,
  dataInicio: '2026-01-01',
  dataFimExclusiva: '2027-01-01',
  estado: 'Ativa' as const,
  versao: 1,
  quantidadeCompeticoes: 0,
}
const seasonPage = {
  page: 1,
  pageSize: 20,
  items: [season],
  totalItems: 1,
  totalPages: 1,
  calendarioConfigurado: true,
  temporadaAtual: season,
  versaoCalendario: 3,
}
const payload = {
  nome: 'Rinha 2026',
  ano: 2026,
  ordemNoAno: 1,
  dataInicio: '2026-01-01',
  dataFimExclusiva: '2027-01-01',
}

describe('seasons service', () => {
  beforeEach(() => vi.clearAllMocks())

  it('serializes current, selected and all scopes exclusively', () => {
    expect([...serializeSeasonScope({ mode: 'current' })]).toEqual([])
    expect([
      ...serializeSeasonScope({
        mode: 'selected',
        seasonIds: [seasonId, otherSeasonId],
      }),
    ]).toEqual([
      ['temporadaIds', seasonId],
      ['temporadaIds', otherSeasonId],
    ])
    expect([...serializeSeasonScope({ mode: 'all' })]).toEqual([
      ['todas', 'true'],
    ])
  })

  it.each([
    { mode: 'selected', seasonIds: [] },
    { mode: 'selected', seasonIds: [seasonId, seasonId] },
    { mode: 'selected', seasonIds: [''] },
    { mode: 'selected', seasonIds: ['not-a-guid'] },
    {
      mode: 'selected',
      seasonIds: ['00000000-0000-0000-0000-000000000000'],
    },
  ])('rejects invalid selected scope %#', (scope) => {
    expect(() =>
      serializeSeasonScope(scope as Parameters<typeof serializeSeasonScope>[0]),
    ).toThrow(SeasonScopeValidationError)
  })

  it('lists and gets Seasons using exact params, encoded paths and ETags', async () => {
    vi.mocked(api.get)
      .mockResolvedValueOnce({
        data: seasonPage,
        headers: { etag: '"calendar-3"' },
      })
      .mockResolvedValueOnce({ data: season, headers: { etag: '"season-1"' } })

    await expect(
      listSeasons({ page: 2, pageSize: 10, estado: 'Ativa' }),
    ).resolves.toEqual({ data: seasonPage, etag: '"calendar-3"' })
    await expect(getSeason('season/id')).resolves.toEqual({
      data: season,
      etag: '"season-1"',
    })
    expect(api.get).toHaveBeenNthCalledWith(1, '/api/v1/temporadas', {
      params: { page: 2, pageSize: 10, estado: 'Ativa' },
    })
    expect(api.get).toHaveBeenNthCalledWith(2, '/api/v1/temporadas/season%2Fid')
  })

  it('preserves response ETags for every Season mutation', async () => {
    vi.mocked(api.post)
      .mockResolvedValueOnce({ data: season, headers: { etag: '"created"' } })
      .mockResolvedValueOnce({ data: season, headers: { etag: '"activated"' } })
      .mockResolvedValueOnce({ data: season, headers: { etag: '"closed"' } })
    vi.mocked(api.patch).mockResolvedValue({
      data: season,
      headers: { etag: '"updated"' },
    })

    await expect(createSeason(payload, 'create-key')).resolves.toEqual({
      data: season,
      etag: '"created"',
    })
    await expect(
      updateSeason('season/id', { nome: 'Nova Season' }, '"old"', 'update-key'),
    ).resolves.toEqual({ data: season, etag: '"updated"' })
    await expect(
      activateSeason('season/id', '"calendar"', 'activate-key'),
    ).resolves.toEqual({ data: season, etag: '"activated"' })
    await expect(
      closeSeason('season/id', '"calendar"', 'close-key'),
    ).resolves.toEqual({ data: season, etag: '"closed"' })

    expect(api.post).toHaveBeenNthCalledWith(1, '/api/v1/temporadas', payload, {
      headers: { 'Idempotency-Key': 'create-key' },
    })
    expect(api.patch).toHaveBeenCalledWith(
      '/api/v1/temporadas/season%2Fid',
      { nome: 'Nova Season' },
      {
        headers: { 'Idempotency-Key': 'update-key', 'If-Match': '"old"' },
      },
    )
    expect(api.post).toHaveBeenNthCalledWith(
      2,
      '/api/v1/temporadas/season%2Fid/aberturas',
      undefined,
      {
        headers: {
          'Idempotency-Key': 'activate-key',
          'If-Match-Calendar': '"calendar"',
        },
      },
    )
    expect(api.post).toHaveBeenNthCalledWith(
      3,
      '/api/v1/temporadas/season%2Fid/encerramentos',
      undefined,
      {
        headers: {
          'Idempotency-Key': 'close-key',
          'If-Match-Calendar': '"calendar"',
        },
      },
    )
  })

  it('maps the complete OpenAPI Axios error envelope', async () => {
    vi.mocked(api.get).mockRejectedValue(apiError(409))

    await expect(listSeasons()).rejects.toMatchObject({
      status: 409,
      message: 'Calendar changed',
      messageCode: 'MC103',
      errors: ['Conflict'],
      fieldErrors: [],
    })
  })

  it('maps the real optional fieldErrors contract and retains errors fallback', async () => {
    vi.mocked(api.post).mockRejectedValue(validationError())

    await expect(createSeason(payload)).rejects.toMatchObject({
      status: 400,
      messageCode: 'ME031',
      errors: ['Validation failed'],
      fieldErrors: [
        {
          field: 'Nome',
          messageCode: 'MV106',
          message: 'Season name is required.',
        },
        {
          field: 'DataFimExclusiva',
          messageCode: 'MV107',
          message: 'The Season period is invalid.',
        },
      ],
    })
  })

  it('does not invent a user-visible fallback for an unknown Axios error', async () => {
    vi.mocked(api.get).mockRejectedValue(new AxiosError('network failure'))

    await expect(listSeasons()).rejects.toMatchObject({
      status: undefined,
      message: '',
      messageCode: undefined,
      errors: [],
    })
  })

  it('requires at least one Season PATCH field at compile time', () => {
    expectTypeOf<
      Record<PropertyKey, never>
    >().not.toMatchTypeOf<UpdateSeasonRequest>()
    expectTypeOf<{ nome: string }>().toMatchTypeOf<UpdateSeasonRequest>()
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
        message: 'Calendar changed',
        messageCode: 'MC103',
        errors: ['Conflict'],
      },
    },
  )
}

function validationError() {
  return new AxiosError(
    'request failed',
    'ERR_BAD_RESPONSE',
    undefined,
    undefined,
    {
      status: 400,
      statusText: 'Bad Request',
      headers: new AxiosHeaders(),
      config: { headers: new AxiosHeaders() },
      data: {
        message: 'Validation failed',
        messageCode: 'ME031',
        errors: ['Validation failed'],
        fieldErrors: [
          {
            field: 'Nome',
            messageCode: 'MV106',
            message: 'Season name is required.',
          },
          {
            field: 'DataFimExclusiva',
            messageCode: 'MV107',
            message: 'The Season period is invalid.',
          },
        ],
      },
    },
  )
}
