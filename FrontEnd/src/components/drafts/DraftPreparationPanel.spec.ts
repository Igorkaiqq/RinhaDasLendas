// @vitest-environment happy-dom
import { mount } from '@vue/test-utils'
import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { describe, expect, it } from 'vitest'

import { i18n } from '@/i18n'
import type { DraftMontagem, DraftMontagemPresenca } from '@/types/draftMontagem'

import DraftPreparationPanel from './DraftPreparationPanel.vue'
import DraftPreparationPanelSource from './DraftPreparationPanel.vue?raw'
const MainCss = readFileSync(resolve(process.cwd(), 'src/styles/main.css'), 'utf8')
i18n.global.mergeLocaleMessage('pt', {
  drafts: { presence: { captainsCount: '{selected} / {total} capitães', reopen: 'Reabrir presença' } },
})

const draft: DraftMontagem = {
  id: 'draft-1',
  nome: 'Rinha de domingo',
  status: 'PresencaAberta',
  modo: 'Manual',
  tamanhoEquipe: 5,
  quantidadeTimes: 2,
  quantidadeReservas: 2,
  criterioCapitaes: 'Manual',
  duracaoTurnoSegundos: 60,
  presencaContinuadaManualmente: false,
  presencas: [],
  times: [],
  livres: [],
  reservas: [],
  escolhas: [],
  substituicoes: [],
  arquivado: false,
  versaoEstado: 1,
  publicacoesDiscord: [],
  dataCadastro: '2026-07-25T12:00:00Z',
  dataAtualizacao: '2026-07-25T12:00:00Z',
}

function presences(count: number): DraftMontagemPresenca[] {
  return Array.from({ length: count }, (_, index) => ({
    id: `presence-${index}`,
    usuarioId: `user-${index}`,
    jogadorId: `player-${index}`,
    nomeExibicao: index === count - 1 ? `Jogador ${index} com nome competitivo deliberadamente longo` : `Jogador ${index}`,
    origemConfirmacao: index % 3 === 0 ? 'Manual' : index % 3 === 1 ? 'Discord' : 'Web',
    status: 'Confirmada',
    confirmadoEm: '2026-07-25T12:00:00Z',
    ordemConfirmacao: index + 1,
  }))
}

function mountPanel(overrides: Record<string, unknown> = {}) {
  return mount(DraftPreparationPanel, {
    props: {
      draft,
      confirmedPresences: presences(1),
      saving: false,
      canConfirmPresence: true,
      canCancelPresence: false,
      canClosePresence: true,
      canContinueManualPresence: true,
      canManageManualPresence: true,
      canSelectCaptains: false,
      canReopenPresence: false,
      canDefineCaptains: false,
      canDrawOrder: false,
      captainSelection: [],
      manualPresenceSearch: '',
      selectedManualPresencePlayerId: '',
      availableManualPresencePlayers: [
        { id: 'eligible-1', nomeExibicao: 'Lux' },
        { id: 'eligible-2', nomeExibicao: 'Morgana' },
      ],
      ...overrides,
    },
    global: { plugins: [i18n] },
  })
}

