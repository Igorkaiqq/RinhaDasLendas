import { describe, expect, it } from 'vitest'

import { MessageCode } from '@/constants/messageCode'
import { getMessage } from './messageService'

describe('messageService', () => {
  it('returns Portuguese text for pt locale', () => {
    expect(getMessage(MessageCode.LoadingInformation, 'pt')).toBe('Carregando informações')
  })

  it('returns English text for en locale', () => {
    expect(getMessage(MessageCode.LoadingInformation, 'en')).toBe('Loading information')
  })

  it('returns code fallback for unknown message codes', () => {
    expect(getMessage('MX999', 'pt')).toBe('[MX999]')
  })
})
