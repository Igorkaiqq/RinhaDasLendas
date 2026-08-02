using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Xml.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using RinhaDasLendas.Api.Filters;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Infrastructure.Identity;
using RinhaDasLendas.Infrastructure.Persistence;
using RinhaDasLendas.Tests.Infrastructure;

namespace RinhaDasLendas.Tests.Messages;

public sealed class CompetitiveFoundationLocalizationTests
{
    private const string ValidationEnvelopeCode = "ME031";
    private const string SeasonNameRequiredCode = "MV106";
    private const string IdempotencyConflictCode = "MV126";
    private const string CompetitiveAccessDeniedCode = "ME053";
    private const string SeasonNotFoundCode = "ME045";

    private static readonly MessageContract[] CompetitiveCatalog =
    [
        new("SeasonNotFound", "ME045", "Temporada não encontrada", "Season not found"),
        new("CompetitionNotFound", "ME046", "Competição não encontrada", "Competition not found"),
        new("RoundNotFound", "ME047", "Rodada não encontrada", "Round not found"),
        new("RulesVersionNotFound", "ME048", "Versão de regras não encontrada", "Rules version not found"),
        new("CompetitiveEventNotFound", "ME049", "Evento competitivo não encontrado", "Competitive event not found"),
        new("CompetitiveSeriesNotFound", "ME050", "Série competitiva não encontrada", "Competitive series not found"),
        new("CompetitiveMatchNotFound", "ME051", "Partida competitiva não encontrada", "Competitive match not found"),
        new("ActiveSeasonNotFound", "ME052", "Não há temporada ativa", "There is no active Season"),
        new("CompetitiveAccessDenied", "ME053", "Acesso competitivo negado", "Competitive access denied"),
        new("SeasonNameRequired", "MV106", "O nome da temporada é obrigatório", "Season name is required"),
        new("SeasonPeriodInvalid", "MV107", "O período da temporada é inválido", "Season period is invalid"),
        new("SeasonPeriodOverlap", "MV108", "O período da temporada sobrepõe outra temporada", "Season period overlaps another Season"),
        new("CompetitiveCalendarVersionStale", "MV109", "A versão do calendário competitivo está desatualizada", "Competitive calendar version is stale"),
        new("CompetitiveResourceVersionStale", "MV110", "A versão do recurso competitivo está desatualizada", "Competitive resource version is stale"),
        new("SeasonalFilterConflict", "MV111", "Selecione temporadas específicas ou todas as temporadas, não ambas", "Select specific Seasons or all Seasons, not both"),
        new("CompetitionSeasonMismatch", "MV112", "A competição deve pertencer à mesma temporada", "Competition must belong to the same Season"),
        new("RoundSeasonMismatch", "MV113", "A rodada deve pertencer à mesma temporada", "Round must belong to the same Season"),
        new("RulesVersionSeasonMismatch", "MV114", "A versão de regras deve pertencer à mesma temporada", "Rules version must belong to the same Season"),
        new("DailyCircuitCompetitionInvalid", "MV115", "A competição do circuito diário é inválida", "Daily circuit competition is invalid"),
        new("DailySeriesDraftInvalid", "MV116", "O draft da série diária deve estar finalizado", "Daily series draft must be finalized"),
        new("DailySeriesCaptainsInvalid", "MV117", "A série diária exige dois capitães distintos", "Daily series requires two distinct captains"),
        new("DailySeriesSidesInvalid", "MV118", "A série diária exige dois lados distintos", "Daily series requires two distinct sides"),
        new("DailySeriesDateInvalid", "MV119", "A data local da série diária é inválida", "Daily series local date is invalid"),
        new("EventFearlessConflict", "MV120", "Eventos com quatro times não permitem Fearless", "Four-team events do not allow Fearless"),
        new("ChampionBlockedByFearless", "MV121", "O campeão está bloqueado pelo Fearless nesta série", "Champion is blocked by Fearless in this series"),
        new("SeriesAlreadyDecided", "MV122", "A série já possui um vencedor", "Series already has a winner"),
        new("SeriesTransitionInvalid", "MV123", "A transição de estado da série é inválida", "Series state transition is invalid"),
        new("SeriesReviewRequired", "MV124", "A série exige revisão antes de continuar", "Series requires review before continuing"),
        new("RemakeDecisionRequired", "MV125", "Informe se os picks do remake devem ser preservados", "Specify whether remake picks must be preserved"),
        new("CompetitiveIdempotencyConflict", "MV126", "A chave de idempotência já foi usada com conteúdo diferente", "Idempotency key was already used with different content"),
        new("CorrectionJustificationRequired", "MV127", "A justificativa da correção é obrigatória", "Correction justification is required"),
        new("CorrectionAnnulConfirmationRequired", "MV128", "Confirme a anulação da série para concluir a correção", "Confirm Series annulment to complete the correction"),
        new("CompetitionCodeConflict", "MV129", "Já existe uma competição com este código na temporada", "A competition with this code already exists in the Season"),
        new("RoundOrderConflict", "MV130", "Já existe uma rodada nesta ordem na competição", "A round with this order already exists in the competition"),
        new("ActiveSeasonConflict", "MV131", "Já existe uma temporada ativa", "An active Season already exists"),
        new("SeasonOrderConflict", "MV132", "Já existe uma temporada nesta ordem para o ano informado", "A Season with this order already exists for the selected year"),
    ];

