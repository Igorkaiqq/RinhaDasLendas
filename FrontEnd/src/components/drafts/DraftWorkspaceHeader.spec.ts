// @vitest-environment happy-dom
import { mount } from '@vue/test-utils'
import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { describe, expect, it } from 'vitest'

import { i18n, setLocale } from '@/i18n'
import type { DraftMontagem } from '@/types/draftMontagem'

import DraftWorkspaceHeader from './DraftWorkspaceHeader.vue'
const MainCss = readFileSync(resolve(process.cwd(), 'src/styles/main.css'), 'utf8')

const draft = {
  id: 'draft-1',
  nome: 'Rinha de domingo com um nome deliberadamente longo para preservar todo o contexto',
  status: 'CapitaesDefinidos',
  modo: 'Manual',
  tamanhoEquipe: 5,
  quantidadeTimes: 2,
  quantidadeReservas: 1,
  criterioCapitaes: 'Manual',
  duracaoTurnoSegundos: 60,
  horarioEncerramentoPresenca: '2026-07-26T18:00:00Z',
  presencaContinuadaManualmente: false,
  presencas: [],
  times: [],
  livres: [],
  reservas: [],
  escolhas: [],
  substituicoes: [],
  publicacoesDiscord: [],
  dataCadastro: '2026-07-25T12:00:00Z',
  dataAtualizacao: '2026-07-25T12:00:00Z',
} satisfies DraftMontagem

describe('DraftWorkspaceHeader', () => {
  it('keeps identity, date, status, counts, and progress together', () => {
    const wrapper = mount(DraftWorkspaceHeader, {
      props: { draft, confirmedCount: 7, finalTeamsPublicationStatus: 'Pendente' },
      global: { plugins: [i18n] },
    })

    expect(wrapper.get('h1').text()).toBe(draft.nome)
    expect(wrapper.get('[data-workspace-date]').text()).toContain('26/07/2026')
    expect(wrapper.get('[data-workspace-status]').text()).toBe('Capitães definidos')
    expect(wrapper.get('[data-workspace-counts]').text()).toContain('7 confirmados')
    expect(wrapper.get('[data-workspace-counts]').text()).toContain('2 times')
    expect(wrapper.get('[data-workspace-counts]').text()).toContain('1 reserva')
    expect(wrapper.getComponent({ name: 'DraftStateRail' }).props()).toMatchObject({
      status: 'CapitaesDefinidos',
      publicationStatus: 'Pendente',
    })
  })

  it('uses localized fallbacks for a missing date and unknown status', () => {
    const wrapper = mount(DraftWorkspaceHeader, {
      props: {
        draft: { ...draft, status: 'StatusLegado', horarioEncerramentoPresenca: null },
        confirmedCount: 0,
        finalTeamsPublicationStatus: null,
      },
      global: { plugins: [i18n] },
    })

    expect(wrapper.get('[data-workspace-date]').text()).toContain('Data não informada')
    expect(wrapper.get('[data-workspace-status]').text()).toBe('Estado desconhecido')
  })

  it('renders primary, secondary, and danger actions in separate stable groups', () => {
    const wrapper = mount(DraftWorkspaceHeader, {
      props: { draft, confirmedCount: 7, finalTeamsPublicationStatus: null },
      slots: {
        'primary-action': '<button>primary</button>',
        'secondary-actions': '<button>secondary</button>',
        'danger-action': '<button>danger</button>',
      },
      global: { plugins: [i18n] },
    })

    expect(wrapper.get('[data-action-group="primary"]').text()).toBe('primary')
    expect(wrapper.get('[data-action-group="secondary"]').text()).toBe('secondary')
    expect(wrapper.get('[data-action-group="danger"]').text()).toBe('danger')
    expect(wrapper.get('[data-action-group="primary"]').findAll('button')).toHaveLength(1)
  })

  it.each([
    ['pt', 1, '1 confirmado · 1 time · 1 reserva'],
    ['pt', 2, '2 confirmados · 2 times · 2 reservas'],
    ['en', 1, '1 confirmed player · 1 team · 1 reserve'],
    ['en', 2, '2 confirmed players · 2 teams · 2 reserves'],
  ] as const)('pluralizes workspace counts in %s for count %i', (locale, count, expected) => {
    setLocale(locale)
    const wrapper = mount(DraftWorkspaceHeader, {
      props: {
        draft: { ...draft, quantidadeTimes: count, quantidadeReservas: count },
        confirmedCount: count,
        finalTeamsPublicationStatus: null,
      },
      global: { plugins: [i18n] },
    })

    expect(wrapper.get('[data-workspace-counts]').text()).toBe(expected)
    setLocale('pt')
  })

  it('orders context, progress, and clearly separated action groups for assistive reading', () => {
    const wrapper = mount(DraftWorkspaceHeader, {
      props: { draft, confirmedCount: 7, finalTeamsPublicationStatus: null },
      slots: {
        'primary-action': '<button>primary</button>',
        'secondary-actions': '<button>secondary</button>',
        'danger-action': '<button>danger</button>',
      },
      global: { plugins: [i18n] },
    })
    const header = wrapper.get('[data-testid="draft-workspace-header"]')
    const children = Array.from(header.element.children)

    expect(children[0]?.classList.contains('draft-summary')).toBe(true)
    expect(children[1]?.classList.contains('draft-state-progress')).toBe(true)
    expect(wrapper.get('.draft-state-progress > ol')).toBeTruthy()
    expect(wrapper.get('.draft-state-progress > [data-discord-integration]')).toBeTruthy()
    expect(children[2]?.classList.contains('draft-hero-actions')).toBe(true)
    expect(MainCss).toMatch(/\.drafts-page\s+\[data-action-group='primary'\][\s\S]*?var\(--color-primary\)/s)
    expect(MainCss).toMatch(/\.drafts-page\s+\[data-action-group='danger'\][\s\S]*?var\(--color-danger\)/s)
  })
})
