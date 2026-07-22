// @vitest-environment happy-dom
import { mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it } from 'vitest'
import { defineComponent, nextTick } from 'vue'

import { SYSTEM_UPDATES } from '@/constants/systemUpdates'
import { i18n, setLocale } from '@/i18n'

import SystemUpdatesView from './SystemUpdatesView.vue'

const PageFrameStub = defineComponent({
  template: '<main><slot /></main>',
})

const PageHeaderStub = defineComponent({
  props: {
    eyebrow: String,
    title: { type: String, required: true },
    description: String,
  },
  template:
    '<header><p v-if="eyebrow">{{ eyebrow }}</p><h1>{{ title }}</h1><p v-if="description">{{ description }}</p></header>',
})

const RouterLinkStub = defineComponent({
  props: {
    to: { type: String, required: true },
  },
  template: '<a data-router-link :href="to"><slot /></a>',
})

function mountView() {
  return mount(SystemUpdatesView, {
    global: {
      plugins: [i18n],
      stubs: {
        PageFrame: PageFrameStub,
        PageHeader: PageHeaderStub,
        RouterLink: RouterLinkStub,
      },
    },
  })
}

describe('SystemUpdatesView', () => {
  beforeEach(() => setLocale('pt'))
  afterEach(() => setLocale('pt'))

  it('renders the latest release hero and all releases in chronological order', () => {
    const wrapper = mountView()
    const hero = wrapper.get('[data-latest-update]')
    const releases = wrapper.findAll('[data-system-update]')

    expect(wrapper.get('h1').text()).toBe('Atualizações do sistema')
    expect(hero.text()).toContain('2026.07.1')
    expect(hero.text()).toContain('Drafts e Discord mais confiáveis')
    expect(hero.get('[data-latest-summary]').text()).toBe(
      'A operação de drafts ganhou acesso direto, confirmações mais claras, publicações recuperáveis e presença sincronizada, com reforços de segurança e estabilidade.',
    )
    expect(hero.get('[data-latest-categories]').text()).toContain('Correção')
    expect(hero.get('[data-latest-categories]').text()).toContain('Segurança')
    expect(hero.get('[data-latest-areas]').text()).toContain('Jogadores')
    expect(hero.get('[data-latest-areas]').text()).toContain('Discord')
    expect(hero.get('time').attributes('datetime')).toBe('2026-07-22')
    expect(releases).toHaveLength(8)
    expect(releases.map((release) => release.attributes('id'))).toEqual(
      SYSTEM_UPDATES.map((release) => `update-${release.id}`),
    )
    expect(releases[0]!.findAll('[data-update-detail]')).toHaveLength(15)
  })

  it('groups the semantic timeline and links every indexed version to its release', () => {
    const wrapper = mountView()
    const hero = wrapper.get('[data-latest-update]')
    const filters = wrapper.get('section[aria-label="Filtrar por categoria"]')
    const timeline = wrapper.get('ol.system-updates-timeline')
    const groups = timeline.findAll(':scope > [data-update-group]')
    const index = wrapper.get('nav.system-updates-index')

    expect(timeline.attributes('aria-label')).toBe(
      'Linha do tempo de atualizações',
    )
    expect(hero.classes()).toContain('system-updates-hero')
    expect(filters.classes()).toContain('system-updates-filters')
    expect(timeline.attributes('role')).toBe('list')
    expect(groups).toHaveLength(2)
    expect(
      groups.every(
        (group) => group.get(':scope > ol').attributes('role') === 'list',
      ),
    ).toBe(true)
    expect(
      groups.map((group) => group.attributes('data-update-group')),
    ).toEqual(['2026-07', '2026-06'])
    expect(index.attributes('aria-label')).toBe('Índice de versões')
    expect(index.findAll('a').map((link) => link.attributes('href'))).toEqual(
      SYSTEM_UPDATES.map((release) => `#update-${release.id}`),
    )
    expect(wrapper.findAll('li[data-system-update]')).toHaveLength(8)
  })

  it('combines localized search and category filters and clears them', async () => {
    const wrapper = mountView()
    const search = wrapper.get('input[type="search"]')
    const fixChip = wrapper.get('[data-category="fix"]')

    expect(wrapper.get('label[for="system-updates-search"]').text()).toBe(
      'Buscar atualizações',
    )
    expect(fixChip.attributes('aria-pressed')).toBe('false')
    expect(wrapper.get('[data-result-count]').text()).toBe('8 atualizações')

    await search.setValue('Discord')
    await fixChip.trigger('click')

    expect(fixChip.attributes('aria-pressed')).toBe('true')
    expect(wrapper.findAll('[data-system-update]').length).toBeGreaterThan(0)
    expect(
      wrapper
        .findAll('[data-system-update]')
        .every((release) =>
          release.text().toLocaleLowerCase().includes('discord'),
        ),
    ).toBe(true)
    expect(
      wrapper
        .findAll('[data-system-update]')
        .every((release) =>
          release.attributes('data-categories')?.split(' ').includes('fix'),
        ),
    ).toBe(true)

    await wrapper.get('[data-clear-filters]').trigger('click')

    expect((search.element as HTMLInputElement).value).toBe('')
    expect(fixChip.attributes('aria-pressed')).toBe('false')
    expect(wrapper.findAll('[data-system-update]')).toHaveLength(8)
  })

  it('recalculates localized search results when the active locale changes', async () => {
    const wrapper = mountView()
    const search = wrapper.get('input[type="search"]')

    await search.setValue('Fundação')
    expect(wrapper.findAll('[data-system-update]')).toHaveLength(1)

    setLocale('en')
    await nextTick()

    expect(wrapper.get('h1').text()).toBe('System updates')
    expect(wrapper.findAll('[data-system-update]')).toHaveLength(0)
    expect(wrapper.get('[data-result-count]').text()).toBe('0 updates')
    expect(wrapper.text()).toContain('No updates found')
  })

  it('uses the localized singular result count in both locales', async () => {
    const wrapper = mountView()
    const search = wrapper.get('input[type="search"]')

    await search.setValue('Fundação')

    expect(wrapper.findAll('[data-system-update]')).toHaveLength(1)
    expect(wrapper.get('[data-result-count]').text()).toBe('1 atualização')

    setLocale('en')
    await search.setValue('Portuguese and English interface')
    await nextTick()

    expect(wrapper.findAll('[data-system-update]')).toHaveLength(1)
    expect(wrapper.get('[data-result-count]').text()).toBe('1 update')
  })

  it('shows localized empty states that restore the complete timeline', async () => {
    const wrapper = mountView()

    await wrapper
      .get('input[type="search"]')
      .setValue('conteudo inexistente 999')

    expect(wrapper.get('[data-result-count]').text()).toBe('0 atualizações')
    expect(wrapper.text()).toContain('Nenhuma atualização encontrada')
    expect(wrapper.find('ol.system-updates-timeline').exists()).toBe(false)

    await wrapper.get('[data-clear-filters]').trigger('click')

    expect(wrapper.findAll('[data-system-update]')).toHaveLength(8)
  })
})
