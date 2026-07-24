// @vitest-environment happy-dom
import { mount } from '@vue/test-utils'
import { afterEach, describe, expect, it } from 'vitest'
import { nextTick } from 'vue'

import { i18n } from '@/i18n'

import PresenceScheduleConfirmDialog from './PresenceScheduleConfirmDialog.vue'

async function mountDialog(action: 'pause' | 'archive', submitting = false) {
  const wrapper = mount(PresenceScheduleConfirmDialog, {
    attachTo: document.body,
    props: { open: true, action, scheduleName: 'Rinha semanal', submitting },
    global: { plugins: [i18n], stubs: { teleport: { template: '<div data-teleport-stub><slot /></div>' } } },
  })
  await nextTick()
  await nextTick()
  return wrapper
}

describe('PresenceScheduleConfirmDialog', () => {
  afterEach(() => { document.body.innerHTML = '' })

  it.each([
    ['pause', 'Drafts já criados permanecem disponíveis.', 'Pausar agendamento'],
    ['archive', 'Drafts já criados e o histórico serão preservados.', 'Arquivar agendamento'],
  ] as const)('renders contextual %s confirmation', async (action, description, confirmLabel) => {
    const wrapper = await mountDialog(action)
    expect(wrapper.text()).toContain(description)
    expect(wrapper.get('[data-confirm-action]').text()).toBe(confirmLabel)
    if (action === 'archive') expect(wrapper.get('[data-confirm-action]').attributes('data-variant')).toBe('destructive')
  })

  it('emits one confirmation and disables repeat submission', async () => {
    const wrapper = await mountDialog('pause')
    await wrapper.get('[data-confirm-action]').trigger('click')
    await wrapper.setProps({ submitting: true })
    await wrapper.get('[data-confirm-action]').trigger('click')

    expect(wrapper.emitted('confirm')).toHaveLength(1)
    expect(wrapper.get('[data-confirm-action]').attributes('disabled')).toBeDefined()
  })
})
