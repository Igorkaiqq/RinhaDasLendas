export interface DiscordConfiguration {
  id?: string | null
  guildId: string
  presenceChannelId: string
  newsChannelId: string
  adminChannelId: string
  draftChannelId: string
  matchResultChannelId: string
  botEnabled: boolean
}
