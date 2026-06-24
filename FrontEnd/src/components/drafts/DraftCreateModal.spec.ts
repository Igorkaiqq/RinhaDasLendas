// @vitest-environment happy-dom
import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'

import { i18n } from '@/i18n'
import { Elo, type Player } from '@/services/players'

import DraftCreateModal from './DraftCreateModal.vue'

const players: Player[] = [
  {
    id: 'player-1',
    nomeExibicao: 'Capitao A',
    status: 'Ativo',
    elo: Elo.Ouro,
    dataCadastro: '2026-06-19T00:00:00Z',
    dataAtualizacao: '2026-06-19T00:00:00Z',
    preferencias: [],
  },
  {
    id: 'player-2',
    nomeExibicao: 'Capitao B',
    status: 'Ativo',
    elo: Elo.Ouro,
    dataCadastro: '2026-06-19T00:00:00Z',
    dataAtualizacao: '2026-06-19T00:00:00Z',
    preferencias: [],
  },
]

describe('DraftCreateModal', () => {
  it('renders players in draft form', () => {
    const wrapper = mount(DraftCreateModal, { global: { plugins: [i18n] }, props: { open: true, players, saving: false, serviceErrors: [] } })

    expect(wrapper.text()).toContain('Criar Draft')
    expect(wrapper.text()).toContain('Capitao A')
    expect(wrapper.text()).toContain('Capitao B')
  })
})
