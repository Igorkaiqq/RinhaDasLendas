import { MessageFlags, PermissionsBitField } from 'discord.js'
import type { ButtonInteraction, ChatInputCommandInteraction, Client, User } from 'discord.js'
import type { DraftMontagem } from '../../shared/api/types.js'
import { rinhaApi } from '../../shared/api/rinhaApi.js'
import { logger } from '../../shared/logger.js'
import { t } from '../../shared/messages/index.js'
import { finalTeamsEmbed, formatDraftStatus, presenceButtons, presenceEmbed } from '../../discord/embeds/draftEmbeds.js'
import {
  DraftCommandNames,
  DraftMontagemStatus,
  DraftOptionNames,
  DraftPickOrderMode,
  DraftPresenceStatus,
  PresenceButtonAction,
} from '../../shared/constants/draftConstants/index.js'

export async function handleDraftCommand(interaction: ChatInputCommandInteraction) {
  if (interaction.commandName === DraftCommandNames.Create) {
    const configuration = await rinhaApi.getDiscordConfiguration()
    const channel = await getSendableChannel(interaction.client, configuration.presenceChannelId, t.channels.presence)
    const presenceClosingTime = parsePresenceClosingTime(
      interaction.options.getString(DraftOptionNames.Day, true),
      interaction.options.getString(DraftOptionNames.Time, true),
    )
    if (!presenceClosingTime) {
      await interaction.reply({ content: t.invalidClosingTime, flags: MessageFlags.Ephemeral })
      return
    }

    const draft = await rinhaApi.createDraft({
      nome: interaction.options.getString(DraftOptionNames.Name, true),
      horarioEncerramentoPresenca: presenceClosingTime,
      observacoes: interaction.options.getString(DraftOptionNames.Note),
      discordGuildId: interaction.guildId,
    })
    const message = await channel.send({ embeds: [presenceEmbed(draft)], components: [presenceButtons(draft.id)] })
    await rinhaApi.registerDiscordPublication(draft.id, { discordGuildId: interaction.guildId, discordPresenceMessageId: message.id })
    await interaction.reply({ content: t.draftCreated, flags: MessageFlags.Ephemeral })
    return
  }

  if (interaction.commandName === DraftCommandNames.Status || interaction.commandName === DraftCommandNames.List) {
    const drafts = await rinhaApi.listActiveDrafts()
    await interaction.reply({ content: formatDraftList(drafts), flags: MessageFlags.Ephemeral })
    return
  }

  if (interaction.commandName === DraftCommandNames.Cancel) {
    await rinhaApi.cancelDraft(
      interaction.options.getString(DraftOptionNames.DraftId, true),
      interaction.options.getString(DraftOptionNames.Reason),
    )
    await interaction.reply({ content: t.draftCancelled, flags: MessageFlags.Ephemeral })
    return
  }

  if (interaction.commandName === DraftCommandNames.ClosePresence) {
    await rinhaApi.closePresence(interaction.options.getString(DraftOptionNames.DraftId, true))
    await interaction.reply({ content: t.presenceClosed, flags: MessageFlags.Ephemeral })
    return
  }

  if (interaction.commandName === DraftCommandNames.DefineCaptains) {
    const ids = parseCommaSeparatedIds(interaction.options.getString(DraftOptionNames.CaptainIds, true))
    await rinhaApi.defineCaptains(interaction.options.getString(DraftOptionNames.DraftId, true), ids)
    await interaction.reply({ content: t.captainsDefined, flags: MessageFlags.Ephemeral })
    return
  }

  if (interaction.commandName === DraftCommandNames.DefinePickOrder) {
    const ids = parseCommaSeparatedIds(interaction.options.getString(DraftOptionNames.CaptainIds))
    await rinhaApi.definePickOrder(
      interaction.options.getString(DraftOptionNames.DraftId, true),
      interaction.options.getString(DraftOptionNames.Mode, true) as typeof DraftPickOrderMode.Manual | typeof DraftPickOrderMode.Drawn,
      ids,
    )
    await interaction.reply({ content: t.pickOrderDefined, flags: MessageFlags.Ephemeral })
  }
}

