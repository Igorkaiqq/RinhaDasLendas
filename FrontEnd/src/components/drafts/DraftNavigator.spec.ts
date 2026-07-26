// @vitest-environment happy-dom
import { mount } from '@vue/test-utils'
import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { afterEach, describe, expect, it } from 'vitest'

import { DRAFT_MONTAGEM_STATUS_OPTIONS } from '@/constants/draftMontagemStatus'
import { i18n, setLocale } from '@/i18n'
import type { DraftMontagemResumo, DraftMontagemStatus } from '@/types/draftMontagem'

import DraftNavigator from './DraftNavigator.vue'
import DraftNavigatorSource from './DraftNavigator.vue?raw'
const MainCss = readFileSync(resolve(process.cwd(), 'src/styles/main.css'), 'utf8')

type DraftNavigatorItem = Omit<DraftMontagemResumo, 'status'> & { status: string }

const longName = 'Rinha de domingo com um nome deliberadamente longo para continuar identificável no modo compacto'
const baseDraft: DraftNavigatorItem = {
  id: 'draft-1',
  nome: longName,
  status: 'PresencaAberta',
  modo: 'Manual',
  tamanhoEquipe: 5,
  quantidadeTimes: 2,
  quantidadeReservas: 1,
  presencaContinuadaManualmente: false,
  dataRinha: '2026-07-26T18:00:00Z',
  dataCadastro: '2026-07-25T12:00:00Z',
  dataAtualizacao: '2026-07-25T12:00:00Z',
  arquivado: false,
  versaoEstado: 3,
}

function mountNavigator(overrides: Partial<{
  drafts: readonly DraftNavigatorItem[]
  selectedDraftId: string | null
  searchTerm: string
  selectedStatus: DraftMontagemStatus | ''
  statusOptions: readonly DraftMontagemStatus[]
  loading: boolean
  loadFailed: boolean
  hasKnownDrafts: boolean
  canCreate: boolean
  canIncludeArchived: boolean
  includeArchived: boolean
}> = {}) {
  return mount(DraftNavigator, {
    props: {
      drafts: [baseDraft],
      selectedDraftId: baseDraft.id,
      searchTerm: '',
      selectedStatus: '',
      statusOptions: DRAFT_MONTAGEM_STATUS_OPTIONS,
      loading: false,
      loadFailed: false,
      hasKnownDrafts: true,
      canCreate: true,
      canIncludeArchived: true,
      includeArchived: false,
      ...overrides,
    },
    global: { plugins: [i18n] },
  })
}

