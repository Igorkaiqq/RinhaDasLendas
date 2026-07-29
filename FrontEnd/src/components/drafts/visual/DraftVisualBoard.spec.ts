// @vitest-environment happy-dom
import { mount } from '@vue/test-utils'
import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { nextTick } from 'vue'
import { afterEach, describe, expect, it, vi } from 'vitest'

import { i18n, setLocale } from '@/i18n'
import { Select } from '@/components/ui/select'
import type { DraftMontagem, DraftMontagemParticipante, DraftMontagemStatus } from '@/types/draftMontagem'

import DraftVisualBoard from './DraftVisualBoard.vue'
const MainCss = readFileSync(resolve(process.cwd(), 'src/styles/main.css'), 'utf8')

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
    cicloVersao: 'Legado',
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
    arquivado: false,
    versaoEstado: 1,
    publicacoesDiscord: [],
    dataCadastro: '2026-07-25T12:00:00Z',
    dataAtualizacao: '2026-07-25T12:02:00Z',
  }
}

function largeMontagem(): DraftMontagem {
  const draft = montagem()
  const teams = Array.from({ length: 10 }, (_, index) => ({
    id: `team-${index + 1}`,
    nome: `Time ${index + 1}`,
    ordem: index + 1,
    cor: index % 2 === 0 ? 'blue' : 'red',
    capitaoId: null,
    jogadores: [],
  }))
  const escolhas = Array.from({ length: 40 }, (_, index) => {
    const round = Math.floor(index / teams.length)
    const position = index % teams.length
    const teamIndex = round % 2 === 0 ? position : teams.length - position - 1

    return {
      sequencia: index + 1,
      timeId: teams[teamIndex]!.id,
      capitaoId: `captain-${teamIndex + 1}`,
      jogadorId: `player-${index + 1}`,
      jogadorNome: `Jogador ${index + 1} com nome competitivo longo`,
      tipo: 'Escolha' as const,
      registradoEm: `2026-07-25T13:${String(index).padStart(2, '0')}:00Z`,
    }
  })

  return {
    ...draft,
    tamanhoEquipe: 5,
    quantidadeTimes: teams.length,
    times: teams,
    escolhas,
  }
}

function mountBoard(draft = montagem(), overrides: { canManage?: boolean; currentPlayerId?: string | null; canCurrentUserPick?: boolean | null; serverClockOffsetMs?: number; eligibleCaptainIds?: string[]; attachTo?: Element } = {}) {
  return mount(DraftVisualBoard, {
    attachTo: overrides.attachTo,
    props: {
      montagem: draft,
      saving: false,
      canManage: overrides.canManage ?? true,
      currentPlayerId: overrides.currentPlayerId ?? null,
      canCurrentUserPick: overrides.canCurrentUserPick,
      serverClockOffsetMs: overrides.serverClockOffsetMs,
      eligibleCaptainIds: overrides.eligibleCaptainIds ?? [],
    },
    global: { plugins: [i18n], stubs: { teleport: { template: '<div data-teleport-stub><slot /></div>' } } },
  })
}

interface PickDenialScenario {
  canCurrentUserPick: boolean
  teamId?: string
  captainId?: string
  expired?: boolean
}

