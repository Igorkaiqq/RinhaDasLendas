import * as signalR from '@microsoft/signalr'

import type { DraftConnectionStatus, DraftMontagemRealtimeSnapshot } from '@/types/draftMontagem'

import { api } from './api'
import { getAccessToken } from './authState'

const RETRY_DELAYS = [0, 2000, 5000, 10000, 15000]
const MAX_LOG_DECODE_ATTEMPTS = 3
const CREDENTIAL_LOG_MESSAGE = 'SignalR transport log omitted because it may contain credentials.'
const ACCESS_TOKEN_PARAMETER = /(?:^|[?&#;\s])access_token\s*=/i
const ENCODED_ACCESS_TOKEN_PARAMETER = /access(?:_|%(?:25)*5f)token(?:=|%(?:25)*3d)/i
const URL_OR_QUERY = /(?:\b(?:https?|wss?):\/\/|[?&#]|%(?:25)*(?:3f|26|23))/i
const MALFORMED_PERCENT_ENCODING = /%(?![0-9a-f]{2})/i

function sanitizeSignalRLogMessage(message: string) {
  let candidate = message
  for (let attempt = 0; attempt <= MAX_LOG_DECODE_ATTEMPTS; attempt++) {
    if (ACCESS_TOKEN_PARAMETER.test(candidate) || ENCODED_ACCESS_TOKEN_PARAMETER.test(candidate)) {
      return CREDENTIAL_LOG_MESSAGE
    }

    const hasUrlOrQuery = URL_OR_QUERY.test(candidate)
    if (MALFORMED_PERCENT_ENCODING.test(candidate)) {
      return hasUrlOrQuery || /access.*token/i.test(candidate) ? CREDENTIAL_LOG_MESSAGE : message
    }
    if (attempt === MAX_LOG_DECODE_ATTEMPTS) {
      return hasUrlOrQuery && /%[0-9a-f]{2}/i.test(candidate) ? CREDENTIAL_LOG_MESSAGE : message
    }

    let decoded: string
    try {
      decoded = decodeURIComponent(candidate)
    } catch {
      return hasUrlOrQuery ? CREDENTIAL_LOG_MESSAGE : message
    }
    if (decoded === candidate) return message
    candidate = decoded
  }
  return CREDENTIAL_LOG_MESSAGE
}

const signalRLogger: signalR.ILogger = {
  log(logLevel, message) {
    if (logLevel < signalR.LogLevel.Warning || logLevel >= signalR.LogLevel.None) return
    const safeMessage = sanitizeSignalRLogMessage(message)
    if (logLevel === signalR.LogLevel.Warning) {
      globalThis.console.warn(safeMessage)
      return
    }
    globalThis.console.error(safeMessage)
  },
}

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
export type DraftMontagemRestoredHandler = (draftMontagemId: string) => void | Promise<void>
export type DraftMontagemRealtimeDegradedHandler = (status: Exclude<DraftConnectionStatus, 'connected'>) => void

export class DraftMontagemRealtimeConnection {
  private connection: signalR.HubConnection | null = null
  private restartTimer: ReturnType<typeof setTimeout> | null = null
  private lifecycle = 0
  private disconnecting: Promise<void> | null = null

  constructor(private readonly draftMontagemId?: string) {}

  async connect(
    onStateUpdated: DraftMontagemRealtimeHandler,
    onReady?: DraftMontagemRealtimeReadyHandler,
    onArchived?: DraftMontagemArchivedHandler,
    onDegraded?: DraftMontagemRealtimeDegradedHandler,
    onRestored?: DraftMontagemRestoredHandler,
  ) {
    await this.disconnect()
    const lifecycle = ++this.lifecycle

    const baseUrl = String(api.defaults.baseURL ?? '').replace(/\/$/, '')
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${baseUrl}/hubs/draft-montagens`, { accessTokenFactory: () => getAccessToken() ?? '' })
      .configureLogging(signalRLogger)
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
      if (!this.draftMontagemId) {
        restartAttempt = 0
        await reportReady()
        return true
      }
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
    if (onRestored) {
      connection.on('DraftMontagemRestored', (restoredId) => {
        if (isCurrent()) void onRestored(restoredId)
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
        if (this.draftMontagemId && connection.state === signalR.HubConnectionState.Connected) {
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
