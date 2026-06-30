import { DraftMontagemStatusValues } from '@/constants/draftMontagem'
import type { DraftMontagemStatus } from '@/types/draftMontagem'

export const DRAFT_MONTAGEM_STATUS_OPTIONS: DraftMontagemStatus[] = [
  DraftMontagemStatusValues.PresencaAberta,
  DraftMontagemStatusValues.PresencaEncerrada,
  DraftMontagemStatusValues.CapitaesDefinidos,
  DraftMontagemStatusValues.Aberta,
  DraftMontagemStatusValues.Finalizada,
  DraftMontagemStatusValues.Cancelada,
]
