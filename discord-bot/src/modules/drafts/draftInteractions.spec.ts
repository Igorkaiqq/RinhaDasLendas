import { afterEach, describe, it, mock } from 'node:test'
import assert from 'node:assert/strict'
import { MessageFlags, PermissionFlagsBits, PermissionsBitField } from 'discord.js'
import type { ChatInputCommandInteraction, Client } from 'discord.js'
import type { DraftMontagem } from '../../shared/api/types.js'

import { assertDiscordBotEnabled, getDraftInteractionErrorMessage, handleDraftCommand, parsePresenceClosingTime, runDraftPollingCycle, validatePresenceClosingTime } from './draftInteractions.js'
import { RinhaApiError, rinhaApi } from '../../shared/api/rinhaApi.js'
import { env } from '../../config/env.js'
import { logger } from '../../shared/logger.js'
import { t } from '../../shared/messages/index.js'

const originalNotifyRoleId = env.DRAFT_NOTIFY_ROLE_ID

afterEach(() => {
  mock.restoreAll()
  env.DRAFT_NOTIFY_ROLE_ID = originalNotifyRoleId
})

function pollingDraft(id: string, publicationStatus?: string): DraftMontagem {
  return {
    id,
    nome: `Rinha ${id}`,
    status: 'PresencaAberta',
    tamanhoEquipe: 5,
    quantidadeTimes: 2,
    quantidadeReservas: 0,
    publicacoesDiscord: publicationStatus ? [{ tipo: 'Presenca', status: publicationStatus }] : [],
    presencas: [],
    times: [],
    reservas: [],
  }
}

function pollingClient(send: (options: unknown) => Promise<{ id: string }>) {
  return {
    channels: {
      fetch: async () => ({ isTextBased: () => true, send }),
    },
    user: null,
  } as unknown as Client
}

function pollingClientWithChannelFailure() {
  return {
    channels: { fetch: async () => null },
    user: null,
  } as unknown as Client
}

function mockPollingApi(drafts: ReturnType<typeof pollingDraft>[]) {
  env.DRAFT_NOTIFY_ROLE_ID = ''
  mock.method(rinhaApi, 'getDiscordConfiguration', async () => ({ guildId: 'guild', presenceChannelId: 'presence', draftChannelId: 'draft', botEnabled: true }))
  mock.method(rinhaApi, 'listActiveDrafts', async () => drafts)
}

function deferred() {
  let resolve!: () => void
  const promise = new Promise<void>((resolvePromise) => {
    resolve = resolvePromise
  })
  return { promise, resolve }
}