    [Fact]
    public void CompetitiveCatalog_ShouldDefineSequentialStableMessageCodes()
    {
        CompetitiveCatalog.Where(contract => contract.Code.StartsWith("ME", StringComparison.Ordinal))
            .Select(contract => contract.Code)
            .Should().Equal(Enumerable.Range(45, 9).Select(number => $"ME{number:000}"));
        CompetitiveCatalog.Where(contract => contract.Code.StartsWith("MV", StringComparison.Ordinal))
            .Select(contract => contract.Code)
            .Should().Equal(Enumerable.Range(106, 27).Select(number => $"MV{number:000}"));

        using var scope = new AssertionScope();
        foreach (var contract in CompetitiveCatalog)
        {
            var field = typeof(MessageCodes).GetField(contract.ConstantName, BindingFlags.Public | BindingFlags.Static);
            field.Should().NotBeNull($"MessageCodes.{contract.ConstantName} must define {contract.Code}");
            field?.GetRawConstantValue().Should().Be(contract.Code);
        }
    }

    [Fact]
    public void CompetitiveResources_ShouldHaveThreeWayParityAndExactLocalizedCatalog()
    {
        var neutral = LoadResource("Messages.resx");
        var portuguese = LoadResource("Messages.pt-BR.resx");
        var english = LoadResource("Messages.en-US.resx");

        neutral.Keys.Should().BeEquivalentTo(portuguese.Keys, "neutral and PT-BR resource keys must remain synchronized");
        neutral.Keys.Should().BeEquivalentTo(english.Keys, "neutral and EN-US resource keys must remain synchronized");

        using var scope = new AssertionScope();
        foreach (var contract in CompetitiveCatalog)
        {
            neutral.Should().ContainKey(contract.Code).WhoseValue.Should().NotBeNullOrWhiteSpace();
            portuguese.Should().ContainKey(contract.Code).WhoseValue.Should().NotBeNullOrWhiteSpace();
            english.Should().ContainKey(contract.Code).WhoseValue.Should().NotBeNullOrWhiteSpace();

            neutral.GetValueOrDefault(contract.Code).Should().Be(contract.Portuguese);
            portuguese.GetValueOrDefault(contract.Code).Should().Be(contract.Portuguese);
            english.GetValueOrDefault(contract.Code).Should().Be(contract.English);
            neutral.GetValueOrDefault(contract.Code).Should().Be(portuguese.GetValueOrDefault(contract.Code));
            english.GetValueOrDefault(contract.Code).Should().NotBe(
                portuguese.GetValueOrDefault(contract.Code),
                $"{contract.Code} must not silently reuse Portuguese in EN-US");
        }
    }

    [Theory]
    [InlineData("pt-BR", "Erro de validação", "O nome da temporada é obrigatório")]
    [InlineData("en-US", "Validation error", "Season name is required")]
    public async Task ValidationEnvelope_ShouldContainExactLocalizedFieldError(
        string culture,
        string expectedMessage,
        string expectedError)
    {
        await using var factory = new CompetitiveLocalizationApiFactory();
        await factory.InitializeDatabaseAsync();
        using var client = factory.CreatePresidentClient();
        using var request = CreateSeasonRequest(culture, $"t008-validation-{culture}", SeasonPayload(name: ""));

        using var response = await client.SendAsync(request);

        await AssertEnvelopeAsync(
            response,
            HttpStatusCode.BadRequest,
            ValidationEnvelopeCode,
            expectedMessage,
            expectedError);
    }

    [Theory]
    [InlineData("pt-BR", "A chave de idempotência já foi usada com conteúdo diferente")]
    [InlineData("en-US", "Idempotency key was already used with different content")]
    public async Task DivergentIdempotencyReplay_ShouldReturnLocalizedConflictEnvelope(
        string culture,
        string expectedMessage)
    {
        await using var factory = new CompetitiveLocalizationApiFactory();
        await factory.InitializeDatabaseAsync();
        using var client = factory.CreatePresidentClient();
        var idempotencyKey = $"t008-conflict-{culture}";
        using var create = CreateSeasonRequest(culture, idempotencyKey, SeasonPayload("Temporada T008"));
        using var created = await client.SendAsync(create);
        created.StatusCode.Should().Be(HttpStatusCode.Created, "a valid Season must exist before testing divergent replay");
        using var divergent = CreateSeasonRequest(culture, idempotencyKey, SeasonPayload("Temporada T008 alterada"));

        using var response = await client.SendAsync(divergent);

        await AssertEnvelopeAsync(response, HttpStatusCode.Conflict, IdempotencyConflictCode, expectedMessage);
    }

