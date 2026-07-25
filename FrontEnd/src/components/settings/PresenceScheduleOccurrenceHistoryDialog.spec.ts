// @vitest-environment happy-dom
import { enableAutoUnmount, flushPromises, mount } from '@vue/test-utils'
import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { nextTick } from 'vue'

import { i18n } from '@/i18n'
import { listPresenceScheduleOccurrences } from '@/services/presenceSchedules'

import PresenceScheduleOccurrenceHistoryDialog from './PresenceScheduleOccurrenceHistoryDialog.vue'

enableAutoUnmount(afterEach)
const presenceScheduleStyles = readFileSync(resolve(process.cwd(), 'src/styles/main.css'), 'utf8')

vi.mock('@/services/presenceSchedules', () => ({ listPresenceScheduleOccurrences: vi.fn() }))

const occurrence = {
  id: 'occurrence-1', dataLocal: '2026-07-18', publicacaoPrevistaEm: '2026-07-18T21:00:00Z',
  encerramentoPrevistoEm: '2026-07-18T23:00:00Z', status: 'Criada' as const,
  draftMontagemId: 'draft-1', messageCode: null,
}

function deferred<T>() {
  let resolve!: (value: T) => void
  let reject!: (error: Error) => void
  const promise = new Promise<T>((resolvePromise, rejectPromise) => {
    resolve = resolvePromise
    reject = rejectPromise
  })
  return { promise, reject, resolve }
}

async function mountDialog(returnFocusTo?: HTMLElement) {
  const wrapper = mount(PresenceScheduleOccurrenceHistoryDialog, {
    attachTo: document.body,
    props: { open: true, scheduleId: 'agenda-1', scheduleName: 'Rinha semanal', returnFocusTo },
    global: { plugins: [i18n], stubs: { teleport: { template: '<div data-teleport-stub><slot /></div>' } } },
  })
  await nextTick()
  await nextTick()
  return wrapper
}

