// @vitest-environment happy-dom
import { enableAutoUnmount, mount } from '@vue/test-utils'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { nextTick } from 'vue'

import { i18n, setLocale } from '@/i18n'

import PresenceScheduleFormDialog from './PresenceScheduleFormDialog.vue'

enableAutoUnmount(afterEach)

async function mountDialog(props: Record<string, unknown> = {}, stubs: Record<string, unknown> = {}) {
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
    global: { plugins: [i18n], stubs: { teleport: { template: '<div data-teleport-stub><slot /></div>' }, ...stubs } },
  })
  await nextTick()
  await nextTick()
  return wrapper
}

describe('PresenceScheduleFormDialog', () => {
  afterEach(() => {
    document.body.innerHTML = ''
    setLocale('pt')
    vi.unstubAllGlobals()
  })

  it('exposes an accessible weekday option set and normalizes a valid payload', async () => {
    const wrapper = await mountDialog()
    const friday = wrapper.get('button[data-weekday="Sexta"]')
    const indicator = friday.get('[data-selection-indicator]')

    expect(friday.attributes('aria-pressed')).toBe('false')
    expect(indicator.attributes('data-visible')).toBe('false')
    await friday.trigger('click')
    expect(friday.attributes('aria-pressed')).toBe('true')
    expect(indicator.attributes('data-visible')).toBe('true')
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
    const name = wrapper.get('#presence-schedule-name')
    const weekdays = wrapper.get('[data-slot="toggle-group"]')
    expect(name.attributes()).toMatchObject({
      'aria-invalid': 'true',
      'aria-errormessage': 'presence-schedule-name-error',
      'aria-describedby': 'presence-schedule-name-error',
      autocomplete: 'off',
      name: 'presenceScheduleName',
    })
    expect(wrapper.get('#presence-schedule-name-error').text()).toContain('Informe um nome')
    expect(weekdays.attributes()).toMatchObject({
      'aria-invalid': 'true',
      'aria-errormessage': 'presence-schedule-weekdays-error',
    })
    expect(document.activeElement).toBe(name.element)
  })

  it('focuses the first invalid control in visual form order', async () => {
    const wrapper = await mountDialog()
    await wrapper.get('#presence-schedule-name').setValue('Nome válido')
    await wrapper.get('#presence-schedule-observation').setValue('x'.repeat(501))
    await wrapper.get('form').trigger('submit')
    await nextTick()

    expect(document.activeElement).toBe(wrapper.get('#presence-schedule-observation').element)
    expect(wrapper.get('#presence-schedule-observation').attributes('aria-describedby')).toContain('presence-schedule-observation-error')
  })

  it('focuses the first weekday option when weekdays are the first invalid field', async () => {
    const wrapper = await mountDialog({}, {
      ToggleGroup: { template: '<div data-slot="toggle-group"><slot /></div>' },
      ToggleGroupItem: { template: '<button><slot /></button>' },
    })
    const firstWeekday = wrapper.get('[data-weekday="Segunda"]')
    const focusFirstWeekday = vi.spyOn(firstWeekday.element as HTMLElement, 'focus')
    await wrapper.get('#presence-schedule-name').setValue('Nome válido')
    await wrapper.get('#presence-schedule-publication').setValue('18:00')
    await wrapper.get('#presence-schedule-closing').setValue('20:00')
    await wrapper.get('form').trigger('submit')
    await nextTick()

    expect(focusFirstWeekday).toHaveBeenCalledOnce()
    expect(document.activeElement).toBe(firstWeekday.element)
  })

  it('disables autocomplete on both time inputs', async () => {
    const wrapper = await mountDialog()
    expect(wrapper.get('#presence-schedule-publication').attributes('autocomplete')).toBe('off')
    expect(wrapper.get('#presence-schedule-closing').attributes('autocomplete')).toBe('off')
  })

  it('uses localized fallback for an unknown backend messageCode', async () => {
    const wrapper = await mountDialog({ serviceMessageCode: 'MV999' })
    expect(wrapper.get('[data-service-error]').text()).toBe('A solicitação não pôde ser concluída pelo servidor.')
    expect(wrapper.text()).not.toContain('settings.presenceSchedules.messageCodes.MV999')
  })

  it('autofocuses the name only on desktop', async () => {
    vi.stubGlobal('matchMedia', vi.fn().mockReturnValue({ matches: false }))
    const wrapper = await mountDialog()
    expect(document.activeElement).toBe(wrapper.get('#presence-schedule-name').element)
  })

  it('does not autofocus a field on mobile', async () => {
    vi.stubGlobal('matchMedia', vi.fn().mockReturnValue({ matches: true }))
    const trigger = document.createElement('button')
    document.body.append(trigger)
    trigger.focus()
    await mountDialog({ returnFocusTo: trigger })

    expect(document.activeElement).toBe(trigger)
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
