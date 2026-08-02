using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Infrastructure.Identity;
using RinhaDasLendas.Infrastructure.Messages;
using RinhaDasLendas.Infrastructure.Persistence;
using RinhaDasLendas.Tests.Fixtures;
using RinhaDasLendas.Tests.Infrastructure;

namespace RinhaDasLendas.Tests.Integration;

public sealed class CompetitiveSeasonApiTests : IAsyncLifetime
{
    private const string SeasonsRoute = "/api/v1/temporadas";
    private const string CompetitionsRoute = "/api/v1/competicoes";
    private const string Portuguese = "pt-BR";
    private const string English = "en-US";
    private static readonly ResourceMessageProvider Messages = new();
    private CompetitivePostgresFixture _database = null!;
    private CompetitiveSeasonApiFactory _factory = null!;

    public static TheoryData<string, string> SeasonalEndpointOperations => new()
    {
        { "GET", SeasonsRoute },
        { "POST", SeasonsRoute },
        { "GET", $"{SeasonsRoute}/{Guid.NewGuid()}" },
        { "PATCH", $"{SeasonsRoute}/{Guid.NewGuid()}" },
        { "POST", $"{SeasonsRoute}/{Guid.NewGuid()}/aberturas" },
        { "POST", $"{SeasonsRoute}/{Guid.NewGuid()}/encerramentos" },
        { "GET", $"{SeasonsRoute}/{Guid.NewGuid()}/competicoes" },
        { "POST", $"{SeasonsRoute}/{Guid.NewGuid()}/competicoes" },
        { "POST", $"{SeasonsRoute}/{Guid.NewGuid()}/regras-publicadas" },
        { "GET", CompetitionsRoute },
        { "GET", $"{CompetitionsRoute}/{Guid.NewGuid()}" },
        { "PATCH", $"{CompetitionsRoute}/{Guid.NewGuid()}" },
        { "GET", $"{CompetitionsRoute}/{Guid.NewGuid()}/rodadas" },
        { "POST", $"{CompetitionsRoute}/{Guid.NewGuid()}/rodadas" },
        { "POST", $"{CompetitionsRoute}/{Guid.NewGuid()}/ordenacoes-rodadas" },
        { "POST", $"{CompetitionsRoute}/{Guid.NewGuid()}/regras-publicadas" },
    };

    public static TheoryData<MutationKind> MutationOperations => new()
    {
        MutationKind.CreateSeason,
        MutationKind.UpdateSeason,
        MutationKind.ActivateSeason,
        MutationKind.CloseSeason,
        MutationKind.CreateCompetition,
        MutationKind.PublishSeasonRules,
        MutationKind.UpdateCompetition,
        MutationKind.CreateRound,
        MutationKind.ReorderRounds,
        MutationKind.PublishCompetitionRules,
    };

    public static TheoryData<ConditionalMutationKind> ConditionalMutationOperations => new()
    {
        ConditionalMutationKind.UpdateSeason,
        ConditionalMutationKind.ActivateSeason,
        ConditionalMutationKind.CloseSeason,
        ConditionalMutationKind.PublishSeasonRules,
        ConditionalMutationKind.UpdateCompetition,
        ConditionalMutationKind.CreateRound,
        ConditionalMutationKind.ReorderRounds,
        ConditionalMutationKind.PublishCompetitionRules,
    };

    public static TheoryData<ConditionalMutationKind> ConditionalHeaderValidationOperations => new()
    {
        ConditionalMutationKind.UpdateSeason,
        ConditionalMutationKind.ActivateSeason,
        ConditionalMutationKind.CloseSeason,
        ConditionalMutationKind.PublishSeasonRules,
        ConditionalMutationKind.UpdateCompetition,
        ConditionalMutationKind.CreateRound,
        ConditionalMutationKind.ReorderRounds,
        ConditionalMutationKind.PublishCompetitionRules,
    };

    public static TheoryData<MutationKind, string, string> ProtectedMutationOperations => new()
    {
        { MutationKind.CreateSeason, AuthRoles.SuperAdmin, AuthRoles.Admin },
        { MutationKind.UpdateSeason, AuthRoles.SuperAdmin, AuthRoles.Admin },
        { MutationKind.ActivateSeason, AuthRoles.SuperAdmin, AuthRoles.Admin },
        { MutationKind.CloseSeason, AuthRoles.SuperAdmin, AuthRoles.Admin },
        { MutationKind.CreateCompetition, AuthRoles.Admin, AuthRoles.SuperAdmin },
        { MutationKind.PublishSeasonRules, AuthRoles.Presidente, AuthRoles.SuperAdmin },
        { MutationKind.PublishSeasonRules, AuthRoles.Presidente, AuthRoles.VicePresidente },
        { MutationKind.PublishSeasonRules, AuthRoles.Presidente, AuthRoles.Admin },
        { MutationKind.PublishSeasonRules, AuthRoles.Presidente, AuthRoles.Moderador },
        { MutationKind.PublishSeasonRules, AuthRoles.Presidente, AuthRoles.Capitao },
        { MutationKind.PublishSeasonRules, AuthRoles.Presidente, AuthRoles.Jogador },
        { MutationKind.UpdateCompetition, AuthRoles.Admin, AuthRoles.SuperAdmin },
        { MutationKind.CreateRound, AuthRoles.Admin, AuthRoles.SuperAdmin },
        { MutationKind.ReorderRounds, AuthRoles.Admin, AuthRoles.SuperAdmin },
        { MutationKind.PublishCompetitionRules, AuthRoles.Presidente, AuthRoles.SuperAdmin },
        { MutationKind.PublishCompetitionRules, AuthRoles.Presidente, AuthRoles.VicePresidente },
        { MutationKind.PublishCompetitionRules, AuthRoles.Presidente, AuthRoles.Admin },
        { MutationKind.PublishCompetitionRules, AuthRoles.Presidente, AuthRoles.Moderador },
        { MutationKind.PublishCompetitionRules, AuthRoles.Presidente, AuthRoles.Capitao },
        { MutationKind.PublishCompetitionRules, AuthRoles.Presidente, AuthRoles.Jogador },
    };

    public static TheoryData<InvalidDtoKind> InvalidDtoOperations => new()
    {
        InvalidDtoKind.CreateSeason,
        InvalidDtoKind.UpdateSeason,
        InvalidDtoKind.Competition,
        InvalidDtoKind.Rules,
    };

    public async Task InitializeAsync()
    {
        _database = await CompetitivePostgresFixture.CreateAsync();
        _factory = new CompetitiveSeasonApiFactory(_database.ConnectionString);
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _database.DisposeAsync();
    }

