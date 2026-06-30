import { ActionRowBuilder, ButtonBuilder, ButtonStyle, EmbedBuilder } from 'discord.js'
import type { DraftMontagem } from '../../shared/api/types.js'
import { DraftPresenceStatus, PresenceButtonAction, PresenceButtonPrefix } from '../../shared/constants/draftConstants/index.js'
import { t } from '../../shared/messages/index.js'

export function presenceEmbed(draft: DraftMontagem) {
  const confirmed = draft.presencas.filter((presence) => presence.status === DraftPresenceStatus.Confirmed)
  const players = confirmed.map((presence, index) => `${index + 1}. ${presence.nomeExibicao}`).join('\n')
  return new EmbedBuilder()
    .setTitle(t.embeds.presenceTitle)
    .setDescription(`${draft.nome}\n${t.embeds.presenceDescription}`)
    .setColor(0x7c3aed)
    .addFields(
      { name: t.embeds.status, value: formatDraftStatus(draft.status), inline: true },
      { name: t.embeds.confirmedTotal, value: String(confirmed.length), inline: true },
      { name: t.embeds.closing, value: draft.horarioEncerramentoPresenca ? new Date(draft.horarioEncerramentoPresenca).toLocaleString(t.locale) : t.embeds.notProvided },
      { name: t.embeds.confirmed, value: players || t.embeds.noConfirmedYet },
    )
}

export function formatDraftStatus(status: string) {
  return t.statusLabels[status as keyof typeof t.statusLabels] ?? status
}

export function presenceButtons(draftId: string) {
  return new ActionRowBuilder<ButtonBuilder>().addComponents(
    new ButtonBuilder().setCustomId(`${PresenceButtonPrefix}:${PresenceButtonAction.Confirm}:${draftId}`).setLabel(t.embeds.confirmButton).setStyle(ButtonStyle.Success),
    new ButtonBuilder().setCustomId(`${PresenceButtonPrefix}:${PresenceButtonAction.Cancel}:${draftId}`).setLabel(t.embeds.cancelButton).setStyle(ButtonStyle.Danger),
    new ButtonBuilder().setCustomId(`${PresenceButtonPrefix}:${PresenceButtonAction.Status}:${draftId}`).setLabel(t.embeds.statusButton).setStyle(ButtonStyle.Secondary),
  )
}

export function finalTeamsEmbed(draft: DraftMontagem) {
  const embed = new EmbedBuilder().setTitle(t.embeds.finalTeamsTitle).setColor(0x2563eb)
  for (const team of draft.times) {
    const captain = team.jogadores.find((player) => player.capitao)?.nomeExibicao ?? t.embeds.captainUndefined
    const players = team.jogadores.map((player) => `- ${player.nomeExibicao}`).join('\n') || t.embeds.noPlayers
    embed.addFields({ name: team.nome, value: `${t.embeds.captain}: ${captain}\n\n${t.embeds.players}:\n${players}` })
  }
  if (draft.reservas.length > 0) {
    embed.addFields({ name: t.embeds.reserves, value: draft.reservas.map((player) => `- ${player.nomeExibicao}`).join('\n') })
  }
  return embed
}
