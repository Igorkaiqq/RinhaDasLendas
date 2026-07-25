import { AxiosError } from 'axios'

import type {
  PaginatedResponse,
  PresenceSchedulePaginatedResponse,
  PresenceScheduleOccurrenceSummary,
  PresenceScheduleSummary,
  SavePresenceScheduleRequest,
} from '@/types/presenceSchedule'

import { api } from './api'

const basePath = '/api/v1/discord/agendamentos-presenca'

interface ApiErrorResponse {
  messageCode?: string
}

export class PresenceScheduleServiceError extends Error {
  constructor(
    public readonly status?: number,
    public readonly messageCode?: string,
  ) {
    super(messageCode ?? 'PRESENCE_SCHEDULE_REQUEST_FAILED')
  }
}

export async function listPresenceSchedules(
  page: number,
  pageSize: number,
): Promise<PresenceSchedulePaginatedResponse> {
  return request(() => api.get(basePath, { params: { page, pageSize } }))
}

export async function listPresenceScheduleOccurrences(
  id: string,
  page: number,
  pageSize: number,
): Promise<PaginatedResponse<PresenceScheduleOccurrenceSummary>> {
  return request(() => api.get(`${basePath}/${encodeURIComponent(id)}/ocorrencias`, {
    params: { page, pageSize },
  }))
}

export async function createPresenceSchedule(
  payload: SavePresenceScheduleRequest,
): Promise<PresenceScheduleSummary> {
  return request(() => api.post(basePath, normalizePayload(payload)))
}

export async function updatePresenceSchedule(
  id: string,
  payload: SavePresenceScheduleRequest,
): Promise<PresenceScheduleSummary> {
  return request(() => api.put(`${basePath}/${encodeURIComponent(id)}`, normalizePayload(payload)))
}

export async function pausePresenceSchedule(id: string): Promise<PresenceScheduleSummary> {
  return request(() => api.post(`${basePath}/${encodeURIComponent(id)}/pausar`))
}

export async function reactivatePresenceSchedule(id: string): Promise<PresenceScheduleSummary> {
  return request(() => api.post(`${basePath}/${encodeURIComponent(id)}/reativar`))
}

export async function archivePresenceSchedule(id: string): Promise<void> {
  await request(() => api.delete(`${basePath}/${encodeURIComponent(id)}`))
}

function normalizePayload(payload: SavePresenceScheduleRequest): SavePresenceScheduleRequest {
  return {
    ...payload,
    nome: payload.nome.trim(),
    observacao: payload.observacao?.trim() || null,
    diasSemana: [...payload.diasSemana],
    horarioPublicacao: payload.horarioPublicacao.slice(0, 5),
    horarioEncerramento: payload.horarioEncerramento.slice(0, 5),
  }
}

async function request<T>(operation: () => Promise<{ data: T }>): Promise<T> {
  try {
    return (await operation()).data
  } catch (error) {
    if (error instanceof AxiosError) {
      const data = error.response?.data as ApiErrorResponse | undefined
      throw new PresenceScheduleServiceError(error.response?.status, data?.messageCode)
    }
    throw error
  }
}
