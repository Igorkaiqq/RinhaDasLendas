import { createI18n } from 'vue-i18n'

import { DEFAULT_LOCALE, type LocaleCode } from '@/types/i18n'

import enUS from './locales/en-US.json'
import ptBR from './locales/pt-BR.json'

export const messages = {
  'pt-BR': ptBR,
  'en-US': enUS,
} as const

export const i18n = createI18n({
  legacy: false,
  locale: DEFAULT_LOCALE,
  fallbackLocale: DEFAULT_LOCALE,
  messages,
})

export function setLocale(locale: LocaleCode) {
  i18n.global.locale.value = locale
}

export function getCurrentLocale(): LocaleCode {
  return i18n.global.locale.value as LocaleCode
}
