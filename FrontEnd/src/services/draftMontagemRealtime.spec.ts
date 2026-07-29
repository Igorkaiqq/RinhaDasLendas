import { beforeEach, describe, expect, it, vi } from 'vitest'

import type { DraftMontagemRealtimeState } from '@/types/draftMontagem'

const signalRMock = vi.hoisted(() => {
  let stateUpdated: ((state: DraftMontagemRealtimeState) => void) | undefined
  let archived: ((draftMontagemId: string) => void) | undefined
  const connection = {
    on: vi.fn((event: string, handler: (state: DraftMontagemRealtimeState) => void) => {
      if (event === 'DraftMontagemStateUpdated') stateUpdated = handler
      if (event === 'DraftMontagemArchived') archived = handler as unknown as (draftMontagemId: string) => void
    }),
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

  return {
    connection,
    builder,
    emitStateUpdated: (state: DraftMontagemRealtimeState) => stateUpdated?.(state),
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

  it('consumes a sanitized public reconciliation projection', async () => {
    const state: DraftMontagemRealtimeState = {
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
      canCurrentUserPick: false,
    }
    const handler = vi.fn()
    const connection = new DraftMontagemRealtimeConnection('draft-1')

    await connection.connect(handler)
    signalRMock.emitStateUpdated(state)

    expect(handler).toHaveBeenCalledOnce()
    expect(handler).toHaveBeenCalledWith(state)
    expect(JSON.stringify(handler.mock.calls[0]?.[0])).not.toMatch(/guildId|channelId|messageId|ultimoErroCodigo|claimId|responsavelUsuarioId|discordUserId|motivo/)
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
