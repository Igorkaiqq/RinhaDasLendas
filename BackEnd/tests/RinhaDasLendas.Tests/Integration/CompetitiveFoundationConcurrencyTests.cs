using System.Collections;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Infrastructure.Identity;
using RinhaDasLendas.Infrastructure.Messages;
using RinhaDasLendas.Infrastructure.Persistence;
using RinhaDasLendas.Tests.Fixtures;
using RinhaDasLendas.Tests.Infrastructure;

namespace RinhaDasLendas.Tests.Integration;

public sealed class CompetitiveFoundationConcurrencyTests
{
    private const string SeasonsRoute = "/api/v1/temporadas";
    private static readonly ResourceMessageProvider Messages = new();

    [Fact]
    public async Task CanonicallyEquivalentContent_ShouldReplayWithoutDuplicateAuditOrIdempotencyEffects()
    {
        await using var factory = new CompetitiveConcurrencyApiFactory();
        var actorId = factory.GetPrimaryActorId();
        using var client = factory.CreatePresidentClient(actorId);
        var key = NewKey();

        using var first = await SendRawJsonAsync(
            client, HttpMethod.Post, SeasonsRoute, SeasonJson("Season canonica", 2026), key);
        first.StatusCode.Should().Be(HttpStatusCode.Created,
            "the competitive Season endpoint must exist before replay behavior can run");
        var firstBodyBytes = await first.Content.ReadAsByteArrayAsync();
        var firstBodyText = await first.Content.ReadAsStringAsync();
        var firstHeaders = ReadReplayableHeaders(first);
        var auditAfterFirst = factory.GetEntityIds("RegistroAuditoriaCompetitiva");
        var operationsAfterFirst = factory.GetIdempotencyRecords(actorId, key, "POST", SeasonsRoute);

        using var replay = await SendRawJsonAsync(
            client, HttpMethod.Post, SeasonsRoute, ReorderedSeasonJson("Season canonica", 2026), key);

        replay.StatusCode.Should().Be(first.StatusCode);
        (await replay.Content.ReadAsByteArrayAsync()).Should().Equal(firstBodyBytes,
            "replay must preserve the exact original response bytes");
        (await replay.Content.ReadAsStringAsync()).Should().Be(firstBodyText,
            "replay must preserve the exact original response text");
        ReadReplayableHeaders(replay).Should().BeEquivalentTo(firstHeaders,
            "replay must preserve every original stable response and content header");
        AssertReplayed(replay, true);
        factory.CountEntities("Season").Should().Be(1);
        auditAfterFirst.Should().NotBeEmpty("Season creation must produce an audit effect");
        factory.GetEntityIds("RegistroAuditoriaCompetitiva").Should().BeEquivalentTo(auditAfterFirst);
        operationsAfterFirst.Should().ContainSingle();
        factory.GetIdempotencyRecords(actorId, key, "POST", SeasonsRoute)
            .Should().BeEquivalentTo(operationsAfterFirst,
                "replay must not append or replace the original idempotency effect");
    }

    [Fact]
    public async Task IdempotencyKey_ShouldBeIsolatedByActorAndRoute()
    {
        await using var factory = new CompetitiveConcurrencyApiFactory();
        var firstActorId = factory.GetPrimaryActorId();
        var secondActorId = await factory.CreateActorAsync("second-president@example.com");
        using var firstActor = factory.CreatePresidentClient(firstActorId);
        using var secondActor = factory.CreatePresidentClient(secondActorId);
        var key = NewKey();

        var firstSeason = await CreateSeasonAsync(firstActor, "Season ator A", 2026, key);
        using var otherActorResponse = await SendRawJsonAsync(
            secondActor, HttpMethod.Post, SeasonsRoute, SeasonJson("Season ator B", 2027), key);
        otherActorResponse.StatusCode.Should().Be(HttpStatusCode.Created,
            "the same key belongs to a distinct namespace for another authenticated actor");
        AssertReplayed(otherActorResponse, false);

        using var otherRouteResponse = await SendRawJsonAsync(
            firstActor,
            HttpMethod.Post,
            $"{SeasonsRoute}/{firstSeason.Id}/aberturas",
            null,
            key,
            "W/\"0\"");
        otherRouteResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "the same actor, method and key on another mutation route must execute independently");
        AssertReplayed(otherRouteResponse, false);

