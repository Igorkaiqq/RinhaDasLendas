import type { GuildMember } from 'discord.js'
import { t } from '../../shared/messages/index.js'

export function buildWelcomeMessage(siteUrl: string) {
  const normalizedUrl = siteUrl.replace(/\/$/, '')
  return [
    t.welcome.title,
    '',
    t.welcome.intro,
    '',
    t.welcome.cta.replace('{url}', normalizedUrl),
    '',
    ...t.welcome.steps.map((step, index) => `${index + 1}. ${step}`),
    '',
    t.welcome.footer,
  ].join('\n')
}

export async function handleGuildMemberAdd(member: GuildMember, siteUrl: string) {
  await member.send(buildWelcomeMessage(siteUrl))
}
