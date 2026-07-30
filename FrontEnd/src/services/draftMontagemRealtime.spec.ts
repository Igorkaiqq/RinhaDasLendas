import { beforeEach, describe, expect, it, vi } from 'vitest'

import type { DraftConnectionStatus, DraftMontagemRealtimeSnapshot } from '@/types/draftMontagem'

const signalRMock = vi.hoisted(() => {
  let stateUpdated: ((state: DraftMontagemRealtimeSnapshot) => void) | undefined
  let archived: ((draftMontagemId: string) => void) | undefined
  const connection = {
    on: vi.fn((event: string, handler: (state: DraftMontagemRealtimeSnapshot) => void) => {
      if (event === 'DraftMontagemStateUpdated') stateUpdated = handler
      if (event === 'DraftMontagemArchived') archived = handler as unknown as (draftMontagemId: string) => void
    }),
    onreconnecting: vi.fn(),
    onreconnected: vi.fn(),
    onclose: vi.fn(),
    state: 'Connected',
    start: vi.fn().mockResolvedValue(undefined),
    invoke: vi.fn().mockResolvedValue(undefined),
    stop: vi.fn().mockResolvedValue(undefined),
  }
  const builder = {
    withUrl: vi.fn().mockReturnThis(),
    withAutomaticReconnect: vi.fn().mockReturnThis(),
    build: vi.fn(() => connection),
  }

  return {
    connection,
    builder,
    emitStateUpdated: (state: DraftMontagemRealtimeSnapshot) => stateUpdated?.(state),
    emitArchived: (draftMontagemId: string) => archived?.(draftMontagemId),
  }
})

vi.mock('@microsoft/signalr', () => ({
  HubConnectionState: {
    Connected: 'Connected',
  },
  HubConnectionBuilder: vi.fn(function HubConnectionBuilder() {
    return signalRMock.builder
  }),
}))

import { DraftMontagemRealtimeConnection } from './draftMontagemRealtime'

function lifecycleHandlers() {
  const statuses: DraftConnectionStatus[] = []
  return {
    onStateUpdated: vi.fn(),
    onReady: vi.fn().mockResolvedValue(undefined),
    onArchived: vi.fn(),
    onDegraded: vi.fn((status: Exclude<DraftConnectionStatus, 'connected'>) => statuses.push(status)),
    statuses,
  }
}

