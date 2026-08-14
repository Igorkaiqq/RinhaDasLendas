// @vitest-environment happy-dom
import { enableAutoUnmount, flushPromises, mount } from '@vue/test-utils'
import type { DOMWrapper, VueWrapper } from '@vue/test-utils'
import { AxiosError, AxiosHeaders } from 'axios'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { i18n, setLocale } from '@/i18n'
import { api } from '@/services/api'
import { setPermissions } from '@/services/authState'
import * as matchService from '@/services/matches'
import * as seasonService from '@/services/seasons'
import * as seriesService from '@/services/series'
import {
  makeMatch,
  makeSeries,
  type MatchFixture,
  type SeriesFixture,
} from '@/test/competitiveFixtures'

import FearlessPanel from './FearlessPanel.vue'
import MatchOperationPanel from './MatchOperationPanel.vue'
import SeriesScoreboard from './SeriesScoreboard.vue'
import MatchDetailView from '../../views/MatchDetailView.vue'
import SeriesDetailView from '../../views/SeriesDetailView.vue'
import SeriesView from '../../views/SeriesView.vue'

const competitiveComponentSources = import.meta.glob(
  ['./MatchOperationPanel.vue'],
  { eager: true, import: 'default', query: '?raw' },
) as Record<string, string>
const operationViewSources = import.meta.glob(
  ['../../views/MatchDetailView.vue', '../../views/SeriesDetailView.vue'],
  { eager: true, import: 'default', query: '?raw' },
) as Record<string, string>

vi.mock('@/services/api', () => ({
  api: {
    get: vi.fn(),
    post: vi.fn(),
  },
}))

enableAutoUnmount(afterEach)

const sideOneId = '00000000-0000-4000-8000-000000000008'
const sideTwoId = '00000000-0000-4000-8000-000000000009'
const seriesId = '00000000-0000-4000-8000-000000000006'
const matchId = '00000000-0000-4000-8000-000000000007'
const mutationInvalidates = [
  'series-detail',
  'series-scoreboard',
  'series-fearless',
  'match-detail',
] as const
const correctedPicksPayload = {
  lados: [
    {
      ladoSerieId: sideOneId,
      championIds: [41, 42, 43, 44, 45] as [
        number,
        number,
        number,
        number,
        number,
      ],
    },
    {
      ladoSerieId: sideTwoId,
      championIds: [46, 47, 48, 49, 50] as [
        number,
        number,
        number,
        number,
        number,
      ],
    },
  ] as [
    {
      ladoSerieId: string
      championIds: [number, number, number, number, number]
    },
    {
      ladoSerieId: string
      championIds: [number, number, number, number, number]
    },
  ],
}

type MatchDto = Omit<MatchFixture, 'versao' | 'serieEtag'>
type SeriesDto = Omit<SeriesFixture, 'etag' | 'partidas'> & {
  partidas: MatchDto[]
}

function matchDto(overrides: Partial<MatchDto> = {}): MatchDto {
  const match = { ...makeMatch() } as Partial<MatchFixture>
  delete match.versao
  delete match.serieEtag
  return { ...(match as MatchDto), ...overrides }
}

function completedMatch(overrides: Partial<MatchDto> = {}): MatchDto {
  return matchDto({
    estado: 'Confirmada',
    ladoVencedorId: sideOneId,
    motivoTermino: 'Normal',
    picks: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10].map((championId, index) => ({
      ladoSerieId: index < 5 ? sideOneId : sideTwoId,
      championId,
      ordem: (index % 5) + 1,
    })),
    acoesPermitidas: ['correct', 'annul'],
    ...overrides,
  })
}

function operationalSeries(overrides: Partial<SeriesDto> = {}): SeriesDto {
  const firstMatch = completedMatch()
  const series = { ...makeSeries() } as Partial<SeriesFixture>
  delete series.etag
  delete series.partidas
  return {
    ...(series as Omit<SeriesFixture, 'etag' | 'partidas'>),
    id: seriesId,
    tipo: 'DiariaTemporaria',
    formato: 'Md3',
    modoDraft: 'Fearless',
    estado: 'EmAndamento',
    placar: [1, 0],
    partidas: [firstMatch],
    bloqueiosFearless: firstMatch.picks.map(({ championId }) => championId),
    acoesPermitidas: ['create-match', 'cancel'],
    ...overrides,
  }
}

function winsRequired(series: SeriesDto): number {
  return series.formato === 'Md5' ? 3 : 2
}

function seriesResult(series: SeriesDto) {
  const winnerIndex = series.placar.findIndex(
    (score) => score >= winsRequired(series),
  )
  return {
    serieId: series.id,
    placar: series.placar,
    ladoVencedorId:
      winnerIndex < 0 ? null : (series.lados[winnerIndex]?.id ?? null),
    concluida: winnerIndex >= 0,
    elegivelOficial: series.elegivelOficial,
  }
}

function mutationResult(series: SeriesDto, match: MatchDto) {
  return {
    partida: match,
    resultadoSerie: seriesResult(series),
    bloqueiosFearless: series.bloqueiosFearless,
    revisaoNecessaria: series.revisaoNecessaria,
  }
}

function observed<T>(data: T, etag: string | null) {
  return {
    data,
    etag,
    idempotencyReplayed: false,
    invalidates: mutationInvalidates,
  }
}

function deferred<T>() {
  let resolve!: (value: T) => void
  const promise = new Promise<T>((resolvePromise) => {
    resolve = resolvePromise
  })
  return { promise, resolve }
}

function seriesPage(items: SeriesDto[]) {
  return {
    page: 1,
    pageSize: 20,
    items,
    totalItems: items.length,
    totalPages: items.length === 0 ? 0 : 1,
    calendarioConfigurado: true,
    temporadaAtual: null,
    seasonsIncluidas: [],
  }
}

function globalMountOptions() {
  return {
    plugins: [i18n],
    stubs: {
      teleport: { template: '<div data-teleport-stub><slot /></div>' },
      transition: false,
      RouterLink: {
        props: ['to'],
        template: '<a :href="String(to)"><slot /></a>',
      },
    },
  }
}

function stubMedia(options: { mobile?: boolean }) {
  const values = new Map<string, boolean>([
    ['(max-width: 767px)', options.mobile === true],
  ])
  const listeners = new Map<string, Set<(event: MediaQueryListEvent) => void>>()
  vi.stubGlobal(
    'matchMedia',
    vi.fn((query: string) => {
      const queryListeners = listeners.get(query) ?? new Set()
      listeners.set(query, queryListeners)
      return {
        get matches() {
          return values.get(query) ?? false
        },
        media: query,
        addEventListener: vi.fn(
          (_event: string, listener: (event: MediaQueryListEvent) => void) => {
            queryListeners.add(listener)
          },
        ),
        removeEventListener: vi.fn(
          (_event: string, listener: (event: MediaQueryListEvent) => void) => {
            queryListeners.delete(listener)
          },
        ),
      }
    }),
  )
  return {
    listenerCount: (query: string) => listeners.get(query)?.size ?? 0,
    setMatches: (query: string, matches: boolean) => {
      values.set(query, matches)
      for (const listener of listeners.get(query) ?? []) {
        listener({ matches, media: query } as MediaQueryListEvent)
      }
    },
  }
}

function getButton(wrapper: VueWrapper, name: string): DOMWrapper<Element> {
  const button = wrapper.findAll('button').find((candidate) => {
    const accessibleName =
      candidate.attributes('aria-label') || candidate.text()
    return accessibleName.trim() === name
  })
  expect(button, `button named "${name}"`).toBeDefined()
  return button!
}

async function fillPicks(wrapper: VueWrapper, championIds: number[]) {
  const inputs = wrapper.findAll('input[data-champion-pick]')
  expect(inputs).toHaveLength(10)
  for (const [index, input] of inputs.entries()) {
    await input.setValue(String(championIds[index]))
  }
}

function staleError() {
  return new matchService.MatchServiceError(
    409,
    'MV110',
    [],
    [],
    'A versão do recurso competitivo está desatualizada',
  )
}

function idempotencyError() {
  return new matchService.MatchServiceError(
    409,
    'MV126',
    [],
    [],
    'A chave de idempotência já foi usada com conteúdo diferente',
  )
}

function validationError() {
  return new matchService.MatchServiceError(
    400,
    'ME031',
    ['Falha de validação'],
    [
      {
        field: 'Justificativa',
        messageCode: 'MV127',
        message: 'A justificativa da correção é obrigatória',
      },
    ],
  )
}

function axiosApiError(
  status: number,
  messageCode: string,
  message: string,
  fieldErrors: Array<{
    field: string
    messageCode: string
    message: string
  }> = [],
) {
  return new AxiosError(
    'request failed',
    'ERR_BAD_RESPONSE',
    undefined,
    undefined,
    {
      status,
      statusText: 'Error',
      headers: new AxiosHeaders(),
      config: { headers: new AxiosHeaders() },
      data: { message, messageCode, errors: [message], fieldErrors },
    },
  )
}

type MatchOperation = 'picks' | 'result' | 'remake' | 'annul' | 'correction'

async function openAndSubmit(
  wrapper: VueWrapper,
  operation: MatchOperation,
  reason = 'Evidência preservada durante o conflito',
) {
  if (operation === 'picks') {
    await fillPicks(wrapper, [11, 12, 13, 14, 15, 16, 17, 18, 19, 20])
    await getButton(wrapper, 'Salvar picks').trigger('click')
    return
  }
  if (operation === 'result') {
    await wrapper.get('input[name="winner"]').setValue(sideOneId)
    await wrapper.get('select[name="endReason"]').setValue('Surrender')
    await getButton(wrapper, 'Confirmar resultado').trigger('click')
    return
  }

  const openLabels = {
    remake: 'Registrar remake',
    annul: 'Anular partida',
    correction: 'Corrigir partida',
  } as const
  await getButton(wrapper, openLabels[operation]).trigger('click')
  if (operation === 'remake') {
    await wrapper
      .get('input[name="remakePickDecision"][value="PreservarPicks"]')
      .setValue(true)
    await wrapper.get('textarea[name="remakeReason"]').setValue(reason)
    await getButton(wrapper, 'Confirmar remake').trigger('click')
  } else if (operation === 'annul') {
    await wrapper.get('textarea[name="annulReason"]').setValue(reason)
    await getButton(wrapper, 'Confirmar anulação').trigger('click')
  } else {
    await wrapper.get('input[name="correctedWinner"]').setValue(sideTwoId)
    await wrapper.get('select[name="correctedEndReason"]').setValue('Normal')
    await wrapper.get('textarea[name="correctionReason"]').setValue(reason)
    await getButton(wrapper, 'Confirmar correção').trigger('click')
  }
}

