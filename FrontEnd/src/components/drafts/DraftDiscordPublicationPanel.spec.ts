// @vitest-environment happy-dom
import { mount } from '@vue/test-utils'
import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { describe, expect, it } from 'vitest'

import { i18n, setLocale } from '@/i18n'

import DraftDiscordPublicationPanel from './DraftDiscordPublicationPanel.vue'
import DraftDiscordPublicationPanelSource from './DraftDiscordPublicationPanel.vue?raw'
const MainCss = readFileSync(resolve(process.cwd(), 'src/styles/main.css'), 'utf8')

const publications = [
  { tipo: 'Presenca', status: 'Pendente' },
  { tipo: 'ChamadaPresenca', status: 'Falha' },
  { tipo: 'TimesDefinidos', status: 'Publicada' },
] as const

function mountPanel(overrides: Record<string, unknown> = {}) {
  return mount(DraftDiscordPublicationPanel, {
    props: { publications, republishableTypes: ['Presenca', 'ChamadaPresenca', 'TimesDefinidos'], saving: false, ...overrides },
    global: { plugins: [i18n] },
  })
}

describe('DraftDiscordPublicationPanel', () => {
  it('renders a normalized empty matrix with legacy republish actions', () => {
    const wrapper = mountPanel({
      publications: [
        { tipo: 'Presenca', status: null },
        { tipo: 'ChamadaPresenca', status: null },
        { tipo: 'TimesDefinidos', status: null },
      ],
      republishableTypes: ['Presenca', 'TimesDefinidos'],
    })

    expect(wrapper.findAll('[data-publication-type]')).toHaveLength(3)
    expect(wrapper.findAll('[data-publication-status="unknown"]')).toHaveLength(3)
    expect(wrapper.find('[data-testid="republish-presence"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="republish-presence-cta"]').exists()).toBe(false)
    expect(wrapper.find('[data-testid="republish-final-teams"]').exists()).toBe(true)
  })

  it('renders a normalized partial matrix without replacing known statuses', () => {
    const wrapper = mountPanel({ publications: [
      { tipo: 'Presenca', status: null },
      { tipo: 'ChamadaPresenca', status: 'Falha' },
      { tipo: 'TimesDefinidos', status: null },
    ] })

    expect(wrapper.findAll('[data-publication-type]')).toHaveLength(3)
    expect(wrapper.get('[data-publication-type="Presenca"] [data-publication-status]').attributes('data-publication-status')).toBe('unknown')
    expect(wrapper.get('[data-publication-type="ChamadaPresenca"] [data-publication-status]').attributes('data-publication-status')).toBe('Falha')
    expect(wrapper.get('[data-publication-type="TimesDefinidos"] [data-publication-status]').attributes('data-publication-status')).toBe('unknown')
    expect(wrapper.find('[data-testid="republish-presence"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="republish-presence-cta"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="republish-final-teams"]').exists()).toBe(true)
  })

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

    expect(wrapper.findAll('[data-publication-status="unknown"]').length).toBeGreaterThanOrEqual(2)
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

    expect(wrapper.emitted('republish')).toEqual([[{
      publicationType: tipo,
      publicationStatus: publications.find((publication) => publication.tipo === tipo)!.status,
    }]])
  })

  it('keeps the presence CTA action restricted to recoverable statuses', () => {
    const wrapper = mountPanel({
      publications: publications.map((publication) => publication.tipo === 'ChamadaPresenca' ? { ...publication, status: 'Publicada' } : publication),
      republishableTypes: ['Presenca', 'TimesDefinidos'],
    })

    expect(wrapper.find('[data-testid="republish-presence-cta"]').exists()).toBe(false)
  })

  it('renders only parent-provided capabilities and blocks duplicate actions while saving', async () => {
    const unauthorized = mountPanel({ republishableTypes: [] })
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
    expect(DraftDiscordPublicationPanelSource).not.toContain('normalizedPublications')
    expect(DraftDiscordPublicationPanelSource).not.toContain('recoverableStatuses')
    expect(DraftDiscordPublicationPanelSource).not.toContain('canManage')
  })

  it('keeps Discord subordinate with textual statuses and semantic visual variants', () => {
    const wrapper = mountPanel()
    const region = wrapper.get('[data-discord-publications]')

    expect(region.attributes('aria-labelledby')).toBe('draft-publications-title')
    expect(region.findAll('[data-publication-status]').map((status) => status.text())).toEqual([
      expect.stringContaining('pendente'),
      expect.stringContaining('falhou'),
      expect.stringContaining('publicada'),
    ])
    expect(MainCss).toMatch(/\.drafts-page\s+\.draft-publications\s+\[data-publication-status='Publicada'\][\s\S]*?var\(--color-success\)/s)
    expect(MainCss).toMatch(/\.drafts-page\s+\.draft-publications\s+\[data-publication-status='Falha'\][\s\S]*?var\(--color-danger\)/s)
    expect(MainCss).toMatch(/\.drafts-page\s+\.draft-publications\s+\[data-publication-status='Pendente'\][\s\S]*?var\(--color-warning\)/s)
  })
})
