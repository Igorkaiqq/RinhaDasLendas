import { describe, it } from 'node:test'
import assert from 'node:assert/strict'

import type { DraftMontagem } from '../../shared/api/types.js'
import { buildDraftPresenceCta, finalTeamsEmbed, formatDraftClosingTime, presenceEmbed } from './draftEmbeds.js'

const operationalDraft: DraftMontagem = {
  id: 'draft-123',
  nome: 'Rinha de domingo',
  status: 'PresencaAberta',
  horarioEncerramentoPresenca: '2026-07-10T22:30:00.000Z',
  discordPresenceMessageId: 'message-1',
  publicacoesDiscord: [{ tipo: 'Presenca', status: 'Pendente' }],
  presencas: [{ nomeExibicao: 'Ahri', status: 'Confirmada' }],
  times: [{ nome: 'Time Azul', jogadores: [{ nomeExibicao: 'Lux', capitao: true }] }],
  reservas: [{ nomeExibicao: 'Garen' }],
}

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

describe('minimal operational draft contract', () => {
  it('contains every field consumed by the presence embed', () => {
    const embed = presenceEmbed(operationalDraft).toJSON()

    assert.equal(embed.description?.startsWith('Rinha de domingo'), true)
    assert.equal(embed.fields?.some((field) => field.value.includes('Ahri')), true)
  })

  it('contains every field consumed by the final teams embed', () => {
    const embed = finalTeamsEmbed(operationalDraft).toJSON()

    assert.equal(embed.fields?.some((field) => field.name === 'Time Azul' && field.value.includes('Lux')), true)
    assert.equal(embed.fields?.some((field) => field.value.includes('Garen')), true)
  })
})