function staleConflictFixture(operation: MatchOperation) {
  const confirmed = completedMatch()
  if (operation === 'picks') {
    return {
      original: matchDto(),
      refreshed: matchDto({ picks: confirmed.picks }),
      field: 'picks',
      beforeText: 'Nenhum pick confirmado',
      afterText: '1, 2, 3, 4, 5',
    }
  }
  if (operation === 'remake') {
    return {
      original: { ...confirmed, acoesPermitidas: ['remake'] },
      refreshed: matchDto({
        estado: 'Remake',
        picks: confirmed.picks,
        decisaoPicksRemake: 'PreservarPicks',
        acoesPermitidas: ['correct', 'annul'],
      }),
      field: 'remake',
      beforeText: 'Confirmada',
      afterText: 'Preservar picks',
    }
  }
  if (operation === 'annul') {
    return {
      original: confirmed,
      refreshed: matchDto({ estado: 'Anulada', acoesPermitidas: ['correct'] }),
      field: 'annul',
      beforeText: 'Confirmada',
      afterText: 'Anulada',
    }
  }
  return {
    original:
      operation === 'result'
        ? { ...confirmed, acoesPermitidas: ['confirm'] }
        : confirmed,
    refreshed: completedMatch({
      ladoVencedorId: sideTwoId,
      motivoTermino: 'Surrender',
    }),
    field: operation,
    beforeText: 'side-fixture-1 · Normal',
    afterText: 'side-fixture-2 · Surrender',
  }
}

afterEach(() => {
  setLocale('pt')
  setPermissions(null)
  vi.restoreAllMocks()
  vi.clearAllMocks()
  vi.unstubAllGlobals()
})

describe('T047 Series and Match service contracts', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('uses exact Series routes and preserves response ETags as transport metadata', async () => {
    const series = operationalSeries()
    vi.mocked(api.get)
      .mockResolvedValueOnce({
        data: seriesPage([series]),
        headers: {},
      })
      .mockResolvedValueOnce({
        data: series,
        headers: { etag: '"series/detail:opaque"' },
      })
      .mockResolvedValueOnce({ data: seriesResult(series), headers: {} })

    await expect(
      seriesService.listSeries({ page: 2, pageSize: 10 }),
    ).resolves.toEqual(seriesPage([series]))
    await expect(seriesService.getSeries('series/id')).resolves.toEqual({
      data: series,
      etag: '"series/detail:opaque"',
    })
    await expect(seriesService.getSeriesResult('series/id')).resolves.toEqual(
      seriesResult(series),
    )
    const params = vi.mocked(api.get).mock.calls[0]?.[1]
      ?.params as URLSearchParams
    expect([...params]).toEqual([
      ['page', '2'],
      ['pageSize', '10'],
    ])
    expect(api.get).toHaveBeenNthCalledWith(1, '/api/v1/series', { params })
    expect(api.get).toHaveBeenNthCalledWith(2, '/api/v1/series/series%2Fid')
    expect(api.get).toHaveBeenNthCalledWith(
      3,
      '/api/v1/series/series%2Fid/resultado',
    )
    expect(series).not.toHaveProperty('etag')
  })

  it('serializes current, selected and all Season scopes without bracketed query keys', async () => {
    const firstSeasonId = '00000000-0000-4000-8000-000000000001'
    const secondSeasonId = '00000000-0000-4000-8000-000000000002'
    vi.mocked(api.get).mockResolvedValue({ data: seriesPage([]), headers: {} })

    await seriesService.listSeries()
    await seriesService.listSeries({
      scope: {
        mode: 'selected',
        seasonIds: [firstSeasonId, secondSeasonId],
      },
    })
    await seriesService.listSeries({ scope: { mode: 'all' } })

    expect([
      ...(vi.mocked(api.get).mock.calls[0]?.[1]?.params as URLSearchParams),
    ]).toEqual([])
    expect([
      ...(vi.mocked(api.get).mock.calls[1]?.[1]?.params as URLSearchParams),
    ]).toEqual([
      ['temporadaIds', firstSeasonId],
      ['temporadaIds', secondSeasonId],
    ])
    expect([
      ...(vi.mocked(api.get).mock.calls[2]?.[1]?.params as URLSearchParams),
    ]).toEqual([['todas', 'true']])
  })

  it('gets a Match from its encoded route and observes only the containing Series ETag', async () => {
    const match = matchDto()
    vi.mocked(api.get).mockResolvedValue({
      data: match,
      headers: { etag: '"containing-series:opaque"' },
    })

    await expect(matchService.getMatch('match/id')).resolves.toEqual({
      data: match,
      etag: '"containing-series:opaque"',
    })
    expect(api.get).toHaveBeenCalledWith('/api/v1/partidas/match%2Fid')
    expect(match).not.toHaveProperty('versao')
    expect(match).not.toHaveProperty('serieEtag')
  })

  it('creates a complete daily Series with a generated idempotency key and response ETag', async () => {
    const payload = {
      seasonId: '00000000-0000-4000-8000-000000000001',
      competicaoId: '00000000-0000-4000-8000-000000000002',
      rodadaId: '00000000-0000-4000-8000-000000000003',
      versaoRegrasId: '00000000-0000-4000-8000-000000000004',
      eventoId: null,
      tipo: 'DiariaTemporaria' as const,
      agendadaPara: '2026-06-01T22:00:00Z',
      dataLocal: '2026-06-01',
      draftMontagemId: '00000000-0000-4000-8000-000000000014',
      ladoOrigemIds: [sideOneId, sideTwoId] as [string, string],
    }
    const created = operationalSeries({ estado: 'Agendada', partidas: [] })
    const generatedKey = '22222222-2222-4222-8222-222222222222'
    vi.spyOn(globalThis.crypto, 'randomUUID').mockReturnValue(generatedKey)
    vi.mocked(api.post).mockResolvedValue({
      data: created,
      headers: {
        etag: '"series-created:opaque"',
        'idempotency-replayed': 'true',
      },
    })

    await expect(seriesService.createSeries(payload)).resolves.toEqual({
      data: created,
      etag: '"series-created:opaque"',
      idempotencyReplayed: true,
      invalidates: mutationInvalidates,
    })
    expect(api.post).toHaveBeenCalledWith('/api/v1/series', payload, {
      headers: { 'Idempotency-Key': generatedKey },
    })
  })

  const seriesMutationCases = [
    {
      name: 'startSeries',
      route: '/api/v1/series/series%2Fid/inicios',
      payload: undefined,
      responseData: operationalSeries(),
      invoke: () =>
        seriesService.startSeries(
          'series/id',
          '"series/current:opaque"',
          'idempotency-key',
        ),
      invokeWithoutKey: () =>
        seriesService.startSeries('series/id', '"series/current:opaque"'),
    },
    {
      name: 'cancelSeries',
      route: '/api/v1/series/series%2Fid/cancelamentos',
      payload: { justificativa: 'Agenda cancelada' },
      responseData: operationalSeries(),
      invoke: () =>
        seriesService.cancelSeries(
          'series/id',
          { justificativa: 'Agenda cancelada' },
          '"series/current:opaque"',
          'idempotency-key',
        ),
      invokeWithoutKey: () =>
        seriesService.cancelSeries(
          'series/id',
          { justificativa: 'Agenda cancelada' },
          '"series/current:opaque"',
        ),
    },
    {
      name: 'annulSeries',
      route: '/api/v1/series/series%2Fid/anulacoes',
      payload: { justificativa: 'Série anulada após revisão' },
      responseData: operationalSeries({ estado: 'Anulada' }),
      invoke: () =>
        seriesService.annulSeries(
          'series/id',
          { justificativa: 'Série anulada após revisão' },
          '"series/current:opaque"',
          'idempotency-key',
        ),
      invokeWithoutKey: () =>
        seriesService.annulSeries(
          'series/id',
          { justificativa: 'Série anulada após revisão' },
          '"series/current:opaque"',
        ),
    },
    {
      name: 'createMatch',
      route: '/api/v1/series/series%2Fid/partidas',
      payload: undefined,
      responseData: matchDto(),
      invoke: () =>
        seriesService.createMatch(
          'series/id',
          '"series/current:opaque"',
          'idempotency-key',
        ),
      invokeWithoutKey: () =>
        seriesService.createMatch('series/id', '"series/current:opaque"'),
    },
  ] as const

  it.each(seriesMutationCases)(
    '$name sends If-Match and Idempotency-Key and returns the response ETag',
    async ({ route, payload, responseData, invoke }) => {
      vi.mocked(api.post).mockResolvedValue({
        data: responseData,
        headers: {
          etag: '"series/mutated:opaque"',
          'idempotency-replayed': 'true',
        },
      })

      await expect(invoke()).resolves.toEqual({
        data: responseData,
        etag: '"series/mutated:opaque"',
        idempotencyReplayed: true,
        invalidates: mutationInvalidates,
      })
      expect(api.post).toHaveBeenCalledWith(route, payload, {
        headers: {
          'If-Match': '"series/current:opaque"',
          'Idempotency-Key': 'idempotency-key',
        },
      })
    },
  )

  const matchMutationCases = [
    {
      name: 'registerMatchPicks',
      suffix: 'picks',
      payload: correctedPicksPayload,
      invoke: () =>
        matchService.registerMatchPicks(
          'match/id',
          correctedPicksPayload,
          '"series/current:opaque"',
          'idempotency-key',
        ),
      invokeWithoutKey: () =>
        matchService.registerMatchPicks(
          'match/id',
          correctedPicksPayload,
          '"series/current:opaque"',
        ),
    },
    {
      name: 'confirmMatchResult',
      suffix: 'resultados',
      payload: { ladoVencedorId: sideOneId, motivoTermino: 'Normal' as const },
      invoke: () =>
        matchService.confirmMatchResult(
          'match/id',
          { ladoVencedorId: sideOneId, motivoTermino: 'Normal' },
          '"series/current:opaque"',
          'idempotency-key',
        ),
      invokeWithoutKey: () =>
        matchService.confirmMatchResult(
          'match/id',
          { ladoVencedorId: sideOneId, motivoTermino: 'Normal' },
          '"series/current:opaque"',
        ),
    },
    {
      name: 'registerMatchRemake',
      suffix: 'remakes',
      payload: {
        decisaoPicks: 'PreservarPicks' as const,
        justificativa: 'Falha técnica',
      },
      invoke: () =>
        matchService.registerMatchRemake(
          'match/id',
          { decisaoPicks: 'PreservarPicks', justificativa: 'Falha técnica' },
          '"series/current:opaque"',
          'idempotency-key',
        ),
      invokeWithoutKey: () =>
        matchService.registerMatchRemake(
          'match/id',
          { decisaoPicks: 'PreservarPicks', justificativa: 'Falha técnica' },
          '"series/current:opaque"',
        ),
    },
    {
      name: 'annulMatch',
      suffix: 'anulacoes',
      payload: {
        justificativa: 'Decisão anulada',
        anularSerieSeInconclusiva: false,
      },
      invoke: () =>
        matchService.annulMatch(
          'match/id',
          {
            justificativa: 'Decisão anulada',
            anularSerieSeInconclusiva: false,
          },
          '"series/current:opaque"',
          'idempotency-key',
        ),
      invokeWithoutKey: () =>
        matchService.annulMatch(
          'match/id',
          {
            justificativa: 'Decisão anulada',
            anularSerieSeInconclusiva: false,
          },
          '"series/current:opaque"',
        ),
    },
    {
      name: 'correctMatch',
      suffix: 'correcoes',
      payload: {
        justificativa: 'Resultado revisado',
        ladoVencedorId: sideTwoId,
        motivoTermino: 'Surrender' as const,
      },
      invoke: () =>
        matchService.correctMatch(
          'match/id',
          {
            justificativa: 'Resultado revisado',
            ladoVencedorId: sideTwoId,
            motivoTermino: 'Surrender',
          },
          '"series/current:opaque"',
          'idempotency-key',
        ),
      invokeWithoutKey: () =>
        matchService.correctMatch(
          'match/id',
          {
            justificativa: 'Resultado revisado',
            ladoVencedorId: sideTwoId,
            motivoTermino: 'Surrender',
          },
          '"series/current:opaque"',
        ),
    },
  ] as const

  it.each(matchMutationCases)(
    '$name uses the OpenAPI route, shared Series ETag and idempotency header',
    async ({ suffix, payload, invoke }) => {
      const result = mutationResult(operationalSeries(), completedMatch())
      vi.mocked(api.post).mockResolvedValue({
        data: result,
        headers: {
          etag: '"series/next:opaque"',
          'idempotency-replayed': 'true',
        },
      })
      await expect(invoke()).resolves.toEqual({
        data: result,
        etag: '"series/next:opaque"',
        idempotencyReplayed: true,
        invalidates: mutationInvalidates,
      })
      expect(api.post).toHaveBeenCalledWith(
        `/api/v1/partidas/match%2Fid/${suffix}`,
        payload,
        {
          headers: {
            'If-Match': '"series/current:opaque"',
            'Idempotency-Key': 'idempotency-key',
          },
        },
      )
      expect(result.partida).not.toHaveProperty('versao')
      expect(result.partida).not.toHaveProperty('serieEtag')
    },
  )

  const generatedKeyCases = [
    ...seriesMutationCases.map(({ name, invokeWithoutKey, responseData }) => ({
      name,
      invokeWithoutKey,
      responseData,
    })),
    ...matchMutationCases.map(({ name, invokeWithoutKey }) => ({
      name,
      invokeWithoutKey,
      responseData: mutationResult(operationalSeries(), completedMatch()),
    })),
  ]

  it.each(generatedKeyCases)(
    '$name generates an Idempotency-Key when the caller omits it',
    async ({ invokeWithoutKey, responseData }) => {
      const generatedKey = '11111111-1111-4111-8111-111111111111'
      vi.spyOn(globalThis.crypto, 'randomUUID').mockReturnValue(generatedKey)
      vi.mocked(api.post).mockResolvedValue({
        data: responseData,
        headers: { etag: '"series-generated-key"' },
      })

      await invokeWithoutKey()

      const calls = vi.mocked(api.post).mock.calls
      const config = calls[calls.length - 1]?.[2]
      expect(config?.headers).toMatchObject({
        'Idempotency-Key': generatedKey,
        'If-Match': '"series/current:opaque"',
      })
    },
  )

  it.each([
    {
      name: 'Series',
      invoke: () => seriesService.getSeries(seriesId),
      error: axiosApiError(
        409,
        'MV110',
        'A versão do recurso competitivo está desatualizada',
      ),
      expected: {
        status: 409,
        messageCode: 'MV110',
        message: 'A versão do recurso competitivo está desatualizada',
        errors: ['A versão do recurso competitivo está desatualizada'],
        fieldErrors: [],
      },
    },
    {
      name: 'Match',
      invoke: () => matchService.getMatch(matchId),
      error: axiosApiError(400, 'ME031', 'Falha de validação', [
        {
          field: 'Justificativa',
          messageCode: 'MV127',
          message: 'A justificativa da correção é obrigatória',
        },
      ]),
      expected: {
        status: 400,
        messageCode: 'ME031',
        message: 'Falha de validação',
        errors: ['Falha de validação'],
        fieldErrors: [
          {
            field: 'Justificativa',
            messageCode: 'MV127',
            message: 'A justificativa da correção é obrigatória',
          },
        ],
      },
    },
  ] as const)(
    'converts the complete $name Axios error envelope',
    async ({ invoke, error, expected }) => {
      vi.mocked(api.get).mockRejectedValue(error)

      await expect(invoke()).rejects.toMatchObject(expected)
    },
  )

  it.each([
    [
      'pt',
      'Registrar remake',
      'Confirmar remake',
      'Escolha como tratar os picks do remake.',
      'Informe a justificativa do remake.',
    ],
    [
      'en',
      'Register remake',
      'Confirm remake',
      'Choose how to handle the remake picks.',
      'Enter the remake reason.',
    ],
  ] as const)(
    'shows exact remake validation messages in %s',
    async (locale, openLabel, confirmLabel, decisionMessage, reasonMessage) => {
      setLocale(locale)
      const wrapper = mount(MatchOperationPanel, {
        attachTo: document.body,
        props: { match: matchDto(), series: operationalSeries(), busy: false },
        global: globalMountOptions(),
      })
      await getButton(wrapper, openLabel).trigger('click')
      await getButton(wrapper, confirmLabel).trigger('click')

      expect(wrapper.get('#remake-pick-decision-error').text()).toBe(
        decisionMessage,
      )
      expect(wrapper.get('#remake-reason-error').text()).toBe(reasonMessage)
      const firstDecision = wrapper.get('input[name="remakePickDecision"]')
      expect(firstDecision.attributes('aria-errormessage')).toBe(
        'remake-pick-decision-error',
      )
      expect(document.activeElement).toBe(firstDecision.element)
    },
  )
})

