import { createI18n } from 'vue-i18n'

import { DEFAULT_LOCALE, type LocaleCode } from '@/types/i18n'

import en from './locales/en.json'
import pt from './locales/pt.json'

export const messages = {
  pt,
  en,
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
