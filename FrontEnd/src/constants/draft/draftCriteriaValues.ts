import type { DraftCriteriaValue } from '@/types/draft'

export const DraftCriteriaValues = {
  Manual: 'Manual',
  Sorteio: 'Sorteio',
} as const satisfies Record<string, DraftCriteriaValue>
