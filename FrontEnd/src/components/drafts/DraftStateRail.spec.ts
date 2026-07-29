// @vitest-environment happy-dom
import { mount } from '@vue/test-utils'
import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { afterEach, describe, expect, it } from 'vitest'

import { DRAFT_MONTAGEM_STATUS_OPTIONS } from '@/constants/draftMontagemStatus'
import { i18n, setLocale } from '@/i18n'

import DraftStateRail from './DraftStateRail.vue'
const MainCss = readFileSync(resolve(process.cwd(), 'src/styles/main.css'), 'utf8')

function mountRail(status = 'Aberta', publicationStatus: string | null = 'Pendente', modo: 'Manual' | 'TempoReal' | null = 'TempoReal', cicloVersao: 'Legado' | 'ModoPosPresenca' = 'Legado') {
  return mount(DraftStateRail, {
    props: { status, publicationStatus, modo, cicloVersao },
    global: { plugins: [i18n] },
  })
}

describe('DraftStateRail', () => {
  afterEach(() => setLocale('pt'))

  it.each([
    ['PresencaAberta', 'Presença aberta', 0, 'active', 'Atual'],
    ['PresencaEncerrada', 'Presença encerrada', 1, 'active', 'Atual'],
    ['CapitaesDefinidos', 'Capitães', 2, 'active', 'Atual'],
    ['OrdemDefinida', 'Ordem', 3, 'active', 'Atual'],
    ['Aberta', 'Escolhas', 4, 'active', 'Atual'],
    ['Finalizada', 'Finalizado', 5, 'terminal', 'Encerrada'],
  ])('maps %s to its canonical current step', (status, label, completedSteps, state, stateLabel) => {
    const wrapper = mountRail(status)

    const current = wrapper.get(`[data-state="${state}"]`)
    expect(current.text()).toContain(label)
    expect(current.get('[data-step-state-label]').text()).toBe(stateLabel)
    expect(current.attributes('aria-label')).toBe(`${label}: ${stateLabel}`)
    expect(current.attributes('aria-current')).toBe('step')
    expect(wrapper.get('ol').findAll('[data-state="done"]')).toHaveLength(completedSteps)
  })

  it('renders cancellation as terminal without an active operational step', () => {
    const wrapper = mountRail('Cancelada', 'Publicada')

    const operational = wrapper.get('ol')
    expect(operational.find('[data-state="active"]').exists()).toBe(false)
    expect(operational.find('[aria-current="step"]').exists()).toBe(false)
    expect(operational.get('[data-state="terminal"] [data-step-state-label]').text()).toBe('Encerrada')
  })

  it('renders an unknown status as neutral without inferring progress', () => {
    const wrapper = mountRail('StatusLegado', null)

    const operational = wrapper.get('ol')
    expect(operational.find('[data-state="active"]').exists()).toBe(false)
    expect(operational.find('[data-state="done"]').exists()).toBe(false)
    expect(operational.get('[data-state="unknown"] [data-step-state-label]').text()).toBe('Indisponível')
  })

  it.each([
    ['Falha', 'attention'],
    ['Pendente', 'attention'],
    ['RequerReconciliacao', 'attention'],
    ['EmAndamento', 'attention'],
    ['Publicada', 'done'],
    ['Ignorada', 'pending'],
    [null, 'pending'],
  ])('keeps Discord %s parallel and never current', (publicationStatus, state) => {
    const wrapper = mountRail('Aberta', publicationStatus)

    const operational = wrapper.get('ol')
    const discord = wrapper.get('[data-discord-integration]')
    expect(operational.find('[data-step-id="discord"]').exists()).toBe(false)
    expect(discord.attributes('data-state')).toBe(state)
    expect(discord.attributes('aria-current')).toBeUndefined()
    expect(operational.element.compareDocumentPosition(discord.element) & 4).toBe(4)
  })

  it('derives the operational sequence from the canonical status options', () => {
    const wrapper = mountRail('PresencaAberta', null)
    const expectedIds = DRAFT_MONTAGEM_STATUS_OPTIONS.filter((status) => status !== 'Cancelada')

    expect(wrapper.get('ol').findAll('.draft-rail__step').map((step) => step.attributes('data-step-id'))).toEqual(expectedIds)
  })

  it('uses a shorter captain-free sequence for a v2 manual draft', () => {
    const wrapper = mountRail('Aberta', null, 'Manual', 'ModoPosPresenca')

    expect(wrapper.get('ol').findAll('.draft-rail__step').map((step) => step.attributes('data-step-id'))).toEqual([
      'PresencaAberta',
      'PresencaEncerrada',
      'Aberta',
      'Finalizada',
    ])
    expect(wrapper.find('[data-step-id="CapitaesDefinidos"]').exists()).toBe(false)
    expect(wrapper.find('[data-step-id="OrdemDefinida"]').exists()).toBe(false)
  })

  it('shows mode selection as the current v2 step without changing legacy rails', () => {
    const v2 = mountRail('PresencaEncerrada', null, null, 'ModoPosPresenca')
    const legacy = mountRail('PresencaEncerrada', null, null, 'Legado')

    expect(v2.get('[aria-current="step"]').attributes('data-step-id')).toBe('Modo')
    expect(v2.get('[aria-current="step"]').text()).toContain('Modo do draft')
    expect(legacy.find('[data-step-id="Modo"]').exists()).toBe(false)
  })

  it('marks mode completed and captains current after realtime mode is selected', () => {
    const wrapper = mountRail('PresencaEncerrada', null, 'TempoReal', 'ModoPosPresenca')

    expect(wrapper.get('[data-step-id="Modo"]').attributes('data-state')).toBe('done')
    expect(wrapper.get('[aria-current="step"]').attributes('data-step-id')).toBe('CapitaesDefinidos')
  })

  it.each([
    ['pt', 'Atenção', 'Discord: Atenção'],
    ['en', 'Attention', 'Discord: Attention'],
  ] as const)('renders visible and accessible Discord attention in %s', (locale, visibleState, accessibleName) => {
    setLocale(locale)
    const wrapper = mountRail('Aberta', 'Falha')
    const discord = wrapper.get('[data-discord-integration]')

    expect(discord.get('[data-integration-state-label]').text()).toBe(visibleState)
    expect(discord.attributes('aria-label')).toBe(accessibleName)
  })

  it.each([
    ['pt', 'Concluída', 'Atual', 'Pendente', 'Encerrada', 'Indisponível'],
    ['en', 'Completed', 'Current', 'Pending', 'Closed', 'Unavailable'],
  ] as const)('exposes every operational state with text and aria in %s', (locale, done, active, pending, terminal, unknown) => {
    setLocale(locale)
    const activeRail = mountRail('PresencaEncerrada')
    const terminalRail = mountRail('Cancelada')
    const unknownRail = mountRail('EstadoLegado')

    expect(activeRail.get('[data-state="done"] [data-step-state-label]').text()).toBe(done)
    expect(activeRail.get('[data-state="active"] [data-step-state-label]').text()).toBe(active)
    expect(activeRail.get('[data-state="pending"] [data-step-state-label]').text()).toBe(pending)
    expect(activeRail.get('[data-state="done"]').attributes('aria-label')).toContain(`: ${done}`)
    expect(activeRail.get('[data-state="active"]').attributes('aria-label')).toContain(`: ${active}`)
    expect(activeRail.get('[data-state="pending"]').attributes('aria-label')).toContain(`: ${pending}`)
    expect(terminalRail.get('[data-state="terminal"] [data-step-state-label]').text()).toBe(terminal)
    expect(unknownRail.get('[data-state="unknown"] [data-step-state-label]').text()).toBe(unknown)
  })

  it('renders an ordered, connected horizontal rail that becomes unambiguously vertical', () => {
    const wrapper = mountRail('Aberta', 'Pendente')

    expect(wrapper.get('ol').attributes('aria-label')).toBe('Fluxo do draft')
    expect(wrapper.get('[aria-current="step"]').attributes('data-step-id')).toBe('Aberta')
    expect(MainCss).toMatch(/@media \(min-width:\s*1025px\)[\s\S]*?\.drafts-page\s+\.draft-rail\s*{[^}]*grid-auto-flow:\s*column/s)
    expect(MainCss).toMatch(/\.drafts-page\s+\.draft-rail__step:not\(:last-child\)::after/s)
    expect(MainCss).toMatch(/@media \(max-width:\s*1024px\)[\s\S]*?\.drafts-page\s+\.draft-rail\s*{[^}]*grid-template-columns:\s*minmax\(0, 1fr\)/s)
  })
})
