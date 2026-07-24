import { afterEach, describe, it, mock } from 'node:test'
import assert from 'node:assert/strict'
import { MessageFlags, PermissionFlagsBits, PermissionsBitField } from 'discord.js'
import type { ButtonInteraction, ChatInputCommandInteraction, Client } from 'discord.js'
import type { DraftMontagem } from '../../shared/api/types.js'

import { assertDiscordBotEnabled, getDraftInteractionErrorMessage, getSendableChannel, handleDraftCommand, handlePresenceButton, parsePresenceClosingTime, runDraftPollingCycle, validatePresenceClosingTime } from './draftInteractions.js'
import { RinhaApiError, rinhaApi } from '../../shared/api/rinhaApi.js'
import { env } from '../../config/env.js'
import { logger } from '../../shared/logger.js'
import { t } from '../../shared/messages/index.js'
import { enUS } from '../../shared/messages/en-US.js'
import { ptBR } from '../../shared/messages/pt-BR.js'
import { buildDraftPresenceCta } from '../../discord/embeds/draftEmbeds.js'
import { DraftOptionNames } from '../../shared/constants/draftConstants/index.js'

const originalNotifyRoleId = env.DRAFT_NOTIFY_ROLE_ID
type DraftMontagemDiscordOperationalDto = Awaited<ReturnType<typeof rinhaApi.listActiveDrafts>>[number]

afterEach(() => {
  mock.restoreAll()
  env.DRAFT_NOTIFY_ROLE_ID = originalNotifyRoleId
})

function pollingDraft(id: string, publicationStatus?: string, publicationType: 'Presenca' | 'ChamadaPresenca' | 'TimesDefinidos' = 'Presenca'): DraftMontagem {
  return {
    id,
    nome: `Rinha ${id}`,
    status: 'PresencaAberta',
    publicacoesDiscord: publicationStatus ? [{ tipo: publicationType, status: publicationStatus }] : [],
    presencas: [],
    times: [],
    reservas: [],
  }
}

function pollingClient(send: (options: unknown) => Promise<{ id: string }>) {
  const permissions = new PermissionsBitField([
    PermissionFlagsBits.ViewChannel,
    PermissionFlagsBits.SendMessages,
    PermissionFlagsBits.EmbedLinks,
    PermissionFlagsBits.MentionEveryone,
  ])
  return {
    channels: {
      fetch: async () => ({ isTextBased: () => true, send, permissionsFor: () => permissions }),
    },
    user: { id: 'bot-user' },
  } as unknown as Client
}

function pollingClientWithChannelFailure() {
  return {
    channels: { fetch: async () => null },
    user: null,
  } as unknown as Client
}

