// Test-only structural types. T031, T047 and T064 must replace fixture-facing
// imports with the production DTO types when those contracts are implemented.

export type SeasonState = 'Planejada' | 'Ativa' | 'Encerrada'
export type SeriesType = 'DiariaTemporaria' | 'ConfrontoOficial' | 'Amistoso'
export type SeriesFormat = 'Md3' | 'Md5'
export type DraftMode = 'Padrao' | 'Fearless'
export type SeriesState =
  | 'Agendada'
  | 'EmAndamento'
  | 'Concluida'
  | 'Cancelada'
  | 'Anulada'
export type MatchState = 'Rascunho' | 'Confirmada' | 'Remake' | 'Anulada'
export type RemakePickDecision = 'PreservarPicks' | 'DesconsiderarPicks'
export type MatchEndReason = 'Normal' | 'Surrender'
export type SideType = 'Temporario' | 'TimeOficial'

export interface SeasonFixture {
  id: string
  nome: string
  ano: number
  ordemNoAno: number
  dataInicio: string
  dataFimExclusiva: string
  estado: SeasonState
  versao: number
  etag: string
  acoesPermitidas: string[]
}

export interface SeasonScopeMetadataFixture {
  calendarioConfigurado: boolean
  temporadaAtual: SeasonFixture | null
  seasonsIncluidas: SeasonFixture[]
  versaoCalendario: number
  calendarioEtag: string
}

export interface RoundFixture {
  id: string
  competicaoId: string
  nome: string
  ordem: number
  versao: number
}

export interface RulesVersionFixture {
  id: string
  seasonId: string
  competicaoId: string | null
  numero: number
  formato: SeriesFormat
  modoDraft: DraftMode
  publicadaEm: string
}

export interface CompetitionFixture {
  id: string
  seasonId: string
  nome: string
  codigo: string
  circuitoDiario: boolean
  rodadas: RoundFixture[]
  regrasPublicadas: RulesVersionFixture[]
  versao: number
  etag: string
  acoesPermitidas: string[]
}

export interface SideFixture {
  id: string
  tipo: SideType
  origemId: string
  nome: string
  tag: string | null
  capitaoJogadorId: string | null
}

export interface EventFixture {
  id: string
  seasonId: string
  nome: string
  modoDraft: 'Padrao'
  times: SideFixture[]
  serieIds: string[]
  versao: number
  etag: string
  acoesPermitidas: string[]
}

export interface PickFixture {
  ladoSerieId: string
  championId: number
  ordem: number
}

export interface MatchFixture {
  id: string
  serieId: string
  ordem: number
  estado: MatchState
  ladoVencedorId: string | null
  motivoTermino: MatchEndReason | null
  decisaoPicksRemake: RemakePickDecision | null
  picks: PickFixture[]
  conflitoFearless: boolean
  versao: number
  serieEtag: string
  acoesPermitidas: string[]
}

export interface SeriesFixture {
  id: string
  seasonId: string
  competicaoId: string | null
  rodadaId: string | null
  versaoRegrasId: string
  eventoId: string | null
  tipo: SeriesType
  formato: SeriesFormat
  modoDraft: DraftMode
  fearlessHabilitado: boolean
  estado: SeriesState
  agendadaPara: string
  lados: SideFixture[]
  placar: [number, number]
  partidas: MatchFixture[]
  bloqueiosFearless: number[]
  elegivelOficial: boolean
  revisaoNecessaria: boolean
  versao: number
  etag: string
  acoesPermitidas: string[]
}

export interface CompetitiveCapabilitiesFixture {
  CanManageSeasons: boolean
  CanManageCompetitions: boolean
  CanManageMatches: boolean
  CanFinalizeMatches: boolean
  CanViewCompetitiveAudit: boolean
}

const ids = {
  season: '00000000-0000-4000-8000-000000000001',
  competition: '00000000-0000-4000-8000-000000000002',
  round: '00000000-0000-4000-8000-000000000003',
  rules: '00000000-0000-4000-8000-000000000004',
  event: '00000000-0000-4000-8000-000000000005',
  series: '00000000-0000-4000-8000-000000000006',
  match: '00000000-0000-4000-8000-000000000007',
  sideOne: '00000000-0000-4000-8000-000000000008',
  sideTwo: '00000000-0000-4000-8000-000000000009',
  teamOne: '00000000-0000-4000-8000-000000000010',
  teamTwo: '00000000-0000-4000-8000-000000000011',
  teamThree: '00000000-0000-4000-8000-000000000012',
  teamFour: '00000000-0000-4000-8000-000000000013',
} as const

