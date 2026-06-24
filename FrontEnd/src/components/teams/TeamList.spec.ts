// @vitest-environment happy-dom
import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'

import { i18n } from '@/i18n'
import TeamList from './TeamList.vue'

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

describe('TeamList', () => {
  it('emits create from empty state', async () => {
    const wrapper = mount(TeamList, { global: { plugins: [i18n] }, props: { teams: [], loading: false, errors: [] } })

    await wrapper.find('button').trigger('click')

    expect(wrapper.text()).toContain('Nenhum time encontrado')
    expect(wrapper.emitted('create')).toHaveLength(1)
  })

  it('renders team cards and forwards edit events', async () => {
    const wrapper = mount(TeamList, { global: { plugins: [i18n] }, props: { teams: [team], loading: false, errors: [] } })

    await wrapper.find('button').trigger('click')

    expect(wrapper.text()).toContain('Os Sem Baron')
    expect(wrapper.emitted('edit')?.[0]).toEqual([team])
  })
})
