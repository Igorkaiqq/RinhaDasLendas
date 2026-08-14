import { AxiosError } from 'axios'
import type { AxiosResponse } from 'axios'

import type { ApiFieldError } from './apiErrors'
import { parseApiError } from './apiErrors'

import type { MatchDetail, MatchPage } from '@/types/match'
import type { ObservedResponse } from '@/types/season'
import type {
  CreateSeriesRequest,
  CompetitiveMutationInvalidations,
  CompetitiveMutationResult,
  SeriesDetail,
  SeriesListFilters,
  SeriesPage,
  SeriesReasonRequest,
  SeriesResult,
} from '@/types/series'

import { api } from './api'
import { serializeSeasonScope } from './seasons'

const basePath = '/api/v1/series'

export class SeriesServiceError extends Error {
  constructor(
    public readonly status?: number,
    public readonly messageCode?: string,
    public readonly errors: string[] = [],
    public readonly fieldErrors: ApiFieldError[] = [],
    message?: string,
  ) {
    super(message)
  }
}

export async function listSeries(
  filters: SeriesListFilters = {},
): Promise<SeriesPage> {
  const params = serializeSeasonScope(filters.scope ?? { mode: 'current' })
  if (filters.page !== undefined) params.set('page', String(filters.page))
  if (filters.pageSize !== undefined) {
    params.set('pageSize', String(filters.pageSize))
  }
  if (filters.tipo !== undefined) params.set('tipo', filters.tipo)
  if (filters.estado !== undefined) params.set('estado', filters.estado)

  return request(() => api.get<SeriesPage>(basePath, { params }))
}

export async function getSeries(
  seriesId: string,
): Promise<ObservedResponse<SeriesDetail>> {
  return requestObserved(() => api.get<SeriesDetail>(resourcePath(seriesId)))
}

export async function createSeries(
  payload: CreateSeriesRequest,
  idempotencyKey: string = crypto.randomUUID(),
): Promise<CompetitiveMutationResult<SeriesDetail>> {
  return requestMutation(() =>
    api.post<SeriesDetail>(basePath, payload, {
      headers: idempotencyHeaders(idempotencyKey),
    }),
  )
}

export async function startSeries(
  seriesId: string,
  seriesEtag: string,
  idempotencyKey: string = crypto.randomUUID(),
): Promise<CompetitiveMutationResult<SeriesDetail>> {
  return mutateSeries(
    seriesId,
    'inicios',
    undefined,
    seriesEtag,
    idempotencyKey,
  )
}

export async function cancelSeries(
  seriesId: string,
  payload: SeriesReasonRequest,
  seriesEtag: string,
  idempotencyKey: string = crypto.randomUUID(),
): Promise<CompetitiveMutationResult<SeriesDetail>> {
  return mutateSeries(
    seriesId,
    'cancelamentos',
    payload,
    seriesEtag,
    idempotencyKey,
  )
}

export async function annulSeries(
  seriesId: string,
  payload: SeriesReasonRequest,
  seriesEtag: string,
  idempotencyKey: string = crypto.randomUUID(),
): Promise<CompetitiveMutationResult<SeriesDetail>> {
  return mutateSeries(
    seriesId,
    'anulacoes',
    payload,
    seriesEtag,
    idempotencyKey,
  )
}

export async function listMatches(
  seriesId: string,
  page = 1,
  pageSize = 20,
): Promise<MatchPage> {
  return request(() =>
    api.get<MatchPage>(`${resourcePath(seriesId)}/partidas`, {
      params: { page, pageSize },
    }),
  )
}

export async function createMatch(
  seriesId: string,
  seriesEtag: string,
  idempotencyKey: string = crypto.randomUUID(),
): Promise<CompetitiveMutationResult<MatchDetail>> {
  return requestMutation(() =>
    api.post<MatchDetail>(`${resourcePath(seriesId)}/partidas`, undefined, {
      headers: mutationHeaders(seriesEtag, idempotencyKey),
    }),
  )
}

export async function getSeriesResult(seriesId: string): Promise<SeriesResult> {
  return request(() =>
    api.get<SeriesResult>(`${resourcePath(seriesId)}/resultado`),
  )
}

function mutateSeries(
  seriesId: string,
  operation: 'inicios' | 'cancelamentos' | 'anulacoes',
  payload: SeriesReasonRequest | undefined,
  seriesEtag: string,
  idempotencyKey: string,
): Promise<CompetitiveMutationResult<SeriesDetail>> {
  return requestMutation(() =>
    api.post<SeriesDetail>(`${resourcePath(seriesId)}/${operation}`, payload, {
      headers: mutationHeaders(seriesEtag, idempotencyKey),
    }),
  )
}

function resourcePath(id: string): string {
  return `${basePath}/${encodeURIComponent(id)}`
}

function idempotencyHeaders(idempotencyKey: string) {
  return { 'Idempotency-Key': idempotencyKey }
}

function mutationHeaders(etag: string, idempotencyKey: string) {
  return {
    'If-Match': etag,
    ...idempotencyHeaders(idempotencyKey),
  }
}

async function request<T>(
  operation: () => Promise<AxiosResponse<T>>,
): Promise<T> {
  try {
    return (await operation()).data
  } catch (error) {
    throw toSeriesServiceError(error)
  }
}

async function requestObserved<T>(
  operation: () => Promise<AxiosResponse<T>>,
): Promise<ObservedResponse<T>> {
  try {
    const response = await operation()
    return { data: response.data, etag: response.headers.etag ?? null }
  } catch (error) {
    throw toSeriesServiceError(error)
  }
}

async function requestMutation<T>(
  operation: () => Promise<AxiosResponse<T>>,
): Promise<CompetitiveMutationResult<T>> {
  try {
    const response = await operation()
    return {
      data: response.data,
      etag: response.headers.etag ?? null,
      idempotencyReplayed: isIdempotencyReplay(response),
      invalidates: mutationInvalidations(),
    }
  } catch (error) {
    throw toSeriesServiceError(error)
  }
}

function isIdempotencyReplay(response: AxiosResponse<unknown>): boolean {
  const value =
    response.headers['idempotency-replayed'] ??
    response.headers['Idempotency-Replayed']
  return String(value).toLowerCase() === 'true'
}

function mutationInvalidations(): CompetitiveMutationInvalidations {
  return [
    'series-detail',
    'series-scoreboard',
    'series-fearless',
    'match-detail',
  ]
}

function toSeriesServiceError(error: unknown): SeriesServiceError {
  if (error instanceof AxiosError) {
    const data = parseApiError(error.response?.data)
    return new SeriesServiceError(
      error.response?.status,
      data.messageCode,
      data.errors,
      data.fieldErrors,
      data.message,
    )
  }

  throw error
}
