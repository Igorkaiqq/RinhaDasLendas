/// <reference types="node" />
// @vitest-environment happy-dom
import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { mount } from '@vue/test-utils'
import { defineComponent, h } from 'vue'
import { beforeEach, describe, expect, it, vi } from 'vitest'

import { i18n, setLocale } from '@/i18n'
import SidebarNav from './SidebarNav.vue'

const mainStyles = readFileSync(resolve('src/styles/main.css'), 'utf8')

vi.mock('vue-router', () => ({
  RouterLink: defineComponent({
    props: {
      to: {
        type: String,
        required: true,
      },
      title: {
        type: String,
        default: '',
      },
    },
    setup(props, { slots, attrs }) {
      return () => h('a', { ...attrs, href: props.to, title: props.title }, slots.default?.())
    },
  }),
  useRoute: () => ({ name: 'players' }),
}))

describe('SidebarNav', () => {
  beforeEach(() => {
    setLocale('pt')
  })

  it('renders translated navigation labels from i18n keys', () => {
    const wrapper = mount(SidebarNav, {
      global: {
        plugins: [i18n],
      },
      props: {
        collapsed: false,
        items: [
          {
            id: 'players',
            label: i18n.global.t('navigation.players'),
            icon: 'J',
            routeName: 'players',
            path: '/jogadores',
            status: 'available',
          },
        ],
      },
    })

    expect(wrapper.text()).toContain('Jogadores')
    expect(wrapper.find('.sidebar__item--active').text()).toContain('Jogadores')
    expect(wrapper.attributes('aria-label')).toBe('Navegação principal')
  })

  it('renders a localized textual badge for a new update', () => {
    const wrapper = mount(SidebarNav, {
      global: {
        plugins: [i18n],
      },
      props: {
        collapsed: false,
        items: [
          {
            id: 'updates',
            label: i18n.global.t('navigation.updates'),
            icon: 'UP',
            routeName: 'updates',
            path: '/atualizacoes',
            status: 'available',
            badge: 'new',
          },
        ],
      },
    })

    const badge = wrapper.get('.sidebar__status--new')
    const updateLink = wrapper.get('.sidebar__nav a')
    expect(badge.text()).toBe('Novo')
    expect(updateLink.text()).toContain('Atualizações')
    expect(updateLink.text()).toContain('Novo')
  })

  it('keeps only the textual new badge visible in compact and mobile navigation', () => {
    expect(mainStyles).toMatch(
      /@media \(max-width: 1024px\)[\s\S]*?\.app-shell:not\(\.app-shell--collapsed\) \.sidebar__status--new\s*\{[^}]*display:\s*inline-flex;/,
    )
    expect(mainStyles).toMatch(
      /@media \(max-width: 1024px\)[\s\S]*?\.app-shell:not\(\.app-shell--collapsed\) \.sidebar__item:has\(\.sidebar__status--new\)\s*\{[^}]*flex-direction:\s*column;/,
    )
    expect(mainStyles).toMatch(
      /@media \(max-width: 760px\)[\s\S]*?\.app-shell \.sidebar__status--new\s*\{[^}]*display:\s*inline-flex;/,
    )
    expect(mainStyles).toMatch(
      /@media \(max-width: 760px\)[\s\S]*?\.app-shell:not\(\.app-shell--collapsed\) \.sidebar__item:has\(\.sidebar__status--new\),\s*\.app-shell--collapsed \.sidebar__item:has\(\.sidebar__status--new\)\s*\{[^}]*flex-direction:\s*row;/,
    )
    expect(mainStyles).toMatch(
      /\.app-shell:not\(\.app-shell--collapsed\) \.sidebar__status,[\s\S]*?display:\s*none;/,
    )
    expect(mainStyles).toMatch(
      /@media \(max-width: 760px\)[\s\S]*?\.sidebar__status\s*\{\s*display:\s*none;/,
    )
  })
})
