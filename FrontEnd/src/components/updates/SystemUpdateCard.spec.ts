// @vitest-environment happy-dom
import { mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it } from 'vitest'
import { defineComponent, nextTick } from 'vue'

import { AppRoutes } from '@/constants/appRoutes'
import { SYSTEM_UPDATES } from '@/constants/systemUpdates'
import { i18n, setLocale } from '@/i18n'

import SystemUpdateCard from './SystemUpdateCard.vue'

const RouterLinkStub = defineComponent({
  props: {
    to: { type: String, required: true },
  },
  template: '<a data-router-link :href="to"><slot /></a>',
})

function mountCard(latest = true) {
  return mount(SystemUpdateCard, {
    props: { release: SYSTEM_UPDATES[0], latest },
    global: {
      plugins: [i18n],
      stubs: { RouterLink: RouterLinkStub },
    },
  })
}

describe('SystemUpdateCard', () => {
  beforeEach(() => setLocale('pt'))
  afterEach(() => setLocale('pt'))

  it('renders localized release metadata and all latest details semantically', () => {
    const wrapper = mountCard()

    expect(wrapper.get('article').classes()).toContain('system-update-card--latest')
    expect(wrapper.get('[data-slot="card-header"]').classes()).toContain(
      'system-update-card__header',
    )
    expect(wrapper.get('time').attributes('datetime')).toBe('2026-07-23')
    expect(wrapper.get('time').text()).toBe('23 de julho de 2026')
    expect(wrapper.get('h2').text()).toBe('Listas de presença agendadas')
    expect(wrapper.text()).toContain('2026.07.2')
    expect(wrapper.text()).toContain(
      'Moderadores agora podem organizar listas semanais com horários definidos',
    )
    expect(wrapper.text()).toContain('Melhoria')
    expect(wrapper.text()).toContain('Drafts')
    expect(wrapper.findAll('[data-update-detail]')).toHaveLength(5)
    expect(wrapper.text()).toContain('Agendamento semanal de presença')
  })

  it('groups every detail by category in native disclosure controls', async () => {
    const wrapper = mountCard()
    const details = wrapper.findAll('details')
    const summaries = wrapper.findAll('summary')
    const firstDetails = wrapper.get('details')
    const firstSummary = wrapper.get('summary')

    expect(details).toHaveLength(2)
    expect(summaries).toHaveLength(2)
    expect(summaries.every((summary) => summary.attributes('tabindex') === undefined)).toBe(true)
    expect(details.flatMap((group) => group.findAll('[data-update-detail]'))).toHaveLength(5)

    ;(firstSummary.element as HTMLElement).click()
    await nextTick()

    expect(firstDetails.attributes()).toHaveProperty('open')
  })

  it('renders only optional links and points each one to a known internal route', () => {
    const wrapper = mountCard()
    const expectedLinks = SYSTEM_UPDATES[0].details.filter((detail) => detail.link)
    const links = wrapper.findAll('[data-router-link]')

    expect(links).toHaveLength(expectedLinks.length)
    expect(links.map((link) => link.attributes('href')).sort()).toEqual(
      expectedLinks.map((detail) => detail.link).sort(),
    )
    expect(
      links.every((link) => Object.values(AppRoutes).some((route) => route === link.attributes('href'))),
    ).toBe(true)
  })

  it('formats dates and content with the active locale', async () => {
    setLocale('en')
    const wrapper = mountCard(false)
    await nextTick()

    expect(wrapper.get('article').classes()).not.toContain('system-update-card--latest')
    expect(wrapper.get('time').text()).toBe('July 23, 2026')
    expect(wrapper.get('h2').text()).toBe('Scheduled presence lists')
    expect(wrapper.text()).toContain('Improvement')
    expect(wrapper.text()).toContain('Drafts')
  })
})
