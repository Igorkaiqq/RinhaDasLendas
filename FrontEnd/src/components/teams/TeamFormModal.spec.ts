// @vitest-environment happy-dom
import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'

import { PlayerStatus } from '@/constants/playerStatus'
import { i18n } from '@/i18n'
import type { TeamPlayerOption } from '@/types/team'

import TeamFormModal from './TeamFormModal.vue'

const players: TeamPlayerOption[] = [
  { id: 'player-1', nomeExibicao: 'Kaique', status: PlayerStatus.Active },
  { id: 'player-2', nomeExibicao: 'Igor', status: PlayerStatus.Active },
  { id: 'player-3', nomeExibicao: 'Reserva', status: PlayerStatus.Inactive },
]

const team = {
  id: 'team-1',
  nome: 'Os Sem Baron',
  tag: 'OSB',
  observacoes: null,
  status: 'Ativo' as const,
  capitao: { id: 'player-1', nomeExibicao: 'Kaique' },
  quantidadeJogadores: 1,
  membros: [{ jogadorId: 'player-1', nomeExibicao: 'Kaique', principal: true, capitao: true }],
  dataCadastro: '2026-06-19T00:00:00Z',
  dataAtualizacao: '2026-06-19T00:00:00Z',
}

describe('TeamFormModal', () => {
  it('shows validation errors and blocks invalid submit', async () => {
    const wrapper = mount(TeamFormModal, {
      props: { open: true, mode: 'create', team: null, players, saving: false, serviceErrors: [] },
      global: { plugins: [i18n], stubs: { Teleport: true, Transition: false } },
    })

    await wrapper.find('form').trigger('submit')

    expect(wrapper.text()).toContain('Nome do time é obrigatório.')
    expect(wrapper.text()).toContain('Tag do time é obrigatória.')
    expect(wrapper.emitted('submit')).toBeUndefined()
  })

  it('submits edit payload with current captain and members', async () => {
    const wrapper = mount(TeamFormModal, {
      props: { open: true, mode: 'edit', team, players, saving: false, serviceErrors: [] },
      global: { plugins: [i18n], stubs: { Teleport: true, Transition: false } },
    })

    await wrapper.find('form').trigger('submit')

    expect(wrapper.emitted('submit')?.[0]).toEqual([
      {
        id: 'team-1',
        nome: 'Os Sem Baron',
        tag: 'OSB',
        observacoes: null,
        capitaoId: 'player-1',
        jogadoresIds: ['player-1'],
      },
    ])
  })
})
