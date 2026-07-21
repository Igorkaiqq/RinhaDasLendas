import 'dotenv/config'
import { z } from 'zod'

const schema = z.object({
  DISCORD_TOKEN: z.string().min(1),
  DISCORD_CLIENT_ID: z.string().min(1),
  DISCORD_GUILD_ID: z.string().min(1),
  FRONTEND_PUBLIC_URL: z.string().url().default('http://localhost:5173'),
  DRAFT_NOTIFY_ROLE_ID: z.string().default(''),
  RINHA_API_BASE_URL: z.string().url(),
  RINHA_API_INTERNAL_TOKEN: z.string().min(1),
  BOT_LOCALE: z.enum(['pt-BR', 'en-US']).default('pt-BR'),
  NODE_ENV: z.string().default('development'),
  LOG_LEVEL: z.string().default('debug'),
})

export const env = schema.parse(process.env)