describe('Series list, detail and scoreboard', () => {
  it.each([
    {
      viewport: 'desktop',
      mobile: false,
      selector: 'table[aria-label="Séries competitivas"]',
      absent: 'ul[aria-label="Séries competitivas em cards"]',
    },
    {
      viewport: 'mobile',
      mobile: true,
      selector: 'ul[aria-label="Séries competitivas em cards"]',
      absent: 'table[aria-label="Séries competitivas"]',
    },
  ])(
    'renders only the semantic $viewport surface selected by matchMedia',
    async ({ mobile, selector, absent }) => {
      stubMedia({ mobile })
      const series = operationalSeries()
      vi.spyOn(seriesService, 'listSeries').mockResolvedValue(
        seriesPage([series]),
      )
      const wrapper = mount(SeriesView, { global: globalMountOptions() })
      await flushPromises()

      const surface = wrapper.get(selector)
      expect(wrapper.find(absent).exists()).toBe(false)
      expect(surface.findAll(`a[data-series-id="${series.id}"]`)).toHaveLength(
        1,
      )
      for (const text of [
        'Série diária',
        'Melhor de 3',
        'Fearless',
        'side-fixture-1',
        'side-fixture-2',
        '1 — 0',
        'Em andamento',
      ]) {
        expect(surface.text()).toContain(text)
      }
    },
  )

  it('switches semantic layouts when the live viewport media query changes', async () => {
    const media = stubMedia({ mobile: false })
    vi.spyOn(seriesService, 'listSeries').mockResolvedValue(
      seriesPage([operationalSeries()]),
    )
    const wrapper = mount(SeriesView, { global: globalMountOptions() })
    await flushPromises()
    expect(
      wrapper.find('table[aria-label="Séries competitivas"]').exists(),
    ).toBe(true)
    expect(media.listenerCount('(max-width: 767px)')).toBeGreaterThan(0)

    media.setMatches('(max-width: 767px)', true)
    globalThis.dispatchEvent(new Event('resize'))
    await flushPromises()
    expect(
      wrapper.find('table[aria-label="Séries competitivas"]').exists(),
    ).toBe(false)
    expect(
      wrapper.find('ul[aria-label="Séries competitivas em cards"]').exists(),
    ).toBe(true)

    media.setMatches('(max-width: 767px)', false)
    globalThis.dispatchEvent(new Event('resize'))
    await flushPromises()
    expect(
      wrapper.find('table[aria-label="Séries competitivas"]').exists(),
    ).toBe(true)
  })

  it.each([
    ['Md3', [1, 1] as [number, number], '2', false],
    ['Md3', [2, 1] as [number, number], '2', true],
    ['Md5', [2, 2] as [number, number], '3', false],
    ['Md5', [3, 2] as [number, number], '3', true],
  ] as const)(
    'derives a valid %s result and announces its score',
    (formato, placar, required, concluded) => {
      const series = operationalSeries({
        formato,
        placar,
        estado: concluded ? 'Concluida' : 'EmAndamento',
      })
      expect(seriesResult(series).concluida).toBe(concluded)
      const wrapper = mount(SeriesScoreboard, {
        props: { series },
        global: globalMountOptions(),
      })
      const output = wrapper.get('output[data-series-scoreboard]')
      expect(output.attributes('aria-live')).toBe('polite')
      expect(output.text()).toContain(`${placar[0]} — ${placar[1]}`)
      expect(output.text()).toContain(`Vitórias necessárias: ${required}`)
    },
  )

  it('blocks confirmation during review while preserving annul and correction recovery', () => {
    const series = operationalSeries({
      bloqueiosFearless: [1, 22, 103],
      revisaoNecessaria: true,
    })
    const fearless = mount(FearlessPanel, {
      props: { series },
      global: globalMountOptions(),
    })
    expect(fearless.get('[role="alert"]').text()).toContain(
      'revisão necessária',
    )
    expect(fearless.findAll('section[data-side-fearless]')).toHaveLength(2)
    for (const side of fearless.findAll('section[data-side-fearless]')) {
      expect(
        side
          .findAll('[data-blocked-champion]')
          .map((item) => item.attributes('data-champion-id')),
      ).toEqual(['1', '22', '103'])
    }

    const operation = mount(MatchOperationPanel, {
      props: {
        match: matchDto({
          acoesPermitidas: [
            'register-picks',
            'confirm',
            'remake',
            'annul',
            'correct',
          ],
        }),
        series,
        busy: false,
      },
      global: globalMountOptions(),
    })
    for (const name of ['Confirmar resultado', 'Registrar remake']) {
      expect(getButton(operation, name).attributes('disabled')).toBeDefined()
    }
    for (const name of ['Anular partida', 'Corrigir partida']) {
      expect(getButton(operation, name).attributes('disabled')).toBeUndefined()
    }
    expect(
      getButton(operation, 'Salvar picks').attributes('disabled'),
    ).toBeUndefined()
  })

  it('renders historical Series detail with scoreboard, matches and Fearless', async () => {
    const series = operationalSeries()
    vi.spyOn(seriesService, 'getSeries').mockResolvedValue(
      observed(series, '"series-detail"'),
    )
    vi.spyOn(seriesService, 'getSeriesResult').mockResolvedValue(
      seriesResult(series),
    )
    const wrapper = mount(SeriesDetailView, {
      props: { seriesId },
      global: globalMountOptions(),
    })
    await flushPromises()

    expect(wrapper.get('main').attributes('aria-labelledby')).toBeTruthy()
    expect(wrapper.get('[data-series-scoreboard]').text()).toContain('1 — 0')
    expect(wrapper.get('[data-fearless-panel]').text()).toContain('10')
    expect(
      wrapper.get('ol[aria-label="Partidas da série"] li').text(),
    ).toContain('Partida 1')
  })

  const seriesUiCases = [
    {
      name: 'start',
      series: operationalSeries({
        estado: 'Agendada',
        acoesPermitidas: ['start'],
      }),
      after: operationalSeries({ estado: 'EmAndamento' }),
      install: () => {
        const spy = vi
          .spyOn(seriesService, 'startSeries')
          .mockResolvedValue(
            observed(
              operationalSeries({ estado: 'EmAndamento' }),
              '"series-after-start"',
            ),
          )
        return () =>
          expect(spy).toHaveBeenCalledWith(seriesId, '"series-ui-etag"')
      },
      act: async (wrapper: VueWrapper) => {
        await getButton(wrapper, 'Iniciar série').trigger('click')
        await getButton(wrapper, 'Confirmar início').trigger('click')
      },
      assertState: (wrapper: VueWrapper) => {
        expect(wrapper.get('[data-series-state]').text()).toBe('Em andamento')
        expect(wrapper.text()).not.toContain('Iniciar série')
        expect(wrapper.text()).toContain('Criar próxima partida')
      },
    },
    {
      name: 'cancel',
      series: operationalSeries({ acoesPermitidas: ['cancel'] }),
      after: operationalSeries({
        estado: 'Cancelada',
        acoesPermitidas: [],
      }),
      install: () => {
        const spy = vi.spyOn(seriesService, 'cancelSeries').mockResolvedValue(
          observed(
            operationalSeries({
              estado: 'Cancelada',
              acoesPermitidas: [],
            }),
            '"series-after-cancel"',
          ),
        )
        return () =>
          expect(spy).toHaveBeenCalledWith(
            seriesId,
            { justificativa: 'Confronto indisponível' },
            '"series-ui-etag"',
          )
      },
      act: async (wrapper: VueWrapper) => {
        await getButton(wrapper, 'Cancelar série').trigger('click')
        await wrapper
          .get('textarea[name="seriesCancelReason"]')
          .setValue('Confronto indisponível')
        await getButton(wrapper, 'Confirmar cancelamento').trigger('click')
      },
      assertState: (wrapper: VueWrapper) => {
        expect(wrapper.get('[data-series-state]').text()).toBe('Cancelada')
        expect(wrapper.text()).not.toContain('Cancelar série')
        expect(wrapper.text()).not.toContain('Criar próxima partida')
      },
    },
    {
      name: 'annul',
      series: operationalSeries({
        estado: 'Concluida',
        placar: [2, 0],
        acoesPermitidas: ['annul'],
      }),
      after: operationalSeries({
        estado: 'Anulada',
        placar: [0, 0],
        acoesPermitidas: [],
      }),
      install: () => {
        const spy = vi.spyOn(seriesService, 'annulSeries').mockResolvedValue(
          observed(
            operationalSeries({
              estado: 'Anulada',
              placar: [0, 0],
              acoesPermitidas: [],
            }),
            '"series-after-annul"',
          ),
        )
        return () =>
          expect(spy).toHaveBeenCalledWith(
            seriesId,
            { justificativa: 'Correção técnica auditada' },
            '"series-ui-etag"',
          )
      },
      act: async (wrapper: VueWrapper) => {
        await getButton(wrapper, 'Anular série').trigger('click')
        await wrapper
          .get('textarea[name="seriesAnnulReason"]')
          .setValue('Correção técnica auditada')
        await getButton(wrapper, 'Confirmar anulação').trigger('click')
      },
      assertState: (wrapper: VueWrapper) => {
        expect(wrapper.get('[data-series-state]').text()).toBe('Anulada')
        expect(wrapper.get('[data-series-scoreboard]').text()).toContain(
          '0 — 0',
        )
        expect(wrapper.text()).not.toContain('Anular série')
      },
    },
    {
      name: 'next match',
      series: operationalSeries({ acoesPermitidas: ['create-match'] }),
      after: operationalSeries({
        partidas: [matchDto(), matchDto({ id: 'match-two', ordem: 2 })],
        acoesPermitidas: ['create-match'],
      }),
      install: () => {
        const spy = vi
          .spyOn(seriesService, 'createMatch')
          .mockResolvedValue(
            observed(matchDto({ ordem: 2 }), '"series-after-next-match"'),
          )
        return () =>
          expect(spy).toHaveBeenCalledWith(seriesId, '"series-ui-etag"')
      },
      act: async (wrapper: VueWrapper) => {
        await getButton(wrapper, 'Criar próxima partida').trigger('click')
      },
      assertState: (wrapper: VueWrapper) => {
        const matches = wrapper.findAll('ol[aria-label="Partidas da série"] li')
        expect(matches).toHaveLength(2)
        expect(matches[1]?.text()).toContain('Partida 2')
      },
    },
  ] as const

  it.each(seriesUiCases)(
    'performs allowed Series $name behavior with the observed ETag',
    async ({ series, after, install, act, assertState }) => {
      vi.spyOn(seriesService, 'getSeries')
        .mockResolvedValueOnce(observed(series, '"series-ui-etag"'))
        .mockResolvedValue(observed(after, '"series-ui-reloaded"'))
      vi.spyOn(seriesService, 'getSeriesResult').mockResolvedValue(
        seriesResult(series),
      )
      const assertCalled = install()
      const wrapper = mount(SeriesDetailView, {
        props: { seriesId },
        global: globalMountOptions(),
      })
      await flushPromises()

      await act(wrapper)
      await flushPromises()
      assertCalled()
      assertState(wrapper)
    },
  )
})

