// @vitest-environment happy-dom
import { enableAutoUnmount, flushPromises, mount } from '@vue/test-utils'
import type { VueWrapper } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { nextTick } from 'vue'

import { i18n, setLocale } from '@/i18n'
import * as competitionService from '@/services/competitions'
import * as seasonService from '@/services/seasons'

import CompetitionPanel from './CompetitionPanel.vue'
import SeasonFormDrawer from './SeasonFormDrawer.vue'
import SeasonScopeSelector from './SeasonScopeSelector.vue'
import SeasonTransitionDialog from './SeasonTransitionDialog.vue'
import SeasonsView from '../../views/SeasonsView.vue'

enableAutoUnmount(afterEach)

vi.mock('@/services/seasons', async (importOriginal) => ({
  ...(await importOriginal<Record<string, unknown>>()),
  listSeasons: vi.fn(),
  getSeason: vi.fn(),
  createSeason: vi.fn(),
  updateSeason: vi.fn(),
  activateSeason: vi.fn(),
  closeSeason: vi.fn(),
}))

vi.mock('@/services/competitions', async (importOriginal) => ({
  ...(await importOriginal<Record<string, unknown>>()),
  listCompetitionsForSeason: vi.fn(),
  createCompetition: vi.fn(),
  updateCompetition: vi.fn(),
  createRound: vi.fn(),
  reorderRounds: vi.fn(),
  publishCompetitionRules: vi.fn(),
  publishSeasonRules: vi.fn(),
}))

vi.mock('vue-sonner', () => ({
  toast: { error: vi.fn(), success: vi.fn() },
}))

type SeasonState = 'Planejada' | 'Ativa' | 'Encerrada'

interface SeasonSummary {
  id: string
  nome: string
  ano: number
  ordemNoAno: number
  dataInicio: string
  dataFimExclusiva: string
  estado: SeasonState
  versao: number
}

interface SeasonDetail extends SeasonSummary {
  quantidadeCompeticoes: number
  ativadaEm: string | null
  encerradaEm: string | null
  acoesPermitidas: string[]
}

interface Round {
  id: string
  competicaoId: string
  nome: string
  ordem: number
  versao: number
}

interface CompetitionDetail {
  id: string
  seasonId: string
  nome: string
  codigo: string
  circuitoDiario: boolean
  rodadas: Round[]
  regrasPublicadas: Array<{
    id: string
    seasonId: string
    competicaoId: string | null
    numero: number
    formato: 'Md3' | 'Md5'
    modoDraft: 'Padrao' | 'Fearless'
    publicadaEm: string
  }>
  versao: number
  acoesPermitidas: string[]
}

const activeSeason: SeasonSummary = {
  id: '00000000-0000-4000-8000-000000000001',
  nome: 'Rinha 2026',
  ano: 2026,
  ordemNoAno: 1,
  dataInicio: '2026-01-01',
  dataFimExclusiva: '2027-01-01',
  estado: 'Ativa',
  versao: 1,
}
const plannedSeason: SeasonSummary = {
  id: '00000000-0000-4000-8000-000000000002',
  nome: 'Rinha 2027',
  ano: 2027,
  ordemNoAno: 1,
  dataInicio: '2027-01-01',
  dataFimExclusiva: '2028-01-01',
  estado: 'Planejada',
  versao: 1,
}
const closedSeason: SeasonSummary = {
  id: '00000000-0000-4000-8000-000000000003',
  nome: 'Rinha 2025',
  ano: 2025,
  ordemNoAno: 1,
  dataInicio: '2025-01-01',
  dataFimExclusiva: '2026-01-01',
  estado: 'Encerrada',
  versao: 1,
}

function seasonDetail(
  season: SeasonSummary,
  acoesPermitidas: string[],
): SeasonDetail {
  return {
    ...season,
    quantidadeCompeticoes: 0,
    ativadaEm: season.estado === 'Ativa' ? '2026-01-01T12:00:00Z' : null,
    encerradaEm: season.estado === 'Encerrada' ? '2026-01-01T12:00:00Z' : null,
    acoesPermitidas,
  }
}

