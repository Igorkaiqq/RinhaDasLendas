// @vitest-environment happy-dom
import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { nextTick } from 'vue'

import { i18n } from '@/i18n'
import { listPresenceScheduleOccurrences } from '@/services/presenceSchedules'

import PresenceScheduleOccurrenceHistoryDialog from './PresenceScheduleOccurrenceHistoryDialog.vue'

vi.mock('@/services/presenceSchedules', () => ({ listPresenceScheduleOccurrences: vi.fn() }))

const occurrence = {
  id: 'occurrence-1', dataLocal: '2026-07-18', publicacaoPrevistaEm: '2026-07-18T21:00:00Z',
  encerramentoPrevistoEm: '2026-07-18T23:00:00Z', status: 'Criada' as const,
  draftMontagemId: 'draft-1', messageCode: null,
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
  })

  it('shows loading, retryable error and empty states without closing', async () => {
    let reject!: (error: Error) => void
    vi.mocked(listPresenceScheduleOccurrences).mockImplementationOnce(() => new Promise((_resolve, rejectPromise) => { reject = rejectPromise }))
    const wrapper = await mountDialog()
    expect(wrapper.findAll('[data-history-skeleton]').length).toBeGreaterThan(0)
    reject(new Error('network'))
    await flushPromises()
    expect(wrapper.text()).toContain('Não foi possível carregar o histórico.')

    vi.mocked(listPresenceScheduleOccurrences).mockResolvedValueOnce({ page: 1, pageSize: 10, items: [], totalItems: 0, totalPages: 0 })
    await wrapper.get('[data-history-retry]').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Nenhuma execução registrada ainda.')
    expect(wrapper.find('[role="dialog"]').exists()).toBe(true)
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