const cloneSeason = (season: SeasonFixture): SeasonFixture => ({
  ...season,
  acoesPermitidas: [...season.acoesPermitidas],
})

const cloneSide = (side: SideFixture): SideFixture => ({ ...side })

const cloneMatch = (match: MatchFixture): MatchFixture => ({
  ...match,
  picks: match.picks.map((pick) => ({ ...pick })),
  acoesPermitidas: [...match.acoesPermitidas],
})

const sanitizeOverrides = <T extends object>(
  overrides: Partial<T>,
): Partial<T> => {
  const sanitized = { ...overrides }

  for (const key of Object.keys(sanitized) as (keyof T)[]) {
    if (sanitized[key] === undefined) {
      delete sanitized[key]
    }
  }

  return sanitized
}

export function makeSeason(
  overrides: Partial<SeasonFixture> = {},
): SeasonFixture {
  overrides = sanitizeOverrides(overrides)
  const defaultActions = ['edit', 'close']
  const fixture: SeasonFixture = {
    id: ids.season,
    nome: 'season-fixture',
    ano: 2026,
    ordemNoAno: 1,
    dataInicio: '2026-01-01',
    dataFimExclusiva: '2027-01-01',
    estado: 'Ativa',
    versao: 1,
    etag: '"1"',
    acoesPermitidas: defaultActions,
    ...overrides,
  }

  fixture.acoesPermitidas = [...(overrides.acoesPermitidas ?? defaultActions)]
  return fixture
}

export function makeSeasonScopeMetadata(
  overrides: Partial<SeasonScopeMetadataFixture> = {},
): SeasonScopeMetadataFixture {
  overrides = sanitizeOverrides(overrides)
  const defaultSeason = makeSeason()
  const currentSeason =
    overrides.temporadaAtual === undefined
      ? defaultSeason
      : overrides.temporadaAtual

  const fixture: SeasonScopeMetadataFixture = {
    calendarioConfigurado: true,
    temporadaAtual: currentSeason ? cloneSeason(currentSeason) : null,
    seasonsIncluidas: (overrides.seasonsIncluidas ?? [defaultSeason]).map(
      cloneSeason,
    ),
    versaoCalendario: 1,
    calendarioEtag: '"1"',
    ...overrides,
  }

  fixture.temporadaAtual = currentSeason ? cloneSeason(currentSeason) : null
  fixture.seasonsIncluidas = (
    overrides.seasonsIncluidas ?? [defaultSeason]
  ).map(cloneSeason)
  return fixture
}

export function makeCompetition(
  overrides: Partial<CompetitionFixture> = {},
): CompetitionFixture {
  overrides = sanitizeOverrides(overrides)
  const defaultActions = ['edit', 'create-round', 'publish-rules']
  const round: RoundFixture = {
    id: ids.round,
    competicaoId: ids.competition,
    nome: 'round-fixture',
    ordem: 1,
    versao: 1,
  }
  const rules: RulesVersionFixture = {
    id: ids.rules,
    seasonId: ids.season,
    competicaoId: ids.competition,
    numero: 1,
    formato: 'Md3',
    modoDraft: 'Padrao',
    publicadaEm: '2026-01-01T12:00:00Z',
  }

  const fixture: CompetitionFixture = {
    id: ids.competition,
    seasonId: ids.season,
    nome: 'competition-fixture',
    codigo: 'competition-code',
    circuitoDiario: true,
    rodadas: [round],
    regrasPublicadas: [rules],
    versao: 1,
    etag: '"1"',
    acoesPermitidas: defaultActions,
    ...overrides,
  }

  fixture.rodadas = (overrides.rodadas ?? [round]).map((item) => ({ ...item }))
  fixture.regrasPublicadas = (overrides.regrasPublicadas ?? [rules]).map(
    (item) => ({
      ...item,
    }),
  )
  fixture.acoesPermitidas = [...(overrides.acoesPermitidas ?? defaultActions)]
  return fixture
}

