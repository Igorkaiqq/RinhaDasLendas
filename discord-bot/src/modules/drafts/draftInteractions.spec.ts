import { describe, it } from 'node:test'
import assert from 'node:assert/strict'

import { assertDiscordBotEnabled, getDraftInteractionErrorMessage, parsePresenceClosingTime, shouldPublishDiscordPublication, validatePresenceClosingTime } from './draftInteractions.js'
import { RinhaApiError } from '../../shared/api/rinhaApi.js'

describe('parsePresenceClosingTime', () => {
  it('interprets the informed time as Brasilia time', () => {
    const result = parsePresenceClosingTime('11/07/2026', '19:30')

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

describe('shouldPublishDiscordPublication', () => {
  it('allows pending presence republish even when a previous message id exists', () => {
    const result = shouldPublishDiscordPublication(
      {
        id: 'draft-1',
        nome: 'Rinha',
        status: 'PresencaAberta',
        tamanhoEquipe: 5,
        quantidadeTimes: 2,
        quantidadeReservas: 0,
        discordPresenceMessageId: 'old-message',
        publicacoesDiscord: [{ tipo: 'Presenca', status: 'Pendente', messageId: 'old-message' }],
        presencas: [],
        times: [],
        reservas: [],
      },
      'Presenca',
      new Set(['draft-1']),
    )

    assert.equal(result, true)
  })
})

describe('getDraftInteractionErrorMessage', () => {
  it('maps API authentication failures to the internal token guidance', () => {
    const result = getDraftInteractionErrorMessage(new Error('Unauthorized: token invalid'), 'create')

    assert.equal(result, 'A autenticação interna do bot falhou. Verifique RINHA_API_INTERNAL_TOKEN no backend e no bot.')
  })

  it('maps structured API message codes to specific messages', () => {
    const result = getDraftInteractionErrorMessage(new RinhaApiError('PresenceAlreadyClosed', 'Lista encerrada', 400), 'confirmPresence')

    assert.equal(result, 'Essa lista de presença já foi encerrada.')
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
