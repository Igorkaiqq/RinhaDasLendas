// @vitest-environment happy-dom
import { mount } from '@vue/test-utils'
import { afterEach, describe, expect, it } from 'vitest'

import { i18n, setLocale } from '@/i18n'

import DialogFooter from './DialogFooter.vue'

function mountFooter(showCloseButton: boolean) {
  return mount(DialogFooter, {
    props: { showCloseButton },
    global: {
      plugins: [i18n],
      stubs: {
        DialogClose: { template: '<div data-slot="dialog-close"><slot /></div>' },
      },
    },
  })
}

describe('DialogFooter', () => {
  afterEach(() => setLocale('pt'))

  it.each([
    ['pt', 'Fechar'],
    ['en', 'Close'],
  ] as const)('renders the localized close button in %s', (locale, label) => {
    setLocale(locale)

    const wrapper = mountFooter(true)

    expect(wrapper.get('button').text()).toBe(label)
  })

  it('does not render the close button when it is disabled', () => {
    const wrapper = mountFooter(false)

    expect(wrapper.find('button').exists()).toBe(false)
  })
})
