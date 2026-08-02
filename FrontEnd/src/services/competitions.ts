import { AxiosError } from 'axios'
import type { AxiosResponse } from 'axios'

import { parseApiError } from './apiErrors'
import type { ApiFieldError } from './apiErrors'

import type {
  CompetitionDetail,
  CompetitionPage,
  CreateCompetitionRequest,
  CreateRoundRequest,
  PublishRulesRequest,
  ReorderRoundsRequest,
  Round,
  RulesVersion,
  UpdateCompetitionRequest,
} from '@/types/competition'
import type { ObservedResponse, SeasonScope } from '@/types/season'

import { api } from './api'
import { serializeSeasonScope } from './seasons'

const basePath = '/api/v1/competicoes'

export class CompetitionServiceError extends Error {
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

export async function listCompetitions(
  scope: SeasonScope = { mode: 'current' },
  page = 1,
  pageSize = 20,
): Promise<CompetitionPage> {
  const params = serializeSeasonScope(scope)
  params.set('page', String(page))
  params.set('pageSize', String(pageSize))

  return request(() => api.get<CompetitionPage>(basePath, { params }))
}

export async function listCompetitionsForSeason(
  seasonId: string,
  page = 1,
  pageSize = 20,
): Promise<CompetitionPage> {
  return request(() =>
    api.get<CompetitionPage>(
      `/api/v1/temporadas/${encodeURIComponent(seasonId)}/competicoes`,
      { params: { page, pageSize } },
    ),
  )
}

export async function getCompetition(
  competitionId: string,
): Promise<ObservedResponse<CompetitionDetail>> {
  return requestObserved(() =>
    api.get<CompetitionDetail>(resourcePath(competitionId)),
  )
}

export async function createCompetition(
  seasonId: string,
  payload: CreateCompetitionRequest,
  idempotencyKey: string = crypto.randomUUID(),
): Promise<ObservedResponse<CompetitionDetail>> {
  return requestObserved(() =>
    api.post<CompetitionDetail>(
      `/api/v1/temporadas/${encodeURIComponent(seasonId)}/competicoes`,
      payload,
      { headers: idempotencyHeaders(idempotencyKey) },
    ),
  )
}

export async function updateCompetition(
  competitionId: string,
  payload: UpdateCompetitionRequest,
  etag: string,
  idempotencyKey: string = crypto.randomUUID(),
): Promise<ObservedResponse<CompetitionDetail>> {
  return requestObserved(() =>
    api.patch<CompetitionDetail>(resourcePath(competitionId), payload, {
      headers: mutationHeaders(etag, idempotencyKey),
    }),
  )
}

export async function listRounds(competitionId: string): Promise<Round[]> {
  return request(() =>
    api.get<Round[]>(`${resourcePath(competitionId)}/rodadas`),
  )
}

export async function createRound(
  competitionId: string,
  payload: CreateRoundRequest,
  etag: string,
  idempotencyKey: string = crypto.randomUUID(),
): Promise<ObservedResponse<Round>> {
  return requestObserved(() =>
    api.post<Round>(`${resourcePath(competitionId)}/rodadas`, payload, {
      headers: mutationHeaders(etag, idempotencyKey),
    }),
  )
}

export async function reorderRounds(
  competitionId: string,
  payload: ReorderRoundsRequest,
  etag: string,
  idempotencyKey: string = crypto.randomUUID(),
): Promise<ObservedResponse<Round[]>> {
  return requestObserved(() =>
    api.post<Round[]>(
      `${resourcePath(competitionId)}/ordenacoes-rodadas`,
      payload,
      { headers: mutationHeaders(etag, idempotencyKey) },
    ),
  )
}

export async function publishCompetitionRules(
  competitionId: string,
  payload: PublishRulesRequest,
  etag: string,
  idempotencyKey: string = crypto.randomUUID(),
): Promise<ObservedResponse<RulesVersion>> {
  return requestObserved(() =>
    api.post<RulesVersion>(
      `${resourcePath(competitionId)}/regras-publicadas`,
      payload,
      { headers: mutationHeaders(etag, idempotencyKey) },
    ),
  )
}

export async function publishSeasonRules(
  seasonId: string,
  payload: PublishRulesRequest,
  etag: string,
  idempotencyKey: string = crypto.randomUUID(),
): Promise<ObservedResponse<RulesVersion>> {
  return requestObserved(() =>
    api.post<RulesVersion>(
      `/api/v1/temporadas/${encodeURIComponent(seasonId)}/regras-publicadas`,
      payload,
      { headers: mutationHeaders(etag, idempotencyKey) },
    ),
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
    ...idempotencyHeaders(idempotencyKey),
    'If-Match': etag,
  }
}

async function request<T>(
  operation: () => Promise<AxiosResponse<T>>,
): Promise<T> {
  try {
    return (await operation()).data
  } catch (error) {
    throw toCompetitionServiceError(error)
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
    throw toCompetitionServiceError(error)
  }
}

function toCompetitionServiceError(error: unknown): CompetitionServiceError {
  if (error instanceof AxiosError) {
    const data = parseApiError(error.response?.data)
    return new CompetitionServiceError(
      error.response?.status,
      data.messageCode,
      data.errors,
      data.fieldErrors,
      data.message,
    )
  }

  throw error
}