describe('Match operation forms and correction effects', () => {
  it('validates 5 unique picks per side and emits only the OpenAPI body', async () => {
    const match = matchDto()
    const wrapper = mount(MatchOperationPanel, {
      attachTo: document.body,
      props: {
        match,
        series: operationalSeries({ partidas: [match] }),
        busy: false,
      },
      global: globalMountOptions(),
    })
    await fillPicks(wrapper, [1, 2, 3, 4, 5, 5, 7, 8, 9, 10])
    await getButton(wrapper, 'Salvar picks').trigger('click')
    expect(wrapper.emitted('savePicks')).toBeUndefined()
    expect(wrapper.get('[role="alert"]').text()).toContain(
      'Cada campeão só pode aparecer uma vez na partida.',
    )
    expect(document.activeElement).toBe(
      wrapper.findAll('input[data-champion-pick]')[5]!.element,
    )

    await fillPicks(wrapper, [1, 2, 3, 4, 5, 6, 7, 8, 9, 10])
    await getButton(wrapper, 'Salvar picks').trigger('click')
    const payload = wrapper.emitted('savePicks')?.[0]?.[0]
    expect(payload).toEqual({
      lados: [
        { ladoSerieId: sideOneId, championIds: [1, 2, 3, 4, 5] },
        { ladoSerieId: sideTwoId, championIds: [6, 7, 8, 9, 10] },
      ],
    })
    expect(payload).not.toHaveProperty('versao')
    expect(payload).not.toHaveProperty('etag')
  })

  it.each(['Normal', 'Surrender'] as const)(
    'emits result reason %s without client-side score mutation',
    async (motivoTermino) => {
      const series = operationalSeries({ placar: [0, 0] })
      const wrapper = mount(MatchOperationPanel, {
        props: { match: matchDto(), series, busy: false },
        global: globalMountOptions(),
      })
      await wrapper.get('input[name="winner"]').setValue(sideOneId)
      await wrapper.get('select[name="endReason"]').setValue(motivoTermino)
      await getButton(wrapper, 'Confirmar resultado').trigger('click')
      expect(wrapper.emitted('confirmResult')).toEqual([
        [{ ladoVencedorId: sideOneId, motivoTermino }],
      ])
      expect(series.placar).toEqual([0, 0])
    },
  )

  it('reconstructs Fearless after pick correction and identifies resolved later conflicts', async () => {
    const first = completedMatch()
    const later = completedMatch({
      id: '00000000-0000-4000-8000-000000000099',
      ordem: 2,
      conflitoFearless: true,
      picks: completedMatch().picks.map((pick) => ({
        ...pick,
        championId: pick.championId + 20,
      })),
    })
    const before = operationalSeries({
      partidas: [first, later],
      bloqueiosFearless: [...first.picks, ...later.picks].map(
        ({ championId }) => championId,
      ),
      revisaoNecessaria: true,
    })
    const corrected = {
      ...first,
      picks: first.picks.map((pick) => ({
        ...pick,
        championId: pick.championId + 40,
      })),
    }
    const after = operationalSeries({
      partidas: [corrected, { ...later, conflitoFearless: false }],
      bloqueiosFearless: [...corrected.picks, ...later.picks].map(
        ({ championId }) => championId,
      ),
      revisaoNecessaria: false,
    })
    vi.spyOn(matchService, 'getMatch')
      .mockResolvedValueOnce(observed(first, '"before"'))
      .mockResolvedValue(observed(corrected, '"after-correction"'))
    vi.spyOn(seriesService, 'getSeries')
      .mockResolvedValueOnce(observed(before, '"before"'))
      .mockResolvedValue(observed(after, '"after-correction"'))
    vi.spyOn(seriesService, 'getSeriesResult').mockResolvedValue(
      seriesResult(before),
    )
    vi.spyOn(matchService, 'correctMatch').mockResolvedValue(
      observed(mutationResult(after, corrected), '"after-correction"'),
    )
    const wrapper = mount(MatchDetailView, {
      props: { matchId },
      global: globalMountOptions(),
    })
    await flushPromises()
    await getButton(wrapper, 'Corrigir partida').trigger('click')
    await fillPicks(
      wrapper,
      corrected.picks.map(({ championId }) => championId),
    )
    await wrapper
      .get('textarea[name="correctionReason"]')
      .setValue('Picks revisados')
    await getButton(wrapper, 'Confirmar correção').trigger('click')
    await flushPromises()

    expect(matchService.correctMatch).toHaveBeenCalledWith(
      matchId,
      {
        lados: [
          {
            ladoSerieId: sideOneId,
            championIds: corrected.picks
              .filter(({ ladoSerieId }) => ladoSerieId === sideOneId)
              .map(({ championId }) => championId),
          },
          {
            ladoSerieId: sideTwoId,
            championIds: corrected.picks
              .filter(({ ladoSerieId }) => ladoSerieId === sideTwoId)
              .map(({ championId }) => championId),
          },
        ],
        justificativa: 'Picks revisados',
      },
      '"before"',
    )
    expect(wrapper.get('[data-fearless-panel]').text()).toContain('41')
    expect(wrapper.get('[role="status"]').text()).toContain(
      'Conflitos Fearless resolvidos',
    )
    expect(wrapper.find('[data-review-required]').exists()).toBe(false)
  })
})

