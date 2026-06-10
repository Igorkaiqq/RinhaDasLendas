export const PlayerStatus = {
  Active: 'Ativo',
  Inactive: 'Inativo',
} as const

export type PlayerStatusValue = (typeof PlayerStatus)[keyof typeof PlayerStatus]