function seasonPage(
  items: SeasonSummary[],
  overrides: Partial<{
    calendarioConfigurado: boolean
    temporadaAtual: SeasonSummary | null
    versaoCalendario: number
  }> = {},
) {
  return {
    page: 1,
    pageSize: 20,
    items,
    totalItems: items.length,
    totalPages: items.length > 0 ? 1 : 0,
    calendarioConfigurado: true,
    temporadaAtual: activeSeason,
    versaoCalendario: 1,
    ...overrides,
  }
}

function observedResponse<T>(data: T, etag: string) {
  return { data, etag }
}

function competitionPage(
  items: CompetitionDetail[],
  seasonsIncluidas: SeasonSummary[] = [activeSeason],
) {
  return {
    page: 1,
    pageSize: 20,
    items,
    totalItems: items.length,
    totalPages: items.length > 0 ? 1 : 0,
    calendarioConfigurado: true,
    temporadaAtual: activeSeason,
    seasonsIncluidas,
  }
}

function makeCompetition(
  overrides: Partial<CompetitionDetail> = {},
): CompetitionDetail {
  const id = overrides.id ?? '00000000-0000-4000-8000-000000000010'
  const seasonId = overrides.seasonId ?? activeSeason.id
  const round: Round = {
    id: '00000000-0000-4000-8000-000000000011',
    competicaoId: id,
    nome: 'Rodada de abertura',
    ordem: 1,
    versao: 1,
  }

  return {
    id,
    seasonId,
    nome: 'Circuito principal',
    codigo: 'PRINCIPAL',
    circuitoDiario: true,
    rodadas: [round],
    regrasPublicadas: [],
    versao: 1,
    acoesPermitidas: ['edit', 'create-round', 'publish-rules'],
    ...overrides,
  }
}

function globalMountOptions() {
  return {
    plugins: [i18n],
    stubs: {
      teleport: { template: '<div data-teleport-stub><slot /></div>' },
      transition: false,
    },
  }
}

function getButtonByName(wrapper: VueWrapper, name: string) {
  const button = wrapper.findAll('button').find((candidate) => {
    const accessibleName =
      candidate.attributes('aria-label') || candidate.text()
    return accessibleName.trim() === name
  })
  expect(button, `button named "${name}"`).toBeDefined()
  return button!
}

async function mountView() {
  const wrapper = mount(SeasonsView, {
    attachTo: document.body,
    global: globalMountOptions(),
  })
  await flushPromises()
  return wrapper
}

describe('season scope URL serialization', () => {
  it('keeps the observed calendar ETag outside the OpenAPI SeasonPage body', () => {
    const response = observedResponse(
      seasonPage([activeSeason], { versaoCalendario: 37 }),
      '"calendar-opaque-37"',
    )

    expect(response.etag).toBe('"calendar-opaque-37"')
    expect(response.data.versaoCalendario).toBe(37)
    expect(response.data).not.toHaveProperty('etag')
    expect(response.data).not.toHaveProperty('calendarioEtag')
  })

  it('omits both seasonal parameters for the current scope', () => {
    const params = seasonService.serializeSeasonScope({ mode: 'current' })

    expect([...params.entries()]).toEqual([])
  })

  it('serializes selected Seasons as repeated temporadaIds', () => {
    const params = seasonService.serializeSeasonScope({
      mode: 'selected',
      seasonIds: [activeSeason.id, closedSeason.id],
    })

    expect(params.getAll('temporadaIds')).toEqual([
      activeSeason.id,
      closedSeason.id,
    ])
    expect(params.has('todas')).toBe(false)
  })

  it('serializes all exclusively as todas=true', () => {
    const params = seasonService.serializeSeasonScope({ mode: 'all' })

    expect(params.get('todas')).toBe('true')
    expect(params.has('temporadaIds')).toBe(false)
  })
})

