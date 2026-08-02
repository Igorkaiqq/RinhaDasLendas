import type { InternalAxiosRequestConfig } from 'axios'

import { getCurrentLocale } from '@/i18n'

declare module 'axios' {
  interface InternalAxiosRequestConfig {
    _acceptLanguageManaged?: boolean
  }
}

export function applyRequestLocale(config: InternalAxiosRequestConfig) {
  if (!config.headers.has('Accept-Language') || config._acceptLanguageManaged) {
    config.headers.set('Accept-Language', getAcceptLanguage())
    config._acceptLanguageManaged = true
  }
  return config
}

export function getAcceptLanguage() {
  return getCurrentLocale() === 'en' ? 'en-US' : 'pt-BR'
}