function pollingClientWithPermissions(
  permissions: PermissionsBitField | null,
  send: (options: unknown) => Promise<{ id: string }>,
  options: { user?: boolean; permissionsFor?: boolean } = {},
) {
  const includeUser = options.user ?? true
  const includePermissionsFor = options.permissionsFor ?? true
  return {
    channels: {
      fetch: async () => ({
        isTextBased: () => true,
        send,
        ...(includePermissionsFor ? { permissionsFor: () => permissions } : {}),
      }),
    },
    user: includeUser ? { id: 'bot-user' } : null,
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
  it('publishes scheduled presence and creates the missing CTA through the existing protocol only once', async () => {
    const backendResponses: DraftMontagemDiscordOperationalDto[][] = [
      [{
        id: 'scheduled-draft',
        nome: 'Rinha semanal - 24/07/2026',
        status: 'PresencaAberta',
        horarioEncerramentoPresenca: '2026-07-24T23:00:00Z',
        discordPresenceMessageId: null,
        publicacoesDiscord: [{ tipo: 'Presenca', status: 'Pendente' }],
        presencas: [],
        times: [],
        reservas: [],
      }],
      [{
        id: 'scheduled-draft',
        nome: 'Rinha semanal - 24/07/2026',
        status: 'PresencaAberta',
        horarioEncerramentoPresenca: '2026-07-24T23:00:00Z',
        discordPresenceMessageId: 'message-1',
        publicacoesDiscord: [
          { tipo: 'Presenca', status: 'Publicada' },
          { tipo: 'ChamadaPresenca', status: 'Publicada' },
        ],
        presencas: [],
        times: [],
        reservas: [],
      }],
    ]
    env.DRAFT_NOTIFY_ROLE_ID = 'role-1'
    mock.method(rinhaApi, 'getDiscordConfiguration', async () => ({ guildId: 'guild', presenceChannelId: 'presence', draftChannelId: 'draft', botEnabled: true }))
    const list = mock.method(rinhaApi, 'listActiveDrafts', async () => backendResponses[list.mock.callCount() - 1] ?? backendResponses[1])
    let claimNumber = 0
    const claim = mock.method(rinhaApi, 'claimDiscordPublication', async () => ({
      adquirido: true,
      claimId: `claim-${++claimNumber}`,
      expiraEm: '2026-07-24T21:05:00Z',
      status: 'EmAndamento',
    }))
    const complete = mock.method(rinhaApi, 'registerDiscordPublication', async () => backendResponses[1]![0] as never)
    const send = mock.fn(async (_options: unknown) => ({ id: `message-${send.mock.callCount() + 1}` }))
    const client = pollingClient(send)

    assert.deepEqual(backendResponses[0]![0]!.publicacoesDiscord, [
      { tipo: 'Presenca', status: 'Pendente' },
    ])
    await runDraftPollingCycle(client)
    await runDraftPollingCycle(client)

    assert.equal(list.mock.callCount(), 2)
    assert.deepEqual(claim.mock.calls.map((call) => call.arguments[1]), ['Presenca', 'ChamadaPresenca'])
    assert.equal(send.mock.callCount(), 2)
    assert.ok('embeds' in (send.mock.calls[0]?.arguments[0] as object))
    assert.deepEqual(send.mock.calls[1]?.arguments[0], {
      content: buildDraftPresenceCta('scheduled-draft', 'role-1', env.FRONTEND_PUBLIC_URL),
      allowedMentions: { roles: ['role-1'] },
    })
    assert.deepEqual(complete.mock.calls.map((call) => [call.arguments[1]?.tipo, call.arguments[1]?.claimId]), [
      ['Presenca', 'claim-1'],
      ['ChamadaPresenca', 'claim-2'],
    ])
  })

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

  it('enforces the channel permission matrix before send', async () => {
    const allPermissions = [
      PermissionFlagsBits.ViewChannel,
      PermissionFlagsBits.SendMessages,
      PermissionFlagsBits.EmbedLinks,
      PermissionFlagsBits.MentionEveryone,
    ]
    const scenarios = [
      { name: 'presence CTA without view', type: 'Presenca', roleId: 'role-1', missing: PermissionFlagsBits.ViewChannel, errorCode: 'DiscordChannelViewPermissionError', blocked: true },
      { name: 'presence CTA without send', type: 'Presenca', roleId: 'role-1', missing: PermissionFlagsBits.SendMessages, errorCode: 'DiscordChannelSendPermissionError', blocked: true },
      { name: 'presence CTA without embed', type: 'Presenca', roleId: 'role-1', missing: PermissionFlagsBits.EmbedLinks, errorCode: 'DiscordChannelEmbedPermissionError', blocked: true },
      { name: 'presence embed without mention', type: 'Presenca', roleId: '', missing: PermissionFlagsBits.MentionEveryone, blocked: false },
      { name: 'presence without role and without mention', type: 'Presenca', roleId: '', missing: PermissionFlagsBits.MentionEveryone, blocked: false },
      { name: 'final teams without mention', type: 'TimesDefinidos', roleId: 'role-1', missing: PermissionFlagsBits.MentionEveryone, blocked: false },
      { name: 'final teams without view', type: 'TimesDefinidos', roleId: 'role-1', missing: PermissionFlagsBits.ViewChannel, errorCode: 'DiscordChannelViewPermissionError', blocked: true },
      { name: 'final teams without send', type: 'TimesDefinidos', roleId: 'role-1', missing: PermissionFlagsBits.SendMessages, errorCode: 'DiscordChannelSendPermissionError', blocked: true },
      { name: 'final teams without embed', type: 'TimesDefinidos', roleId: 'role-1', missing: PermissionFlagsBits.EmbedLinks, errorCode: 'DiscordChannelEmbedPermissionError', blocked: true },
    ] as const

    for (const scenario of scenarios) {
      mock.restoreAll()
      env.DRAFT_NOTIFY_ROLE_ID = scenario.roleId
      const draft = pollingDraft(scenario.name)
      draft.status = scenario.type === 'Presenca' ? 'PresencaAberta' : 'Finalizada'
      if (scenario.type === 'Presenca') draft.publicacoesDiscord?.push({ tipo: 'ChamadaPresenca', status: 'Publicada' })
      mock.method(rinhaApi, 'getDiscordConfiguration', async () => ({ guildId: 'guild', presenceChannelId: 'presence', draftChannelId: 'draft', botEnabled: true }))
      mock.method(rinhaApi, 'listActiveDrafts', async () => [draft])
      mock.method(rinhaApi, 'claimDiscordPublication', async () => ({ adquirido: true, claimId: 'claim-1', expiraEm: null, status: 'EmAndamento' }))
      mock.method(rinhaApi, 'registerDiscordPublication', async () => draft as never)
      const failure = mock.method(rinhaApi, 'registerDiscordPublicationFailure', async () => draft as never)
      const send = mock.fn(async () => ({ id: 'message-1' }))
      const granted = new PermissionsBitField(allPermissions.filter((permission) => permission !== scenario.missing))

      await runDraftPollingCycle(pollingClientWithPermissions(granted, send))

      assert.equal(send.mock.callCount(), scenario.blocked ? 0 : 1, scenario.name)
      assert.equal(failure.mock.callCount(), scenario.blocked ? 1 : 0, scenario.name)
      if (scenario.blocked) {
        assert.equal(failure.mock.calls[0]?.arguments[1]?.erroCodigo, scenario.errorCode, scenario.name)
      }
    }
  })

  it('fails safely with a specific error when channel permissions cannot be resolved', async () => {
    const scenarios = [
      { name: 'permissionsFor returns null', permissions: null, options: {} },
      { name: 'client user is unavailable', permissions: new PermissionsBitField(PermissionFlagsBits.ViewChannel), options: { user: false } },
      { name: 'permissionsFor is unavailable', permissions: new PermissionsBitField(PermissionFlagsBits.ViewChannel), options: { permissionsFor: false } },
    ] as const

    for (const scenario of scenarios) {
      mock.restoreAll()
      mockPollingApi([pollingDraft('draft-1')])
      mock.method(rinhaApi, 'claimDiscordPublication', async () => ({ adquirido: true, claimId: 'claim-1', expiraEm: null, status: 'EmAndamento' }))
      const failure = mock.method(rinhaApi, 'registerDiscordPublicationFailure', async () => pollingDraft('draft-1') as never)
      const send = mock.fn(async () => ({ id: 'message-1' }))
      const client = pollingClientWithPermissions(scenario.permissions, send, scenario.options)

      await assert.rejects(
        () => getSendableChannel(client, 'presence', t.channels.presence, { embed: true, mentionRole: false }),
        (error: Error) => error.name === 'DiscordChannelPermissionsUnknownError'
          && error.message === `${t.channels.presence} (presence) ${t.indeterminateChannelPermissions}`,
        scenario.name,
      )
      await runDraftPollingCycle(client)

      assert.equal(send.mock.callCount(), 0, scenario.name)
      assert.equal(failure.mock.calls[0]?.arguments[1]?.erroCodigo, 'DiscordChannelPermissionsUnknownError', scenario.name)
    }
  })

  it('sends the presence message and exact role CTA when all permissions are available', async () => {
    const draft = pollingDraft('draft-1')
    mockPollingApi([draft])
    env.DRAFT_NOTIFY_ROLE_ID = 'role-1'
    let claimNumber = 0
    mock.method(rinhaApi, 'claimDiscordPublication', async () => ({ adquirido: true, claimId: `claim-${++claimNumber}`, expiraEm: null, status: 'EmAndamento' }))
    const complete = mock.method(rinhaApi, 'registerDiscordPublication', async () => draft as never)
    const send = mock.fn(async (_options: unknown) => ({ id: `message-${send.mock.callCount() + 1}` }))

    await runDraftPollingCycle(pollingClient(send))

    assert.equal(send.mock.callCount(), 2)
    assert.ok('embeds' in (send.mock.calls[0]?.arguments[0] as object))
    assert.deepEqual(send.mock.calls[1]?.arguments[0], {
      content: buildDraftPresenceCta('draft-1', 'role-1', env.FRONTEND_PUBLIC_URL),
      allowedMentions: { roles: ['role-1'] },
    })
    assert.deepEqual(complete.mock.calls.map((call) => [call.arguments[1]?.tipo, call.arguments[1]?.claimId, call.arguments[1]?.messageId]), [
      ['Presenca', 'claim-1', 'message-1'],
      ['ChamadaPresenca', 'claim-2', 'message-2'],
    ])
  })

  it('completes the presence embed and fails only the CTA on a known pre-send error', async () => {
    const draft = pollingDraft('draft-1')
    mockPollingApi([draft])
    env.DRAFT_NOTIFY_ROLE_ID = 'role-1'
    let claimNumber = 0
    mock.method(rinhaApi, 'claimDiscordPublication', async () => ({ adquirido: true, claimId: `claim-${++claimNumber}`, expiraEm: null, status: 'EmAndamento' }))
    const complete = mock.method(rinhaApi, 'registerDiscordPublication', async () => draft as never)
    const failure = mock.method(rinhaApi, 'registerDiscordPublicationFailure', async () => draft as never)
    const permissions = new PermissionsBitField([
      PermissionFlagsBits.ViewChannel,
      PermissionFlagsBits.SendMessages,
      PermissionFlagsBits.EmbedLinks,
    ])
    const send = mock.fn(async () => ({ id: 'presence-message' }))

    await runDraftPollingCycle(pollingClientWithPermissions(permissions, send))

    assert.deepEqual(complete.mock.calls.map((call) => call.arguments[1]?.tipo), ['Presenca'])
    assert.equal(complete.mock.calls[0]?.arguments[1]?.messageId, 'presence-message')
    assert.equal(failure.mock.callCount(), 1)
    assert.equal(failure.mock.calls[0]?.arguments[1]?.tipo, 'ChamadaPresenca')
    assert.equal(failure.mock.calls[0]?.arguments[1]?.erroCodigo, 'DiscordChannelMentionPermissionError')
  })

  it('recovers a pending CTA without sending the main presence embed again', async () => {
    const draft = pollingDraft('draft-1', 'Pendente', 'ChamadaPresenca')
    draft.status = 'Finalizada'
    draft.publicacoesDiscord?.push({ tipo: 'Presenca', status: 'Publicada' })
    draft.publicacoesDiscord?.push({ tipo: 'TimesDefinidos', status: 'Publicada' })
    mockPollingApi([draft])
    env.DRAFT_NOTIFY_ROLE_ID = 'role-1'
    const claim = mock.method(rinhaApi, 'claimDiscordPublication', async () => ({ adquirido: true, claimId: 'claim-cta', expiraEm: null, status: 'EmAndamento' }))
    const complete = mock.method(rinhaApi, 'registerDiscordPublication', async () => draft as never)
    const send = mock.fn(async (_options: unknown) => ({ id: 'cta-message' }))

    await runDraftPollingCycle(pollingClient(send))

    assert.deepEqual(claim.mock.calls.map((call) => call.arguments[1]), ['ChamadaPresenca'])
    assert.equal(send.mock.callCount(), 1)
    assert.equal('content' in (send.mock.calls[0]?.arguments[0] as object), true)
    assert.equal('embeds' in (send.mock.calls[0]?.arguments[0] as object), false)
    assert.equal(complete.mock.calls[0]?.arguments[1]?.tipo, 'ChamadaPresenca')
  })

  it('keeps only the CTA claim in progress when its send result is unknown', async () => {
    const draft = pollingDraft('draft-1', 'Pendente', 'ChamadaPresenca')
    draft.status = 'Finalizada'
    draft.publicacoesDiscord?.push({ tipo: 'Presenca', status: 'Publicada' })
    draft.publicacoesDiscord?.push({ tipo: 'TimesDefinidos', status: 'Publicada' })
    mockPollingApi([draft])
    env.DRAFT_NOTIFY_ROLE_ID = 'role-1'
    mock.method(rinhaApi, 'claimDiscordPublication', async () => ({ adquirido: true, claimId: 'claim-cta', expiraEm: null, status: 'EmAndamento' }))
    const complete = mock.method(rinhaApi, 'registerDiscordPublication', async () => draft as never)
    const failure = mock.method(rinhaApi, 'registerDiscordPublicationFailure', async () => draft as never)

    await runDraftPollingCycle(pollingClient(async () => { throw new Error('unknown CTA send result') }))

    assert.equal(complete.mock.callCount(), 0)
    assert.equal(failure.mock.callCount(), 0)
  })

  it('does not claim a CTA when no notification role is configured', async () => {
    const draft = pollingDraft('draft-1', 'Pendente', 'ChamadaPresenca')
    draft.publicacoesDiscord?.push({ tipo: 'Presenca', status: 'Publicada' })
    mockPollingApi([draft])
    const claim = mock.method(rinhaApi, 'claimDiscordPublication', async () => ({ adquirido: true, claimId: 'claim-cta', expiraEm: null, status: 'EmAndamento' }))
    const send = mock.fn(async () => ({ id: 'cta-message' }))

    await runDraftPollingCycle(pollingClient(send))

    assert.equal(claim.mock.callCount(), 0)
    assert.equal(send.mock.callCount(), 0)
  })

  it('sends final teams once even when a notification role is configured', async () => {
    const draft = pollingDraft('draft-1')
    draft.status = 'Finalizada'
    mockPollingApi([draft])
    env.DRAFT_NOTIFY_ROLE_ID = 'role-1'
    mock.method(rinhaApi, 'claimDiscordPublication', async () => ({ adquirido: true, claimId: 'claim-1', expiraEm: null, status: 'EmAndamento' }))
    mock.method(rinhaApi, 'registerDiscordPublication', async () => draft as never)
    const send = mock.fn(async (_options: unknown) => ({ id: 'message-1' }))

    await runDraftPollingCycle(pollingClient(send))

    assert.equal(send.mock.callCount(), 1)
    assert.ok('embeds' in (send.mock.calls[0]?.arguments[0] as object))
  })
})

