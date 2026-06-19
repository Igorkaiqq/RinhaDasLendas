import type { DraftStatusValue } from '@/types/draft'

export const DRAFT_STATUS_OPTIONS: DraftStatusValue[] = ['Aberto', 'Concluido', 'Cancelado']

export const DRAFT_STATUS_LABELS: Record<DraftStatusValue, string> = {
  Aberto: 'Aberto',
  Concluido: 'Concluido',
  Cancelado: 'Cancelado',
}
