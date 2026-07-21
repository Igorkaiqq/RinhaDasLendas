import { MessageFlags, PermissionFlagsBits, PermissionsBitField } from 'discord.js'
import type { ButtonInteraction, ChatInputCommandInteraction, Client, User } from 'discord.js'
import type { DraftMontagem } from '../../shared/api/types.js'
import type { DiscordConfiguration } from '../../shared/api/types.js'
import { env } from '../../config/env.js'
import { RinhaApiError, rinhaApi } from '../../shared/api/rinhaApi.js'
import { logger } from '../../shared/logger.js'
import { t } from '../../shared/messages/index.js'
import { buildDraftPresenceCta, finalTeamsEmbed, formatDraftStatus, presenceButtons, presenceEmbed } from '../../discord/embeds/draftEmbeds.js'
import {
  DraftCommandNames,
  DraftMontagemStatus,
  DraftOptionNames,
  DraftPickOrderMode,
  DraftPresenceStatus,
  PresenceButtonAction,
} from '../../shared/constants/draftConstants/index.js'

export async function handleDraftCommand(interaction: ChatInputCommandInteraction) {
  try {
    if (isMutableDraftCommand(interaction.commandName) && !isDraftAdministrator(interaction, parseCommaSeparatedIds(env.DRAFT_ADMIN_ROLE_IDS))) {
      await interaction.reply({ content: t.draftAdministrationDenied, flags: MessageFlags.Ephemeral })
      return
    }

    if (interaction.commandName === DraftCommandNames.Create) {
    const draftName = interaction.options.getString(DraftOptionNames.Name, true)
    if (!draftName.trim()) {
      await interaction.reply({ content: t.invalidDraftName, flags: MessageFlags.Ephemeral })
      return
    }

    const configuration = await rinhaApi.getDiscordConfiguration()
    assertDiscordBotEnabled(configuration)
    const channel = await getSendableChannel(interaction.client, configuration.presenceChannelId, t.channels.presence)
    const presenceClosingTimeValidation = validatePresenceClosingTime(
      interaction.options.getString(DraftOptionNames.Day, true),
      interaction.options.getString(DraftOptionNames.Time, true),
    )
    if (!presenceClosingTimeValidation.ok) {
      const message = presenceClosingTimeValidation.reason === 'past'
        ? t.invalidPastClosingTime
        : presenceClosingTimeValidation.reason === 'invalid-date'
          ? t.invalidCalendarDate
          : t.invalidClosingTime
      await interaction.reply({ content: message, flags: MessageFlags.Ephemeral })
      return
    }

    const draft = await rinhaApi.createDraft({
      nome: draftName,
      horarioEncerramentoPresenca: presenceClosingTimeValidation.value,
      observacoes: interaction.options.getString(DraftOptionNames.Note),
      discordGuildId: interaction.guildId,
    })
    const message = await channel.send({ embeds: [presenceEmbed(draft)], components: [presenceButtons(draft.id)] })
    const ctaResult = await sendDraftPresenceCta(channel, draft.id)
    await rinhaApi.registerDiscordPublication(draft.id, { discordGuildId: interaction.guildId, discordPresenceMessageId: message.id })
    await interaction.reply({ content: getDraftCreatedMessage(ctaResult), flags: MessageFlags.Ephemeral })
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
  } catch (error) {
    logger.error('Discord draft command failed', error, { commandName: interaction.commandName, interactionId: interaction.id })
    if (!interaction.replied) {
      await interaction.reply({ content: getDraftInteractionErrorMessage(error, getDraftCommandErrorContext(interaction.commandName)), flags: MessageFlags.Ephemeral })
    }
  }
}

export function isDraftAdministrator(interaction: ChatInputCommandInteraction, configuredRoleIds: readonly string[]) {
  if (interaction.memberPermissions?.has(PermissionFlagsBits.ManageGuild)) return true
  if (!interaction.member || !('roles' in interaction.member)) return false

  const memberRoles = interaction.member.roles
  const roleIds = Array.isArray(memberRoles) ? memberRoles : Array.from(memberRoles.cache.keys())
  return configuredRoleIds.some((roleId) => roleIds.includes(roleId))
}

function isMutableDraftCommand(commandName: string) {
  return commandName === DraftCommandNames.Create
    || commandName === DraftCommandNames.Cancel
    || commandName === DraftCommandNames.ClosePresence
    || commandName === DraftCommandNames.DefineCaptains
    || commandName === DraftCommandNames.DefinePickOrder
}

export async function handlePresenceButton(interaction: ButtonInteraction) {
  const [, action, draftId] = interaction.customId.split(':')
  if (!draftId) return

  try {
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
  await interaction.reply({ content: draft ? formatDraftLine(draft) : t.draftNotFoundMaybeFinished, flags: MessageFlags.Ephemeral })
  } catch (error) {
    logger.error('Discord presence button failed', error, { action, draftId, interactionId: interaction.id })
    if (!interaction.replied) {
      await interaction.reply({ content: getDraftInteractionErrorMessage(error, getPresenceButtonErrorContext(action)), flags: MessageFlags.Ephemeral })
    }
  }
}

export function startDraftPolling(client: Client) {
  const publishedPresences = new Set<string>()
  const publishedFinalTeams = new Set<string>()
  setInterval(async () => {
    try {
      const configuration = await rinhaApi.getDiscordConfiguration()
      assertDiscordBotEnabled(configuration)
      const drafts = await rinhaApi.listActiveDrafts()

      for (const draft of drafts.filter((item) => hasPublishedDiscordPublication(item, 'Presenca') || item.discordPresenceMessageId)) {
        publishedPresences.add(draft.id)
      }

      for (const draft of drafts.filter((item) => hasPublishedDiscordPublication(item, 'TimesDefinidos'))) {
        publishedFinalTeams.add(draft.id)
      }

      for (const draft of drafts.filter((item) => item.status === DraftMontagemStatus.PresenceOpen && shouldPublishDiscordPublication(item, 'Presenca', publishedPresences))) {
        try {
          const channel = await getSendableChannel(client, configuration.presenceChannelId, t.channels.presence)
          const message = await channel.send({ embeds: [presenceEmbed(draft)], components: [presenceButtons(draft.id)] })
          await sendDraftPresenceCta(channel, draft.id)
          await rinhaApi.registerDiscordPublication(draft.id, { discordGuildId: configuration.guildId, discordPresenceMessageId: message.id, tipo: 'Presenca', discordChannelId: configuration.presenceChannelId })
          publishedPresences.add(draft.id)
          logger.info(t.logs.siteDraftPublished, { draftId: draft.id })
        } catch (error) {
          await rinhaApi.registerDiscordPublicationFailure(draft.id, { tipo: 'Presenca', discordGuildId: configuration.guildId, discordChannelId: configuration.presenceChannelId, erroCodigo: getPublicationErrorCode(error) })
          throw error
        }
      }

      for (const draft of drafts.filter((item) => item.status === DraftMontagemStatus.Finalized && shouldPublishDiscordPublication(item, 'TimesDefinidos', publishedFinalTeams))) {
        try {
          const channel = await getSendableChannel(client, configuration.draftChannelId, t.channels.draft)
          const message = await channel.send({ embeds: [finalTeamsEmbed(draft)] })
          await rinhaApi.registerDiscordPublication(draft.id, { discordGuildId: configuration.guildId, discordPresenceMessageId: message.id, tipo: 'TimesDefinidos', discordChannelId: configuration.draftChannelId })
          publishedFinalTeams.add(draft.id)
          logger.info(t.logs.finalTeamsPublished, { draftId: draft.id })
        } catch (error) {
          await rinhaApi.registerDiscordPublicationFailure(draft.id, { tipo: 'TimesDefinidos', discordGuildId: configuration.guildId, discordChannelId: configuration.draftChannelId, erroCodigo: getPublicationErrorCode(error) })
          throw error
        }
      }
    } catch (error) {
      if (error instanceof DiscordChannelAccessError) {
        logger.error(t.logs.finalTeamsPublishFailed, error)
        return
      }

      logger.error('Draft polling failed', error)
    }
  }, 30000)
}

function hasPublishedDiscordPublication(draft: DraftMontagem, tipo: string) {
  return draft.publicacoesDiscord?.some((publication) => publication.tipo === tipo && publication.status === 'Publicada' && publication.messageId) ?? false
}

export function shouldPublishDiscordPublication(draft: DraftMontagem, tipo: string, locallyPublishedDraftIds: ReadonlySet<string>) {
  const publication = draft.publicacoesDiscord?.find((item) => item.tipo === tipo)
  if (publication?.status === 'Pendente') {
    return true
  }

  if (publication?.status === 'Publicada' && publication.messageId) {
    return false
  }

  if (locallyPublishedDraftIds.has(draft.id)) {
    return false
  }

  if (tipo === 'Presenca' && draft.discordPresenceMessageId) {
    return false
  }

  return true
}

function getPublicationErrorCode(error: unknown) {
  if (error instanceof DiscordChannelAccessError) {
    return 'DiscordChannelAccessError'
  }

  if (error instanceof Error) {
    return error.name || 'DiscordPublicationFailed'
  }

  return 'DiscordPublicationFailed'
}

async function updatePresenceMessage(interaction: ButtonInteraction, draft: DraftMontagem) {
  const message = draft.discordPresenceMessageId ? await fetchPresenceMessage(interaction.client, draft) : interaction.message
  await message.edit({ embeds: [presenceEmbed(draft)], components: [presenceButtons(draft.id)] })
}

async function fetchPresenceMessage(client: Client, draft: DraftMontagem) {
  const configuration = await rinhaApi.getDiscordConfiguration()
  assertDiscordBotEnabled(configuration)
  const channel = await getSendableChannel(client, configuration.presenceChannelId, t.channels.presence)
  if (!channel.messages) throw new Error(t.presenceMessageFetchUnsupported)

  return channel.messages.fetch(draft.discordPresenceMessageId!)
}

export class DiscordChannelAccessError extends Error {
  constructor(public readonly userMessage: string) {
    super(userMessage)
  }
}

export function assertDiscordBotEnabled(configuration: DiscordConfiguration) {
  if (!configuration.botEnabled) {
    throw new Error(t.integrationUnavailable)
  }
}

type SendableTextChannel = {
  send: (options: unknown) => Promise<{ id: string }>
  messages?: { fetch: (messageId: string) => Promise<{ edit: (options: unknown) => Promise<unknown> }> }
  permissionsFor?: (user: User) => PermissionsBitField | null
}

async function sendDraftPresenceCta(channel: SendableTextChannel, draftId: string): Promise<'sent' | 'not-configured' | 'failed'> {
  if (!env.DRAFT_NOTIFY_ROLE_ID) {
    logger.info(t.logs.ctaRoleNotConfigured, { draftId })
    return 'not-configured'
  }

  try {
    await channel.send({ content: buildDraftPresenceCta(draftId, env.DRAFT_NOTIFY_ROLE_ID, env.FRONTEND_PUBLIC_URL), allowedMentions: { roles: [env.DRAFT_NOTIFY_ROLE_ID] } })
    return 'sent'
  } catch (error) {
    logger.error(t.draftCreatedCtaFailed, error, { draftId })
    return 'failed'
  }
}

async function getSendableChannel(client: Client, channelId: string, label: string) {
  const channel = await client.channels.fetch(channelId)
  if (!channel?.isTextBased() || !('send' in channel)) {
    throw new DiscordChannelAccessError(`${label} (${channelId}) ${t.inaccessibleChannel}`)
  }

  const sendable = channel as SendableTextChannel
  const permissions = client.user && sendable.permissionsFor ? sendable.permissionsFor(client.user) : null
  const required = [PermissionsBitField.Flags.ViewChannel, PermissionsBitField.Flags.SendMessages, PermissionsBitField.Flags.EmbedLinks, PermissionsBitField.Flags.MentionEveryone]
  if (permissions && !permissions.has(required)) {
    throw new DiscordChannelAccessError(`${label} (${channelId}) ${t.missingChannelPermissions}`)
  }

  return sendable
}

function parseCommaSeparatedIds(value: string | null) {
  return value?.split(',').map((item) => item.trim()).filter(Boolean) ?? []
}

function formatDraftList(drafts: DraftMontagem[]) {
  const lines = drafts.map(formatDraftLine)
  return lines.length > 0 ? `${t.activeDraftsFound}\n${lines.join('\n')}` : t.noActiveDrafts
}

function formatDraftLine(draft: DraftMontagem) {
  return `${draft.nome}: ${formatDraftStatus(draft.status)} (${draft.presencas.filter((presence) => presence.status === DraftPresenceStatus.Confirmed).length} ${t.confirmedCount})`
}

export function parsePresenceClosingTime(dayInput: string, timeInput: string, now = new Date()) {
  const validation = validatePresenceClosingTime(dayInput, timeInput, now)
  return validation.ok ? validation.value : null
}

type PresenceClosingTimeValidation =
  | { ok: true; value: string }
  | { ok: false; reason: 'format' | 'invalid-date' | 'past' }

export function validatePresenceClosingTime(dayInput: string, timeInput: string, now = new Date()): PresenceClosingTimeValidation {
  const dayMatch = dayInput.trim().match(/^(\d{1,2})[/-](\d{1,2})(?:[/-](\d{2}|\d{4}))?$/)
  const timeMatch = timeInput.trim().match(/^(\d{1,2}):(\d{2})$/)
  if (!dayMatch || !timeMatch) {
    return { ok: false, reason: 'format' }
  }

  const day = Number(dayMatch[1])
  const month = Number(dayMatch[2])
  const hour = Number(timeMatch[1])
  const minute = Number(timeMatch[2])
  if (month < 1 || month > 12 || day < 1 || day > 31 || hour > 23 || minute > 59) {
    return { ok: false, reason: 'format' }
  }

  let year = dayMatch[3] ? Number(dayMatch[3]) : now.getUTCFullYear()
  year = year < 100 ? 2000 + year : year
  const localDateCheck = new Date(Date.UTC(year, month - 1, day, 0, 0, 0))
  if (localDateCheck.getUTCFullYear() !== year || localDateCheck.getUTCMonth() !== month - 1 || localDateCheck.getUTCDate() !== day) {
    return { ok: false, reason: 'invalid-date' }
  }

  let date = new Date(Date.UTC(year, month - 1, day, hour + 3, minute, 0))
  if (!dayMatch[3] && date.getTime() < now.getTime()) {
    date = new Date(Date.UTC(year + 1, month - 1, day, hour + 3, minute, 0))
  }

  if (date.getTime() < now.getTime()) {
    return { ok: false, reason: 'past' }
  }

  return { ok: true, value: date.toISOString() }
}

export type DraftInteractionErrorContext =
  | 'create'
  | 'list'
  | 'cancel'
  | 'closePresence'
  | 'defineCaptains'
  | 'definePickOrder'
  | 'confirmPresence'
  | 'cancelPresence'
  | 'status'

export function getDraftInteractionErrorMessage(error: unknown, context: DraftInteractionErrorContext) {
  if (error instanceof RinhaApiError && error.messageCode) {
    const byCode = getDraftInteractionErrorMessageByCode(error.messageCode, context)
    if (byCode) return byCode
  }

  const message = normalizeErrorMessage(error)

  if (containsAny(message, 'unauthorized', '401', 'forbidden', 'token')) return t.internalTokenInvalid
  if (containsAny(message, 'fetch failed', 'econnrefused', 'enotfound', 'networkerror')) return context === 'list' ? t.draftErrors.listFailed : t.apiUnavailable
  if (containsAny(message, 'timeout', 'timed out', 'aborted')) return t.apiTimeout
  if (containsAny(message, 'rate limit', 'ratelimit', 'too many requests', '429')) return t.discordRateLimited
  if (containsAny(message, 'nao configurado', 'not configured', 'presencechannelid', 'canal de lista de presenca ainda')) return t.draftErrors.channelNotConfigured
  if (containsAny(message, 'nao foi encontrado', 'not found or is not a text channel', 'unknown channel', 'inaccessible')) return t.draftErrors.channelInaccessible
  if (containsAny(message, 'sem permissao', 'missing bot permissions', 'missing permissions', 'permission')) return t.draftErrors.channelPermissionDenied
  if (containsAny(message, '404', 'not found', 'nao encontrado', 'nao encontrei')) return context === 'status' ? t.draftNotFoundMaybeFinished : t.draftNotFound

  if (context === 'confirmPresence') {
    if (containsAny(message, 'perfil jogador incompleto', 'perfil incompleto', 'player profile incomplete')) return t.playerProfileIncomplete
    if (containsAny(message, 'jogador inativo', 'perfil inativo', 'inactive')) return t.playerInactive
    if (containsAny(message, 'sem jogador', 'without player', 'linked account without player')) return t.linkedAccountWithoutPlayer
    if (containsAny(message, 'lista lotada', 'limite', 'full')) return t.presenceListFull
    if (containsAny(message, 'ja confirmado', 'already confirmed')) return t.draftErrors.presenceAlreadyConfirmed
    if (containsAny(message, 'presenca encerrada', 'presence closed', 'lista encerrada')) return t.draftErrors.presenceAlreadyClosed
  }

  if (context === 'cancelPresence') {
    if (containsAny(message, 'nao estava confirmado', 'nao confirmado', 'not confirmed')) return t.draftErrors.presenceNotConfirmed
    if (containsAny(message, 'presenca encerrada', 'presence closed', 'lista encerrada')) return t.draftErrors.presenceClosedCannotCancel
  }

  if (context === 'closePresence') {
    if (containsAny(message, 'menos de 10', 'fewer than 10', 'less than 10')) return t.draftErrors.closePresenceLessThanTen
    if (containsAny(message, 'ja encerrada', 'already closed', 'presenca encerrada')) return t.draftErrors.presenceAlreadyClosed
  }

  if (context === 'cancel') {
    if (containsAny(message, 'ja encerrado', 'ja finalizado', 'already closed', 'already finalized', 'cancelado')) return t.draftErrors.draftAlreadyClosed
  }

  if (context === 'defineCaptains') {
    if (containsAny(message, 'duplicado', 'repetido', 'duplicate')) return t.draftErrors.duplicateCaptains
    if (containsAny(message, 'quantidade', 'count', 'number of captains')) return t.draftErrors.captainCountMismatch
    if (containsAny(message, 'nao confirmado', 'not confirmed')) return t.draftErrors.captainNotConfirmed
    if (containsAny(message, 'presenca aberta', 'presence is still open')) return t.draftErrors.presenceStillOpen
    if (containsAny(message, 'formato', 'format')) return t.draftErrors.captainFormat
    if (containsAny(message, 'ids invalidos', 'invalid ids', 'invalidos')) return t.draftErrors.defineCaptainsFailed
  }

  if (context === 'definePickOrder') {
    if (containsAny(message, 'modo invalido', 'invalid mode')) return t.draftErrors.invalidPickOrderMode
    if (containsAny(message, 'manual sem capitaes', 'manual without ids', 'requires ids')) return t.draftErrors.manualPickOrderRequiresIds
    if (containsAny(message, 'sem capitaes', 'captains not defined', 'missing captains')) return t.draftErrors.missingCaptains
    if (containsAny(message, 'ordem manual', 'manual order')) return t.draftErrors.invalidManualPickOrder
  }

  return contextGenericMessage(context)
}

function getDraftInteractionErrorMessageByCode(messageCode: string, context: DraftInteractionErrorContext) {
  const messages: Record<string, string> = {
    BotInternalTokenInvalid: t.internalTokenInvalid,
    DiscordAccountNotLinked: t.accountNotLinked,
    PlayerProfileNotFound: t.playerProfileIncomplete,
    InactivePlayerCannotJoinQueue: t.playerInactive,
    PresenceAlreadyClosed: context === 'cancelPresence' ? t.draftErrors.presenceClosedCannotCancel : t.draftErrors.presenceAlreadyClosed,
    PresenceNotFound: context === 'cancelPresence' ? t.draftErrors.presenceNotConfirmed : t.draftNotFound,
    PlayerAlreadyInQueue: t.draftErrors.presenceAlreadyConfirmed,
    DraftMontagemInsufficientPresencePlayers: t.draftErrors.closePresenceLessThanTen,
    DraftMontagemPresenceMustBeClosed: t.draftErrors.presenceStillOpen,
    DraftMontagemCaptainsRequired: context === 'definePickOrder' ? t.draftErrors.missingCaptains : t.draftErrors.captainCountMismatch,
    DraftMontagemCaptainsMustBePlayers: t.draftErrors.captainNotConfirmed,
    DraftMontagemPickOrderInvalid: t.draftErrors.invalidManualPickOrder,
    DraftClosed: context === 'cancel' ? t.draftErrors.draftAlreadyClosed : contextGenericMessage(context),
    DraftMontagemNotFound: context === 'status' ? t.draftNotFoundMaybeFinished : t.draftNotFound,
  }

  return messages[messageCode]
}

function getDraftCreatedMessage(ctaResult: 'sent' | 'not-configured' | 'failed') {
  if (ctaResult === 'sent') return t.draftCreatedWithCta
  if (ctaResult === 'not-configured') return t.draftCreatedCtaNotConfigured
  return t.draftCreatedCtaFailed
}

function getDraftCommandErrorContext(commandName: string): DraftInteractionErrorContext {
  if (commandName === DraftCommandNames.Create) return 'create'
  if (commandName === DraftCommandNames.Cancel) return 'cancel'
  if (commandName === DraftCommandNames.ClosePresence) return 'closePresence'
  if (commandName === DraftCommandNames.DefineCaptains) return 'defineCaptains'
  if (commandName === DraftCommandNames.DefinePickOrder) return 'definePickOrder'
  if (commandName === DraftCommandNames.Status) return 'status'
  return 'list'
}

function getPresenceButtonErrorContext(action: string | undefined): DraftInteractionErrorContext {
  if (action === PresenceButtonAction.Confirm) return 'confirmPresence'
  if (action === PresenceButtonAction.Cancel) return 'cancelPresence'
  return 'status'
}

function contextGenericMessage(context: DraftInteractionErrorContext) {
  const messages: Record<DraftInteractionErrorContext, string> = {
    create: t.draftErrors.createFailed,
    list: t.draftErrors.listFailed,
    cancel: t.draftErrors.cancelFailed,
    closePresence: t.draftErrors.closePresenceFailed,
    defineCaptains: t.draftErrors.defineCaptainsFailed,
    definePickOrder: t.draftErrors.definePickOrderFailed,
    confirmPresence: t.draftErrors.confirmPresenceFailed,
    cancelPresence: t.draftErrors.cancelPresenceFailed,
    status: t.draftErrors.statusFailed,
  }

  return messages[context]
}

function normalizeErrorMessage(error: unknown) {
  const value = error instanceof Error ? error.message : String(error)
  return value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase()
}

function containsAny(value: string, ...needles: string[]) {
  return needles.some((needle) => value.includes(needle))
}