describe('SeasonScopeSelector', () => {
  it('implements an accessible keyboard radio group for current, selected and all', async () => {
    const wrapper = mount(SeasonScopeSelector, {
      attachTo: document.body,
      props: {
        seasons: [activeSeason, closedSeason],
        modelValue: { mode: 'current' },
        calendarConfigured: true,
        loading: false,
      },
      global: globalMountOptions(),
    })
    const group = wrapper.get('[role="radiogroup"]')
    const radios = group.findAll('[role="radio"]')

    expect(group.attributes('aria-label')).toBe('Recorte de temporadas')
    expect(radios).toHaveLength(3)
    expect(radios.map((radio) => radio.text().trim())).toEqual([
      'Temporada atual',
      'Temporadas selecionadas',
      'Todas as temporadas',
    ])
    expect(radios.map((radio) => radio.attributes('aria-checked'))).toEqual([
      'true',
      'false',
      'false',
    ])
    expect(radios.map((radio) => radio.attributes('tabindex'))).toEqual([
      '0',
      '-1',
      '-1',
    ])
    ;(radios[0]!.element as HTMLElement).focus()
    await radios[0]!.trigger('keydown', { key: 'ArrowRight' })
    expect(document.activeElement).toBe(radios[1]!.element)
    expect(radios[1]!.attributes('aria-checked')).toBe('true')
    expect(wrapper.emitted('update:modelValue')).toBeUndefined()

    const seasonOptions = wrapper.findAll('input[type="checkbox"]')
    expect(seasonOptions).toHaveLength(2)
    for (const option of seasonOptions) {
      expect(option.attributes('id')).toBeTruthy()
      expect(
        wrapper.get(`label[for="${option.attributes('id')}"]`).text(),
      ).toBeTruthy()
    }
    expect(
      getButtonByName(wrapper, 'Aplicar seleção').attributes('disabled'),
    ).toBeDefined()

    await seasonOptions[0]!.setValue(true)
    await seasonOptions[1]!.setValue(true)
    await getButtonByName(wrapper, 'Aplicar seleção').trigger('click')
    expect(wrapper.emitted('update:modelValue')).toEqual([
      [
        {
          mode: 'selected',
          seasonIds: [activeSeason.id, closedSeason.id],
        },
      ],
    ])

    await radios[2]!.trigger('keydown', { key: ' ' })
    const updates = wrapper.emitted('update:modelValue') ?? []
    expect(updates[updates.length - 1]).toEqual([{ mode: 'all' }])
  })

  it('keeps current selected and announces an unconfigured calendar without fallback', () => {
    const wrapper = mount(SeasonScopeSelector, {
      props: {
        seasons: [closedSeason],
        modelValue: { mode: 'current' },
        calendarConfigured: false,
      },
      global: globalMountOptions(),
    })

    expect(wrapper.get('[role="radio"][aria-checked="true"]').text()).toContain(
      'Temporada atual',
    )
    expect(wrapper.get('[role="status"]').attributes('aria-live')).toBe(
      'polite',
    )
    expect(wrapper.get('[role="status"]').text()).toContain(
      'Nenhuma temporada ativa está configurada.',
    )
    expect(wrapper.emitted('update:modelValue')).toBeUndefined()
  })

  it('localizes visible labels and accessible names in English', () => {
    setLocale('en')
    const wrapper = mount(SeasonScopeSelector, {
      props: {
        seasons: [activeSeason],
        modelValue: { mode: 'all' },
        calendarConfigured: true,
      },
      global: globalMountOptions(),
    })

    expect(wrapper.get('[role="radiogroup"]').attributes('aria-label')).toBe(
      'Season scope',
    )
    expect(
      wrapper.findAll('[role="radio"]').map((radio) => radio.text().trim()),
    ).toEqual(['Current season', 'Selected seasons', 'All seasons'])
    expect(wrapper.text()).not.toMatch(
      /Recorte de temporadas|Temporada atual|Todas as temporadas/,
    )
  })
})