function draftCommandInteraction(commandName: string) {
  const replies: unknown[] = []
  const values: Record<string, string> = {
    [DraftOptionNames.Name]: 'Rinha segura',
    [DraftOptionNames.Day]: '31/12/2099',
    [DraftOptionNames.Time]: '21:30',
    [DraftOptionNames.DraftId]: 'draft-1',
    [DraftOptionNames.CaptainIds]: 'captain-1,captain-2',
    [DraftOptionNames.Mode]: 'Manual',
  }
  const interaction = {
    commandName,
    id: 'interaction-1',
    replied: false,
    guildId: 'guild',
    memberPermissions: new PermissionsBitField(PermissionFlagsBits.ManageGuild),
    member: { roles: { cache: new Map() } },
    options: { getString: (name: string) => values[name] ?? null },
    reply: async (payload: unknown) => { replies.push(payload) },
  } as unknown as ChatInputCommandInteraction

  return { interaction, replies }
}

function presenceButtonInteraction(action: 'confirm' | 'cancel') {
  const replies: unknown[] = []
  const interaction = {
    customId: `draft-presence:${action}:draft-1`,
    id: 'interaction-1',
    replied: false,
    user: { id: 'discord-user' },
    reply: async (payload: unknown) => { replies.push(payload) },
  } as unknown as ButtonInteraction

  return { interaction, replies }
}

