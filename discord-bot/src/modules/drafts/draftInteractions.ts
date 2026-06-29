import { MessageFlags, PermissionsBitField } from 'discord.js'
import type { ButtonInteraction, ChatInputCommandInteraction, Client, User } from 'discord.js'
import type { DraftMontagem } from '../../shared/api/types.js'
import { rinhaApi } from '../../shared/api/rinhaApi.js'
import { t } from '../../shared/messages/index.js'
import { finalTeamsEmbed, presenceButtons, presenceEmbed } from '../../discord/embeds/draftEmbeds.js'

export async function handleDraftCommand(interaction: ChatInputCommandInteraction) {
  if (interaction.commandName === 'draft-criar') {
    const configuration = await rinhaApi.getDiscordConfiguration()
    const channel = await getSendableChannel(interaction.client, configuration.presenceChannelId, 'Canal Lista de Presença')
    const presenceClosingTime = parsePresenceClosingTime(
      interaction.options.getString('dia', true),
      interaction.options.getString('horario', true),
    )
    if (!presenceClosingTime) {
      await interaction.reply({ content: 'Informe o encerramento como dia 29/06 e horário 21:30.', flags: MessageFlags.Ephemeral })
      return
    }

    const draft = await rinhaApi.createDraft({
      nome: interaction.options.getString('nome', true),
      horarioEncerramentoPresenca: presenceClosingTime,
      observacoes: interaction.options.getString('observacao'),
      discordGuildId: interaction.guildId,
    })
    const message = await channel.send({ embeds: [presenceEmbed(draft)], components: [presenceButtons(draft.id)] })
    await rinhaApi.registerDiscordPublication(draft.id, { discordGuildId: interaction.guildId, discordPresenceMessageId: message.id })
    await interaction.reply({ content: t.draftCreated, flags: MessageFlags.Ephemeral })
    return
  }

  if (interaction.commandName === 'draft-status') {
    const drafts = await rinhaApi.listActiveDrafts()
    await interaction.reply({ content: drafts.map((draft) => `${draft.nome}: ${draft.status} (${draft.presencas.filter((presence) => presence.status === 'Confirmada').length} confirmados)`).join('\n') || 'Nenhum draft ativo.', flags: MessageFlags.Ephemeral })
    return
  }

  if (interaction.commandName === 'draft-encerrar-presenca') {
    await rinhaApi.closePresence(interaction.options.getString('draft_id', true))
    await interaction.reply({ content: t.presenceClosed, flags: MessageFlags.Ephemeral })
    return
  }

  if (interaction.commandName === 'draft-definir-capitaes') {
    const ids = interaction.options.getString('capitaes_ids', true).split(',').map((item) => item.trim()).filter(Boolean)
    await rinhaApi.defineCaptains(interaction.options.getString('draft_id', true), ids)
    await interaction.reply({ content: 'Capitães definidos.', flags: MessageFlags.Ephemeral })
    return
  }

  if (interaction.commandName === 'draft-definir-ordem-escolha') {
    const ids = interaction.options.getString('capitaes_ids')?.split(',').map((item) => item.trim()).filter(Boolean) ?? []
    await rinhaApi.definePickOrder(interaction.options.getString('draft_id', true), interaction.options.getString('modo', true) as 'Manual' | 'Sorteado', ids)
    await interaction.reply({ content: 'Ordem de escolha definida.', flags: MessageFlags.Ephemeral })
  }
}

export async function handlePresenceButton(interaction: ButtonInteraction) {
  const [, action, draftId] = interaction.customId.split(':')
  if (!draftId) return

  if (action === 'confirm') {
    const linked = await rinhaApi.getDiscordLink(interaction.user.id)
    if (!linked.vinculado) {
      await interaction.reply({ content: t.accountNotLinked, flags: MessageFlags.Ephemeral })
      return
    }
    const draft = await rinhaApi.confirmPresence(draftId, interaction.user.id)
    await updatePresenceMessage(interaction, draft)
    await interaction.reply({ content: t.presenceConfirmed, flags: MessageFlags.Ephemeral })
    return
  }

  if (action === 'cancel') {
    const draft = await rinhaApi.cancelPresence(draftId, interaction.user.id)
    await updatePresenceMessage(interaction, draft)
    await interaction.reply({ content: t.presenceCancelled, flags: MessageFlags.Ephemeral })
    return
  }

  const drafts = await rinhaApi.listActiveDrafts()
  const draft = drafts.find((item) => item.id === draftId)
  await interaction.reply({ content: draft ? `${draft.nome}: ${draft.status} (${draft.presencas.filter((presence) => presence.status === 'Confirmada').length} confirmados)` : 'Draft não encontrado.', flags: MessageFlags.Ephemeral })
}