describe('runDraftPollingCycle', () => {
  it('does not send when the claim is denied', async () => {
    mockPollingApi([pollingDraft('draft-1')])
    mock.method(rinhaApi, 'claimDiscordPublication', async () => ({ adquirido: false, claimId: null, expiraEm: null, status: 'EmAndamento' }))
    const complete = mock.method(rinhaApi, 'registerDiscordPublication', async () => pollingDraft('draft-1') as never)
    const send = mock.fn(async () => ({ id: 'message-1' }))

    await runDraftPollingCycle(pollingClient(send))

    assert.equal(send.mock.callCount(), 0)
    assert.equal(complete.mock.callCount(), 0)
  })

  it('completes an acquired claim with the same claim id', async () => {
    mockPollingApi([pollingDraft('draft-1')])
    mock.method(rinhaApi, 'claimDiscordPublication', async () => ({ adquirido: true, claimId: 'claim-1', expiraEm: '2026-07-21T10:05:00Z', status: 'EmAndamento' }))
    const complete = mock.method(rinhaApi, 'registerDiscordPublication', async () => pollingDraft('draft-1') as never)

    await runDraftPollingCycle(pollingClient(async () => ({ id: 'message-1' })))

    assert.equal(complete.mock.callCount(), 1)
    const completeCall = complete.mock.calls[0]
    assert.ok(completeCall)
    const completePayload = completeCall.arguments[1]
    assert.ok(completePayload)
    assert.equal(completePayload.claimId, 'claim-1')
    assert.equal(completePayload.messageId, 'message-1')
  })

  it('registers failure when channel validation fails before send starts', async () => {
    mockPollingApi([pollingDraft('draft-1')])
    mock.method(rinhaApi, 'claimDiscordPublication', async () => ({ adquirido: true, claimId: 'claim-1', expiraEm: '2026-07-21T10:05:00Z', status: 'EmAndamento' }))
    const failure = mock.method(rinhaApi, 'registerDiscordPublicationFailure', async () => pollingDraft('draft-1') as never)
    const complete = mock.method(rinhaApi, 'registerDiscordPublication', async () => pollingDraft('draft-1') as never)

    await runDraftPollingCycle(pollingClientWithChannelFailure())

    assert.equal(failure.mock.callCount(), 1)
    assert.equal(complete.mock.callCount(), 0)
    const failureCall = failure.mock.calls[0]
    assert.ok(failureCall)
    const failurePayload = failureCall.arguments[1]
    assert.ok(failurePayload)
    assert.equal(failurePayload.claimId, 'claim-1')
  })

  it('keeps the claim in progress when send rejects', async () => {
    mockPollingApi([pollingDraft('draft-1')])
    mock.method(rinhaApi, 'claimDiscordPublication', async () => ({ adquirido: true, claimId: 'claim-1', expiraEm: null, status: 'EmAndamento' }))
    const failure = mock.method(rinhaApi, 'registerDiscordPublicationFailure', async () => pollingDraft('draft-1') as never)
    const complete = mock.method(rinhaApi, 'registerDiscordPublication', async () => pollingDraft('draft-1') as never)
    const logError = mock.method(logger, 'error', () => undefined)

    await runDraftPollingCycle(pollingClient(async () => { throw new Error('sensitive send detail') }))

    assert.equal(failure.mock.callCount(), 0)
    assert.equal(complete.mock.callCount(), 0)
    assert.equal(JSON.stringify(logError.mock.calls).includes('sensitive send detail'), false)
  })

  it('keeps the claim in progress when backend completion fails after send', async () => {
    mockPollingApi([pollingDraft('draft-1')])
    mock.method(rinhaApi, 'claimDiscordPublication', async () => ({ adquirido: true, claimId: 'claim-1', expiraEm: null, status: 'EmAndamento' }))
    const failure = mock.method(rinhaApi, 'registerDiscordPublicationFailure', async () => pollingDraft('draft-1') as never)
    mock.method(rinhaApi, 'registerDiscordPublication', async () => { throw new Error('sensitive backend detail') })
    const logError = mock.method(logger, 'error', () => undefined)

    await runDraftPollingCycle(pollingClient(async () => ({ id: 'message-1' })))

    assert.equal(failure.mock.callCount(), 0)
    assert.equal(JSON.stringify(logError.mock.calls).includes('sensitive backend detail'), false)
  })

  it('does not send again in the next cycle while the claim is in progress', async () => {
    mockPollingApi([pollingDraft('draft-1')])
    let claimAttempt = 0
    mock.method(rinhaApi, 'claimDiscordPublication', async () => ++claimAttempt === 1
      ? { adquirido: true, claimId: 'claim-1', expiraEm: null, status: 'EmAndamento' }
      : { adquirido: false, claimId: null, expiraEm: null, status: 'EmAndamento' })
    const failure = mock.method(rinhaApi, 'registerDiscordPublicationFailure', async () => pollingDraft('draft-1') as never)
    const send = mock.fn(async () => { throw new Error('send result unknown') })
    const client = pollingClient(send)

    await runDraftPollingCycle(client)
    await runDraftPollingCycle(client)

    assert.equal(send.mock.callCount(), 1)
    assert.equal(failure.mock.callCount(), 0)
  })

  it('does not claim or send a publication requiring reconciliation', async () => {
    mockPollingApi([pollingDraft('draft-1', 'RequerReconciliacao')])
    const claim = mock.method(rinhaApi, 'claimDiscordPublication', async () => ({ adquirido: true, claimId: 'claim-1', expiraEm: null, status: 'EmAndamento' }))
    const send = mock.fn(async () => ({ id: 'message-1' }))

    await runDraftPollingCycle(pollingClient(send))

    assert.equal(claim.mock.callCount(), 0)
    assert.equal(send.mock.callCount(), 0)
  })

  it('continues with the second draft when the first publication fails', async () => {
    mockPollingApi([pollingDraft('draft-1'), pollingDraft('draft-2')])
    let claimNumber = 0
    mock.method(rinhaApi, 'claimDiscordPublication', async () => ({ adquirido: true, claimId: `claim-${++claimNumber}`, expiraEm: null, status: 'EmAndamento' }))
    const complete = mock.method(rinhaApi, 'registerDiscordPublication', async () => pollingDraft('draft-2') as never)
    const failure = mock.method(rinhaApi, 'registerDiscordPublicationFailure', async () => pollingDraft('draft-1') as never)
    let sendNumber = 0

    await runDraftPollingCycle(pollingClient(async () => {
      if (++sendNumber === 1) throw new Error('first failed')
      return { id: 'message-2' }
    }))

    assert.equal(failure.mock.callCount(), 0)
    assert.equal(complete.mock.callCount(), 1)
    assert.equal(complete.mock.calls[0].arguments[0], 'draft-2')
  })

  it('allows only one send across two concurrent polling cycles', async () => {
    mockPollingApi([pollingDraft('draft-1')])
    const ready = deferred()
    const release = deferred()
    let waitingClaims = 0
    let claimNumber = 0
    mock.method(rinhaApi, 'claimDiscordPublication', async () => {
      if (++waitingClaims === 2) ready.resolve()
      await release.promise
      return ++claimNumber === 1
        ? { adquirido: true, claimId: 'claim-1', expiraEm: null, status: 'EmAndamento' }
        : { adquirido: false, claimId: null, expiraEm: null, status: 'EmAndamento' }
    })
    mock.method(rinhaApi, 'registerDiscordPublication', async () => pollingDraft('draft-1') as never)
    const send = mock.fn(async () => ({ id: 'message-1' }))
    const client = pollingClient(send)

    const cycles = [runDraftPollingCycle(client), runDraftPollingCycle(client)]
    await ready.promise
    release.resolve()
    await Promise.all(cycles)

    assert.equal(send.mock.callCount(), 1)
  })
})

