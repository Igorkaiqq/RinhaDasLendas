// @vitest-environment happy-dom
import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'

import { i18n } from '@/i18n'
import type { DraftMontagem } from '@/types/draftMontagem'

import DraftWorkspaceHeader from './DraftWorkspaceHeader.vue'

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
    expect(wrapper.get('[data-workspace-counts]').text()).toContain('1 reservas')
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
})