describe('DraftPreparationPanel', () => {
  it.each([0, 1, 10, 14, 30])('keeps a stable roster structure with %i participants', (count) => {
    const expectedPresences = presences(count)
    const wrapper = mountPanel({ confirmedPresences: expectedPresences })
    const rows = wrapper.findAll('[data-presence-row]')

    expect(rows).toHaveLength(count)
    expect(wrapper.get('[data-presence-roster]').classes()).toContain('draft-preparation__roster')
    if (count === 0) expect(wrapper.get('[data-presence-empty]').text()).toBe('Nenhum jogador confirmou presença.')
    rows.forEach((row, index) => {
      expect(row.element.tagName).toBe('LI')
      expect(row.get('[data-presence-identity]').text()).toContain(expectedPresences[index]!.nomeExibicao)
      expect(row.get('[data-presence-origin]').text()).toMatch(/^(Manual|Discord|Site)$/)
      expect(row.get('[data-presence-actions] [data-testid="remove-manual-presence"]')).toBeTruthy()
      expect(row.classes()).toContain('draft-preparation__player')
    })
  })

  it('separates participant identity, origin, and actions without making the row a button', () => {
    const wrapper = mountPanel()
    const row = wrapper.get('[data-presence-row]')

    expect(row.element.tagName).toBe('LI')
    expect(row.get('[data-presence-identity]').text()).toContain('Jogador 0')
    expect(row.get('[data-presence-origin]').text()).toBe('Manual')
    expect(row.get('[data-presence-actions]').find('button').exists()).toBe(true)
  })

  it('groups manual search, selection, and addition and emits exact model events', async () => {
    const wrapper = mountPanel()
    const group = wrapper.get('[data-manual-presence]')

    await group.get('input[type="search"]').setValue('lu')
    await group.get('select').setValue('eligible-1')
    await wrapper.setProps({ selectedManualPresencePlayerId: 'eligible-1' })
    await group.get('[data-testid="add-manual-presence"]').trigger('click')

    expect(wrapper.emitted('update:manualPresenceSearch')).toEqual([['lu']])
    expect(wrapper.emitted('search-manual-presence')).toEqual([[]])
    expect(wrapper.emitted('update:selectedManualPresencePlayerId')).toEqual([['eligible-1']])
    expect(wrapper.emitted('add-manual-presence')).toEqual([[]])
  })

  it('provides meaningful form names and autocomplete metadata for manual controls', () => {
    const group = mountPanel().get('[data-manual-presence]')

    expect(group.get('input[type="search"]').attributes()).toMatchObject({ name: 'manual-presence-search', autocomplete: 'off' })
    expect(group.get('select').attributes()).toMatchObject({ name: 'manual-presence-player', autocomplete: 'off' })
  })

  it('emits exact presence and manual removal events', async () => {
    const wrapper = mountPanel()

    await wrapper.get('[data-testid="confirm-presence"]').trigger('click')
    await wrapper.get('[data-testid="close-presence"]').trigger('click')
    await wrapper.get('[data-testid="continue-manual-presence"]').trigger('click')
    await wrapper.get('[data-testid="remove-manual-presence"]').trigger('click')

    expect(wrapper.emitted('confirm-presence')).toEqual([[]])
    expect(wrapper.emitted('close-presence')).toEqual([[false], [true]])
    expect(wrapper.emitted('remove-manual-presence')).toEqual([['player-0', 'Jogador 0 com nome competitivo deliberadamente longo']])

    const cancellation = mountPanel({ canConfirmPresence: false, canCancelPresence: true })
    await cancellation.get('[data-testid="cancel-presence"]').trigger('click')
    expect(cancellation.emitted('cancel-presence')).toEqual([[]])
  })

  it('exposes captain selection as an accessible and visible selected state', async () => {
    const wrapper = mountPanel({
      draft: { ...draft, status: 'PresencaEncerrada' },
      canClosePresence: false,
      canContinueManualPresence: false,
      canManageManualPresence: false,
      canSelectCaptains: true,
      captainSelection: ['player-0'],
      confirmedPresences: presences(2),
    })
    const selectedCaptain = wrapper.get('[data-testid="toggle-captain-player-0"]')
    const unselectedCaptain = wrapper.get('[data-testid="toggle-captain-player-1"]')

    expect(selectedCaptain.attributes('aria-pressed')).toBe('true')
    expect(selectedCaptain.classes()).toContain('draft-preparation__captain-toggle--selected')
    expect(selectedCaptain.element.closest('[data-presence-row]')?.classList).toContain('draft-preparation__player--captain')
    expect(unselectedCaptain.attributes('aria-pressed')).toBe('false')
    expect(unselectedCaptain.classes()).not.toContain('draft-preparation__captain-toggle--selected')

    await selectedCaptain.trigger('click')
    expect(wrapper.emitted('toggle-captain')).toEqual([['player-0']])
  })

  it('emits captain and order intents only from their matching states', async () => {
    const captains = mountPanel({ draft: { ...draft, status: 'PresencaEncerrada' }, canConfirmPresence: false, canClosePresence: false, canContinueManualPresence: false, canManageManualPresence: false, canSelectCaptains: true, canDefineCaptains: true, captainSelection: ['player-0', 'player-1'], confirmedPresences: presences(2) })
    await captains.get('[data-testid="define-captains"]').trigger('click')
    expect(captains.emitted('define-captains')).toEqual([[]])

    const order = mountPanel({ draft: { ...draft, status: 'CapitaesDefinidos' }, canConfirmPresence: false, canClosePresence: false, canContinueManualPresence: false, canManageManualPresence: false, canDrawOrder: true })
    await order.get('[data-testid="draw-order"]').trigger('click')
    expect(order.emitted('draw-order')).toEqual([[]])
  })

  it('shows captain progress and keeps reopen secondary while defining captains remains primary', async () => {
    const wrapper = mountPanel({
      draft: { ...draft, status: 'PresencaEncerrada', quantidadeTimes: 3 },
      canConfirmPresence: false,
      canClosePresence: false,
      canContinueManualPresence: false,
      canManageManualPresence: false,
      canSelectCaptains: true,
      canReopenPresence: true,
      canDefineCaptains: true,
      captainSelection: ['player-0', 'player-1', 'player-2'],
      confirmedPresences: presences(19),
    })

    expect(wrapper.get('[data-captains-count]').text()).toBe('3 / 3 capitães')
    expect(wrapper.get('[data-testid="reopen-presence"]').classes()).toContain('button-secondary')
    expect(wrapper.findAll('[data-stage-primary-action]')).toHaveLength(1)
    expect(wrapper.get('[data-stage-primary-action]').attributes('data-testid')).toBe('define-captains')

    await wrapper.get('[data-testid="reopen-presence"]').trigger('click')
    expect(wrapper.emitted('reopen-presence')).toEqual([[]])
  })

  it('does not expose or emit reopen without capability and blocks it while saving', async () => {
    const unavailable = mountPanel({ canReopenPresence: false })
    expect(unavailable.find('[data-testid="reopen-presence"]').exists()).toBe(false)

    const saving = mountPanel({ canReopenPresence: true, saving: true })
    await saving.get('[data-testid="reopen-presence"]').trigger('click')
    expect(saving.emitted('reopen-presence')).toBeUndefined()
  })

  it('renders only parent-provided capabilities and disables all available actions while saving', () => {
    const unauthorized = mountPanel({ canClosePresence: false, canContinueManualPresence: false, canManageManualPresence: false })
    expect(unauthorized.find('[data-manual-presence]').exists()).toBe(false)
    expect(unauthorized.find('[data-testid="remove-manual-presence"]').exists()).toBe(false)
    expect(unauthorized.find('[data-testid="close-presence"]').exists()).toBe(false)

    const saving = mountPanel({ saving: true })
    expect(saving.findAll('button').every((button) => button.attributes('disabled') !== undefined)).toBe(true)
    expect(saving.findAll('input, select').every((control) => control.attributes('disabled') !== undefined)).toBe(true)
  })

  it('does not import services', () => {
    expect(DraftPreparationPanelSource).not.toMatch(/@\/services\//)
    expect(DraftPreparationPanelSource).not.toMatch(/\bcanManage:\s*boolean/)
  })

  it('keeps the labelled roster after controls and reflows cards without nested vertical scrolling', () => {
    const wrapper = mountPanel({ confirmedPresences: presences(14) })
    const panel = wrapper.get('section')
    const manual = panel.get('[data-manual-presence]')
    const roster = panel.get('[data-presence-roster]')

    expect(panel.attributes('aria-labelledby')).toBe(`draft-preparation-title-${draft.id}`)
    expect(manual.element.compareDocumentPosition(roster.element) & 4).toBe(4)
    expect(MainCss).toMatch(/\.drafts-page\s+\.draft-preparation__roster\s*{[^}]*repeat\(auto-fit,\s*minmax\(min\(/s)
    expect(MainCss).toMatch(/@media \(max-width:\s*768px\)[\s\S]*?\.drafts-page\s+\.draft-preparation__roster\s*{[^}]*grid-template-columns:\s*minmax\(0, 1fr\)/s)
    expect(MainCss).toMatch(/\.drafts-page\s+\.draft-preparation__roster\s*{[^}]*overflow-y:\s*visible/s)
  })
})
