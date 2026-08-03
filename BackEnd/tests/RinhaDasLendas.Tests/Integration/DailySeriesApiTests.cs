using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Infrastructure.Messages;
using RinhaDasLendas.Infrastructure.Persistence;
using RinhaDasLendas.Tests.Fixtures;
using RinhaDasLendas.Tests.Infrastructure;

namespace RinhaDasLendas.Tests.Integration;

public sealed class DailySeriesApiTests : IAsyncLifetime
{
    private const string SeriesRoute = "/api/v1/series";
    private const string MatchesRoute = "/api/v1/partidas";
    private const string Portuguese = "pt-BR";
    private const string English = "en-US";
    private const string ContractRelativePath =
        "specs/023-fundacao-competitiva-sazonal/contracts/competitive-foundation.openapi.yaml";
    private static readonly ResourceMessageProvider Messages = new();
    private static readonly string[] AllRoles =
    [
        AuthRoles.SuperAdmin,
        AuthRoles.Presidente,
        AuthRoles.VicePresidente,
        AuthRoles.Admin,
        AuthRoles.Moderador,
        AuthRoles.Capitao,
        AuthRoles.Jogador,
    ];
    private static int _nextYear = 2200;
    private CompetitivePostgresFixture _database = null!;
    private DailySeriesApiFactory _factory = null!;

    public static TheoryData<string, string> DailySeriesEndpoints => new()
    {
        { "GET", SeriesRoute },
        { "POST", SeriesRoute },
        { "GET", $"{SeriesRoute}/{Guid.NewGuid()}" },
        { "POST", $"{SeriesRoute}/{Guid.NewGuid()}/inicios" },
        { "POST", $"{SeriesRoute}/{Guid.NewGuid()}/cancelamentos" },
        { "POST", $"{SeriesRoute}/{Guid.NewGuid()}/anulacoes" },
        { "GET", $"{SeriesRoute}/{Guid.NewGuid()}/partidas" },
        { "POST", $"{SeriesRoute}/{Guid.NewGuid()}/partidas" },
        { "GET", $"{SeriesRoute}/{Guid.NewGuid()}/resultado" },
        { "GET", $"{SeriesRoute}/{Guid.NewGuid()}/auditoria" },
        { "GET", $"{MatchesRoute}/{Guid.NewGuid()}" },
        { "POST", $"{MatchesRoute}/{Guid.NewGuid()}/picks" },
        { "POST", $"{MatchesRoute}/{Guid.NewGuid()}/resultados" },
        { "POST", $"{MatchesRoute}/{Guid.NewGuid()}/remakes" },
        { "POST", $"{MatchesRoute}/{Guid.NewGuid()}/anulacoes" },
        { "POST", $"{MatchesRoute}/{Guid.NewGuid()}/correcoes" },
    };

    public static TheoryData<MutationKind> IdempotentMutations => new()
    {
        MutationKind.CreateSeries,
        MutationKind.StartSeries,
        MutationKind.CancelSeries,
        MutationKind.AnnulSeries,
        MutationKind.CreateMatch,
        MutationKind.RegisterPicks,
        MutationKind.ConfirmNormalResult,
        MutationKind.ConfirmSurrender,
        MutationKind.RemakePreservingPicks,
        MutationKind.RemakeDiscardingPicks,
        MutationKind.AnnulMatch,
        MutationKind.CorrectPicks,
        MutationKind.CorrectResult,
    };

    public static TheoryData<MutationKind> MatchMutations => new()
    {
        MutationKind.CreateMatch,
        MutationKind.RegisterPicks,
        MutationKind.ConfirmNormalResult,
        MutationKind.ConfirmSurrender,
        MutationKind.RemakePreservingPicks,
        MutationKind.RemakeDiscardingPicks,
        MutationKind.AnnulMatch,
        MutationKind.CorrectPicks,
        MutationKind.CorrectResult,
    };

    public static TheoryData<MutationKind, string, HttpStatusCode> DailyResourcePolicies
    {
        get
        {
            var data = new TheoryData<MutationKind, string, HttpStatusCode>();
            foreach (var operation in Enum.GetValues<MutationKind>())
            {
                foreach (var role in AllRoles)
                {
                    data.Add(operation, role, ExpectedMutationStatus(operation, role));
                }
            }

            return data;
        }
    }

    public static TheoryData<ReadKind, string, HttpStatusCode> ReadResourcePolicies
    {
        get
        {
            var data = new TheoryData<ReadKind, string, HttpStatusCode>();
            foreach (var read in Enum.GetValues<ReadKind>())
            {
                foreach (var role in AllRoles)
                {
                    var status = read == ReadKind.Audit
                        && role is not AuthRoles.SuperAdmin and not AuthRoles.Presidente and not AuthRoles.Admin
                            ? HttpStatusCode.Forbidden
                            : HttpStatusCode.OK;
                    data.Add(read, role, status);
                }
            }

            return data;
        }
    }

    public static TheoryData<MutationKind> SeriesConditionalMutations => new()
    {
        MutationKind.StartSeries,
        MutationKind.CancelSeries,
        MutationKind.AnnulSeries,
    };

    public static TheoryData<MutationKind> TechnicalMaintenanceMutations => new()
    {
        MutationKind.AnnulSeries,
        MutationKind.AnnulMatch,
        MutationKind.CorrectPicks,
        MutationKind.CorrectResult,
    };

    public static TheoryData<MutationKind> OpenApiBadRequestMutations => new()
    {
        MutationKind.CreateSeries,
        MutationKind.RegisterPicks,
        MutationKind.CorrectPicks,
        MutationKind.CorrectResult,
    };

    public async Task InitializeAsync()
    {
        _database = await CompetitivePostgresFixture.CreateAsync();
        _factory = new DailySeriesApiFactory(_database.ConnectionString);
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _database.DisposeAsync();
    }

    [Fact]
    public void StaticOpenApi_ShouldDeclareConditionalBadRequestsAndEmittedDtoRequiredSets()
    {
        var lines = File.ReadAllLines(FindContractPath());
        var conditionalPaths = new[]
        {
            "/series/{seriesId}/inicios",
            "/series/{seriesId}/cancelamentos",
            "/series/{seriesId}/anulacoes",
            "/series/{seriesId}/partidas",
            "/partidas/{matchId}/picks",
            "/partidas/{matchId}/resultados",
            "/partidas/{matchId}/remakes",
            "/partidas/{matchId}/anulacoes",
            "/partidas/{matchId}/correcoes",
        };

        foreach (var path in conditionalPaths)
        {
            var operation = ReadPostOperationBlock(lines, path);
            operation.Should().Contain("- $ref: '#/components/parameters/IfMatch'", $"POST {path} requires If-Match");
            var badRequest = operation.IndexOf("'400':");
            badRequest.Should().BeGreaterThanOrEqualTo(0, $"POST {path} declares malformed/missing If-Match");
            operation[badRequest + 1].Should().Be("$ref: '#/components/responses/ValidationError'",
                $"POST {path} uses the localized validation envelope");
        }

        lines.Select(line => line.Trim()).Should().Contain(
            "required: [id, seasonId, versaoRegrasId, tipo, formato, modoDraft, estado, agendadaPara, lados, placar, partidas, bloqueiosFearless, elegivelOficial, revisaoNecessaria, versao, acoesPermitidas]");
        lines.Select(line => line.Trim()).Should().Contain(
            "required: [id, serieId, ordem, estado, picks, conflitoFearless, acoesPermitidas]");
    }

