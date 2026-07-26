import { describe, it } from 'node:test'
import assert from 'node:assert/strict'

import type { DraftMontagem } from '../../shared/api/types.js'
import { buildDraftPresenceCta, cancellationEmbed, finalTeamsEmbed, formatDraftClosingTime, presenceEmbed } from './draftEmbeds.js'
import { enUS } from '../../shared/messages/en-US.js'
import { ptBR } from '../../shared/messages/pt-BR.js'

const operationalDraft: DraftMontagem = {
  id: 'draft-123',
  nome: 'Rinha de domingo',
  status: 'PresencaAberta',
  horarioEncerramentoPresenca: '2026-07-10T22:30:00.000Z',
  discordPresenceMessageId: 'message-1',
  publicacoesDiscord: [{ tipo: 'Presenca', status: 'Pendente' }],
  arquivado: false,
  versaoEstado: 1,
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

describe('cancellationEmbed', () => {
  it('publishes the localized cancellation without an archive reason', () => {
    const draft = {
      ...operationalDraft,
      status: 'Cancelada',
      arquivado: true,
      publicacoesDiscord: [{ tipo: 'Cancelamento', status: 'Pendente' }],
    } satisfies DraftMontagem

    const embed = cancellationEmbed(draft).toJSON()

    assert.equal(embed.title, ptBR.embeds.cancellationTitle)
    assert.equal(embed.description, ptBR.embeds.cancellationDescription.replace('{name}', draft.nome))
    assert.equal(JSON.stringify(embed).includes('motivo'), false)
    assert.equal(JSON.stringify(embed).includes('reason'), false)
  })

  it('keeps equivalent pt-BR and en-US cancellation embed and log messages', () => {
    assert.equal(ptBR.embeds.cancellationTitle, 'Draft cancelado')
    assert.equal(enUS.embeds.cancellationTitle, 'Draft cancelled')
    assert.equal(ptBR.embeds.cancellationDescription, 'O draft **{name}** foi cancelado e não continuará.')
    assert.equal(enUS.embeds.cancellationDescription, 'Draft **{name}** was cancelled and will not continue.')
    assert.equal(ptBR.logs.cancellationPublished, 'Cancelamento do draft publicado.')
    assert.equal(enUS.logs.cancellationPublished, 'Draft cancellation published.')
    assert.equal(ptBR.logs.stalePublicationSkipped, 'Publicação obsoleta ignorada após revalidar o draft.')
    assert.equal(enUS.logs.stalePublicationSkipped, 'Stale publication skipped after revalidating the draft.')
  })
})
