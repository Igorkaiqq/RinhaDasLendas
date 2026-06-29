import { Client, GatewayIntentBits, MessageFlags, REST, Routes } from 'discord.js'
import { env } from './config/env.js'
import { draftCommands } from './discord/commands/draftCommands.js'
import { DiscordChannelAccessError, handleDraftCommand, handlePresenceButton, startDraftPolling } from './modules/drafts/draftInteractions.js'
import { t } from './shared/messages/index.js'

const client = new Client({ intents: [GatewayIntentBits.Guilds] })

client.once('clientReady', () => {
  console.log(`Discord bot online: ${client.user?.tag}`)
  startDraftPolling(client)
})

client.on('interactionCreate', async (interaction) => {
  try {
    if (interaction.isChatInputCommand()) {
      await handleDraftCommand(interaction)
      return
    }

    if (interaction.isButton() && interaction.customId.startsWith('presence:')) {
      await handlePresenceButton(interaction)
    }
  } catch (error) {
    console.error(error)
    if (interaction.isRepliable() && !interaction.replied) {
      await interaction.reply({ content: error instanceof DiscordChannelAccessError ? error.userMessage : t.genericError, flags: MessageFlags.Ephemeral })
    }
  }
})

const rest = new REST({ version: '10' }).setToken(env.DISCORD_TOKEN)
await rest.put(Routes.applicationGuildCommands(env.DISCORD_CLIENT_ID, env.DISCORD_GUILD_ID), { body: draftCommands })
await client.login(env.DISCORD_TOKEN)
