import * as signalR from '@microsoft/signalr'

import type { DraftMontagemRealtimeState } from '@/types/draftMontagem'

import { api } from './api'
import { getAccessToken } from './authState'

export type DraftMontagemRealtimeHandler = (state: DraftMontagemRealtimeState) => void
export type DraftMontagemRealtimeReconnectHandler = () => void | Promise<void>

export class DraftMontagemRealtimeConnection {
  private connection: signalR.HubConnection | null = null

  constructor(private readonly draftMontagemId: string) {}

  async connect(onStateUpdated: DraftMontagemRealtimeHandler, onReconnected?: DraftMontagemRealtimeReconnectHandler) {
    if (this.connection) {
      await this.disconnect()
    }

    const baseUrl = String(api.defaults.baseURL ?? '').replace(/\/$/, '')
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${baseUrl}/hubs/draft-montagens`, { accessTokenFactory: () => getAccessToken() ?? '' })
      .withAutomaticReconnect()
      .build()
    this.connection = connection

    connection.on('DraftMontagemStateUpdated', onStateUpdated)
    connection.onreconnected(async () => {
      if (this.connection !== connection) return
      await connection.invoke('JoinDraftMontagem', this.draftMontagemId)
      await onReconnected?.()
    })
    try {
      await connection.start()
    } catch (error) {
      if (this.connection !== connection) return
      this.connection = null
      throw error
    }

    if (this.connection !== connection) return
    await connection.invoke('JoinDraftMontagem', this.draftMontagemId)
  }

  async disconnect() {
    const connection = this.connection
    if (!connection) {
      return
    }
    this.connection = null

    try {
      if (connection.state === signalR.HubConnectionState.Connected) {
        await connection.invoke('LeaveDraftMontagem', this.draftMontagemId)
      }
    } finally {
      await connection.stop()
    }
  }
}