    [Theory]
    [InlineData("pt-BR", "Acesso competitivo negado")]
    [InlineData("en-US", "Competitive access denied")]
    public async Task ForbiddenEnvelope_ShouldBeFullyLocalized(string culture, string expectedMessage)
    {
        await using var factory = new CompetitiveLocalizationApiFactory();
        await factory.InitializeDatabaseAsync();
        using var client = factory.CreateVicePresidentClient();
        using var request = CreateSeasonRequest(culture, $"t008-forbidden-{culture}", SeasonPayload("Temporada proibida"));

        using var response = await client.SendAsync(request);

        await AssertEnvelopeAsync(response, HttpStatusCode.Forbidden, CompetitiveAccessDeniedCode, expectedMessage);
    }

    [Theory]
    [InlineData("pt-BR", "Temporada não encontrada")]
    [InlineData("en-US", "Season not found")]
    public async Task NotFoundEnvelope_ShouldBeFullyLocalized(string culture, string expectedMessage)
    {
        await using var factory = new CompetitiveLocalizationApiFactory();
        await factory.InitializeDatabaseAsync();
        using var client = factory.CreatePresidentClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/temporadas/00000000-0000-0000-0000-000000000404");
        request.Headers.Add("Accept-Language", culture);

        using var response = await client.SendAsync(request);

        await AssertEnvelopeAsync(response, HttpStatusCode.NotFound, SeasonNotFoundCode, expectedMessage);
    }

    private static HttpRequestMessage CreateSeasonRequest(string culture, string idempotencyKey, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/temporadas")
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Add("Accept-Language", culture);
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return request;
    }

    private static object SeasonPayload(string name) => new
    {
        nome = name,
        ano = 2027,
        ordemNoAno = 1,
        dataInicio = "2027-01-01",
        dataFimExclusiva = "2027-05-01",
    };

    private static async Task AssertEnvelopeAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedCode,
        string expectedMessage,
        params string[] expectedErrors)
    {
        response.StatusCode.Should().Be(expectedStatus);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotBeNullOrWhiteSpace("the endpoint must return the standard localized envelope");
        var envelope = JsonSerializer.Deserialize<ApiErrorResponse>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        envelope.Should().NotBeNull();
        envelope!.MessageCode.Should().Be(expectedCode);
        envelope.Message.Should().Be(expectedMessage);
        envelope.Errors.Should().Equal(expectedErrors);
    }

    private static IReadOnlyDictionary<string, string> LoadResource(string fileName)
    {
        var path = Path.Combine(
            FindRepositoryRoot(),
            "BackEnd",
            "src",
            "RinhaDasLendas.Infrastructure",
            "Messages",
            fileName);

        return XDocument.Load(path)
            .Root!
            .Elements("data")
            .ToDictionary(
                element => element.Attribute("name")!.Value,
                element => element.Element("value")!.Value,
                StringComparer.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "BackEnd")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("the tests must execute from inside the repository worktree");
        return directory!.FullName;
    }

    private sealed record MessageContract(string ConstantName, string Code, string Portuguese, string English);

    private sealed class CompetitiveLocalizationApiFactory() : SecurityApiFactory(useIsolatedPostgreSql: true)
    {
        private readonly Guid _actorId = Guid.NewGuid();

        internal HttpClient CreatePresidentClient() => CreateJwtClient(_actorId, "Presidente");

        internal HttpClient CreateVicePresidentClient() => CreateJwtClient(_actorId, "VicePresidente");

        internal async Task InitializeDatabaseAsync()
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
            await context.Database.EnsureCreatedAsync();
            context.Users.Add(new ApplicationUser
            {
                Id = _actorId,
                Nome = "Ator de localização",
                UserName = "competitive-localization@example.com",
                NormalizedUserName = "COMPETITIVE-LOCALIZATION@EXAMPLE.COM",
                Email = "competitive-localization@example.com",
                NormalizedEmail = "COMPETITIVE-LOCALIZATION@EXAMPLE.COM",
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                ConcurrencyStamp = Guid.NewGuid().ToString("N"),
                DataCadastro = DateTimeOffset.UtcNow,
                DataAtualizacao = DateTimeOffset.UtcNow,
            });
            await context.SaveChangesAsync();
        }
    }
}
