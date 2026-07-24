// @vitest-environment happy-dom
import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { nextTick } from 'vue'

import { i18n } from '@/i18n'
import * as service from '@/services/presenceSchedules'

import PresenceScheduleSection from './PresenceScheduleSection.vue'

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

  it('appends two backend-ordered pages without duplicate IDs, including paused schedules', async () => {
    vi.mocked(service.listPresenceSchedules)
      .mockResolvedValueOnce({ page: 1, pageSize: 6, items: [baseSchedule, { ...baseSchedule, id: 'agenda-b' }], totalItems: 4, totalPages: 2 })
      .mockResolvedValueOnce({ page: 2, pageSize: 6, items: [{ ...baseSchedule, id: 'agenda-b' }, { ...baseSchedule, id: 'agenda-c', status: 'Pausado', proximaExecucaoEm: null }], totalItems: 4, totalPages: 2 })
    const wrapper = await mountSection()
    await flushPromises()

    expect(wrapper.findAll('[data-schedule-id]').map((card) => card.attributes('data-schedule-id'))).toEqual(['agenda-a', 'agenda-b'])
    await wrapper.get('[data-load-more]').trigger('click')
    await flushPromises()
    expect(wrapper.findAll('[data-schedule-id]').map((card) => card.attributes('data-schedule-id'))).toEqual(['agenda-a', 'agenda-b', 'agenda-c'])
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
    reject(new Error('network'))
    await flushPromises()
    expect(wrapper.text()).toContain('Não foi possível carregar os agendamentos.')

    vi.mocked(service.listPresenceSchedules).mockResolvedValueOnce({ page: 1, pageSize: 6, items: [], totalItems: 0, totalPages: 0 })
    await wrapper.get('[data-schedule-retry]').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Nenhum agendamento criado ainda.')
    expect(wrapper.get('[data-empty-create]').text()).toBe('Criar primeiro agendamento')
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

  it('uses semantic single-column-ready cards and touch-sized actions', async () => {
    vi.mocked(service.listPresenceSchedules).mockResolvedValue({ page: 1, pageSize: 6, items: [baseSchedule], totalItems: 1, totalPages: 1 })
    const wrapper = await mountSection()
    await flushPromises()
    expect(wrapper.get('[data-schedule-list]').attributes('role')).toBe('list')
    expect(wrapper.get('[data-schedule-card]').classes()).toContain('presence-schedule-card')
    expect(wrapper.get('[data-card-actions]').classes()).toContain('presence-schedule-card__actions')
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