describe('DraftVisualBoard', () => {
  afterEach(() => setLocale('pt'))

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

  it('keeps an open manual board free from captain, order, realtime, and pick-history controls', async () => {
    const wrapper = mountBoard({ ...montagem(), cicloVersao: 'ModoPosPresenca' })

    expect(wrapper.find('[data-team-captain]').exists()).toBe(false)
    expect(wrapper.find('[data-team-order]').exists()).toBe(false)
    expect(wrapper.find('.draft-pick-overview').exists()).toBe(false)
    expect(wrapper.findAll('button').some((button) => ['Sortear capitães', 'Iniciar tempo real'].includes(button.text()))).toBe(false)
    expect(wrapper.get('[data-stage-primary-action]').attributes('disabled')).toBeDefined()

    await wrapper.get('[data-team-id="team-a"] input').setValue('Manual A')
    await wrapper.findAll('button').find((button) => button.text() === 'Salvar layout')!.trigger('click')
    expect((wrapper.emitted('save')?.[0]?.[0] as { times: Array<{ capitaoId: string | null }> }).times.every((time) => time.capitaoId === null)).toBe(true)
  })

  it.each(['Finalizada', 'Cancelada'] as const)('keeps a terminal v2 manual board captain-free and read-only when %s', (status) => {
    const wrapper = mountBoard({ ...montagem(status), cicloVersao: 'ModoPosPresenca' })

    expect(wrapper.find('[data-team-captain]').exists()).toBe(false)
    expect(wrapper.find('[data-team-order]').exists()).toBe(false)
    expect(wrapper.find('.draft-slot__captain').exists()).toBe(false)
    expect(wrapper.find('.draft-pick-overview').exists()).toBe(false)
    expect(wrapper.find('.draft-team__header input').exists()).toBe(false)
    expect(wrapper.find('[draggable="true"]').exists()).toBe(false)
    expect(wrapper.find('[data-move-destination]').exists()).toBe(false)
    wrapper.unmount()
  })

  it('enables manual finalization only for a complete server-shaped layout', () => {
    const draft = montagem()
    draft.cicloVersao = 'ModoPosPresenca'
    draft.times.forEach((team) => {
      team.capitaoId = null
      team.jogadores = [
        player(`${team.id}-1`, `${team.nome} 1`, 'Time'),
        player(`${team.id}-2`, `${team.nome} 2`, 'Time'),
        player(`${team.id}-3`, `${team.nome} 3`, 'Time'),
      ]
    })
    draft.livres = []

    const wrapper = mountBoard(draft)

    expect(wrapper.get('[data-stage-primary-action]').attributes('disabled')).toBeUndefined()
  })

  it('offers explicit realtime start only after realtime order is defined', async () => {
    const draft = montagem('OrdemDefinida', 'TempoReal')
    draft.cicloVersao = 'ModoPosPresenca'
    const wrapper = mountBoard(draft)

    await wrapper.get('[data-testid="start-realtime"]').trigger('click')

    expect(wrapper.emitted('startRealtime')).toEqual([[]])
  })

  it('shows explicit team order, captains, pick progress, ordered sequence, and preferred routes', () => {
    const wrapper = mountBoard()

    expect(wrapper.get('[data-team-id="team-a"] [data-team-order]').text()).toContain('1')
    expect(wrapper.get('[data-team-id="team-a"] [data-team-captain]').text()).toContain('Captain A')
    expect(wrapper.get('[data-pick-progress]').text()).toContain('2 / 4')
    const picks = wrapper.findAll('[data-pick-sequence]')

    expect(picks).toHaveLength(3)
    expect(picks[0]!.get('[data-pick-sequence-number]').text()).toBe('#1')
    expect(picks[0]!.get('[data-pick-player]').text()).toBe('First Pick')
    expect(picks[0]!.get('[data-pick-team-order]').text()).toBe('Team A · 1ª escolha')
    expect(picks[1]!.get('[data-pick-sequence-number]').text()).toBe('#2')
    expect(picks[1]!.get('[data-pick-team-order]').text()).toBe('Team B · 1ª escolha')
    expect(picks[2]!.get('[data-pick-player]').text()).toBe('Tempo esgotado')
    expect(picks[2]!.get('[data-pick-team-order]').text()).toBe('Team A · 2ª escolha')
    expect(wrapper.get('[data-player-id="available-1"]').text()).toContain('Mid')
    expect(wrapper.get('[data-player-id="available-1"]').text()).toContain('Support')
    wrapper.unmount()
  })

  it('keeps tied choices stable and localizes a missing team without breaking team ordinals', async () => {
    const draft = montagem()
    draft.escolhas = [
      ...draft.escolhas,
      {
        sequencia: 3,
        timeId: 'missing-team',
        capitaoId: 'missing-captain',
        jogadorId: 'picked-missing',
        jogadorNome: 'Missing Team Pick',
        tipo: 'Escolha',
        registradoEm: '2026-07-25T12:03:30Z',
      },
    ]
    const wrapper = mountBoard(draft)
    const picks = wrapper.findAll('[data-pick-sequence]')

    expect(picks.map((pick) => pick.get('[data-pick-player]').text())).toEqual([
      'First Pick',
      'Second Pick',
      'Tempo esgotado',
      'Missing Team Pick',
    ])
    expect(picks[2]!.get('[data-pick-team-order]').text()).toBe('Team A · 2ª escolha')
    expect(picks[3]!.get('[data-pick-team-order]').text()).toBe('Time não encontrado · 1ª escolha')

    await setLocale('en')
    await nextTick()
    expect(picks[0]!.get('[data-pick-team-order]').text()).toBe('Team A · pick 1')
    expect(picks[3]!.get('[data-pick-team-order]').text()).toBe('Unknown team · pick 1')
    wrapper.unmount()
  })

  it('renders every choice for ten or more teams with snake ordinals and unbounded sequence digits', () => {
    const draft = largeMontagem()
    draft.escolhas.push({
      ...draft.escolhas[0]!,
      sequencia: 100,
      jogadorId: 'player-100',
      jogadorNome: 'Jogador 100',
    })
    const wrapper = mountBoard(draft)
    const picks = wrapper.findAll('[data-pick-sequence]')

    expect(picks).toHaveLength(41)
    expect(picks[0]!.get('[data-pick-team-order]').text()).toBe('Time 1 · 1ª escolha')
    expect(picks[19]!.get('[data-pick-team-order]').text()).toBe('Time 1 · 2ª escolha')
    expect(picks[20]!.get('[data-pick-team-order]').text()).toBe('Time 1 · 3ª escolha')
    expect(picks[39]!.get('[data-pick-team-order]').text()).toBe('Time 1 · 4ª escolha')
    expect(picks[40]!.get('[data-pick-sequence-number]').text()).toBe('#100')
    expect(picks[40]!.get('[data-pick-team-order]').text()).toBe('Time 1 · 5ª escolha')
    expect(picks[0]!.get('[data-pick-player]').text()).toBe('Jogador 1 com nome competitivo longo')
    expect(wrapper.get('[data-pick-sequence-list]').element.children).toHaveLength(41)
    wrapper.unmount()
  })

  it('preserves the labeled ordered list, progress, and localized empty state', async () => {
    const wrapper = mountBoard()
    const overview = wrapper.get('.draft-pick-overview')
    const list = wrapper.get('[data-pick-sequence-list]')

    expect(overview.attributes('aria-label')).toBe('Sequência de escolhas')
    expect(list.element.tagName).toBe('OL')
    expect(list.attributes('aria-label')).toBe('Sequência de escolhas')
    expect(Array.from(list.element.children).every((child) => child.tagName === 'LI')).toBe(true)
    expect(wrapper.get('[data-pick-progress]').text()).toBe('2 / 4 escolhas')

    await setLocale('en')
    await nextTick()
    expect(list.attributes('aria-label')).toBe('Pick sequence')
    expect(wrapper.get('[data-pick-progress]').text()).toBe('2 / 4 picks')
    expect(wrapper.findAll('[data-pick-player]')[2]!.text()).toBe('Turn timed out')
    wrapper.unmount()

    await setLocale('pt')
    await nextTick()
    const emptyDraft = montagem()
    emptyDraft.escolhas = []
    const emptyWrapper = mountBoard(emptyDraft)

    expect(emptyWrapper.find('[data-pick-sequence-list]').exists()).toBe(false)
    expect(emptyWrapper.get('.draft-pick-overview').text()).toContain('Nenhuma escolha registrada ainda.')

    await setLocale('en')
    await nextTick()
    expect(emptyWrapper.get('.draft-pick-overview').text()).toContain('No picks registered yet.')
    emptyWrapper.unmount()
  })

  it('uses an auto-fit pick grid without internal scrolling or fixed number width', () => {
    expect(MainCss).toMatch(/\.draft-pick-overview ol\s*{[\s\S]*?grid-template-columns:\s*repeat\(auto-fit,\s*minmax\(min\(220px,\s*100%\),\s*1fr\)\)/)
    expect(MainCss).toMatch(/\.draft-pick-card\s*{[\s\S]*?grid-template-columns:\s*minmax\(36px,\s*auto\)\s+minmax\(0,\s*1fr\)/)
    expect(MainCss).toMatch(/\.draft-pick-card__number\s*{[\s\S]*?min-width:\s*36px/)
    expect(MainCss).toMatch(/\.draft-pick-card__copy\s*>\s*strong,[\s\S]*?overflow-wrap:\s*anywhere/)

    const overviewRule = MainCss.match(/\.draft-pick-overview\s*{(?<declarations>[^}]*)}/)?.groups?.declarations ?? ''
    const listRule = MainCss.match(/\.draft-pick-overview ol\s*{(?<declarations>[^}]*)}/)?.groups?.declarations ?? ''
    expect(`${overviewRule}\n${listRule}`).not.toMatch(/max-height|overflow-y|overflow:\s*(auto|scroll)/)
  })

  it('uses a semantic list whose direct children are only available-player list items', () => {
    const wrapper = mountBoard()
    const list = wrapper.get('[data-available-player-list]')
    const header = wrapper.get('.draft-player-row--head')

    expect(list.element.tagName).toBe('UL')
    expect(list.attributes('role')).toBe('list')
    expect(list.element.contains(header.element)).toBe(false)
    expect(header.attributes('role')).toBeUndefined()
    expect(Array.from(list.element.children).every((child) => child.tagName === 'LI')).toBe(true)
    expect(list.findAll(':scope > li')).toHaveLength(2)
    wrapper.unmount()
  })

  it('keeps the active turn and available pool explicit and emits only jogadorId for a pick', async () => {
    const draft = montagem('Aberta', 'TempoReal')
    draft.turnoAtualTimeId = 'team-a'
    draft.turnoAtualCapitaoId = 'captain-a'
    draft.turnoSequencia = 3
    draft.turnoExpiraEm = new Date(Date.now() + 60_000).toISOString()
    const wrapper = mountBoard(draft, { currentPlayerId: 'captain-a', canCurrentUserPick: true })

    expect(wrapper.get('[data-active-turn]').text()).toContain('Captain A')
    expect(wrapper.get('[data-available-pool]').text()).toContain('Jogadores Disponíveis')
    await wrapper.get('[data-player-id="available-1"] .draft-pick-action').trigger('click')

    expect(wrapper.emitted('pick')).toEqual([['available-1']])
    wrapper.unmount()
  })

  it.each<[string, PickDenialScenario]>([
    ['server authorization is false', { canCurrentUserPick: false }],
    ['current team is missing', { canCurrentUserPick: true, teamId: 'missing-team' }],
    ['current captain is missing', { canCurrentUserPick: true, captainId: 'missing-captain' }],
    ['captain does not belong to the current team', { canCurrentUserPick: true, captainId: 'captain-b' }],
    ['turn is expired', { canCurrentUserPick: true, expired: true }],
  ])('does not offer picks when %s', (_, scenario) => {
    const draft = montagem('Aberta', 'TempoReal')
    draft.turnoAtualTimeId = scenario.teamId ?? 'team-a'
    draft.turnoAtualCapitaoId = scenario.captainId ?? 'captain-a'
    draft.turnoSequencia = 3
    draft.turnoExpiraEm = new Date(Date.now() + (scenario.expired ? -1_000 : 60_000)).toISOString()

    const wrapper = mountBoard(draft, {
      currentPlayerId: draft.turnoAtualCapitaoId,
      canCurrentUserPick: scenario.canCurrentUserPick,
    })

    expect(wrapper.find('.draft-pick-action').exists()).toBe(false)
    wrapper.unmount()
  })

  it('does not offer, emit, or lock a pick when the matching participant is not marked as captain', async () => {
    const draft = montagem('Aberta', 'TempoReal')
    draft.turnoAtualTimeId = 'team-a'
    draft.turnoAtualCapitaoId = 'captain-a'
    draft.turnoSequencia = 3
    draft.turnoExpiraEm = new Date(Date.now() + 60_000).toISOString()
    draft.times[1]!.jogadores[0]!.capitao = false
    const wrapper = mountBoard(draft, { currentPlayerId: 'captain-a', canCurrentUserPick: true })
    const vm = wrapper.vm as unknown as {
      localMontagem: DraftMontagem
      pickPlayer: (player: DraftMontagemParticipante) => void
    }

    expect(wrapper.find('.draft-pick-action').exists()).toBe(false)
    vm.pickPlayer(draft.livres[0]!)
    expect(wrapper.emitted('pick')).toBeUndefined()

    vm.localMontagem.times.find((team) => team.id === 'team-a')!.jogadores[0]!.capitao = true
    await nextTick()
    await wrapper.get('.draft-pick-action').trigger('click')
    expect(wrapper.emitted('pick')).toEqual([['available-1']])
    wrapper.unmount()
  })

  it('locks rapid pick events until a saving cycle or projection update resets it', async () => {
    const draft = montagem('Aberta', 'TempoReal')
    draft.turnoAtualTimeId = 'team-a'
    draft.turnoAtualCapitaoId = 'captain-a'
    draft.turnoSequencia = 3
    draft.turnoExpiraEm = new Date(Date.now() + 60_000).toISOString()
    const wrapper = mountBoard(draft, { currentPlayerId: 'captain-a', canCurrentUserPick: true })

    const pick = wrapper.get('.draft-pick-action')
    await Promise.all([pick.trigger('click'), pick.trigger('click')])
    expect(wrapper.emitted('pick')).toEqual([['available-1']])

    await wrapper.setProps({ saving: true })
    await wrapper.setProps({ saving: false })
    await wrapper.get('.draft-pick-action').trigger('click')
    expect(wrapper.emitted('pick')).toEqual([['available-1'], ['available-1']])

    await wrapper.setProps({ montagem: { ...draft, turnoSequencia: 4 } })
    await wrapper.get('.draft-pick-action').trigger('click')
    expect(wrapper.emitted('pick')).toEqual([['available-1'], ['available-1'], ['available-1']])
    wrapper.unmount()
  })

  it('uses the server clock offset to reject a locally future but server-expired turn', () => {
    const localNow = Date.parse('2026-07-25T12:00:00Z')
    const dateNow = vi.spyOn(Date, 'now').mockReturnValue(localNow)
    const draft = montagem('Aberta', 'TempoReal')
    draft.turnoAtualTimeId = 'team-a'
    draft.turnoAtualCapitaoId = 'captain-a'
    draft.turnoSequencia = 3
    draft.turnoExpiraEm = '2026-07-25T12:05:00Z'

    const wrapper = mountBoard(draft, {
      currentPlayerId: 'captain-a',
      canCurrentUserPick: true,
      serverClockOffsetMs: 10 * 60 * 1000,
    })
    dateNow.mockRestore()

    expect(wrapper.find('.draft-pick-action').exists()).toBe(false)
    expect(wrapper.find('[data-active-turn]').exists()).toBe(false)
    wrapper.unmount()
  })

  it('localizes route filter labels in English without changing their filtering values', async () => {
    setLocale('en')
    const wrapper = mountBoard()
    const filters = wrapper.findAll('.draft-route-filters button')

    expect(filters.map((filter) => filter.text())).toEqual(['ALL ROUTES', 'TOP', 'JUNGLE', 'MID', 'ADC', 'SUPPORT'])
    expect(filters.map((filter) => filter.attributes('aria-pressed'))).toEqual(['true', 'false', 'false', 'false', 'false', 'false'])
    await filters.find((filter) => filter.text() === 'SUPPORT')!.trigger('click')
    expect(filters.find((filter) => filter.text() === 'SUPPORT')!.classes()).toContain('is-active')
    expect(filters.find((filter) => filter.text() === 'SUPPORT')!.attributes('aria-pressed')).toBe('true')
    expect(filters[0]!.attributes('aria-pressed')).toBe('false')
    expect(wrapper.find('[data-player-id="available-1"]').exists()).toBe(true)

    await filters.find((filter) => filter.text() === 'ADC')!.trigger('click')
    expect(wrapper.find('[data-player-id="available-1"]').exists()).toBe(false)
    expect(wrapper.text()).toContain('No available players for the current filter.')
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

  it('labels the board and preserves accessible controls, safe names, and reduced motion', () => {
    const wrapper = mountBoard()

    expect(wrapper.get('.draft-visual-shell').attributes('aria-label')).toBe('Tabuleiro do draft')
    expect(wrapper.findAll('button, input, [role="button"]').every((control) => control.attributes('aria-hidden') !== 'true')).toBe(true)
    expect(MainCss).toMatch(/\.drafts-page\s+:is\(button,\s*input:not\(\[type='checkbox'\]\),\s*select,\s*textarea,\s*\[role='button'\]\)\s*{[^}]*min-height:\s*44px/s)
    expect(MainCss).toMatch(/\.drafts-page\s+:is\(button,\s*\[role='button'\]\)\s*{[^}]*touch-action:\s*manipulation/s)
    expect(MainCss).toMatch(/\.drafts-page\s+\.draft-slot__copy\s*>\s*strong[\s\S]*?overflow-wrap:\s*anywhere/s)
    expect(MainCss).toMatch(/@media \(prefers-reduced-motion:\s*reduce\)[\s\S]*?\.drafts-page\s+\.draft-turn-clock__bar span[\s\S]*?transition:\s*none/s)
    expect(MainCss).toMatch(/@media \(max-width:\s*768px\)[\s\S]*?\.drafts-page\s+\.draft-visual-board\s*{[^}]*grid-template-columns:\s*minmax\(0, 1fr\)/s)
    wrapper.unmount()
  })

  it.each([
    ['pt', ['Nome do time Team A', 'Nome do time Team B'], 'Buscar jogadores disponíveis'],
    ['en', ['Team Team A name', 'Team Team B name'], 'Search available players'],
  ] as const)('labels team names and player search in %s', (locale, teamLabels, searchLabel) => {
    setLocale(locale)
    const wrapper = mountBoard()
    const teamInputs = wrapper.findAll('.draft-team__header input')

    expect(teamInputs.map((input) => input.attributes('aria-label'))).toEqual(teamLabels)
    expect(teamInputs.map((input) => input.attributes('name'))).toEqual(['draft-team-team-a', 'draft-team-team-b'])
    expect(teamInputs.every((input) => input.attributes('autocomplete') === 'off')).toBe(true)
    expect(wrapper.get('.draft-search-field input').attributes('aria-label')).toBe(searchLabel)
    expect(wrapper.get('.draft-search-field input').attributes('name')).toBe('draft-player-search')
    wrapper.unmount()
  })

  it('gives every player-details link a scoped 44px target', async () => {
    const draft = montagem()
    draft.livres[0]!.opGgUrl = 'https://example.com/opgg'
    draft.livres[0]!.deepLolUrl = 'https://example.com/deeplol'
    const wrapper = mountBoard(draft)

    await wrapper.get('[data-player-id="available-1"] [data-player-details]').trigger('click')
    const links = wrapper.get('.player-details-modal').findAll('a')

    expect(links.map((link) => link.attributes('href'))).toEqual(['https://example.com/opgg', 'https://example.com/deeplol'])
    expect(MainCss).toMatch(/\.drafts-page\s+\.player-details-modal\s+a\s*{[^}]*display:\s*inline-flex[^}]*min-width:\s*44px[^}]*min-height:\s*44px/s)
    wrapper.unmount()
  })

  it('opens player details with Space and hides decorative initials from assistive technology', async () => {
    const wrapper = mountBoard()
    const playerRow = wrapper.get('[data-player-id="available-1"]')
    const details = playerRow.get('[data-player-details]')

    expect(wrapper.findAll('.draft-slot__avatar').every((avatar) => avatar.attributes('aria-hidden') === 'true')).toBe(true)
    expect(playerRow.attributes('role')).toBeUndefined()
    expect(playerRow.attributes('tabindex')).toBeUndefined()
    expect(details.element.tagName).toBe('BUTTON')
    await details.trigger('keydown', { key: ' ' })
    await details.trigger('click')

    expect(wrapper.get('[role="dialog"]').attributes('aria-label')).toContain('Available Mid')
    wrapper.unmount()
  })

  it('keeps pick and substitute keyboard actions separate from player details', async () => {
    const realtime = montagem('Aberta', 'TempoReal')
    realtime.turnoAtualTimeId = 'team-a'
    realtime.turnoAtualCapitaoId = 'captain-a'
    realtime.turnoSequencia = 3
    realtime.turnoExpiraEm = new Date(Date.now() + 60_000).toISOString()
    const picks = mountBoard(realtime, { currentPlayerId: 'captain-a', canCurrentUserPick: true })
    const pick = picks.get('.draft-pick-action')
    const pickBubbled = vi.fn()
    picks.get('[data-player-id="available-1"]').element.addEventListener('keydown', pickBubbled)

    await pick.trigger('keydown', { key: 'Enter' })
    await pick.trigger('click')
    expect(picks.emitted('pick')).toEqual([['available-1']])
    expect(pickBubbled).not.toHaveBeenCalled()
    expect(picks.find('[role="dialog"]').exists()).toBe(false)
    picks.unmount()

    const substitutionDraft = montagem()
    substitutionDraft.times[1]!.jogadores.push({ ...player('team-player', 'Team Player', 'Time'), ordem: 2 })
    const substitutions = mountBoard(substitutionDraft)
    const substitute = substitutions.get('[data-team-id="team-a"] [data-player-id="team-player"] .draft-substitute-action')
    const substituteBubbled = vi.fn()
    substitutions.get('[data-team-id="team-a"] .draft-visual-slot:not(.is-captain)').element.addEventListener('keydown', substituteBubbled)
    await substitute.trigger('keydown', { key: ' ' })
    await substitute.trigger('click')
    expect(substitutions.emitted('substituteReserve')).toBeUndefined()
    expect(substituteBubbled).not.toHaveBeenCalled()
    expect(substitutions.find('[role="dialog"]').exists()).toBe(true)

    substitutions.getComponent(Select).vm.$emit('update:modelValue', 'reserve-1')
    await nextTick()
    await substitutions.get('form').trigger('submit')
    expect(substitutions.emitted('substituteReserve')).toEqual([[
      { timeId: 'team-a', jogadorSaiuId: 'team-player', reservaEntrouId: 'reserve-1', novoCapitaoId: null, motivo: null },
    ]])
    substitutions.unmount()
  })

  it('requires an explicit eligible new captain when the daily captain leaves', async () => {
    const draft = montagem('Aberta', 'TempoReal')
    draft.cicloVersao = 'ModoPosPresenca'
    const wrapper = mountBoard(draft, { eligibleCaptainIds: ['reserve-1', 'captain-b'] })

    await wrapper.get('[data-team-id="team-a"] [data-player-id="captain-a"] .draft-substitute-action').trigger('click')
    const selects = wrapper.findAllComponents(Select)
    selects[0]!.vm.$emit('update:modelValue', 'reserve-1')
    await nextTick()
    expect(wrapper.emitted('substituteReserve')).toBeUndefined()

    selects[1]!.vm.$emit('update:modelValue', 'reserve-1')
    await wrapper.get('form').trigger('submit')

    expect(wrapper.emitted('substituteReserve')).toEqual([[
      { timeId: 'team-a', jogadorSaiuId: 'captain-a', reservaEntrouId: 'reserve-1', novoCapitaoId: 'reserve-1', motivo: null },
    ]])
    wrapper.unmount()
  })

  it('disables substitutions while saving or terminal and restores the triggering action after cancel', async () => {
    const draft = montagem()
    draft.times[1]!.jogadores.push({ ...player('team-player', 'Team Player', 'Time'), ordem: 2 })
    const wrapper = mountBoard(draft, { attachTo: document.body })
    const substitute = wrapper.get('.draft-substitute-action')

    ;(substitute.element as HTMLElement).focus()
    await substitute.trigger('click')
    await wrapper.get('[data-testid="substitution-cancel"]').trigger('click')
    await nextTick()
    expect(document.activeElement).toBe(substitute.element)
    expect(wrapper.emitted('substituteReserve')).toBeUndefined()

    await wrapper.setProps({ saving: true })
    expect(wrapper.get('.draft-substitute-action').attributes('disabled')).toBeDefined()
    wrapper.unmount()

    const terminal = montagem('Finalizada')
    terminal.times[1]!.jogadores.push({ ...player('team-player', 'Team Player', 'Time'), ordem: 2 })
    const terminalWrapper = mountBoard(terminal)
    expect(terminalWrapper.find('.draft-substitute-action').exists()).toBe(false)
    terminalWrapper.unmount()
  })

  it('offers a localized keyboard and touch move destination for editable players', async () => {
    const wrapper = mountBoard(montagem(), { attachTo: document.body })
    const row = wrapper.get('[data-player-id="available-1"]')
    const destination = row.get('[data-move-destination]')
    const moveControls = wrapper.findAll('[data-move-destination]')

    expect(destination.element.tagName).toBe('SELECT')
    expect(destination.attributes('aria-label')).toBe('Mover Available Mid para')
    expect(moveControls.every((control) => control.attributes('name')?.startsWith('draft-move-'))).toBe(true)
    expect(moveControls.every((control) => control.attributes('autocomplete') === 'off')).toBe(true)
    expect(destination.findAll('option').map((option) => option.text())).toEqual(expect.arrayContaining(['Jogadores livres', 'Reservas', 'Team A', 'Team B']))
    ;(destination.element as InstanceType<typeof globalThis.HTMLElement>).focus()
    await destination.setValue('team-a')
    await nextTick()
    await nextTick()

    const movedPlayer = wrapper.get('[data-team-id="team-a"] [data-player-id="available-1"]')
    expect(movedPlayer.text()).toContain('Available Mid')
    expect(movedPlayer.element.contains(document.activeElement)).toBe(true)
    expect(document.activeElement?.matches('[data-move-destination], [data-player-details]')).toBe(true)
    expect(wrapper.get('[data-move-announcement]').attributes()).toMatchObject({ role: 'status', 'aria-live': 'polite', 'aria-atomic': 'true' })
    expect(wrapper.get('[data-move-announcement]').text()).toBe('Available Mid foi movido para Team A.')
    wrapper.unmount()
  })

  it.each([
    ['livres', 'search', 'Jogadores livres'],
    ['reservas', 'route', 'Reservas'],
  ] as const)('focuses player search after moving an excluded team player to %s', async (target, activeFilter, destinationLabel) => {
    const draft = montagem()
    draft.times[1]!.jogadores.push({ ...player('team-player', 'Team Player', 'Time'), ordem: 2 })
    const wrapper = mountBoard(draft, { attachTo: document.body })
    const search = wrapper.get('.draft-search-field input')
    const adcFilter = wrapper.findAll('.draft-route-filters button').find((filter) => filter.text() === 'ADC')!

    if (activeFilter === 'search') await search.setValue('Reserve Support')
    else await adcFilter.trigger('click')

    const destination = wrapper.get('[data-team-id="team-a"] [data-player-id="team-player"] [data-move-destination]')
    ;(destination.element as InstanceType<typeof globalThis.HTMLElement>).focus()
    await destination.setValue(target)
    await nextTick()
    await nextTick()

    expect(wrapper.find('[data-player-id="team-player"]').exists()).toBe(false)
    expect(document.activeElement).toBe(search.element)
    expect((search.element as InstanceType<typeof globalThis.HTMLInputElement>).value).toBe(activeFilter === 'search' ? 'Reserve Support' : '')
    expect(adcFilter.attributes('aria-pressed')).toBe(activeFilter === 'route' ? 'true' : 'false')
    expect(wrapper.get('[data-move-announcement]').text()).toBe(`Team Player foi movido para ${destinationLabel}.`)
    wrapper.unmount()
  })

  it('keeps a player in place when a move destination is invalid', () => {
    const wrapper = mountBoard()
    const vm = wrapper.vm as unknown as {
      localMontagem: DraftMontagem
      movePlayerById: (jogadorId: string, target: string) => void
    }

    vm.movePlayerById('available-1', 'missing-team')

    expect(vm.localMontagem.livres.map((item) => item.jogadorId)).toContain('available-1')
    wrapper.unmount()
  })

  it('announces realtime turn and pick progress politely', () => {
    const draft = montagem('Aberta', 'TempoReal')
    draft.turnoAtualTimeId = 'team-a'
    draft.turnoAtualCapitaoId = 'captain-a'
    draft.turnoSequencia = 3
    draft.turnoExpiraEm = new Date(Date.now() + 60_000).toISOString()
    const wrapper = mountBoard(draft)
    const announcement = wrapper.get('[data-realtime-announcement]')

    expect(announcement.attributes()).toMatchObject({ role: 'status', 'aria-live': 'polite', 'aria-atomic': 'true' })
    expect(announcement.text()).toContain('Captain A')
    expect(announcement.text()).toContain('2 de 4')
    wrapper.unmount()
  })

  it('does not render the realtime announcement region in manual mode', () => {
    const wrapper = mountBoard()

    expect(wrapper.find('[data-realtime-announcement]').exists()).toBe(false)
    wrapper.unmount()
  })

  it('animates active indicators with transform and opacity instead of width or box-shadow', () => {
    expect(MainCss).toMatch(/\.draft-turn-clock__pulse\s*{[^}]*opacity:/s)
    expect(MainCss).not.toMatch(/\.draft-turn-clock__pulse\s*{[^}]*box-shadow:/s)
    expect(MainCss).toMatch(/\.draft-turn-clock__bar span\s*{[^}]*transform-origin:\s*left[^}]*transition:\s*transform/s)
    expect(MainCss).not.toMatch(/\.draft-turn-clock__bar span\s*{[^}]*transition:\s*width/s)
  })
})
