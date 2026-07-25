// @vitest-environment happy-dom
import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'

import { i18n, setLocale } from '@/i18n'

import DraftDiscordPublicationPanel from './DraftDiscordPublicationPanel.vue'
import DraftDiscordPublicationPanelSource from './DraftDiscordPublicationPanel.vue?raw'

const publications = [
  { tipo: 'Presenca', status: 'Pendente' },
  { tipo: 'ChamadaPresenca', status: 'Falha' },
  { tipo: 'TimesDefinidos', status: 'Publicada' },
] as const

function mountPanel(overrides: Record<string, unknown> = {}) {
  return mount(DraftDiscordPublicationPanel, {
    props: { publications, canManage: true, saving: false, ...overrides },
    global: { plugins: [i18n] },
  })
}

describe('DraftDiscordPublicationPanel', () => {
  it('presents localized publication statuses in a subordinate region', () => {
    const wrapper = mountPanel()

    expect(wrapper.get('[data-discord-publications]').classes()).toContain('draft-publications--subordinate')
    expect(wrapper.text()).toContain('Presença no Discord: pendente')
    expect(wrapper.text()).toContain('Chamada no Discord: falhou')
    expect(wrapper.text()).toContain('Times no Discord: publicada')
  })

  it('uses a localized neutral fallback for missing and unknown statuses in both locales', () => {
    const values = [
      { tipo: 'Presenca', status: null },
      { tipo: 'IntegracaoLegada', status: 'EstadoLegado' },
    ]
    const wrapper = mountPanel({ publications: values })

    expect(wrapper.findAll('[data-publication-status="unknown"]')).toHaveLength(2)
    expect(wrapper.text()).toContain('Estado de publicação desconhecido')

    setLocale('en')
    const english = mountPanel({ publications: values })
    expect(english.text()).toContain('Unknown publication status')
    setLocale('pt')
  })

  it.each([
    ['Presenca', 'republish-presence'],
    ['ChamadaPresenca', 'republish-presence-cta'],
    ['TimesDefinidos', 'republish-final-teams'],
  ] as const)('emits the exact %s publication type', async (tipo, testId) => {
    const wrapper = mountPanel()

    await wrapper.get(`[data-testid="${testId}"]`).trigger('click')

    expect(wrapper.emitted('republish')).toEqual([[tipo]])
  })

  it('keeps the presence CTA action restricted to recoverable statuses', () => {
    const wrapper = mountPanel({ publications: publications.map((publication) => publication.tipo === 'ChamadaPresenca' ? { ...publication, status: 'Publicada' } : publication) })

    expect(wrapper.find('[data-testid="republish-presence-cta"]').exists()).toBe(false)
  })

  it('hides actions without permission and blocks duplicate actions while saving', async () => {
    const unauthorized = mountPanel({ canManage: false })
    expect(unauthorized.findAll('button')).toHaveLength(0)

    const saving = mountPanel({ saving: true })
    const action = saving.get('[data-testid="republish-presence"]')
    expect(action.attributes('disabled')).toBeDefined()
    await action.trigger('click')
    await action.trigger('click')
    expect(saving.emitted('republish')).toBeUndefined()
  })

  it('uses secondary actions and does not import services', () => {
    const wrapper = mountPanel()

    expect(wrapper.findAll('button').every((button) => button.classes().includes('button-secondary'))).toBe(true)
    expect(DraftDiscordPublicationPanelSource).not.toMatch(/@\/services\//)
  })
})
