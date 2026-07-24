// @vitest-environment happy-dom
import { mount } from '@vue/test-utils'
import { afterEach, describe, expect, it } from 'vitest'
import { nextTick } from 'vue'

import { i18n, setLocale } from '@/i18n'

import PresenceScheduleFormDialog from './PresenceScheduleFormDialog.vue'

async function mountDialog(props: Record<string, unknown> = {}) {
  const wrapper = mount(PresenceScheduleFormDialog, {
    attachTo: document.body,
    props: {
      open: true,
      mode: 'create',
      schedule: null,
      saving: false,
      serviceMessageCode: null,
      ...props,
    },
    global: { plugins: [i18n], stubs: { teleport: { template: '<div data-teleport-stub><slot /></div>' } } },
  })
  await nextTick()
  await nextTick()
  return wrapper
}

describe('PresenceScheduleFormDialog', () => {
  afterEach(() => {
    document.body.innerHTML = ''
    setLocale('pt')
  })

  it('exposes an accessible weekday option set and normalizes a valid payload', async () => {
    const wrapper = await mountDialog()
    const friday = wrapper.get('button[data-weekday="Sexta"]')

    expect(friday.attributes('aria-pressed')).toBe('false')
    await friday.trigger('click')
    expect(friday.attributes('aria-pressed')).toBe('true')
    await wrapper.get('#presence-schedule-name').setValue('  Sexta da Rinha  ')
    await wrapper.get('#presence-schedule-observation').setValue('  Formar times  ')
    await wrapper.get('#presence-schedule-publication').setValue('18:00')
    await wrapper.get('#presence-schedule-closing').setValue('20:00')
    await wrapper.get('form').trigger('submit')

    expect(wrapper.emitted('submit')?.[0]).toEqual([{
      nome: 'Sexta da Rinha',
      observacao: 'Formar times',
      diasSemana: ['Sexta'],
      horarioPublicacao: '18:00',
      horarioEncerramento: '20:00',
    }])
  })

  it('validates name, observation, weekdays and closing time with localized messages', async () => {
    const wrapper = await mountDialog()
    await wrapper.get('#presence-schedule-name').setValue('ab')
    await wrapper.get('#presence-schedule-observation').setValue('x'.repeat(501))
    await wrapper.get('#presence-schedule-publication').setValue('20:00')
    await wrapper.get('#presence-schedule-closing').setValue('20:00')
    await wrapper.get('form').trigger('submit')

    expect(wrapper.text()).toContain('Informe um nome entre 3 e 100 caracteres.')
    expect(wrapper.text()).toContain('A observação deve ter no máximo 500 caracteres.')
    expect(wrapper.text()).toContain('Selecione ao menos um dia da semana.')
    expect(wrapper.text()).toContain('O encerramento deve ser posterior à publicação.')
    expect(wrapper.emitted('submit')).toBeUndefined()
    expect(wrapper.get('#presence-schedule-name').attributes('aria-invalid')).toBe('true')
  })

  it('disables controls while saving and blocks duplicate submits', async () => {
    const wrapper = await mountDialog({
      saving: true,
      schedule: {
        id: 'agenda-1', nome: 'Sexta da Rinha', observacao: null, status: 'Ativo',
        diasSemana: ['Sexta'], horarioPublicacao: '18:00', horarioEncerramento: '20:00',
        proximaExecucaoEm: null, ultimaOcorrencia: null,
      },
      mode: 'edit',
    })

    expect(wrapper.get('button[type="submit"]').attributes('disabled')).toBeDefined()
    await wrapper.get('form').trigger('submit')
    await wrapper.get('form').trigger('submit')
    expect(wrapper.emitted('submit')).toBeUndefined()
  })

  it('closes with Escape and restores focus to the opener', async () => {
    const trigger = document.createElement('button')
    document.body.append(trigger)
    trigger.focus()
    const wrapper = await mountDialog({ returnFocusTo: trigger })

    expect(wrapper.get('[role="dialog"]').attributes('aria-labelledby')).toBeTruthy()
    await wrapper.get('[role="dialog"]').trigger('keydown', { key: 'Escape' })
    await wrapper.setProps({ open: false })
    await nextTick()
    await nextTick()

    const openEvents = wrapper.emitted('update:open') ?? []
    expect(openEvents[openEvents.length - 1]).toEqual([false])
    expect(document.activeElement).toBe(trigger)
  })
})
