import { describe, expect, it } from 'vitest'

import { DraftMontagemStatusValues } from '@/constants/draftMontagem'

import { DRAFT_MONTAGEM_STATUS_OPTIONS } from './draftMontagemStatus'

describe('DRAFT_MONTAGEM_STATUS_OPTIONS', () => {
  it('provides all seven filters in canonical lifecycle order', () => {
    expect(DRAFT_MONTAGEM_STATUS_OPTIONS).toEqual([
      DraftMontagemStatusValues.PresencaAberta,
      DraftMontagemStatusValues.PresencaEncerrada,
      DraftMontagemStatusValues.CapitaesDefinidos,
      DraftMontagemStatusValues.OrdemDefinida,
      DraftMontagemStatusValues.Aberta,
      DraftMontagemStatusValues.Finalizada,
      DraftMontagemStatusValues.Cancelada,
    ])
  })
})
