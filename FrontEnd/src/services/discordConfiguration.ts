import type { DiscordConfiguration } from '@/types/discordConfiguration'

import { api } from './api'

export async function getDiscordConfiguration(): Promise<DiscordConfiguration | null> {
  try {
    const response = await api.get<DiscordConfiguration>('/api/v1/discord/configuracoes')
    return response.data
  } catch {
    return null
  }
}

export async function saveDiscordConfiguration(payload: DiscordConfiguration): Promise<DiscordConfiguration> {
  const response = await api.put<DiscordConfiguration>('/api/v1/discord/configuracoes', payload)
  return response.data
}