        factory.CountEntities("Season").Should().Be(2);
        factory.GetIdempotencyRecords(firstActorId, key, "POST", SeasonsRoute).Should().ContainSingle();
        factory.GetIdempotencyRecords(secondActorId, key, "POST", SeasonsRoute).Should().ContainSingle();
        var firstActorRecords = factory.GetIdempotencyRecords(firstActorId, key);
        firstActorRecords.Should().HaveCount(2);
        firstActorRecords.Select(record =>
                $"{ReadProperty(record, "Metodo")}|{ReadProperty(record, "Rota")}")
            .Should().OnlyHaveUniqueItems("route forms part of the namespace even when the HTTP method is the same");
        firstActorRecords.Select(record => ReadProperty(record, "Metodo")?.ToString())
            .Should().OnlyContain(method => string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ConcurrencyHookProxy_ShouldForwardObservedVersionAndReleaseBothArrivals()
    {
        var coordinator = new ConcurrentReadCoordinator(TimeSpan.FromSeconds(1));
        var proxyObject = DispatchProxy.Create(
            typeof(IConcurrencyHookContractForTests),
            typeof(ConcurrencyHookProxy));
        ((ConcurrencyHookProxy)proxyObject).Coordinator = coordinator;
        var proxy = (IConcurrencyHookContractForTests)proxyObject;
        var seriesId = Guid.NewGuid();
        const long observedVersion = 17;
        coordinator.Arm(seriesId, observedVersion);

        var first = proxy.WaitAfterSeriesReadAsync(seriesId, observedVersion, CancellationToken.None);
        first.IsCompleted.Should().BeFalse("the first operation must wait for its competitor");
        var second = proxy.WaitAfterSeriesReadAsync(seriesId, observedVersion, CancellationToken.None);
        await Task.WhenAll(first, second);

        coordinator.AssertReleasedPair();
    }

    [Fact]
    public async Task ReadBarrier_ShouldThrowWhenSecondArrivalDoesNotReleaseBeforeTimeout()
    {
        var coordinator = new ConcurrentReadCoordinator(TimeSpan.FromMilliseconds(25));
        var seriesId = Guid.NewGuid();
        coordinator.Arm(seriesId, 3);

        var act = () => coordinator.WaitAsync(seriesId, 3, CancellationToken.None);

        await act.Should().ThrowAsync<TimeoutException>()
            .WithMessage("*second operation*");
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(6L)]
    public async Task ReadBarrier_ShouldRejectMissingOrMismatchedObservedVersion(long observedVersion)
    {
        var coordinator = new ConcurrentReadCoordinator(TimeSpan.FromSeconds(1));
        var seriesId = Guid.NewGuid();
        coordinator.Arm(seriesId, 7);

        var act = () => coordinator.WaitAsync(seriesId, observedVersion, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*observed Series version*");
    }

    [Fact]
    public async Task DivergentContent_ShouldReturnSameStableCodeLocalizedInPortugueseAndEnglishWithoutEffects()
    {
        await using var factory = new CompetitiveConcurrencyApiFactory();
        var actorId = factory.GetPrimaryActorId();
        using var client = factory.CreatePresidentClient(actorId);
        var key = NewKey();

        using var first = await SendRawJsonAsync(
            client, HttpMethod.Post, SeasonsRoute, SeasonJson("Conteudo original", 2026), key);
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        var seasonsAfterFirst = factory.CountEntities("Season");
        var auditAfterFirst = factory.CountEntities("RegistroAuditoriaCompetitiva");
        var operationsAfterFirst = factory.GetIdempotencyRecords(actorId, key, "POST", SeasonsRoute);

        using var ptConflict = await SendRawJsonAsync(
            client, HttpMethod.Post, SeasonsRoute, SeasonJson("Conteudo divergente", 2027), key, culture: "pt-BR");
        using var enConflict = await SendRawJsonAsync(
            client, HttpMethod.Post, SeasonsRoute, SeasonJson("Different content", 2028), key, culture: "en-US");

        ptConflict.StatusCode.Should().Be(HttpStatusCode.Conflict);
        enConflict.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var ptError = await ReadApiErrorAsync(ptConflict);
        var enError = await ReadApiErrorAsync(enConflict);
        ptError.MessageCode.Should().NotBeNullOrWhiteSpace();
        enError.MessageCode.Should().Be(ptError.MessageCode);
        ptError.Message.Should().Be(Messages.GetMessage(ptError.MessageCode, "pt-BR"));
        enError.Message.Should().Be(Messages.GetMessage(enError.MessageCode, "en-US"));
        ptError.Message.Should().NotBe($"[{ptError.MessageCode}]", "PT-BR must resolve a real resource");
        enError.Message.Should().NotBe($"[{enError.MessageCode}]", "EN-US must resolve a real resource");
        enError.Message.Should().NotBe(ptError.Message);
        factory.CountEntities("Season").Should().Be(seasonsAfterFirst);
        factory.CountEntities("RegistroAuditoriaCompetitiva").Should().Be(auditAfterFirst);
        factory.GetIdempotencyRecords(actorId, key, "POST", SeasonsRoute)
            .Should().BeEquivalentTo(operationsAfterFirst);
    }

    [Fact]
    public async Task Entry_ShouldReplayAtExactlyNinetyDaysAndExpireImmediatelyAfterBoundary()
    {
        await using var factory = new CompetitiveConcurrencyApiFactory();
        var actorId = factory.GetPrimaryActorId();
        using var client = factory.CreatePresidentClient(actorId);
        var key = NewKey();

        using var first = await SendRawJsonAsync(
            client, HttpMethod.Post, SeasonsRoute, SeasonJson("Retencao exata", 2026), key);
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        var originalRecord = factory.GetIdempotencyRecords(actorId, key, "POST", SeasonsRoute)
            .Should().ContainSingle().Subject;
        ReadInstant(originalRecord, "CriadaEm").Should().Be(factory.Clock.GetUtcNow());
        ReadInstant(originalRecord, "ExpiraEm").Should().Be(factory.Clock.GetUtcNow().AddDays(90));

        factory.Clock.Advance(TimeSpan.FromDays(90));
        using var boundaryReplay = await SendRawJsonAsync(
            client, HttpMethod.Post, SeasonsRoute, ReorderedSeasonJson("Retencao exata", 2026), key);
        boundaryReplay.StatusCode.Should().Be(HttpStatusCode.Created);
        AssertReplayed(boundaryReplay, true);
        factory.CountEntities("Season").Should().Be(1);

        factory.Clock.Advance(TimeSpan.FromTicks(1));
        using var afterExpiration = await SendRawJsonAsync(
            client, HttpMethod.Post, SeasonsRoute, SeasonJson("Nova operacao apos expirar", 2027), key);
        afterExpiration.StatusCode.Should().Be(HttpStatusCode.Created,
            "the key may be reused immediately after the inclusive 90-day replay boundary");
        AssertReplayed(afterExpiration, false);
        factory.CountEntities("Season").Should().Be(2);
        factory.CountEntities("RegistroAuditoriaCompetitiva").Should().Be(2);
        var replacement = factory.GetIdempotencyRecords(actorId, key, "POST", SeasonsRoute)
            .Should().ContainSingle().Subject;
        ReadGuid(replacement, "Id").Should().NotBe(ReadGuid(originalRecord, "Id"));
        ReadInstant(replacement, "CriadaEm").Should().Be(factory.Clock.GetUtcNow());
    }

    [Fact]
    public async Task StaleETag_ShouldLeaveETagVersionAuditAndSeasonEffectsUnchanged()
    {
        await using var factory = new CompetitiveConcurrencyApiFactory();
        using var client = factory.CreatePresidentClient(factory.GetPrimaryActorId());
        var created = await CreateSeasonAsync(client, "Season versionada", 2026);

        using var accepted = await SendRawJsonAsync(
            client,
            HttpMethod.Patch,
            $"{SeasonsRoute}/{created.Id}",
            "{\"nome\":\"Nome aceito\"}",
            NewKey(),
            created.ETag);
        accepted.StatusCode.Should().Be(HttpStatusCode.OK);
        var acceptedETag = RequireETag(accepted);
        var acceptedBody = await ReadJsonAsync(accepted);
        var acceptedVersion = acceptedBody.GetProperty("versao").GetInt64();
        var seasonsAfterAccepted = factory.CountEntities("Season");
        var auditAfterAccepted = factory.GetEntityIds("RegistroAuditoriaCompetitiva");

        using var stale = await SendRawJsonAsync(
            client,
            HttpMethod.Patch,
            $"{SeasonsRoute}/{created.Id}",
            "{\"nome\":\"Sobrescrita silenciosa\"}",
            NewKey(),
            created.ETag,
            "pt-BR");
        stale.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var staleError = await ReadApiErrorAsync(stale);
        staleError.MessageCode.Should().NotBeNullOrWhiteSpace();
        staleError.Message.Should().Be(Messages.GetMessage(staleError.MessageCode, "pt-BR"));

        using var detail = await client.GetAsync($"{SeasonsRoute}/{created.Id}");
        detail.StatusCode.Should().Be(HttpStatusCode.OK);
        RequireETag(detail).Should().Be(acceptedETag);
        var detailBody = await ReadJsonAsync(detail);
        detailBody.GetProperty("versao").GetInt64().Should().Be(acceptedVersion);
        JsonElement.DeepEquals(detailBody, acceptedBody).Should().BeTrue();
        factory.CountEntities("Season").Should().Be(seasonsAfterAccepted);
        factory.GetEntityIds("RegistroAuditoriaCompetitiva").Should().BeEquivalentTo(auditAfterAccepted);
    }

    [Fact]
    public async Task ConcurrentConfirmations_ShouldAdvanceOneVersionAndPersistOnlyWinningAlternative()
    {
        await using var factory = new CompetitiveConcurrencyApiFactory();
        using var client = factory.CreatePresidentClient(factory.GetPrimaryActorId());
        var scenario = await CreateSeriesWithTwoDraftMatchesAsync(factory, client);
        var auditBefore = factory.GetEntityIds("RegistroAuditoriaCompetitiva");
        factory.ArmSeriesReadBarrier(scenario.SeriesId, scenario.Version);

        var responses = await Task.WhenAll(
            SendRawJsonAsync(
                client,
                HttpMethod.Post,
                $"/api/v1/partidas/{scenario.FirstMatchId}/resultados",
                ResultJson(scenario.FirstSideId),
                NewKey(),
                scenario.ETag),
            SendRawJsonAsync(
                client,
                HttpMethod.Post,
                $"/api/v1/partidas/{scenario.SecondMatchId}/resultados",
                ResultJson(scenario.SecondSideId),
                NewKey(),
                scenario.ETag));
        using var first = responses[0];
        using var second = responses[1];

        factory.AssertBothOperationsCrossedReadBoundary();
        responses.Select(response => response.StatusCode).Should().BeEquivalentTo(
            [HttpStatusCode.OK, HttpStatusCode.Conflict]);
        var winnerIndex = Array.FindIndex(responses, response => response.StatusCode == HttpStatusCode.OK);
        var winnerMatchId = winnerIndex == 0 ? scenario.FirstMatchId : scenario.SecondMatchId;
        var loserMatchId = winnerIndex == 0 ? scenario.SecondMatchId : scenario.FirstMatchId;
        var winnerSideId = winnerIndex == 0 ? scenario.FirstSideId : scenario.SecondSideId;
        var expectedScore = winnerIndex == 0 ? new[] { 1, 0 } : new[] { 0, 1 };

        using var detail = await client.GetAsync($"/api/v1/series/{scenario.SeriesId}");
        detail.StatusCode.Should().Be(HttpStatusCode.OK);
        RequireETag(detail).Should().Be(RequireETag(responses[winnerIndex])).And.NotBe(scenario.ETag);
        var aggregate = await ReadJsonAsync(detail);
        aggregate.GetProperty("versao").GetInt64().Should().Be(scenario.Version + 1);
        ReadScore(aggregate).Should().Equal(expectedScore);
        ReadNullableGuid(aggregate, "ladoVencedorId").Should().BeNull("one MD3 win does not conclude the Series");
        AssertMatch(aggregate, winnerMatchId, "Confirmada", winnerSideId);
        AssertMatch(aggregate, loserMatchId, "Rascunho", null);
        var newAudit = factory.GetNewEntities("RegistroAuditoriaCompetitiva", auditBefore)
            .Should().ContainSingle("the losing transaction must not leave partial audit records").Subject;
        ReadGuid(newAudit, "RecursoId").Should().Be(winnerMatchId);
    }

    [Fact]
    public async Task ConcurrentCorrectionAndConfirmation_ShouldPersistOneCompleteAllowedAggregateAlternative()
    {
        await using var factory = new CompetitiveConcurrencyApiFactory();
        using var client = factory.CreatePresidentClient(factory.GetPrimaryActorId());
        var scenario = await CreateSeriesWithTwoDraftMatchesAsync(factory, client);
        using var initialConfirmation = await SendRawJsonAsync(
            client,
            HttpMethod.Post,
            $"/api/v1/partidas/{scenario.FirstMatchId}/resultados",
            ResultJson(scenario.FirstSideId),
            NewKey(),
            scenario.ETag);
        initialConfirmation.StatusCode.Should().Be(HttpStatusCode.OK);
        var sharedETag = RequireETag(initialConfirmation);
        using var baselineResponse = await client.GetAsync($"/api/v1/series/{scenario.SeriesId}");
        var baseline = await ReadJsonAsync(baselineResponse);
        var baselineVersion = baseline.GetProperty("versao").GetInt64();
        var auditBefore = factory.GetEntityIds("RegistroAuditoriaCompetitiva");
        factory.ArmSeriesReadBarrier(scenario.SeriesId, baselineVersion);

        var responses = await Task.WhenAll(
            SendRawJsonAsync(
                client,
                HttpMethod.Post,
                $"/api/v1/partidas/{scenario.FirstMatchId}/correcoes",
                CorrectionJson(scenario.SecondSideId),
                NewKey(),
                sharedETag),
            SendRawJsonAsync(
                client,
                HttpMethod.Post,
                $"/api/v1/partidas/{scenario.SecondMatchId}/resultados",
                ResultJson(scenario.SecondSideId),
                NewKey(),
                sharedETag));
        using var correction = responses[0];
        using var confirmation = responses[1];

        factory.AssertBothOperationsCrossedReadBoundary();
        responses.Select(response => response.StatusCode).Should().BeEquivalentTo(
            [HttpStatusCode.OK, HttpStatusCode.Conflict]);
        var winnerIndex = Array.FindIndex(responses, response => response.StatusCode == HttpStatusCode.OK);
        using var detail = await client.GetAsync($"/api/v1/series/{scenario.SeriesId}");
        detail.StatusCode.Should().Be(HttpStatusCode.OK);
        RequireETag(detail).Should().Be(RequireETag(responses[winnerIndex])).And.NotBe(sharedETag);
        var aggregate = await ReadJsonAsync(detail);
        aggregate.GetProperty("versao").GetInt64().Should().Be(baselineVersion + 1);
        ReadNullableGuid(aggregate, "ladoVencedorId").Should().BeNull();

        if (winnerIndex == 0)
        {
            ReadScore(aggregate).Should().Equal(0, 1);
            AssertMatch(aggregate, scenario.FirstMatchId, "Confirmada", scenario.SecondSideId);
            AssertMatch(aggregate, scenario.SecondMatchId, "Rascunho", null);
        }
        else
        {
            ReadScore(aggregate).Should().Equal(1, 1);
            AssertMatch(aggregate, scenario.FirstMatchId, "Confirmada", scenario.FirstSideId);
            AssertMatch(aggregate, scenario.SecondMatchId, "Confirmada", scenario.SecondSideId);
        }

        var newAudit = factory.GetNewEntities("RegistroAuditoriaCompetitiva", auditBefore)
            .Should().ContainSingle("exactly the winning correction or confirmation may append audit history").Subject;
        ReadGuid(newAudit, "RecursoId").Should().Be(
            winnerIndex == 0 ? scenario.FirstMatchId : scenario.SecondMatchId);
    }

    private static async Task<SeriesScenario> CreateSeriesWithTwoDraftMatchesAsync(
        CompetitiveConcurrencyApiFactory factory,
        HttpClient client)
    {
        var season = await CreateSeasonAsync(client, "Season concorrente", 2026);
        using var activation = await SendRawJsonAsync(
            client, HttpMethod.Post, $"{SeasonsRoute}/{season.Id}/aberturas", null, NewKey(), "W/\"0\"");
        activation.StatusCode.Should().Be(HttpStatusCode.OK, "the scenario requires an active Season");

        using var seasonDetail = await client.GetAsync($"{SeasonsRoute}/{season.Id}");
        seasonDetail.StatusCode.Should().Be(HttpStatusCode.OK);
        using var rules = await SendRawJsonAsync(
            client,
            HttpMethod.Post,
            $"{SeasonsRoute}/{season.Id}/regras-publicadas",
            "{\"formato\":\"Md3\",\"modoDraft\":\"Padrao\"}",
            NewKey(),
            RequireETag(seasonDetail));
        rules.StatusCode.Should().Be(HttpStatusCode.Created);
        var rulesId = (await ReadJsonAsync(rules)).GetProperty("id").GetGuid();
        var teamIds = await factory.SeedTeamsAsync();

        var seriesJson = JsonSerializer.Serialize(new
        {
            seasonId = season.Id,
            versaoRegrasId = rulesId,
            tipo = "Amistoso",
            agendadaPara = "2026-08-15T18:00:00-03:00",
            ladoOrigemIds = teamIds,
        });
        using var series = await SendRawJsonAsync(client, HttpMethod.Post, "/api/v1/series", seriesJson, NewKey());
        series.StatusCode.Should().Be(HttpStatusCode.Created);
        var seriesBody = await ReadJsonAsync(series);
        var seriesId = seriesBody.GetProperty("id").GetGuid();
        var sides = seriesBody.GetProperty("lados").EnumerateArray()
            .Select(side => side.GetProperty("id").GetGuid()).ToArray();

        using var start = await SendRawJsonAsync(
            client, HttpMethod.Post, $"/api/v1/series/{seriesId}/inicios", null, NewKey(), RequireETag(series));
        start.StatusCode.Should().Be(HttpStatusCode.OK);
        var first = await AddMatchWithPicksAsync(client, seriesId, sides, RequireETag(start), 1);
        var second = await AddMatchWithPicksAsync(client, seriesId, sides, first.ETag, 11);
        using var baseline = await client.GetAsync($"/api/v1/series/{seriesId}");
        var baselineBody = await ReadJsonAsync(baseline);
        return new SeriesScenario(
            seriesId,
            first.MatchId,
            second.MatchId,
            sides[0],
            sides[1],
            RequireETag(baseline),
            baselineBody.GetProperty("versao").GetInt64());
    }

    private static async Task<MatchVersion> AddMatchWithPicksAsync(
        HttpClient client,
        Guid seriesId,
        IReadOnlyList<Guid> sideIds,
        string etag,
        int firstChampionId)
    {
        using var created = await SendRawJsonAsync(
            client, HttpMethod.Post, $"/api/v1/series/{seriesId}/partidas", null, NewKey(), etag);
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var matchId = (await ReadJsonAsync(created)).GetProperty("id").GetGuid();
        var picksJson = JsonSerializer.Serialize(new
        {
            lados = new[]
            {
                new { ladoSerieId = sideIds[0], championIds = Enumerable.Range(firstChampionId, 5).ToArray() },
                new { ladoSerieId = sideIds[1], championIds = Enumerable.Range(firstChampionId + 5, 5).ToArray() },
            },
        });
        using var picks = await SendRawJsonAsync(
            client,
            HttpMethod.Post,
            $"/api/v1/partidas/{matchId}/picks",
            picksJson,
            NewKey(),
            RequireETag(created));
        picks.StatusCode.Should().Be(HttpStatusCode.OK);
        return new MatchVersion(matchId, RequireETag(picks));
    }

    private static async Task<CreatedResource> CreateSeasonAsync(
        HttpClient client,
        string name,
        int year,
        string? key = null)
    {
        using var response = await SendRawJsonAsync(
            client, HttpMethod.Post, SeasonsRoute, SeasonJson(name, year), key ?? NewKey());
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "the competitive Season endpoint is the prerequisite for this RED scenario");
        return new CreatedResource((await ReadJsonAsync(response)).GetProperty("id").GetGuid(), RequireETag(response));
    }

    private static async Task<HttpResponseMessage> SendRawJsonAsync(
        HttpClient client,
        HttpMethod method,
        string route,
        string? body,
        string idempotencyKey,
        string? etag = null,
        string? culture = null)
    {
        var request = new HttpRequestMessage(method, route);
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        if (etag is not null)
        {
            request.Headers.TryAddWithoutValidation("If-Match", etag);
        }

        if (culture is not null)
        {
            request.Headers.Add("Accept-Language", culture);
        }

        if (body is not null)
        {
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        return await client.SendAsync(request);
    }

    private static string SeasonJson(string name, int year) =>
        $"{{\"nome\":{JsonSerializer.Serialize(name)},\"ano\":{year},\"ordemNoAno\":1," +
        $"\"dataInicio\":\"{year}-01-01\",\"dataFimExclusiva\":\"{year + 1}-01-01\"}}";

    private static string ReorderedSeasonJson(string name, int year) =>
        $"{{\"dataFimExclusiva\":\"{year + 1}-01-01\",\"ordemNoAno\":1," +
        $"\"nome\":{JsonSerializer.Serialize(name)},\"dataInicio\":\"{year}-01-01\",\"ano\":{year}}}";

    private static string ResultJson(Guid sideId) =>
        JsonSerializer.Serialize(new { ladoVencedorId = sideId, motivoTermino = "Normal" });

    private static string CorrectionJson(Guid sideId) => JsonSerializer.Serialize(new
    {
        ladoVencedorId = sideId,
        motivoTermino = "Normal",
        justificativa = "Correcao concorrente obrigatoria",
        anularSerieSeInconclusiva = false,
    });

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }

    private static async Task<ApiError> ReadApiErrorAsync(HttpResponseMessage response)
    {
        var json = await ReadJsonAsync(response);
        return new ApiError(json.GetProperty("message").GetString()!, json.GetProperty("messageCode").GetString()!);
    }

    private static string RequireETag(HttpResponseMessage response)
    {
        var etag = response.Headers.ETag?.ToString();
        etag.Should().NotBeNullOrWhiteSpace("versioned competitive responses must return an ETag");
        return etag!;
    }

    private static IReadOnlyDictionary<string, string[]> ReadReplayableHeaders(HttpResponseMessage response)
    {
        var ignoredResponseHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Date",
            "Server",
            "Transfer-Encoding",
            "Idempotency-Replayed",
        };
        return response.Headers
            .Where(header => !ignoredResponseHeaders.Contains(header.Key))
            .ToDictionary(
                header => $"response:{header.Key}",
                header => header.Value.ToArray(),
                StringComparer.OrdinalIgnoreCase)
            .Concat(response.Content.Headers.Select(header => new KeyValuePair<string, string[]>(
                $"content:{header.Key}",
                header.Value.ToArray())))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static void AssertReplayed(HttpResponseMessage response, bool expected)
    {
        var present = response.Headers.TryGetValues("Idempotency-Replayed", out var values);
        if (expected)
        {
            present.Should().BeTrue();
            values.Should().ContainSingle().Which.Should().Be("true");
        }
        else
        {
            present.Should().BeFalse();
        }
    }

    private static int[] ReadScore(JsonElement aggregate) =>
        aggregate.GetProperty("placar").EnumerateArray().Select(item => item.GetInt32()).ToArray();

    private static Guid? ReadNullableGuid(JsonElement json, string propertyName) =>
        !json.TryGetProperty(propertyName, out var value) || value.ValueKind == JsonValueKind.Null
            ? null
            : value.GetGuid();

    private static void AssertMatch(JsonElement aggregate, Guid matchId, string state, Guid? winnerId)
    {
        var match = aggregate.GetProperty("partidas").EnumerateArray()
            .Single(item => item.GetProperty("id").GetGuid() == matchId);
        match.GetProperty("estado").GetString().Should().Be(state);
        ReadNullableGuid(match, "ladoVencedorId").Should().Be(winnerId);
    }

    private static DateTimeOffset ReadInstant(object instance, string propertyName)
    {
        var value = ReadProperty(instance, propertyName);
        return value switch
        {
            DateTimeOffset instant => instant,
            DateTime instant => new DateTimeOffset(DateTime.SpecifyKind(instant, DateTimeKind.Utc)),
            _ => throw new InvalidOperationException($"{instance.GetType().Name}.{propertyName} must be a UTC instant."),
        };
    }

    private static Guid ReadGuid(object instance, string propertyName) =>
        (Guid)(ReadProperty(instance, propertyName)
            ?? throw new InvalidOperationException($"{instance.GetType().Name}.{propertyName} is required."));

    private static object? ReadProperty(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        property.Should().NotBeNull($"{instance.GetType().Name} must expose {propertyName}");
        return property!.GetValue(instance);
    }

    private static string NewKey() => $"test-{Guid.NewGuid():N}";

    private sealed class CompetitiveConcurrencyApiFactory : SecurityApiFactory
    {
        private const string HookTypeName =
            "RinhaDasLendas.Application.Interfaces.ICompetitiveConcurrencyTestHook";
        private readonly ConcurrentReadCoordinator _coordinator = new();
        private string? _hookContractError;

        public CompetitiveConcurrencyApiFactory() : base(useIsolatedPostgreSql: true)
        {
        }

        public MutableTimeProvider Clock { get; } = new(
            new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));

        public Guid GetPrimaryActorId()
        {
            using var scope = Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>().Users
                .Select(user => user.Id).First();
        }

        public HttpClient CreatePresidentClient(Guid actorId) => CreateJwtClient(actorId, "Presidente");

        public async Task<Guid> CreateActorAsync(string email)
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
            var actor = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Nome = "Presidente de teste",
                UserName = email,
                NormalizedUserName = email.ToUpperInvariant(),
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                ConcurrencyStamp = Guid.NewGuid().ToString("N"),
                DataCadastro = Clock.GetUtcNow(),
                DataAtualizacao = Clock.GetUtcNow(),
            };
            context.Users.Add(actor);
            await context.SaveChangesAsync();
            return actor.Id;
        }

        public async Task<Guid[]> SeedTeamsAsync()
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
            var teams = new[]
            {
                CompetitiveFoundationFixtures.CreateTime("Azul Concorrente", "AZC"),
                CompetitiveFoundationFixtures.CreateTime("Vermelho Concorrente", "VMC"),
            };
            context.Times.AddRange(teams);
            await context.SaveChangesAsync();
            return teams.Select(team => team.Id).ToArray();
        }

        public int CountEntities(string entityName) => ReadSet(entityName).Count;

        public IReadOnlySet<Guid> GetEntityIds(string entityName) =>
            ReadSet(entityName).Select(entity => ReadGuid(entity, "Id")).ToHashSet();

        public IReadOnlyList<object> GetNewEntities(string entityName, IReadOnlySet<Guid> previousIds) =>
            ReadSet(entityName).Where(entity => !previousIds.Contains(ReadGuid(entity, "Id"))).ToArray();

        public IReadOnlyList<object> GetIdempotencyRecords(Guid actorId, string key) =>
            ReadSet("OperacaoIdempotente")
                .Where(record => ReadGuid(record, "AtorUsuarioId") == actorId)
                .Where(record => string.Equals(ReadProperty(record, "Chave")?.ToString(), key, StringComparison.Ordinal))
                .ToArray();

        public IReadOnlyList<object> GetIdempotencyRecords(
            Guid actorId,
            string key,
            string method,
            string route) => GetIdempotencyRecords(actorId, key)
            .Where(record => string.Equals(ReadProperty(record, "Metodo")?.ToString(), method, StringComparison.OrdinalIgnoreCase))
            .Where(record => string.Equals(ReadProperty(record, "Rota")?.ToString(), route, StringComparison.Ordinal))
            .ToArray();

        public void ArmSeriesReadBarrier(Guid seriesId, long observedVersion)
        {
            _hookContractError.Should().BeNull(
                $"the API must register {HookTypeName}.WaitAfterSeriesReadAsync(Guid, long, CancellationToken) for deterministic concurrency tests");
            _coordinator.Arm(seriesId, observedVersion);
        }

        public void AssertBothOperationsCrossedReadBoundary() =>
            _coordinator.AssertReleasedPair();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder
                .UseSetting("Authentication:BootstrapSuperAdmin:Enabled", "true")
                .UseSetting("Authentication:BootstrapSuperAdmin:Email", "competitive-concurrency@example.com")
                .UseSetting("Authentication:BootstrapSuperAdmin:Senha", "CompetitiveConcurrency123!")
                .ConfigureServices(services =>
                {
                    services.RemoveAll<TimeProvider>();
                    services.AddSingleton<TimeProvider>(Clock);
                    ConfigureConcurrencyHook(services);
                });
        }

        private void ConfigureConcurrencyHook(IServiceCollection services)
        {
            var hookType = typeof(ICurrentUser).Assembly.GetType(HookTypeName);
            if (hookType is null)
            {
                _hookContractError = $"{HookTypeName} is absent";
                return;
            }

            var method = hookType.GetMethod(
                "WaitAfterSeriesReadAsync",
                [typeof(Guid), typeof(long), typeof(CancellationToken)]);
            if (method?.ReturnType != typeof(Task))
            {
                _hookContractError =
                    $"{HookTypeName}.WaitAfterSeriesReadAsync(Guid, long, CancellationToken) is absent";
                return;
            }

            var proxy = DispatchProxy.Create(hookType, typeof(ConcurrencyHookProxy));
            ((ConcurrencyHookProxy)proxy).Coordinator = _coordinator;
            services.RemoveAll(hookType);
            services.Add(ServiceDescriptor.Singleton(hookType, proxy));
            _hookContractError = null;
        }

        private IReadOnlyList<object> ReadSet(string entityName)
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
            var entityType = typeof(RinhaDasLendas.Domain.Entities.Time).Assembly.GetType(
                $"RinhaDasLendas.Domain.Entities.{entityName}");
            entityType.Should().NotBeNull($"T006 expects the competitive domain entity {entityName}");
            var setMethod = typeof(DbContext).GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Single(method => method.Name == nameof(DbContext.Set) && method.IsGenericMethod && method.GetParameters().Length == 0)
                .MakeGenericMethod(entityType!);
            return ((IEnumerable)setMethod.Invoke(context, null)!).Cast<object>().ToArray();
        }
    }

    public class ConcurrencyHookProxy : DispatchProxy
    {
        public ConcurrencyHookProxy()
        {
        }

        internal ConcurrentReadCoordinator Coordinator { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "WaitAfterSeriesReadAsync" &&
                args is [Guid seriesId, long observedVersion, CancellationToken cancellationToken])
            {
                return Coordinator.WaitAsync(seriesId, observedVersion, cancellationToken);
            }

            throw new MissingMethodException(
                $"Unexpected concurrency hook invocation: {targetMethod?.Name ?? "<null>"}.");
        }
    }

    internal sealed class ConcurrentReadCoordinator(TimeSpan? timeout = null)
    {
        private TaskCompletionSource _release = NewCompletionSource();
        private readonly TimeSpan _timeout = timeout ?? TimeSpan.FromSeconds(10);
        private Guid _seriesId;
        private long _expectedVersion;
        private int _armed;
        private int _arrivals;
        private int _released;

        public int ArrivalCount => Volatile.Read(ref _arrivals);

        public void Arm(Guid seriesId, long observedVersion)
        {
            seriesId.Should().NotBeEmpty("a synchronization target requires a persisted Series");
            observedVersion.Should().BeGreaterThan(0, "the synchronization target requires a valid observed Series version");
            _seriesId = seriesId;
            _expectedVersion = observedVersion;
            _release = NewCompletionSource();
            Volatile.Write(ref _arrivals, 0);
            Volatile.Write(ref _released, 0);
            Volatile.Write(ref _armed, 1);
        }

        public async Task WaitAsync(Guid seriesId, long observedVersion, CancellationToken cancellationToken)
        {
            if (Volatile.Read(ref _armed) == 0)
            {
                return;
            }

            if (seriesId != _seriesId)
            {
                return;
            }

            if (observedVersion <= 0 || observedVersion != _expectedVersion)
            {
                throw new InvalidOperationException(
                    $"The hook requires observed Series version {_expectedVersion}, but received {observedVersion}.");
            }

            var arrival = Interlocked.Increment(ref _arrivals);
            if (arrival > 2)
            {
                throw new InvalidOperationException("The deterministic read barrier accepts exactly two operations.");
            }

            if (arrival == 2)
            {
                Volatile.Write(ref _released, 1);
                _release.TrySetResult();
            }

            try
            {
                await _release.Task.WaitAsync(_timeout, cancellationToken);
            }
            catch (TimeoutException exception)
            {
                throw new TimeoutException(
                    $"The second operation did not reach the Series read barrier within {_timeout}.",
                    exception);
            }
        }

        public void AssertReleasedPair()
        {
            ArrivalCount.Should().Be(2,
                "both competing operations must pause after reading the same Series version");
            Volatile.Read(ref _released).Should().Be(1,
                "the second valid arrival must release the pair before the timeout");
        }

        private static TaskCompletionSource NewCompletionSource() =>
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class MutableTimeProvider(DateTimeOffset initialUtc) : TimeProvider
    {
        private long _utcTicks = initialUtc.UtcTicks;

        public override DateTimeOffset GetUtcNow() =>
            new(Interlocked.Read(ref _utcTicks), TimeSpan.Zero);

        public void Advance(TimeSpan duration) => Interlocked.Add(ref _utcTicks, duration.Ticks);
    }

    private sealed record ApiError(string Message, string MessageCode);

    public interface IConcurrencyHookContractForTests
    {
        Task WaitAfterSeriesReadAsync(
            Guid seriesId,
            long observedVersion,
            CancellationToken cancellationToken);
    }

    private sealed record CreatedResource(Guid Id, string ETag);
    private sealed record MatchVersion(Guid MatchId, string ETag);
    private sealed record SeriesScenario(
        Guid SeriesId,
        Guid FirstMatchId,
        Guid SecondMatchId,
        Guid FirstSideId,
        Guid SecondSideId,
        string ETag,
        long Version);
}
