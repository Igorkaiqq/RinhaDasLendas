// @vitest-environment happy-dom
import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'

import TeamDeleteDialog from './TeamDeleteDialog.vue'

const team = {
  id: 'team-1',
  nome: 'Os Sem Baron',
  tag: 'OSB',
  observacoes: null,
  status: 'Ativo' as const,
  capitao: null,
  quantidadeJogadores: 1,
  membros: [{ jogadorId: 'player-1', nomeExibicao: 'Kaique', principal: true, capitao: false }],
  dataCadastro: '2026-06-19T00:00:00Z',
  dataAtualizacao: '2026-06-19T00:00:00Z',
}

describe('TeamDeleteDialog', () => {
  it('emits confirmation actions', async () => {
    const wrapper = mount(TeamDeleteDialog, { props: { open: true, team } })
    const buttons = wrapper.findAll('button')

    await buttons[0]!.trigger('click')
    await buttons[1]!.trigger('click')

    expect(wrapper.text()).toContain('Inativar Os Sem Baron?')
    expect(wrapper.emitted('cancel')).toHaveLength(1)
    expect(wrapper.emitted('confirm')).toHaveLength(1)
  })
})
