// @vitest-environment happy-dom
import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'

import EmptyDescription from './EmptyDescription.vue'

describe('EmptyDescription', () => {
  it('merges the class prop into the rendered description', () => {
    const wrapper = mount(EmptyDescription, {
      props: { class: 'custom-empty-description' },
      slots: { default: 'No data' },
    })

    expect(wrapper.classes()).toContain('custom-empty-description')
  })
})