function cancelInteraction(options: {
  memberPermissions?: PermissionsBitField
  roleIds?: string[]
}) {
  const replies: unknown[] = []
  const interaction = {
    commandName: 'draft-cancelar',
    id: 'interaction-1',
    replied: false,
    memberPermissions: options.memberPermissions ?? new PermissionsBitField(),
    member: { roles: { cache: new Map((options.roleIds ?? []).map((roleId) => [roleId, {}])) } },
    options: {
      getString: (name: string) => name === 'draft-id' ? 'draft-1' : null,
    },
    reply: async (payload: unknown) => {
      replies.push(payload)
    },
  } as unknown as ChatInputCommandInteraction

  return { interaction, replies }
}

describe('handleDraftCommand authorization', () => {
  it('allows a member with ManageGuild to run a mutable command', async () => {
    const cancelDraft = mock.method(rinhaApi, 'cancelDraft', async () => ({} as never))
    const { interaction } = cancelInteraction({
      memberPermissions: new PermissionsBitField(PermissionFlagsBits.ManageGuild),
    })

    await handleDraftCommand(interaction)

    assert.equal(cancelDraft.mock.callCount(), 1)
  })

  it('allows a member with a configured draft administrator role', async () => {
    const previousRoleIds = env.DRAFT_ADMIN_ROLE_IDS
    env.DRAFT_ADMIN_ROLE_IDS = 'role-1, role-2'
    const cancelDraft = mock.method(rinhaApi, 'cancelDraft', async () => ({} as never))
    const { interaction } = cancelInteraction({ roleIds: ['role-2'] })

    try {
      await handleDraftCommand(interaction)
      assert.equal(cancelDraft.mock.callCount(), 1)
    } finally {
      env.DRAFT_ADMIN_ROLE_IDS = previousRoleIds
    }
  })

  it('denies a member without permission before calling the mutable API', async () => {
    const cancelDraft = mock.method(rinhaApi, 'cancelDraft', async () => ({} as never))
    const { interaction, replies } = cancelInteraction({ roleIds: ['other-role'] })

    await handleDraftCommand(interaction)

    assert.equal(cancelDraft.mock.callCount(), 0)
    assert.deepEqual(replies, [{ content: t.draftAdministrationDenied, flags: MessageFlags.Ephemeral }])
  })
})

describe('parsePresenceClosingTime', () => {
  it('interprets the informed time as Brasilia time', () => {
    const result = parsePresenceClosingTime(
      '11/07/2026',
      '19:30',
      new Date('2026-07-10T12:00:00.000Z'),
    )

    assert.equal(result, '2026-07-11T22:30:00.000Z')
  })
})

describe('validatePresenceClosingTime', () => {
  it('rejects explicit dates in the past', () => {
    const result = validatePresenceClosingTime('09/07/2026', '19:30', new Date('2026-07-10T12:00:00.000Z'))

    assert.deepEqual(result, { ok: false, reason: 'past' })
  })

  it('distinguishes invalid calendar dates', () => {
    const result = validatePresenceClosingTime('31/02/2026', '19:30', new Date('2026-07-10T12:00:00.000Z'))

    assert.deepEqual(result, { ok: false, reason: 'invalid-date' })
  })
})

