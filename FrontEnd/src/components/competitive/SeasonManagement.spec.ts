// @vitest-environment happy-dom
import { enableAutoUnmount, flushPromises, mount } from '@vue/test-utils'
import type { VueWrapper } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { nextTick } from 'vue'

import { AuthRoles } from '@/constants/authRoles'
import { i18n, setLocale } from '@/i18n'
import { setPermissions } from '@/services/authState'
import * as competitionService from '@/services/competitions'
import * as seasonService from '@/services/seasons'
import type { UserPermissions } from '@/types/auth'
import type { CompetitionDetail, Round } from '@/types/competition'
import type {
  ObservedResponse,
  SeasonDetail,
  SeasonSummary,
} from '@/types/season'

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
  listCompetitions: vi.fn(),
  listCompetitionsForSeason: vi.fn(),
  getCompetition: vi.fn(),
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
    page: number
    pageSize: number
    totalItems: number
    totalPages: number
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

function observedResponse<T>(
  data: T,
  etag: string | null,
): ObservedResponse<T> {
  return { data, etag }
}

function deferred<T>() {
  let resolve!: (value: T) => void
  let reject!: (reason?: unknown) => void
  const promise = new Promise<T>((resolvePromise, rejectPromise) => {
    resolve = resolvePromise
    reject = rejectPromise
  })
  return { promise, resolve, reject }
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

function grantPermissions(...permissions: string[]) {
  setPermissions({
    roles: [AuthRoles.Admin],
    permissions: permissions as UserPermissions['permissions'],
    effectiveRole: AuthRoles.Admin,
  })
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
    expect(wrapper.emitted('update:modelValue')).toEqual([
      [{ mode: 'selected', seasonIds: [activeSeason.id] }],
    ])

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
    ).toBeUndefined()

    await seasonOptions[0]!.setValue(true)
    await seasonOptions[1]!.setValue(true)
    await getButtonByName(wrapper, 'Aplicar seleção').trigger('click')
    const updatesAfterApply = wrapper.emitted('update:modelValue') ?? []
    expect(updatesAfterApply[updatesAfterApply.length - 1]).toEqual([
      {
        mode: 'selected',
        seasonIds: [activeSeason.id, closedSeason.id],
      },
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

  it('fully resets rule defaults after a successful publication', async () => {
    const wrapper = mount(CompetitionPanel, {
      props: {
        season: activeSeason,
        competitions: [],
        loading: false,
        saving: false,
        serviceMessageCode: null,
        mutationVersion: 0,
      },
      global: globalMountOptions(),
    })

    await getButtonByName(wrapper, 'Publicar regra geral').trigger('click')
    await wrapper.get('#rules-format').setValue('Md5')
    await wrapper.get('#rules-mode').setValue('Fearless')
    await wrapper.setProps({ mutationVersion: 1 })
    await nextTick()
    expect(wrapper.find('#rules-format').exists()).toBe(false)

    await getButtonByName(wrapper, 'Publicar regra geral').trigger('click')
    expect((wrapper.get('#rules-format').element as HTMLSelectElement).value).toBe('Md3')
    expect((wrapper.get('#rules-mode').element as HTMLSelectElement).value).toBe('Padrao')
  })

  it('keeps an inline mutation form open and disables its controls while saving', async () => {
    const wrapper = mount(CompetitionPanel, {
      props: {
        season: activeSeason,
        competitions: [],
        loading: false,
        saving: false,
        serviceMessageCode: null,
        mutationVersion: 0,
      },
      global: globalMountOptions(),
    })

    await getButtonByName(wrapper, 'Criar competição').trigger('click')
    await wrapper.setProps({ saving: true })
    const form = wrapper.get('#competition-form')
    await form
      .findAll('button')
      .find((button) => button.text().trim() === 'Cancelar')!
      .trigger('click')

    expect(wrapper.find('#competition-form').exists()).toBe(true)
    expect(
      form.findAll('button, input').every((control) => control.attributes('disabled') !== undefined),
    ).toBe(true)
  })
})

describe('Season management orchestration', () => {
  beforeEach(() => {
    vi.resetAllMocks()
    grantPermissions('CanManageSeasons', 'CanManageCompetitions')
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
            ? seasonDetail(plannedSeason, [
                'edit',
                'activate',
                'create-competition',
                'publish-rules',
              ])
            : seasonDetail(activeSeason, [
                'edit',
                'close',
                'create-competition',
                'publish-rules',
              ]),
          `"season-${seasonId}"`,
        ),
    )
    vi.mocked(competitionService.listCompetitionsForSeason).mockResolvedValue(
      competitionPage([]),
    )
    vi.mocked(competitionService.listCompetitions).mockResolvedValue(
      competitionPage([]),
    )
    vi.mocked(competitionService.getCompetition).mockImplementation(
      async (competitionId: string) =>
        observedResponse(
          makeCompetition({ id: competitionId }),
          `"competition-${competitionId}"`,
        ),
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
        return observedResponse(
          seasonDetail(activatedTarget, ['close']),
          '"calendar-after-activate"',
        )
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
        return observedResponse(
          seasonDetail(activatedTarget, ['close']),
          '"calendar-after-activate"',
        )
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
      return observedResponse(
        seasonDetail(closedTarget, []),
        '"calendar-after-close"',
      )
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
            season.id === createdSeason.id
              ? ['edit', 'create-competition', 'publish-rules']
              : ['edit', 'close', 'create-competition', 'publish-rules'],
          ),
          `"season-${season.id}"`,
        )
      },
    )
    vi.mocked(seasonService.createSeason).mockImplementation(async () => {
      seasons = [...seasons, createdSeason]
      return observedResponse(
        seasonDetail(createdSeason, [
          'edit',
          'create-competition',
          'publish-rules',
        ]),
        '"season-created"',
      )
    })
    vi.mocked(competitionService.listCompetitionsForSeason).mockImplementation(
      async () => competitionPage(competitions, [createdSeason]),
    )
    vi.mocked(competitionService.createCompetition).mockImplementation(
      async () => {
        competitions = [competition]
        return observedResponse(competition, '"competition-created"')
      },
    )
    vi.mocked(competitionService.createRound).mockImplementation(async () => {
      competitions = [{ ...competition, rodadas: [round] }]
      return observedResponse(round, '"competition-after-round"')
    })
    vi.mocked(competitionService.getCompetition).mockImplementation(
      async (competitionId: string) =>
        observedResponse(
          competitions.find(({ id }) => id === competitionId)!,
          `"competition-${competitionId}"`,
        ),
    )
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
      `"competition-${competition.id}"`,
    )
    expect(wrapper.text()).toContain('Rodada 1')
  })

  it('initializes repeated Season scope from the URL and scopes the first query', async () => {
    globalThis.history.replaceState(
      {},
      '',
      `/?temporadaIds=${activeSeason.id}&temporadaIds=${closedSeason.id}`,
    )
    vi.mocked(seasonService.listSeasons).mockResolvedValue(
      observedResponse(
        seasonPage([activeSeason, closedSeason]),
        '"calendar-scope"',
      ),
    )

    const wrapper = await mountView()

    expect(wrapper.get('[role="radio"][aria-checked="true"]').text()).toContain(
      'Temporadas selecionadas',
    )
    expect(
      wrapper
        .findAll('input[type="checkbox"]')
        .filter((input) => (input.element as HTMLInputElement).checked),
    ).toHaveLength(2)
    expect(competitionService.listCompetitions).toHaveBeenCalledWith({
      mode: 'selected',
      seasonIds: [activeSeason.id, closedSeason.id],
    })
  })

  it('handles session expiry actionably during the initial Season query', async () => {
    vi.mocked(seasonService.listSeasons).mockRejectedValue(
      new seasonService.SeasonServiceError(401, 'MA001'),
    )

    const wrapper = await mountView()

    expect(wrapper.get('[role="alert"]').text()).toContain(
      'Sua sessão expirou. Entre novamente para continuar.',
    )
    expect(wrapper.text()).not.toContain('Criar temporada')
    expect(wrapper.text()).toContain('Tentar novamente')
  })

  it('serializes scope changes and restores scoped queries on browser navigation', async () => {
    const wrapper = await mountView()

    await getButtonByName(wrapper, 'Todas as temporadas').trigger('click')
    await flushPromises()
    expect(new globalThis.URL(globalThis.location.href).searchParams.get('todas')).toBe('true')
    expect(competitionService.listCompetitions).toHaveBeenLastCalledWith({ mode: 'all' })

    globalThis.history.pushState(
      {},
      '',
      `/?temporadaIds=${closedSeason.id}`,
    )
    globalThis.dispatchEvent(new globalThis.PopStateEvent('popstate'))
    await flushPromises()

    expect(wrapper.get('[role="radio"][aria-checked="true"]').text()).toContain(
      'Temporadas selecionadas',
    )
    expect(competitionService.listCompetitions).toHaveBeenLastCalledWith({
      mode: 'selected',
      seasonIds: [closedSeason.id],
    })
  })

  it('commits radio arrow navigation to the URL and scoped query immediately', async () => {
    const wrapper = await mountView()
    const current = wrapper.get('[role="radio"][aria-checked="true"]')
    ;(current.element as HTMLElement).focus()
    await current.trigger('keydown', { key: 'ArrowRight' })
    await flushPromises()

    expect(new globalThis.URL(globalThis.location.href).searchParams.getAll('temporadaIds')).toEqual([
      activeSeason.id,
    ])
    expect(competitionService.listCompetitions).toHaveBeenLastCalledWith({
      mode: 'selected',
      seasonIds: [activeSeason.id],
    })
  })

  it('renders scoped competition items and identifies every included Season', async () => {
    const scoped = deferred<ReturnType<typeof competitionPage>>()
    vi.mocked(competitionService.listCompetitions).mockImplementationOnce(
      () => scoped.promise,
    )
    const wrapper = await mountView()

    expect(wrapper.get('[data-scope-summary]').text()).toContain(
      'Carregando competições do recorte.',
    )
    const currentCompetition = makeCompetition()
    const plannedCompetition = makeCompetition({
      id: '00000000-0000-4000-8000-000000000099',
      seasonId: plannedSeason.id,
      nome: 'Circuito futuro',
      codigo: 'FUTURO',
    })
    scoped.resolve(
      competitionPage(
        [currentCompetition, plannedCompetition],
        [activeSeason, plannedSeason],
      ),
    )
    await flushPromises()
    const summary = wrapper.get('[data-scope-summary]')
    expect(summary.text()).toContain('2 competições neste recorte.')
    expect(summary.get('[data-included-seasons]').text()).toContain('Rinha 2026')
    expect(summary.get('[data-included-seasons]').text()).toContain('Rinha 2027')
    expect(summary.get('[data-included-seasons]').attributes('aria-label')).toBe(
      'Temporadas incluídas',
    )
    expect(summary.get(`[data-scope-competition-id="${currentCompetition.id}"]`).text()).toContain(
      'Circuito principal',
    )
    expect(summary.get(`[data-scope-competition-id="${currentCompetition.id}"]`).text()).toContain(
      'Rinha 2026',
    )
    expect(summary.get(`[data-scope-competition-id="${plannedCompetition.id}"]`).text()).toContain(
      'Circuito futuro',
    )
    expect(summary.get(`[data-scope-competition-id="${plannedCompetition.id}"]`).text()).toContain(
      'Rinha 2027',
    )

    vi.mocked(competitionService.listCompetitions).mockResolvedValue(
      competitionPage([]),
    )
    await getButtonByName(wrapper, 'Todas as temporadas').trigger('click')
    await flushPromises()
    expect(wrapper.get('[data-scope-summary]').text()).toContain(
      'Nenhuma competição neste recorte.',
    )
  })

  it('ignores a stale scoped response that resolves after the latest URL scope', async () => {
    const currentRequest = deferred<ReturnType<typeof competitionPage>>()
    const allRequest = deferred<ReturnType<typeof competitionPage>>()
    vi.mocked(competitionService.listCompetitions)
      .mockImplementationOnce(() => currentRequest.promise)
      .mockImplementationOnce(() => allRequest.promise)
    const wrapper = await mountView()

    await getButtonByName(wrapper, 'Todas as temporadas').trigger('click')
    const latestCompetition = makeCompetition({ nome: 'Resultado mais recente' })
    allRequest.resolve(competitionPage([latestCompetition], [activeSeason, plannedSeason]))
    await flushPromises()
    expect(wrapper.get('[data-scope-summary]').text()).toContain('Resultado mais recente')

    currentRequest.resolve(
      competitionPage([makeCompetition({ nome: 'Resultado obsoleto' })]),
    )
    await flushPromises()

    expect(new globalThis.URL(globalThis.location.href).searchParams.get('todas')).toBe('true')
    expect(wrapper.get('[data-scope-summary]').text()).toContain('Resultado mais recente')
    expect(wrapper.get('[data-scope-summary]').text()).not.toContain('Resultado obsoleto')
  })

  it('pages through more than 20 Seasons and resets server paging for status history', async () => {
    const pageOne = Array.from({ length: 20 }, (_, index) => ({
      ...plannedSeason,
      id: `00000000-0000-4000-8000-${String(index + 10).padStart(12, '0')}`,
      nome: `Temporada ${index + 1}`,
      ano: 2009 + index,
    }))
    const historicalSeason = {
      ...closedSeason,
      nome: 'Temporada histórica paginada',
    }
    vi.mocked(seasonService.listSeasons).mockImplementation(async (filters = {}) => {
      if (filters.estado === 'Encerrada') {
        return observedResponse(
          seasonPage([historicalSeason], {
            page: 1,
            totalItems: 1,
            totalPages: 1,
          }),
          '"calendar-filtered"',
        )
      }
      if (filters.page === 2) {
        return observedResponse(
          seasonPage([historicalSeason], {
            page: 2,
            totalItems: 21,
            totalPages: 2,
          }),
          '"calendar-page-2"',
        )
      }
      return observedResponse(
        seasonPage(pageOne, {
          page: 1,
          totalItems: 21,
          totalPages: 2,
        }),
        '"calendar-page-1"',
      )
    })
    const wrapper = await mountView()

    expect(wrapper.findAll('.season-card')).toHaveLength(20)
    expect(wrapper.get('[data-season-pagination]').text()).toContain('Página 1 de 2')
    await getButtonByName(wrapper, 'Próxima página').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Temporada histórica paginada')
    expect(seasonService.listSeasons).toHaveBeenCalledWith({ page: 2, pageSize: 20 })

    await wrapper.get('select[name="seasonState"]').setValue('Encerrada')
    await flushPromises()
    expect(seasonService.listSeasons).toHaveBeenLastCalledWith({
      page: 1,
      pageSize: 20,
      estado: 'Encerrada',
    })
    expect(wrapper.get('[data-season-pagination]').text()).toContain('Página 1 de 1')
    expect(wrapper.text()).toContain('Temporada histórica paginada')
  })

  it('requires current permissions and authoritative actions for every management control', async () => {
    grantPermissions()
    vi.mocked(seasonService.getSeason).mockResolvedValue(
      observedResponse(
        seasonDetail(activeSeason, [
          'edit',
          'close',
          'create-competition',
          'publish-rules',
        ]),
        '"season-authoritative"',
      ),
    )
    vi.mocked(competitionService.listCompetitionsForSeason).mockResolvedValue(
      competitionPage([
        makeCompetition({
          acoesPermitidas: ['edit', 'create-round', 'publish-rules'],
        }),
      ]),
    )
    const wrapper = await mountView()

    expect(wrapper.text()).not.toContain('Criar temporada')
    await getButtonByName(wrapper, 'Selecionar temporada Rinha 2026').trigger('click')
    await flushPromises()
    expect(wrapper.text()).not.toContain('Editar temporada')
    expect(wrapper.text()).not.toContain('Encerrar temporada')
    expect(wrapper.text()).not.toContain('Criar competição')
    expect(wrapper.text()).not.toContain('Criar rodada')
    expect(
      wrapper
        .findAll('button')
        .some((button) => button.text().trim() === 'Publicar regras'),
    ).toBe(false)
  })

  it('uses the competition ETag observed with loaded data and never refreshes it at submit', async () => {
    const competition = makeCompetition({ acoesPermitidas: ['edit'] })
    vi.mocked(seasonService.getSeason).mockResolvedValue(
      observedResponse(
        seasonDetail(activeSeason, ['create-competition']),
        '"season-loaded"',
      ),
    )
    vi.mocked(competitionService.listCompetitionsForSeason).mockResolvedValue(
      competitionPage([competition]),
    )
    vi.mocked(competitionService.getCompetition).mockResolvedValue(
      observedResponse(competition, '"competition-loaded"'),
    )
    vi.mocked(competitionService.updateCompetition).mockResolvedValue(
      observedResponse(
        { ...competition, nome: 'Circuito atualizado' },
        '"competition-updated"',
      ),
    )
    const wrapper = await mountView()

    await getButtonByName(wrapper, 'Selecionar temporada Rinha 2026').trigger('click')
    await flushPromises()
    expect(competitionService.getCompetition).toHaveBeenCalledTimes(1)
    await getButtonByName(wrapper, 'Editar').trigger('click')
    await wrapper.get('#competition-name').setValue('Circuito atualizado')
    await wrapper.get('#competition-form').trigger('submit')
    await flushPromises()

    expect(competitionService.updateCompetition).toHaveBeenCalledWith(
      competition.id,
      {
        nome: 'Circuito atualizado',
        codigo: competition.codigo,
        circuitoDiario: true,
      },
      '"competition-loaded"',
    )
    expect(competitionService.getCompetition).toHaveBeenCalledTimes(2)
    expect(
      vi.mocked(competitionService.getCompetition).mock.invocationCallOrder[0],
    ).toBeLessThan(
      vi.mocked(competitionService.updateCompetition).mock.invocationCallOrder[0]!,
    )
    expect(
      vi.mocked(competitionService.getCompetition).mock.invocationCallOrder[1],
    ).toBeGreaterThan(
      vi.mocked(competitionService.updateCompetition).mock.invocationCallOrder[0]!,
    )
    expect(wrapper.find('#competition-form').exists()).toBe(false)
  })

  it.each([
    {
      messageCode: 'MV106',
      message: 'O nome da temporada é obrigatório',
    },
    {
      messageCode: 'MV036',
      message: 'Tamanho máximo excedido',
    },
  ])('uses the exact $messageCode API field message on Season controls', async ({ messageCode, message }) => {
    vi.mocked(seasonService.createSeason).mockRejectedValue(
      new seasonService.SeasonServiceError(
        400,
        'ME031',
        [],
        [
          {
            field: 'Request.Nome',
            messageCode,
            message,
          },
        ],
      ),
    )
    const wrapper = await mountView()

    await getButtonByName(wrapper, 'Criar temporada').trigger('click')
    await wrapper.get('#season-name').setValue('Temporada inválida')
    await wrapper.get('#season-year').setValue('2027')
    await wrapper.get('#season-order').setValue('1')
    await wrapper.get('#season-start-date').setValue('2027-01-01')
    await wrapper.get('#season-exclusive-end-date').setValue('2028-01-01')
    await wrapper.get('#season-form').trigger('submit')
    await flushPromises()

    expect(wrapper.get('#season-name-error').text()).toBe(message)
    expect(wrapper.get('#season-name').attributes('aria-invalid')).toBe('true')
    expect(globalThis.document.activeElement).toBe(wrapper.get('#season-name').element)
    expect(wrapper.find('#season-form').exists()).toBe(true)
  })

  it('announces a localized domain message without fieldErrors and never exposes its code', async () => {
    vi.mocked(seasonService.createSeason).mockRejectedValue(
      new seasonService.SeasonServiceError(
        400,
        'MD999',
        [],
        [],
        'Já existe uma temporada com esse período.',
      ),
    )
    const wrapper = await mountView()
    await getButtonByName(wrapper, 'Criar temporada').trigger('click')
    await wrapper.get('#season-name').setValue('Temporada duplicada')
    await wrapper.get('#season-year').setValue('2027')
    await wrapper.get('#season-order').setValue('1')
    await wrapper.get('#season-start-date').setValue('2027-01-01')
    await wrapper.get('#season-exclusive-end-date').setValue('2028-01-01')
    await wrapper.get('#season-form').trigger('submit')
    await flushPromises()

    expect(
      wrapper
        .findAll('[role="alert"]')
        .some((alert) => alert.text().includes('Já existe uma temporada com esse período.')),
    ).toBe(true)
    expect(wrapper.text()).not.toContain('MD999')
    expect(wrapper.find('#season-name-error').exists()).toBe(false)
  })

  it('does not infer a field from localized strings in the standard backend envelope', async () => {
    vi.mocked(seasonService.createSeason).mockRejectedValue(
      new seasonService.SeasonServiceError(
        400,
        'ME031',
        ['Informe o nome da temporada.'],
      ),
    )
    const wrapper = await mountView()
    await getButtonByName(wrapper, 'Criar temporada').trigger('click')
    await wrapper.get('#season-name').setValue('Nome enviado')
    await wrapper.get('#season-year').setValue('2027')
    await wrapper.get('#season-order').setValue('1')
    await wrapper.get('#season-start-date').setValue('2027-01-01')
    await wrapper.get('#season-exclusive-end-date').setValue('2028-01-01')
    await wrapper.get('#season-form').trigger('submit')
    await flushPromises()

    expect(wrapper.find('#season-name-error').exists()).toBe(false)
    expect(wrapper.get('[role="alert"]').text()).toContain(
      'Não foi possível salvar a temporada.',
    )
  })

  it('removes only the attempted Season action after a forbidden transition', async () => {
    vi.mocked(seasonService.closeSeason).mockRejectedValue(
      new seasonService.SeasonServiceError(403, 'MA002'),
    )
    const wrapper = await mountView()

    await getButtonByName(wrapper, 'Selecionar temporada Rinha 2026').trigger('click')
    await flushPromises()
    await getButtonByName(wrapper, 'Encerrar temporada').trigger('click')
    await getButtonByName(wrapper, 'Confirmar encerramento').trigger('click')
    await flushPromises()

    expect(wrapper.get('[role="alert"]').text()).toContain(
      'Você não possui permissão para esta ação.',
    )
    expect(wrapper.text()).toContain('Criar temporada')
    expect(wrapper.text()).toContain('Editar temporada')
    expect(wrapper.text()).toContain('Criar competição')
    expect(wrapper.text()).toContain('Publicar regra geral')
    expect(wrapper.text()).not.toContain('Encerrar temporada')
  })

  it('removes an unavailable selected resource after a 404 edit response', async () => {
    vi.mocked(seasonService.updateSeason).mockRejectedValue(
      new seasonService.SeasonServiceError(404, 'MC104'),
    )
    const wrapper = await mountView()

    await getButtonByName(wrapper, 'Selecionar temporada Rinha 2026').trigger('click')
    await flushPromises()
    await getButtonByName(wrapper, 'Editar temporada').trigger('click')
    await wrapper.get('#season-form').trigger('submit')
    await flushPromises()

    expect(wrapper.get('[role="alert"]').text()).toContain(
      'A temporada não está mais disponível.',
    )
    expect(wrapper.find('#season-form').exists()).toBe(false)
    expect(wrapper.find('.season-workspace').exists()).toBe(false)
    expect(seasonService.listSeasons).toHaveBeenCalledTimes(2)
  })

  it('formats Season dates and published instants with the active locale', async () => {
    setLocale('en')
    const rulesPublishedAt = '2026-03-04T18:30:00Z'
    const competition = makeCompetition({
      regrasPublicadas: [
        {
          id: '00000000-0000-4000-8000-000000000040',
          seasonId: activeSeason.id,
          competicaoId: '00000000-0000-4000-8000-000000000010',
          numero: 1,
          formato: 'Md3',
          modoDraft: 'Padrao',
          publicadaEm: rulesPublishedAt,
        },
      ],
    })
    vi.mocked(competitionService.listCompetitionsForSeason).mockResolvedValue(
      competitionPage([competition]),
    )
    vi.mocked(competitionService.getCompetition).mockResolvedValue(
      observedResponse(competition, '"competition-rules"'),
    )
    const wrapper = await mountView()
    await getButtonByName(wrapper, 'Select season Rinha 2026').trigger('click')
    await flushPromises()

    const expectedDate = new Intl.DateTimeFormat('en-US', {
      dateStyle: 'medium',
      timeZone: 'UTC',
    }).format(new Date('2026-01-01T00:00:00Z'))
    const expectedInstant = new Intl.DateTimeFormat('en-US', {
      dateStyle: 'medium',
      timeStyle: 'short',
      timeZone: 'America/Sao_Paulo',
    }).format(new Date(rulesPublishedAt))
    expect(wrapper.text()).toContain(expectedDate)
    expect(wrapper.text()).toContain(expectedInstant)
  })

  it('maps structured competition field keys and codes without closing the active form', async () => {
    const competition = makeCompetition({ acoesPermitidas: ['edit'] })
    vi.mocked(competitionService.listCompetitionsForSeason).mockResolvedValue(
      competitionPage([competition]),
    )
    vi.mocked(competitionService.getCompetition).mockResolvedValue(
      observedResponse(competition, '"competition-validation"'),
    )
    vi.mocked(competitionService.updateCompetition).mockRejectedValue(
      new competitionService.CompetitionServiceError(
        400,
        'ME031',
        [],
        [
          {
            field: 'Nome',
            messageCode: 'MV001',
            message: 'O nome da competição é obrigatório.',
          },
        ],
      ),
    )
    const wrapper = await mountView()
    await getButtonByName(wrapper, 'Selecionar temporada Rinha 2026').trigger('click')
    await flushPromises()
    await getButtonByName(wrapper, 'Editar').trigger('click')
    await wrapper.get('#competition-name').setValue('Nome inválido')
    await wrapper.get('#competition-form').trigger('submit')
    await flushPromises()

    expect(wrapper.get('#competition-name-error').text()).toBe(
      'O nome da competição é obrigatório.',
    )
    expect(globalThis.document.activeElement).toBe(
      wrapper.get('#competition-name').element,
    )
    expect(wrapper.find('#competition-form').exists()).toBe(true)
  })

  it('announces a localized competition domain message without fieldErrors', async () => {
    const competition = makeCompetition({ acoesPermitidas: ['edit'] })
    vi.mocked(competitionService.listCompetitionsForSeason).mockResolvedValue(
      competitionPage([competition]),
    )
    vi.mocked(competitionService.getCompetition).mockResolvedValue(
      observedResponse(competition, '"competition-domain-error"'),
    )
    vi.mocked(competitionService.updateCompetition).mockRejectedValue(
      new competitionService.CompetitionServiceError(
        400,
        'MD998',
        [],
        [],
        'O código da competição já está em uso.',
      ),
    )
    const wrapper = await mountView()
    await getButtonByName(wrapper, 'Selecionar temporada Rinha 2026').trigger('click')
    await flushPromises()
    await getButtonByName(wrapper, 'Editar').trigger('click')
    await wrapper.get('#competition-form').trigger('submit')
    await flushPromises()

    expect(wrapper.get('.seasons-view__alert').text()).toContain(
      'O código da competição já está em uso.',
    )
    expect(wrapper.text()).not.toContain('MD998')
    expect(wrapper.find('#competition-name-error').exists()).toBe(false)
  })

  it('reloads an opaque competition ETag after 409 and requires a second submit', async () => {
    const competition = makeCompetition({ acoesPermitidas: ['edit'] })
    vi.mocked(competitionService.listCompetitionsForSeason).mockResolvedValue(
      competitionPage([competition]),
    )
    vi.mocked(competitionService.getCompetition)
      .mockResolvedValueOnce(observedResponse(competition, '"competition-stale"'))
      .mockResolvedValue(observedResponse(competition, '"competition-fresh"'))
    vi.mocked(competitionService.updateCompetition)
      .mockRejectedValueOnce(new competitionService.CompetitionServiceError(409, 'MC103'))
      .mockResolvedValue(
        observedResponse({ ...competition, nome: 'Novo nome' }, '"competition-after"'),
      )
    const wrapper = await mountView()
    await getButtonByName(wrapper, 'Selecionar temporada Rinha 2026').trigger('click')
    await flushPromises()
    await getButtonByName(wrapper, 'Editar').trigger('click')
    await wrapper.get('#competition-name').setValue('Novo nome')
    await wrapper.get('#competition-form').trigger('submit')
    await flushPromises()

    expect(competitionService.updateCompetition).toHaveBeenCalledTimes(1)
    expect(wrapper.find('#competition-form').exists()).toBe(true)
    expect(wrapper.get('[role="alert"]').text()).toContain(
      'Os dados mudaram. Revise as informações e confirme novamente.',
    )

    await wrapper.get('#competition-form').trigger('submit')
    await flushPromises()
    expect(competitionService.updateCompetition).toHaveBeenNthCalledWith(
      2,
      competition.id,
      {
        nome: 'Novo nome',
        codigo: competition.codigo,
        circuitoDiario: true,
      },
      '"competition-fresh"',
    )
    expect(wrapper.find('#competition-form').exists()).toBe(false)
  })

  it('removes only the denied competition actions after a resource-specific 403', async () => {
    const denied = makeCompetition({ acoesPermitidas: ['edit'] })
    const allowed = makeCompetition({
      id: '00000000-0000-4000-8000-000000000088',
      nome: 'Circuito permitido',
      acoesPermitidas: ['edit'],
    })
    vi.mocked(competitionService.listCompetitionsForSeason).mockResolvedValue(
      competitionPage([denied, allowed]),
    )
    vi.mocked(competitionService.getCompetition).mockImplementation(
      async (id: string) =>
        observedResponse(id === denied.id ? denied : allowed, `"${id}"`),
    )
    vi.mocked(competitionService.updateCompetition).mockRejectedValue(
      new competitionService.CompetitionServiceError(403, 'MA002'),
    )
    const wrapper = await mountView()
    await getButtonByName(wrapper, 'Selecionar temporada Rinha 2026').trigger('click')
    await flushPromises()
    const deniedCard = wrapper.get(`[data-competition-id="${denied.id}"]`)
    await deniedCard
      .findAll('button')
      .find((button) => button.text().trim() === 'Editar')!
      .trigger('click')
    await wrapper.get('#competition-form').trigger('submit')
    await flushPromises()

    expect(wrapper.find('#competition-form').exists()).toBe(false)
    expect(deniedCard.findAll('button')).toHaveLength(0)
    expect(
      wrapper
        .get(`[data-competition-id="${allowed.id}"]`)
        .findAll('button')
        .some((button) => button.text().trim() === 'Editar'),
    ).toBe(true)
    expect(wrapper.text()).toContain('Criar competição')
  })

  it('removes only the attempted selected Season action after a competition 403', async () => {
    const competition = makeCompetition({ acoesPermitidas: ['edit'] })
    vi.mocked(competitionService.listCompetitionsForSeason).mockResolvedValue(
      competitionPage([competition]),
    )
    vi.mocked(competitionService.getCompetition).mockResolvedValue(
      observedResponse(competition, '"competition-before-forbidden-create"'),
    )
    vi.mocked(competitionService.createCompetition).mockRejectedValue(
      new competitionService.CompetitionServiceError(403, 'MA002'),
    )
    const wrapper = await mountView()
    await getButtonByName(wrapper, 'Selecionar temporada Rinha 2026').trigger('click')
    await flushPromises()
    await getButtonByName(wrapper, 'Criar competição').trigger('click')
    await wrapper.get('#competition-name').setValue('Nova competição')
    await wrapper.get('#competition-code').setValue('NOVA')
    await wrapper.get('#competition-form').trigger('submit')
    await flushPromises()

    expect(wrapper.find('#competition-form').exists()).toBe(false)
    expect(wrapper.text()).toContain('Editar temporada')
    expect(wrapper.text()).toContain('Encerrar temporada')
    expect(wrapper.text()).not.toContain('Criar competição')
    expect(wrapper.text()).toContain('Publicar regra geral')
    expect(
      wrapper
        .get(`[data-competition-id="${competition.id}"]`)
        .findAll('button')
        .some((button) => button.text().trim() === 'Editar'),
    ).toBe(true)

    await getButtonByName(wrapper, 'Selecionar temporada Rinha 2027').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Criar competição')
  })

  it.each([
    {
      status: 401,
      alert: 'Sua sessão expirou. Entre novamente para continuar.',
      keepsItem: true,
      keepsGlobalControl: false,
    },
    {
      status: 403,
      alert: 'Você não possui permissão para esta ação.',
      keepsItem: true,
      keepsGlobalControl: true,
    },
    {
      status: 404,
      alert: 'A competição não está mais disponível.',
      keepsItem: false,
      keepsGlobalControl: true,
    },
  ] as const)(
    'handles observe-detail $status without silently retaining stale actions',
    async ({ status, alert, keepsItem, keepsGlobalControl }) => {
      const competition = makeCompetition({ acoesPermitidas: ['edit'] })
      vi.mocked(competitionService.listCompetitionsForSeason).mockResolvedValue(
        competitionPage([competition]),
      )
      vi.mocked(competitionService.getCompetition).mockRejectedValue(
        new competitionService.CompetitionServiceError(status, `M${status}`),
      )
      const wrapper = await mountView()
      await getButtonByName(wrapper, 'Selecionar temporada Rinha 2026').trigger('click')
      await flushPromises()

      expect(wrapper.get('[role="alert"]').text()).toContain(alert)
      expect(wrapper.find(`[data-competition-id="${competition.id}"]`).exists()).toBe(
        keepsItem,
      )
      if (keepsItem) {
        expect(
          wrapper
            .get(`[data-competition-id="${competition.id}"]`)
            .findAll('button'),
        ).toHaveLength(0)
      }
      expect(wrapper.text().includes('Criar competição')).toBe(keepsGlobalControl)
    },
  )

  it('clears the session capabilities and keeps known data after 401', async () => {
    vi.mocked(seasonService.createSeason).mockRejectedValue(
      new seasonService.SeasonServiceError(401, 'MA001'),
    )
    const wrapper = await mountView()
    await getButtonByName(wrapper, 'Criar temporada').trigger('click')
    await wrapper.get('#season-name').setValue('Rinha 2028')
    await wrapper.get('#season-year').setValue('2028')
    await wrapper.get('#season-order').setValue('1')
    await wrapper.get('#season-start-date').setValue('2028-01-01')
    await wrapper.get('#season-exclusive-end-date').setValue('2029-01-01')
    await wrapper.get('#season-form').trigger('submit')
    await flushPromises()

    expect(wrapper.get('[role="alert"]').text()).toContain(
      'Sua sessão expirou. Entre novamente para continuar.',
    )
    expect(wrapper.text()).toContain('Rinha 2026')
    expect(wrapper.text()).not.toContain('Criar temporada')
  })

  it('keeps the calendar and Season ETags separate after activation', async () => {
    const activated = {
      ...plannedSeason,
      estado: 'Ativa' as const,
      versao: 2,
    }
    let seasonList = observedResponse(
      seasonPage([activeSeason, plannedSeason]),
      '"calendar-before-separation"',
    )
    vi.mocked(seasonService.listSeasons).mockImplementation(async () => seasonList)
    vi.mocked(seasonService.getSeason)
      .mockResolvedValueOnce(
        observedResponse(
          seasonDetail(plannedSeason, ['activate', 'publish-rules']),
          '"season-before-separation"',
        ),
      )
      .mockResolvedValue(
        observedResponse(
          seasonDetail(activated, ['close', 'publish-rules']),
          '"season-after-separation"',
        ),
      )
    vi.mocked(seasonService.activateSeason).mockImplementation(async () => {
      seasonList = observedResponse(
        seasonPage([activeSeason, activated], { temporadaAtual: activated }),
        '"calendar-after-separation"',
      )
      return observedResponse(
        seasonDetail(activated, ['close', 'publish-rules']),
        '"calendar-after-separation"',
      )
    })
    vi.mocked(competitionService.publishSeasonRules)
      .mockResolvedValueOnce(
        observedResponse(
          {
            id: '00000000-0000-4000-8000-000000000050',
            seasonId: activated.id,
            competicaoId: null,
            numero: 1,
            formato: 'Md3',
            modoDraft: 'Padrao',
            publicadaEm: '2026-08-02T12:00:00Z',
          },
          '"season-after-publication-1"',
        ),
      )
      .mockResolvedValueOnce(
        observedResponse(
          {
            id: '00000000-0000-4000-8000-000000000051',
            seasonId: activated.id,
            competicaoId: null,
            numero: 2,
            formato: 'Md3',
            modoDraft: 'Padrao',
            publicadaEm: '2026-08-02T13:00:00Z',
          },
          '"season-after-publication-2"',
        ),
      )
    const wrapper = await mountView()
    await getButtonByName(wrapper, 'Selecionar temporada Rinha 2027').trigger('click')
    await flushPromises()
    await getButtonByName(wrapper, 'Ativar temporada').trigger('click')
    await getButtonByName(wrapper, 'Confirmar ativação').trigger('click')
    await flushPromises()

    expect(seasonService.getSeason).toHaveBeenCalledTimes(2)
    await getButtonByName(wrapper, 'Publicar regra geral').trigger('click')
    await wrapper.get('form:not(#season-form)').trigger('submit')
    await flushPromises()
    expect(competitionService.publishSeasonRules).toHaveBeenCalledWith(
      activated.id,
      { formato: 'Md3', modoDraft: 'Padrao' },
      '"season-after-separation"',
    )
    await getButtonByName(wrapper, 'Publicar regra geral').trigger('click')
    await wrapper.get('form:not(#season-form)').trigger('submit')
    await flushPromises()
    expect(competitionService.publishSeasonRules).toHaveBeenNthCalledWith(
      2,
      activated.id,
      { formato: 'Md3', modoDraft: 'Padrao' },
      '"season-after-publication-1"',
    )
  })

  it('blocks edit when the observed Season ETag is missing', async () => {
    vi.mocked(seasonService.getSeason).mockResolvedValue(
      observedResponse(seasonDetail(activeSeason, ['edit']), null),
    )
    const wrapper = await mountView()
    await getButtonByName(wrapper, 'Selecionar temporada Rinha 2026').trigger('click')
    await flushPromises()
    await getButtonByName(wrapper, 'Editar temporada').trigger('click')

    expect(wrapper.find('#season-form').exists()).toBe(false)
    expect(wrapper.get('[role="alert"]').text()).toContain(
      'A versão do recurso não foi observada.',
    )
    expect(seasonService.createSeason).not.toHaveBeenCalled()
    expect(seasonService.updateSeason).not.toHaveBeenCalled()
  })

  it('closes a successful Season form before refresh and warns without resubmitting', async () => {
    const refresh = deferred<ObservedResponse<ReturnType<typeof seasonPage>>>()
    vi.mocked(seasonService.listSeasons)
      .mockResolvedValueOnce(
        observedResponse(seasonPage([activeSeason]), '"calendar-before-write"'),
      )
      .mockImplementationOnce(() => refresh.promise)
    vi.mocked(seasonService.createSeason).mockResolvedValue(
      observedResponse(
        seasonDetail(plannedSeason, ['edit']),
        '"season-created-before-refresh"',
      ),
    )
    const wrapper = await mountView()
    await getButtonByName(wrapper, 'Criar temporada').trigger('click')
    await wrapper.get('#season-name').setValue('Rinha 2027')
    await wrapper.get('#season-year').setValue('2027')
    await wrapper.get('#season-order').setValue('1')
    await wrapper.get('#season-start-date').setValue('2027-01-01')
    await wrapper.get('#season-exclusive-end-date').setValue('2028-01-01')
    await wrapper.get('#season-form').trigger('submit')
    await nextTick()

    expect(wrapper.find('#season-form').exists()).toBe(false)
    expect(seasonService.createSeason).toHaveBeenCalledTimes(1)
    refresh.reject(new seasonService.SeasonServiceError(503, 'ME001'))
    await flushPromises()
    expect(wrapper.get('[role="alert"]').text()).toContain(
      'A alteração foi salva, mas os dados não puderam ser atualizados.',
    )
    expect(seasonService.createSeason).toHaveBeenCalledTimes(1)
  })

  it('keeps an accepted transition closed and warns when its refresh fails', async () => {
    vi.mocked(seasonService.listSeasons)
      .mockResolvedValueOnce(
        observedResponse(
          seasonPage([activeSeason, plannedSeason]),
          '"calendar-before-transition"',
        ),
      )
      .mockRejectedValueOnce(new seasonService.SeasonServiceError(503, 'ME001'))
    vi.mocked(seasonService.activateSeason).mockResolvedValue(
      observedResponse(
        seasonDetail({ ...plannedSeason, estado: 'Ativa' }, ['close']),
        '"calendar-after-transition"',
      ),
    )
    const wrapper = await mountView()
    await getButtonByName(wrapper, 'Selecionar temporada Rinha 2027').trigger('click')
    await flushPromises()
    await getButtonByName(wrapper, 'Ativar temporada').trigger('click')
    await getButtonByName(wrapper, 'Confirmar ativação').trigger('click')
    await flushPromises()

    expect(wrapper.find('[role="dialog"]').exists()).toBe(false)
    expect(seasonService.activateSeason).toHaveBeenCalledTimes(1)
    expect(wrapper.get('[role="alert"]').text()).toContain(
      'A alteração foi salva, mas os dados não puderam ser atualizados.',
    )
  })

  it('closes a successful competition form before a failed refresh', async () => {
    const competition = makeCompetition({ acoesPermitidas: ['edit'] })
    vi.mocked(competitionService.listCompetitionsForSeason)
      .mockResolvedValueOnce(competitionPage([competition]))
      .mockRejectedValueOnce(new competitionService.CompetitionServiceError(503, 'ME001'))
    vi.mocked(competitionService.getCompetition).mockResolvedValue(
      observedResponse(competition, '"competition-before-write"'),
    )
    vi.mocked(competitionService.updateCompetition).mockResolvedValue(
      observedResponse({ ...competition, nome: 'Nome salvo' }, '"competition-after-write"'),
    )
    const wrapper = await mountView()
    await getButtonByName(wrapper, 'Selecionar temporada Rinha 2026').trigger('click')
    await flushPromises()
    await getButtonByName(wrapper, 'Editar').trigger('click')
    await wrapper.get('#competition-name').setValue('Nome salvo')
    await wrapper.get('#competition-form').trigger('submit')
    await flushPromises()

    expect(wrapper.find('#competition-form').exists()).toBe(false)
    expect(wrapper.get('[role="alert"]').text()).toContain(
      'A alteração foi salva, mas os dados não puderam ser atualizados.',
    )
    expect(competitionService.updateCompetition).toHaveBeenCalledTimes(1)
  })

  it('keeps mutation ETags authoritative when refresh fails and uses them on the next write', async () => {
    const competition = makeCompetition({
      acoesPermitidas: ['create-round', 'publish-rules'],
    })
    const round = {
      id: 'round-etag',
      competicaoId: competition.id,
      nome: 'Rodada ETag',
      ordem: 1,
      versao: 1,
    }
    vi.mocked(seasonService.getSeason).mockResolvedValue(
      observedResponse(seasonDetail(activeSeason, []), '"season-no-rules"'),
    )
    vi.mocked(competitionService.listCompetitionsForSeason)
      .mockResolvedValueOnce(competitionPage([competition]))
      .mockRejectedValue(new competitionService.CompetitionServiceError(503, 'ME001'))
    vi.mocked(competitionService.getCompetition).mockResolvedValue(
      observedResponse(competition, '"competition-before-round"'),
    )
    vi.mocked(competitionService.createRound).mockResolvedValue(
      observedResponse(round, '"competition-after-round"'),
    )
    vi.mocked(competitionService.publishCompetitionRules).mockResolvedValue(
      observedResponse(
        {
          id: 'rules-etag',
          seasonId: activeSeason.id,
          competicaoId: competition.id,
          numero: 1,
          formato: 'Md3',
          modoDraft: 'Padrao',
          publicadaEm: '2026-01-01T00:00:00Z',
        },
        '"competition-after-rules"',
      ),
    )
    const wrapper = await mountView()
    await getButtonByName(wrapper, 'Selecionar temporada Rinha 2026').trigger('click')
    await flushPromises()

    await getButtonByName(wrapper, 'Criar rodada').trigger('click')
    await wrapper.get('#round-name').setValue('Rodada ETag')
    await wrapper.get('#round-order').setValue('1')
    await wrapper.get('#round-form').trigger('submit')
    await flushPromises()
    await getButtonByName(wrapper, 'Publicar regras').trigger('click')
    await wrapper
      .findAll('button[type="submit"]')
      .find((button) => button.text().trim() === 'Publicar regras')!
      .trigger('click')
    await flushPromises()

    expect(competitionService.createRound).toHaveBeenCalledWith(
      competition.id,
      { nome: 'Rodada ETag', ordem: 1 },
      '"competition-before-round"',
    )
    expect(competitionService.publishCompetitionRules).toHaveBeenCalledWith(
      competition.id,
      { formato: 'Md3', modoDraft: 'Padrao' },
      '"competition-after-round"',
    )
  })
})