describe('Season forms and transitions', () => {
  it.each([
    {
      locale: 'pt',
      labels: [
        'Nome da temporada',
        'Ano',
        'Ordem no ano',
        'Data de início',
        'Data final exclusiva',
      ],
      errors: [
        'Informe o nome da temporada.',
        'Informe um ano entre 2009 e 9999.',
        'A ordem deve ser maior que zero.',
        'Informe a data de início.',
        'Informe a data final exclusiva.',
      ],
      exclusiveHelp: 'não faz parte da temporada',
    },
    {
      locale: 'en',
      labels: [
        'Season name',
        'Year',
        'Order within year',
        'Start date',
        'Exclusive end date',
      ],
      errors: [
        'Enter the season name.',
        'Enter a year between 2009 and 9999.',
        'Order must be greater than zero.',
        'Enter the start date.',
        'Enter the exclusive end date.',
      ],
      exclusiveHelp: 'is not part of the season',
    },
  ] as const)(
    'validates every required Season field accessibly in $locale',
    async ({ locale, labels, errors, exclusiveHelp }) => {
      setLocale(locale)
      const wrapper = mount(SeasonFormDrawer, {
        attachTo: document.body,
        props: {
          open: true,
          mode: 'create',
          season: null,
          saving: false,
          fieldErrors: {},
          serviceMessageCode: null,
        },
        global: globalMountOptions(),
      })
      const fields = [
        ['season-name', 'season-name-error'],
        ['season-year', 'season-year-error'],
        ['season-order', 'season-order-error'],
        ['season-start-date', 'season-start-date-error'],
        ['season-exclusive-end-date', 'season-exclusive-end-date-error'],
      ] as const

      expect(
        wrapper.get('[role="dialog"]').attributes('aria-labelledby'),
      ).toBeTruthy()
      expect(wrapper.find('[name="estado"]').exists()).toBe(false)
      expect(wrapper.get('[data-exclusive-end-help]').text()).toContain(
        exclusiveHelp,
      )
      expect(
        fields.map(([fieldId]) =>
          wrapper.get(`label[for="${fieldId}"]`).text().trim(),
        ),
      ).toEqual(labels)

      await wrapper.get('form').trigger('submit')
      await nextTick()

      fields.forEach(([fieldId, errorId], index) => {
        const field = wrapper.get(`#${fieldId}`)
        expect(field.attributes('aria-invalid')).toBe('true')
        expect(field.attributes('aria-errormessage')).toBe(errorId)
        expect(field.attributes('aria-describedby')).toContain(errorId)
        expect(wrapper.get(`#${errorId}`).text()).toBe(errors[index])
      })
      const name = wrapper.get('#season-name')
      expect(name.attributes('autocomplete')).toBe('off')
      expect(document.activeElement).toBe(name.element)
      expect(wrapper.emitted('submit')).toBeUndefined()
    },
  )

  it('trims and emits the complete Season create payload', async () => {
    const wrapper = mount(SeasonFormDrawer, {
      props: {
        open: true,
        mode: 'create',
        season: null,
        saving: false,
        fieldErrors: {},
        serviceMessageCode: null,
      },
      global: globalMountOptions(),
    })

    await wrapper.get('#season-name').setValue('  Rinha 2027  ')
    await wrapper.get('#season-year').setValue('2027')
    await wrapper.get('#season-order').setValue('1')
    await wrapper.get('#season-start-date').setValue('2027-01-01')
    await wrapper.get('#season-exclusive-end-date').setValue('2028-01-01')
    await wrapper.get('form').trigger('submit')

    expect(wrapper.emitted('submit')?.[0]).toEqual([
      {
        nome: 'Rinha 2027',
        ano: 2027,
        ordemNoAno: 1,
        dataInicio: '2027-01-01',
        dataFimExclusiva: '2028-01-01',
      },
    ])
  })

  it('describes activation consequences and emits only explicit confirmation', async () => {
    const wrapper = mount(SeasonTransitionDialog, {
      attachTo: document.body,
      props: {
        open: true,
        action: 'activate',
        season: plannedSeason,
        currentSeason: activeSeason,
        calendarVersion: 7,
      },
      global: globalMountOptions(),
    })

    const dialog = wrapper.get('[role="alertdialog"]')
    expect(dialog.attributes('aria-labelledby')).toBeTruthy()
    expect(dialog.attributes('aria-describedby')).toBeTruthy()
    expect(dialog.text()).toContain('Rinha 2026')
    expect(dialog.text()).toContain('será encerrada')

    await getButtonByName(wrapper, 'Ativar temporada').trigger('click')
    expect(wrapper.emitted('confirm')).toEqual([[]])
  })

  it.each([
    {
      locale: 'pt',
      consequence: 'novas séries oficiais não poderão ser confirmadas',
      confirm: 'Encerrar temporada',
    },
    {
      locale: 'en',
      consequence: 'new official series can no longer be confirmed',
      confirm: 'Close season',
    },
  ] as const)(
    'localizes the close consequence and emits confirmation in $locale',
    async ({ locale, consequence, confirm }) => {
      setLocale(locale)
      const wrapper = mount(SeasonTransitionDialog, {
        attachTo: document.body,
        props: {
          open: true,
          action: 'close',
          season: activeSeason,
          currentSeason: activeSeason,
          calendarVersion: 7,
        },
        global: globalMountOptions(),
      })

      const dialog = wrapper.get('[role="alertdialog"]')
      expect(dialog.attributes('aria-labelledby')).toBeTruthy()
      expect(dialog.attributes('aria-describedby')).toBeTruthy()
      expect(dialog.text()).toContain(consequence)

      await getButtonByName(wrapper, confirm).trigger('click')
      expect(wrapper.emitted('confirm')).toEqual([[]])
    },
  )
})

