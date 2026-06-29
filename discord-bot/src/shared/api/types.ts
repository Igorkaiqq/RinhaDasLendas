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

export interface DraftMontagem {
  id: string
  nome: string
  observacoes?: string | null
  status: string
  tamanhoEquipe: number
  quantidadeTimes: number
  quantidadeReservas: number
  horarioEncerramentoPresenca?: string | null
  discordGuildId?: string | null
  discordPresenceMessageId?: string | null
  presencas: Array<{ id: string; nomeExibicao: string; status: string; origemConfirmacao: string }>
  times: Array<{ id: string; nome: string; cor: string; capitaoId?: string | null; jogadores: Array<{ nomeExibicao: string; capitao: boolean }> }>
  reservas: Array<{ nomeExibicao: string }>
}