    [Theory]
    [MemberData(nameof(DailySeriesEndpoints))]
    public async Task EveryDailySeriesEndpoint_ShouldRequireBearerAuthentication(string method, string route)
    {
        using var client = _factory.CreateAnonymousClient();
        using var response = await SendAsync(
            client,
            new HttpMethod(method),
            route,
            new { justificativa = "Operação protegida" },
            NewKey(),
            "W/\"0\"");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            $"{method} {route} is covered by the OpenAPI bearer requirement");
        response.Headers.WwwAuthenticate.Should().ContainSingle(header => header.Scheme == "Bearer");
    }

    [Fact]
    public async Task AuthenticatedMember_ShouldReadSeriesMatchesResultAndCompleteDtos()
    {
        var scenario = await _factory.SeedDailySeriesAsync(SeededSeriesState.ConfirmedOneZero, seedAudit: true);
        using var client = _factory.CreateClientFor(AuthRoles.Jogador);

        using var list = await SendAsync(client, HttpMethod.Get, $"{SeriesRoute}?page=1&pageSize=20");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        var seriesPage = await ReadJsonAsync(list);
        AssertPage(seriesPage, 1, 20);
        seriesPage.GetProperty("calendarioConfigurado").GetBoolean().Should().BeTrue();
        AssertSeasonSummary(seriesPage.GetProperty("temporadaAtual"));
        seriesPage.GetProperty("seasonsIncluidas").EnumerateArray().Should().ContainSingle();
        AssertSeasonSummary(seriesPage.GetProperty("seasonsIncluidas")[0]);
        seriesPage.GetProperty("items").EnumerateArray().Should().ContainSingle();
        AssertSeriesDetail(seriesPage.GetProperty("items")[0], scenario.SeriesId);

        using var detail = await SendAsync(client, HttpMethod.Get, $"{SeriesRoute}/{scenario.SeriesId}");
        detail.StatusCode.Should().Be(HttpStatusCode.OK);
        RequireETag(detail).Should().Be(scenario.ETag);
        var detailBody = await ReadJsonAsync(detail);
        AssertSeriesDetail(detailBody, scenario.SeriesId);
        AssertFearless(detailBody.GetProperty("bloqueiosFearless"), Enumerable.Range(1, 10));

        using var matches = await SendAsync(
            client, HttpMethod.Get, $"{SeriesRoute}/{scenario.SeriesId}/partidas?page=1&pageSize=20");
        matches.StatusCode.Should().Be(HttpStatusCode.OK);
        var matchPage = await ReadJsonAsync(matches);
        AssertPage(matchPage, 1, 20);
        matchPage.GetProperty("items").EnumerateArray().Should().ContainSingle();
        AssertMatchDetail(matchPage.GetProperty("items")[0], scenario.MatchId!.Value, scenario.SeriesId);

        using var match = await SendAsync(client, HttpMethod.Get, $"{MatchesRoute}/{scenario.MatchId}");
        match.StatusCode.Should().Be(HttpStatusCode.OK);
        RequireETag(match).Should().Be(scenario.ETag, "Partida exposes its containing Series version");
        AssertMatchDetail(await ReadJsonAsync(match), scenario.MatchId!.Value, scenario.SeriesId);

        using var result = await SendAsync(client, HttpMethod.Get, $"{SeriesRoute}/{scenario.SeriesId}/resultado");
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        AssertSeriesResult(await ReadJsonAsync(result), scenario.SeriesId);

        using var auditClient = _factory.CreateClientFor(AuthRoles.Presidente);
        using var audit = await SendAsync(
            auditClient, HttpMethod.Get, $"{SeriesRoute}/{scenario.SeriesId}/auditoria?page=1&pageSize=20");
        audit.StatusCode.Should().Be(HttpStatusCode.OK);
        var auditPage = await ReadJsonAsync(audit);
        AssertPage(auditPage, 1, 20);
        auditPage.GetProperty("items").EnumerateArray().Should().ContainSingle();
        AssertAuditRecord(auditPage.GetProperty("items")[0], scenario.SeriesId);
    }

    [Theory]
    [MemberData(nameof(ReadResourcePolicies))]
    public async Task ReadEndpoint_ShouldApplyAuthenticatedRoleAndAuditResourcePolicy(
        ReadKind read,
        string role,
        HttpStatusCode expectedStatus)
    {
        var scenario = await _factory.SeedDailySeriesAsync(SeededSeriesState.ConfirmedOneZero, seedAudit: true);
        using var client = _factory.CreateClientFor(role);
        var route = read switch
        {
            ReadKind.SeriesList => SeriesRoute,
            ReadKind.SeriesDetail => $"{SeriesRoute}/{scenario.SeriesId}",
            ReadKind.MatchList => $"{SeriesRoute}/{scenario.SeriesId}/partidas",
            ReadKind.SeriesResult => $"{SeriesRoute}/{scenario.SeriesId}/resultado",
            ReadKind.MatchDetail => $"{MatchesRoute}/{scenario.MatchId}",
            ReadKind.Audit => $"{SeriesRoute}/{scenario.SeriesId}/auditoria",
            _ => throw new ArgumentOutOfRangeException(nameof(read), read, null),
        };

        using var response = await SendAsync(client, HttpMethod.Get, route, culture: English);
        if (expectedStatus == HttpStatusCode.Forbidden)
        {
            await AssertLocalizedErrorAsync(response, expectedStatus, English);
            return;
        }

        response.StatusCode.Should().Be(expectedStatus);
    }

    [Fact]
    public async Task DailySeriesLifecycle_ShouldCreateStartAddMatchesConfirmNormalAndSurrenderThenConclude()
    {
        using var client = _factory.CreateClientFor(AuthRoles.Presidente);
        var prerequisites = await _factory.SeedDailyPrerequisitesAsync();

        using var created = await SendAsync(
            client, HttpMethod.Post, SeriesRoute, prerequisites.CreateRequest, NewKey());
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var series = await ReadJsonAsync(created);
        var seriesId = series.GetProperty("id").GetGuid();
        var sideIds = series.GetProperty("lados").EnumerateArray()
            .Select(side => side.GetProperty("id").GetGuid())
            .ToArray();
        AssertSeriesDetail(series, seriesId, "Agendada");
        var etag = RequireETag(created);

        using var started = await SendAsync(
            client, HttpMethod.Post, $"{SeriesRoute}/{seriesId}/inicios", idempotencyKey: NewKey(), etag: etag);
        started.StatusCode.Should().Be(HttpStatusCode.OK);
        AssertSeriesDetail(await ReadJsonAsync(started), seriesId, "EmAndamento");
        etag = RequireChangedETag(started, etag);

        using var firstCreated = await SendAsync(
            client, HttpMethod.Post, $"{SeriesRoute}/{seriesId}/partidas", idempotencyKey: NewKey(), etag: etag);
        firstCreated.StatusCode.Should().Be(HttpStatusCode.Created);
        var firstMatch = await ReadJsonAsync(firstCreated);
        var firstMatchId = firstMatch.GetProperty("id").GetGuid();
        AssertMatchDetail(firstMatch, firstMatchId, seriesId, "Rascunho");
        etag = RequireChangedETag(firstCreated, etag);

        using var firstPicks = await SendAsync(
            client, HttpMethod.Post, $"{MatchesRoute}/{firstMatchId}/picks",
            PicksBody(sideIds, 1), NewKey(), etag);
        firstPicks.StatusCode.Should().Be(HttpStatusCode.OK);
        AssertMutationResult(await ReadJsonAsync(firstPicks), firstMatchId, seriesId);
        etag = RequireChangedETag(firstPicks, etag);

        using var normal = await SendAsync(
            client, HttpMethod.Post, $"{MatchesRoute}/{firstMatchId}/resultados",
            ResultBody(sideIds[0], "Normal"), NewKey(), etag);
        normal.StatusCode.Should().Be(HttpStatusCode.OK);
        var normalBody = await ReadJsonAsync(normal);
        AssertMutationResult(normalBody, firstMatchId, seriesId, "Confirmada", "Normal");
        AssertFearless(normalBody.GetProperty("bloqueiosFearless"), Enumerable.Range(1, 10));
        etag = RequireChangedETag(normal, etag);

        using var secondCreated = await SendAsync(
            client, HttpMethod.Post, $"{SeriesRoute}/{seriesId}/partidas", idempotencyKey: NewKey(), etag: etag);
        secondCreated.StatusCode.Should().Be(HttpStatusCode.Created);
        var secondMatchId = (await ReadJsonAsync(secondCreated)).GetProperty("id").GetGuid();
        etag = RequireChangedETag(secondCreated, etag);

        using var secondPicks = await SendAsync(
            client, HttpMethod.Post, $"{MatchesRoute}/{secondMatchId}/picks",
            PicksBody(sideIds, 11), NewKey(), etag);
        secondPicks.StatusCode.Should().Be(HttpStatusCode.OK);
        etag = RequireChangedETag(secondPicks, etag);

        using var surrender = await SendAsync(
            client, HttpMethod.Post, $"{MatchesRoute}/{secondMatchId}/resultados",
            ResultBody(sideIds[0], "Surrender"), NewKey(), etag);
        surrender.StatusCode.Should().Be(HttpStatusCode.OK);
        var completed = await ReadJsonAsync(surrender);
        AssertMutationResult(completed, secondMatchId, seriesId, "Confirmada", "Surrender");
        completed.GetProperty("resultadoSerie").GetProperty("concluida").GetBoolean().Should().BeTrue();
        completed.GetProperty("resultadoSerie").GetProperty("placar").EnumerateArray()
            .Select(score => score.GetInt32()).Should().Equal(2, 0);
        completed.GetProperty("resultadoSerie").GetProperty("ladoVencedorId").GetGuid().Should().Be(sideIds[0]);
        AssertFearless(completed.GetProperty("bloqueiosFearless"), Enumerable.Range(1, 20));
        RequireChangedETag(surrender, etag);
    }

    [Fact]
    public async Task CancelRemakeAnnulAndCorrectionEndpoints_ShouldReturnContractDtosAndStatuses()
    {
        using var client = _factory.CreateClientFor(AuthRoles.Presidente);

        var cancellation = await _factory.SeedDailySeriesAsync(SeededSeriesState.EmAndamento);
        using var cancelled = await SendAsync(
            client, HttpMethod.Post, $"{SeriesRoute}/{cancellation.SeriesId}/cancelamentos",
            ReasonBody("Cancelamento operacional"), NewKey(), cancellation.ETag);
        cancelled.StatusCode.Should().Be(HttpStatusCode.OK);
        AssertSeriesDetail(await ReadJsonAsync(cancelled), cancellation.SeriesId, "Cancelada");
        RequireChangedETag(cancelled, cancellation.ETag);

        var remake = await _factory.SeedDailySeriesAsync(SeededSeriesState.DraftMatchWithPicks);
        using var remade = await SendAsync(
            client, HttpMethod.Post, $"{MatchesRoute}/{remake.MatchId}/remakes",
            new { decisaoPicks = "PreservarPicks", justificativa = "Falha antes do início" },
            NewKey(), remake.ETag);
        remade.StatusCode.Should().Be(HttpStatusCode.OK);
        var remakeBody = await ReadJsonAsync(remade);
        AssertMutationResult(remakeBody, remake.MatchId!.Value, remake.SeriesId, "Remake");
        AssertFearless(remakeBody.GetProperty("bloqueiosFearless"), Enumerable.Range(1, 10));
        RequireChangedETag(remade, remake.ETag);

        var matchAnnulment = await _factory.SeedDailySeriesAsync(SeededSeriesState.ConfirmedOneZero);
        using var annulledMatch = await SendAsync(
            client, HttpMethod.Post, $"{MatchesRoute}/{matchAnnulment.MatchId}/anulacoes",
            new { justificativa = "Fato inválido", anularSerieSeInconclusiva = false },
            NewKey(), matchAnnulment.ETag);
        annulledMatch.StatusCode.Should().Be(HttpStatusCode.OK);
        AssertMutationResult(
            await ReadJsonAsync(annulledMatch), matchAnnulment.MatchId!.Value, matchAnnulment.SeriesId, "Anulada");
        RequireChangedETag(annulledMatch, matchAnnulment.ETag);

        var correction = await _factory.SeedDailySeriesAsync(SeededSeriesState.ConfirmedOneZero);
        using var corrected = await SendAsync(
            client, HttpMethod.Post, $"{MatchesRoute}/{correction.MatchId}/correcoes",
            CorrectionBody(correction.SideIds, "Correção auditada", 21), NewKey(), correction.ETag);
        corrected.StatusCode.Should().Be(HttpStatusCode.OK);
        var correctedBody = await ReadJsonAsync(corrected);
        AssertMutationResult(correctedBody, correction.MatchId!.Value, correction.SeriesId);
        AssertFearless(correctedBody.GetProperty("bloqueiosFearless"), Enumerable.Range(21, 10));
        RequireChangedETag(corrected, correction.ETag);

        var seriesAnnulment = await _factory.SeedDailySeriesAsync(SeededSeriesState.EmAndamento);
        using var annulledSeries = await SendAsync(
            client, HttpMethod.Post, $"{SeriesRoute}/{seriesAnnulment.SeriesId}/anulacoes",
            ReasonBody("Correção exige anulação"), NewKey(), seriesAnnulment.ETag);
        annulledSeries.StatusCode.Should().Be(HttpStatusCode.OK);
        AssertSeriesDetail(await ReadJsonAsync(annulledSeries), seriesAnnulment.SeriesId, "Anulada");
        RequireChangedETag(annulledSeries, seriesAnnulment.ETag);
    }

    [Theory]
    [MemberData(nameof(DailyResourcePolicies))]
    public async Task Mutation_ShouldApplyCapabilityAndDailyResourcePolicy(
        MutationKind kind,
        string role,
        HttpStatusCode expectedStatus)
    {
        using var client = _factory.CreateClientFor(role);
        var mutation = await PrepareMutationAsync(kind);

        using var response = await SendAsync(
            client, mutation.Method, mutation.Route, mutation.Body, NewKey(), mutation.ETag, English);

        if (expectedStatus == HttpStatusCode.Forbidden)
        {
            await AssertLocalizedErrorAsync(response, expectedStatus, English);
            return;
        }

        response.StatusCode.Should().Be(expectedStatus,
            $"{role} has the OpenAPI resource condition for {kind} on DiariaTemporaria");
    }

    [Theory]
    [MemberData(nameof(TechnicalMaintenanceMutations))]
    public async Task SuperAdminTechnicalMaintenance_ShouldRequireJustification(MutationKind kind)
    {
        using var client = _factory.CreateClientFor(AuthRoles.SuperAdmin);
        var mutation = await PrepareMutationAsync(kind);
        var original = JsonSerializer.SerializeToElement(mutation.Body);
        object body = kind switch
        {
            MutationKind.AnnulSeries => ReasonBody(" "),
            MutationKind.AnnulMatch => new { justificativa = " ", anularSerieSeInconclusiva = false },
            MutationKind.CorrectPicks => new
            {
                lados = original.GetProperty("lados"),
                justificativa = " ",
                anularSerieSeInconclusiva = false,
            },
            MutationKind.CorrectResult => new
            {
                ladoVencedorId = original.GetProperty("ladoVencedorId").GetGuid(),
                motivoTermino = original.GetProperty("motivoTermino").GetString(),
                justificativa = " ",
                anularSerieSeInconclusiva = false,
            },
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };

        using var response = await SendAsync(
            client, mutation.Method, mutation.Route, body, NewKey(), mutation.ETag, Portuguese);
        await AssertLocalizedErrorAsync(response, HttpStatusCode.Forbidden, Portuguese);
    }

    [Theory]
    [MemberData(nameof(IdempotentMutations))]
    public async Task EveryMutation_ShouldReplaySameContentAndRejectDivergentContent(MutationKind kind)
    {
        using var client = _factory.CreateClientFor(AuthRoles.Presidente);
        var mutation = await PrepareMutationAsync(kind);
        var key = NewKey();

        using var first = await SendAsync(
            client, mutation.Method, mutation.Route, mutation.Body, key, mutation.ETag, Portuguese);
        first.StatusCode.Should().Be(mutation.SuccessStatus);
        var firstBytes = await first.Content.ReadAsByteArrayAsync();
        var firstEtag = RequireETag(first);
        first.Headers.Contains("Idempotency-Replayed").Should().BeFalse();

        using var replay = await SendAsync(
            client, mutation.Method, mutation.Route, mutation.Body, key, mutation.ETag, English);
        replay.StatusCode.Should().Be(first.StatusCode);
        (await replay.Content.ReadAsByteArrayAsync()).Should().Equal(firstBytes);
        RequireETag(replay).Should().Be(firstEtag);
        replay.Headers.GetValues("Idempotency-Replayed").Should().ContainSingle().Which.Should().Be("true");

        if (mutation.DivergentBody is null)
        {
            return;
        }

        using var divergent = await SendAsync(
            client, mutation.Method, mutation.Route, mutation.DivergentBody, key, mutation.ETag, English);
        await AssertLocalizedErrorAsync(divergent, HttpStatusCode.Conflict, English);
    }

    [Theory]
    [MemberData(nameof(MatchMutations))]
    public async Task EveryMatchMutation_ShouldUseSeriesETagAndRejectStaleVersion(MutationKind kind)
    {
        using var client = _factory.CreateClientFor(AuthRoles.Presidente);
        var mutation = await PrepareMutationAsync(kind);

        using var accepted = await SendAsync(
            client, mutation.Method, mutation.Route, mutation.Body, NewKey(), mutation.ETag);
        accepted.StatusCode.Should().Be(mutation.SuccessStatus);
        RequireChangedETag(accepted, mutation.ETag!);

        using var stale = await SendAsync(
            client, mutation.Method, mutation.Route, mutation.DivergentBody ?? mutation.Body,
            NewKey(), mutation.ETag, Portuguese);
        await AssertLocalizedErrorAsync(stale, HttpStatusCode.Conflict, Portuguese);
    }

    [Theory]
    [MemberData(nameof(MatchMutations))]
    public async Task EveryMatchMutation_ShouldRejectMissingAndMalformedSeriesETag(MutationKind kind)
    {
        using var client = _factory.CreateClientFor(AuthRoles.Presidente);
        var mutation = await PrepareMutationAsync(kind);

        using var missing = await SendAsync(
            client, mutation.Method, mutation.Route, mutation.Body, NewKey(), culture: Portuguese);
        await AssertLocalizedErrorAsync(missing, HttpStatusCode.BadRequest, Portuguese, requireFieldErrors: true);

        using var malformed = await SendAsync(
            client, mutation.Method, mutation.Route, mutation.Body, NewKey(), "invalid-etag", English);
        await AssertLocalizedErrorAsync(malformed, HttpStatusCode.BadRequest, English, requireFieldErrors: true);
    }

    [Theory]
    [MemberData(nameof(SeriesConditionalMutations))]
    public async Task SeriesTransition_ShouldRejectMissingMalformedAndStaleSeriesETag(MutationKind kind)
    {
        using var client = _factory.CreateClientFor(AuthRoles.Presidente);
        var mutation = await PrepareMutationAsync(kind);

        using var missing = await SendAsync(
            client, mutation.Method, mutation.Route, mutation.Body, NewKey(), culture: Portuguese);
        await AssertLocalizedErrorAsync(missing, HttpStatusCode.BadRequest, Portuguese, requireFieldErrors: true);

        using var malformed = await SendAsync(
            client, mutation.Method, mutation.Route, mutation.Body, NewKey(), "invalid-etag", English);
        await AssertLocalizedErrorAsync(malformed, HttpStatusCode.BadRequest, English, requireFieldErrors: true);

        using var stale = await SendAsync(
            client, mutation.Method, mutation.Route, mutation.Body, NewKey(), "W/\"999999\"", Portuguese);
        await AssertLocalizedErrorAsync(stale, HttpStatusCode.Conflict, Portuguese);
    }

    [Theory]
    [MemberData(nameof(OpenApiBadRequestMutations))]
    public async Task OpenApiMutationDeclaringBadRequest_ShouldReturnLocalizedEnvelopeAndFieldErrors(MutationKind kind)
    {
        using var client = _factory.CreateClientFor(AuthRoles.Presidente);
        var mutation = await PrepareMutationAsync(kind);

        using var portuguese = await SendAsync(
            client, mutation.Method, mutation.Route, mutation.Body, etag: mutation.ETag, culture: Portuguese);
        var ptError = await AssertLocalizedErrorAsync(
            portuguese, HttpStatusCode.BadRequest, Portuguese, requireFieldErrors: true);

        using var english = await SendAsync(
            client, mutation.Method, mutation.Route, mutation.Body, etag: mutation.ETag, culture: English);
        var enError = await AssertLocalizedErrorAsync(
            english, HttpStatusCode.BadRequest, English, requireFieldErrors: true);

        enError.GetProperty("messageCode").GetString().Should().Be(ptError.GetProperty("messageCode").GetString());
        enError.GetProperty("message").GetString().Should().NotBe(ptError.GetProperty("message").GetString());
    }

    [Fact]
    public async Task CorrectConcludedTwoZeroToOneOne_ShouldRequireExplicitSeriesAnnulment()
    {
        using var client = _factory.CreateClientFor(AuthRoles.Presidente);
        var scenario = await _factory.SeedDailySeriesAsync(SeededSeriesState.ConcludedTwoZero);
        var correctionRoute = $"{MatchesRoute}/{scenario.SecondMatchId}/correcoes";
        using var beforeCorrection = await SendAsync(client, HttpMethod.Get, $"{SeriesRoute}/{scenario.SeriesId}");
        beforeCorrection.StatusCode.Should().Be(HttpStatusCode.OK);
        var concluded = await ReadJsonAsync(beforeCorrection);
        AssertSeriesDetail(concluded, scenario.SeriesId, "Concluida");
        concluded.GetProperty("placar").EnumerateArray()
            .Select(score => score.GetInt32()).Should().Equal(2, 0);
        concluded.GetProperty("ladoVencedorId").GetGuid().Should().Be(scenario.SideIds[0]);

        var correction = new
        {
            ladoVencedorId = scenario.SideIds[1],
            motivoTermino = "Normal",
            justificativa = "Correção técnica do resultado decisivo",
            anularSerieSeInconclusiva = false,
        };

        using var rejected = await SendAsync(
            client, HttpMethod.Post, correctionRoute, correction, NewKey(), scenario.ETag, Portuguese);
        var error = await AssertLocalizedErrorAsync(rejected, HttpStatusCode.Conflict, Portuguese);
        error.GetProperty("messageCode").GetString().Should().Be(MessageCodes.CorrectionAnnulConfirmationRequired);

        using var accepted = await SendAsync(
            client,
            HttpMethod.Post,
            correctionRoute,
            new
            {
                correction.ladoVencedorId,
                correction.motivoTermino,
                correction.justificativa,
                anularSerieSeInconclusiva = true,
            },
            NewKey(),
            scenario.ETag,
            Portuguese);
        accepted.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync(accepted);
        AssertMutationResult(body, scenario.SecondMatchId!.Value, scenario.SeriesId, "Confirmada", "Normal");
        body.GetProperty("resultadoSerie").GetProperty("placar").EnumerateArray()
            .Select(score => score.GetInt32()).Should().Equal(1, 1);
        body.GetProperty("resultadoSerie").GetProperty("concluida").GetBoolean().Should().BeFalse();
        var correctedEtag = RequireChangedETag(accepted, scenario.ETag);

        using var reloaded = await SendAsync(client, HttpMethod.Get, $"{SeriesRoute}/{scenario.SeriesId}");
        reloaded.StatusCode.Should().Be(HttpStatusCode.OK);
        RequireETag(reloaded).Should().Be(correctedEtag);
        var reloadedSeries = await ReadJsonAsync(reloaded);
        AssertSeriesDetail(reloadedSeries, scenario.SeriesId, "Anulada");
        reloadedSeries.GetProperty("placar").EnumerateArray()
            .Select(score => score.GetInt32()).Should().Equal(1, 1);
    }

    [Fact]
    public async Task ConcurrentMutationsOnDifferentMatches_ShouldShareSeriesVersionAndAllowOnlyOneCommit()
    {
        using var client = _factory.CreateClientFor(AuthRoles.Presidente);
        var scenario = await _factory.SeedDailySeriesAsync(SeededSeriesState.TwoDraftMatchesWithPicks);
        var firstRequest = Request(
            HttpMethod.Post,
            $"{MatchesRoute}/{scenario.MatchId}/resultados",
            ResultBody(scenario.SideIds[0], "Normal"),
            NewKey(),
            scenario.ETag,
            Portuguese);
        var secondRequest = Request(
            HttpMethod.Post,
            $"{MatchesRoute}/{scenario.SecondMatchId}/resultados",
            ResultBody(scenario.SideIds[1], "Surrender"),
            NewKey(),
            scenario.ETag,
            English);

        var responses = await Task.WhenAll(client.SendAsync(firstRequest), client.SendAsync(secondRequest));
        try
        {
            responses.Select(response => response.StatusCode).Should()
                .BeEquivalentTo([HttpStatusCode.OK, HttpStatusCode.Conflict]);
            RequireETag(responses.Single(response => response.StatusCode == HttpStatusCode.OK))
                .Should().NotBe(scenario.ETag);
            await AssertLocalizedErrorAsync(
                responses.Single(response => response.StatusCode == HttpStatusCode.Conflict),
                HttpStatusCode.Conflict,
                responses[0].StatusCode == HttpStatusCode.Conflict ? Portuguese : English);
        }
        finally
        {
            firstRequest.Dispose();
            secondRequest.Dispose();
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }
    }

    [Fact]
    public async Task ValidationForbiddenNotFoundAndConflict_ShouldUseLocalizedContractEnvelopes()
    {
        using var president = _factory.CreateClientFor(AuthRoles.Presidente);
        using var player = _factory.CreateClientFor(AuthRoles.Jogador);
        var prerequisites = await _factory.SeedDailyPrerequisitesAsync();

        using var invalidList = await SendAsync(
            president, HttpMethod.Get, $"{SeriesRoute}?page=0&pageSize=101", culture: Portuguese);
        await AssertLocalizedErrorAsync(
            invalidList, HttpStatusCode.BadRequest, Portuguese, requireFieldErrors: true);

        using var invalidPt = await SendAsync(
            president, HttpMethod.Post, SeriesRoute,
            new { tipo = "DiariaTemporaria", ladoOrigemIds = Array.Empty<Guid>() }, NewKey(), culture: Portuguese);
        var pt = await AssertLocalizedErrorAsync(
            invalidPt, HttpStatusCode.BadRequest, Portuguese, requireFieldErrors: true);

        using var invalidEn = await SendAsync(
            president, HttpMethod.Post, SeriesRoute,
            new { tipo = "DiariaTemporaria", ladoOrigemIds = Array.Empty<Guid>() }, NewKey(), culture: English);
        var en = await AssertLocalizedErrorAsync(
            invalidEn, HttpStatusCode.BadRequest, English, requireFieldErrors: true);
        en.GetProperty("messageCode").GetString().Should().Be(pt.GetProperty("messageCode").GetString());
        en.GetProperty("message").GetString().Should().NotBe(pt.GetProperty("message").GetString());

        using var forbidden = await SendAsync(
            player, HttpMethod.Post, SeriesRoute, prerequisites.CreateRequest, NewKey(), culture: Portuguese);
        await AssertLocalizedErrorAsync(forbidden, HttpStatusCode.Forbidden, Portuguese);

        using var missingSeries = await SendAsync(
            president, HttpMethod.Get, $"{SeriesRoute}/{Guid.NewGuid()}", culture: Portuguese);
        await AssertLocalizedErrorAsync(missingSeries, HttpStatusCode.NotFound, Portuguese);

        using var missingMatch = await SendAsync(
            president, HttpMethod.Get, $"{MatchesRoute}/{Guid.NewGuid()}", culture: English);
        await AssertLocalizedErrorAsync(missingMatch, HttpStatusCode.NotFound, English);

        var scenario = await _factory.SeedDailySeriesAsync(SeededSeriesState.DraftMatchWithPicks);
        using var stale = await SendAsync(
            president, HttpMethod.Post, $"{MatchesRoute}/{scenario.MatchId}/picks",
            PicksBody(scenario.SideIds, 1), NewKey(), "W/\"999999\"", Portuguese);
        await AssertLocalizedErrorAsync(stale, HttpStatusCode.Conflict, Portuguese);
    }

    [Fact]
    public async Task AnonymousRequest_ShouldReturnLocalizedUnauthorizedEnvelope()
    {
        using var client = _factory.CreateAnonymousClient();
        using var portuguese = await SendAsync(client, HttpMethod.Get, SeriesRoute, culture: Portuguese);
        using var english = await SendAsync(client, HttpMethod.Get, SeriesRoute, culture: English);

        var ptError = await AssertLocalizedErrorAsync(portuguese, HttpStatusCode.Unauthorized, Portuguese);
        var enError = await AssertLocalizedErrorAsync(english, HttpStatusCode.Unauthorized, English);
        ptError.GetProperty("messageCode").GetString().Should().Be(enError.GetProperty("messageCode").GetString());
        ptError.GetProperty("message").GetString().Should().NotBe(enError.GetProperty("message").GetString());
        portuguese.Headers.WwwAuthenticate.Should().ContainSingle(header => header.Scheme == "Bearer");
        english.Headers.WwwAuthenticate.Should().ContainSingle(header => header.Scheme == "Bearer");
    }

    private async Task<MutationScenario> PrepareMutationAsync(MutationKind kind)
    {
        if (kind == MutationKind.CreateSeries)
        {
            var prerequisites = await _factory.SeedDailyPrerequisitesAsync();
            var divergent = await _factory.SeedDailyPrerequisitesAsync();
            return new(
                HttpMethod.Post,
                SeriesRoute,
                prerequisites.CreateRequest,
                divergent.CreateRequest,
                null,
                HttpStatusCode.Created);
        }

        var state = kind switch
        {
            MutationKind.StartSeries => SeededSeriesState.Agendada,
            MutationKind.CancelSeries or MutationKind.AnnulSeries or MutationKind.CreateMatch =>
                SeededSeriesState.EmAndamento,
            MutationKind.RegisterPicks => SeededSeriesState.DraftMatchEmpty,
            MutationKind.ConfirmNormalResult or MutationKind.ConfirmSurrender
                or MutationKind.RemakePreservingPicks or MutationKind.RemakeDiscardingPicks =>
                SeededSeriesState.DraftMatchWithPicks,
            MutationKind.AnnulMatch or MutationKind.CorrectPicks or MutationKind.CorrectResult =>
                SeededSeriesState.ConfirmedOneZero,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
        var scenario = await _factory.SeedDailySeriesAsync(state);
        return kind switch
        {
            MutationKind.StartSeries => Mutation(
                HttpMethod.Post, $"{SeriesRoute}/{scenario.SeriesId}/inicios", null, null, scenario, HttpStatusCode.OK),
            MutationKind.CancelSeries => Mutation(
                HttpMethod.Post, $"{SeriesRoute}/{scenario.SeriesId}/cancelamentos",
                ReasonBody("Cancelamento original"), ReasonBody("Cancelamento divergente"), scenario, HttpStatusCode.OK),
            MutationKind.AnnulSeries => Mutation(
                HttpMethod.Post, $"{SeriesRoute}/{scenario.SeriesId}/anulacoes",
                ReasonBody("Anulação original"), ReasonBody("Anulação divergente"), scenario, HttpStatusCode.OK),
            MutationKind.CreateMatch => Mutation(
                HttpMethod.Post, $"{SeriesRoute}/{scenario.SeriesId}/partidas", null, null, scenario, HttpStatusCode.Created),
            MutationKind.RegisterPicks => Mutation(
                HttpMethod.Post, $"{MatchesRoute}/{scenario.MatchId}/picks",
                PicksBody(scenario.SideIds, 1), PicksBody(scenario.SideIds, 21), scenario, HttpStatusCode.OK),
            MutationKind.ConfirmNormalResult => Mutation(
                HttpMethod.Post, $"{MatchesRoute}/{scenario.MatchId}/resultados",
                ResultBody(scenario.SideIds[0], "Normal"), ResultBody(scenario.SideIds[1], "Normal"), scenario, HttpStatusCode.OK),
            MutationKind.ConfirmSurrender => Mutation(
                HttpMethod.Post, $"{MatchesRoute}/{scenario.MatchId}/resultados",
                ResultBody(scenario.SideIds[0], "Surrender"), ResultBody(scenario.SideIds[1], "Surrender"), scenario, HttpStatusCode.OK),
            MutationKind.RemakePreservingPicks => Mutation(
                HttpMethod.Post, $"{MatchesRoute}/{scenario.MatchId}/remakes",
                new { decisaoPicks = "PreservarPicks", justificativa = "Remake original" },
                new { decisaoPicks = "DesconsiderarPicks", justificativa = "Remake divergente" }, scenario, HttpStatusCode.OK),
            MutationKind.RemakeDiscardingPicks => Mutation(
                HttpMethod.Post, $"{MatchesRoute}/{scenario.MatchId}/remakes",
                new { decisaoPicks = "DesconsiderarPicks", justificativa = "Remake original" },
                new { decisaoPicks = "PreservarPicks", justificativa = "Remake divergente" }, scenario, HttpStatusCode.OK),
            MutationKind.AnnulMatch => Mutation(
                HttpMethod.Post, $"{MatchesRoute}/{scenario.MatchId}/anulacoes",
                new { justificativa = "Anulação original", anularSerieSeInconclusiva = false },
                new { justificativa = "Anulação divergente", anularSerieSeInconclusiva = false }, scenario, HttpStatusCode.OK),
            MutationKind.CorrectPicks => Mutation(
                HttpMethod.Post, $"{MatchesRoute}/{scenario.MatchId}/correcoes",
                CorrectionBody(scenario.SideIds, "Correção original", 21),
                CorrectionBody(scenario.SideIds, "Correção divergente", 31), scenario, HttpStatusCode.OK),
            MutationKind.CorrectResult => Mutation(
                HttpMethod.Post, $"{MatchesRoute}/{scenario.MatchId}/correcoes",
                new
                {
                    ladoVencedorId = scenario.SideIds[1],
                    motivoTermino = "Normal",
                    justificativa = "Resultado original",
                    anularSerieSeInconclusiva = false,
                },
                new
                {
                    ladoVencedorId = scenario.SideIds[0],
                    motivoTermino = "Surrender",
                    justificativa = "Resultado divergente",
                    anularSerieSeInconclusiva = false,
                }, scenario, HttpStatusCode.OK),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
    }

    private static MutationScenario Mutation(
        HttpMethod method,
        string route,
        object? body,
        object? divergentBody,
        DailyScenario scenario,
        HttpStatusCode successStatus) =>
        new(method, route, body, divergentBody, scenario.ETag, successStatus);

    private static HttpStatusCode ExpectedMutationStatus(MutationKind operation, string role)
    {
        var success = operation is MutationKind.CreateSeries or MutationKind.CreateMatch
            ? HttpStatusCode.Created
            : HttpStatusCode.OK;
        var technicalMaintenance = operation is MutationKind.AnnulSeries
            or MutationKind.AnnulMatch
            or MutationKind.CorrectPicks
            or MutationKind.CorrectResult;
        if (role is AuthRoles.Presidente or AuthRoles.Admin
            || role == AuthRoles.Moderador && !technicalMaintenance
            || role == AuthRoles.SuperAdmin && technicalMaintenance)
        {
            return success;
        }

        return HttpStatusCode.Forbidden;
    }

    private static object PicksBody(IReadOnlyList<Guid> sideIds, int firstChampion) => new
    {
        lados = new[]
        {
            new { ladoSerieId = sideIds[0], championIds = Enumerable.Range(firstChampion, 5).ToArray() },
            new { ladoSerieId = sideIds[1], championIds = Enumerable.Range(firstChampion + 5, 5).ToArray() },
        },
    };

    private static object ResultBody(Guid winnerSideId, string reason) => new
    {
        ladoVencedorId = winnerSideId,
        motivoTermino = reason,
    };

    private static object ReasonBody(string reason) => new { justificativa = reason };

    private static object CorrectionBody(IReadOnlyList<Guid> sideIds, string reason, int firstChampion = 1) => new
    {
        lados = new[]
        {
            new { ladoSerieId = sideIds[0], championIds = Enumerable.Range(firstChampion, 5).ToArray() },
            new { ladoSerieId = sideIds[1], championIds = Enumerable.Range(firstChampion + 5, 5).ToArray() },
        },
        justificativa = reason,
        anularSerieSeInconclusiva = false,
    };

    private static HttpRequestMessage Request(
        HttpMethod method,
        string route,
        object? body,
        string? idempotencyKey,
        string? etag,
        string culture)
    {
        var request = new HttpRequestMessage(method, route);
        request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(culture));
        if (idempotencyKey is not null)
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        if (etag is not null)
        {
            request.Headers.TryAddWithoutValidation("If-Match", etag);
        }

        if (body is not null && method != HttpMethod.Get)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        }

        return request;
    }

    private static async Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        HttpMethod method,
        string route,
        object? body = null,
        string? idempotencyKey = null,
        string? etag = null,
        string culture = Portuguese)
    {
        using var request = Request(method, route, body, idempotencyKey, etag, culture);
        return await client.SendAsync(request);
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        var text = await response.Content.ReadAsStringAsync();
        text.Should().NotBeNullOrWhiteSpace();
        using var document = JsonDocument.Parse(text);
        return document.RootElement.Clone();
    }

    private static async Task<JsonElement> AssertLocalizedErrorAsync(
        HttpResponseMessage response,
        HttpStatusCode status,
        string culture,
        bool requireFieldErrors = false)
    {
        response.StatusCode.Should().Be(status);
        var error = await ReadJsonAsync(response);
        var code = error.GetProperty("messageCode").GetString();
        code.Should().NotBeNullOrWhiteSpace();
        error.GetProperty("message").GetString().Should().Be(Messages.GetMessage(code!, culture));
        var errors = error.GetProperty("errors");
        errors.ValueKind.Should().Be(JsonValueKind.Array);
        errors.EnumerateArray().Should().OnlyContain(item => !string.IsNullOrWhiteSpace(item.GetString()));
        if (requireFieldErrors)
        {
            var fieldErrors = error.GetProperty("fieldErrors");
            fieldErrors.EnumerateArray().Should().NotBeEmpty();
            foreach (var fieldError in fieldErrors.EnumerateArray())
            {
                fieldError.GetProperty("field").GetString().Should().NotBeNullOrWhiteSpace();
                var fieldCode = fieldError.GetProperty("messageCode").GetString();
                fieldCode.Should().NotBeNullOrWhiteSpace();
                fieldError.GetProperty("message").GetString().Should().Be(Messages.GetMessage(fieldCode!, culture));
            }
        }

        return error;
    }

    private static string RequireETag(HttpResponseMessage response)
    {
        var etag = response.Headers.ETag?.ToString();
        etag.Should().NotBeNullOrWhiteSpace();
        etag.Should().MatchRegex("^W/\\\"[0-9]+\\\"$");
        return etag!;
    }

    private static string RequireChangedETag(HttpResponseMessage response, string previous)
    {
        var current = RequireETag(response);
        current.Should().NotBe(previous, "every Match mutation advances the containing Series version");
        return current;
    }

    private static void AssertPage(JsonElement page, int number, int size)
    {
        page.GetProperty("page").GetInt32().Should().Be(number);
        page.GetProperty("pageSize").GetInt32().Should().Be(size);
        page.GetProperty("items").ValueKind.Should().Be(JsonValueKind.Array);
        page.GetProperty("totalItems").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        page.GetProperty("totalPages").GetInt32().Should().BeGreaterThanOrEqualTo(0);
    }

    private static void AssertSeriesDetail(JsonElement series, Guid id, string? expectedState = null)
    {
        series.GetProperty("id").GetGuid().Should().Be(id);
        series.GetProperty("seasonId").GetGuid().Should().NotBeEmpty();
        series.GetProperty("competicaoId").GetGuid().Should().NotBeEmpty();
        series.GetProperty("rodadaId").GetGuid().Should().NotBeEmpty();
        series.GetProperty("versaoRegrasId").GetGuid().Should().NotBeEmpty();
        AssertOptionalGuid(series, "eventoId");
        series.GetProperty("tipo").GetString().Should().Be("DiariaTemporaria");
        series.GetProperty("formato").GetString().Should().BeOneOf("Md3", "Md5");
        series.GetProperty("modoDraft").GetString().Should().BeOneOf("Padrao", "Fearless");
        var state = series.GetProperty("estado").GetString();
        state.Should().BeOneOf("Agendada", "EmAndamento", "Concluida", "Cancelada", "Anulada");
        if (expectedState is not null)
        {
            state.Should().Be(expectedState);
        }
        series.GetProperty("agendadaPara").GetDateTimeOffset().Should().NotBe(default);
        var sides = series.GetProperty("lados");
        sides.GetArrayLength().Should().Be(2);
        sides.EnumerateArray().Select(side => side.GetProperty("id").GetGuid()).Should().OnlyHaveUniqueItems();
        foreach (var side in sides.EnumerateArray())
        {
            side.GetProperty("tipo").GetString().Should().Be("Temporario");
            side.GetProperty("origemId").GetGuid().Should().NotBeEmpty();
            side.GetProperty("nome").GetString().Should().NotBeNullOrWhiteSpace();
            side.GetProperty("capitaoJogadorId").GetGuid().Should().NotBeEmpty();
        }

        series.GetProperty("placar").EnumerateArray().Should().HaveCount(2)
            .And.OnlyContain(score => score.GetInt32() >= 0);
        var matches = series.GetProperty("partidas");
        matches.ValueKind.Should().Be(JsonValueKind.Array);
        foreach (var match in matches.EnumerateArray())
        {
            AssertMatchDetail(match, match.GetProperty("id").GetGuid(), id);
        }

        AssertFearless(series.GetProperty("bloqueiosFearless"));
        series.GetProperty("elegivelOficial").ValueKind.Should().BeOneOf(JsonValueKind.True, JsonValueKind.False);
        series.GetProperty("revisaoNecessaria").ValueKind.Should().BeOneOf(JsonValueKind.True, JsonValueKind.False);
        series.GetProperty("versao").GetInt64().Should().BeGreaterThanOrEqualTo(0);
        series.GetProperty("acoesPermitidas").ValueKind.Should().Be(JsonValueKind.Array);
    }

    private static void AssertMatchDetail(
        JsonElement match,
        Guid id,
        Guid seriesId,
        string? expectedState = null,
        string? expectedReason = null)
    {
        match.GetProperty("id").GetGuid().Should().Be(id);
        match.GetProperty("serieId").GetGuid().Should().Be(seriesId);
        match.GetProperty("ordem").GetInt32().Should().BeGreaterThan(0);
        match.GetProperty("estado").GetString().Should().BeOneOf("Rascunho", "Confirmada", "Remake", "Anulada");
        if (expectedState is not null)
        {
            match.GetProperty("estado").GetString().Should().Be(expectedState);
        }

        if (expectedReason is not null)
        {
            match.GetProperty("motivoTermino").GetString().Should().Be(expectedReason);
        }

        AssertOptionalGuid(match, "ladoVencedorId");
        AssertOptionalString(match, "motivoTermino", "Normal", "Surrender");
        AssertOptionalString(match, "decisaoPicksRemake", "PreservarPicks", "DesconsiderarPicks");
        var picks = match.GetProperty("picks");
        picks.ValueKind.Should().Be(JsonValueKind.Array);
        foreach (var pick in picks.EnumerateArray())
        {
            pick.GetProperty("ladoSerieId").GetGuid().Should().NotBeEmpty();
            pick.GetProperty("championId").GetInt32().Should().BePositive();
            pick.GetProperty("ordem").GetInt32().Should().BeInRange(1, 5);
        }

        match.GetProperty("conflitoFearless").ValueKind.Should().BeOneOf(JsonValueKind.True, JsonValueKind.False);
        match.GetProperty("acoesPermitidas").ValueKind.Should().Be(JsonValueKind.Array);
    }

    private static void AssertSeriesResult(JsonElement result, Guid seriesId)
    {
        result.GetProperty("serieId").GetGuid().Should().Be(seriesId);
        result.GetProperty("placar").EnumerateArray().Should().HaveCount(2)
            .And.OnlyContain(score => score.GetInt32() >= 0);
        AssertOptionalGuid(result, "ladoVencedorId");
        result.GetProperty("concluida").ValueKind.Should().BeOneOf(JsonValueKind.True, JsonValueKind.False);
        result.GetProperty("elegivelOficial").ValueKind.Should().BeOneOf(JsonValueKind.True, JsonValueKind.False);
    }

    private static void AssertMutationResult(
        JsonElement result,
        Guid matchId,
        Guid seriesId,
        string? expectedState = null,
        string? expectedReason = null)
    {
        AssertMatchDetail(result.GetProperty("partida"), matchId, seriesId, expectedState, expectedReason);
        AssertSeriesResult(result.GetProperty("resultadoSerie"), seriesId);
        AssertFearless(result.GetProperty("bloqueiosFearless"));
        result.GetProperty("revisaoNecessaria").ValueKind.Should().BeOneOf(JsonValueKind.True, JsonValueKind.False);
    }

    private static void AssertSeasonSummary(JsonElement season)
    {
        season.GetProperty("id").GetGuid().Should().NotBeEmpty();
        season.GetProperty("nome").GetString().Should().NotBeNullOrWhiteSpace();
        season.GetProperty("ano").GetInt32().Should().BeInRange(2009, 9999);
        season.GetProperty("ordemNoAno").GetInt32().Should().BePositive();
        DateOnly.Parse(season.GetProperty("dataInicio").GetString()!).Should()
            .BeBefore(DateOnly.Parse(season.GetProperty("dataFimExclusiva").GetString()!));
        season.GetProperty("estado").GetString().Should().BeOneOf("Planejada", "Ativa", "Encerrada");
        season.GetProperty("versao").GetInt64().Should().BeGreaterThanOrEqualTo(0);
    }

    private static void AssertAuditRecord(JsonElement audit, Guid seriesId)
    {
        audit.GetProperty("id").GetGuid().Should().NotBeEmpty();
        audit.GetProperty("recursoTipo").GetString().Should().Be("Serie");
        audit.GetProperty("recursoId").GetGuid().Should().Be(seriesId);
        audit.GetProperty("acao").GetString().Should().NotBeNullOrWhiteSpace();
        audit.GetProperty("atorUsuarioId").GetGuid().Should().NotBeEmpty();
        audit.GetProperty("capacidade").GetString().Should().NotBeNullOrWhiteSpace();
        if (audit.TryGetProperty("justificativa", out var justification)
            && justification.ValueKind != JsonValueKind.Null)
        {
            justification.GetString().Should().NotBeNullOrWhiteSpace();
        }

        audit.GetProperty("ocorridoEm").GetDateTimeOffset().Should().NotBe(default);
    }

    private static void AssertFearless(JsonElement blockers, IEnumerable<int>? expected = null)
    {
        blockers.ValueKind.Should().Be(JsonValueKind.Array);
        var championIds = blockers.EnumerateArray().Select(champion => champion.GetInt32()).ToArray();
        championIds.Should().OnlyContain(championId => championId > 0).And.OnlyHaveUniqueItems();
        if (expected is not null)
        {
            championIds.Should().BeEquivalentTo(expected);
        }
    }

    private static void AssertOptionalGuid(JsonElement owner, string propertyName)
    {
        if (owner.TryGetProperty(propertyName, out var property) && property.ValueKind != JsonValueKind.Null)
        {
            property.GetGuid().Should().NotBeEmpty();
        }
    }

    private static void AssertOptionalString(
        JsonElement owner,
        string propertyName,
        params string[] allowedValues)
    {
        if (owner.TryGetProperty(propertyName, out var property) && property.ValueKind != JsonValueKind.Null)
        {
            property.GetString().Should().BeOneOf(allowedValues);
        }
    }

    private static string NewKey() => $"daily-{Guid.NewGuid():N}";

    private static string FindContractPath()
    {
        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            for (var directory = new DirectoryInfo(Path.GetFullPath(start)); directory is not null; directory = directory.Parent)
            {
                var candidate = Path.Combine(directory.FullName, ContractRelativePath);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        throw new FileNotFoundException($"OpenAPI contract '{ContractRelativePath}' was not found.");
    }

    private static List<string> ReadPostOperationBlock(IReadOnlyList<string> lines, string path)
    {
        var pathIndex = lines
            .Select((line, index) => (Text: line.Trim(), Index: index))
            .Single(item => item.Text == $"{path}:")
            .Index;
        var postIndex = Enumerable.Range(pathIndex + 1, lines.Count - pathIndex - 1)
            .First(index => lines[index].Trim() == "post:");
        var postIndent = lines[postIndex].Length - lines[postIndex].TrimStart().Length;

        return lines
            .Skip(postIndex + 1)
            .TakeWhile(line => string.IsNullOrWhiteSpace(line)
                || line.Length - line.TrimStart().Length > postIndent)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Trim())
            .ToList();
    }

    public enum MutationKind
    {
        CreateSeries,
        StartSeries,
        CancelSeries,
        AnnulSeries,
        CreateMatch,
        RegisterPicks,
        ConfirmNormalResult,
        ConfirmSurrender,
        RemakePreservingPicks,
        RemakeDiscardingPicks,
        AnnulMatch,
        CorrectPicks,
        CorrectResult,
    }

    public enum ReadKind
    {
        SeriesList,
        SeriesDetail,
        MatchList,
        SeriesResult,
        MatchDetail,
        Audit,
    }

    internal enum SeededSeriesState
    {
        Agendada,
        EmAndamento,
        DraftMatchEmpty,
        DraftMatchWithPicks,
        TwoDraftMatchesWithPicks,
        ConfirmedOneZero,
        ConcludedTwoZero,
    }

    private sealed class DailySeriesApiFactory(string connectionString) : SecurityApiFactory
    {
        internal HttpClient CreateClientFor(string role) => CreateJwtClient(GetActorId(), role);

        internal async Task<DailyPrerequisites> SeedDailyPrerequisitesAsync()
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
            var actorId = await context.Users.Select(user => user.Id).FirstAsync();
            var now = DateTimeOffset.UtcNow;
            var year = Interlocked.Increment(ref _nextYear);
            var players = Enumerable.Range(1, 10)
                .Select(index => CreatePlayer(year, index))
                .ToArray();
            var playerIds = players.Select(player => player.Id).ToArray();
            var captainIds = new[] { playerIds[0], playerIds[5] };
            var draft = CompetitiveFoundationFixtures.CreateDraftMontagem(
                $"Draft diário {year}", jogadoresIds: playerIds, capitaesIds: captainIds);
            var layouts = draft.Times.OrderBy(team => team.Ordem).Select((team, index) =>
                new DraftMontagemLayoutTime(
                    team.Id,
                    index == 0 ? "Lado Azul" : "Lado Vermelho",
                    captainIds[index],
                    playerIds.Skip(index * 5).Take(5)
                        .Select((playerId, order) => new DraftMontagemLayoutParticipante(playerId, order + 1, null))
                        .ToArray())).ToArray();
            draft.SalvarLayout(layouts, [], []);
            draft.Finalizar();

            var season = new Season(
                $"Temporada diária {year}", year, 1,
                new DateOnly(year, 1, 1), new DateOnly(year + 1, 1, 1), actorId, now);
            var calendar = await context.CalendariosCompetitivos.SingleOrDefaultAsync()
                ?? new CalendarioCompetitivo(Guid.NewGuid(), actorId, now);
            var previous = await context.Seasons.SingleOrDefaultAsync(item => item.Estado == SeasonEstado.Ativa);
            calendar.AtivarSeason(season, previous, calendar.Versao, actorId, now);

            var competition = new Competicao(
                season.Id, $"Circuito diário {year}", $"D{year}", true, actorId, now);
            var round = competition.AdicionarRodada("Rodada diária", 1, now, actorId);
            var rules = competition.PublicarRegras(
                SerieFormato.Md3, ModoDraft.Fearless, competition.Versao, actorId, now);

            context.Jogadores.AddRange(players);
            context.DraftMontagens.Add(draft);
            context.Seasons.Add(season);
            if (context.Entry(calendar).State == EntityState.Detached)
            {
                context.CalendariosCompetitivos.Add(calendar);
            }
            context.Competicoes.Add(competition);
            await context.SaveChangesAsync();

            var scheduled = new DateTimeOffset(year, 6, 15, 21, 0, 0, TimeSpan.FromHours(-3));
            return new DailyPrerequisites(
                season.Id,
                competition.Id,
                round.Id,
                rules.Id,
                draft.Id,
                draft.Times.OrderBy(team => team.Ordem).Select(team => team.Id).ToArray(),
                scheduled,
                new DateOnly(year, 6, 15));
        }

        internal async Task<DailyScenario> SeedDailySeriesAsync(
            SeededSeriesState state,
            bool seedAudit = false)
        {
            var prerequisites = await SeedDailyPrerequisitesAsync();
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
            var actorId = await context.Users.Select(user => user.Id).FirstAsync();
            var players = await context.DraftMontagemParticipantes
                .Where(participant => participant.DraftMontagemId == prerequisites.DraftId)
                .OrderBy(participant => participant.Ordem)
                .ToArrayAsync();
            var teams = await context.DraftMontagemTimes
                .Where(team => team.DraftMontagemId == prerequisites.DraftId)
                .OrderBy(team => team.Ordem)
                .ToArrayAsync();
            var playerNames = await context.Jogadores
                .Where(player => players.Select(participant => participant.JogadorId).Contains(player.Id))
                .ToDictionaryAsync(player => player.Id, player => player.NomeExibicao);
            var sideSeeds = teams.Select((team, index) =>
            {
                var members = players.Where(participant => participant.TimeId == team.Id).OrderBy(participant => participant.Ordem)
                    .Select(participant => new ParticipanteEsperadoSerie(
                        Guid.NewGuid(), participant.JogadorId, playerNames[participant.JogadorId], participant.Ordem))
                    .ToArray();
                return new LadoSerie(
                    Guid.NewGuid(), index + 1, LadoSerieTipo.Temporario, team.Id,
                    team.Nome, null, team.CapitaoId, playerNames[team.CapitaoId!.Value], members);
            }).ToArray();
            var series = new Serie(
                prerequisites.SeasonId,
                prerequisites.CompetitionId,
                prerequisites.RoundId,
                prerequisites.RulesId,
                eventoId: null,
                prerequisites.DraftId,
                SerieTipo.DiariaTemporaria,
                SerieFormato.Md3,
                ModoDraft.Fearless,
                fearlessHabilitado: true,
                prerequisites.ScheduledAt,
                prerequisites.LocalDate,
                actorId,
                DateTimeOffset.UtcNow,
                sideSeeds);
            context.Series.Add(series);
            await context.SaveChangesAsync();
            var persistedSides = await context.LadosSeries.AsNoTracking()
                .Where(side => side.SerieId == series.Id)
                .OrderBy(side => side.Ordem)
                .ToArrayAsync();
            var now = DateTimeOffset.UtcNow;
            Partida? firstMatch = null;
            Partida? secondMatch = null;

            if (state >= SeededSeriesState.EmAndamento)
            {
                TransitionSeries(series, SerieEstado.EmAndamento, 1, actorId, now);
            }

            if (state >= SeededSeriesState.DraftMatchEmpty)
            {
                firstMatch = AddDraftMatch(context, series, 1, now.AddMinutes(1));
                SetProperty(series, nameof(Serie.Versao), 2L);
            }

            if (state >= SeededSeriesState.DraftMatchWithPicks)
            {
                AddCompletePicks(context, firstMatch!, persistedSides, firstChampionId: 1, now.AddMinutes(2));
                SetProperty(firstMatch!, nameof(Partida.Versao), 1L);
                SetProperty(firstMatch!, nameof(Partida.AtualizadaEm), now.AddMinutes(2));
                SetProperty(series, nameof(Serie.Versao), 3L);
            }

            if (state == SeededSeriesState.TwoDraftMatchesWithPicks)
            {
                secondMatch = AddDraftMatch(context, series, 2, now.AddMinutes(3));
                AddCompletePicks(context, secondMatch, persistedSides, firstChampionId: 11, now.AddMinutes(4));
                SetProperty(secondMatch, nameof(Partida.Versao), 1L);
                SetProperty(secondMatch, nameof(Partida.AtualizadaEm), now.AddMinutes(4));
                SetProperty(series, nameof(Serie.Versao), 5L);
            }

            if (state is SeededSeriesState.ConfirmedOneZero or SeededSeriesState.ConcludedTwoZero)
            {
                ConfirmMatch(firstMatch!, persistedSides[0].Id, now.AddMinutes(3));
                SetProperty(series, nameof(Serie.Versao), 4L);
            }

            if (state == SeededSeriesState.ConcludedTwoZero)
            {
                secondMatch = AddDraftMatch(context, series, 2, now.AddMinutes(4));
                AddCompletePicks(context, secondMatch, persistedSides, firstChampionId: 11, now.AddMinutes(5));
                ConfirmMatch(secondMatch, persistedSides[0].Id, now.AddMinutes(6));
                TransitionSeries(series, SerieEstado.Concluida, 7, actorId, now.AddMinutes(6));
                SetProperty(series, nameof(Serie.LadoVencedorId), (Guid?)persistedSides[0].Id);
                SetProperty(series, nameof(Serie.ConcluidaEm), (DateTimeOffset?)now.AddMinutes(6));
            }

            if (seedAudit)
            {
                context.RegistrosAuditoriaCompetitiva.Add(new RegistroAuditoriaCompetitiva(
                    RecursoCompetitivoTipo.Serie,
                    series.Id,
                    AcaoAuditoriaCompetitiva.SerieCriada,
                    actorId,
                    AuthPermissions.CanManageMatches,
                    justificativa: null,
                    valorAnterior: null,
                    valorPosterior: null,
                    Guid.NewGuid(),
                    now));
            }

            await context.SaveChangesAsync();
            return new DailyScenario(
                series.Id,
                persistedSides.Select(side => side.Id).ToArray(),
                firstMatch?.Id,
                secondMatch?.Id,
                $"W/\"{series.Versao}\"");
        }

        private static Partida AddDraftMatch(
            RinhaDasLendasDbContext context,
            Serie series,
            int order,
            DateTimeOffset createdAt)
        {
            var match = new Partida(series.Id, order, createdAt);
            context.Partidas.Add(match);
            return match;
        }

        private static void AddCompletePicks(
            RinhaDasLendasDbContext context,
            Partida match,
            IReadOnlyCollection<LadoSerie> sides,
            int firstChampionId,
            DateTimeOffset registeredAt)
        {
            var orderedSides = sides.OrderBy(side => side.Ordem).ToArray();
            var picks = orderedSides.SelectMany((side, sideIndex) =>
                Enumerable.Range(1, 5).Select(order => new PickPartida(
                    match,
                    side.Id,
                    firstChampionId + sideIndex * 5 + order - 1,
                    order,
                    registeredAt)));
            context.PicksPartidas.AddRange(picks);
        }

        private static void ConfirmMatch(Partida match, Guid winnerSideId, DateTimeOffset confirmedAt)
        {
            SetProperty(match, nameof(Partida.Estado), PartidaEstado.Confirmada);
            SetProperty(match, nameof(Partida.LadoVencedorId), (Guid?)winnerSideId);
            SetProperty(match, nameof(Partida.MotivoTermino), (MotivoTerminoPartida?)MotivoTerminoPartida.Normal);
            SetProperty(match, nameof(Partida.ConfirmadaEm), (DateTimeOffset?)confirmedAt);
            SetProperty(match, nameof(Partida.AtualizadaEm), confirmedAt);
            SetProperty(match, nameof(Partida.Versao), 2L);
        }

        private static void TransitionSeries(
            Serie series,
            SerieEstado state,
            long version,
            Guid actorId,
            DateTimeOffset updatedAt)
        {
            SetProperty(series, nameof(Serie.Estado), state);
            SetProperty(series, nameof(Serie.Versao), version);
            SetProperty(series, nameof(Serie.AtualizadaPorUsuarioId), actorId);
            SetProperty(series, nameof(Serie.AtualizadaEm), updatedAt);
        }

        private static void SetProperty<T>(object target, string propertyName, T value) =>
            target.GetType().GetProperty(propertyName)!.SetValue(target, value);

        private Guid GetActorId()
        {
            using var scope = Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>().Users
                .Select(user => user.Id)
                .First();
        }

        private static Jogador CreatePlayer(int year, int index) => new(
            $"Jogador diário {year}-{index}",
            null,
            $"daily-{year}-{index}",
            null,
            null,
            null,
            Elo.Ouro,
            Divisao.II,
            [
                new PreferenciaRota(Rota.Top, 1, false),
                new PreferenciaRota(Rota.Jungle, 2, false),
                new PreferenciaRota(Rota.Mid, 3, false),
                new PreferenciaRota(Rota.Adc, 4, false),
                new PreferenciaRota(Rota.Support, 5, false),
            ]);

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder
                .UseSetting("ConnectionStrings:RinhaDasLendas", connectionString)
                .UseSetting("ConnectionStrings:DefaultConnection", connectionString)
                .UseSetting("Authentication:BootstrapSuperAdmin:Enabled", "true")
                .UseSetting("Authentication:BootstrapSuperAdmin:Email", "daily-series-api@example.com")
                .UseSetting("Authentication:BootstrapSuperAdmin:Senha", "DailySeriesApi123!")
                .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:RinhaDasLendas"] = connectionString,
                        ["ConnectionStrings:DefaultConnection"] = connectionString,
                    }));
        }
    }

    private sealed record DailyPrerequisites(
        Guid SeasonId,
        Guid CompetitionId,
        Guid RoundId,
        Guid RulesId,
        Guid DraftId,
        IReadOnlyList<Guid> SideOriginIds,
        DateTimeOffset ScheduledAt,
        DateOnly LocalDate)
    {
        internal object CreateRequest => new
        {
            seasonId = SeasonId,
            competicaoId = CompetitionId,
            rodadaId = RoundId,
            versaoRegrasId = RulesId,
            eventoId = (Guid?)null,
            tipo = "DiariaTemporaria",
            agendadaPara = ScheduledAt,
            dataLocal = LocalDate,
            draftMontagemId = DraftId,
            ladoOrigemIds = SideOriginIds,
        };
    }

    private sealed record DailyScenario(
        Guid SeriesId,
        IReadOnlyList<Guid> SideIds,
        Guid? MatchId,
        Guid? SecondMatchId,
        string ETag);

    private sealed record MutationScenario(
        HttpMethod Method,
        string Route,
        object? Body,
        object? DivergentBody,
        string? ETag,
        HttpStatusCode SuccessStatus);
}
