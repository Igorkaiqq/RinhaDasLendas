import { AxiosError } from 'axios'
import type { AxiosResponse } from 'axios'

import { parseApiError } from './apiErrors'
import type { ApiFieldError } from './apiErrors'

import type {
  CreateSeasonRequest,
  ObservedResponse,
  SeasonDetail,
  SeasonListFilters,
  SeasonPage,
  SeasonScope,
  UpdateSeasonRequest,
} from '@/types/season'

import { api } from './api'

const basePath = '/api/v1/temporadas'

export class SeasonServiceError extends Error {
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

export class SeasonScopeValidationError extends Error {}

export function serializeSeasonScope(scope: SeasonScope): URLSearchParams {
  const params = new URLSearchParams()

  if (scope.mode === 'selected') {
    validateSelectedSeasonIds(scope.seasonIds)
    for (const seasonId of scope.seasonIds) {
      params.append('temporadaIds', seasonId)
    }
  } else if (scope.mode === 'all') {
    params.set('todas', 'true')
  }

  return params
}

export async function listSeasons(
  filters: SeasonListFilters = {},
): Promise<ObservedResponse<SeasonPage>> {
  return requestObserved(() =>
    api.get<SeasonPage>(basePath, {
      params: {
        page: filters.page ?? 1,
        pageSize: filters.pageSize ?? 20,
        ...(filters.estado ? { estado: filters.estado } : {}),
      },
    }),
  )
}

export async function getSeason(
  seasonId: string,
): Promise<ObservedResponse<SeasonDetail>> {
  return requestObserved(() => api.get<SeasonDetail>(resourcePath(seasonId)))
}

export async function createSeason(
  payload: CreateSeasonRequest,
  idempotencyKey: string = crypto.randomUUID(),
): Promise<ObservedResponse<SeasonDetail>> {
  return requestObserved(() =>
    api.post<SeasonDetail>(basePath, payload, {
      headers: idempotencyHeaders(idempotencyKey),
    }),
  )
}

export async function updateSeason(
  seasonId: string,
  payload: UpdateSeasonRequest,
  etag: string,
  idempotencyKey: string = crypto.randomUUID(),
): Promise<ObservedResponse<SeasonDetail>> {
  return requestObserved(() =>
    api.patch<SeasonDetail>(resourcePath(seasonId), payload, {
      headers: mutationHeaders(etag, idempotencyKey),
    }),
  )
}

export async function activateSeason(
  seasonId: string,
  calendarEtag: string,
  idempotencyKey: string = crypto.randomUUID(),
): Promise<ObservedResponse<SeasonDetail>> {
  return transitionSeason(seasonId, 'aberturas', calendarEtag, idempotencyKey)
}

export async function closeSeason(
  seasonId: string,
  calendarEtag: string,
  idempotencyKey: string = crypto.randomUUID(),
): Promise<ObservedResponse<SeasonDetail>> {
  return transitionSeason(
    seasonId,
    'encerramentos',
    calendarEtag,
    idempotencyKey,
  )
}

async function transitionSeason(
  seasonId: string,
  transition: 'aberturas' | 'encerramentos',
  calendarEtag: string,
  idempotencyKey: string,
): Promise<ObservedResponse<SeasonDetail>> {
  return requestObserved(() =>
    api.post<SeasonDetail>(
      `${resourcePath(seasonId)}/${transition}`,
      undefined,
      { headers: calendarMutationHeaders(calendarEtag, idempotencyKey) },
    ),
  )
}

function resourcePath(id: string): string {
  return `${basePath}/${encodeURIComponent(id)}`
}

function validateSelectedSeasonIds(seasonIds: readonly string[]): void {
  const guidPattern =
    /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i
  const emptyGuid = '00000000-0000-0000-0000-000000000000'
  const normalizedIds = new Set<string>()

  if (seasonIds.length === 0) {
    throw new SeasonScopeValidationError()
  }

  for (const seasonId of seasonIds) {
    const normalizedId = seasonId.toLowerCase()
    if (
      !guidPattern.test(seasonId) ||
      normalizedId === emptyGuid ||
      normalizedIds.has(normalizedId)
    ) {
      throw new SeasonScopeValidationError()
    }
    normalizedIds.add(normalizedId)
  }
}

function idempotencyHeaders(idempotencyKey: string) {
  return { 'Idempotency-Key': idempotencyKey }
}

function mutationHeaders(etag: string, idempotencyKey: string) {
  return {
    ...idempotencyHeaders(idempotencyKey),
    'If-Match': etag,
  }
}

function calendarMutationHeaders(etag: string, idempotencyKey: string) {
  return {
    ...idempotencyHeaders(idempotencyKey),
    'If-Match-Calendar': etag,
  }
}

async function requestObserved<T>(
  operation: () => Promise<AxiosResponse<T>>,
): Promise<ObservedResponse<T>> {
  try {
    const response = await operation()
    return {
      data: response.data,
      etag: response.headers.etag ?? null,
    }
  } catch (error) {
    throw toSeasonServiceError(error)
  }
}

function toSeasonServiceError(error: unknown): SeasonServiceError {
  if (error instanceof AxiosError) {
    const data = parseApiError(error.response?.data)
    return new SeasonServiceError(
      error.response?.status,
      data.messageCode,
      data.errors,
      data.fieldErrors,
      data.message,
    )
  }

  throw error
}
