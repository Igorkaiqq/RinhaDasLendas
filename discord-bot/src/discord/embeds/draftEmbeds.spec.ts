import { describe, it } from 'node:test'
import assert from 'node:assert/strict'

import { buildDraftPresenceCta, formatDraftClosingTime } from './draftEmbeds.js'

describe('formatDraftClosingTime', () => {
  it('formats UTC closing time in Brasilia time for the bot message', () => {
    const result = formatDraftClosingTime('2026-07-10T22:30:00.000Z')

    assert.match(result, /19:30/)
    assert.match(result, /10\/07\/2026|7\/10\/2026/)
  })
})

describe('buildDraftPresenceCta', () => {
  it('mentions the configured role and includes the draft website URL', () => {
    const result = buildDraftPresenceCta('draft-123', 'role-456', 'https://rinha.example.com/')

    assert.match(result, /<@&role-456>/)
    assert.match(result, /https:\/\/rinha\.example\.com\/drafts\?draftId=draft-123/)
  })
})