describe('DraftNavigator', () => {
  afterEach(() => setLocale('pt'))

  it('renders all seven statuses in cycle order and emits controlled filter updates', async () => {
    const wrapper = mountNavigator()
    const status = wrapper.get('select')

    expect(status.findAll('option').map((option) => option.attributes('value'))).toEqual([
      '',
      'PresencaAberta',
      'PresencaEncerrada',
      'CapitaesDefinidos',
      'OrdemDefinida',
      'Aberta',
      'Finalizada',
      'Cancelada',
    ])

    await wrapper.get('input[type="search"]').setValue('domingo')
    await status.setValue('OrdemDefinida')

    expect(wrapper.emitted('update:searchTerm')).toEqual([['domingo']])
    expect(wrapper.emitted('update:selectedStatus')).toEqual([['OrdemDefinida']])
    expect(status.attributes('autocomplete')).toBe('off')
    expect(wrapper.get('input[type="search"]').attributes('placeholder')).toBe('Ex.: Rinha de domingo…')
  })

  it('marks only the selected draft as current and emits its exact identity', async () => {
    const second = { ...baseDraft, id: 'draft-2', nome: 'Rinha de segunda', status: 'Finalizada' }
    const wrapper = mountNavigator({ drafts: [baseDraft, second] })
    const items = wrapper.findAll('[data-draft-id]')

    expect(items[0]!.attributes('aria-current')).toBe('true')
    expect(items[1]!.attributes('aria-current')).toBeUndefined()

    await items[1]!.trigger('click')
    expect(wrapper.emitted('select')).toEqual([['draft-2']])
  })

  it('shows the Admin-only archived filter and emits its controlled value', async () => {
    const wrapper = mountNavigator()

    await wrapper.get('[data-include-archived]').setValue(true)

    expect(wrapper.emitted('update:includeArchived')).toEqual([[true]])
    expect(mountNavigator({ canIncludeArchived: false }).find('[data-include-archived]').exists()).toBe(false)
  })

  it('shows archived and operational badges independently', () => {
    const wrapper = mountNavigator({ drafts: [{ ...baseDraft, arquivado: true, status: 'Cancelada' }] })

    expect(wrapper.get('[data-draft-archived]').text()).toBe('Arquivado')
    expect(wrapper.get('[data-draft-status]').text()).toBe('Cancelada')
  })

  it('uses the archived empty state when that administrative filter has no results', () => {
    const wrapper = mountNavigator({ drafts: [], includeArchived: true, hasKnownDrafts: false })

    expect(wrapper.get('[data-navigator-archived-empty]').text()).toContain('Nenhum draft arquivado')
  })

  it.each([
    ['pt', 'Data não informada', 'Estado desconhecido'],
    ['en', 'Date not informed', 'Unknown status'],
  ] as const)('localizes missing date and unknown status in %s', (locale, missingDate, unknownStatus) => {
    setLocale(locale)
    const wrapper = mountNavigator({
      drafts: [{ ...baseDraft, status: 'EstadoLegado', dataRinha: null, horarioEncerramentoPresenca: null }],
    })

    expect(wrapper.get('[data-draft-date]').text()).toContain(missingDate)
    expect(wrapper.get('[data-draft-status]').text()).toBe(unknownStatus)
    expect(wrapper.get('[data-draft-status]').attributes('data-status')).toBe('unknown')
  })

  it('shows a localized skeleton state before list, failure, or empty content', () => {
    const wrapper = mountNavigator({ drafts: [], loading: true, hasKnownDrafts: false })

    expect(wrapper.get('[data-navigator-loading]').attributes('aria-label')).toBe('Carregando drafts')
    expect(wrapper.findAll('[data-slot="skeleton"]')).toHaveLength(3)
    expect(wrapper.find('[role="alert"]').exists()).toBe(false)
    expect(wrapper.find('[data-navigator-empty]').exists()).toBe(false)
  })

  it('keeps known drafts visible with nonblocking loading feedback and no skeleton', () => {
    const wrapper = mountNavigator({ loading: true })

    expect(wrapper.get('[data-draft-id="draft-1"]').text()).toContain(longName)
    expect(wrapper.get('[data-navigator-feedback="loading"]').text()).toContain('Atualizando lista de drafts')
    expect(wrapper.find('[data-slot="skeleton"]').exists()).toBe(false)
  })

  it('keeps known drafts visible with nonblocking failure feedback and emits retry', async () => {
    const wrapper = mountNavigator({ loadFailed: true })

    expect(wrapper.get('[data-draft-id="draft-1"]').text()).toContain(longName)
    expect(wrapper.get('[data-navigator-feedback="error"][role="alert"]').text()).toContain('Não foi possível atualizar os drafts')
    await wrapper.get('[data-navigator-retry]').trigger('click')

    expect(wrapper.emitted('retry')).toHaveLength(1)
    expect(wrapper.find('[data-navigator-empty]').exists()).toBe(false)
  })

  it('shows blocking failure and retry when no draft data is known', async () => {
    const wrapper = mountNavigator({ drafts: [], hasKnownDrafts: false, loadFailed: true })

    expect(wrapper.get('[data-navigator-load-failure][role="alert"]').text()).toContain('Não foi possível carregar os drafts')
    await wrapper.get('[data-navigator-retry]').trigger('click')

    expect(wrapper.emitted('retry')).toHaveLength(1)
    expect(wrapper.find('[data-navigator-empty]').exists()).toBe(false)
  })

  it('distinguishes filtered zero results and emits clear filters without creation', async () => {
    const wrapper = mountNavigator({ drafts: [], hasKnownDrafts: true, searchTerm: 'inexistente' })

    expect(wrapper.get('[data-navigator-no-results]').text()).toContain('Nenhum draft corresponde aos filtros')
    expect(wrapper.find('[data-navigator-create]').exists()).toBe(false)
    await wrapper.get('[data-navigator-clear-results]').trigger('click')

    expect(wrapper.emitted('reset')).toHaveLength(1)
  })

  it('reveals filtered no-results only after loading and failure transitions settle successfully', async () => {
    const wrapper = mountNavigator({ drafts: [], hasKnownDrafts: true, selectedStatus: 'Cancelada', loading: true })

    expect(wrapper.get('[data-navigator-feedback="loading"]')).toBeTruthy()
    expect(wrapper.find('[data-navigator-no-results]').exists()).toBe(false)

    await wrapper.setProps({ loading: false, loadFailed: true })
    expect(wrapper.get('[data-navigator-feedback="error"]')).toBeTruthy()
    expect(wrapper.find('[data-navigator-no-results]').exists()).toBe(false)

    await wrapper.setProps({ loadFailed: false })
    expect(wrapper.find('[data-navigator-feedback]').exists()).toBe(false)
    expect(wrapper.get('[data-navigator-no-results]')).toBeTruthy()
  })

  it('offers creation only for a genuinely empty authorized collection', async () => {
    const authorized = mountNavigator({ drafts: [], hasKnownDrafts: false })
    const unauthorized = mountNavigator({ drafts: [], hasKnownDrafts: false, canCreate: false })

    await authorized.get('[data-navigator-create]').trigger('click')
    expect(authorized.emitted('create')).toHaveLength(1)
    expect(unauthorized.find('[data-navigator-create]').exists()).toBe(false)
    expect(unauthorized.get('[data-navigator-empty]').text()).toContain('Ainda não há drafts disponíveis')
  })

  it.each([
    ['PresencaAberta', 'info'],
    ['PresencaEncerrada', 'warning'],
    ['CapitaesDefinidos', 'warning'],
    ['OrdemDefinida', 'info'],
    ['Aberta', 'info'],
    ['Finalizada', 'success'],
    ['Cancelada', 'danger'],
    ['EstadoLegado', 'neutral'],
  ] as const)('uses the semantic %s status variant %s', (status, variant) => {
    const wrapper = mountNavigator({ drafts: [{ ...baseDraft, status }] })
    const badge = wrapper.get('[data-draft-status]')

    expect(badge.attributes('data-variant')).toBe(variant)
    expect(badge.classes()).toContain('team-status')
    expect(badge.classes()).toContain(`draft-navigator__status--${variant}`)
  })

  it('emits reset and keeps compact expansion and long-name presentation inside the child', async () => {
    const wrapper = mountNavigator({ searchTerm: 'domingo', selectedStatus: 'Aberta' })

    expect(wrapper.get('[data-draft-name]').text()).toBe(longName)
    expect(wrapper.get('[data-navigator-toggle]').attributes('aria-expanded')).toBe('false')
    expect(wrapper.get('[data-testid="draft-navigator"]').attributes('data-compact-expanded')).toBe('false')

    await wrapper.get('[data-navigator-toggle]').trigger('click')
    expect(wrapper.get('[data-navigator-toggle]').attributes('aria-expanded')).toBe('true')
    expect(wrapper.get('[data-testid="draft-navigator"]').attributes('data-compact-expanded')).toBe('true')

    await wrapper.get('[data-navigator-reset]').trigger('click')
    expect(wrapper.emitted('reset')).toHaveLength(1)
  })

  it('does not import services or authentication', () => {
    expect(DraftNavigatorSource).not.toMatch(/@\/services\//)
    expect(DraftNavigatorSource).not.toContain('useAuthState')
  })

  it('uses the data font token without an alias local to the component', () => {
    expect(DraftNavigatorSource).toContain('font-family: var(--font-data)')
    expect(DraftNavigatorSource).not.toContain('font-family: var(--font-mono)')
  })

  it('keeps a named navigation region horizontal on tablet and compact on mobile', () => {
    const wrapper = mountNavigator()
    const navigator = wrapper.get('nav')

    expect(navigator.attributes('aria-label')).toBe('Lista de drafts')
    expect(navigator.get('[aria-controls="draft-navigator-list"]')).toBeTruthy()
    expect(MainCss).toMatch(/@media \(min-width:\s*769px\) and \(max-width:\s*1024px\)[\s\S]*?\.drafts-page\s+\.draft-navigator__list\s*{[^}]*grid-auto-flow:\s*column/s)
    expect(MainCss).toMatch(/@media \(max-width:\s*768px\)[\s\S]*?\.drafts-page\s+\.draft-navigator\[data-compact-expanded='false'\]/s)
  })
})
