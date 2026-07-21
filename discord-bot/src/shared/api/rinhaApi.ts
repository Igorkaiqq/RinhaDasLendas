import { env } from '../../config/env.js'
import { DraftPresenceOrigin, DraftPickOrderMode } from '../constants/draftConstants/index.js'
import { t } from '../messages/index.js'
import type { DiscordConfiguration, DiscordPublicationClaim, DiscordPublicationType, DiscordUserLink, DraftMontagem } from './types.js'

interface ApiErrorResponse {
  messageCode?: string
  message?: string
  errors?: string[]
}

export class RinhaApiError extends Error {
  constructor(public readonly messageCode: string | undefined, message: string, public readonly status: number) {
    super(message)
  }
}

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(new URL(path, env.RINHA_API_BASE_URL), {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      'X-Rinha-Internal-Token': env.RINHA_API_INTERNAL_TOKEN,
      ...init.headers,
    },
  })

  if (!response.ok) {
    const body = await response.text()
    if (response.status === 401) {
      throw new Error(t.unauthorizedApi)
    }

    throw parseApiError(body, response.statusText, response.status)
  }

  return response.json() as Promise<T>
}

export function parseApiError(body: string, statusText: string, status: number) {
  try {
    const parsed = JSON.parse(body) as ApiErrorResponse
    const message = parsed.message || parsed.errors?.[0] || statusText
    return new RinhaApiError(parsed.messageCode, message, status)
  } catch {
    return new RinhaApiError(undefined, body || statusText, status)
  }
}

export const rinhaApi = {
  getDiscordConfiguration: () => request<DiscordConfiguration>('/api/v1/discord/configuracoes'),
  getDiscordLink: (discordUserId: string) => request<DiscordUserLink>(`/api/v1/usuarios/discord/${discordUserId}/vinculo`),
  listActiveDrafts: () => request<DraftMontagem[]>('/api/v1/draft-montagens/ativos'),
  createDraft: (payload: { nome: string; horarioEncerramentoPresenca: string; observacoes?: string | null; discordGuildId?: string | null }) =>
    request<DraftMontagem>('/api/v1/draft-montagens', {
      method: 'POST',
      body: JSON.stringify({ ...payload, tamanhoEquipe: 5, sortearCapitaes: false, capitaesIds: [], jogadoresIds: [] }),
    }),
  confirmPresence: (draftId: string, discordUserId: string) =>
    request<DraftMontagem>(`/api/v1/draft-montagens/${draftId}/discord/presencas/confirmar`, {
      method: 'POST',
      body: JSON.stringify({ discordUserId, origem: DraftPresenceOrigin.Discord }),
    }),
  cancelPresence: (draftId: string, discordUserId: string) =>
    request<DraftMontagem>(`/api/v1/draft-montagens/${draftId}/discord/presencas/cancelar`, {
      method: 'POST',
      body: JSON.stringify({ discordUserId }),
    }),
  claimDiscordPublication: (draftId: string, tipo: DiscordPublicationType) =>
    request<DiscordPublicationClaim>(`/api/v1/draft-montagens/${draftId}/discord/publicacoes/claim`, {
      method: 'POST',
      body: JSON.stringify({ tipo }),
    }),
  registerDiscordPublication: (draftId: string, payload: { tipo: DiscordPublicationType; claimId: string; discordGuildId?: string | null; discordChannelId?: string | null; messageId: string }) =>
    request<DraftMontagem>(`/api/v1/draft-montagens/${draftId}/discord/publicacao`, {
      method: 'POST',
      body: JSON.stringify(payload),
    }),
  registerDiscordPublicationFailure: (draftId: string, payload: { tipo: DiscordPublicationType; claimId: string; discordGuildId?: string | null; discordChannelId?: string | null; erroCodigo?: string | null }) =>
    request<DraftMontagem>(`/api/v1/draft-montagens/${draftId}/discord/publicacao/falha`, {
      method: 'POST',
      body: JSON.stringify(payload),
    }),
  closePresence: (draftId: string) =>
    request<DraftMontagem>(`/api/v1/draft-montagens/${draftId}/encerrar-presenca`, {
      method: 'POST',
      body: JSON.stringify({ continuarComMenosDez: false, tamanhoEquipe: 5 }),
    }),
  cancelDraft: (draftId: string, motivo?: string | null) =>
    request<DraftMontagem>(`/api/v1/draft-montagens/${draftId}/cancelar`, {
      method: 'PATCH',
      body: JSON.stringify({ motivo }),
    }),
  defineCaptains: (draftId: string, capitaesIds: string[]) =>
    request<DraftMontagem>(`/api/v1/draft-montagens/${draftId}/capitaes`, { method: 'POST', body: JSON.stringify({ capitaesIds }) }),
  definePickOrder: (draftId: string, modo: (typeof DraftPickOrderMode)[keyof typeof DraftPickOrderMode], capitaesIds: string[] = []) =>
    request<DraftMontagem>(`/api/v1/draft-montagens/${draftId}/ordem-escolha`, { method: 'POST', body: JSON.stringify({ modo, capitaesIds }) }),
}
