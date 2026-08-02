// @vitest-environment happy-dom
import { mount } from '@vue/test-utils'
import { nextTick } from 'vue'
import { afterEach, describe, expect, it } from 'vitest'

import { i18n, setLocale } from '@/i18n'

import DraftUnsavedLayoutDialog from './DraftUnsavedLayoutDialog.vue'

function mountDialog(intent: 'remote-update' | 'switch-draft' | 'route-leave' | 'draft-removed' | 'archive' = 'remote-update') {
  return mount(DraftUnsavedLayoutDialog, {
    attachTo: document.body,
    props: { open: true, intent },
    global: {
      plugins: [i18n],
      stubs: { teleport: { template: '<div data-teleport-stub><slot /></div>' } },
    },
  })
}

describe('DraftUnsavedLayoutDialog', () => {
  afterEach(() => setLocale('pt'))

  it('renders one titled dialog and focuses continue editing first', async () => {
    const wrapper = mountDialog()
    await new Promise((resolve) => setTimeout(resolve))

    expect(document.querySelectorAll('[role="dialog"]')).toHaveLength(1)
    expect(wrapper.get('[role="dialog"]').attributes('aria-describedby')).toBeTruthy()
    expect(document.activeElement).toBe(wrapper.get('[data-testid="keep-editing"]').element)
    wrapper.unmount()
  })

  it('keeps editing on Escape without emitting discard', async () => {
    const wrapper = mountDialog('route-leave')

    await wrapper.get('[role="dialog"]').trigger('keydown', { key: 'Escape' })
    await nextTick()

    expect(wrapper.emitted('keep-editing')).toEqual([[]])
    expect(wrapper.emitted('discard')).toBeUndefined()
    wrapper.unmount()
  })

  it('emits exactly the explicit continue or destructive discard decision', async () => {
    const keep = mountDialog('switch-draft')
    await keep.get('[data-testid="keep-editing"]').trigger('click')
    expect(keep.emitted('keep-editing')).toEqual([[]])
    keep.unmount()

    const discard = mountDialog('archive')
    await discard.get('[data-testid="discard-layout"]').trigger('click')
    expect(discard.emitted('discard')).toEqual([[]])
    discard.unmount()
  })

  it('uses synchronized English copy for a remote update', async () => {
    setLocale('en')
    const wrapper = mountDialog()

    expect(wrapper.text()).toContain('A newer draft version is available')
    expect(wrapper.text()).toContain('Keep editing')
    expect(wrapper.text()).toContain('Discard changes')
    wrapper.unmount()
  })
})
