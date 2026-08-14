import { describe, expect, it } from 'vitest'

import {
  makeCapabilities,
  makeCompetition,
  makeEvent,
  makeMatch,
  makeSeason,
  makeSeasonScopeMetadata,
  makeSeries,
} from './competitiveFixtures'

describe('competitive fixtures', () => {
  it('creates deterministic contract-shaped defaults', () => {
    const season = makeSeason()
    const competition = makeCompetition()
    const event = makeEvent()
    const series = makeSeries()
    const match = makeMatch()
    const capabilities = makeCapabilities()

    expect(season).toMatchObject({
      id: '00000000-0000-4000-8000-000000000001',
      estado: 'Ativa',
      versao: 1,
      etag: '"1"',
    })
    expect(competition).toMatchObject({
      seasonId: season.id,
      circuitoDiario: true,
      versao: 1,
      etag: '"1"',
    })
    expect(event).toMatchObject({
      seasonId: season.id,
      modoDraft: 'Padrao',
      versao: 1,
      etag: '"1"',
    })
    expect(event.times).toHaveLength(4)
    expect(series).toMatchObject({
      seasonId: season.id,
      tipo: 'ConfrontoOficial',
      formato: 'Md3',
      modoDraft: 'Padrao',
      elegivelOficial: true,
      versao: 1,
      etag: '"1"',
    })
    expect(match).toMatchObject({
      serieId: series.id,
      estado: 'Rascunho',
      decisaoPicksRemake: null,
      conflitoFearless: false,
      versao: 1,
      serieEtag: '"1"',
    })
    expect(capabilities).toEqual({
      CanManageSeasons: true,
      CanManageCompetitions: true,
      CanManageMatches: true,
      CanFinalizeMatches: true,
      CanViewCompetitiveAudit: true,
    })
  })

  it('applies partial overrides for friendly, Fearless and remake scenarios', () => {
    const friendlySeries = makeSeries({
      tipo: 'Amistoso',
      competicaoId: null,
      rodadaId: null,
      modoDraft: 'Fearless',
      elegivelOficial: false,
      bloqueiosFearless: [1, 2],
      revisaoNecessaria: true,
    })
    const remake = makeMatch({
      estado: 'Remake',
      decisaoPicksRemake: 'PreservarPicks',
      picks: [
        { ladoSerieId: friendlySeries.lados[0]!.id, championId: 1, ordem: 1 },
      ],
    })
    const restrictedCapabilities = makeCapabilities({
      CanFinalizeMatches: false,
    })

    expect(friendlySeries).toMatchObject({
      tipo: 'Amistoso',
      modoDraft: 'Fearless',
      elegivelOficial: false,
      bloqueiosFearless: [1, 2],
      revisaoNecessaria: true,
    })
    expect(remake).toMatchObject({
      estado: 'Remake',
      decisaoPicksRemake: 'PreservarPicks',
    })
    expect(restrictedCapabilities.CanManageMatches).toBe(true)
    expect(restrictedCapabilities.CanFinalizeMatches).toBe(false)
  })

  it('provides deterministic Season scope metadata', () => {
    const season = makeSeason()
    const metadata = makeSeasonScopeMetadata()

    expect(metadata).toEqual({
      calendarioConfigurado: true,
      temporadaAtual: season,
      seasonsIncluidas: [season],
      versaoCalendario: 1,
      calendarioEtag: '"1"',
    })
  })

  it('does not share mutable arrays or nested objects between calls', () => {
    const firstCompetition = makeCompetition()
    const secondCompetition = makeCompetition()
    const firstEvent = makeEvent()
    const secondEvent = makeEvent()
    const firstSeries = makeSeries()
    const secondSeries = makeSeries()
    const firstScope = makeSeasonScopeMetadata()
    const secondScope = makeSeasonScopeMetadata()

    firstCompetition.rodadas[0]!.nome = 'changed-round'
    firstCompetition.regrasPublicadas.push({
      ...firstCompetition.regrasPublicadas[0]!,
      numero: 2,
    })
    firstEvent.times[0]!.nome = 'changed-team'
    firstSeries.lados[0]!.nome = 'changed-side'
    firstSeries.placar[0] = 1
    firstSeries.partidas.push(makeMatch())
    firstSeries.bloqueiosFearless.push(99)
    firstSeries.acoesPermitidas.push('changed-action')
    firstScope.temporadaAtual!.nome = 'changed-season'
    firstScope.seasonsIncluidas.push(
      makeSeason({ id: '00000000-0000-4000-8000-000000000099' }),
    )

    expect(secondCompetition.rodadas[0]!.nome).toBe('round-fixture')
    expect(secondCompetition.regrasPublicadas).toHaveLength(1)
    expect(secondEvent.times[0]!.nome).toBe('team-fixture-1')
    expect(secondSeries.lados[0]!.nome).toBe('side-fixture-1')
    expect(secondSeries.placar).toEqual([0, 0])
    expect(secondSeries.partidas).toEqual([])
    expect(secondSeries.bloqueiosFearless).toEqual([])
    expect(secondSeries.acoesPermitidas).toEqual(['start', 'cancel'])
    expect(secondScope.temporadaAtual!.nome).toBe('season-fixture')
    expect(secondScope.seasonsIncluidas).toHaveLength(1)
  })

  it('retains deterministic mutable defaults for explicit undefined overrides', () => {
    expect(makeSeason({ acoesPermitidas: undefined }).acoesPermitidas).toEqual([
      'edit',
      'close',
    ])
    expect(
      makeSeasonScopeMetadata({
        temporadaAtual: undefined,
        seasonsIncluidas: undefined,
      }),
    ).toEqual(makeSeasonScopeMetadata())
    expect(
      makeCompetition({
        rodadas: undefined,
        regrasPublicadas: undefined,
        acoesPermitidas: undefined,
      }),
    ).toEqual(makeCompetition())
    expect(
      makeEvent({
        times: undefined,
        serieIds: undefined,
        acoesPermitidas: undefined,
      }),
    ).toEqual(makeEvent())
    expect(
      makeSeries({
        lados: undefined,
        placar: undefined,
        partidas: undefined,
        bloqueiosFearless: undefined,
        acoesPermitidas: undefined,
      }),
    ).toEqual(makeSeries())
    expect(makeMatch({ picks: undefined, acoesPermitidas: undefined })).toEqual(
      makeMatch(),
    )
  })

  it('omits explicit undefined scalar and capability overrides', () => {
    expect(makeSeason({ id: undefined, estado: undefined })).toEqual(
      makeSeason(),
    )
    expect(
      makeSeasonScopeMetadata({
        calendarioConfigurado: undefined,
        versaoCalendario: undefined,
      }),
    ).toEqual(makeSeasonScopeMetadata())
    expect(
      makeCompetition({ nome: undefined, circuitoDiario: undefined }),
    ).toEqual(makeCompetition())
    expect(makeEvent({ seasonId: undefined, modoDraft: undefined })).toEqual(
      makeEvent(),
    )
    expect(makeSeries({ tipo: undefined, competicaoId: undefined })).toEqual(
      makeSeries(),
    )
    expect(
      makeMatch({ estado: undefined, conflitoFearless: undefined }),
    ).toEqual(makeMatch())
    expect(
      makeCapabilities({
        CanManageSeasons: undefined,
        CanManageCompetitions: undefined,
        CanManageMatches: undefined,
        CanFinalizeMatches: undefined,
        CanViewCompetitiveAudit: undefined,
      }),
    ).toEqual(makeCapabilities())
  })

  it('isolates caller-provided nested mutable values for all aggregate fixtures', () => {
    const seasonActions = ['edit']
    const scopeSource = makeSeasonScopeMetadata()
    const competitionSource = makeCompetition()
    const eventSource = makeEvent()
    const seriesSource = makeSeries({
      partidas: [
        makeMatch({
          picks: [
            {
              ladoSerieId: makeSeries().lados[0]!.id,
              championId: 1,
              ordem: 1,
            },
          ],
        }),
      ],
    })
    const matchSource = makeMatch({
      picks: [
        {
          ladoSerieId: makeSeries().lados[0]!.id,
          championId: 2,
          ordem: 1,
        },
      ],
    })

    const season = makeSeason({ acoesPermitidas: seasonActions })
    const scope = makeSeasonScopeMetadata({
      temporadaAtual: scopeSource.temporadaAtual,
      seasonsIncluidas: scopeSource.seasonsIncluidas,
    })
    const competition = makeCompetition({
      rodadas: competitionSource.rodadas,
      regrasPublicadas: competitionSource.regrasPublicadas,
      acoesPermitidas: competitionSource.acoesPermitidas,
    })
    const event = makeEvent({
      times: eventSource.times,
      serieIds: eventSource.serieIds,
      acoesPermitidas: eventSource.acoesPermitidas,
    })
    const series = makeSeries({
      lados: seriesSource.lados,
      placar: seriesSource.placar,
      partidas: seriesSource.partidas,
      bloqueiosFearless: seriesSource.bloqueiosFearless,
      acoesPermitidas: seriesSource.acoesPermitidas,
    })
    const match = makeMatch({
      picks: matchSource.picks,
      acoesPermitidas: matchSource.acoesPermitidas,
    })

    season.acoesPermitidas.push('close')
    scope.temporadaAtual!.nome = 'returned-current-season'
    scope.seasonsIncluidas.push(
      makeSeason({ id: '00000000-0000-4000-8000-000000000090' }),
    )
    competition.rodadas[0]!.nome = 'returned-round'
    competition.rodadas.push({
      ...competition.rodadas[0]!,
      id: '00000000-0000-4000-8000-000000000091',
      ordem: 2,
    })
    event.times[0]!.nome = 'returned-event-team'
    series.lados[0]!.nome = 'returned-series-side'
    series.partidas[0]!.picks[0]!.championId = 3
    match.picks[0]!.championId = 4

    expect(seasonActions).toEqual(['edit'])
    expect(scopeSource.temporadaAtual!.nome).toBe('season-fixture')
    expect(scopeSource.seasonsIncluidas).toHaveLength(1)
    expect(competitionSource.rodadas[0]!.nome).toBe('round-fixture')
    expect(competitionSource.rodadas).toHaveLength(1)
    expect(eventSource.times[0]!.nome).toBe('team-fixture-1')
    expect(seriesSource.lados[0]!.nome).toBe('side-fixture-1')
    expect(seriesSource.partidas[0]!.picks[0]!.championId).toBe(1)
    expect(matchSource.picks[0]!.championId).toBe(2)

    seasonActions.push('source-action')
    scopeSource.temporadaAtual!.nome = 'source-current-season'
    scopeSource.seasonsIncluidas[0]!.nome = 'source-included-season'
    competitionSource.regrasPublicadas[0]!.numero = 2
    competitionSource.regrasPublicadas.push({
      ...competitionSource.regrasPublicadas[0]!,
      id: '00000000-0000-4000-8000-000000000092',
      numero: 3,
    })
    eventSource.times[1]!.nome = 'source-event-team'
    seriesSource.lados[1]!.nome = 'source-series-side'
    seriesSource.partidas[0]!.picks[0]!.championId = 5
    matchSource.picks[0]!.championId = 6

    expect(season.acoesPermitidas).toEqual(['edit', 'close'])
    expect(scope.temporadaAtual!.nome).toBe('returned-current-season')
    expect(scope.seasonsIncluidas[0]!.nome).toBe('season-fixture')
    expect(competition.regrasPublicadas[0]!.numero).toBe(1)
    expect(competition.regrasPublicadas).toHaveLength(1)
    expect(event.times[1]!.nome).toBe('team-fixture-2')
    expect(series.lados[1]!.nome).toBe('side-fixture-2')
    expect(series.partidas[0]!.picks[0]!.championId).toBe(3)
    expect(match.picks[0]!.championId).toBe(4)
  })
})
