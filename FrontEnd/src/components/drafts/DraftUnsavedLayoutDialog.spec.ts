// @vitest-environment happy-dom
import { DOMWrapper, mount } from '@vue/test-utils'
import { nextTick } from 'vue'
import { afterEach, describe, expect, it } from 'vitest'

import { i18n, setLocale } from '@/i18n'

import DraftUnsavedLayoutDialog from './DraftUnsavedLayoutDialog.vue'

function mountDialog(intent: 'remote-update' | 'switch-draft' | 'route-leave' | 'draft-removed' | 'archive' = 'remote-update') {
  return mount(DraftUnsavedLayoutDialog, {
    attachTo: document.body,
    props: { open: true, intent },
    global: { plugins: [i18n] },
  })
}

function dialogBody() {
  return new DOMWrapper(document.body)
}

describe('DraftUnsavedLayoutDialog', () => {
  afterEach(() => {
    document.body.innerHTML = ''
    setLocale('pt')
  })

  it('renders one titled dialog and focuses continue editing first', async () => {
    const wrapper = mountDialog()
    await new Promise((resolve) => setTimeout(resolve))

    expect(document.querySelectorAll('[role="dialog"]')).toHaveLength(1)
    expect(dialogBody().get('[role="dialog"]').attributes('aria-describedby')).toBeTruthy()
    expect(document.activeElement).toBe(dialogBody().get('[data-testid="keep-editing"]').element)
    wrapper.unmount()
  })

  it('keeps editing on Escape without emitting discard', async () => {
    const wrapper = mountDialog('route-leave')
    await new Promise((resolve) => setTimeout(resolve))

    await dialogBody().get('[role="dialog"]').trigger('keydown', { key: 'Escape' })
    await nextTick()

    expect(wrapper.emitted('keep-editing')).toEqual([[]])
    expect(wrapper.emitted('discard')).toBeUndefined()
    wrapper.unmount()
  })

  it('emits exactly the explicit continue or destructive discard decision', async () => {
    const keep = mountDialog('switch-draft')
    await new Promise((resolve) => setTimeout(resolve))
    await dialogBody().get('[data-testid="keep-editing"]').trigger('click')
    expect(keep.emitted('keep-editing')).toEqual([[]])
    keep.unmount()

    const discard = mountDialog('archive')
    await new Promise((resolve) => setTimeout(resolve))
    await dialogBody().get('[data-testid="discard-layout"]').trigger('click')
    expect(discard.emitted('discard')).toEqual([[]])
    discard.unmount()
  })

  it('uses synchronized English copy for a remote update', async () => {
    setLocale('en')
    const wrapper = mountDialog()
    await new Promise((resolve) => setTimeout(resolve))

    expect(dialogBody().text()).toContain('A newer draft version is available')
    expect(dialogBody().text()).toContain('Keep editing')
    expect(dialogBody().text()).toContain('Discard changes')
    wrapper.unmount()
  })

  it('traps Tab and Shift+Tab between the real dialog actions', async () => {
    const wrapper = mountDialog()
    await new Promise((resolve) => setTimeout(resolve))
    const keep = dialogBody().get('[data-testid="keep-editing"]')
    const discard = dialogBody().get('[data-testid="discard-layout"]')

    ;(discard.element as HTMLButtonElement).focus()
    await discard.trigger('keydown', { key: 'Tab' })
    expect(document.activeElement).toBe(keep.element)

    ;(keep.element as HTMLButtonElement).focus()
    await keep.trigger('keydown', { key: 'Tab', shiftKey: true })
    expect(document.activeElement).toBe(discard.element)
    wrapper.unmount()
  })
})
