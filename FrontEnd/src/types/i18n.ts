export type LocaleCode = 'pt-BR' | 'en-US'

export const DEFAULT_LOCALE: LocaleCode = 'pt-BR'
export const SUPPORTED_LOCALES = ['pt-BR', 'en-US'] as const satisfies readonly LocaleCode[]
