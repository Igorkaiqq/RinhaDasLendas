import 'dotenv/config'
import { z } from 'zod'

const schema = z.object({
  DISCORD_TOKEN: z.string().min(1),
  DISCORD_CLIENT_ID: z.string().min(1),
  DISCORD_GUILD_ID: z.string().min(1),
  RINHA_API_BASE_URL: z.string().url(),
  RINHA_API_INTERNAL_TOKEN: z.string().min(1),
  NODE_ENV: z.string().default('development'),
  LOG_LEVEL: z.string().default('debug'),
})

export const env = schema.parse(process.env)
