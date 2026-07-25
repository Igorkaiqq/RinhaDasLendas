import { describe, it } from 'node:test'
import assert from 'node:assert/strict'

import { buildWelcomeMessage } from './welcomeInteractions.js'

describe('buildWelcomeMessage', () => {
  it('includes the website CTA and registration tutorial', () => {
    const message = buildWelcomeMessage('https://rinha.example.com')

    assert.match(message, /https:\/\/rinha\.example\.com/)
    assert.match(message, /crie sua conta/i)
    assert.match(message, /complete seu perfil/i)
    assert.match(message, /vincule/i)
    assert.match(message, /confirme presença/i)
  })
})