describe('botEnabled mutation guard', () => {
  it('blocks every command mutation before its mutable API call', async () => {
    const scenarios = [
      ['draft-criar', 'createDraft'],
      ['draft-encerrar-presenca', 'closePresence'],
      ['draft-definir-capitaes', 'defineCaptains'],
      ['draft-definir-ordem-escolha', 'definePickOrder'],
    ] as const

    for (const [commandName, apiMethod] of scenarios) {
      mock.restoreAll()
      mock.method(rinhaApi, 'getDiscordConfiguration', async () => ({ guildId: 'guild', presenceChannelId: 'presence', draftChannelId: 'draft', botEnabled: false }))
      const mutation = mock.method(rinhaApi, apiMethod, async () => pollingDraft('draft-1') as never)
      const { interaction, replies } = draftCommandInteraction(commandName)

      await handleDraftCommand(interaction)

      assert.equal(mutation.mock.callCount(), 0, commandName)
      assert.deepEqual(replies, [{ content: t.integrationUnavailable, flags: MessageFlags.Ephemeral }], commandName)
    }
  })

  it('blocks presence button mutations before link lookup or mutable API calls', async () => {
    const scenarios = [
      ['confirm', 'confirmPresence'],
      ['cancel', 'cancelPresence'],
    ] as const

    for (const [action, apiMethod] of scenarios) {
      mock.restoreAll()
      mock.method(rinhaApi, 'getDiscordConfiguration', async () => ({ guildId: 'guild', presenceChannelId: 'presence', draftChannelId: 'draft', botEnabled: false }))
      const getDiscordLink = mock.method(rinhaApi, 'getDiscordLink', async () => ({ vinculado: true, roles: [] }))
      const mutation = mock.method(rinhaApi, apiMethod, async () => pollingDraft('draft-1') as never)
      const { interaction, replies } = presenceButtonInteraction(action)

      await handlePresenceButton(interaction)

      assert.equal(getDiscordLink.mock.callCount(), 0, action)
      assert.equal(mutation.mock.callCount(), 0, action)
      assert.deepEqual(replies, [{ content: t.integrationUnavailable, flags: MessageFlags.Ephemeral }], action)
    }
  })

  it('does not consult bot configuration for read-only commands and button', async () => {
    const getConfiguration = mock.method(rinhaApi, 'getDiscordConfiguration', async () => ({ guildId: 'guild', presenceChannelId: 'presence', draftChannelId: 'draft', botEnabled: false }))
    const listDrafts = mock.method(rinhaApi, 'listActiveDrafts', async () => [])

    for (const commandName of ['draft-listar', 'draft-status']) {
      const { interaction } = draftCommandInteraction(commandName)
      await handleDraftCommand(interaction)
    }
    const { interaction } = presenceButtonInteraction('confirm')
    ;(interaction as unknown as { customId: string }).customId = 'draft-presence:status:draft-1'
    await handlePresenceButton(interaction)

    assert.equal(getConfiguration.mock.callCount(), 0)
    assert.equal(listDrafts.mock.callCount(), 3)
  })

  it('blocks every mutation when loading configuration fails', async () => {
    const mutationScenarios = [
      ...[
        ['draft-criar', 'createDraft'],
        ['draft-encerrar-presenca', 'closePresence'],
        ['draft-definir-capitaes', 'defineCaptains'],
        ['draft-definir-ordem-escolha', 'definePickOrder'],
      ].map(([commandName, apiMethod]) => ({
        name: commandName,
        apiMethod,
        execute: async () => {
          const result = draftCommandInteraction(commandName)
          await handleDraftCommand(result.interaction)
          return result.replies
        },
      })),
      ...(['confirm', 'cancel'] as const).map((action) => ({
        name: `presence-${action}`,
        apiMethod: action === 'confirm' ? 'confirmPresence' : 'cancelPresence',
        execute: async () => {
          const result = presenceButtonInteraction(action)
          await handlePresenceButton(result.interaction)
          return result.replies
        },
      })),
    ]
    const errors = [
      { name: 'network', create: () => new TypeError('fetch failed'), expected: t.apiUnavailable },
      { name: 'internal token', create: () => new RinhaApiError('MV079', 'technical detail', 401), expected: t.internalTokenInvalid },
    ]

    for (const errorScenario of errors) {
      for (const mutationScenario of mutationScenarios) {
        mock.restoreAll()
        mock.method(rinhaApi, 'getDiscordConfiguration', async () => { throw errorScenario.create() })
        const mutation = mock.method(rinhaApi, mutationScenario.apiMethod as keyof typeof rinhaApi, async () => pollingDraft('draft-1') as never)
        const getDiscordLink = mock.method(rinhaApi, 'getDiscordLink', async () => ({ vinculado: true, roles: [] }))

        const replies = await mutationScenario.execute()

        assert.equal(mutation.mock.callCount(), 0, `${errorScenario.name}: ${mutationScenario.name}`)
        assert.equal(getDiscordLink.mock.callCount(), 0, `${errorScenario.name}: ${mutationScenario.name}`)
        assert.deepEqual(replies, [{ content: errorScenario.expected, flags: MessageFlags.Ephemeral }], `${errorScenario.name}: ${mutationScenario.name}`)
      }
    }
  })

  it('passes the real draft option and command arguments to mutable APIs', async () => {
    const scenarios = [
      { commandName: 'draft-encerrar-presenca', apiMethod: 'closePresence', expected: ['draft-1'] },
      { commandName: 'draft-definir-capitaes', apiMethod: 'defineCaptains', expected: ['draft-1', ['captain-1', 'captain-2']] },
      { commandName: 'draft-definir-ordem-escolha', apiMethod: 'definePickOrder', expected: ['draft-1', 'Manual', ['captain-1', 'captain-2']] },
    ] as const

    for (const scenario of scenarios) {
      mock.restoreAll()
      mock.method(rinhaApi, 'getDiscordConfiguration', async () => ({ guildId: 'guild', presenceChannelId: 'presence', draftChannelId: 'draft', botEnabled: true }))
      const mutation = mock.method(rinhaApi, scenario.apiMethod, async () => pollingDraft('draft-1') as never)
      const { interaction } = draftCommandInteraction(scenario.commandName)

      await handleDraftCommand(interaction)

      assert.deepEqual(mutation.mock.calls[0]?.arguments, scenario.expected, scenario.commandName)
    }
  })
})