export function makeEvent(overrides: Partial<EventFixture> = {}): EventFixture {
  overrides = sanitizeOverrides(overrides)
  const defaultActions = ['edit', 'associate-series']
  const teams: SideFixture[] = [
    ids.teamOne,
    ids.teamTwo,
    ids.teamThree,
    ids.teamFour,
  ].map((teamId, index) => ({
    id: teamId,
    tipo: 'TimeOficial',
    origemId: teamId,
    nome: `team-fixture-${index + 1}`,
    tag: `T${index + 1}`,
    capitaoJogadorId: null,
  }))

  const fixture: EventFixture = {
    id: ids.event,
    seasonId: ids.season,
    nome: 'event-fixture',
    modoDraft: 'Padrao',
    times: teams,
    serieIds: [],
    versao: 1,
    etag: '"1"',
    acoesPermitidas: defaultActions,
    ...overrides,
  }

  fixture.times = (overrides.times ?? teams).map(cloneSide)
  fixture.serieIds = [...(overrides.serieIds ?? [])]
  fixture.acoesPermitidas = [...(overrides.acoesPermitidas ?? defaultActions)]
  return fixture
}

export function makeMatch(overrides: Partial<MatchFixture> = {}): MatchFixture {
  overrides = sanitizeOverrides(overrides)
  const defaultActions = ['register-picks', 'confirm', 'remake']
  const fixture: MatchFixture = {
    id: ids.match,
    serieId: ids.series,
    ordem: 1,
    estado: 'Rascunho',
    ladoVencedorId: null,
    motivoTermino: null,
    decisaoPicksRemake: null,
    picks: [],
    conflitoFearless: false,
    versao: 1,
    serieEtag: '"1"',
    acoesPermitidas: defaultActions,
    ...overrides,
  }

  fixture.picks = (overrides.picks ?? []).map((pick) => ({ ...pick }))
  fixture.acoesPermitidas = [...(overrides.acoesPermitidas ?? defaultActions)]
  return fixture
}

export function makeSeries(
  overrides: Partial<SeriesFixture> = {},
): SeriesFixture {
  overrides = sanitizeOverrides(overrides)
  const defaultScore: [number, number] = [0, 0]
  const defaultActions = ['start', 'cancel']
  const sides: [SideFixture, SideFixture] = [
    {
      id: ids.sideOne,
      tipo: 'TimeOficial',
      origemId: ids.teamOne,
      nome: 'side-fixture-1',
      tag: 'S1',
      capitaoJogadorId: null,
    },
    {
      id: ids.sideTwo,
      tipo: 'TimeOficial',
      origemId: ids.teamTwo,
      nome: 'side-fixture-2',
      tag: 'S2',
      capitaoJogadorId: null,
    },
  ]

  const fixture: SeriesFixture = {
    id: ids.series,
    seasonId: ids.season,
    competicaoId: ids.competition,
    rodadaId: ids.round,
    versaoRegrasId: ids.rules,
    eventoId: null,
    tipo: 'ConfrontoOficial',
    formato: 'Md3',
    modoDraft: 'Padrao',
    fearlessHabilitado: false,
    estado: 'Agendada',
    agendadaPara: '2026-06-01T22:00:00Z',
    lados: sides,
    placar: defaultScore,
    partidas: [],
    bloqueiosFearless: [],
    elegivelOficial: true,
    revisaoNecessaria: false,
    versao: 1,
    etag: '"1"',
    acoesPermitidas: defaultActions,
    ...overrides,
  }

  fixture.lados = (overrides.lados ?? sides).map(cloneSide)
  fixture.placar = [...(overrides.placar ?? defaultScore)]
  fixture.partidas = (overrides.partidas ?? []).map(cloneMatch)
  fixture.bloqueiosFearless = [...(overrides.bloqueiosFearless ?? [])]
  fixture.acoesPermitidas = [...(overrides.acoesPermitidas ?? defaultActions)]
  return fixture
}

export function makeCapabilities(
  overrides: Partial<CompetitiveCapabilitiesFixture> = {},
): CompetitiveCapabilitiesFixture {
  overrides = sanitizeOverrides(overrides)
  return {
    CanManageSeasons: true,
    CanManageCompetitions: true,
    CanManageMatches: true,
    CanFinalizeMatches: true,
    CanViewCompetitiveAudit: true,
    ...overrides,
  }
}
