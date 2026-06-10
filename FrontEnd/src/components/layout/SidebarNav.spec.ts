// @vitest-environment happy-dom
import { mount } from '@vue/test-utils'
import { defineComponent, h } from 'vue'
import { beforeEach, describe, expect, it, vi } from 'vitest'

import { i18n, setLocale } from '@/i18n'
import SidebarNav from './SidebarNav.vue'

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
    setup(props, { slots }) {
      return () => h('a', { href: props.to, title: props.title }, slots.default?.())
    },
  }),
  useRoute: () => ({ name: 'players' }),
}))

describe('SidebarNav', () => {
  beforeEach(() => {
    setLocale('pt-BR')
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
    expect(wrapper.attributes('aria-label')).toBe('Navegação principal')
  })
})
