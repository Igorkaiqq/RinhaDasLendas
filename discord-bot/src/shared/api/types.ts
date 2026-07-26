export interface DiscordConfiguration {
  guildId: string
  presenceChannelId: string
  draftChannelId: string
  botEnabled: boolean
}

export interface DiscordUserLink {
  vinculado: boolean
  usuarioId?: string | null
  jogadorId?: string | null
  nomeExibicao?: string | null
  roles: string[]
}

export type DiscordPublicationType = 'Presenca' | 'ChamadaPresenca' | 'TimesDefinidos' | 'Cancelamento'

export interface DiscordPublicationClaim {
  adquirido: boolean
  claimId?: string | null
  expiraEm?: string | null
  status: string
}

export interface DraftMontagem {
  id: string
  nome: string
  status: string
  horarioEncerramentoPresenca?: string | null
  discordPresenceMessageId?: string | null
  publicacoesDiscord?: Array<{ tipo: DiscordPublicationType; status: string }>
  arquivado: boolean
  versaoEstado: number
  presencas: Array<{ nomeExibicao: string; status: string }>
  times: Array<{ nome: string; jogadores: Array<{ nomeExibicao: string; capitao: boolean }> }>
  reservas: Array<{ nomeExibicao: string }>
}
