import { beforeEach, describe, expect, it, vi } from 'vitest'

import { api } from './api'
import { listEligibleCaptains, type Player } from './players'

vi.mock('./api', () => ({
  api: { get: vi.fn() },
}))

function player(id: string): Player {
  return { id, nomeExibicao: id } as Player
}

describe('players service', () => {
  beforeEach(() => {
    vi.mocked(api.get).mockReset()
  })

  it('loads eligible captains sequentially until the first incomplete page', async () => {
    const firstPage = Array.from({ length: 100 }, (_, index) => player(`captain-${index}`))
    const canonicalCaptain = player('canonical-page-2')
    const controller = new AbortController()
    vi.mocked(api.get)
      .mockResolvedValueOnce({ data: { page: 1, pageSize: 100, items: firstPage } })
      .mockResolvedValueOnce({ data: { page: 2, pageSize: 100, items: [canonicalCaptain] } })

    const result = await listEligibleCaptains(controller.signal)

    expect(result).toHaveLength(101)
    expect(result).toContain(canonicalCaptain)
    expect(api.get).toHaveBeenNthCalledWith(1, '/api/v1/jogadores/capitaes-elegiveis', { params: { page: 1, pageSize: 100 }, signal: controller.signal })
    expect(api.get).toHaveBeenNthCalledWith(2, '/api/v1/jogadores/capitaes-elegiveis', { params: { page: 2, pageSize: 100 }, signal: controller.signal })
  })

  it('uses the same abort signal for every page and stops a paginated load when aborted', async () => {
    const firstPage = Array.from({ length: 100 }, (_, index) => player(`captain-${index}`))
    const controller = new AbortController()
    vi.mocked(api.get)
      .mockResolvedValueOnce({ data: { page: 1, pageSize: 100, items: firstPage } })
      .mockImplementationOnce((_url, config) => new Promise((_resolve, reject) => {
        config?.signal?.addEventListener?.('abort', () => reject(new DOMException('Aborted', 'AbortError')), { once: true })
      }))

    const loading = listEligibleCaptains(controller.signal)
    await vi.waitFor(() => expect(api.get).toHaveBeenCalledTimes(2))
    controller.abort()

    await expect(loading).rejects.toThrow()
    expect(vi.mocked(api.get).mock.calls.every(([, config]) => config?.signal === controller.signal)).toBe(true)
  })
})