describe('opaque shared Series ETag and conflict recovery', () => {
  beforeEach(() => {
    vi.spyOn(seriesService, 'getSeriesResult').mockImplementation(
      async (id: string) => seriesResult(operationalSeries({ id })),
    )
  })

  it('blocks writes and reloads both resources until divergent Series ETags converge', async () => {
    const match = matchDto()
    const series = operationalSeries({ partidas: [match] })
    const convergedMatch = deferred<ReturnType<typeof observed<MatchDto>>>()
    const convergedSeries = deferred<ReturnType<typeof observed<SeriesDto>>>()
    vi.spyOn(matchService, 'getMatch')
      .mockResolvedValueOnce(observed(match, '"match-read-diverged"'))
      .mockReturnValueOnce(convergedMatch.promise)
    vi.spyOn(seriesService, 'getSeries')
      .mockResolvedValueOnce(observed(series, '"series-read-diverged"'))
      .mockReturnValueOnce(convergedSeries.promise)
    const save = vi
      .spyOn(matchService, 'registerMatchPicks')
      .mockResolvedValue(
        observed(mutationResult(series, match), '"after-save"'),
      )
    const wrapper = mount(MatchDetailView, {
      props: { matchId },
      global: globalMountOptions(),
    })
    await flushPromises()

    expect(
      wrapper.get('[role="status"][data-etag-reconciliation]').text(),
    ).toContain('Sincronizando a versão da série…')
    expect(
      wrapper
        .findAll('button')
        .every((button) => button.attributes('disabled') !== undefined),
    ).toBe(true)
    expect(save).not.toHaveBeenCalled()

    convergedMatch.resolve(observed(match, '"converged-series-etag"'))
    convergedSeries.resolve(observed(series, '"converged-series-etag"'))
    await flushPromises()
    await fillPicks(wrapper, [1, 2, 3, 4, 5, 6, 7, 8, 9, 10])
    await getButton(wrapper, 'Salvar picks').trigger('click')

    expect(save).toHaveBeenCalledWith(
      matchId,
      expect.any(Object),
      '"converged-series-etag"',
    )
  })

  it('uses the latest observed ETag for the next write when mutation and reload tokens differ', async () => {
    const draft = matchDto()
    const afterPicks = { ...draft, picks: completedMatch().picks }
    const series = operationalSeries({ partidas: [draft] })
    vi.spyOn(matchService, 'getMatch')
      .mockResolvedValueOnce(observed(draft, '"initial-observed"'))
      .mockResolvedValue(observed(afterPicks, '"reload-observed-different"'))
    vi.spyOn(seriesService, 'getSeries')
      .mockResolvedValueOnce(observed(series, '"initial-observed"'))
      .mockResolvedValue(
        observed(
          { ...series, partidas: [afterPicks] },
          '"reload-observed-different"',
        ),
      )
    vi.spyOn(matchService, 'registerMatchPicks').mockResolvedValue(
      observed(
        mutationResult(series, afterPicks),
        '"mutation-returned-latest"',
      ),
    )
    vi.spyOn(matchService, 'confirmMatchResult').mockResolvedValue(
      observed(mutationResult(series, completedMatch()), '"result-returned"'),
    )
    const wrapper = mount(MatchDetailView, {
      props: { matchId },
      global: globalMountOptions(),
    })
    await flushPromises()
    await fillPicks(wrapper, [1, 2, 3, 4, 5, 6, 7, 8, 9, 10])
    await getButton(wrapper, 'Salvar picks').trigger('click')
    await flushPromises()
    await wrapper.get('input[name="winner"]').setValue(sideOneId)
    await wrapper.get('select[name="endReason"]').setValue('Normal')
    await getButton(wrapper, 'Confirmar resultado').trigger('click')

    expect(matchService.registerMatchPicks).toHaveBeenCalledWith(
      matchId,
      expect.any(Object),
      '"initial-observed"',
    )
    expect(matchService.confirmMatchResult).toHaveBeenCalledWith(
      matchId,
      { ladoVencedorId: sideOneId, motivoTermino: 'Normal' },
      '"reload-observed-different"',
    )
  })

  const staleCases = [
    {
      operation: 'picks',
      install: (after: SeriesDto, refreshed: MatchDto) => {
        const spy = vi
          .spyOn(matchService, 'registerMatchPicks')
          .mockRejectedValueOnce(staleError())
          .mockResolvedValue(
            observed(
              mutationResult(after, refreshed),
              '"mutation-after-reconfirm"',
            ),
          )
        return {
          assertOnce: () => expect(spy).toHaveBeenCalledOnce(),
          assertReconfirmed: () => {
            expect(spy).toHaveBeenCalledTimes(2)
            expect(spy.mock.calls[1]?.[1]).toEqual(spy.mock.calls[0]?.[1])
            expect(spy.mock.calls[1]?.[2]).toBe('"fresh-after-reload"')
          },
        }
      },
    },
    {
      operation: 'result',
      install: (after: SeriesDto, refreshed: MatchDto) => {
        const spy = vi
          .spyOn(matchService, 'confirmMatchResult')
          .mockRejectedValueOnce(staleError())
          .mockResolvedValue(
            observed(
              mutationResult(after, refreshed),
              '"mutation-after-reconfirm"',
            ),
          )
        return {
          assertOnce: () => expect(spy).toHaveBeenCalledOnce(),
          assertReconfirmed: () => {
            expect(spy).toHaveBeenCalledTimes(2)
            expect(spy.mock.calls[1]?.[1]).toEqual(spy.mock.calls[0]?.[1])
            expect(spy.mock.calls[1]?.[2]).toBe('"fresh-after-reload"')
          },
        }
      },
    },
    {
      operation: 'remake',
      install: (after: SeriesDto, refreshed: MatchDto) => {
        const spy = vi
          .spyOn(matchService, 'registerMatchRemake')
          .mockRejectedValueOnce(staleError())
          .mockResolvedValue(
            observed(
              mutationResult(after, refreshed),
              '"mutation-after-reconfirm"',
            ),
          )
        return {
          assertOnce: () => expect(spy).toHaveBeenCalledOnce(),
          assertReconfirmed: () => {
            expect(spy).toHaveBeenCalledTimes(2)
            expect(spy.mock.calls[1]?.[1]).toEqual(spy.mock.calls[0]?.[1])
            expect(spy.mock.calls[1]?.[2]).toBe('"fresh-after-reload"')
          },
        }
      },
    },
    {
      operation: 'annul',
      install: (after: SeriesDto, refreshed: MatchDto) => {
        const spy = vi
          .spyOn(matchService, 'annulMatch')
          .mockRejectedValueOnce(staleError())
          .mockResolvedValue(
            observed(
              mutationResult(after, refreshed),
              '"mutation-after-reconfirm"',
            ),
          )
        return {
          assertOnce: () => expect(spy).toHaveBeenCalledOnce(),
          assertReconfirmed: () => {
            expect(spy).toHaveBeenCalledTimes(2)
            expect(spy.mock.calls[1]?.[1]).toEqual(spy.mock.calls[0]?.[1])
            expect(spy.mock.calls[1]?.[2]).toBe('"fresh-after-reload"')
          },
        }
      },
    },
    {
      operation: 'correction',
      install: (after: SeriesDto, refreshed: MatchDto) => {
        const spy = vi
          .spyOn(matchService, 'correctMatch')
          .mockRejectedValueOnce(staleError())
          .mockResolvedValue(
            observed(
              mutationResult(after, refreshed),
              '"mutation-after-reconfirm"',
            ),
          )
        return {
          assertOnce: () => expect(spy).toHaveBeenCalledOnce(),
          assertReconfirmed: () => {
            expect(spy).toHaveBeenCalledTimes(2)
            expect(spy.mock.calls[1]?.[1]).toEqual(spy.mock.calls[0]?.[1])
            expect(spy.mock.calls[1]?.[2]).toBe('"fresh-after-reload"')
          },
        }
      },
    },
  ] as const

  it.each(staleCases)(
    'recovers stale $operation with MV110, a real diff, preserved safe input and explicit reconfirmation',
    async ({ operation, install }) => {
      const { original, refreshed, field, beforeText, afterText } =
        staleConflictFixture(operation)
      const before = operationalSeries({
        partidas: [original],
        bloqueiosFearless: original.picks.map(({ championId }) => championId),
      })
      const after = operationalSeries({
        partidas: [refreshed],
        bloqueiosFearless: refreshed.picks.map(({ championId }) => championId),
      })
      vi.spyOn(matchService, 'getMatch')
        .mockResolvedValueOnce(observed(original, '"stale"'))
        .mockResolvedValue(observed(refreshed, '"fresh-after-reload"'))
      vi.spyOn(seriesService, 'getSeries')
        .mockResolvedValueOnce(observed(before, '"stale"'))
        .mockResolvedValue(observed(after, '"fresh-after-reload"'))
      const probe = install(after, refreshed)
      const wrapper = mount(MatchDetailView, {
        props: { matchId },
        global: globalMountOptions(),
      })
      await flushPromises()

      await openAndSubmit(wrapper, operation)
      await flushPromises()
      probe.assertOnce()
      const conflict = wrapper.get(
        '[role="alertdialog"][data-version-conflict]',
      )
      expect(conflict.text()).toContain(
        'A série mudou. Compare os dados e confirme novamente.',
      )
      const relevantDifference = conflict.get(
        `[data-conflict-field="${field}"]`,
      )
      expect(relevantDifference.get('[data-before]').text()).toContain(
        beforeText,
      )
      expect(relevantDifference.get('[data-after]').text()).toContain(afterText)
      expect(conflict.get('[data-preserved-input]').text()).toContain(
        operation === 'picks'
          ? '11'
          : operation === 'result'
            ? 'Surrender'
            : 'Evidência preservada',
      )
      probe.assertOnce()

      await getButton(wrapper, 'Confirmar novamente').trigger('click')
      await flushPromises()
      probe.assertReconfirmed()
    },
  )

  it('distinguishes MV126 idempotency conflict and does not offer stale reconfirmation', async () => {
    const match = matchDto()
    const series = operationalSeries({ partidas: [match] })
    const getMatch = vi
      .spyOn(matchService, 'getMatch')
      .mockResolvedValue(observed(match, '"current"'))
    const getSeries = vi
      .spyOn(seriesService, 'getSeries')
      .mockResolvedValue(observed(series, '"current"'))
    vi.spyOn(matchService, 'registerMatchRemake').mockRejectedValue(
      idempotencyError(),
    )
    const wrapper = mount(MatchDetailView, {
      props: { matchId },
      global: globalMountOptions(),
    })
    await flushPromises()
    await openAndSubmit(wrapper, 'remake', 'Justificativa não descartada')
    await flushPromises()

    expect(wrapper.get('[role="alert"]').text()).toContain(
      'Esta chave de idempotência já foi usada com outro conteúdo.',
    )
    expect(wrapper.find('[data-version-conflict]').exists()).toBe(false)
    expect(wrapper.get('textarea[name="remakeReason"]').element).toHaveProperty(
      'value',
      'Justificativa não descartada',
    )
    expect(getMatch).toHaveBeenCalledOnce()
    expect(getSeries).toHaveBeenCalledOnce()
  })
})

