import { describe, it } from 'node:test'
import assert from 'node:assert/strict'

import { RinhaApiError, parseApiError, rinhaApi } from './rinhaApi.js'

describe('parseApiError', () => {
  it('extracts messageCode from standard API error JSON', () => {
    const result = parseApiError('{"messageCode":"PresenceAlreadyClosed","message":"Lista encerrada","errors":[]}', 'Bad Request', 400)

    assert.ok(result instanceof RinhaApiError)
    assert.equal(result.messageCode, 'PresenceAlreadyClosed')
    assert.equal(result.message, 'Lista encerrada')
    assert.equal(result.status, 400)
  })

  it('falls back to response text when body is not JSON', () => {
    const result = parseApiError('plain failure', 'Internal Server Error', 500)

    assert.equal(result.message, 'plain failure')
    assert.equal(result.messageCode, undefined)
    assert.equal(result.status, 500)
  })
})

describe('rinhaApi Discord publication failure', () => {
  it('posts failure state to the draft publication failure endpoint', async () => {
    const originalFetch = globalThis.fetch
    const calls: Array<{ url: string; init?: RequestInit }> = []
    globalThis.fetch = (async (url: URL | RequestInfo, init?: RequestInit) => {
      calls.push({ url: String(url), init })
      return new Response('{}', { status: 200, headers: { 'content-type': 'application/json' } })
    }) as typeof fetch

    try {
      await rinhaApi.registerDiscordPublicationFailure('draft-1', { tipo: 'Presenca', discordGuildId: 'guild', discordChannelId: 'channel', erroCodigo: 'MissingPermissions' })
    } finally {
      globalThis.fetch = originalFetch
    }

    assert.equal(calls[0].url.endsWith('/api/v1/draft-montagens/draft-1/discord/publicacao/falha'), true)
    assert.equal(calls[0].init?.method, 'POST')
    assert.equal(calls[0].init?.body, JSON.stringify({ tipo: 'Presenca', discordGuildId: 'guild', discordChannelId: 'channel', erroCodigo: 'MissingPermissions' }))
  })
})