describe('PresenceScheduleOccurrenceHistoryDialog', () => {
  beforeEach(() => vi.clearAllMocks())
  afterEach(() => { document.body.innerHTML = '' })

  it('loads paginated history, renders semantic items and announces page changes', async () => {
    vi.mocked(listPresenceScheduleOccurrences)
      .mockResolvedValueOnce({ page: 1, pageSize: 10, items: [occurrence], totalItems: 11, totalPages: 2 })
      .mockResolvedValueOnce({ page: 2, pageSize: 10, items: [{ ...occurrence, id: 'occurrence-2', status: 'Bloqueada' }], totalItems: 11, totalPages: 2 })
    const wrapper = await mountDialog()
    await flushPromises()

    expect(listPresenceScheduleOccurrences).toHaveBeenCalledWith('agenda-1', 1, 10)
    expect(wrapper.get('[role="dialog"]').attributes('aria-labelledby')).toBeTruthy()
    expect(wrapper.get('ol').attributes('role')).toBe('list')
    expect(wrapper.findAll('[data-occurrence]')).toHaveLength(1)
    expect(wrapper.text()).toContain('Draft draft-1')

    await wrapper.get('[data-history-next]').trigger('click')
    await flushPromises()
    expect(listPresenceScheduleOccurrences).toHaveBeenLastCalledWith('agenda-1', 2, 10)
    expect(wrapper.get('[aria-live="polite"]').text()).toContain('Página 2 de 2')
    expect(wrapper.get('[data-history-next]').attributes('disabled')).toBeDefined()
    expect(wrapper.get('[data-history-pagination]').findAll('button').map((button) => button.text())).toEqual(['Anterior', 'Próxima'])
  })

  it('shows loading, retryable error and empty states without closing', async () => {
    let reject!: (error: Error) => void
    vi.mocked(listPresenceScheduleOccurrences).mockImplementationOnce(() => new Promise((_resolve, rejectPromise) => { reject = rejectPromise }))
    const wrapper = await mountDialog()
    expect(wrapper.findAll('[data-history-skeleton]').length).toBeGreaterThan(0)
    expect(wrapper.get('[data-history-loading]').attributes()).toMatchObject({
      role: 'status',
      'aria-live': 'polite',
    })
    expect(wrapper.get('[data-history-body]').attributes('aria-busy')).toBe('true')
    reject(new Error('network'))
    await flushPromises()
    expect(wrapper.text()).toContain('Não foi possível carregar o histórico.')

    vi.mocked(listPresenceScheduleOccurrences).mockResolvedValueOnce({ page: 1, pageSize: 10, items: [], totalItems: 0, totalPages: 0 })
    await wrapper.get('[data-history-retry]').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Nenhuma execução registrada ainda.')
    expect(wrapper.find('[role="dialog"]').exists()).toBe(true)
  })

  it('ignores stale responses after closing and reopening the history', async () => {
    const stale = deferred<Awaited<ReturnType<typeof listPresenceScheduleOccurrences>>>()
    const current = deferred<Awaited<ReturnType<typeof listPresenceScheduleOccurrences>>>()
    vi.mocked(listPresenceScheduleOccurrences)
      .mockReturnValueOnce(stale.promise)
      .mockReturnValueOnce(current.promise)
    const wrapper = await mountDialog()

    await wrapper.setProps({ open: false })
    await wrapper.setProps({ open: true })
    current.resolve({ page: 1, pageSize: 10, items: [{ ...occurrence, id: 'current-occurrence' }], totalItems: 1, totalPages: 1 })
    await flushPromises()
    stale.resolve({ page: 2, pageSize: 10, items: [{ ...occurrence, id: 'stale-occurrence' }], totalItems: 11, totalPages: 2 })
    await flushPromises()

    expect(wrapper.findAll('[data-occurrence]').map((item) => item.attributes('data-occurrence-id'))).toEqual(['current-occurrence'])
    expect(wrapper.text()).not.toContain('Página 2 de 2')
  })

  it('resets pagination on schedule change and retries only the current schedule', async () => {
    const firstSchedule = deferred<Awaited<ReturnType<typeof listPresenceScheduleOccurrences>>>()
    const secondSchedule = deferred<Awaited<ReturnType<typeof listPresenceScheduleOccurrences>>>()
    const retry = deferred<Awaited<ReturnType<typeof listPresenceScheduleOccurrences>>>()
    vi.mocked(listPresenceScheduleOccurrences)
      .mockReturnValueOnce(firstSchedule.promise)
      .mockReturnValueOnce(secondSchedule.promise)
      .mockReturnValueOnce(retry.promise)
    const wrapper = await mountDialog()

    await wrapper.setProps({ scheduleId: 'agenda-2', scheduleName: 'Agenda atual' })
    expect(listPresenceScheduleOccurrences).toHaveBeenLastCalledWith('agenda-2', 1, 10)
    secondSchedule.reject(new Error('current failure'))
    await flushPromises()
    firstSchedule.resolve({ page: 2, pageSize: 10, items: [{ ...occurrence, id: 'stale-occurrence' }], totalItems: 11, totalPages: 2 })
    await flushPromises()

    expect(wrapper.text()).toContain('Não foi possível carregar o histórico.')
    expect(wrapper.find('[data-occurrence-id="stale-occurrence"]').exists()).toBe(false)
    await wrapper.get('[data-history-retry]').trigger('click')
    expect(listPresenceScheduleOccurrences).toHaveBeenLastCalledWith('agenda-2', 1, 10)
    retry.resolve({ page: 1, pageSize: 10, items: [{ ...occurrence, id: 'agenda-2-occurrence' }], totalItems: 1, totalPages: 1 })
    await flushPromises()

    expect(wrapper.findAll('[data-occurrence]').map((item) => item.attributes('data-occurrence-id'))).toEqual(['agenda-2-occurrence'])
    expect(wrapper.get('[data-history-previous]').attributes('disabled')).toBeDefined()
  })

  it('formats occurrence instants in America/Sao_Paulo even under another process timezone', async () => {
    const originalTimezone = process.env.TZ
    process.env.TZ = 'Pacific/Auckland'
    vi.mocked(listPresenceScheduleOccurrences).mockResolvedValue({
      page: 1,
      pageSize: 10,
      items: [{
        ...occurrence,
        publicacaoPrevistaEm: '2026-07-18T02:30:00Z',
        encerramentoPrevistoEm: '2026-07-18T03:30:00Z',
      }],
      totalItems: 1,
      totalPages: 1,
    })

    try {
      const wrapper = await mountDialog()
      await flushPromises()
      const expectedPublication = new Intl.DateTimeFormat('pt-BR', {
        dateStyle: 'medium',
        timeStyle: 'short',
        timeZone: 'America/Sao_Paulo',
      }).format(new Date('2026-07-18T02:30:00Z'))
      expect(wrapper.text()).toContain(expectedPublication)
    } finally {
      process.env.TZ = originalTimezone
    }
  })

  it('constrains the mobile history footer and preserves interaction behavior', () => {
    expect(presenceScheduleStyles).toMatch(/\.presence-schedule-history__pagination\s*{[^}]*min-width:\s*0;[^}]*width:\s*100%;/s)
    expect(presenceScheduleStyles).toMatch(/\.presence-schedule-history-dialog\s*{[^}]*grid-template-columns:\s*minmax\(0,\s*1fr\);/s)
    expect(presenceScheduleStyles).toMatch(/\.presence-schedule-history__pagination\s*{[^}]*flex-direction:\s*row;/s)
    expect(presenceScheduleStyles).toMatch(/\.presence-schedule-dialog,\s*\.presence-schedule-history-dialog\s*{[^}]*overscroll-behavior:\s*contain;/s)
    expect(presenceScheduleStyles).toMatch(/touch-action:\s*manipulation;/)
  })

  it('closes on Escape and restores focus', async () => {
    vi.mocked(listPresenceScheduleOccurrences).mockResolvedValue({ page: 1, pageSize: 10, items: [], totalItems: 0, totalPages: 0 })
    const trigger = document.createElement('button')
    document.body.append(trigger)
    trigger.focus()
    const wrapper = await mountDialog(trigger)
    await flushPromises()
    await wrapper.get('[role="dialog"]').trigger('keydown', { key: 'Escape' })
    await wrapper.setProps({ open: false })
    await nextTick()
    await nextTick()

    const openEvents = wrapper.emitted('update:open') ?? []
    expect(openEvents[openEvents.length - 1]).toEqual([false])
    expect(document.activeElement).toBe(trigger)
  })
})