describe('CompetitionPanel forms', () => {
  it.each([
    ['pt', 'Informe o nome da competição.', 'Informe o código da competição.'],
    ['en', 'Enter the competition name.', 'Enter the competition code.'],
  ] as const)(
    'provides accessible localized competition validation in %s',
    async (locale, nameError, codeError) => {
      setLocale(locale)
      const wrapper = mount(CompetitionPanel, {
        attachTo: document.body,
        props: {
          season: activeSeason,
          competitions: [],
          loading: false,
          saving: false,
          serviceMessageCode: null,
        },
        global: globalMountOptions(),
      })

      await getButtonByName(
        wrapper,
        locale === 'pt' ? 'Criar competição' : 'Create competition',
      ).trigger('click')
      await wrapper.get('#competition-form').trigger('submit')
      await nextTick()

      const name = wrapper.get('#competition-name')
      expect(wrapper.get('label[for="competition-name"]').text()).toBeTruthy()
      expect(wrapper.get('label[for="competition-code"]').text()).toBeTruthy()
      expect(name.attributes()).toMatchObject({
        'aria-invalid': 'true',
        'aria-errormessage': 'competition-name-error',
      })
      expect(wrapper.get('#competition-name-error').text()).toBe(nameError)
      expect(wrapper.get('#competition-code-error').text()).toBe(codeError)
      expect(document.activeElement).toBe(name.element)
      expect(wrapper.emitted('createCompetition')).toBeUndefined()
    },
  )

  it.each([
    ['pt', 'Informe o nome da rodada.', 'A ordem deve ser maior que zero.'],
    ['en', 'Enter the round name.', 'Order must be greater than zero.'],
  ] as const)(
    'provides accessible localized round validation in %s',
    async (locale, nameError, orderError) => {
      setLocale(locale)
      const competition = makeCompetition({ rodadas: [] })
      const wrapper = mount(CompetitionPanel, {
        attachTo: document.body,
        props: {
          season: activeSeason,
          competitions: [competition],
          loading: false,
          saving: false,
          serviceMessageCode: null,
        },
        global: globalMountOptions(),
      })

      await getButtonByName(
        wrapper,
        locale === 'pt' ? 'Criar rodada' : 'Create round',
      ).trigger('click')
      await wrapper.get('#round-order').setValue('0')
      await wrapper.get('#round-form').trigger('submit')
      await nextTick()

      const name = wrapper.get('#round-name')
      expect(wrapper.get('label[for="round-name"]').text()).toBeTruthy()
      expect(wrapper.get('label[for="round-order"]').text()).toBeTruthy()
      expect(name.attributes()).toMatchObject({
        'aria-invalid': 'true',
        'aria-errormessage': 'round-name-error',
      })
      expect(wrapper.get('#round-name-error').text()).toBe(nameError)
      expect(wrapper.get('#round-order-error').text()).toBe(orderError)
      expect(document.activeElement).toBe(name.element)
      expect(wrapper.emitted('createRound')).toBeUndefined()
    },
  )
})