    [Theory]
    [MemberData(nameof(SeasonalEndpointOperations))]
    public async Task EverySeasonalEndpoint_ShouldRequireAuthentication(string method, string route)
    {
        using var client = _factory.CreateAnonymousClient();
        using var request = Request(new HttpMethod(method), route, new { }, NewKey(), "W/\"0\"", Portuguese);

        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            $"{method} {route} is protected by the OpenAPI bearer security requirement");
        response.Headers.WwwAuthenticate.Should().ContainSingle(header => header.Scheme == "Bearer");
    }

    [Fact]
    public async Task OrdinaryAuthenticatedMember_ShouldReadEverySeasonCompetitionAndRoundProjection()
    {
        using var president = _factory.CreateClientFor(AuthRoles.Presidente);
        using var player = _factory.CreateClientFor(AuthRoles.Jogador);
        var season = await CreateSeasonAsync(president, "Temporada Leitura", 2030, 1);
        var competition = await CreateCompetitionAsync(president, season.Id, "Circuito Leitura", "LER", true);
        var roundId = await CreateRoundAsync(president, competition.Id, "Rodada Leitura", 1);

        var reads = new[]
        {
            SeasonsRoute,
            $"{SeasonsRoute}/{season.Id}",
            $"{SeasonsRoute}/{season.Id}/competicoes?page=1&pageSize=20",
            CompetitionsRoute,
            $"{CompetitionsRoute}/{competition.Id}",
            $"{CompetitionsRoute}/{competition.Id}/rodadas",
        };

        foreach (var route in reads)
        {
            using var response = await SendAsync(player, HttpMethod.Get, route, culture: English);
            response.StatusCode.Should().Be(HttpStatusCode.OK, $"ordinary authenticated members may read {route}");
            AssertJsonContent(response);
        }

        using var detailResponse = await SendAsync(player, HttpMethod.Get, $"{SeasonsRoute}/{season.Id}", culture: English);
        AssertSeasonDetail(await ReadJsonAsync(detailResponse), season.Id, "Temporada Leitura", 2030, "Planejada");
        RequireETag(detailResponse).Should().Be(season.ETag);

        using var competitionResponse = await SendAsync(
            player, HttpMethod.Get, $"{CompetitionsRoute}/{competition.Id}", culture: English);
        AssertCompetitionDetail(
            await ReadJsonAsync(competitionResponse), competition.Id, season.Id, "Circuito Leitura", "LER", true);
        RequireETag(competitionResponse).Should().NotBeNullOrWhiteSpace();

        using var roundsResponse = await SendAsync(
            player, HttpMethod.Get, $"{CompetitionsRoute}/{competition.Id}/rodadas", culture: English);
        var rounds = await ReadJsonAsync(roundsResponse);
        rounds.EnumerateArray().Should().ContainSingle();
        AssertRound(rounds[0], roundId, competition.Id, "Rodada Leitura", 1);
    }

    [Fact]
    public async Task SeasonalSelection_ShouldCoverNoActiveCurrentRepeatedSelectedIdsAllAndInvalidMixedScope()
    {
        using var client = _factory.CreateClientFor(AuthRoles.Presidente);
        var firstSeason = await CreateSeasonAsync(client, "Temporada Escopo Um", 2031, 1);
        var secondSeason = await CreateSeasonAsync(client, "Temporada Escopo Dois", 2032, 1);
        var firstCompetition = await CreateCompetitionAsync(client, firstSeason.Id, "Circuito Um", "C1", false);
        var secondCompetition = await CreateCompetitionAsync(client, secondSeason.Id, "Circuito Dois", "C2", false);

        using var noActiveResponse = await SendAsync(
            client, HttpMethod.Get, $"{CompetitionsRoute}?page=1&pageSize=20", culture: Portuguese);
        noActiveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        AssertCompetitionPage(
            await ReadJsonAsync(noActiveResponse), 1, 20, false, null, [], []);

        using var activationResponse = await SendAsync(
            client,
            HttpMethod.Post,
            $"{SeasonsRoute}/{firstSeason.Id}/aberturas",
            idempotencyKey: NewKey(),
            etag: "W/\"0\"",
            culture: Portuguese);
        activationResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var currentResponse = await SendAsync(
            client, HttpMethod.Get, $"{CompetitionsRoute}?page=1&pageSize=20", culture: Portuguese);
        currentResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        AssertCompetitionPage(
            await ReadJsonAsync(currentResponse),
            1,
            20,
            true,
            firstSeason.Id,
            [firstSeason.Id],
            [firstCompetition.Id]);

        using var selectedResponse = await SendAsync(
            client,
            HttpMethod.Get,
            $"{CompetitionsRoute}?temporadaIds={firstSeason.Id}&temporadaIds={secondSeason.Id}&page=1&pageSize=20",
            culture: English);
        selectedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        AssertCompetitionPage(
            await ReadJsonAsync(selectedResponse),
            1,
            20,
            true,
            firstSeason.Id,
            [firstSeason.Id, secondSeason.Id],
            [firstCompetition.Id, secondCompetition.Id]);

        using var allResponse = await SendAsync(
            client, HttpMethod.Get, $"{CompetitionsRoute}?todas=true&page=1&pageSize=20", culture: English);
        allResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        AssertCompetitionPage(
            await ReadJsonAsync(allResponse),
            1,
            20,
            true,
            firstSeason.Id,
            [firstSeason.Id, secondSeason.Id],
            [firstCompetition.Id, secondCompetition.Id]);

        using var mixedResponse = await SendAsync(
            client,
            HttpMethod.Get,
            $"{CompetitionsRoute}?temporadaIds={firstSeason.Id}&todas=true&page=1&pageSize=20",
            culture: Portuguese);
        await AssertLocalizedErrorAsync(mixedResponse, HttpStatusCode.BadRequest, Portuguese);
    }

    [Theory]
    [MemberData(nameof(MutationOperations))]
    public async Task EveryMutation_ShouldValidateKeyReplayAndDivergentContent(MutationKind kind)
    {
        using var client = _factory.CreateClientFor(AuthRoles.Presidente);
        var mutation = await PrepareMutationAsync(client, kind);

        if (DeclaresBadRequest(kind))
        {
            using var missingKey = await SendAsync(
                client, mutation.Method, mutation.Route, mutation.Body, etag: mutation.ETag, culture: Portuguese);
            await AssertLocalizedErrorAsync(missingKey, HttpStatusCode.BadRequest, Portuguese);

            using var invalidKey = await SendAsync(
                client,
                mutation.Method,
                mutation.Route,
                mutation.Body,
                "short",
                mutation.ETag,
                English);
            await AssertLocalizedErrorAsync(invalidKey, HttpStatusCode.BadRequest, English);
        }

        var key = NewKey();
        using var first = await SendAsync(
            client, mutation.Method, mutation.Route, mutation.Body, key, mutation.ETag, Portuguese);
        first.StatusCode.Should().Be(mutation.SuccessStatus);
        AssertMutationDto(kind, await ReadJsonAsync(first));
        var firstBytes = await first.Content.ReadAsByteArrayAsync();
        var firstETag = mutation.RequiresResponseETag
            ? RequireETag(first)
            : first.Headers.ETag?.ToString();
        first.Headers.Contains("Idempotency-Replayed").Should().BeFalse();

        using var replay = await SendAsync(
            client, mutation.Method, mutation.Route, mutation.Body, key, mutation.ETag, English);
        replay.StatusCode.Should().Be(first.StatusCode);
        (await replay.Content.ReadAsByteArrayAsync()).Should().Equal(firstBytes);
        if (mutation.RequiresResponseETag)
        {
            RequireETag(replay).Should().Be(firstETag);
        }
        else
        {
            replay.Headers.ETag?.ToString().Should().Be(firstETag);
        }
        replay.Headers.GetValues("Idempotency-Replayed").Should().ContainSingle().Which.Should().Be("true");

        if (mutation.DivergentBody is not null)
        {
            using var divergent = await SendAsync(
                client,
                mutation.Method,
                mutation.Route,
                mutation.DivergentBody,
                key,
                mutation.ETag,
                English);
            await AssertLocalizedErrorAsync(divergent, HttpStatusCode.Conflict, English);
        }
    }

    [Fact]
    public async Task IdempotencyNamespace_ShouldBeIsolatedByRouteAndActor()
    {
        var firstActorId = _factory.GetBootstrapActorId();
        var secondActorId = await _factory.CreateActorAsync("second-season-actor@example.com");
        using var firstActor = _factory.CreateClientFor(AuthRoles.Presidente, firstActorId);
        using var secondActor = _factory.CreateClientFor(AuthRoles.Presidente, secondActorId);
        var key = NewKey();

        var firstSeason = await CreateSeasonAsync(
            firstActor, "Temporada Ator Um", 2045, 1, idempotencyKey: key);
        var secondSeason = await CreateSeasonAsync(
            secondActor, "Temporada Ator Dois", 2046, 1, idempotencyKey: key);
        secondSeason.Id.Should().NotBe(firstSeason.Id,
            "the same method, route and key belongs to a separate authenticated actor namespace");

        using var otherRoute = await SendAsync(
            firstActor,
            HttpMethod.Post,
            $"{SeasonsRoute}/{firstSeason.Id}/competicoes",
            CompetitionBody("Mesmo token em outra rota", "OUTRA", false),
            key,
            culture: Portuguese);
        otherRoute.StatusCode.Should().Be(HttpStatusCode.Created,
            "normalized route is part of the idempotency namespace even when the HTTP method is the same");
        otherRoute.Headers.Contains("Idempotency-Replayed").Should().BeFalse();
    }

    [Fact]
    public async Task IdempotencyNamespace_ShouldBeIsolatedByConcreteResourcePath()
    {
        using var client = _factory.CreateClientFor(AuthRoles.Presidente);
        var first = await CreateSeasonAsync(client, "Temporada concreta um", 2047, 1);
        var second = await CreateSeasonAsync(client, "Temporada concreta dois", 2048, 1);
        var key = NewKey();

        using var firstUpdate = await SendAsync(
            client, HttpMethod.Patch, $"{SeasonsRoute}/{first.Id}", new { nome = "Mesmo nome" }, key, first.ETag);
        using var secondUpdate = await SendAsync(
            client, HttpMethod.Patch, $"{SeasonsRoute}/{second.Id}", new { nome = "Mesmo nome" }, key, second.ETag);

        firstUpdate.StatusCode.Should().Be(HttpStatusCode.OK);
        secondUpdate.StatusCode.Should().Be(HttpStatusCode.OK);
        secondUpdate.Headers.Contains("Idempotency-Replayed").Should().BeFalse();
        (await ReadJsonAsync(secondUpdate)).GetProperty("id").GetGuid().Should().Be(second.Id);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"nome\":null}")]
    [InlineData("{\"ano\":null}")]
    [InlineData("{\"ordemNoAno\":null}")]
    [InlineData("{\"dataInicio\":null}")]
    [InlineData("{\"dataFimExclusiva\":null}")]
    public async Task UpdateSeason_ShouldDistinguishOmittedPropertiesFromExplicitNull(string body)
    {
        using var client = _factory.CreateClientFor(AuthRoles.Presidente);
        var season = await CreateSeasonAsync(client, "Temporada optional", 2049, 1);

        using var response = await SendRawAsync(
            client, HttpMethod.Patch, $"{SeasonsRoute}/{season.Id}", body, NewKey(), season.ETag);

        await AssertLocalizedErrorAsync(response, HttpStatusCode.BadRequest, Portuguese);
    }

    [Theory]
    [InlineData("{\"nome\":\"Sem booleano\",\"codigo\":\"SB\"}")]
    [InlineData("{\"nome\":\"Booleano nulo\",\"codigo\":\"BN\",\"circuitoDiario\":null}")]
    public async Task CreateCompetition_ShouldRejectOmittedOrNullRequiredBoolean(string body)
    {
        using var client = _factory.CreateClientFor(AuthRoles.Presidente);
        var season = await CreateSeasonAsync(client, "Temporada booleano", 2050, 1);

        using var response = await SendRawAsync(
            client, HttpMethod.Post, $"{SeasonsRoute}/{season.Id}/competicoes", body, NewKey());

        await AssertLocalizedErrorAsync(response, HttpStatusCode.BadRequest, Portuguese);
    }

    [Theory]
    [InlineData("{\"modoDraft\":\"Padrao\"}")]
    [InlineData("{\"formato\":null,\"modoDraft\":\"Padrao\"}")]
    [InlineData("{\"formato\":\"Md3\"}")]
    [InlineData("{\"formato\":\"Md3\",\"modoDraft\":null}")]
    [InlineData("{\"formato\":0,\"modoDraft\":0}")]
    public async Task PublishRules_ShouldRejectOmittedNullOrNumericEnums(string body)
    {
        using var client = _factory.CreateClientFor(AuthRoles.Presidente);
        var season = await CreateSeasonAsync(client, "Temporada enum", 2051, 1);

        using var response = await SendRawAsync(
            client, HttpMethod.Post, $"{SeasonsRoute}/{season.Id}/regras-publicadas", body, NewKey(), season.ETag);

        await AssertLocalizedErrorAsync(response, HttpStatusCode.BadRequest, Portuguese);
    }

    [Fact]
    public async Task SeasonalFilters_ShouldRejectNumericEstadoAndDuplicateSeasonIds()
    {
        using var client = _factory.CreateClientFor(AuthRoles.Presidente);
        var season = await CreateSeasonAsync(client, "Temporada filtros", 2052, 1);

        using var numericState = await SendAsync(client, HttpMethod.Get, $"{SeasonsRoute}?estado=0");
        using var duplicateIds = await SendAsync(
            client, HttpMethod.Get, $"{CompetitionsRoute}?temporadaIds={season.Id}&temporadaIds={season.Id}");

        await AssertLocalizedErrorAsync(numericState, HttpStatusCode.BadRequest, Portuguese);
        await AssertLocalizedErrorAsync(duplicateIds, HttpStatusCode.BadRequest, Portuguese);
    }

    [Fact]
    public async Task ChildCreates_ShouldReturnCanonicalLocationsAndSeasonMutationsShouldReturnRealCompetitionCount()
    {
        using var client = _factory.CreateClientFor(AuthRoles.Presidente);
        var season = await CreateSeasonAsync(client, "Temporada filhos", 2053, 1);
        _ = await CreateCompetitionAsync(client, season.Id, "Competicao filha", "FILHA", false);

        using var update = await SendAsync(
            client, HttpMethod.Patch, $"{SeasonsRoute}/{season.Id}", new { nome = "Temporada filhos atualizada" },
            NewKey(), season.ETag);
        update.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadJsonAsync(update)).GetProperty("quantidadeCompeticoes").GetInt32().Should().Be(1);

        using var seasonRulesResponse = await SendAsync(
            client, HttpMethod.Post, $"{SeasonsRoute}/{season.Id}/regras-publicadas",
            RulesBody("Md3", "Padrao"), NewKey(), RequireETag(update));
        var seasonRulesId = (await ReadJsonAsync(seasonRulesResponse)).GetProperty("id").GetGuid();
        RequireLocationPath(seasonRulesResponse).Should().Be(
            $"{SeasonsRoute}/{season.Id}/regras-publicadas/{seasonRulesId}");

        var competition = await CreateCompetitionAsync(client, season.Id, "Competicao localizacao", "LOC", false);
        var competitionEtag = await GetCompetitionETagAsync(client, competition.Id);
        using var roundResponse = await SendAsync(
            client, HttpMethod.Post, $"{CompetitionsRoute}/{competition.Id}/rodadas",
            RoundBody("Rodada localizada", 1), NewKey(), competitionEtag);
        var roundId = (await ReadJsonAsync(roundResponse)).GetProperty("id").GetGuid();
        RequireLocationPath(roundResponse).Should().Be(
            $"{CompetitionsRoute}/{competition.Id}/rodadas/{roundId}");

        using var rulesResponse = await SendAsync(
            client, HttpMethod.Post, $"{CompetitionsRoute}/{competition.Id}/regras-publicadas",
            RulesBody("Md3", "Padrao"), NewKey(), RequireETag(roundResponse));
        var rulesId = (await ReadJsonAsync(rulesResponse)).GetProperty("id").GetGuid();
        RequireLocationPath(rulesResponse).Should().Be(
            $"{CompetitionsRoute}/{competition.Id}/regras-publicadas/{rulesId}");
    }

    [Fact]
    public async Task GeneratedSwagger_ShouldDescribeT030BearerAndRequestResponseHeaders()
    {
        using var client = _factory.CreateAnonymousClient();
        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = await ReadJsonAsync(response);

        document.GetProperty("components").GetProperty("securitySchemes")
            .GetProperty("Bearer").GetProperty("scheme").GetString().Should().Be("bearer");
        document.TryGetProperty("security", out _).Should().BeFalse();

        var paths = document.GetProperty("paths");
        AssertSwaggerSecurity(paths, "/api/v1/auth/login", "post", bearer: false);
        AssertSwaggerSecurity(paths, "/api/v1/auth/logout", "post", bearer: true);
        AssertSwaggerSecurity(paths, "/api/v1/discord/configuracoes", "get", bearer: true);
        AssertSwaggerSecurity(paths, "/api/v1/draft-montagens/{id}/discord/publicacao", "post", bearer: false);
        AssertSwaggerSecurity(paths, "/api/v1/temporadas", "get", bearer: true);

        var schemas = document.GetProperty("components").GetProperty("schemas");
        AssertRequestSchema(schemas, "CreateSeasonRequestDto",
            new Dictionary<string, string> { ["nome"] = "string", ["ano"] = "integer", ["ordemNoAno"] = "integer", ["dataInicio"] = "string", ["dataFimExclusiva"] = "string" },
            ["nome", "ano", "ordemNoAno", "dataInicio", "dataFimExclusiva"]);
        AssertRequestSchema(schemas, "UpdateSeasonRequestDto",
            new Dictionary<string, string> { ["nome"] = "string", ["ano"] = "integer", ["ordemNoAno"] = "integer", ["dataInicio"] = "string", ["dataFimExclusiva"] = "string" }, []);
        AssertRequestSchema(schemas, "CreateCompetitionRequestDto",
            new Dictionary<string, string> { ["nome"] = "string", ["codigo"] = "string", ["circuitoDiario"] = "boolean" },
            ["nome", "codigo", "circuitoDiario"]);
        AssertRequestSchema(schemas, "UpdateCompetitionRequestDto",
            new Dictionary<string, string> { ["nome"] = "string", ["codigo"] = "string", ["circuitoDiario"] = "boolean" }, []);
        AssertRequestSchema(schemas, "CreateRoundRequestDto",
            new Dictionary<string, string> { ["nome"] = "string", ["ordem"] = "integer" }, ["nome", "ordem"]);
        AssertRequestSchema(schemas, "ReorderRoundsRequestDto",
            new Dictionary<string, string> { ["rodadaIds"] = "array" }, ["rodadaIds"]);
        AssertRequestSchema(schemas, "PublishRulesRequestDto",
            new Dictionary<string, string> { ["formato"] = "string", ["modoDraft"] = "string" }, ["formato", "modoDraft"]);
        AssertSchemaEnum(schemas, "PublishRulesRequestDto", "formato", ["Md3", "Md5"]);
        AssertSchemaEnum(schemas, "PublishRulesRequestDto", "modoDraft", ["Padrao", "Fearless"]);
        var roundIds = schemas.GetProperty("ReorderRoundsRequestDto")
            .GetProperty("properties").GetProperty("rodadaIds");
        roundIds.GetProperty("items").GetProperty("format").GetString().Should().Be("uuid");
        var expected = new Dictionary<(string Path, string Method), SwaggerExpectation>
        {
            [("/api/v1/temporadas", "get")] = new([], ["ETag"], ["200", "400", "401"]),
            [("/api/v1/temporadas", "post")] = new(["Idempotency-Key"], ["ETag", "Location"], ["201", "400", "401", "403", "409"]),
            [("/api/v1/temporadas/{seasonId}", "get")] = new([], ["ETag"], ["200", "401", "404"]),
            [("/api/v1/temporadas/{seasonId}", "patch")] = new(["Idempotency-Key", "If-Match"], ["ETag"], ["200", "400", "401", "403", "404", "409"]),
            [("/api/v1/temporadas/{seasonId}/aberturas", "post")] = new(["Idempotency-Key", "If-Match-Calendar"], ["ETag"], ["200", "400", "401", "403", "404", "409"]),
            [("/api/v1/temporadas/{seasonId}/encerramentos", "post")] = new(["Idempotency-Key", "If-Match-Calendar"], ["ETag"], ["200", "400", "401", "403", "404", "409"]),
            [("/api/v1/temporadas/{seasonId}/competicoes", "get")] = new([], [], ["200", "400", "401", "404"]),
            [("/api/v1/temporadas/{seasonId}/competicoes", "post")] = new(["Idempotency-Key"], ["ETag", "Location"], ["201", "400", "401", "403", "404", "409"]),
            [("/api/v1/temporadas/{seasonId}/regras-publicadas", "post")] = new(["Idempotency-Key", "If-Match"], ["ETag", "Location"], ["201", "400", "401", "403", "404", "409"]),
            [("/api/v1/competicoes", "get")] = new([], [], ["200", "400", "401"]),
            [("/api/v1/competicoes/{competitionId}", "get")] = new([], ["ETag"], ["200", "401", "404"]),
            [("/api/v1/competicoes/{competitionId}", "patch")] = new(["Idempotency-Key", "If-Match"], ["ETag"], ["200", "400", "401", "403", "404", "409"]),
            [("/api/v1/competicoes/{competitionId}/rodadas", "get")] = new([], [], ["200", "401", "404"]),
            [("/api/v1/competicoes/{competitionId}/rodadas", "post")] = new(["Idempotency-Key", "If-Match"], ["ETag", "Location"], ["201", "400", "401", "403", "404", "409"]),
            [("/api/v1/competicoes/{competitionId}/ordenacoes-rodadas", "post")] = new(["Idempotency-Key", "If-Match"], ["ETag"], ["200", "400", "401", "403", "404", "409"]),
            [("/api/v1/competicoes/{competitionId}/regras-publicadas", "post")] = new(["Idempotency-Key", "If-Match"], ["ETag", "Location"], ["201", "400", "401", "403", "404", "409"]),
        };
        foreach (var operation in expected)
        {
            AssertSwaggerOperation(paths, operation.Key.Path, operation.Key.Method, operation.Value);
        }
    }

    [Theory]
    [MemberData(nameof(ProtectedMutationOperations))]
    public async Task ProtectedOperation_ShouldApplyEndpointCapabilityAndResourceCondition(
        MutationKind kind,
        string allowedRole,
        string deniedRole)
    {
        using var setupClient = _factory.CreateClientFor(AuthRoles.Presidente);
        using var deniedClient = _factory.CreateClientFor(deniedRole);
        using var allowedClient = _factory.CreateClientFor(allowedRole);
        var mutation = await PrepareMutationAsync(setupClient, kind);

        using var denied = await SendAsync(
            deniedClient,
            mutation.Method,
            mutation.Route,
            mutation.Body,
            NewKey(),
            mutation.ETag,
            English);
        await AssertLocalizedErrorAsync(denied, HttpStatusCode.Forbidden, English);

        using var allowed = await SendAsync(
            allowedClient,
            mutation.Method,
            mutation.Route,
            mutation.Body,
            NewKey(),
            mutation.ETag,
            Portuguese);
        allowed.StatusCode.Should().Be(mutation.SuccessStatus,
            $"{allowedRole} must be allowed for the {kind} endpoint resource condition");
    }

    [Fact]
    public async Task CompetitionConfiguration_ShouldEnforceResourceConditionAfterSeriesStarts()
    {
        using var president = _factory.CreateClientFor(AuthRoles.Presidente);
        using var admin = _factory.CreateClientFor(AuthRoles.Admin);
        var season = await CreateSeasonAsync(president, "Temporada com Serie", 2055, 1);
        var competition = await CreateCompetitionAsync(president, season.Id, "Competicao iniciada", "INI", false);
        var roundId = await CreateRoundAsync(president, competition.Id, "Rodada iniciada", 1);
        var competitionEtag = await GetCompetitionETagAsync(president, competition.Id);
        using var rulesResponse = await SendAsync(
            president, HttpMethod.Post, $"{CompetitionsRoute}/{competition.Id}/regras-publicadas",
            RulesBody("Md3", "Padrao"), NewKey(), competitionEtag);
        rulesResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var rulesId = (await ReadJsonAsync(rulesResponse)).GetProperty("id").GetGuid();
        await _factory.SeedStartedSeriesAsync(season.Id, competition.Id, roundId, rulesId);
        var currentEtag = RequireETag(rulesResponse);

        using var deniedUpdate = await SendAsync(
            admin, HttpMethod.Patch, $"{CompetitionsRoute}/{competition.Id}",
            new { nome = "Admin nao pode" }, NewKey(), currentEtag, English);
        await AssertLocalizedErrorAsync(deniedUpdate, HttpStatusCode.Forbidden, English);

        using var deniedCreate = await SendAsync(
            admin, HttpMethod.Post, $"{SeasonsRoute}/{season.Id}/competicoes",
            CompetitionBody("Outra competicao", "OUT", false), NewKey(), culture: Portuguese);
        await AssertLocalizedErrorAsync(deniedCreate, HttpStatusCode.Forbidden, Portuguese);

        using var presidentUpdate = await SendAsync(
            president, HttpMethod.Patch, $"{CompetitionsRoute}/{competition.Id}",
            new { nome = "Presidente pode" }, NewKey(), currentEtag);
        presidentUpdate.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [MemberData(nameof(ConditionalMutationOperations))]
    public async Task ConditionalMutation_ShouldRejectStaleIfMatch(ConditionalMutationKind kind)
    {
        using var client = _factory.CreateClientFor(AuthRoles.Presidente);
        var scenario = await PrepareConditionalMutationAsync(client, kind);

        using var accepted = await SendAsync(
            client,
            scenario.Method,
            scenario.Route,
            scenario.Body,
            NewKey(),
            scenario.ETag,
            Portuguese);
        accepted.StatusCode.Should().Be(scenario.SuccessStatus);
        if (kind == ConditionalMutationKind.CloseSeason)
        {
            (await ReadJsonAsync(accepted)).GetProperty("estado").GetString().Should().Be("Ativa",
                "the stale closure precondition starts with the target Season active");
        }

        var staleRequest = await scenario.CreateStaleRequestAsync();
        using var stale = await SendAsync(
            client,
            staleRequest.Method,
            staleRequest.Route,
            staleRequest.Body,
            NewKey(),
            scenario.ETag,
            Portuguese);
        await AssertLocalizedErrorAsync(stale, HttpStatusCode.Conflict, Portuguese);
        if (kind == ConditionalMutationKind.CloseSeason)
        {
            var detailRoute = staleRequest.Route[..^"/encerramentos".Length];
            using var detail = await SendAsync(client, HttpMethod.Get, detailRoute, culture: Portuguese);
            detail.StatusCode.Should().Be(HttpStatusCode.OK);
            (await ReadJsonAsync(detail)).GetProperty("estado").GetString().Should().Be("Ativa",
                "a stale ETag must reject the first closure without changing the active Season");
        }
    }

    [Theory]
    [MemberData(nameof(ConditionalHeaderValidationOperations))]
    public async Task ConditionalMutationDeclaring400_ShouldRejectAbsentAndMalformedIfMatch(
        ConditionalMutationKind kind)
    {
        using var client = _factory.CreateClientFor(AuthRoles.Presidente);
        var scenario = await PrepareConditionalMutationAsync(client, kind);

        using var absent = await SendAsync(
            client,
            scenario.Method,
            scenario.Route,
            scenario.Body,
            NewKey(),
            culture: Portuguese);
        await AssertLocalizedErrorAsync(absent, HttpStatusCode.BadRequest, Portuguese);

        using var malformed = await SendAsync(
            client,
            scenario.Method,
            scenario.Route,
            scenario.Body,
            NewKey(),
            "not-an-etag",
            English);
        await AssertLocalizedErrorAsync(malformed, HttpStatusCode.BadRequest, English);
    }

    [Fact]
    public async Task ConditionalMutations_ShouldNotInterchangeResourceAndCalendarEtags()
    {
        using var client = _factory.CreateClientFor(AuthRoles.Presidente);
        var season = await CreateSeasonAsync(client, "Season escopos ETag", 2054, 1);
        using var update = await SendAsync(
            client, HttpMethod.Patch, $"{SeasonsRoute}/{season.Id}", new { nome = "Season versao um" },
            NewKey(), season.ETag);
        update.StatusCode.Should().Be(HttpStatusCode.OK);
        var resourceEtag = RequireETag(update);

        using var activationWithResource = await SendWithPreconditionHeaderAsync(
            client, HttpMethod.Post, $"{SeasonsRoute}/{season.Id}/aberturas", null,
            NewKey(), "If-Match-Calendar", resourceEtag);
        await AssertLocalizedErrorAsync(activationWithResource, HttpStatusCode.Conflict, Portuguese);

        using var rulesWithCalendar = await SendWithPreconditionHeaderAsync(
            client, HttpMethod.Post, $"{SeasonsRoute}/{season.Id}/regras-publicadas",
            RulesBody("Md3", "Padrao"), NewKey(), "If-Match", "W/\"0\"");
        await AssertLocalizedErrorAsync(rulesWithCalendar, HttpStatusCode.Conflict, Portuguese);
    }

    [Fact]
    public async Task NegativeResponses_ShouldUseContractStatusBodiesAndRequestedLocalization()
    {
        using var client = _factory.CreateClientFor(AuthRoles.Presidente);

        using var invalidPt = await SendAsync(
            client, HttpMethod.Post, SeasonsRoute, SeasonBody("", 2008, 0), NewKey(), culture: Portuguese);
        var ptError = await AssertLocalizedErrorAsync(invalidPt, HttpStatusCode.BadRequest, Portuguese);
        ptError.GetProperty("errors").GetArrayLength().Should().BeGreaterThan(0);

        using var invalidEn = await SendAsync(
            client, HttpMethod.Post, SeasonsRoute, SeasonBody("", 2008, 0), NewKey(), culture: English);
        var enError = await AssertLocalizedErrorAsync(invalidEn, HttpStatusCode.BadRequest, English);
        enError.GetProperty("messageCode").GetString().Should().Be(ptError.GetProperty("messageCode").GetString());
        enError.GetProperty("message").GetString().Should().NotBe(ptError.GetProperty("message").GetString());

        using var malformedJson = await SendRawAsync(
            client, HttpMethod.Post, SeasonsRoute, "{", NewKey(), culture: English);
        await AssertLocalizedErrorAsync(malformedJson, HttpStatusCode.BadRequest, English);

        foreach (var route in new[]
        {
            $"{SeasonsRoute}/{Guid.NewGuid()}",
            $"{CompetitionsRoute}/{Guid.NewGuid()}",
        })
        {
            using var missing = await SendAsync(client, HttpMethod.Get, route, culture: Portuguese);
            await AssertLocalizedErrorAsync(missing, HttpStatusCode.NotFound, Portuguese);
        }
    }

    [Theory]
    [MemberData(nameof(InvalidDtoOperations))]
    public async Task MutationDto_ShouldRejectInvalidShapeAndAdditionalProperties(InvalidDtoKind kind)
    {
        using var client = _factory.CreateClientFor(AuthRoles.Presidente);
        var scenario = await PrepareInvalidDtoAsync(client, kind);

        using var invalid = await SendAsync(
            client,
            scenario.Method,
            scenario.Route,
            scenario.InvalidBody,
            NewKey(),
            scenario.ETag,
            Portuguese);
        await AssertLocalizedErrorAsync(invalid, HttpStatusCode.BadRequest, Portuguese);

        using var additionalProperty = await SendAsync(
            client,
            scenario.Method,
            scenario.Route,
            scenario.AdditionalPropertyBody,
            NewKey(),
            scenario.ETag,
            English);
        await AssertLocalizedErrorAsync(additionalProperty, HttpStatusCode.BadRequest, English);
    }

    [Fact]
    public async Task SuccessResponses_ShouldExposeCompleteDtoEnvelopesPaginationAndVersionHeaders()
    {
        using var client = _factory.CreateClientFor(AuthRoles.Presidente);
        var firstSeason = await CreateSeasonAsync(client, "Temporada DTO Um", 2036, 1);
        _ = await CreateSeasonAsync(client, "Temporada DTO Dois", 2037, 1);
        var competition = await CreateCompetitionAsync(client, firstSeason.Id, "Competicao DTO", "DTO", true);
        _ = await CreateCompetitionAsync(client, firstSeason.Id, "Competicao DTO Dois", "DTO2", false);
        var round = await CreateRoundAsync(client, competition.Id, "Rodada DTO", 1);

        using var seasonsResponse = await SendAsync(
            client, HttpMethod.Get, $"{SeasonsRoute}?page=2&pageSize=1", culture: Portuguese);
        seasonsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var calendarETag = RequireETag(seasonsResponse);
        var seasons = await ReadJsonAsync(seasonsResponse);
        AssertPageEnvelope(seasons, 2, 1, 2, 2);
        seasons.GetProperty("calendarioConfigurado").ValueKind.Should().Be(JsonValueKind.False);
        seasons.GetProperty("temporadaAtual").ValueKind.Should().Be(JsonValueKind.Null);
        seasons.GetProperty("versaoCalendario").GetInt64().Should().BeGreaterThanOrEqualTo(0);
        calendarETag.Should().Be($"W/\"{seasons.GetProperty("versaoCalendario").GetInt64()}\"");
        AssertSeasonSummary(seasons.GetProperty("items")[0]);

        using var filteredSeasonsResponse = await SendAsync(
            client,
            HttpMethod.Get,
            $"{SeasonsRoute}?estado=Planejada&page=1&pageSize=20",
            culture: English);
        filteredSeasonsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var filteredSeasons = await ReadJsonAsync(filteredSeasonsResponse);
        AssertPageEnvelope(filteredSeasons, 1, 20, 2, 1);
        filteredSeasons.GetProperty("items").EnumerateArray().Should()
            .OnlyContain(item => item.GetProperty("estado").GetString() == "Planejada");

        using var competitionsPageResponse = await SendAsync(
            client,
            HttpMethod.Get,
            $"{CompetitionsRoute}?temporadaIds={firstSeason.Id}&page=2&pageSize=1",
            culture: Portuguese);
        competitionsPageResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var competitionsPage = await ReadJsonAsync(competitionsPageResponse);
        AssertPageEnvelope(competitionsPage, 2, 1, 2, 2);
        competitionsPage.GetProperty("items").EnumerateArray().Should().ContainSingle();
        competitionsPage.GetProperty("items")[0].GetProperty("seasonId").GetGuid().Should().Be(firstSeason.Id);

        using var seasonCompetitionsPageResponse = await SendAsync(
            client,
            HttpMethod.Get,
            $"{SeasonsRoute}/{firstSeason.Id}/competicoes?page=2&pageSize=1",
            culture: English);
        seasonCompetitionsPageResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var seasonCompetitionsPage = await ReadJsonAsync(seasonCompetitionsPageResponse);
        AssertPageEnvelope(seasonCompetitionsPage, 2, 1, 2, 2);
        seasonCompetitionsPage.GetProperty("items").EnumerateArray().Should().ContainSingle();
        seasonCompetitionsPage.GetProperty("items")[0].GetProperty("seasonId").GetGuid().Should().Be(firstSeason.Id);

        using var seasonResponse = await SendAsync(
            client, HttpMethod.Get, $"{SeasonsRoute}/{firstSeason.Id}", culture: Portuguese);
        AssertSeasonDetail(await ReadJsonAsync(seasonResponse), firstSeason.Id, "Temporada DTO Um", 2036, "Planejada");
        RequireETag(seasonResponse).Should().Be(firstSeason.ETag);

        using var competitionResponse = await SendAsync(
            client, HttpMethod.Get, $"{CompetitionsRoute}/{competition.Id}", culture: Portuguese);
        var competitionBody = await ReadJsonAsync(competitionResponse);
        AssertCompetitionDetail(
            competitionBody, competition.Id, firstSeason.Id, "Competicao DTO", "DTO", true);
        competitionBody.GetProperty("rodadas").EnumerateArray().Should().ContainSingle();
        AssertRound(competitionBody.GetProperty("rodadas")[0], round, competition.Id, "Rodada DTO", 1);
        RequireETag(competitionResponse).Should().NotBeNullOrWhiteSpace();
    }

    private async Task<MutationScenario> PrepareMutationAsync(HttpClient client, MutationKind kind)
    {
        var year = 2060 + (int)kind;
        switch (kind)
        {
            case MutationKind.CreateSeason:
                return new(HttpMethod.Post, SeasonsRoute, SeasonBody("Temporada Idempotente", year, 1),
                    SeasonBody("Temporada Divergente", year, 1), null, HttpStatusCode.Created, true);
            case MutationKind.UpdateSeason:
            {
                var season = await CreateSeasonAsync(client, "Temporada para editar", year, 1);
                return new(HttpMethod.Patch, $"{SeasonsRoute}/{season.Id}", new { nome = "Nome editado" },
                    new { nome = "Nome divergente" }, season.ETag, HttpStatusCode.OK, true);
            }
            case MutationKind.ActivateSeason:
            {
                var season = await CreateSeasonAsync(client, "Temporada para ativar", year, 1);
                return new(HttpMethod.Post, $"{SeasonsRoute}/{season.Id}/aberturas", null, null,
                    "W/\"0\"", HttpStatusCode.OK, true);
            }
            case MutationKind.CloseSeason:
            {
                var season = await CreateSeasonAsync(client, "Temporada para encerrar", year, 1);
                using var activation = await SendAsync(
                    client, HttpMethod.Post, $"{SeasonsRoute}/{season.Id}/aberturas",
                    idempotencyKey: NewKey(), etag: "W/\"0\"", culture: Portuguese);
                activation.StatusCode.Should().Be(HttpStatusCode.OK);
                return new(HttpMethod.Post, $"{SeasonsRoute}/{season.Id}/encerramentos", null, null,
                    RequireETag(activation), HttpStatusCode.OK, true);
            }
            case MutationKind.CreateCompetition:
            {
                var season = await CreateSeasonAsync(client, "Temporada da competicao", year, 1);
                return new(HttpMethod.Post, $"{SeasonsRoute}/{season.Id}/competicoes",
                    CompetitionBody("Competicao Idempotente", $"I{(int)kind}", false),
                    CompetitionBody("Competicao Divergente", $"D{(int)kind}", false), null,
                    HttpStatusCode.Created, true);
            }
            case MutationKind.PublishSeasonRules:
            {
                var season = await CreateSeasonAsync(client, "Temporada da regra", year, 1);
                return new(HttpMethod.Post, $"{SeasonsRoute}/{season.Id}/regras-publicadas",
                    RulesBody("Md3", "Padrao"), RulesBody("Md5", "Fearless"), season.ETag,
                    HttpStatusCode.Created, true);
            }
            case MutationKind.UpdateCompetition:
            {
                var (_, competition) = await CreateSeasonAndCompetitionAsync(client, year);
                return new(HttpMethod.Patch, $"{CompetitionsRoute}/{competition.Id}",
                    new { nome = "Competicao editada" }, new { nome = "Competicao divergente" },
                    competition.ETag, HttpStatusCode.OK, true);
            }
            case MutationKind.CreateRound:
            {
                var (_, competition) = await CreateSeasonAndCompetitionAsync(client, year);
                return new(HttpMethod.Post, $"{CompetitionsRoute}/{competition.Id}/rodadas",
                    RoundBody("Rodada Idempotente", 1), RoundBody("Rodada Divergente", 2),
                    competition.ETag, HttpStatusCode.Created, true);
            }
            case MutationKind.ReorderRounds:
            {
                var (_, competition) = await CreateSeasonAndCompetitionAsync(client, year);
                var first = await CreateRoundAsync(client, competition.Id, "Rodada Um", 1);
                var second = await CreateRoundAsync(client, competition.Id, "Rodada Dois", 2);
                var etag = await GetCompetitionETagAsync(client, competition.Id);
                return new(HttpMethod.Post, $"{CompetitionsRoute}/{competition.Id}/ordenacoes-rodadas",
                    new { rodadaIds = new[] { second, first } }, new { rodadaIds = new[] { first, second } },
                    etag, HttpStatusCode.OK, true);
            }
            case MutationKind.PublishCompetitionRules:
            {
                var (_, competition) = await CreateSeasonAndCompetitionAsync(client, year);
                return new(HttpMethod.Post, $"{CompetitionsRoute}/{competition.Id}/regras-publicadas",
                    RulesBody("Md3", "Padrao"), RulesBody("Md5", "Fearless"),
                    competition.ETag, HttpStatusCode.Created, true);
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }

    private async Task<ConditionalMutationScenario> PrepareConditionalMutationAsync(
        HttpClient client,
        ConditionalMutationKind kind)
    {
        var year = 2080 + (int)kind;
        switch (kind)
        {
            case ConditionalMutationKind.UpdateSeason:
            {
                var season = await CreateSeasonAsync(client, "Season condicional", year, 1);
                return Conditional(
                    HttpMethod.Patch,
                    $"{SeasonsRoute}/{season.Id}",
                    new { nome = "Season atualizada" },
                    season.ETag,
                    HttpStatusCode.OK);
            }
            case ConditionalMutationKind.ActivateSeason:
            {
                var first = await CreateSeasonAsync(client, "Season ativada", year, 1);
                var second = await CreateSeasonAsync(client, "Season stale", year + 1, 1);
                return Conditional(
                    HttpMethod.Post,
                    $"{SeasonsRoute}/{first.Id}/aberturas",
                    null,
                    "W/\"0\"",
                    HttpStatusCode.OK,
                    () => Task.FromResult(Mutation(
                        HttpMethod.Post,
                        $"{SeasonsRoute}/{second.Id}/aberturas",
                        null)));
            }
            case ConditionalMutationKind.CloseSeason:
            {
                var first = await CreateSeasonAsync(client, "Season para fechar", year, 1);
                var second = await CreateSeasonAsync(client, "Season ativa com ETag stale", year + 1, 1);
                using var firstActivation = await SendAsync(
                    client, HttpMethod.Post, $"{SeasonsRoute}/{first.Id}/aberturas",
                    idempotencyKey: NewKey(), etag: "W/\"0\"", culture: Portuguese);
                firstActivation.StatusCode.Should().Be(HttpStatusCode.OK);
                var staleCalendarETag = RequireETag(firstActivation);
                return Conditional(
                    HttpMethod.Post,
                    $"{SeasonsRoute}/{second.Id}/aberturas",
                    null,
                    staleCalendarETag,
                    HttpStatusCode.OK,
                    () => Task.FromResult(Mutation(
                        HttpMethod.Post,
                        $"{SeasonsRoute}/{second.Id}/encerramentos",
                        null)));
            }
            case ConditionalMutationKind.PublishSeasonRules:
            {
                var season = await CreateSeasonAsync(client, "Season regra condicional", year, 1);
                return Conditional(
                    HttpMethod.Post,
                    $"{SeasonsRoute}/{season.Id}/regras-publicadas",
                    RulesBody("Md3", "Padrao"),
                    season.ETag,
                    HttpStatusCode.Created);
            }
            case ConditionalMutationKind.UpdateCompetition:
            {
                var (_, competition) = await CreateSeasonAndCompetitionAsync(client, year);
                return Conditional(
                    HttpMethod.Patch,
                    $"{CompetitionsRoute}/{competition.Id}",
                    new { nome = "Competicao condicional" },
                    competition.ETag,
                    HttpStatusCode.OK);
            }
            case ConditionalMutationKind.CreateRound:
            {
                var (_, competition) = await CreateSeasonAndCompetitionAsync(client, year);
                return Conditional(
                    HttpMethod.Post,
                    $"{CompetitionsRoute}/{competition.Id}/rodadas",
                    RoundBody("Rodada condicional", 1),
                    competition.ETag,
                    HttpStatusCode.Created,
                    () => Task.FromResult(Mutation(
                        HttpMethod.Post,
                        $"{CompetitionsRoute}/{competition.Id}/rodadas",
                        RoundBody("Rodada stale", 2))));
            }
            case ConditionalMutationKind.ReorderRounds:
            {
                var (_, competition) = await CreateSeasonAndCompetitionAsync(client, year);
                var first = await CreateRoundAsync(client, competition.Id, "Rodada Um", 1);
                var second = await CreateRoundAsync(client, competition.Id, "Rodada Dois", 2);
                return Conditional(
                    HttpMethod.Post,
                    $"{CompetitionsRoute}/{competition.Id}/ordenacoes-rodadas",
                    new { rodadaIds = new[] { second, first } },
                    await GetCompetitionETagAsync(client, competition.Id),
                    HttpStatusCode.OK);
            }
            case ConditionalMutationKind.PublishCompetitionRules:
            {
                var (_, competition) = await CreateSeasonAndCompetitionAsync(client, year);
                return Conditional(
                    HttpMethod.Post,
                    $"{CompetitionsRoute}/{competition.Id}/regras-publicadas",
                    RulesBody("Md3", "Padrao"),
                    competition.ETag,
                    HttpStatusCode.Created);
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }

    private async Task<InvalidDtoScenario> PrepareInvalidDtoAsync(HttpClient client, InvalidDtoKind kind)
    {
        var year = 2090 + (int)kind;
        switch (kind)
        {
            case InvalidDtoKind.CreateSeason:
                return new(
                    HttpMethod.Post,
                    SeasonsRoute,
                    new
                    {
                        nome = "",
                        ano = 2008,
                        ordemNoAno = 0,
                        dataInicio = "2090-01-01",
                        dataFimExclusiva = "2090-01-01",
                    },
                    new
                    {
                        nome = "Season extra",
                        ano = year,
                        ordemNoAno = 1,
                        dataInicio = $"{year}-01-01",
                        dataFimExclusiva = $"{year + 1}-01-01",
                        inesperado = true,
                    },
                    null);
            case InvalidDtoKind.UpdateSeason:
            {
                var season = await CreateSeasonAsync(client, "Season DTO update", year, 1);
                return new(
                    HttpMethod.Patch,
                    $"{SeasonsRoute}/{season.Id}",
                    new { },
                    new { nome = "Season extra", inesperado = true },
                    season.ETag);
            }
            case InvalidDtoKind.Competition:
            {
                var season = await CreateSeasonAsync(client, "Season DTO competicao", year, 1);
                return new(
                    HttpMethod.Post,
                    $"{SeasonsRoute}/{season.Id}/competicoes",
                    new { nome = "", circuitoDiario = false },
                    new { nome = "Competicao extra", codigo = "EXT", circuitoDiario = false, inesperado = true },
                    null);
            }
            case InvalidDtoKind.Rules:
            {
                var season = await CreateSeasonAsync(client, "Season DTO regras", year, 1);
                return new(
                    HttpMethod.Post,
                    $"{SeasonsRoute}/{season.Id}/regras-publicadas",
                    new { formato = "Md7", modoDraft = "Desconhecido" },
                    new { formato = "Md3", modoDraft = "Padrao", inesperado = true },
                    season.ETag);
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }

    private static bool DeclaresBadRequest(MutationKind kind) => kind is
        MutationKind.CreateSeason
        or MutationKind.UpdateSeason
        or MutationKind.CreateCompetition
        or MutationKind.PublishSeasonRules
        or MutationKind.PublishCompetitionRules;

    private static ConditionalMutationScenario Conditional(
        HttpMethod method,
        string route,
        object? body,
        string etag,
        HttpStatusCode successStatus,
        Func<Task<MutationRequest>>? staleFactory = null) =>
        new(
            method,
            route,
            body,
            etag,
            successStatus,
            staleFactory ?? (() => Task.FromResult(Mutation(method, route, body))));

    private static MutationRequest Mutation(HttpMethod method, string route, object? body) => new(method, route, body);

    private static async Task<CreatedResource> CreateSeasonAsync(
        HttpClient client,
        string name,
        int year,
        int order,
        string? idempotencyKey = null)
    {
        using var response = await SendAsync(
            client,
            HttpMethod.Post,
            SeasonsRoute,
            SeasonBody(name, year, order),
            idempotencyKey ?? NewKey(),
            culture: Portuguese);
        response.StatusCode.Should().Be(HttpStatusCode.Created, "the scenario requires a created Season");
        var body = await ReadJsonAsync(response);
        var resource = new CreatedResource(body.GetProperty("id").GetGuid(), RequireETag(response), body);
        RequireLocationPath(response).Should().Be($"{SeasonsRoute}/{resource.Id}");
        AssertSeasonDetail(body, resource.Id, name, year, "Planejada");
        return resource;
    }

    private static async Task<CreatedResource> CreateCompetitionAsync(
        HttpClient client,
        Guid seasonId,
        string name,
        string code,
        bool dailyCircuit)
    {
        using var response = await SendAsync(
            client,
            HttpMethod.Post,
            $"{SeasonsRoute}/{seasonId}/competicoes",
            CompetitionBody(name, code, dailyCircuit),
            NewKey(),
            culture: Portuguese);
        response.StatusCode.Should().Be(HttpStatusCode.Created, "the scenario requires a created competition");
        var body = await ReadJsonAsync(response);
        var resource = new CreatedResource(body.GetProperty("id").GetGuid(), RequireETag(response), body);
        RequireLocationPath(response).Should().Be($"{CompetitionsRoute}/{resource.Id}");
        AssertCompetitionDetail(body, resource.Id, seasonId, name, code, dailyCircuit);
        return resource;
    }

    private static async Task<(CreatedResource Season, CreatedResource Competition)> CreateSeasonAndCompetitionAsync(
        HttpClient client,
        int year)
    {
        var season = await CreateSeasonAsync(client, $"Temporada {year}", year, 1);
        var competition = await CreateCompetitionAsync(client, season.Id, $"Competicao {year}", $"C{year}", false);
        return (season, competition);
    }

    private static async Task<Guid> CreateRoundAsync(HttpClient client, Guid competitionId, string name, int order)
    {
        var etag = await GetCompetitionETagAsync(client, competitionId);
        using var response = await SendAsync(
            client,
            HttpMethod.Post,
            $"{CompetitionsRoute}/{competitionId}/rodadas",
            RoundBody(name, order),
            NewKey(),
            etag,
            culture: Portuguese);
        response.StatusCode.Should().Be(HttpStatusCode.Created, "the scenario requires a created round");
        RequireETag(response).Should().NotBe(etag);
        var body = await ReadJsonAsync(response);
        var id = body.GetProperty("id").GetGuid();
        AssertRound(body, id, competitionId, name, order);
        return id;
    }

    private static async Task<string> GetCompetitionETagAsync(HttpClient client, Guid competitionId)
    {
        using var response = await SendAsync(
            client, HttpMethod.Get, $"{CompetitionsRoute}/{competitionId}", culture: Portuguese);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return RequireETag(response);
    }

    private static object SeasonBody(string name, int year, int order) => new
    {
        nome = name,
        ano = year,
        ordemNoAno = order,
        dataInicio = $"{year}-01-01",
        dataFimExclusiva = $"{year + 1}-01-01",
    };

    private static object CompetitionBody(string name, string code, bool dailyCircuit) => new
    {
        nome = name,
        codigo = code,
        circuitoDiario = dailyCircuit,
    };

    private static object RoundBody(string name, int order) => new { nome = name, ordem = order };

    private static object RulesBody(string format, string draftMode) => new
    {
        formato = format,
        modoDraft = draftMode,
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
            request.Headers.TryAddWithoutValidation(
                IsCalendarMutation(route) ? "If-Match-Calendar" : "If-Match",
                etag);
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

    private static async Task<HttpResponseMessage> SendRawAsync(
        HttpClient client,
        HttpMethod method,
        string route,
        string body,
        string? idempotencyKey = null,
        string? etag = null,
        string culture = Portuguese)
    {
        using var request = new HttpRequestMessage(method, route);
        request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(culture));
        if (idempotencyKey is not null)
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        if (etag is not null)
        {
            request.Headers.TryAddWithoutValidation(
                IsCalendarMutation(route) ? "If-Match-Calendar" : "If-Match",
                etag);
        }

        request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> SendWithPreconditionHeaderAsync(
        HttpClient client,
        HttpMethod method,
        string route,
        object? body,
        string idempotencyKey,
        string header,
        string etag)
    {
        using var request = Request(method, route, body, idempotencyKey, etag: null, Portuguese);
        request.Headers.TryAddWithoutValidation(header, etag);
        return await client.SendAsync(request);
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        AssertJsonContent(response);
        var text = await response.Content.ReadAsStringAsync();
        text.Should().NotBeNullOrWhiteSpace("the OpenAPI response declares a JSON body");
        using var document = JsonDocument.Parse(text);
        return document.RootElement.Clone();
    }

    private static void AssertJsonContent(HttpResponseMessage response) =>
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

    private static async Task<JsonElement> AssertLocalizedErrorAsync(
        HttpResponseMessage response,
        HttpStatusCode status,
        string culture)
    {
        response.StatusCode.Should().Be(status);
        var error = await ReadJsonAsync(response);
        error.ValueKind.Should().Be(JsonValueKind.Object);
        var messageCode = error.GetProperty("messageCode").GetString();
        messageCode.Should().NotBeNullOrWhiteSpace();
        var message = error.GetProperty("message").GetString();
        message.Should().NotBeNullOrWhiteSpace();
        message.Should().Be(Messages.GetMessage(messageCode!, culture));
        message.Should().NotBe($"[{messageCode}]", "the requested culture must resolve a real resource");
        error.GetProperty("errors").ValueKind.Should().Be(JsonValueKind.Array);
        return error;
    }

    private static string RequireETag(HttpResponseMessage response)
    {
        var etag = response.Headers.ETag?.ToString();
        etag.Should().NotBeNullOrWhiteSpace("versioned resources declare ETag in OpenAPI");
        etag.Should().MatchRegex("^W/\\\"[0-9]+\\\"$");
        return etag!;
    }

    private static string RequireLocationPath(HttpResponseMessage response)
    {
        var location = response.Headers.Location;
        location.Should().NotBeNull("created resources must expose their canonical URI");
        return location!.IsAbsoluteUri ? location.AbsolutePath : location.OriginalString;
    }

    private static void AssertSwaggerOperation(
        JsonElement paths,
        string path,
        string method,
        SwaggerExpectation expected)
    {
        var operation = paths.GetProperty(path).GetProperty(method);
        var documentedRequestHeaders = operation.GetProperty("parameters").EnumerateArray()
            .Where(parameter => parameter.GetProperty("in").GetString() == "header")
            .Select(parameter => parameter.GetProperty("name").GetString())
            .ToArray();
        if (expected.RequestHeaders.Count > 0)
        {
            documentedRequestHeaders.Should().Contain(expected.RequestHeaders);
        }

        var responses = operation.GetProperty("responses");
        responses.EnumerateObject().Select(response => response.Name).Should()
            .BeEquivalentTo(expected.Statuses);
        var successResponse = responses.EnumerateObject()
            .Single(response => response.Name.StartsWith('2')).Value;
        var documentedResponseHeaders = successResponse.TryGetProperty("headers", out var headers)
            ? headers.EnumerateObject().Select(header => header.Name).ToArray()
            : [];
        if (expected.ResponseHeaders.Count > 0)
        {
            documentedResponseHeaders.Should().Contain(expected.ResponseHeaders);
        }
    }

    private static void AssertSwaggerSecurity(
        JsonElement paths,
        string path,
        string method,
        bool bearer)
    {
        var operation = paths.GetProperty(path).GetProperty(method);
        var hasBearer = operation.TryGetProperty("security", out var security)
            && security.EnumerateArray().Any(requirement => requirement.TryGetProperty("Bearer", out _));
        hasBearer.Should().Be(bearer, operation.GetRawText());
    }

    private static void AssertRequestSchema(
        JsonElement schemas,
        string name,
        IReadOnlyDictionary<string, string> properties,
        IReadOnlyCollection<string> required)
    {
        var schema = schemas.GetProperty(name);
        schema.GetProperty("additionalProperties").GetBoolean().Should().BeFalse();
        var requiredProperties = schema.TryGetProperty("required", out var requiredNode)
            ? requiredNode.EnumerateArray().Select(item => item.GetString()).ToArray()
            : [];
        requiredProperties.Should().BeEquivalentTo(required);
        foreach (var property in properties)
        {
            var propertySchema = ResolveSchema(
                schemas,
                schema.GetProperty("properties").GetProperty(property.Key));
            propertySchema.GetProperty("type").GetString().Should().Be(property.Value);
            if (propertySchema.TryGetProperty("nullable", out var nullable))
            {
                nullable.GetBoolean().Should().BeFalse();
            }
        }
    }

    private static void AssertSchemaEnum(
        JsonElement schemas,
        string schemaName,
        string propertyName,
        IReadOnlyCollection<string> expected)
    {
        var property = ResolveSchema(
            schemas,
            schemas.GetProperty(schemaName).GetProperty("properties").GetProperty(propertyName));
        property.GetProperty("enum").EnumerateArray().Select(item => item.GetString()).Should()
            .BeEquivalentTo(expected);
    }

    private static JsonElement ResolveSchema(JsonElement schemas, JsonElement schema)
    {
        if (!schema.TryGetProperty("$ref", out var reference))
        {
            return schema;
        }

        return schemas.GetProperty(reference.GetString()!.Split('/')[^1]);
    }

    private static void AssertPageEnvelope(
        JsonElement page,
        int expectedPage,
        int expectedPageSize,
        int? totalItems = null,
        int? totalPages = null)
    {
        page.ValueKind.Should().Be(JsonValueKind.Object);
        page.GetProperty("page").GetInt32().Should().Be(expectedPage);
        page.GetProperty("pageSize").GetInt32().Should().Be(expectedPageSize);
        page.GetProperty("items").ValueKind.Should().Be(JsonValueKind.Array);
        page.GetProperty("totalItems").GetInt32().Should().Be(totalItems ?? page.GetProperty("items").GetArrayLength());
        page.GetProperty("totalPages").GetInt32().Should().Be(totalPages ?? (totalItems is 0 ? 0 : 1));
    }

    private static void AssertSeasonSummary(JsonElement season)
    {
        season.GetProperty("id").GetGuid().Should().NotBeEmpty();
        season.GetProperty("nome").GetString().Should().NotBeNullOrWhiteSpace();
        season.GetProperty("ano").GetInt32().Should().BeInRange(2009, 9999);
        season.GetProperty("ordemNoAno").GetInt32().Should().BeGreaterThan(0);
        DateOnly.Parse(season.GetProperty("dataInicio").GetString()!).Should()
            .BeBefore(DateOnly.Parse(season.GetProperty("dataFimExclusiva").GetString()!));
        season.GetProperty("estado").GetString().Should().BeOneOf("Planejada", "Ativa", "Encerrada");
        season.GetProperty("versao").GetInt64().Should().BeGreaterThanOrEqualTo(0);
    }

    private static void AssertSeasonDetail(
        JsonElement season,
        Guid id,
        string name,
        int year,
        string state)
    {
        AssertSeasonSummary(season);
        season.GetProperty("id").GetGuid().Should().Be(id);
        season.GetProperty("nome").GetString().Should().Be(name);
        season.GetProperty("ano").GetInt32().Should().Be(year);
        season.GetProperty("estado").GetString().Should().Be(state);
        season.GetProperty("quantidadeCompeticoes").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        season.GetProperty("acoesPermitidas").ValueKind.Should().Be(JsonValueKind.Array);
    }

    private static void AssertCompetitionPage(
        JsonElement page,
        int expectedPage,
        int expectedPageSize,
        bool calendarConfigured,
        Guid? currentSeasonId,
        IReadOnlyCollection<Guid> includedSeasonIds,
        IReadOnlyCollection<Guid> competitionIds)
    {
        AssertPageEnvelope(page, expectedPage, expectedPageSize, competitionIds.Count, competitionIds.Count == 0 ? 0 : 1);
        page.GetProperty("calendarioConfigurado").GetBoolean().Should().Be(calendarConfigured);
        if (currentSeasonId.HasValue)
        {
            page.GetProperty("temporadaAtual").GetProperty("id").GetGuid().Should().Be(currentSeasonId.Value);
        }
        else
        {
            page.GetProperty("temporadaAtual").ValueKind.Should().Be(JsonValueKind.Null);
        }

        page.GetProperty("seasonsIncluidas").EnumerateArray()
            .Select(item => item.GetProperty("id").GetGuid())
            .Should().BeEquivalentTo(includedSeasonIds);
        if (includedSeasonIds.Count > 0)
        {
            page.GetProperty("seasonsIncluidas").EnumerateArray().Should()
                .OnlyContain(season => HasSeasonSummaryShape(season));
        }
        page.GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("id").GetGuid())
            .Should().BeEquivalentTo(competitionIds);
        if (competitionIds.Count > 0)
        {
            page.GetProperty("items").EnumerateArray().Should().OnlyContain(item =>
                includedSeasonIds.Contains(item.GetProperty("seasonId").GetGuid())
                && HasCompetitionDetailShape(item));
        }
    }

    private static void AssertCompetitionDetail(
        JsonElement competition,
        Guid id,
        Guid seasonId,
        string name,
        string code,
        bool dailyCircuit)
    {
        competition.GetProperty("id").GetGuid().Should().Be(id);
        competition.GetProperty("seasonId").GetGuid().Should().Be(seasonId);
        competition.GetProperty("nome").GetString().Should().Be(name);
        competition.GetProperty("codigo").GetString().Should().Be(code);
        competition.GetProperty("circuitoDiario").GetBoolean().Should().Be(dailyCircuit);
        competition.GetProperty("rodadas").ValueKind.Should().Be(JsonValueKind.Array);
        competition.GetProperty("regrasPublicadas").ValueKind.Should().Be(JsonValueKind.Array);
        competition.GetProperty("versao").GetInt64().Should().BeGreaterThanOrEqualTo(0);
        competition.GetProperty("acoesPermitidas").ValueKind.Should().Be(JsonValueKind.Array);
    }

    private static void AssertMutationDto(MutationKind kind, JsonElement body)
    {
        switch (kind)
        {
            case MutationKind.CreateSeason:
            case MutationKind.UpdateSeason:
            case MutationKind.ActivateSeason:
            case MutationKind.CloseSeason:
                AssertSeasonSummary(body);
                body.GetProperty("quantidadeCompeticoes").GetInt32().Should().BeGreaterThanOrEqualTo(0);
                body.GetProperty("acoesPermitidas").ValueKind.Should().Be(JsonValueKind.Array);
                break;
            case MutationKind.CreateCompetition:
            case MutationKind.UpdateCompetition:
                body.GetProperty("id").GetGuid().Should().NotBeEmpty();
                body.GetProperty("seasonId").GetGuid().Should().NotBeEmpty();
                body.GetProperty("nome").GetString().Should().NotBeNullOrWhiteSpace();
                body.GetProperty("codigo").GetString().Should().NotBeNullOrWhiteSpace();
                body.GetProperty("circuitoDiario").ValueKind.Should().BeOneOf(JsonValueKind.True, JsonValueKind.False);
                body.GetProperty("rodadas").ValueKind.Should().Be(JsonValueKind.Array);
                body.GetProperty("regrasPublicadas").ValueKind.Should().Be(JsonValueKind.Array);
                body.GetProperty("versao").GetInt64().Should().BeGreaterThanOrEqualTo(0);
                body.GetProperty("acoesPermitidas").ValueKind.Should().Be(JsonValueKind.Array);
                break;
            case MutationKind.CreateRound:
                AssertRoundSummary(body);
                break;
            case MutationKind.ReorderRounds:
                body.ValueKind.Should().Be(JsonValueKind.Array);
                body.EnumerateArray().Should().NotBeEmpty().And
                    .OnlyContain(round => HasRoundShape(round));
                break;
            case MutationKind.PublishSeasonRules:
            case MutationKind.PublishCompetitionRules:
                body.GetProperty("id").GetGuid().Should().NotBeEmpty();
                body.GetProperty("seasonId").GetGuid().Should().NotBeEmpty();
                body.GetProperty("numero").GetInt32().Should().BeGreaterThan(0);
                body.GetProperty("formato").GetString().Should().BeOneOf("Md3", "Md5");
                body.GetProperty("modoDraft").GetString().Should().BeOneOf("Padrao", "Fearless");
                body.GetProperty("publicadaEm").GetDateTimeOffset().Should().BeBefore(DateTimeOffset.UtcNow.AddMinutes(1));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }

    private static void AssertRoundSummary(JsonElement round)
    {
        round.GetProperty("id").GetGuid().Should().NotBeEmpty();
        round.GetProperty("competicaoId").GetGuid().Should().NotBeEmpty();
        round.GetProperty("nome").GetString().Should().NotBeNullOrWhiteSpace();
        round.GetProperty("ordem").GetInt32().Should().BeGreaterThan(0);
        round.GetProperty("versao").GetInt64().Should().BeGreaterThanOrEqualTo(0);
    }

    private static bool HasSeasonSummaryShape(JsonElement season) =>
        season.TryGetProperty("nome", out _)
        && season.TryGetProperty("ano", out _)
        && season.TryGetProperty("ordemNoAno", out _)
        && season.TryGetProperty("dataInicio", out _)
        && season.TryGetProperty("dataFimExclusiva", out _)
        && season.TryGetProperty("estado", out _)
        && season.TryGetProperty("versao", out _);

    private static bool HasCompetitionDetailShape(JsonElement competition) =>
        competition.TryGetProperty("nome", out _)
        && competition.TryGetProperty("codigo", out _)
        && competition.TryGetProperty("circuitoDiario", out _)
        && competition.TryGetProperty("rodadas", out _)
        && competition.TryGetProperty("regrasPublicadas", out _)
        && competition.TryGetProperty("versao", out _);

    private static bool HasRoundShape(JsonElement round) =>
        round.TryGetProperty("id", out _)
        && round.TryGetProperty("competicaoId", out _)
        && round.TryGetProperty("nome", out _)
        && round.TryGetProperty("ordem", out _)
        && round.TryGetProperty("versao", out _);

    private static void AssertRound(
        JsonElement round,
        Guid id,
        Guid competitionId,
        string name,
        int order)
    {
        round.GetProperty("id").GetGuid().Should().Be(id);
        round.GetProperty("competicaoId").GetGuid().Should().Be(competitionId);
        round.GetProperty("nome").GetString().Should().Be(name);
        round.GetProperty("ordem").GetInt32().Should().Be(order);
        round.GetProperty("versao").GetInt64().Should().BeGreaterThanOrEqualTo(0);
    }

    private static string NewKey() => $"test-{Guid.NewGuid():N}";

    private static bool IsCalendarMutation(string route) =>
        route.EndsWith("/aberturas", StringComparison.Ordinal)
        || route.EndsWith("/encerramentos", StringComparison.Ordinal);

    public enum MutationKind
    {
        CreateSeason,
        UpdateSeason,
        ActivateSeason,
        CloseSeason,
        CreateCompetition,
        PublishSeasonRules,
        UpdateCompetition,
        CreateRound,
        ReorderRounds,
        PublishCompetitionRules,
    }

    public enum ConditionalMutationKind
    {
        UpdateSeason,
        ActivateSeason,
        CloseSeason,
        PublishSeasonRules,
        UpdateCompetition,
        CreateRound,
        ReorderRounds,
        PublishCompetitionRules,
    }

    public enum InvalidDtoKind
    {
        CreateSeason,
        UpdateSeason,
        Competition,
        Rules,
    }

    private sealed class CompetitiveSeasonApiFactory(string connectionString) : SecurityApiFactory
    {
        internal Guid GetBootstrapActorId()
        {
            using var scope = Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>().Users
                .Select(user => user.Id)
                .First();
        }

        internal HttpClient CreateClientFor(string role, Guid? actorId = null) =>
            CreateJwtClient(actorId ?? GetBootstrapActorId(), role);

        internal async Task<Guid> CreateActorAsync(string email)
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
            var actor = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Nome = "Ator de teste",
                UserName = email,
                NormalizedUserName = email.ToUpperInvariant(),
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                ConcurrencyStamp = Guid.NewGuid().ToString("N"),
                DataCadastro = DateTimeOffset.UtcNow,
                DataAtualizacao = DateTimeOffset.UtcNow,
            };
            context.Users.Add(actor);
            await context.SaveChangesAsync();
            return actor.Id;
        }

        internal async Task SeedStartedSeriesAsync(
            Guid seasonId,
            Guid competitionId,
            Guid roundId,
            Guid rulesId)
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
            var actorId = context.Users.Select(user => user.Id).First();
            var sides = new[]
            {
                new LadoSerie(Guid.NewGuid(), 1, LadoSerieTipo.TimeOficial, Guid.NewGuid(), "Azul", "AZ", null, null, []),
                new LadoSerie(Guid.NewGuid(), 2, LadoSerieTipo.TimeOficial, Guid.NewGuid(), "Vermelho", "VM", null, null, []),
            };
            var series = new Serie(
                seasonId,
                competitionId,
                roundId,
                rulesId,
                eventoId: null,
                draftMontagemId: null,
                SerieTipo.ConfrontoOficial,
                SerieFormato.Md3,
                ModoDraft.Padrao,
                fearlessHabilitado: false,
                new DateTimeOffset(2055, 6, 1, 21, 0, 0, TimeSpan.Zero),
                dataLocal: null,
                actorId,
                DateTimeOffset.UtcNow,
                sides);
            context.Series.Add(series);
            context.Entry(series).Property(item => item.Estado).CurrentValue = SerieEstado.EmAndamento;
            await context.SaveChangesAsync();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder
                .UseSetting("ConnectionStrings:RinhaDasLendas", connectionString)
                .UseSetting("ConnectionStrings:DefaultConnection", connectionString)
                .UseSetting("Authentication:BootstrapSuperAdmin:Enabled", "true")
                .UseSetting("Authentication:BootstrapSuperAdmin:Email", "competitive-season-api@example.com")
                .UseSetting("Authentication:BootstrapSuperAdmin:Senha", "CompetitiveSeasonApi123!")
                .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:RinhaDasLendas"] = connectionString,
                        ["ConnectionStrings:DefaultConnection"] = connectionString,
                    }));
        }
    }

    private sealed record CreatedResource(Guid Id, string ETag, JsonElement Body);

    private sealed record MutationScenario(
        HttpMethod Method,
        string Route,
        object? Body,
        object? DivergentBody,
        string? ETag,
        HttpStatusCode SuccessStatus,
        bool RequiresResponseETag);

    private sealed record MutationRequest(HttpMethod Method, string Route, object? Body);

    private sealed record ConditionalMutationScenario(
        HttpMethod Method,
        string Route,
        object? Body,
        string ETag,
        HttpStatusCode SuccessStatus,
        Func<Task<MutationRequest>> CreateStaleRequestAsync);

    private sealed record InvalidDtoScenario(
        HttpMethod Method,
        string Route,
        object InvalidBody,
        object AdditionalPropertyBody,
        string? ETag);

    private sealed record SwaggerExpectation(
        IReadOnlyCollection<string> RequestHeaders,
        IReadOnlyCollection<string> ResponseHeaders,
        IReadOnlyCollection<string> Statuses);
}
