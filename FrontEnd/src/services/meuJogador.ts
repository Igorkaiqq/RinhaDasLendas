import { api } from './api'

import type { MeuJogadorProfile, MeuJogadorProfilePayload } from '@/types/meuJogador'

export async function getMeuJogadorProfile(): Promise<MeuJogadorProfile | null> {
  try {
    const response = await api.get<MeuJogadorProfile>('/api/v1/auth/me/jogador')
    return response.data
  } catch (error: unknown) {
    if (typeof error === 'object' && error !== null && 'response' in error) {
      const status = (error as { response?: { status?: number } }).response?.status
      if (status === 404) {
        return null
      }
    }

    throw error
  }
}

export async function completeMeuJogadorProfile(payload: MeuJogadorProfilePayload): Promise<MeuJogadorProfile> {
  const response = await api.post<MeuJogadorProfile>('/api/v1/auth/me/jogador', payload)
  return response.data
}

export async function updateMeuJogadorProfile(payload: MeuJogadorProfilePayload): Promise<MeuJogadorProfile> {
  const response = await api.put<MeuJogadorProfile>('/api/v1/auth/me/jogador', payload)
  return response.data
}