describe('channel permission messages', () => {
  it('keeps distinct pt-BR and en-US guidance in parity', () => {
    const scenarios = [
      ['missingViewChannelPermission', 'não permite que o bot veja o canal. Libere Ver canal.', 'does not allow the bot to view the channel. Allow View Channel.'],
      ['missingSendMessagesPermission', 'não permite que o bot envie mensagens. Libere Enviar mensagens.', 'does not allow the bot to send messages. Allow Send Messages.'],
      ['missingEmbedLinksPermission', 'não permite que o bot incorpore links. Libere Incorporar links.', 'does not allow the bot to embed links. Allow Embed Links.'],
      ['missingMentionRolePermission', 'não permite que o bot mencione o cargo configurado. Libere Mencionar @everyone, @here e todos os cargos.', 'does not allow the bot to mention the configured role. Allow Mention @everyone, @here, and All Roles.'],
      ['indeterminateChannelPermissions', 'não foi possível determinar as permissões do bot no canal. Verifique o acesso do bot e tente novamente.', 'could not determine the bot permissions in the channel. Check the bot access and try again.'],
    ] as const

    for (const [key, portuguese, english] of scenarios) {
      assert.equal(ptBR[key], portuguese, key)
      assert.equal(enUS[key], english, key)
    }
  })
})