describe('capabilities and recoverable API states', () => {
  it.each([
    [['register-picks'], ['Salvar picks']],
    [['confirm'], ['Confirmar resultado']],
    [['remake'], ['Registrar remake']],
    [['annul'], ['Anular partida']],
    [['correct'], ['Corrigir partida']],
    [[], []],
  ] as const)('shows only authoritative actions %j', (actions, visible) => {
    const match = matchDto({ acoesPermitidas: [...actions] })
    const wrapper = mount(MatchOperationPanel, {
      props: {
        match,
        series: operationalSeries({ partidas: [match] }),
        busy: false,
      },
      global: globalMountOptions(),
    })
    const allActions = [
      'Salvar picks',
      'Confirmar resultado',
      'Registrar remake',
      'Anular partida',
      'Corrigir partida',
    ]
    for (const label of allActions) {
      expect(wrapper.text().includes(label)).toBe(
        (visible as readonly string[]).includes(label),
      )
    }
  })

  it('maps API fieldErrors inline, preserves input and focuses the invalid field', async () => {
    const match = completedMatch()
    const series = operationalSeries({ partidas: [match] })
    vi.spyOn(matchService, 'getMatch').mockResolvedValue(
      observed(match, '"current"'),
    )
    vi.spyOn(seriesService, 'getSeries').mockResolvedValue(
      observed(series, '"current"'),
    )
    vi.spyOn(seriesService, 'getSeriesResult').mockResolvedValue(
      seriesResult(series),
    )
    vi.spyOn(matchService, 'correctMatch').mockRejectedValue(validationError())
    const wrapper = mount(MatchDetailView, {
      attachTo: document.body,
      props: { matchId },
      global: globalMountOptions(),
    })
    await flushPromises()
    await openAndSubmit(wrapper, 'correction', 'Texto preservado')
    await flushPromises()
    const reason = wrapper.get('textarea[name="correctionReason"]')
    expect(reason.attributes('aria-invalid')).toBe('true')
    expect(wrapper.get('#correction-reason-error').text()).toBe(
      'A justificativa da correção é obrigatória',
    )
    expect(reason.element).toHaveProperty('value', 'Texto preservado')
    expect(document.activeElement).toBe(reason.element)
  })

  it.each([
    [401, 'MA001', 'Sua sessão expirou. Entre novamente para continuar.'],
    [403, 'MA002', 'Você não possui permissão para esta ação.'],
    [404, 'ME404', 'Esta partida não está disponível.'],
  ] as const)(
    'renders actionable %s state without leaking message codes',
    async (status, code, text) => {
      vi.spyOn(matchService, 'getMatch').mockRejectedValue(
        new matchService.MatchServiceError(status, code),
      )
      const wrapper = mount(MatchDetailView, {
        props: { matchId },
        global: globalMountOptions(),
      })
      await flushPromises()
      expect(wrapper.get('main [role="alert"]').text()).toContain(text)
      expect(wrapper.text()).not.toContain(code)
      expect(wrapper.find('form').exists()).toBe(false)
    },
  )

  it('provides semantic loading, empty and retry states', async () => {
    let resolveList!: (value: ReturnType<typeof seriesPage>) => void
    const pending = new Promise<ReturnType<typeof seriesPage>>((resolve) => {
      resolveList = resolve
    })
    const list = vi
      .spyOn(seriesService, 'listSeries')
      .mockReturnValueOnce(pending)
    const loading = mount(SeriesView, { global: globalMountOptions() })
    expect(loading.get('main').attributes('aria-busy')).toBe('true')
    expect(
      loading.get('[data-series-skeleton]').attributes('aria-hidden'),
    ).toBe('true')
    resolveList(seriesPage([]))
    await flushPromises()
    expect(loading.get('[role="status"]').text()).toContain(
      'Nenhuma série encontrada.',
    )

    list.mockRejectedValueOnce(new Error('network'))
    const failed = mount(SeriesView, { global: globalMountOptions() })
    await flushPromises()
    expect(failed.get('[role="alert"]').text()).toContain(
      'Não foi possível carregar as séries.',
    )
    list.mockResolvedValueOnce(seriesPage([]))
    await getButton(failed, 'Tentar novamente').trigger('click')
    await flushPromises()
    expect(failed.get('[role="status"]').text()).toContain(
      'Nenhuma série encontrada.',
    )
  })
})

