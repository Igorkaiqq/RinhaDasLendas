import * as signalR from '@microsoft/signalr'

import type { DraftConnectionStatus, DraftMontagemRealtimeSnapshot } from '@/types/draftMontagem'

import { api } from './api'
import { getAccessToken } from './authState'

const RETRY_DELAYS = [0, 2000, 5000, 10000, 15000]

async function stopBestEffort(connection: signalR.HubConnection) {
  try {
    await connection.stop()
  } catch {
    // Lifecycle invalidation remains authoritative when transport cleanup fails.
  }
}

export type DraftMontagemRealtimeHandler = (state: DraftMontagemRealtimeSnapshot) => void
export type DraftMontagemRealtimeReadyHandler = () => void | Promise<void>
export type DraftMontagemArchivedHandler = (draftMontagemId: string) => void | Promise<void>
export type DraftMontagemRealtimeDegradedHandler = (status: Exclude<DraftConnectionStatus, 'connected'>) => void

export class DraftMontagemRealtimeConnection {
  private connection: signalR.HubConnection | null = null
  private restartTimer: ReturnType<typeof setTimeout> | null = null
  private lifecycle = 0
  private disconnecting: Promise<void> | null = null

  constructor(private readonly draftMontagemId: string) {}

  async connect(
    onStateUpdated: DraftMontagemRealtimeHandler,
    onReady?: DraftMontagemRealtimeReadyHandler,
    onArchived?: DraftMontagemArchivedHandler,
    onDegraded?: DraftMontagemRealtimeDegradedHandler,
  ) {
    await this.disconnect()
    const lifecycle = ++this.lifecycle

    const baseUrl = String(api.defaults.baseURL ?? '').replace(/\/$/, '')
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${baseUrl}/hubs/draft-montagens`, { accessTokenFactory: () => getAccessToken() ?? '' })
      .withAutomaticReconnect(RETRY_DELAYS)
      .build()
    this.connection = connection
    let restartAttempt = 0
    let stoppingForRestart = false
    let lastDegradedStatus: Exclude<DraftConnectionStatus, 'connected'> | null = null

    const isCurrent = () => this.lifecycle === lifecycle && this.connection === connection
    const reportDegraded = (status: Exclude<DraftConnectionStatus, 'connected'>) => {
      if (!isCurrent() || status === lastDegradedStatus) return
      lastDegradedStatus = status
      onDegraded?.(status)
    }
    const reportReady = async () => {
      if (!isCurrent()) return
      lastDegradedStatus = null
      try {
        await onReady?.()
      } catch {
        // The view owns canonical GET failure and fallback activation after readiness.
      }
    }
    const scheduleRestart = () => {
      if (!isCurrent() || this.restartTimer !== null) return
      const delay = RETRY_DELAYS[Math.min(restartAttempt, RETRY_DELAYS.length - 1)]
      restartAttempt += 1
      this.restartTimer = setTimeout(() => {
        this.restartTimer = null
        if (!isCurrent()) return
        reportDegraded('reconnecting')
        void startAndJoin()
      }, delay)
    }
    const stopAndScheduleRestart = async () => {
      if (!isCurrent()) return
      reportDegraded('fallback')
      stoppingForRestart = true
      try {
        await stopBestEffort(connection)
      } finally {
        stoppingForRestart = false
      }
      scheduleRestart()
    }
    const join = async () => {
      try {
        await connection.invoke('JoinDraftMontagem', this.draftMontagemId)
      } catch {
        await stopAndScheduleRestart()
        return false
      }
      if (!isCurrent()) {
        await stopBestEffort(connection)
        return false
      }
      restartAttempt = 0
      await reportReady()
      return true
    }
    const startAndJoin = async () => {
      try {
        await connection.start()
      } catch {
        if (isCurrent()) {
          await stopAndScheduleRestart()
        } else {
          await stopBestEffort(connection)
        }
        return
      }
      if (!isCurrent()) {
        await stopBestEffort(connection)
        return
      }
      await join()
    }

    connection.on('DraftMontagemStateUpdated', (state) => {
      if (isCurrent()) onStateUpdated(state)
    })
    if (onArchived) {
      connection.on('DraftMontagemArchived', (archivedId) => {
        if (isCurrent()) void onArchived(archivedId)
      })
    }
    connection.onreconnecting(() => {
      reportDegraded('reconnecting')
    })
    connection.onreconnected(async () => {
      if (this.connection !== connection) return
      await join()
    })
    connection.onclose(() => {
      if (!isCurrent() || stoppingForRestart) return
      reportDegraded('disconnected')
      scheduleRestart()
    })

    await startAndJoin()
  }

  async disconnect() {
    if (this.disconnecting) {
      await this.disconnecting
      return
    }

    const connection = this.connection
    this.lifecycle += 1
    if (this.restartTimer !== null) {
      clearTimeout(this.restartTimer)
      this.restartTimer = null
    }
    if (!connection) {
      return
    }
    this.connection = null

    const disconnecting = (async () => {
      try {
        if (connection.state === signalR.HubConnectionState.Connected) {
          try {
            await connection.invoke('LeaveDraftMontagem', this.draftMontagemId)
          } catch {
            // Group cleanup is best-effort; stopping the connection is authoritative.
          }
        }
      } finally {
        await stopBestEffort(connection)
      }
    })()
    this.disconnecting = disconnecting
    try {
      await disconnecting
    } finally {
      if (this.disconnecting === disconnecting) this.disconnecting = null
    }
  }
}
