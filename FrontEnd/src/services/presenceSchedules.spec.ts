import { AxiosError, AxiosHeaders } from 'axios'
import { beforeEach, describe, expect, it, vi } from 'vitest'

import { api } from './api'
import {
  archivePresenceSchedule,
  createPresenceSchedule,
  listPresenceScheduleOccurrences,
  listPresenceSchedules,
  pausePresenceSchedule,
  reactivatePresenceSchedule,
  updatePresenceSchedule,
} from './presenceSchedules'

vi.mock('./api', () => ({
  api: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}))

const schedule = {
  id: 'agenda-b',
  nome: 'Rinha semanal',
  observacao: null,
  status: 'Ativo' as const,
  diasSemana: ['Sexta' as const],
  horarioPublicacao: '18:00',
  horarioEncerramento: '20:00',
  proximaExecucaoEm: '2026-07-31T21:00:00Z',
  ultimaOcorrencia: null,
}

describe('presence schedules service', () => {
  beforeEach(() => vi.clearAllMocks())

  it('preserves the paginated backend order across two schedule pages', async () => {
    const firstPage = {
      page: 1,
      pageSize: 2,
      items: [{ ...schedule, id: 'agenda-a' }, schedule],
      totalItems: 4,
      totalPages: 2,
      activeItems: 2,
    }
    const secondPage = {
      page: 2,
      pageSize: 2,
      items: [
        { ...schedule, id: 'agenda-c', status: 'Pausado', proximaExecucaoEm: null },
        { ...schedule, id: 'agenda-d', status: 'Pausado', proximaExecucaoEm: null },
      ],
      totalItems: 4,
      totalPages: 2,
      activeItems: 2,
    }
    vi.mocked(api.get).mockResolvedValueOnce({ data: firstPage }).mockResolvedValueOnce({ data: secondPage })

    await expect(listPresenceSchedules(1, 2)).resolves.toEqual(firstPage)
    await expect(listPresenceSchedules(2, 2)).resolves.toEqual(secondPage)
    expect(api.get).toHaveBeenNthCalledWith(1, '/api/v1/discord/agendamentos-presenca', {
      params: { page: 1, pageSize: 2 },
    })
    expect(api.get).toHaveBeenNthCalledWith(2, '/api/v1/discord/agendamentos-presenca', {
      params: { page: 2, pageSize: 2 },
    })
  })

  it('encodes occurrence pagination and preserves its envelope', async () => {
    const page = {
      page: 2,
      pageSize: 10,
      items: [{
        id: 'occurrence-1',
        dataLocal: '2026-07-18',
        publicacaoPrevistaEm: '2026-07-18T21:00:00Z',
        encerramentoPrevistoEm: '2026-07-18T23:00:00Z',
        status: 'Criada' as const,
        draftMontagemId: 'draft-1',
        messageCode: null,
      }],
      totalItems: 11,
      totalPages: 2,
    }
    vi.mocked(api.get).mockResolvedValue({ data: page })

    await expect(listPresenceScheduleOccurrences('agenda/a', 2, 10)).resolves.toEqual(page)
    expect(api.get).toHaveBeenCalledWith('/api/v1/discord/agendamentos-presenca/agenda%2Fa/ocorrencias', {
      params: { page: 2, pageSize: 10 },
    })
  })

  it('uses the mutation endpoints and normalizes time values to HH:mm', async () => {
    const payload = {
      nome: '  Rinha semanal  ',
      observacao: '',
      diasSemana: ['Sexta' as const],
      horarioPublicacao: '18:00:45',
      horarioEncerramento: '20:00:59',
    }
    vi.mocked(api.post).mockResolvedValue({ data: schedule })
    vi.mocked(api.put).mockResolvedValue({ data: schedule })
    vi.mocked(api.delete).mockResolvedValue({ data: undefined })

    await createPresenceSchedule(payload)
    await updatePresenceSchedule('agenda/a', payload)
    await pausePresenceSchedule('agenda/a')
    await reactivatePresenceSchedule('agenda/a')
    await archivePresenceSchedule('agenda/a')

    const expectedPayload = {
      nome: 'Rinha semanal',
      observacao: null,
      diasSemana: ['Sexta'],
      horarioPublicacao: '18:00',
      horarioEncerramento: '20:00',
    }
    expect(api.post).toHaveBeenNthCalledWith(1, '/api/v1/discord/agendamentos-presenca', expectedPayload)
    expect(api.put).toHaveBeenCalledWith('/api/v1/discord/agendamentos-presenca/agenda%2Fa', expectedPayload)
    expect(api.post).toHaveBeenNthCalledWith(2, '/api/v1/discord/agendamentos-presenca/agenda%2Fa/pausar')
    expect(api.post).toHaveBeenNthCalledWith(3, '/api/v1/discord/agendamentos-presenca/agenda%2Fa/reativar')
    expect(api.delete).toHaveBeenCalledWith('/api/v1/discord/agendamentos-presenca/agenda%2Fa')
  })

  it.each([401, 403, 409, 500])('propagates status %s and messageCode without returning an empty list', async (status) => {
    const error = new AxiosError('request failed', 'ERR_BAD_RESPONSE', undefined, undefined, {
      status,
      statusText: 'Error',
      headers: new AxiosHeaders(),
      config: { headers: new AxiosHeaders() },
      data: { messageCode: 'MV097' },
    })
    vi.mocked(api.get).mockRejectedValue(error)

    const promise = listPresenceSchedules(1, 20)
    await expect(promise).rejects.toMatchObject({
      status,
      messageCode: 'MV097',
    })
  })
})