export async function handlePresenceButton(interaction: ButtonInteraction) {
  const [, action, draftId] = interaction.customId.split(':')
  if (!draftId) return

  if (action === PresenceButtonAction.Confirm) {
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

  if (action === PresenceButtonAction.Cancel) {
    const draft = await rinhaApi.cancelPresence(draftId, interaction.user.id)
    await updatePresenceMessage(interaction, draft)
    await interaction.reply({ content: t.presenceCancelled, flags: MessageFlags.Ephemeral })
    return
  }

  const drafts = await rinhaApi.listActiveDrafts()
  const draft = drafts.find((item) => item.id === draftId)
  await interaction.reply({ content: draft ? formatDraftLine(draft) : t.draftNotFound, flags: MessageFlags.Ephemeral })
}

export function startDraftPolling(client: Client) {
  const publishedPresences = new Set<string>()
  const publishedFinalTeams = new Set<string>()
  setInterval(async () => {
    try {
      const configuration = await rinhaApi.getDiscordConfiguration()
      const drafts = await rinhaApi.listActiveDrafts()

      for (const draft of drafts.filter((item) => item.discordPresenceMessageId)) {
        publishedPresences.add(draft.id)
      }

      for (const draft of drafts.filter((item) => item.status === DraftMontagemStatus.PresenceOpen && !item.discordPresenceMessageId && !publishedPresences.has(item.id))) {
        const channel = await getSendableChannel(client, configuration.presenceChannelId, t.channels.presence)
        const message = await channel.send({ embeds: [presenceEmbed(draft)], components: [presenceButtons(draft.id)] })
        await rinhaApi.registerDiscordPublication(draft.id, { discordGuildId: configuration.guildId, discordPresenceMessageId: message.id })
        publishedPresences.add(draft.id)
      }

      for (const draft of drafts.filter((item) => item.status === DraftMontagemStatus.Finalized && !publishedFinalTeams.has(item.id))) {
        const channel = await getSendableChannel(client, configuration.draftChannelId, t.channels.draft)
        await channel.send({ embeds: [finalTeamsEmbed(draft)] })
        publishedFinalTeams.add(draft.id)
      }
    } catch (error) {
      if (error instanceof DiscordChannelAccessError) {
        logger.error('Discord channel access failed', error)
        return
      }

      logger.error('Draft polling failed', error)
    }
  }, 30000)
}

async function updatePresenceMessage(interaction: ButtonInteraction, draft: DraftMontagem) {
  const message = draft.discordPresenceMessageId ? await fetchPresenceMessage(interaction.client, draft) : interaction.message
  await message.edit({ embeds: [presenceEmbed(draft)], components: [presenceButtons(draft.id)] })
}

async function fetchPresenceMessage(client: Client, draft: DraftMontagem) {
  const configuration = await rinhaApi.getDiscordConfiguration()
  const channel = await getSendableChannel(client, configuration.presenceChannelId, t.channels.presence)
  if (!channel.messages) throw new Error(t.presenceMessageFetchUnsupported)

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
    throw new DiscordChannelAccessError(`${label} (${channelId}) ${t.inaccessibleChannel}`)
  }

  const sendable = channel as SendableTextChannel
  const permissions = client.user && sendable.permissionsFor ? sendable.permissionsFor(client.user) : null
  const required = [PermissionsBitField.Flags.ViewChannel, PermissionsBitField.Flags.SendMessages, PermissionsBitField.Flags.EmbedLinks]
  if (permissions && !permissions.has(required)) {
    throw new DiscordChannelAccessError(`${label} (${channelId}) ${t.missingChannelPermissions}`)
  }

  return sendable
}

function parseCommaSeparatedIds(value: string | null) {
  return value?.split(',').map((item) => item.trim()).filter(Boolean) ?? []
}

function formatDraftList(drafts: DraftMontagem[]) {
  return drafts.map(formatDraftLine).join('\n') || t.noActiveDrafts
}

function formatDraftLine(draft: DraftMontagem) {
  return `${draft.nome}: ${formatDraftStatus(draft.status)} (${draft.presencas.filter((presence) => presence.status === DraftPresenceStatus.Confirmed).length} ${t.confirmedCount})`
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
