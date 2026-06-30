import { env } from '../../config/env.js'
import { DraftPresenceOrigin, DraftPickOrderMode } from '../constants/draftConstants/index.js'
import { t } from '../messages/index.js'
import type { DiscordConfiguration, DiscordUserLink, DraftMontagem } from './types.js'

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

    throw new Error(body || response.statusText)
  }

  return response.json() as Promise<T>
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
  registerDiscordPublication: (draftId: string, payload: { discordGuildId?: string | null; discordPresenceMessageId: string }) =>
    request<DraftMontagem>(`/api/v1/draft-montagens/${draftId}/discord/publicacao`, {
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
