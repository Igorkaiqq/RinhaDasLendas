// @vitest-environment happy-dom
import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'

import { i18n } from '@/i18n'
import type { DraftMontagem, DraftMontagemParticipante, DraftMontagemStatus } from '@/types/draftMontagem'

import DraftVisualBoard from './DraftVisualBoard.vue'

function player(id: string, name: string, estado: DraftMontagemParticipante['estado'] = 'Livre'): DraftMontagemParticipante {
  return {
    jogadorId: id,
    nomeExibicao: name,
    elo: 'Ouro',
    divisao: 'I',
    status: 'Ativo',
    preferencias: [
      { rota: 'Mid', prioridade: 1, naoJogoNemLascando: false },
      { rota: 'Support', prioridade: 2, naoJogoNemLascando: false },
      { rota: 'Top', prioridade: 3, naoJogoNemLascando: false },
    ],
    estado,
    capitao: false,
    ordem: 1,
    dataCadastro: '2026-07-25T12:00:00Z',
    dataAtualizacao: '2026-07-25T12:00:00Z',
  }
}

function montagem(status: DraftMontagemStatus = 'Aberta', modo: DraftMontagem['modo'] = 'Manual'): DraftMontagem {
  const captainA = { ...player('captain-a', 'Captain A', 'Time'), capitao: true }
  const captainB = { ...player('captain-b', 'Captain B', 'Time'), capitao: true }
  return {
    id: 'draft-1',
    nome: 'Rinha ordenada',
    status,
    modo,
    tamanhoEquipe: 3,
    quantidadeTimes: 2,
    quantidadeReservas: 1,
    criterioCapitaes: 'Manual',
    duracaoTurnoSegundos: 60,
    presencaContinuadaManualmente: false,
    presencas: [],
    times: [
      { id: 'team-b', nome: 'Team B', ordem: 2, cor: 'red', capitaoId: captainB.jogadorId, jogadores: [captainB] },
      { id: 'team-a', nome: 'Team A', ordem: 1, cor: 'blue', capitaoId: captainA.jogadorId, jogadores: [captainA] },
    ],
    livres: [player('available-1', 'Available Mid')],
    reservas: [player('reserve-1', 'Reserve Support', 'Reserva')],
    escolhas: [
      {
        sequencia: 2,
        timeId: 'team-b',
        capitaoId: 'captain-b',
        jogadorId: 'picked-2',
        jogadorNome: 'Second Pick',
        tipo: 'Escolha',
        registradoEm: '2026-07-25T12:02:00Z',
      },
      {
        sequencia: 1,
        timeId: 'team-a',
        capitaoId: 'captain-a',
        jogadorId: 'picked-1',
        jogadorNome: 'First Pick',
        tipo: 'Escolha',
        registradoEm: '2026-07-25T12:01:00Z',
      },
      {
        sequencia: 3,
        timeId: 'team-a',
        capitaoId: 'captain-a',
        jogadorId: null,
        jogadorNome: null,
        tipo: 'Timeout',
        registradoEm: '2026-07-25T12:03:00Z',
      },
    ],
    substituicoes: [],
    publicacoesDiscord: [],
    dataCadastro: '2026-07-25T12:00:00Z',
    dataAtualizacao: '2026-07-25T12:02:00Z',
  }
}

function mountBoard(draft = montagem(), overrides: { canManage?: boolean; currentPlayerId?: string | null } = {}) {
  return mount(DraftVisualBoard, {
    props: {
      montagem: draft,
      saving: false,
      canManage: overrides.canManage ?? true,
      currentPlayerId: overrides.currentPlayerId ?? null,
    },
    global: { plugins: [i18n] },
  })
}