describe('DraftMontagemRealtimeConnection', () => {
  beforeEach(() => {
    vi.useRealTimers()
    vi.clearAllMocks()
    signalRMock.connection.state = 'Connected'
    signalRMock.connection.start.mockResolvedValue(undefined)
    signalRMock.connection.invoke.mockResolvedValue(undefined)
    signalRMock.connection.stop.mockResolvedValue(undefined)
    signalRMock.connection.on.mockImplementation((event: string, handler: (state: DraftMontagemRealtimeSnapshot) => void) => {
      if (event === 'DraftMontagemStateUpdated') {
        signalRMock.emitStateUpdated = (state: DraftMontagemRealtimeSnapshot) => handler(state)
      }
      if (event === 'DraftMontagemArchived') {
        signalRMock.emitArchived = (draftMontagemId: string) =>
          (handler as unknown as (id: string) => void)(draftMontagemId)
      }
    })
    signalRMock.connection.onreconnecting.mockImplementation(() => undefined)
    signalRMock.connection.onreconnected.mockImplementation(() => undefined)
    signalRMock.connection.onclose.mockImplementation(() => undefined)
  })

  it('registers callbacks and exact retry delays before starting, then hands off readiness after Join', async () => {
    const order: string[] = []
    signalRMock.connection.on.mockImplementation((event: string) => order.push(`on:${event}`))
    signalRMock.connection.onreconnecting.mockImplementation(() => order.push('onreconnecting'))
    signalRMock.connection.onreconnected.mockImplementation(() => order.push('onreconnected'))
    signalRMock.connection.onclose.mockImplementation(() => order.push('onclose'))
    signalRMock.connection.start.mockImplementation(async () => {
      order.push('start')
    })
    signalRMock.connection.invoke.mockImplementation(async (method: string) => {
      order.push(method)
    })
    const handlers = lifecycleHandlers()
    handlers.onReady.mockImplementation(async () => {
      order.push('ready')
    })

    await new DraftMontagemRealtimeConnection('draft-1').connect(
      handlers.onStateUpdated,
      handlers.onReady,
      handlers.onArchived,
      handlers.onDegraded,
    )

    expect(signalRMock.builder.withAutomaticReconnect).toHaveBeenCalledWith([0, 2000, 5000, 10000, 15000])
    expect(order).toEqual([
      'on:DraftMontagemStateUpdated',
      'on:DraftMontagemArchived',
      'onreconnecting',
      'onreconnected',
      'onclose',
      'start',
      'JoinDraftMontagem',
      'ready',
    ])
    expect(handlers.statuses).not.toContain('connected')
  })

  it('rejoins and hands off readiness after recovering without declaring connected', async () => {
    const handlers = lifecycleHandlers()
    const connection = new DraftMontagemRealtimeConnection('draft-1')

    await connection.connect(handlers.onStateUpdated, handlers.onReady, handlers.onArchived, handlers.onDegraded)
    handlers.onReady.mockClear()
    signalRMock.connection.invoke.mockClear()
    const reconnectHandler = signalRMock.connection.onreconnected.mock.calls[0]?.[0]
    expect(reconnectHandler).toBeTypeOf('function')
    await reconnectHandler()

    expect(signalRMock.connection.invoke).toHaveBeenCalledWith('JoinDraftMontagem', 'draft-1')
    expect(handlers.onReady).toHaveBeenCalledOnce()
    expect(handlers.statuses).not.toContain('connected')
  })

  it('degrades, stops and schedules one controlled restart when the initial Join fails', async () => {
    vi.useFakeTimers()
    const handlers = lifecycleHandlers()
    signalRMock.connection.invoke.mockRejectedValueOnce(new Error('join failed'))

    await expect(
      new DraftMontagemRealtimeConnection('draft-1').connect(
        handlers.onStateUpdated,
        handlers.onReady,
        handlers.onArchived,
        handlers.onDegraded,
      ),
    ).resolves.toBeUndefined()

    expect(handlers.statuses).toEqual(['fallback'])
    expect(handlers.onReady).not.toHaveBeenCalled()
    expect(signalRMock.connection.stop).toHaveBeenCalledOnce()
    expect(vi.getTimerCount()).toBe(1)
  })

  it('degrades and controls restart when rejoin fails', async () => {
    vi.useFakeTimers()
    const handlers = lifecycleHandlers()
    const connection = new DraftMontagemRealtimeConnection('draft-1')
    await connection.connect(handlers.onStateUpdated, handlers.onReady, handlers.onArchived, handlers.onDegraded)
    handlers.onReady.mockClear()
    signalRMock.connection.invoke.mockRejectedValueOnce(new Error('rejoin failed'))

    await signalRMock.connection.onreconnected.mock.calls[0]?.[0]()

    expect(handlers.statuses).toEqual(['fallback'])
    expect(handlers.onReady).not.toHaveBeenCalled()
    expect(signalRMock.connection.stop).toHaveBeenCalledOnce()
    expect(vi.getTimerCount()).toBe(1)
  })

  it('reports transport loss and schedules only one restart timer on repeated close notifications', async () => {
    vi.useFakeTimers()
    const handlers = lifecycleHandlers()
    const connection = new DraftMontagemRealtimeConnection('draft-1')
    await connection.connect(handlers.onStateUpdated, handlers.onReady, handlers.onArchived, handlers.onDegraded)
    const closeHandler = signalRMock.connection.onclose.mock.calls[0]?.[0]

    closeHandler()
    closeHandler()

    expect(handlers.statuses).toEqual(['disconnected'])
    expect(vi.getTimerCount()).toBe(1)
  })

  it('reports reconnecting while SignalR is recovering', async () => {
    const handlers = lifecycleHandlers()
    const connection = new DraftMontagemRealtimeConnection('draft-1')
    await connection.connect(handlers.onStateUpdated, handlers.onReady, handlers.onArchived, handlers.onDegraded)

    signalRMock.connection.onreconnecting.mock.calls[0]?.[0]()

    expect(handlers.statuses).toEqual(['reconnecting'])
  })

  it('tears down idempotently and ignores stale lifecycle callbacks', async () => {
    vi.useFakeTimers()
    const handlers = lifecycleHandlers()
    const connection = new DraftMontagemRealtimeConnection('draft-1')
    await connection.connect(handlers.onStateUpdated, handlers.onReady, handlers.onArchived, handlers.onDegraded)
    const closeHandler = signalRMock.connection.onclose.mock.calls[0]?.[0]
    const reconnectHandler = signalRMock.connection.onreconnected.mock.calls[0]?.[0]

    await Promise.all([connection.disconnect(), connection.disconnect()])
    closeHandler()
    await reconnectHandler()
    signalRMock.emitArchived('draft-1')

    expect(signalRMock.connection.invoke).toHaveBeenCalledWith('LeaveDraftMontagem', 'draft-1')
    expect(signalRMock.connection.stop).toHaveBeenCalledOnce()
    expect(handlers.onReady).toHaveBeenCalledOnce()
    expect(handlers.onArchived).not.toHaveBeenCalled()
    expect(handlers.onDegraded).not.toHaveBeenCalled()
    expect(vi.getTimerCount()).toBe(0)
  })

  it('does not report an error when disconnected while the connection is starting', async () => {
    let rejectStart: ((error: Error) => void) | undefined
    signalRMock.connection.state = 'Connecting'
    signalRMock.connection.start.mockImplementationOnce(
      () =>
        new Promise<void>((_, reject) => {
          rejectStart = reject
        }),
    )
    signalRMock.connection.stop.mockImplementationOnce(async () => {
      rejectStart?.(new Error('Failed to start the HttpConnection before stop() was called.'))
    })
    const connection = new DraftMontagemRealtimeConnection('draft-1')

    const connecting = connection.connect(vi.fn())
    await vi.waitFor(() => expect(signalRMock.connection.start).toHaveBeenCalledOnce())
    const connectingExpectation = expect(connecting).resolves.toBeUndefined()

    await expect(connection.disconnect()).resolves.toBeUndefined()
    await connectingExpectation
    expect(signalRMock.connection.invoke).not.toHaveBeenCalledWith('JoinDraftMontagem', 'draft-1')
  })

  it('still stops cleanly when best-effort Leave fails', async () => {
    const connection = new DraftMontagemRealtimeConnection('draft-1')
    await connection.connect(vi.fn())
    signalRMock.connection.invoke.mockRejectedValueOnce(new Error('leave failed'))

    await expect(connection.disconnect()).resolves.toBeUndefined()

    expect(signalRMock.connection.invoke).toHaveBeenCalledWith('LeaveDraftMontagem', 'draft-1')
    expect(signalRMock.connection.stop).toHaveBeenCalledOnce()
  })

  it('consumes the shared event without personalized HTTP fields', async () => {
    const state: DraftMontagemRealtimeSnapshot = {
      montagem: {
        id: 'draft-1',
        nome: 'Rinha',
        status: 'PresencaAberta',
        modo: 'Manual',
        cicloVersao: 'Legado',
        tamanhoEquipe: 5,
        quantidadeTimes: 0,
        quantidadeReservas: 0,
        criterioCapitaes: 'Manual',
        duracaoTurnoSegundos: 30,
        presencaContinuadaManualmente: false,
        presencas: [],
        times: [],
        livres: [],
        reservas: [],
        escolhas: [],
        substituicoes: [],
        publicacoesDiscord: [{ tipo: 'Presenca', status: 'RequerReconciliacao' }],
        arquivado: false,
        versaoEstado: 2,
        dataCadastro: '2026-07-21T11:00:00Z',
        dataAtualizacao: '2026-07-21T12:00:00Z',
      },
      serverNow: '2026-07-21T12:00:01Z',
    }
    const handler = vi.fn()
    const connection = new DraftMontagemRealtimeConnection('draft-1')

    await connection.connect(handler)
    signalRMock.emitStateUpdated(state)

    expect(handler).toHaveBeenCalledWith(state)
    expect(JSON.stringify(handler.mock.calls[0]?.[0])).not.toMatch(
      /canCurrentUserPick|guildId|channelId|messageId|ultimoErroCodigo|claimId|responsavelUsuarioId|discordUserId|motivo/,
    )
  })

  it('registers and delivers the archive event as an ID-only payload', async () => {
    const onArchived = vi.fn()
    const connection = new DraftMontagemRealtimeConnection('draft-1')

    await connection.connect(vi.fn(), undefined, onArchived)
    signalRMock.emitArchived('draft-1')

    expect(signalRMock.connection.on).toHaveBeenCalledWith('DraftMontagemArchived', expect.any(Function))
    expect(onArchived).toHaveBeenCalledWith('draft-1')
  })
})