describe('assertDiscordBotEnabled', () => {
  it('throws the integration unavailable message when bot is disabled', () => {
    assert.throws(
      () => assertDiscordBotEnabled({ guildId: 'guild', presenceChannelId: 'presence', draftChannelId: 'draft', botEnabled: false }),
      /A integração está indisponível no momento/,
    )
  })
})

describe('getDraftInteractionErrorMessage', () => {
  it('maps API authentication failures to the internal token guidance', () => {
    const result = getDraftInteractionErrorMessage(new Error('Unauthorized: token invalid'), 'create')

    assert.equal(result, 'A autenticação interna do bot falhou. Verifique RINHA_API_INTERNAL_TOKEN no backend e no bot.')
  })

  it('maps public API message codes to specific messages', () => {
    const scenarios = [
      ['MV079', 'create', t.internalTokenInvalid],
      ['MV063', 'confirmPresence', t.accountNotLinked],
      ['ME033', 'confirmPresence', t.playerProfileIncomplete],
      ['MV012', 'confirmPresence', t.playerInactive],
      ['MV072', 'confirmPresence', t.draftErrors.presenceAlreadyClosed],
      ['MV072', 'cancelPresence', t.draftErrors.presenceClosedCannotCancel],
      ['MV073', 'cancelPresence', t.draftErrors.presenceNotConfirmed],
      ['MV014', 'confirmPresence', t.draftErrors.presenceAlreadyConfirmed],
      ['MV074', 'closePresence', t.draftErrors.closePresenceLessThanTen],
      ['MV075', 'defineCaptains', t.draftErrors.presenceStillOpen],
      ['MV048', 'definePickOrder', t.draftErrors.missingCaptains],
      ['MV050', 'defineCaptains', t.draftErrors.captainNotConfirmed],
      ['MV076', 'definePickOrder', t.draftErrors.invalidManualPickOrder],
      ['MV029', 'cancel', t.draftErrors.draftAlreadyClosed],
      ['ME035', 'status', t.draftNotFoundMaybeFinished],
    ] as const

    for (const [messageCode, context, expected] of scenarios) {
      const result = getDraftInteractionErrorMessage(new RinhaApiError(messageCode, 'technical detail', 400), context)
      assert.equal(result, expected, messageCode)
    }
  })

  it('maps network failures to the API unavailable message', () => {
    const result = getDraftInteractionErrorMessage(new TypeError('fetch failed'), 'list')

    assert.equal(result, 'Não consegui carregar os drafts ativos agora. Tente novamente em instantes.')
  })

  it('maps not found errors to the draft not found message', () => {
    const result = getDraftInteractionErrorMessage(new Error('404 draft not found'), 'cancel')

    assert.equal(result, 'Não encontrei esse draft. Confira o ID e tente novamente.')
  })

  it('maps missing Discord channel permissions to permission guidance', () => {
    const result = getDraftInteractionErrorMessage(new Error('Canal Lista de Presença está sem permissão para o bot'), 'create')

    assert.equal(result, 'Estou sem permissão no canal de presença. Libere Ver canal, Enviar mensagens, Incorporar links e Mencionar cargos.')
  })

  it('maps closed presence errors for confirmation buttons', () => {
    const result = getDraftInteractionErrorMessage(new Error('presenca encerrada'), 'confirmPresence')

    assert.equal(result, 'Essa lista de presença já foi encerrada.')
  })

  it('maps incomplete player profile errors for confirmation buttons', () => {
    const result = getDraftInteractionErrorMessage(new Error('perfil jogador incompleto'), 'confirmPresence')

    assert.equal(result, 'Sua conta ainda não tem perfil de jogador completo. Complete o perfil no site para participar.')
  })

  it('maps invalid captain ids to a specific captain message', () => {
    const result = getDraftInteractionErrorMessage(new Error('ids invalidos'), 'defineCaptains')

    assert.equal(result, 'Não consegui definir os capitães. Envie IDs de jogadores confirmados, separados por vírgula.')
  })

  it('maps manual pick order without ids to a specific pick order message', () => {
    const result = getDraftInteractionErrorMessage(new Error('ordem manual sem capitaes'), 'definePickOrder')

    assert.equal(result, 'Para modo Manual, informe os IDs dos capitães na ordem desejada.')
  })

  it('falls back to the context generic message', () => {
    const result = getDraftInteractionErrorMessage(new Error('unexpected'), 'create')

    assert.equal(result, 'Não foi possível criar a lista de presença. Tente novamente ou chame um ADM.')
  })
})