export function startDraftPolling(client: Client) {
  const publishedPresences = new Set<string>()
  const publishedFinalTeams = new Set<string>()
  setInterval(async () => {
    try {
      const configuration = await rinhaApi.getDiscordConfiguration()
      const drafts = await rinhaApi.listActiveDrafts()

      for (const draft of drafts.filter((item) => item.status === 'PresencaAberta' && !publishedPresences.has(item.id))) {
        const channel = await getSendableChannel(client, configuration.presenceChannelId, 'Canal Lista de Presença')
        const message = await channel.send({ embeds: [presenceEmbed(draft)], components: [presenceButtons(draft.id)] })
        await rinhaApi.registerDiscordPublication(draft.id, { discordGuildId: configuration.guildId, discordPresenceMessageId: message.id })
        publishedPresences.add(draft.id)
      }

      for (const draft of drafts.filter((item) => item.status === 'Finalizada' && !publishedFinalTeams.has(item.id))) {
        const channel = await getSendableChannel(client, configuration.draftChannelId, 'Canal Drafts/Times Definidos')
        await channel.send({ embeds: [finalTeamsEmbed(draft)] })
        publishedFinalTeams.add(draft.id)
      }
    } catch (error) {
      if (error instanceof DiscordChannelAccessError) {
        console.error(error.message)
        return
      }

      console.error(error)
    }
  }, 30000)
}

async function updatePresenceMessage(interaction: ButtonInteraction, draft: DraftMontagem) {
  const message = draft.discordPresenceMessageId ? await fetchPresenceMessage(interaction.client, draft) : interaction.message
  await message.edit({ embeds: [presenceEmbed(draft)], components: [presenceButtons(draft.id)] })
}

async function fetchPresenceMessage(client: Client, draft: DraftMontagem) {
  const configuration = await rinhaApi.getDiscordConfiguration()
  const channel = await getSendableChannel(client, configuration.presenceChannelId, 'Canal Lista de Presença')
  if (!channel.messages) throw new Error('Canal de presença não suporta busca de mensagens.')

  return channel.messages.fetch(draft.discordPresenceMessageId!)
}

export class DiscordChannelAccessError extends Error {
  constructor(public readonly userMessage: string) {
    super(userMessage)
  }
}

type SendableTextChannel = {
  send: (options: unknown) => Promise<{ id: string }>
  messages?: { fetch: (messageId: string) => Promise<{ edit: (options: unknown) => Promise<unknown> }> }
  permissionsFor?: (user: User) => PermissionsBitField | null
}

async function getSendableChannel(client: Client, channelId: string, label: string) {
  const channel = await client.channels.fetch(channelId)
  if (!channel?.isTextBased() || !('send' in channel)) {
    throw new DiscordChannelAccessError(`${label} (${channelId}) não foi encontrado ou não é um canal de texto acessível pelo bot.`)
  }

  const sendable = channel as SendableTextChannel
  const permissions = client.user && sendable.permissionsFor ? sendable.permissionsFor(client.user) : null
  const required = [PermissionsBitField.Flags.ViewChannel, PermissionsBitField.Flags.SendMessages, PermissionsBitField.Flags.EmbedLinks]
  if (permissions && !permissions.has(required)) {
    throw new DiscordChannelAccessError(`${label} (${channelId}) está sem permissão para o bot. Libere Ver canal, Enviar mensagens e Incorporar links.`)
  }

  return sendable
}

function parsePresenceClosingTime(dayInput: string, timeInput: string) {
  const dayMatch = dayInput.trim().match(/^(\d{1,2})[/-](\d{1,2})(?:[/-](\d{2}|\d{4}))?$/)
  const timeMatch = timeInput.trim().match(/^(\d{1,2}):(\d{2})$/)
  if (!dayMatch || !timeMatch) {
    return null
  }

  const day = Number(dayMatch[1])
  const month = Number(dayMatch[2])
  const hour = Number(timeMatch[1])
  const minute = Number(timeMatch[2])
  if (month < 1 || month > 12 || day < 1 || day > 31 || hour > 23 || minute > 59) {
    return null
  }

  const now = new Date()
  let year = dayMatch[3] ? Number(dayMatch[3]) : now.getUTCFullYear()
  year = year < 100 ? 2000 + year : year
  const localDateCheck = new Date(Date.UTC(year, month - 1, day, 0, 0, 0))
  if (localDateCheck.getUTCFullYear() !== year || localDateCheck.getUTCMonth() !== month - 1 || localDateCheck.getUTCDate() !== day) {
    return null
  }

  let date = new Date(Date.UTC(year, month - 1, day, hour + 3, minute, 0))
  if (!dayMatch[3] && date.getTime() < now.getTime()) {
    date = new Date(Date.UTC(year + 1, month - 1, day, hour + 3, minute, 0))
  }

  return date.toISOString()
}
