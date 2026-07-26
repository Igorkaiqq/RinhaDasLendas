import { AxiosError } from 'axios'

import { DraftMontagemPresencaOrigemValues } from '@/constants/draftMontagem'
import { MessageCode } from '@/constants/messageCode'
import type {
  DraftMontagem,
  DraftMontagemAdmin,
  DraftMontagemArquivamento,
  DraftMontagemArquivamentoResultado,
  DraftMontagemLayoutPayload,
  DraftMontagemManualPresencePayload,
  DraftMontagemOrdemEscolhaModo,
  DraftMontagemPublicacaoDiscordTipo,
  DraftMontagemPayload,
  DraftMontagemRealtimeState,
  DraftMontagemResumo,
  DraftMontagemStatus,
} from '@/types/draftMontagem'

import { api } from './api'
import { getMessage } from './messageService'
import type { Player } from './players'

interface PaginatedDraftMontagens {
  page: number
  pageSize: number
  items: DraftMontagemResumo[]
  totalItems: number
  totalPages: number
}

interface PaginatedEligiblePlayers {
  page: number
  pageSize: number
  items: Pick<Player, 'id' | 'nomeExibicao'>[]
  totalItems: number
  totalPages: number
}

interface ApiErrorResponse {
  messageCode?: string
  message?: string
  errors?: string[]
}

export class DraftMontagemServiceError extends Error {
  constructor(public readonly errors: string[], public readonly status?: number) {
    super(errors[0] ?? getMessage(MessageCode.RequestProcessingFailed))
  }
}

export async function listDraftMontagens(filters: { search?: string; status?: DraftMontagemStatus | ''; includeCancelled?: boolean; includeArchived?: boolean } = {}): Promise<DraftMontagemResumo[]> {
  try {
    const response = await api.get<PaginatedDraftMontagens>('/api/v1/draft-montagens', {
      params: { ...filters, includeArchived: filters.includeArchived ?? false, page: 1, pageSize: 100 },
    })
    return response.data.items
  } catch (error) {
    throw toDraftMontagemServiceError(error)
  }
}

export async function getDraftMontagemArchivingById(id: string): Promise<DraftMontagemArquivamento> {
  try {
    const response = await api.get<DraftMontagemArquivamento>(`/api/v1/draft-montagens/${encodeURIComponent(id)}/arquivamento`)
    return response.data
  } catch (error) {
    throw toDraftMontagemServiceError(error)
  }
}

export async function archiveDraftMontagem(id: string, motivo: string, versaoEstado: number): Promise<DraftMontagemArquivamentoResultado> {
  try {
    const response = await api.patch<DraftMontagemArquivamentoResultado>(`/api/v1/draft-montagens/${encodeURIComponent(id)}/arquivar`, {
      motivo: motivo.trim(),
      versaoEstado,
    })
    return response.data
  } catch (error) {
    throw toDraftMontagemServiceError(error)
  }
}

export async function restoreDraftMontagem(id: string, versaoEstado: number): Promise<DraftMontagemArquivamentoResultado> {
  try {
    const response = await api.patch<DraftMontagemArquivamentoResultado>(`/api/v1/draft-montagens/${encodeURIComponent(id)}/restaurar`, { versaoEstado })
    return response.data
  } catch (error) {
    throw toDraftMontagemServiceError(error)
  }
}

