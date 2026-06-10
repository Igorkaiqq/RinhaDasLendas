import { describe, expect, it } from 'vitest'

import { MessageCode } from '@/constants/messageCode'
import { getMessage } from './messageService'

describe('messageService', () => {
  it('returns Portuguese text for pt-BR locale', () => {
    expect(getMessage(MessageCode.LoadingInformation, 'pt-BR')).toBe('Carregando informações')
  })

  it('returns English text for en-US locale', () => {
    expect(getMessage(MessageCode.LoadingInformation, 'en-US')).toBe('Loading information')
  })

  it('returns code fallback for unknown message codes', () => {
    expect(getMessage('MX999', 'pt-BR')).toBe('[MX999]')
  })
})
