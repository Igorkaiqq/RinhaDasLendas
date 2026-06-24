export type LocaleCode = 'pt' | 'en'

export const DEFAULT_LOCALE: LocaleCode = 'pt'
export const SUPPORTED_LOCALES = ['pt', 'en'] as const satisfies readonly LocaleCode[]