describe('DraftVisualBoard', () => {
  it('edits only its local clone', async () => {
    const draft = montagem()
    const wrapper = mountBoard(draft)

    await wrapper.find('input').setValue('Renamed locally')

    expect(draft.times.map((team) => team.nome)).toEqual(['Team B', 'Team A'])
    wrapper.unmount()
  })

  it('renders teams by order without changing save payload order', async () => {
    const draft = montagem()
    const wrapper = mountBoard(draft)

    expect(wrapper.findAll('[data-team-id]').map((team) => team.attributes('data-team-id'))).toEqual(['team-a', 'team-b'])
    expect(draft.times.map((team) => team.id)).toEqual(['team-b', 'team-a'])

    const teamBInput = wrapper.get('[data-team-id="team-b"] input')
    await teamBInput.setValue('Team B renamed')
    await wrapper.findAll('button').find((button) => button.text() === 'Salvar layout')!.trigger('click')

    expect(wrapper.emitted('save')?.[0]?.[0]).toEqual({
      times: [
        { timeId: 'team-b', nome: 'Team B renamed', capitaoId: 'captain-b', jogadores: [{ jogadorId: 'captain-b', ordem: 1, rotaContextual: 'Mid' }] },
        { timeId: 'team-a', nome: 'Team A', capitaoId: 'captain-a', jogadores: [{ jogadorId: 'captain-a', ordem: 1, rotaContextual: 'Mid' }] },
      ],
      livres: [{ jogadorId: 'available-1', ordem: 1, rotaContextual: 'Mid' }],
      reservas: [{ jogadorId: 'reserve-1', ordem: 1, rotaContextual: 'Mid' }],
    })
    expect(draft.times[0]?.nome).toBe('Team B')
    wrapper.unmount()
  })

  it('shows explicit team order, captains, pick progress, ordered sequence, and preferred routes', () => {
    const wrapper = mountBoard()

    expect(wrapper.get('[data-team-id="team-a"] [data-team-order]').text()).toContain('1')
    expect(wrapper.get('[data-team-id="team-a"] [data-team-captain]').text()).toContain('Captain A')
    expect(wrapper.get('[data-pick-progress]').text()).toContain('2 / 4')
    expect(wrapper.findAll('[data-pick-sequence]').map((pick) => pick.text())).toEqual([
      expect.stringContaining('First Pick'),
      expect.stringContaining('Second Pick'),
      expect.stringContaining('Tempo esgotado'),
    ])
    expect(wrapper.get('[data-player-id="available-1"]').text()).toContain('Mid')
    expect(wrapper.get('[data-player-id="available-1"]').text()).toContain('Support')
    wrapper.unmount()
  })

  it('keeps the active turn and available pool explicit and emits only jogadorId for a pick', async () => {
    const draft = montagem('Aberta', 'TempoReal')
    draft.turnoAtualTimeId = 'team-a'
    draft.turnoAtualCapitaoId = 'captain-a'
    draft.turnoSequencia = 3
    draft.turnoExpiraEm = new Date(Date.now() + 60_000).toISOString()
    const wrapper = mountBoard(draft, { currentPlayerId: 'captain-a' })

    expect(wrapper.get('[data-active-turn]').text()).toContain('Captain A')
    expect(wrapper.get('[data-available-pool]').text()).toContain('Jogadores Disponíveis')
    await wrapper.get('[data-player-id="available-1"] .draft-pick-action').trigger('click')

    expect(wrapper.emitted('pick')).toEqual([['available-1']])
    wrapper.unmount()
  })

  it.each(['Finalizada', 'Cancelada'] as const)('renders %s without mutable controls', (status) => {
    const wrapper = mountBoard(montagem(status))

    expect(wrapper.find('.draft-team__header input').exists()).toBe(false)
    expect(wrapper.find('[draggable="true"]').exists()).toBe(false)
    expect(wrapper.find('.draft-substitute-action').exists()).toBe(false)
    expect(wrapper.find('.draft-pick-action').exists()).toBe(false)
    expect(wrapper.findAll('button').some((button) => ['Sortear capitães', 'Iniciar tempo real', 'Salvar layout', 'Finalizar', 'Cancelar'].includes(button.text()))).toBe(false)
    expect(wrapper.text()).toContain('Team A')
    expect(wrapper.text()).toContain('Captain A')
    wrapper.unmount()
  })
})