describe('Season management orchestration', () => {
  beforeEach(() => {
    vi.resetAllMocks()
    vi.mocked(seasonService.listSeasons).mockResolvedValue(
      observedResponse(
        seasonPage([activeSeason, plannedSeason]),
        '"calendar-observed"',
      ),
    )
    vi.mocked(seasonService.getSeason).mockImplementation(
      async (seasonId: string) =>
        observedResponse(
          seasonId === plannedSeason.id
            ? seasonDetail(plannedSeason, ['edit', 'activate'])
            : seasonDetail(activeSeason, ['edit', 'close']),
          `"season-${seasonId}"`,
        ),
    )
    vi.mocked(competitionService.listCompetitionsForSeason).mockResolvedValue(
      competitionPage([]),
    )
  })

  it.each([
    {
      locale: 'pt',
      select: 'Selecionar temporada Rinha 2027',
      activate: 'Ativar temporada',
      confirm: 'Confirmar ativação',
      consequence: 'será encerrada',
      active: 'Ativa',
    },
    {
      locale: 'en',
      select: 'Select season Rinha 2027',
      activate: 'Activate season',
      confirm: 'Confirm activation',
      consequence: 'will be closed',
      active: 'Active',
    },
  ] as const)(
    'activates with the observed calendar ETag in $locale',
    async ({ locale, select, activate, confirm, consequence, active }) => {
      setLocale(locale)
      const activatedTarget: SeasonSummary = {
        ...plannedSeason,
        estado: 'Ativa',
        versao: 2,
      }
      let currentResponse = observedResponse(
        seasonPage([activeSeason, plannedSeason], { versaoCalendario: 41 }),
        '"calendar-opaque-activate"',
      )
      vi.mocked(seasonService.listSeasons).mockImplementation(
        async () => currentResponse,
      )
      vi.mocked(seasonService.activateSeason).mockImplementation(async () => {
        currentResponse = observedResponse(
          seasonPage([activeSeason, activatedTarget], {
            temporadaAtual: activatedTarget,
            versaoCalendario: 42,
          }),
          '"calendar-after-activate"',
        )
        return seasonDetail(activatedTarget, ['close'])
      })
      const wrapper = await mountView()

      await getButtonByName(wrapper, select).trigger('click')
      await flushPromises()
      await getButtonByName(wrapper, activate).trigger('click')
      expect(
        wrapper.getComponent(SeasonTransitionDialog).props('calendarVersion'),
      ).toBe(41)
      expect(wrapper.get('[role="alertdialog"]').text()).toContain('Rinha 2026')
      expect(wrapper.get('[role="alertdialog"]').text()).toContain(consequence)
      await getButtonByName(wrapper, confirm).trigger('click')
      await flushPromises()

      expect(seasonService.activateSeason).toHaveBeenCalledWith(
        plannedSeason.id,
        '"calendar-opaque-activate"',
      )
      expect(
        wrapper.get(`[data-season-id="${plannedSeason.id}"]`).text(),
      ).toContain(active)
    },
  )

  it.each([
    {
      locale: 'pt',
      select: 'Selecionar temporada Rinha 2027',
      activate: 'Ativar temporada',
      confirm: 'Confirmar ativação',
      conflict: 'O calendário mudou. Revise os dados e confirme novamente.',
      planned: 'Planejada',
      active: 'Ativa',
    },
    {
      locale: 'en',
      select: 'Select season Rinha 2027',
      activate: 'Activate season',
      confirm: 'Confirm activation',
      conflict: 'The calendar changed. Review the data and confirm again.',
      planned: 'Planned',
      active: 'Active',
    },
  ] as const)(
    'reloads a new ETag after 409 and requires reconfirmation in $locale',
    async ({
      locale,
      select,
      activate,
      confirm,
      conflict,
      planned,
      active,
    }) => {
      setLocale(locale)
      const refreshedCurrent: SeasonSummary = {
        ...activeSeason,
        nome: 'Rinha 2026 atualizada',
        versao: 2,
      }
      const activatedTarget: SeasonSummary = {
        ...plannedSeason,
        estado: 'Ativa',
        versao: 2,
      }
      let currentResponse = observedResponse(
        seasonPage([activeSeason, plannedSeason]),
        '"calendar-before-conflict"',
      )
      let firstAttempt = true
      vi.mocked(seasonService.listSeasons).mockImplementation(
        async () => currentResponse,
      )
      vi.mocked(seasonService.activateSeason).mockImplementation(async () => {
        if (firstAttempt) {
          firstAttempt = false
          currentResponse = observedResponse(
            seasonPage([refreshedCurrent, plannedSeason], {
              temporadaAtual: refreshedCurrent,
              versaoCalendario: 2,
            }),
            '"calendar-after-conflict"',
          )
          throw new seasonService.SeasonServiceError(409, 'MC103')
        }

        currentResponse = observedResponse(
          seasonPage([refreshedCurrent, activatedTarget], {
            temporadaAtual: activatedTarget,
            versaoCalendario: 3,
          }),
          '"calendar-after-activate"',
        )
        return seasonDetail(activatedTarget, ['close'])
      })
      const wrapper = await mountView()

      await getButtonByName(wrapper, select).trigger('click')
      await flushPromises()
      await getButtonByName(wrapper, activate).trigger('click')
      await getButtonByName(wrapper, confirm).trigger('click')
      await flushPromises()

      expect(seasonService.activateSeason).toHaveBeenNthCalledWith(
        1,
        plannedSeason.id,
        '"calendar-before-conflict"',
      )
      expect(seasonService.activateSeason).toHaveBeenCalledTimes(1)
      expect(wrapper.get('[role="alert"]').text()).toContain(conflict)
      expect(wrapper.text()).toContain('Rinha 2026 atualizada')
      expect(wrapper.find('[role="alertdialog"]').exists()).toBe(false)
      const targetBeforeReconfirmation = wrapper.get(
        `[data-season-id="${plannedSeason.id}"]`,
      )
      expect(targetBeforeReconfirmation.text()).toContain(planned)
      expect(targetBeforeReconfirmation.text()).not.toContain(active)
      await flushPromises()
      expect(seasonService.activateSeason).toHaveBeenCalledTimes(1)

      await getButtonByName(wrapper, activate).trigger('click')
      expect(
        wrapper.getComponent(SeasonTransitionDialog).props('calendarVersion'),
      ).toBe(2)
      await getButtonByName(wrapper, confirm).trigger('click')
      await flushPromises()

      expect(seasonService.activateSeason).toHaveBeenNthCalledWith(
        2,
        plannedSeason.id,
        '"calendar-after-conflict"',
      )
      expect(
        wrapper.get(`[data-season-id="${plannedSeason.id}"]`).text(),
      ).toContain(active)
      expect(wrapper.find('[role="alert"]').exists()).toBe(false)
    },
  )

  it('closes the active Season with the observed calendar ETag', async () => {
    const closedTarget: SeasonSummary = {
      ...activeSeason,
      estado: 'Encerrada',
      versao: 2,
    }
    let currentResponse = observedResponse(
      seasonPage([activeSeason], { versaoCalendario: 17 }),
      '"calendar-opaque-close"',
    )
    vi.mocked(seasonService.listSeasons).mockImplementation(
      async () => currentResponse,
    )
    vi.mocked(seasonService.closeSeason).mockImplementation(async () => {
      currentResponse = observedResponse(
        seasonPage([closedTarget], {
          temporadaAtual: null,
          versaoCalendario: 18,
        }),
        '"calendar-after-close"',
      )
      return seasonDetail(closedTarget, [])
    })
    const wrapper = await mountView()

    await getButtonByName(wrapper, 'Selecionar temporada Rinha 2026').trigger(
      'click',
    )
    await flushPromises()
    await getButtonByName(wrapper, 'Encerrar temporada').trigger('click')
    expect(
      wrapper.getComponent(SeasonTransitionDialog).props('calendarVersion'),
    ).toBe(17)
    await getButtonByName(wrapper, 'Confirmar encerramento').trigger('click')
    await flushPromises()

    expect(seasonService.closeSeason).toHaveBeenCalledWith(
      activeSeason.id,
      '"calendar-opaque-close"',
    )
    expect(
      wrapper.get(`[data-season-id="${activeSeason.id}"]`).text(),
    ).toContain('Encerrada')
  })

  it('renders each successful Season to competition to round result', async () => {
    const createdSeason: SeasonSummary = {
      ...plannedSeason,
      id: '00000000-0000-4000-8000-000000000020',
      nome: 'Circuito 2027',
    }
    const competition = makeCompetition({
      id: '00000000-0000-4000-8000-000000000021',
      seasonId: createdSeason.id,
      nome: 'Circuito diário',
      codigo: 'DIARIO',
      rodadas: [],
    })
    const round: Round = {
      id: '00000000-0000-4000-8000-000000000022',
      competicaoId: competition.id,
      nome: 'Rodada 1',
      ordem: 1,
      versao: 1,
    }
    let seasons = [activeSeason]
    let competitions: CompetitionDetail[] = []
    vi.mocked(seasonService.listSeasons).mockImplementation(async () =>
      observedResponse(seasonPage(seasons), '"calendar-workflow"'),
    )
    vi.mocked(seasonService.getSeason).mockImplementation(
      async (seasonId: string) => {
        const season = seasons.find(({ id }) => id === seasonId)!
        return observedResponse(
          seasonDetail(
            season,
            season.id === createdSeason.id ? ['edit'] : ['edit', 'close'],
          ),
          `"season-${season.id}"`,
        )
      },
    )
    vi.mocked(seasonService.createSeason).mockImplementation(async () => {
      seasons = [...seasons, createdSeason]
      return seasonDetail(createdSeason, ['edit'])
    })
    vi.mocked(competitionService.listCompetitionsForSeason).mockImplementation(
      async () => competitionPage(competitions, [createdSeason]),
    )
    vi.mocked(competitionService.createCompetition).mockImplementation(
      async () => {
        competitions = [competition]
        return competition
      },
    )
    vi.mocked(competitionService.createRound).mockImplementation(async () => {
      competitions = [{ ...competition, rodadas: [round] }]
      return round
    })
    const wrapper = await mountView()

    await getButtonByName(wrapper, 'Criar temporada').trigger('click')
    await wrapper.get('#season-name').setValue('Circuito 2027')
    await wrapper.get('#season-year').setValue('2027')
    await wrapper.get('#season-order').setValue('1')
    await wrapper.get('#season-start-date').setValue('2027-01-01')
    await wrapper.get('#season-exclusive-end-date').setValue('2028-01-01')
    await wrapper.get('#season-form').trigger('submit')
    await flushPromises()
    expect(seasonService.createSeason).toHaveBeenCalledWith({
      nome: 'Circuito 2027',
      ano: 2027,
      ordemNoAno: 1,
      dataInicio: '2027-01-01',
      dataFimExclusiva: '2028-01-01',
    })
    expect(wrapper.text()).toContain('Circuito 2027')

    await getButtonByName(
      wrapper,
      'Selecionar temporada Circuito 2027',
    ).trigger('click')
    await flushPromises()
    await getButtonByName(wrapper, 'Criar competição').trigger('click')
    await wrapper.get('#competition-name').setValue('Circuito diário')
    await wrapper.get('#competition-code').setValue('DIARIO')
    await wrapper.get('#competition-daily-circuit').setValue(true)
    await wrapper.get('#competition-form').trigger('submit')
    await flushPromises()
    expect(competitionService.createCompetition).toHaveBeenCalledWith(
      createdSeason.id,
      {
        nome: 'Circuito diário',
        codigo: 'DIARIO',
        circuitoDiario: true,
      },
    )
    expect(wrapper.text()).toContain('Circuito diário')

    await getButtonByName(wrapper, 'Criar rodada').trigger('click')
    await wrapper.get('#round-name').setValue('Rodada 1')
    await wrapper.get('#round-order').setValue('1')
    await wrapper.get('#round-form').trigger('submit')
    await flushPromises()
    expect(competitionService.createRound).toHaveBeenCalledWith(
      competition.id,
      {
        nome: 'Rodada 1',
        ordem: 1,
      },
    )
    expect(wrapper.text()).toContain('Rodada 1')
  })
})

afterEach(() => {
  document.body.innerHTML = ''
  setLocale('pt')
  vi.unstubAllGlobals()
})
