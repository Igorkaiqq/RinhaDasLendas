import { beforeEach, describe, expect, it, vi } from 'vitest'

const signalRMock = vi.hoisted(() => {
  const connection = {
    on: vi.fn(),
    onreconnected: vi.fn(),
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

  return { connection, builder }
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

describe('DraftMontagemRealtimeConnection', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    signalRMock.connection.state = 'Connected'
    signalRMock.connection.start.mockResolvedValue(undefined)
    signalRMock.connection.invoke.mockResolvedValue(undefined)
    signalRMock.connection.stop.mockResolvedValue(undefined)
  })

  it('rejoins and refreshes draft state after reconnecting', async () => {
    const onRefresh = vi.fn().mockResolvedValue(undefined)
    const connection = new DraftMontagemRealtimeConnection('draft-1')

    await connection.connect(vi.fn(), onRefresh)
    const reconnectHandler = signalRMock.connection.onreconnected.mock.calls[0]?.[0]
    expect(reconnectHandler).toBeTypeOf('function')
    await reconnectHandler()

    expect(signalRMock.connection.invoke).toHaveBeenCalledWith('JoinDraftMontagem', 'draft-1')
    expect(onRefresh).toHaveBeenCalledOnce()
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
})
