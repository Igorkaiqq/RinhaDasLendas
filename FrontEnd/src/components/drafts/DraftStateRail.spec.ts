// @vitest-environment happy-dom
import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'

import DraftStateRail from './DraftStateRail.vue'

describe('DraftStateRail', () => {
  it('marks the current draft status as active', () => {
    const wrapper = mount(DraftStateRail, {
      props: { status: 'CapitaesDefinidos', publicationStatus: 'Pendente' },
      global: { mocks: { $t: (key: string) => key } },
    })

    expect(wrapper.find('[data-state="active"]').text()).toContain('drafts.rail.captains')
  })
})