describe('Season modal accessibility', () => {
  it('prevents drawer dismissal and disables every control while saving', async () => {
    const wrapper = mount(SeasonFormDrawer, {
      attachTo: globalThis.document.body,
      props: {
        open: true,
        mode: 'create',
        season: null,
        saving: true,
        fieldErrors: {},
        serviceMessageCode: null,
      },
      global: globalMountOptions(),
    })
    await nextTick()

    await wrapper.get('.season-drawer__backdrop').trigger('click')
    await wrapper
      .findAll('button')
      .find((button) => button.text().trim() === 'Cancelar')!
      .trigger('click')
    await wrapper.get('[role="dialog"]').trigger('keydown', { key: 'Escape' })

    expect(wrapper.emitted('close')).toBeUndefined()
    expect(
      wrapper.findAll('button, input').every((control) => control.attributes('disabled') !== undefined),
    ).toBe(true)
  })

  it('prevents transition dismissal and disables every control while busy', async () => {
    const wrapper = mount(SeasonTransitionDialog, {
      attachTo: globalThis.document.body,
      props: {
        open: true,
        action: 'activate',
        season: plannedSeason,
        currentSeason: activeSeason,
        calendarVersion: 1,
        busy: true,
      },
      global: globalMountOptions(),
    })
    await nextTick()

    await wrapper.get('.transition-dialog__backdrop').trigger('click')
    await wrapper
      .findAll('button')
      .find((button) => button.text().trim() === 'Cancelar')!
      .trigger('click')
    await wrapper.get('[role="alertdialog"]').trigger('keydown', { key: 'Escape' })

    expect(wrapper.emitted('cancel')).toBeUndefined()
    expect(
      wrapper.findAll('button').every((control) => control.attributes('disabled') !== undefined),
    ).toBe(true)
    expect(wrapper.get('[role="alertdialog"]').attributes('aria-busy')).toBe('true')
    expect(wrapper.get('[role="status"]').attributes('aria-live')).toBe('polite')
    expect(wrapper.get('[role="status"]').text()).toBe('Aplicando alteração da temporada.')
  })

  it('traps focus, makes the app inert, closes on Escape and restores the trigger', async () => {
    const app = globalThis.document.createElement('main')
    app.id = 'app'
    const trigger = globalThis.document.createElement('button')
    app.append(trigger)
    globalThis.document.body.append(app)
    trigger.focus()

    const wrapper = mount(SeasonFormDrawer, {
      attachTo: globalThis.document.body,
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
    await nextTick()

    expect(app.hasAttribute('inert')).toBe(true)
    const dialog = wrapper.get('[role="dialog"]')
    const buttons = dialog.findAll('button')
    const lastButton = buttons[buttons.length - 1]!
    ;(lastButton.element as HTMLElement).focus()
    await lastButton.trigger('keydown', { key: 'Tab' })
    expect(globalThis.document.activeElement).toBe(buttons[0]!.element)

    await dialog.trigger('keydown', { key: 'Escape' })
    expect(wrapper.emitted('close')).toEqual([[]])
    await wrapper.setProps({ open: false })
    await nextTick()
    expect(app.hasAttribute('inert')).toBe(false)
    expect(globalThis.document.activeElement).toBe(trigger)
  })

  it('uses a mobile-safe dialog focus target and names every form control', async () => {
    vi.stubGlobal('matchMedia', vi.fn(() => ({ matches: true })))
    const wrapper = mount(SeasonFormDrawer, {
      attachTo: globalThis.document.body,
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
    await nextTick()

    expect(globalThis.document.activeElement).toBe(wrapper.get('[role="dialog"]').element)
    for (const input of wrapper.findAll('input')) {
      expect(input.attributes('name')).toBeTruthy()
      expect(input.attributes('autocomplete')).toBeTruthy()
    }
  })

  it('restores the transition trigger when Escape causes the dialog to unmount', async () => {
    const trigger = globalThis.document.createElement('button')
    globalThis.document.body.append(trigger)
    trigger.focus()
    const wrapper = mount(SeasonTransitionDialog, {
      attachTo: globalThis.document.body,
      props: {
        open: true,
        action: 'close',
        season: activeSeason,
        currentSeason: activeSeason,
        calendarVersion: 3,
      },
      global: globalMountOptions(),
    })
    await nextTick()

    await wrapper.get('[role="alertdialog"]').trigger('keydown', { key: 'Escape' })
    expect(wrapper.emitted('cancel')).toEqual([[]])
    wrapper.unmount()
    await nextTick()

    expect(globalThis.document.activeElement).toBe(trigger)
  })
})

afterEach(() => {
  document.body.innerHTML = ''
  globalThis.history.replaceState({}, '', '/')
  setPermissions(null)
  setLocale('pt')
  vi.unstubAllGlobals()
})
