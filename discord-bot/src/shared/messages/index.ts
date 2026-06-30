import { enUS } from './en-US.js'
import { ptBR } from './pt-BR.js'
import { env } from '../../config/env.js'

export const messages = { 'pt-BR': ptBR, 'en-US': enUS }
export const t = messages[env.BOT_LOCALE]
