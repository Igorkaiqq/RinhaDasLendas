// @vitest-environment happy-dom
import { enableAutoUnmount, flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { nextTick } from 'vue'

import { i18n } from '@/i18n'
import * as service from '@/services/presenceSchedules'
import type { PresenceScheduleSummary } from '@/types/presenceSchedule'

import PresenceScheduleSection from './PresenceScheduleSection.vue'

enableAutoUnmount(afterEach)

vi.mock('@/services/presenceSchedules', () => ({
  listPresenceSchedules: vi.fn(), createPresenceSchedule: vi.fn(), updatePresenceSchedule: vi.fn(),
  pausePresenceSchedule: vi.fn(), reactivatePresenceSchedule: vi.fn(), archivePresenceSchedule: vi.fn(),
  listPresenceScheduleOccurrences: vi.fn(), PresenceScheduleServiceError: class extends Error {},
}))

const baseSchedule = {
  id: 'agenda-a', nome: 'Rinha semanal', observacao: 'Times de cinco', status: 'Ativo' as const,
  diasSemana: ['Sexta' as const], horarioPublicacao: '18:00', horarioEncerramento: '20:00',
  proximaExecucaoEm: '2026-07-31T21:00:00Z', ultimaOcorrencia: null,
}

function deferred<T>() {
  let resolve!: (value: T) => void
  const promise = new Promise<T>((resolvePromise) => { resolve = resolvePromise })
  return { promise, resolve }
}

function pageWith(items: PresenceScheduleSummary[], page = 1, totalPages = 1) {
  return { page, pageSize: 6, items, totalItems: items.length, totalPages }
}

async function fillCreateForm(wrapper: Awaited<ReturnType<typeof mountSection>>, name = 'Nova agenda') {
  await wrapper.get('#presence-schedule-name').setValue(name)
  await wrapper.get('[data-weekday="Sexta"]').trigger('click')
  await wrapper.get('#presence-schedule-publication').setValue('18:00')
  await wrapper.get('#presence-schedule-closing').setValue('20:00')
  await wrapper.get('form').trigger('submit')
}

async function mountSection() {
  const wrapper = mount(PresenceScheduleSection, {
    attachTo: document.body,
    global: { plugins: [i18n], stubs: { teleport: { template: '<div data-teleport-stub><slot /></div>' } } },
  })
  await nextTick()
  await nextTick()
  return wrapper
}

describe('PresenceScheduleSection', () => {
  beforeEach(() => vi.clearAllMocks())
  afterEach(() => { document.body.innerHTML = '' })

  it('preserves four unique IDs in exact backend order across tied and paused pages', async () => {
    const tiedExecution = '2026-07-31T21:00:00Z'
    vi.mocked(service.listPresenceSchedules)
      .mockResolvedValueOnce({
        page: 1,
        pageSize: 6,
        items: [
          { ...baseSchedule, id: 'agenda-a', nome: 'Mesmo nome', proximaExecucaoEm: tiedExecution },
          { ...baseSchedule, id: 'agenda-b', nome: 'Mesmo nome', proximaExecucaoEm: tiedExecution },
        ],
        totalItems: 4,
        totalPages: 2,
      })
      .mockResolvedValueOnce({
        page: 2,
        pageSize: 6,
        items: [
          { ...baseSchedule, id: 'agenda-c', nome: 'Seguinte' },
          { ...baseSchedule, id: 'agenda-d', nome: 'Pausada final', status: 'Pausado', proximaExecucaoEm: null },
        ],
        totalItems: 4,
        totalPages: 2,
      })
    const wrapper = await mountSection()
    await flushPromises()

    expect(wrapper.findAll('[data-schedule-id]').map((card) => card.attributes('data-schedule-id'))).toEqual(['agenda-a', 'agenda-b'])
    await wrapper.get('[data-load-more]').trigger('click')
    await flushPromises()
    const ids = wrapper.findAll('[data-schedule-id]').map((card) => card.attributes('data-schedule-id'))
    expect(ids).toEqual(['agenda-a', 'agenda-b', 'agenda-c', 'agenda-d'])
    expect(new Set(ids).size).toBe(4)
    expect(wrapper.text()).toContain('Pausado')
    expect(wrapper.find('[data-load-more]').exists()).toBe(false)
  })

  it.each([
    ['Processando', 'Em processamento'], ['Bloqueada', 'Bloqueada'], ['Criada', 'Criada'],
    ['Perdida', 'Perdida'], ['Falha', 'Falha'],
  ] as const)('renders the %s occurrence state with text', async (status, label) => {
    vi.mocked(service.listPresenceSchedules).mockResolvedValue({
      page: 1, pageSize: 6, items: [{ ...baseSchedule, ultimaOcorrencia: {
        id: 'occ-1', dataLocal: '2026-07-18', publicacaoPrevistaEm: '2026-07-18T21:00:00Z',
        encerramentoPrevistoEm: '2026-07-18T23:00:00Z', status, draftMontagemId: null,
        messageCode: status === 'Falha' ? 'MV096' : null,
      } }], totalItems: 1, totalPages: 1,
    })
    const wrapper = await mountSection()
    await flushPromises()
    expect(wrapper.get('[data-occurrence-status]').text()).toContain(label)
  })

  it('shows skeleton, retryable error and an empty state with CTA', async () => {
    let reject!: (error: Error) => void
    vi.mocked(service.listPresenceSchedules).mockImplementationOnce(() => new Promise((_resolve, rejectPromise) => { reject = rejectPromise }))
    const wrapper = await mountSection()
    expect(wrapper.findAll('[data-schedule-skeleton]').length).toBeGreaterThan(0)
    expect(wrapper.get('[data-schedule-loading]').attributes()).toMatchObject({ role: 'status', 'aria-live': 'polite' })
    expect(wrapper.get('[data-schedule-loading]').attributes('aria-busy')).toBe('true')
    reject(new Error('network'))
    await flushPromises()
    expect(wrapper.text()).toContain('Não foi possível carregar os agendamentos.')

    vi.mocked(service.listPresenceSchedules).mockResolvedValueOnce({ page: 1, pageSize: 6, items: [], totalItems: 0, totalPages: 0 })
    await wrapper.get('[data-schedule-retry]').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Nenhum agendamento criado ainda.')
    expect(wrapper.get('[data-empty-create]').text()).toBe('Criar primeiro agendamento')
  })

  it('does not move focus to an action when closed dialogs mount', async () => {
    vi.mocked(service.listPresenceSchedules).mockResolvedValue(pageWith([baseSchedule]))
    const outside = document.createElement('button')
    document.body.append(outside)
    outside.focus()
    await mountSection()
    await flushPromises()

    expect(document.activeElement).toBe(outside)
  })

  it('announces incremental loading and marks the list busy', async () => {
    const nextPage = deferred<Awaited<ReturnType<typeof service.listPresenceSchedules>>>()
    vi.mocked(service.listPresenceSchedules)
      .mockResolvedValueOnce({ page: 1, pageSize: 6, items: [baseSchedule], totalItems: 2, totalPages: 2 })
      .mockReturnValueOnce(nextPage.promise)
    const wrapper = await mountSection()
    await flushPromises()

    await wrapper.get('[data-load-more]').trigger('click')
    expect(wrapper.get('[data-schedule-list]').attributes('aria-busy')).toBe('true')
    expect(wrapper.get('[data-load-more-status]').attributes()).toMatchObject({ role: 'status', 'aria-live': 'polite' })
    nextPage.resolve({ page: 2, pageSize: 6, items: [{ ...baseSchedule, id: 'agenda-b' }], totalItems: 2, totalPages: 2 })
    await flushPromises()
  })

  it('ignores a stale loadMore response when a mutation reload resolves first', async () => {
    const staleLoadMore = deferred<Awaited<ReturnType<typeof service.listPresenceSchedules>>>()
    const mutationReload = deferred<Awaited<ReturnType<typeof service.listPresenceSchedules>>>()
    vi.mocked(service.listPresenceSchedules)
      .mockResolvedValueOnce({ page: 1, pageSize: 6, items: [baseSchedule, { ...baseSchedule, id: 'agenda-b' }], totalItems: 3, totalPages: 2 })
      .mockReturnValueOnce(staleLoadMore.promise)
      .mockReturnValueOnce(mutationReload.promise)
    vi.mocked(service.pausePresenceSchedule).mockResolvedValue({ ...baseSchedule, status: 'Pausado' })
    const wrapper = await mountSection()
    await flushPromises()

    await wrapper.get('[data-load-more]').trigger('click')
    await wrapper.get('[data-pause-schedule]').trigger('click')
    await wrapper.get('[data-confirm-action]').trigger('click')
    mutationReload.resolve({
      page: 1,
      pageSize: 6,
      items: [{ ...baseSchedule, status: 'Pausado' }, { ...baseSchedule, id: 'agenda-b' }],
      totalItems: 2,
      totalPages: 1,
    })
    await flushPromises()
    staleLoadMore.resolve({ page: 2, pageSize: 6, items: [{ ...baseSchedule, id: 'stale-agenda' }], totalItems: 3, totalPages: 2 })
    await flushPromises()

    expect(wrapper.findAll('[data-schedule-id]').map((card) => card.attributes('data-schedule-id'))).toEqual(['agenda-a', 'agenda-b'])
    expect(wrapper.find('[data-schedule-id="stale-agenda"]').exists()).toBe(false)
    expect(wrapper.get('[data-schedule-id="agenda-a"]').text()).toContain('Pausado')
  })

  it('supports create, edit, pause, reactivate, archive and history actions', async () => {
    vi.mocked(service.listPresenceSchedules).mockResolvedValue({ page: 1, pageSize: 6, items: [baseSchedule, { ...baseSchedule, id: 'agenda-b', status: 'Pausado' }], totalItems: 2, totalPages: 1 })
    vi.mocked(service.pausePresenceSchedule).mockResolvedValue({ ...baseSchedule, status: 'Pausado' })
    vi.mocked(service.reactivatePresenceSchedule).mockResolvedValue({ ...baseSchedule, id: 'agenda-b' })
    vi.mocked(service.archivePresenceSchedule).mockResolvedValue()
    const wrapper = await mountSection()
    await flushPromises()

    expect(wrapper.findAll('[data-edit-schedule]')).toHaveLength(2)
    expect(wrapper.findAll('[data-view-history]')).toHaveLength(2)
    await wrapper.get('[data-pause-schedule]').trigger('click')
    expect(wrapper.findComponent({ name: 'PresenceScheduleConfirmDialog' }).exists()).toBe(true)
    await wrapper.get('[data-reactivate-schedule]').trigger('click')
    await nextTick()
    await nextTick()
    await wrapper.get('[data-confirm-action]').trigger('click')
    await flushPromises()
    expect(service.reactivatePresenceSchedule).toHaveBeenCalledWith('agenda-b')
    await wrapper.get('[data-archive-schedule]').trigger('click')
    expect(wrapper.text()).toContain('Arquivar agendamento')
  })

  it('creates a schedule with the form payload and restores the stable create action', async () => {
    const created = { ...baseSchedule, id: 'agenda-created', nome: 'Nova agenda' }
    vi.mocked(service.listPresenceSchedules)
      .mockResolvedValueOnce(pageWith([]))
      .mockResolvedValueOnce(pageWith([created]))
    vi.mocked(service.createPresenceSchedule).mockResolvedValue(created)
    const wrapper = await mountSection()
    await flushPromises()

    await wrapper.get('[data-create-schedule]').trigger('click')
    await fillCreateForm(wrapper)
    await flushPromises()

    expect(service.createPresenceSchedule).toHaveBeenCalledWith({
      nome: 'Nova agenda', observacao: null, diasSemana: ['Sexta'], horarioPublicacao: '18:00', horarioEncerramento: '20:00',
    })
    expect(wrapper.get('[data-schedule-id="agenda-created"]').text()).toContain('Nova agenda')
    expect(document.activeElement).toBe(wrapper.get('[data-create-schedule]').element)
  })

  it('edits a schedule and focuses its rerendered edit action', async () => {
    const updated = { ...baseSchedule, nome: 'Agenda atualizada' }
    vi.mocked(service.listPresenceSchedules)
      .mockResolvedValueOnce(pageWith([baseSchedule]))
      .mockResolvedValueOnce(pageWith([updated]))
    vi.mocked(service.updatePresenceSchedule).mockResolvedValue(updated)
    const wrapper = await mountSection()
    await flushPromises()

    await wrapper.get('[data-edit-schedule]').trigger('click')
    await wrapper.get('#presence-schedule-name').setValue('Agenda atualizada')
    await wrapper.get('form').trigger('submit')
    await flushPromises()

    expect(service.updatePresenceSchedule).toHaveBeenCalledWith('agenda-a', expect.objectContaining({ nome: 'Agenda atualizada' }))
    expect(wrapper.get('[data-schedule-id="agenda-a"]').text()).toContain('Agenda atualizada')
    expect(document.activeElement).toBe(wrapper.get('[data-schedule-id="agenda-a"] [data-edit-schedule]').element)
  })

  it('pauses a schedule and focuses the replacement reactivate action', async () => {
    const paused = { ...baseSchedule, status: 'Pausado' as const, proximaExecucaoEm: null }
    vi.mocked(service.listPresenceSchedules)
      .mockResolvedValueOnce(pageWith([baseSchedule]))
      .mockResolvedValueOnce(pageWith([paused]))
    vi.mocked(service.pausePresenceSchedule).mockResolvedValue(paused)
    const wrapper = await mountSection()
    await flushPromises()

    await wrapper.get('[data-pause-schedule]').trigger('click')
    await wrapper.get('[data-confirm-action]').trigger('click')
    await flushPromises()

    expect(service.pausePresenceSchedule).toHaveBeenCalledWith('agenda-a')
    expect(document.activeElement).toBe(wrapper.get('[data-schedule-id="agenda-a"] [data-reactivate-schedule]').element)
  })

  it('reactivates a schedule and focuses the replacement pause action', async () => {
    const paused = { ...baseSchedule, status: 'Pausado' as const, proximaExecucaoEm: null }
    vi.mocked(service.listPresenceSchedules)
      .mockResolvedValueOnce(pageWith([paused]))
      .mockResolvedValueOnce(pageWith([baseSchedule]))
    vi.mocked(service.reactivatePresenceSchedule).mockResolvedValue(baseSchedule)
    const wrapper = await mountSection()
    await flushPromises()

    await wrapper.get('[data-reactivate-schedule]').trigger('click')
    await wrapper.get('[data-confirm-action]').trigger('click')
    await flushPromises()

    expect(service.reactivatePresenceSchedule).toHaveBeenCalledWith('agenda-a')
    expect(document.activeElement).toBe(wrapper.get('[data-schedule-id="agenda-a"] [data-pause-schedule]').element)
  })

  it('archives a schedule and focuses the next logical archive action', async () => {
    const nextSchedule = { ...baseSchedule, id: 'agenda-b', nome: 'Próxima agenda' }
    vi.mocked(service.listPresenceSchedules)
      .mockResolvedValueOnce(pageWith([baseSchedule, nextSchedule]))
      .mockResolvedValueOnce(pageWith([nextSchedule]))
    vi.mocked(service.archivePresenceSchedule).mockResolvedValue()
    const wrapper = await mountSection()
    await flushPromises()

    await wrapper.get('[data-schedule-id="agenda-a"] [data-archive-schedule]').trigger('click')
    await wrapper.get('[data-confirm-action]').trigger('click')
    await flushPromises()

    expect(service.archivePresenceSchedule).toHaveBeenCalledWith('agenda-a')
    expect(wrapper.find('[data-schedule-id="agenda-a"]').exists()).toBe(false)
    expect(document.activeElement).toBe(wrapper.get('[data-schedule-id="agenda-b"] [data-archive-schedule]').element)
  })

  it('opens history for the selected schedule and requests its first page', async () => {
    vi.mocked(service.listPresenceSchedules).mockResolvedValue(pageWith([baseSchedule]))
    vi.mocked(service.listPresenceScheduleOccurrences).mockResolvedValue({ page: 1, pageSize: 10, items: [], totalItems: 0, totalPages: 0 })
    const wrapper = await mountSection()
    await flushPromises()

    await wrapper.get('[data-view-history]').trigger('click')
    await flushPromises()

    expect(service.listPresenceScheduleOccurrences).toHaveBeenCalledWith('agenda-a', 1, 10)
    expect(wrapper.get('[role="dialog"]').text()).toContain('Histórico de Rinha semanal')
  })

  it('uses semantic single-column-ready cards and touch-sized actions', async () => {
    vi.mocked(service.listPresenceSchedules).mockResolvedValue({ page: 1, pageSize: 6, items: [baseSchedule], totalItems: 1, totalPages: 1 })
    const wrapper = await mountSection()
    await flushPromises()
    expect(wrapper.find('ul[data-schedule-list]').exists()).toBe(true)
    expect(wrapper.findAll('ul[data-schedule-list] > li')).toHaveLength(1)
    expect(wrapper.get('[data-schedule-card]').classes()).toContain('presence-schedule-card')
    expect(wrapper.get('[data-card-actions]').classes()).toContain('presence-schedule-card__actions')
  })

  it('keeps long unbroken names and observations inside semantic mobile cards', async () => {
    const longName = 'A'.repeat(100)
    const longObservation = 'B'.repeat(500)
    vi.mocked(service.listPresenceSchedules).mockResolvedValue(pageWith([{ ...baseSchedule, nome: longName, observacao: longObservation }]))
    Object.defineProperty(globalThis, 'innerWidth', { configurable: true, value: 320 })
    const wrapper = await mountSection()
    await flushPromises()

    const card = wrapper.get('li[data-schedule-id="agenda-a"]')
    expect(card.text()).toContain(longName)
    expect(card.text()).toContain(longObservation)
    expect(card.get('[data-slot="card-title"]').classes()).toContain('presence-schedule-card__name')
    expect(card.get('[data-slot="card-description"]').classes()).toContain('presence-schedule-card__description')
  })

  it('closes history with Escape and restores the history trigger', async () => {
    vi.mocked(service.listPresenceSchedules).mockResolvedValue({ page: 1, pageSize: 6, items: [baseSchedule], totalItems: 1, totalPages: 1 })
    vi.mocked(service.listPresenceScheduleOccurrences).mockResolvedValue({ page: 1, pageSize: 10, items: [], totalItems: 0, totalPages: 0 })
    const wrapper = await mountSection()
    await flushPromises()
    const trigger = wrapper.get('[data-view-history]')

    await trigger.trigger('click')
    await nextTick()
    await nextTick()
    expect(wrapper.find('[role="dialog"]').exists()).toBe(true)

    await wrapper.get('[role="dialog"]').trigger('keydown', { key: 'Escape' })
    await nextTick()
    await nextTick()
    expect(wrapper.find('[role="dialog"]').exists()).toBe(false)
    expect(document.activeElement).toBe(trigger.element)
  })
})
