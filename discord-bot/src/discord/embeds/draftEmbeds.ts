import { ActionRowBuilder, ButtonBuilder, ButtonStyle, EmbedBuilder } from 'discord.js'
import type { DraftMontagem } from '../../shared/api/types.js'

export function presenceEmbed(draft: DraftMontagem) {
  const confirmed = draft.presencas.filter((presence) => presence.status === 'Confirmada')
  const players = confirmed.map((presence, index) => `${index + 1}. ${presence.nomeExibicao}`).join('\n')
  return new EmbedBuilder()
    .setTitle('📋 Lista de Presença Aberta')
    .setDescription(`${draft.nome}\n/join, /leave ou clique nos botões abaixo.`)
    .setColor(0x7c3aed)
    .addFields(
      { name: 'Status', value: draft.status, inline: true },
      { name: 'size', value: String(confirmed.length), inline: true },
      { name: 'Encerramento', value: draft.horarioEncerramentoPresenca ? new Date(draft.horarioEncerramentoPresenca).toLocaleString('pt-BR') : 'Não informado' },
      { name: 'Confirmados', value: players || 'Nenhum confirmado ainda.' },
    )
}

export function presenceButtons(draftId: string) {
  return new ActionRowBuilder<ButtonBuilder>().addComponents(
    new ButtonBuilder().setCustomId(`presence:confirm:${draftId}`).setLabel('Confirmar Presença').setStyle(ButtonStyle.Success),
    new ButtonBuilder().setCustomId(`presence:cancel:${draftId}`).setLabel('Cancelar Presença').setStyle(ButtonStyle.Danger),
    new ButtonBuilder().setCustomId(`presence:status:${draftId}`).setLabel('Ver Status').setStyle(ButtonStyle.Secondary),
  )
}

export function finalTeamsEmbed(draft: DraftMontagem) {
  const embed = new EmbedBuilder().setTitle('🏆 Times Definidos').setColor(0x2563eb)
  for (const team of draft.times) {
    const captain = team.jogadores.find((player) => player.capitao)?.nomeExibicao ?? 'Não definido'
    const players = team.jogadores.map((player) => `- ${player.nomeExibicao}`).join('\n') || 'Sem jogadores'
    embed.addFields({ name: team.nome, value: `Capitão: ${captain}\n\nJogadores:\n${players}` })
  }
  if (draft.reservas.length > 0) {
    embed.addFields({ name: 'Reservas', value: draft.reservas.map((player) => `- ${player.nomeExibicao}`).join('\n') })
  }
  return embed
}
