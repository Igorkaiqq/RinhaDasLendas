import axios, { AxiosError, AxiosHeaders } from 'axios'
import type { AxiosAdapter, AxiosResponse, InternalAxiosRequestConfig } from 'axios'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { setLocale } from '@/i18n'

import { api } from './api'
import { listSeasons } from './seasons'

const originalAdapter = api.defaults.adapter

describe('shared API request locale', () => {
  beforeEach(() => setLocale('pt'))

  afterEach(() => {
    api.defaults.adapter = originalAdapter
    setLocale('pt')
    vi.restoreAllMocks()
  })

  it('derives Accept-Language at request time for normal and competitive requests', async () => {
    const requests: InternalAxiosRequestConfig[] = []
    api.defaults.adapter = successfulAdapter(requests)

    await api.get('/api/v1/health')
    setLocale('en')
    await listSeasons()

    expect(AxiosHeaders.from(requests[0]!.headers).get('Accept-Language')).toBe('pt-BR')
    expect(AxiosHeaders.from(requests[1]!.headers).get('Accept-Language')).toBe('en-US')
    expect(requests[1]!.url).toBe('/api/v1/temporadas')
  })

  it('preserves an explicit per-request Accept-Language override', async () => {
    const requests: InternalAxiosRequestConfig[] = []
    api.defaults.adapter = successfulAdapter(requests)

    await api.get('/api/v1/health', {
      headers: { 'Accept-Language': 'es-ES' },
    })

    expect(AxiosHeaders.from(requests[0]!.headers).get('Accept-Language')).toBe('es-ES')
  })

  it('sends locale on refresh and reevaluates it when retrying the original request', async () => {
    const requests: InternalAxiosRequestConfig[] = []
    let attempts = 0
    api.defaults.adapter = (async (config) => {
      requests.push(config)
      attempts += 1
      if (attempts === 1) throw unauthorized(config)
      return success(config, {})
    }) as AxiosAdapter
    const refresh = deferred<AxiosResponse>()
    const refreshPost = vi.spyOn(axios, 'post').mockImplementation(() => refresh.promise)

    const request = api.get('/api/v1/protected')
    await vi.waitFor(() => expect(refreshPost).toHaveBeenCalledTimes(1))
    const refreshHeaders = refreshPost.mock.calls[0]![2]?.headers as
      | Record<string, string>
      | undefined
    expect(refreshHeaders?.['Accept-Language']).toBe('pt-BR')

    setLocale('en')
    refresh.resolve(
      success({} as InternalAxiosRequestConfig, {
        accessToken: 'refreshed-token',
        usuario: {
          id: 'user-1',
          nome: 'User',
          email: 'user@example.com',
          roles: [],
          ativo: true,
        },
      }),
    )
    await request

    expect(AxiosHeaders.from(requests[0]!.headers).get('Accept-Language')).toBe('pt-BR')
    expect(AxiosHeaders.from(requests[1]!.headers).get('Accept-Language')).toBe('en-US')
  })
})

function successfulAdapter(requests: InternalAxiosRequestConfig[]): AxiosAdapter {
  return async (config) => {
    requests.push(config)
    return success(config, {
      page: 1,
      pageSize: 20,
      items: [],
      totalItems: 0,
      totalPages: 0,
      calendarioConfigurado: false,
      temporadaAtual: null,
      versaoCalendario: 0,
    })
  }
}

function success<T>(config: InternalAxiosRequestConfig, data: T): AxiosResponse<T> {
  return {
    data,
    status: 200,
    statusText: 'OK',
    headers: new AxiosHeaders(),
    config,
  }
}

function unauthorized(config: InternalAxiosRequestConfig) {
  return new AxiosError(
    'Unauthorized',
    'ERR_BAD_REQUEST',
    config,
    undefined,
    {
      data: {},
      status: 401,
      statusText: 'Unauthorized',
      headers: new AxiosHeaders(),
      config,
    },
  )
}

function deferred<T>() {
  let resolve!: (value: T) => void
  const promise = new Promise<T>((resolvePromise) => {
    resolve = resolvePromise
  })
  return { promise, resolve }
}