describe('dialog, keyboard, busy and reduced-motion contracts', () => {
  it('traps focus, closes on Escape and restores focus to the opener', async () => {
    const wrapper = mount(MatchOperationPanel, {
      attachTo: document.body,
      props: { match: matchDto(), series: operationalSeries(), busy: false },
      global: globalMountOptions(),
    })
    const opener = getButton(wrapper, 'Registrar remake')
    ;(opener.element as HTMLElement).focus()
    await opener.trigger('click')
    await flushPromises()
    const dialog = wrapper.get('[role="alertdialog"]')
    const labelledBy = dialog.attributes('aria-labelledby')
    const describedBy = dialog.attributes('aria-describedby')
    expect(dialog.attributes('aria-modal')).toBe('true')
    expect(labelledBy).toBeTruthy()
    expect(describedBy).toBeTruthy()
    expect(wrapper.get(`#${labelledBy}`).text()).toBe('Registrar remake')
    expect(wrapper.get(`#${describedBy}`).text()).toBeTruthy()
    expect(document.activeElement).toBe(
      dialog.get('input[name="remakePickDecision"]').element,
    )
    const last = getButton(wrapper, 'Confirmar remake')
    ;(last.element as HTMLElement).focus()
    await dialog.trigger('keydown', { key: 'Tab' })
    expect(document.activeElement).toBe(
      dialog.get('input[name="remakePickDecision"]').element,
    )
    await dialog.trigger('keydown', { key: 'Tab', shiftKey: true })
    expect(document.activeElement).toBe(last.element)
    ;(last.element as HTMLElement).focus()
    await dialog.trigger('keydown', { key: 'Escape' })
    expect(wrapper.find('[role="alertdialog"]').exists()).toBe(false)
    expect(document.activeElement).toBe(opener.element)
  })

  it('keeps dialog open, disables controls and announces progress while busy', async () => {
    const wrapper = mount(MatchOperationPanel, {
      props: { match: matchDto(), series: operationalSeries(), busy: false },
      global: globalMountOptions(),
    })
    await getButton(wrapper, 'Registrar remake').trigger('click')
    await wrapper.setProps({ busy: true })
    const dialog = wrapper.get('[role="alertdialog"]')
    expect(dialog.attributes('aria-busy')).toBe('true')
    expect(
      dialog
        .findAll('button, input, textarea')
        .every((control) => control.attributes('disabled') !== undefined),
    ).toBe(true)
    await dialog.trigger('keydown', { key: 'Escape' })
    expect(wrapper.find('[role="alertdialog"]').exists()).toBe(true)
    expect(wrapper.get('[role="status"]').text()).toContain('Salvando…')
  })

  it('uses real focus-visible and reduced-motion CSS with semantic form controls', () => {
    const wrapper = mount(MatchOperationPanel, {
      attachTo: document.body,
      props: { match: matchDto(), series: operationalSeries(), busy: false },
      global: globalMountOptions(),
    })
    const source =
      competitiveComponentSources['./MatchOperationPanel.vue'] ?? ''
    expect(source).toMatch(
      /(?:button|input|select|textarea)[^{,]*:focus-visible/,
    )
    const reducedMotionRule = source.match(
      /@media\s*\(prefers-reduced-motion:\s*reduce\)\s*\{([\s\S]*?)\n\}/,
    )?.[1]
    expect(reducedMotionRule).toBeTruthy()
    expect(reducedMotionRule).toMatch(
      /transition(?:-duration)?\s*:\s*(?:none|0(?:ms|s))\b/,
    )
    expect(reducedMotionRule).toMatch(
      /animation(?:-duration)?\s*:\s*(?:none|0(?:ms|s))\b/,
    )
    expect(
      wrapper.get('[data-operation-feedback]').attributes('aria-live'),
    ).toBe('polite')
    expect(source).not.toMatch(/transition\s*:\s*all/)
    const firstAction = wrapper.get('button')
    ;(firstAction.element as HTMLElement).focus()
    expect(document.activeElement).toBe(firstAction.element)
    expect(wrapper.find('[onclick]').exists()).toBe(false)
    for (const control of wrapper.findAll('input, select, textarea')) {
      const id = control.attributes('id')
      expect(
        Boolean(control.attributes('aria-label')) ||
          Boolean(id && wrapper.find(`label[for="${id}"]`).exists()),
      ).toBe(true)
      expect(control.attributes('name')).toBeTruthy()
      expect(control.attributes('autocomplete')).toBe('off')
    }
  })

  it.each([
    ['pt', 'A justificativa da correção é obrigatória'],
    ['en', 'Correction justification is required'],
  ] as const)(
    'shows exact localized MV127 correction validation in %s',
    async (locale, expected) => {
      setLocale(locale)
      const wrapper = mount(MatchOperationPanel, {
        attachTo: document.body,
        props: {
          match: completedMatch(),
          series: operationalSeries(),
          busy: false,
        },
        global: globalMountOptions(),
      })
      await getButton(
        wrapper,
        locale === 'pt' ? 'Corrigir partida' : 'Correct match',
      ).trigger('click')
      await wrapper.get('input[name="correctedWinner"]').setValue(sideTwoId)
      await getButton(
        wrapper,
        locale === 'pt' ? 'Confirmar correção' : 'Confirm correction',
      ).trigger('click')

      const reason = wrapper.get('textarea[name="correctionReason"]')
      expect(wrapper.get('#correction-reason-error').text()).toBe(expected)
      expect(reason.attributes('aria-errormessage')).toBe(
        'correction-reason-error',
      )
      expect(document.activeElement).toBe(reason.element)
    },
  )
})

describe('PT/EN and structural i18n contract', () => {
  it.each([
    {
      locale: 'pt',
      scoreboard: 'Placar da série',
      fearless: 'Campeões bloqueados',
      actions: [
        'Salvar picks',
        'Confirmar resultado',
        'Registrar remake',
        'Anular partida',
        'Corrigir partida',
      ],
      forbidden:
        /Series scoreboard|Save picks|Confirm result|Register remake|Correct match/,
    },
    {
      locale: 'en',
      scoreboard: 'Series scoreboard',
      fearless: 'Blocked champions',
      actions: [
        'Save picks',
        'Confirm result',
        'Register remake',
        'Annul match',
        'Correct match',
      ],
      forbidden:
        /Placar da série|Salvar picks|Confirmar resultado|Registrar remake|Corrigir partida/,
    },
  ] as const)(
    'localizes visible, accessible, dialog and validation copy in $locale',
    async ({ locale, scoreboard, fearless, actions, forbidden }) => {
      setLocale(locale)
      const match = matchDto({
        acoesPermitidas: [
          'register-picks',
          'confirm',
          'remake',
          'annul',
          'correct',
        ],
      })
      const series = operationalSeries({ partidas: [match] })
      const score = mount(SeriesScoreboard, {
        props: { series },
        global: globalMountOptions(),
      })
      const blocks = mount(FearlessPanel, {
        props: { series },
        global: globalMountOptions(),
      })
      const operation = mount(MatchOperationPanel, {
        attachTo: document.body,
        props: { match, series, busy: false },
        global: globalMountOptions(),
      })
      expect(
        score.get('[data-series-scoreboard]').attributes('aria-label'),
      ).toBe(scoreboard)
      expect(blocks.get('[data-fearless-panel]').attributes('aria-label')).toBe(
        fearless,
      )
      for (const action of actions)
        expect(getButton(operation, action).exists()).toBe(true)
      await getButton(operation, actions[2]!).trigger('click')
      await getButton(
        operation,
        locale === 'pt' ? 'Confirmar remake' : 'Confirm remake',
      ).trigger('click')
      expect(operation.get('[role="alert"]').text()).toBeTruthy()
      expect(
        `${score.text()} ${blocks.text()} ${operation.text()}`,
      ).not.toMatch(forbidden)
    },
  )

  it('keeps competitive locale trees synchronized and includes every operational state', async () => {
    const [{ default: pt }, { default: en }] = await Promise.all([
      import('@/i18n/locales/pt.json'),
      import('@/i18n/locales/en.json'),
    ])
    const flatten = (value: object, prefix = ''): string[] =>
      Object.entries(value).flatMap(([key, child]) => {
        const path = prefix ? `${prefix}.${key}` : key
        return child && typeof child === 'object'
          ? flatten(child as object, path)
          : [path]
      })
    const ptKeys = flatten(pt).filter((key) =>
      key.startsWith('competitive.series'),
    )
    const enKeys = flatten(en).filter((key) =>
      key.startsWith('competitive.series'),
    )
    expect(ptKeys.sort()).toEqual(enKeys.sort())
    expect(ptKeys).toEqual(
      expect.arrayContaining([
        'competitive.series.loading',
        'competitive.series.empty',
        'competitive.series.errors.unauthorized',
        'competitive.series.errors.forbidden',
        'competitive.series.errors.notFound',
        'competitive.series.errors.versionConflict',
        'competitive.series.errors.idempotencyConflict',
        'competitive.series.match.picks.save',
        'competitive.series.match.result.surrender',
        'competitive.series.match.remake.confirm',
        'competitive.series.match.annul.confirm',
        'competitive.series.match.correction.confirm',
        'competitive.series.match.conflict.reconfirm',
      ]),
    )
  })
})

describe('T048 review findings', () => {
  it('leaves failed conflict reconciliation retryable and builds differences only after a fresh reload', async () => {
    const original = completedMatch({ acoesPermitidas: ['remake'] })
    const originalSeries = operationalSeries({ partidas: [original] })
    const refreshed = matchDto({
      estado: 'Remake',
      decisaoPicksRemake: 'DesconsiderarPicks',
      acoesPermitidas: ['correct'],
    })
    const refreshedSeries = operationalSeries({ partidas: [refreshed] })
    vi.spyOn(matchService, 'getMatch')
      .mockResolvedValueOnce(observed(original, '"current"'))
      .mockRejectedValueOnce(new Error('reconciliation unavailable'))
      .mockResolvedValue(observed(refreshed, '"fresh"'))
    vi.spyOn(seriesService, 'getSeries')
      .mockResolvedValueOnce(observed(originalSeries, '"current"'))
      .mockResolvedValue(observed(refreshedSeries, '"fresh"'))
    vi.spyOn(seriesService, 'getSeriesResult').mockResolvedValue(
      seriesResult(refreshedSeries),
    )
    vi.spyOn(matchService, 'registerMatchRemake').mockRejectedValue(
      staleError(),
    )

    const wrapper = mount(MatchDetailView, {
      props: { matchId },
      global: globalMountOptions(),
    })
    await flushPromises()
    await openAndSubmit(wrapper, 'remake', 'Entrada preservada')
    await flushPromises()

    expect(wrapper.get('main').attributes('aria-busy')).toBe('false')
    expect(wrapper.get('[data-reconciliation-error]').text()).toContain(
      'Não foi possível sincronizar os dados competitivos.',
    )
    expect(wrapper.find('[data-version-conflict]').exists()).toBe(false)

    await getButton(wrapper, 'Tentar sincronizar novamente').trigger('click')
    await flushPromises()
    expect(
      wrapper.get('[data-version-conflict] [data-after]').text(),
    ).toContain('Desconsiderar picks')
  })

  it.each([
    ['remake', 'remakeReason'],
    ['annul', 'annulReason'],
    ['correction', 'correctionReason'],
  ] as const)(
    'maps Justificativa validation to the active %s field',
    async (operation, fieldName) => {
      const match = completedMatch({
        acoesPermitidas: [operation === 'correction' ? 'correct' : operation],
      })
      const series = operationalSeries({ partidas: [match] })
      vi.spyOn(matchService, 'getMatch').mockResolvedValue(
        observed(match, '"current"'),
      )
      vi.spyOn(seriesService, 'getSeries').mockResolvedValue(
        observed(series, '"current"'),
      )
      vi.spyOn(seriesService, 'getSeriesResult').mockResolvedValue(
        seriesResult(series),
      )
      const error = new matchService.MatchServiceError(
        400,
        'ME031',
        [],
        [
          {
            field: 'Justificativa',
            messageCode: 'MV127',
            message: 'Justificativa estruturada',
          },
        ],
      )
      if (operation === 'remake')
        vi.spyOn(matchService, 'registerMatchRemake').mockRejectedValue(error)
      else if (operation === 'annul')
        vi.spyOn(matchService, 'annulMatch').mockRejectedValue(error)
      else vi.spyOn(matchService, 'correctMatch').mockRejectedValue(error)

      const wrapper = mount(MatchDetailView, {
        attachTo: document.body,
        props: { matchId },
        global: globalMountOptions(),
      })
      await flushPromises()
      await openAndSubmit(wrapper, operation, 'Entrada preservada')
      await flushPromises()
      const field = wrapper.get(`textarea[name="${fieldName}"]`)
      expect(field.attributes('aria-invalid')).toBe('true')
      expect(document.activeElement).toBe(field.element)
      expect(
        field.element.parentElement?.nextElementSibling?.textContent,
      ).toContain('Justificativa estruturada')
    },
  )

  it('renders captain snapshot identity and a localized fallback without UUIDs', async () => {
    const series = operationalSeries()
    series.lados[0] = {
      ...series.lados[0]!,
      capitaoJogadorId: sideOneId,
      participantes: [
        {
          jogadorId: sideOneId,
          nomeExibicao: 'Capitã Ahri',
          tag: 'MID',
        },
      ],
    } as (typeof series.lados)[number] & {
      participantes: Array<{
        jogadorId: string
        nomeExibicao: string
        tag?: string | null
      }>
    }
    series.lados[1] = {
      ...series.lados[1]!,
      capitaoJogadorId: sideTwoId,
    }
    vi.spyOn(seriesService, 'getSeries').mockResolvedValue(
      observed(series, '"captains"'),
    )
    vi.spyOn(seriesService, 'getSeriesResult').mockResolvedValue(
      seriesResult(series),
    )
    const wrapper = mount(SeriesDetailView, {
      props: { seriesId },
      global: globalMountOptions(),
    })
    await flushPromises()

    expect(wrapper.text()).toContain('Capitã Ahri · MID')
    expect(wrapper.text()).toContain('Capitão não identificado')
    expect(wrapper.text()).not.toContain(sideOneId)
    expect(wrapper.text()).not.toContain(sideTwoId)
  })

  it('uses numeric input intent for correction champions', async () => {
    const match = completedMatch({ acoesPermitidas: ['correct'] })
    const wrapper = mount(MatchOperationPanel, {
      props: {
        match,
        series: operationalSeries({ partidas: [match] }),
        busy: false,
      },
      global: globalMountOptions(),
    })
    await getButton(wrapper, 'Corrigir partida').trigger('click')
    for (const input of wrapper.findAll('input[data-correction-pick]'))
      expect(input.attributes('inputmode')).toBe('numeric')
  })

  it('contains overscroll in every scrollable operation dialog', () => {
    const sources = [
      ...Object.values(competitiveComponentSources),
      ...Object.values(operationViewSources),
    ]
    expect(sources).toHaveLength(3)
    for (const source of sources) {
      expect(source).toContain('overflow: auto')
      expect(source).toContain('overscroll-behavior: contain')
    }
  })

  it('never synthesizes confirm or remake actions from correct', async () => {
    const confirmed = completedMatch({ acoesPermitidas: ['correct'] })
    const series = operationalSeries({ partidas: [confirmed] })
    vi.spyOn(matchService, 'getMatch').mockResolvedValue(
      observed(confirmed, '"authoritative"'),
    )
    vi.spyOn(seriesService, 'getSeries').mockResolvedValue(
      observed(series, '"authoritative"'),
    )
    vi.spyOn(seriesService, 'getSeriesResult').mockResolvedValue(
      seriesResult(series),
    )

    const wrapper = mount(MatchDetailView, {
      props: { matchId },
      global: globalMountOptions(),
    })
    await flushPromises()

    expect(getButton(wrapper, 'Corrigir partida').exists()).toBe(true)
    expect(wrapper.text()).not.toContain('Confirmar resultado')
    expect(wrapper.text()).not.toContain('Registrar remake')
  })

  it('resets annul-Series confirmation whenever a dialog closes', async () => {
    const match = completedMatch({ acoesPermitidas: ['annul'] })
    const wrapper = mount(MatchOperationPanel, {
      attachTo: document.body,
      props: {
        match,
        series: operationalSeries({
          estado: 'Concluida',
          placar: [2, 0],
          partidas: [match, completedMatch({ id: 'match-two', ordem: 2 })],
        }),
        busy: false,
      },
      global: globalMountOptions(),
    })

    await getButton(wrapper, 'Anular partida').trigger('click')
    await wrapper.get('input[name="annulSeries"]').setValue(true)
    await getButton(wrapper, 'Cancelar').trigger('click')
    await getButton(wrapper, 'Anular partida').trigger('click')
    await wrapper.get('textarea[name="annulReason"]').setValue('Revisão')
    await getButton(wrapper, 'Confirmar anulação').trigger('click')

    expect(wrapper.emitted('annul')?.[0]?.[0]).toMatchObject({
      anularSerieSeInconclusiva: false,
    })
  })

  it('requires visible before/after confirmation when correction annuls the Series', async () => {
    const first = completedMatch({ acoesPermitidas: ['correct'] })
    const second = completedMatch({ id: 'match-two', ordem: 2 })
    const wrapper = mount(MatchOperationPanel, {
      attachTo: document.body,
      props: {
        match: second,
        series: operationalSeries({
          estado: 'Concluida',
          placar: [2, 0],
          partidas: [first, second],
        }),
        busy: false,
      },
      global: globalMountOptions(),
    })

    await getButton(wrapper, 'Corrigir partida').trigger('click')
    await wrapper
      .get('input[name="correctedWinner"][value="' + sideTwoId + '"]')
      .setValue(true)
    await wrapper
      .get('textarea[name="correctionReason"]')
      .setValue('Resultado revisto')
    expect(wrapper.get('[data-correction-series-impact]').text()).toContain(
      '2 — 0',
    )
    expect(wrapper.get('[data-correction-series-impact]').text()).toContain(
      '1 — 1',
    )
    await getButton(wrapper, 'Confirmar correção').trigger('click')
    expect(wrapper.emitted('correct')).toBeUndefined()
    expect(wrapper.get('#correction-annul-series-error').text()).toBeTruthy()

    await wrapper.get('input[name="correctionAnnulSeries"]').setValue(true)
    await getButton(wrapper, 'Confirmar correção').trigger('click')
    expect(wrapper.emitted('correct')?.[0]?.[0]).toEqual({
      justificativa: 'Resultado revisto',
      ladoVencedorId: sideTwoId,
      motivoTermino: 'Normal',
      anularSerieSeInconclusiva: true,
    })
  })

  it('focuses and associates winner and remake validation errors', async () => {
    const match = matchDto({ acoesPermitidas: ['confirm', 'remake'] })
    const wrapper = mount(MatchOperationPanel, {
      attachTo: document.body,
      props: { match, series: operationalSeries(), busy: false },
      global: globalMountOptions(),
    })

    await getButton(wrapper, 'Confirmar resultado').trigger('click')
    const winner = wrapper.get('input[name="winner"]')
    expect(winner.attributes('aria-errormessage')).toBe('winner-error')
    expect(document.activeElement).toBe(winner.element)

    await getButton(wrapper, 'Registrar remake').trigger('click')
    await wrapper.get('input[name="remakePickDecision"]').setValue(true)
    await getButton(wrapper, 'Confirmar remake').trigger('click')
    const reason = wrapper.get('textarea[name="remakeReason"]')
    expect(reason.attributes('aria-errormessage')).toBe('remake-reason-error')
    expect(document.activeElement).toBe(reason.element)
  })

  it('uses invalidations as an atomic reload and never duplicates a replayed Match', async () => {
    const existing = matchDto()
    const before = operationalSeries({ partidas: [existing] })
    const after = operationalSeries({
      partidas: [existing],
      acoesPermitidas: ['cancel'],
    })
    vi.spyOn(seriesService, 'getSeries')
      .mockResolvedValueOnce(observed(before, '"before"'))
      .mockResolvedValueOnce(observed(after, '"after"'))
    vi.spyOn(seriesService, 'getSeriesResult')
      .mockResolvedValueOnce(seriesResult(before))
      .mockResolvedValueOnce(seriesResult(after))
    vi.spyOn(seriesService, 'createMatch').mockResolvedValue({
      ...observed(existing, '"mutation"'),
      idempotencyReplayed: true,
    })

    const wrapper = mount(SeriesDetailView, {
      props: { seriesId },
      global: globalMountOptions(),
    })
    await flushPromises()
    await getButton(wrapper, 'Criar próxima partida').trigger('click')
    await flushPromises()

    expect(
      wrapper.findAll('ol[aria-label="Partidas da série"] li'),
    ).toHaveLength(1)
    expect(seriesService.getSeries).toHaveBeenCalledTimes(2)
    expect(wrapper.text()).not.toContain('Criar próxima partida')
  })

  it('shows Series conflict differences and reconfirms createMatch only explicitly', async () => {
    const before = operationalSeries({
      partidas: [],
      acoesPermitidas: ['create-match'],
    })
    const concurrent = operationalSeries({
      partidas: [matchDto()],
      acoesPermitidas: ['create-match'],
    })
    vi.spyOn(seriesService, 'getSeries')
      .mockResolvedValueOnce(observed(before, '"before"'))
      .mockResolvedValue(observed(concurrent, '"fresh"'))
    vi.spyOn(seriesService, 'getSeriesResult').mockResolvedValue(
      seriesResult(concurrent),
    )
    const create = vi
      .spyOn(seriesService, 'createMatch')
      .mockRejectedValueOnce(new seriesService.SeriesServiceError(409, 'MV110'))
      .mockResolvedValue(observed(matchDto({ ordem: 2 }), '"mutation"'))

    const wrapper = mount(SeriesDetailView, {
      attachTo: document.body,
      props: { seriesId },
      global: globalMountOptions(),
    })
    await flushPromises()
    await getButton(wrapper, 'Criar próxima partida').trigger('click')
    await flushPromises()

    const conflict = wrapper.get('[data-series-version-conflict]')
    expect(conflict.text()).toContain('0')
    expect(conflict.text()).toContain('1')
    expect(create).toHaveBeenCalledOnce()
    await getButton(wrapper, 'Confirmar novamente').trigger('click')
    await flushPromises()
    expect(create).toHaveBeenCalledTimes(2)
    expect(create.mock.calls[1]?.[1]).toBe('"fresh"')
  })

  it('removes only the attempted Series action after 403', async () => {
    const series = operationalSeries({
      estado: 'Agendada',
      acoesPermitidas: ['start', 'cancel'],
    })
    vi.spyOn(seriesService, 'getSeries').mockResolvedValue(
      observed(series, '"current"'),
    )
    vi.spyOn(seriesService, 'getSeriesResult').mockResolvedValue(
      seriesResult(series),
    )
    vi.spyOn(seriesService, 'startSeries').mockRejectedValue(
      new seriesService.SeriesServiceError(403, 'MA002'),
    )

    const wrapper = mount(SeriesDetailView, {
      props: { seriesId },
      global: globalMountOptions(),
    })
    await flushPromises()
    await getButton(wrapper, 'Iniciar série').trigger('click')
    await getButton(wrapper, 'Confirmar início').trigger('click')
    await flushPromises()

    expect(wrapper.text()).not.toContain('Iniciar série')
    expect(wrapper.text()).toContain('Cancelar série')
  })

  it('removes only the attempted Match action after 403', async () => {
    const match = matchDto({ acoesPermitidas: ['confirm', 'annul'] })
    const series = operationalSeries({ partidas: [match] })
    vi.spyOn(matchService, 'getMatch').mockResolvedValue(
      observed(match, '"current"'),
    )
    vi.spyOn(seriesService, 'getSeries').mockResolvedValue(
      observed(series, '"current"'),
    )
    vi.spyOn(seriesService, 'getSeriesResult').mockResolvedValue(
      seriesResult(series),
    )
    vi.spyOn(matchService, 'confirmMatchResult').mockRejectedValue(
      new matchService.MatchServiceError(403, 'MA002'),
    )
    const wrapper = mount(MatchDetailView, {
      props: { matchId },
      global: globalMountOptions(),
    })
    await flushPromises()
    await wrapper.get('input[name="winner"]').setValue(true)
    await getButton(wrapper, 'Confirmar resultado').trigger('click')
    await flushPromises()

    expect(wrapper.text()).not.toContain('Confirmar resultado')
    expect(wrapper.text()).toContain('Anular partida')
  })

  it('blocks stale reconfirmation after one finite reconciliation attempt', async () => {
    const match = matchDto({ acoesPermitidas: ['remake'] })
    const series = operationalSeries({ partidas: [match] })
    const getMatch = vi
      .spyOn(matchService, 'getMatch')
      .mockResolvedValueOnce(observed(match, '"current"'))
      .mockResolvedValueOnce(observed(match, '"match-a"'))
      .mockResolvedValueOnce(observed(match, '"match-b"'))
    vi.spyOn(seriesService, 'getSeries')
      .mockResolvedValueOnce(observed(series, '"current"'))
      .mockResolvedValueOnce(observed(series, '"series-a"'))
      .mockResolvedValueOnce(observed(series, '"series-b"'))
    vi.spyOn(seriesService, 'getSeriesResult').mockResolvedValue(
      seriesResult(series),
    )
    vi.spyOn(matchService, 'registerMatchRemake').mockRejectedValue(
      staleError(),
    )
    const wrapper = mount(MatchDetailView, {
      props: { matchId },
      global: globalMountOptions(),
    })
    await flushPromises()
    await openAndSubmit(wrapper, 'remake', 'Conservar entrada')
    await flushPromises()

    expect(wrapper.find('[data-version-conflict]').exists()).toBe(false)
    expect(
      getButton(wrapper, 'Tentar sincronizar novamente').attributes('disabled'),
    ).toBeUndefined()
    expect(getMatch).toHaveBeenCalledTimes(3)
    await flushPromises()
    expect(getMatch).toHaveBeenCalledTimes(3)
  })

  it('applies Season scope to URL and list requests', async () => {
    globalThis.history.replaceState({}, '', '/series')
    const season = {
      id: '00000000-0000-4000-8000-000000000001',
      nome: 'Temporada ativa',
      ano: 2026,
      ordemNoAno: 1,
      dataInicio: '2026-01-01',
      dataFimExclusiva: '2027-01-01',
      estado: 'Ativa' as const,
      versao: 1,
    }
    vi.spyOn(seasonService, 'listSeasons').mockResolvedValue(
      observed(
        {
          page: 1,
          pageSize: 100,
          items: [season],
          totalItems: 1,
          totalPages: 1,
          calendarioConfigurado: true,
          temporadaAtual: season,
          versaoCalendario: 1,
        },
        '"calendar"',
      ),
    )
    const list = vi
      .spyOn(seriesService, 'listSeries')
      .mockResolvedValue(seriesPage([]))
    const wrapper = mount(SeriesView, { global: globalMountOptions() })
    await flushPromises()

    await getButton(wrapper, 'Todas as temporadas').trigger('click')
    await flushPromises()
    expect(list).toHaveBeenLastCalledWith(
      expect.objectContaining({ scope: { mode: 'all' } }),
    )
    expect(new URL(globalThis.location.href).searchParams.get('todas')).toBe(
      'true',
    )
  })

  it('renders localized format and existing competitive context IDs', async () => {
    stubMedia({ mobile: false })
    const series = operationalSeries()
    vi.spyOn(seasonService, 'listSeasons').mockResolvedValue(
      observed(
        {
          page: 1,
          pageSize: 100,
          items: [],
          totalItems: 0,
          totalPages: 0,
          calendarioConfigurado: true,
          temporadaAtual: null,
          versaoCalendario: 1,
        },
        null,
      ),
    )
    vi.spyOn(seriesService, 'listSeries').mockResolvedValue(
      seriesPage([series]),
    )
    const wrapper = mount(SeriesView, { global: globalMountOptions() })
    await flushPromises()

    expect(wrapper.text()).toContain('Melhor de 3')
    expect(wrapper.text()).toContain(series.seasonId)
    expect(wrapper.text()).toContain(series.competicaoId)
    expect(wrapper.text()).toContain(series.rodadaId)
    expect(wrapper.text()).toContain(series.versaoRegrasId)
  })

  it('traps and restores focus for Series view dialogs', async () => {
    const series = operationalSeries({
      estado: 'Agendada',
      acoesPermitidas: ['start'],
    })
    vi.spyOn(seriesService, 'getSeries').mockResolvedValue(
      observed(series, '"current"'),
    )
    vi.spyOn(seriesService, 'getSeriesResult').mockResolvedValue(
      seriesResult(series),
    )
    const wrapper = mount(SeriesDetailView, {
      attachTo: document.body,
      props: { seriesId },
      global: globalMountOptions(),
    })
    await flushPromises()
    const opener = getButton(wrapper, 'Iniciar série')
    ;(opener.element as HTMLElement).focus()
    await opener.trigger('click')
    await flushPromises()
    const dialog = wrapper.get('[role="alertdialog"]')
    expect(dialog.element.contains(document.activeElement)).toBe(true)
    await dialog.trigger('keydown', { key: 'Escape' })
    expect(wrapper.find('[role="alertdialog"]').exists()).toBe(false)
    expect(document.activeElement).toBe(opener.element)
  })
})