function mutableInteraction(options: {
  memberPermissions?: PermissionsBitField
  roleIds?: string[]
}) {
  const replies: unknown[] = []
  const interaction = {
    commandName: 'draft-encerrar-presenca',
    id: 'interaction-1',
    replied: false,
    memberPermissions: options.memberPermissions ?? new PermissionsBitField(),
    member: { roles: { cache: new Map((options.roleIds ?? []).map((roleId) => [roleId, {}])) } },
    options: {
      getString: (name: string) => name === DraftOptionNames.DraftId ? 'draft-1' : null,
    },
    reply: async (payload: unknown) => {
      replies.push(payload)
    },
  } as unknown as ChatInputCommandInteraction

  return { interaction, replies }
}

describe('handleDraftCommand authorization', () => {
  it('allows a member with ManageGuild to run a mutable command', async () => {
    mock.method(rinhaApi, 'getDiscordConfiguration', async () => ({ guildId: 'guild', presenceChannelId: 'presence', draftChannelId: 'draft', botEnabled: true }))
    const closePresence = mock.method(rinhaApi, 'closePresence', async () => ({} as never))
    const { interaction } = mutableInteraction({
      memberPermissions: new PermissionsBitField(PermissionFlagsBits.ManageGuild),
    })

    await handleDraftCommand(interaction)

    assert.equal(closePresence.mock.callCount(), 1)
    assert.deepEqual(closePresence.mock.calls[0]?.arguments, ['draft-1'])
  })

  it('allows a member with a configured draft administrator role', async () => {
    const previousRoleIds = env.DRAFT_ADMIN_ROLE_IDS
    env.DRAFT_ADMIN_ROLE_IDS = 'role-1, role-2'
    mock.method(rinhaApi, 'getDiscordConfiguration', async () => ({ guildId: 'guild', presenceChannelId: 'presence', draftChannelId: 'draft', botEnabled: true }))
    const closePresence = mock.method(rinhaApi, 'closePresence', async () => ({} as never))
    const { interaction } = mutableInteraction({ roleIds: ['role-2'] })

    try {
      await handleDraftCommand(interaction)
      assert.equal(closePresence.mock.callCount(), 1)
    } finally {
      env.DRAFT_ADMIN_ROLE_IDS = previousRoleIds
    }
  })

  it('denies a member without permission before calling the mutable API', async () => {
    const closePresence = mock.method(rinhaApi, 'closePresence', async () => ({} as never))
    const { interaction, replies } = mutableInteraction({ roleIds: ['other-role'] })

    await handleDraftCommand(interaction)

    assert.equal(closePresence.mock.callCount(), 0)
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
    const result = getDraftInteractionErrorMessage(new Error('404 draft not found'), 'closePresence')

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