export async function republishArchivedDraftCancellation(id: string): Promise<DraftMontagemArquivamentoResultado> {
  try {
    const response = await api.post<DraftMontagemArquivamentoResultado>(`/api/v1/draft-montagens/${encodeURIComponent(id)}/discord/publicacoes/cancelamento/republicar`)
    return response.data
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

export async function getDraftMontagemAdminById(id: string): Promise<DraftMontagemAdmin> {
  try {
    const response = await api.get<DraftMontagemAdmin>(`/api/v1/draft-montagens/${id}/administracao`)
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

export async function confirmDraftMontagemPresence(id: string): Promise<DraftMontagem> {
  try {
    const response = await api.post<DraftMontagem>(`/api/v1/draft-montagens/${id}/presencas/confirmar`, { origem: DraftMontagemPresencaOrigemValues.Web })
    return response.data
  } catch (error) {
    throw toDraftMontagemServiceError(error)
  }
}

export async function cancelDraftMontagemPresence(id: string): Promise<DraftMontagem> {
  try {
    const response = await api.post<DraftMontagem>(`/api/v1/draft-montagens/${id}/presencas/cancelar`, {})
    return response.data
  } catch (error) {
    throw toDraftMontagemServiceError(error)
  }
}

export async function addManualDraftMontagemPresence(id: string, jogadorId: string, motivo: string): Promise<DraftMontagem> {
  try {
    const payload: DraftMontagemManualPresencePayload = { jogadorId, motivo }
    const response = await api.post<DraftMontagem>(`/api/v1/draft-montagens/${id}/presencas/manual`, payload)
    return response.data
  } catch (error) {
    throw toDraftMontagemServiceError(error)
  }
}

export async function removeManualDraftMontagemPresence(id: string, jogadorId: string, motivo: string | null = null): Promise<DraftMontagem> {
  try {
    const response = await api.delete<DraftMontagem>(`/api/v1/draft-montagens/${id}/presencas/${jogadorId}`, { data: { motivo } })
    return response.data
  } catch (error) {
    throw toDraftMontagemServiceError(error)
  }
}

export async function listEligibleManualPresencePlayers(id: string, search = '', page = 1, pageSize = 20, signal?: AbortSignal): Promise<Pick<Player, 'id' | 'nomeExibicao'>[]> {
  try {
    const response = await api.get<PaginatedEligiblePlayers>(`/api/v1/draft-montagens/${id}/presencas/elegiveis`, {
      params: { search, page, pageSize },
      ...(signal ? { signal } : {}),
    })
    return response.data.items
  } catch (error) {
    throw toDraftMontagemServiceError(error)
  }
}

export async function republishDraftMontagemDiscordPublication(id: string, tipo: DraftMontagemPublicacaoDiscordTipo, motivo: string | null = null): Promise<DraftMontagem> {
  try {
    const response = await api.post<DraftMontagem>(`/api/v1/draft-montagens/${id}/discord/publicacoes/republicar`, { tipo, motivo })
    return response.data
  } catch (error) {
    throw toDraftMontagemServiceError(error)
  }
}

export async function closeDraftMontagemPresence(id: string, continuarComMenosDez = false, tamanhoEquipe = 5): Promise<DraftMontagem> {
  try {
    const response = await api.post<DraftMontagem>(`/api/v1/draft-montagens/${id}/encerrar-presenca`, { continuarComMenosDez, tamanhoEquipe })
    return response.data
  } catch (error) {
    throw toDraftMontagemServiceError(error)
  }
}

export async function defineDraftMontagemCaptains(id: string, capitaesIds: string[]): Promise<DraftMontagem> {
  try {
    const response = await api.post<DraftMontagem>(`/api/v1/draft-montagens/${id}/capitaes`, { capitaesIds })
    return response.data
  } catch (error) {
    throw toDraftMontagemServiceError(error)
  }
}

export async function defineDraftMontagemPickOrder(id: string, modo: DraftMontagemOrdemEscolhaModo, capitaesIds: string[] = []): Promise<DraftMontagem> {
  try {
    const response = await api.post<DraftMontagem>(`/api/v1/draft-montagens/${id}/ordem-escolha`, { modo, capitaesIds })
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
      return new DraftMontagemServiceError(data.errors, error.response?.status)
    }
    if (data?.messageCode) {
      return new DraftMontagemServiceError([getMessage(data.messageCode)], error.response?.status)
    }

    if (data?.message) {
      return new DraftMontagemServiceError([data.message], error.response?.status)
    }
  }

  return new DraftMontagemServiceError([getMessage(MessageCode.ServerConnectionFailed)])
}
