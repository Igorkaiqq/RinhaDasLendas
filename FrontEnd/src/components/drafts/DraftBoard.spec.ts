// @vitest-environment happy-dom
import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'

import type { Draft } from '@/types/draft'

import DraftBoard from './DraftBoard.vue'

const draft: Draft = {
  id: 'draft-1',
  nome: 'Rinha',
  observacoes: null,
  status: 'Aberto',
  tamanhoTime: 5,
  criterioCapitaes: 'Manual',
  criterioPrimeiroPick: 'Manual',
  proximoTime: 'TimeA',
  capitaoTimeA: { id: 'player-1', nomeExibicao: 'Capitao A' },
  capitaoTimeB: { id: 'player-2', nomeExibicao: 'Capitao B' },
  timeA: [{ jogadorId: 'player-1', nomeExibicao: 'Capitao A', capitao: true }],
  timeB: [{ jogadorId: 'player-2', nomeExibicao: 'Capitao B', capitao: true }],
  disponiveis: [{ id: 'player-3', nomeExibicao: 'Jogador Livre' }],
  escolhas: [],
  dataCadastro: '2026-06-19T00:00:00Z',
  dataAtualizacao: '2026-06-19T00:00:00Z',
}

describe('DraftBoard', () => {
  it('renders captains and emits pick', async () => {
    const wrapper = mount(DraftBoard, { props: { draft, picking: false } })

    await wrapper.find('.draft-player-grid button').trigger('click')

    expect(wrapper.text()).toContain('Capitao A')
    expect(wrapper.text()).toContain('Capitao B')
    expect(wrapper.emitted('pick')?.[0]).toEqual([draft.disponiveis[0]])
  })
})
