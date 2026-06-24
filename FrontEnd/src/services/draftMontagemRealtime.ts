import * as signalR from '@microsoft/signalr'

import type { DraftMontagemRealtimeState } from '@/types/draftMontagem'

import { api } from './api'
import { getAccessToken } from './authState'

export type DraftMontagemRealtimeHandler = (state: DraftMontagemRealtimeState) => void

export class DraftMontagemRealtimeConnection {
  private connection: signalR.HubConnection | null = null

  constructor(private readonly draftMontagemId: string) {}

  async connect(onStateUpdated: DraftMontagemRealtimeHandler) {
    if (this.connection) {
      await this.disconnect()
    }

    const baseUrl = String(api.defaults.baseURL ?? '').replace(/\/$/, '')
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${baseUrl}/hubs/draft-montagens`, { accessTokenFactory: () => getAccessToken() ?? '' })
      .withAutomaticReconnect()
      .build()

    this.connection.on('DraftMontagemStateUpdated', onStateUpdated)
    await this.connection.start()
    await this.connection.invoke('JoinDraftMontagem', this.draftMontagemId)
  }

  async disconnect() {
    if (!this.connection) {
      return
    }

    try {
      await this.connection.invoke('LeaveDraftMontagem', this.draftMontagemId)
    } finally {
      await this.connection.stop()
      this.connection = null
    }
  }
}
