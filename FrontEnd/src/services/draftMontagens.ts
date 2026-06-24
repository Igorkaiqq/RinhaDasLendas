import { AxiosError } from 'axios'

import { MessageCode } from '@/constants/messageCode'
import type {
  DraftMontagem,
  DraftMontagemLayoutPayload,
  DraftMontagemPayload,
  DraftMontagemRealtimeState,
  DraftMontagemResumo,
  DraftMontagemStatus,
} from '@/types/draftMontagem'

import { api } from './api'
import { getMessage } from './messageService'

interface PaginatedDraftMontagens {
  page: number
  pageSize: number
  items: DraftMontagemResumo[]
  totalItems: number
  totalPages: number
}

interface ApiErrorResponse {
  message?: string
  errors?: string[]
}

export class DraftMontagemServiceError extends Error {
  constructor(public readonly errors: string[]) {
    super(errors[0] ?? getMessage(MessageCode.RequestProcessingFailed))
  }
}

export async function listDraftMontagens(filters: { search?: string; status?: DraftMontagemStatus | '' } = {}): Promise<DraftMontagemResumo[]> {
  try {
    const response = await api.get<PaginatedDraftMontagens>('/api/v1/draft-montagens', {
      params: { ...filters, page: 1, pageSize: 100 },
    })
    return response.data.items
  } catch (error) {
    throw toDraftMontagemServiceError(error)
  }
}

export async function getDraftMontagemById(id: string): Promise<DraftMontagem> {
  try {
    const response = await api.get<DraftMontagem>(`/api/v1/draft-montagens/${id}`)
    return response.data
  } catch (error) {
    throw toDraftMontagemServiceError(error)
  }
}

export async function getDraftMontagemRealtimeState(id: string): Promise<DraftMontagemRealtimeState> {
  try {
    const response = await api.get<DraftMontagemRealtimeState>(`/api/v1/draft-montagens/${id}/realtime-state`)
    return response.data
  } catch (error) {
    throw toDraftMontagemServiceError(error)
  }
}

export async function createDraftMontagem(payload: DraftMontagemPayload): Promise<DraftMontagem> {
  try {
    const response = await api.post<DraftMontagem>('/api/v1/draft-montagens', normalizePayload(payload))
    return response.data
  } catch (error) {
    throw toDraftMontagemServiceError(error)
  }
}

export async function saveDraftMontagemLayout(id: string, payload: DraftMontagemLayoutPayload): Promise<DraftMontagem> {
  try {
    const response = await api.put<DraftMontagem>(`/api/v1/draft-montagens/${id}/layout`, payload)
    return response.data
  } catch (error) {
    throw toDraftMontagemServiceError(error)
  }
}

export async function startDraftMontagemRealtime(id: string): Promise<DraftMontagemRealtimeState> {
  try {
    const response = await api.post<DraftMontagemRealtimeState>(`/api/v1/draft-montagens/${id}/iniciar-tempo-real`)
    return response.data
  } catch (error) {
    throw toDraftMontagemServiceError(error)
  }
}

export async function registerDraftMontagemPick(id: string, jogadorId: string): Promise<DraftMontagemRealtimeState> {
  try {
    const response = await api.post<DraftMontagemRealtimeState>(`/api/v1/draft-montagens/${id}/picks`, { jogadorId })
    return response.data
  } catch (error) {
    throw toDraftMontagemServiceError(error)
  }
}

export async function substituteDraftMontagemReserve(
  id: string,
  payload: { timeId: string; jogadorSaiuId: string; reservaEntrouId: string; motivo?: string | null },
): Promise<DraftMontagemRealtimeState> {
  try {
    const response = await api.post<DraftMontagemRealtimeState>(`/api/v1/draft-montagens/${id}/reservas/substituir`, normalizePayload(payload))
    return response.data
  } catch (error) {
    throw toDraftMontagemServiceError(error)
  }
}

export async function drawDraftMontagemCaptains(id: string): Promise<DraftMontagem> {
  try {
    const response = await api.post<DraftMontagem>(`/api/v1/draft-montagens/${id}/capitaes/sortear`)
    return response.data
  } catch (error) {
    throw toDraftMontagemServiceError(error)
  }
}

export async function finalizeDraftMontagem(id: string): Promise<DraftMontagem> {
  try {
    const response = await api.patch<DraftMontagem>(`/api/v1/draft-montagens/${id}/finalizar`)
    return response.data
  } catch (error) {
    throw toDraftMontagemServiceError(error)
  }
}

export async function cancelDraftMontagem(id: string, motivo: string | null = null): Promise<DraftMontagem> {
  try {
    const response = await api.patch<DraftMontagem>(`/api/v1/draft-montagens/${id}/cancelar`, { motivo })
    return response.data
  } catch (error) {
    throw toDraftMontagemServiceError(error)
  }
}

function normalizePayload<T extends object>(payload: T): T {
  return Object.fromEntries(Object.entries(payload).map(([key, value]) => [key, value === '' ? null : value])) as T
}

function toDraftMontagemServiceError(error: unknown): DraftMontagemServiceError {
  if (error instanceof AxiosError) {
    const data = error.response?.data as ApiErrorResponse | undefined
    if (Array.isArray(data?.errors) && data.errors.length > 0) {
      return new DraftMontagemServiceError(data.errors)
    }
    if (data?.message) {
      return new DraftMontagemServiceError([data.message])
    }
  }

  return new DraftMontagemServiceError([getMessage(MessageCode.ServerConnectionFailed)])
}
