import { AxiosError } from 'axios'
import type { AxiosResponse } from 'axios'

import type { ApiFieldError } from './apiErrors'
import { parseApiError } from './apiErrors'

import type {
  AnnulMatchRequest,
  ConfirmMatchResultRequest,
  CorrectMatchRequest,
  MatchDetail,
  MatchMutationResult,
  RegisterMatchPicksRequest,
  RegisterMatchRemakeRequest,
} from '@/types/match'
import type { ObservedResponse } from '@/types/season'
import type {
  CompetitiveMutationInvalidations,
  CompetitiveMutationResult,
} from '@/types/series'

import { api } from './api'

const basePath = '/api/v1/partidas'

export class MatchServiceError extends Error {
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

export async function getMatch(
  matchId: string,
): Promise<ObservedResponse<MatchDetail>> {
  return requestObserved(() => api.get<MatchDetail>(resourcePath(matchId)))
}

export async function registerMatchPicks(
  matchId: string,
  payload: RegisterMatchPicksRequest,
  seriesEtag: string,
  idempotencyKey: string = crypto.randomUUID(),
): Promise<CompetitiveMutationResult<MatchMutationResult>> {
  return mutateMatch(matchId, 'picks', payload, seriesEtag, idempotencyKey)
}

export async function confirmMatchResult(
  matchId: string,
  payload: ConfirmMatchResultRequest,
  seriesEtag: string,
  idempotencyKey: string = crypto.randomUUID(),
): Promise<CompetitiveMutationResult<MatchMutationResult>> {
  return mutateMatch(matchId, 'resultados', payload, seriesEtag, idempotencyKey)
}

export async function registerMatchRemake(
  matchId: string,
  payload: RegisterMatchRemakeRequest,
  seriesEtag: string,
  idempotencyKey: string = crypto.randomUUID(),
): Promise<CompetitiveMutationResult<MatchMutationResult>> {
  return mutateMatch(matchId, 'remakes', payload, seriesEtag, idempotencyKey)
}

export async function annulMatch(
  matchId: string,
  payload: AnnulMatchRequest,
  seriesEtag: string,
  idempotencyKey: string = crypto.randomUUID(),
): Promise<CompetitiveMutationResult<MatchMutationResult>> {
  return mutateMatch(matchId, 'anulacoes', payload, seriesEtag, idempotencyKey)
}

export async function correctMatch(
  matchId: string,
  payload: CorrectMatchRequest,
  seriesEtag: string,
  idempotencyKey: string = crypto.randomUUID(),
): Promise<CompetitiveMutationResult<MatchMutationResult>> {
  return mutateMatch(matchId, 'correcoes', payload, seriesEtag, idempotencyKey)
}

function mutateMatch(
  matchId: string,
  operation: 'picks' | 'resultados' | 'remakes' | 'anulacoes' | 'correcoes',
  payload:
    | RegisterMatchPicksRequest
    | ConfirmMatchResultRequest
    | RegisterMatchRemakeRequest
    | AnnulMatchRequest
    | CorrectMatchRequest,
  seriesEtag: string,
  idempotencyKey: string,
): Promise<CompetitiveMutationResult<MatchMutationResult>> {
  return requestMutation(() =>
    api.post<MatchMutationResult>(
      `${resourcePath(matchId)}/${operation}`,
      payload,
      { headers: mutationHeaders(seriesEtag, idempotencyKey) },
    ),
  )
}

function resourcePath(id: string): string {
  return `${basePath}/${encodeURIComponent(id)}`
}

function mutationHeaders(etag: string, idempotencyKey: string) {
  return {
    'If-Match': etag,
    'Idempotency-Key': idempotencyKey,
  }
}

async function requestObserved<T>(
  operation: () => Promise<AxiosResponse<T>>,
): Promise<ObservedResponse<T>> {
  try {
    const response = await operation()
    return { data: response.data, etag: response.headers.etag ?? null }
  } catch (error) {
    throw toMatchServiceError(error)
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
    throw toMatchServiceError(error)
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

function toMatchServiceError(error: unknown): MatchServiceError {
  if (error instanceof AxiosError) {
    const data = parseApiError(error.response?.data)
    return new MatchServiceError(
      error.response?.status,
      data.messageCode,
      data.errors,
      data.fieldErrors,
      data.message,
    )
  }

  throw error
}
