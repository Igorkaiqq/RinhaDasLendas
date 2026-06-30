import type { DraftStatusValue } from '@/types/draft'

export const DraftStatusValues = {
  Aberto: 'Aberto',
  Concluido: 'Concluido',
  Cancelado: 'Cancelado',
} as const satisfies Record<string, DraftStatusValue>

export const DRAFT_STATUS_OPTIONS: DraftStatusValue[] = [
  DraftStatusValues.Aberto,
  DraftStatusValues.Concluido,
  DraftStatusValues.Cancelado,
]

export const DRAFT_STATUS_LABELS: Record<DraftStatusValue, string> = {
  [DraftStatusValues.Aberto]: 'Aberto',
  [DraftStatusValues.Concluido]: 'Concluido',
  [DraftStatusValues.Cancelado]: 'Cancelado',
}
