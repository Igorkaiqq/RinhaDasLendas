using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using FluentValidation;
using RinhaDasLendas.Application.Commands.Partidas;
using RinhaDasLendas.Application.Commands.Series;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Validators;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Tests.Application;

public sealed class DailySeriesCommandContractTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [Theory]
    [MemberData(nameof(HttpRequestDtoTypes))]
    public void HttpRequestDto_ShouldRejectUnknownProperties(Type requestType)
    {
        requestType.GetCustomAttribute<JsonUnmappedMemberHandlingAttribute>()?.UnmappedMemberHandling
            .Should().Be(JsonUnmappedMemberHandling.Disallow);
    }

    [Fact]
    public void HttpBinding_ShouldActuallyRejectClientActorOrRoleProperties()
    {
        var act = () => JsonSerializer.Deserialize<CorrectMatchRequestDto>(
            """{"justificativa":"Revisao tecnica","atorUsuarioId":"00000000-0000-0000-0000-000000000001"}""",
            JsonOptions);

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void CreateSeriesRequest_ShouldMatchCorrectedOpenApiFieldsIncludingDataLocal()
    {
        var expected = new[]
        {
            "SeasonId", "CompeticaoId", "RodadaId", "VersaoRegrasId", "EventoId", "Tipo",
            "AgendadaPara", "DataLocal", "DraftMontagemId", "LadoOrigemIds",
        };

        typeof(CreateSeriesRequestDto).GetProperties().Select(property => property.Name)
            .Should().BeEquivalentTo(expected);
        RequestDtoProperties().Where(property =>
            property.Name.Contains("Ator", StringComparison.OrdinalIgnoreCase)
            || property.Name.Contains("Usuario", StringComparison.OrdinalIgnoreCase)
            || property.Name.Contains("Role", StringComparison.OrdinalIgnoreCase)
            || property.Name is "Versao" or "ExpectedVersion")
            .Should().BeEmpty();

        var request = DailyRequest();
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(request, JsonOptions));
        json.RootElement.TryGetProperty("dataLocal", out var dataLocal).Should().BeTrue();
        dataLocal.GetString().Should().Be("2026-07-28");
    }

    [Fact]
    public void CorrectMatchRequest_ShouldPreserveOmittedAndExplicitNullOptionalFields()
    {
        var omitted = JsonSerializer.Deserialize<CorrectMatchRequestDto>(
            """{"justificativa":"Revisao tecnica"}""", JsonOptions)!;
        var explicitNull = JsonSerializer.Deserialize<CorrectMatchRequestDto>(
            """{"lados":null,"justificativa":"Revisao tecnica"}""", JsonOptions)!;

        omitted.Lados.HasValue.Should().BeFalse();
        explicitNull.Lados.HasValue.Should().BeTrue();
        explicitNull.Lados.Value.Should().BeNull();
        omitted.LadoVencedorId.HasValue.Should().BeFalse();
        omitted.MotivoTermino.HasValue.Should().BeFalse();
        omitted.AnularSerieSeInconclusiva.HasValue.Should().BeFalse();
    }

    [Theory]
    [InlineData(typeof(CreateSeriesRequestDto), "tipo", "DiariaTemporaria", "diariaTemporaria")]
    [InlineData(typeof(ConfirmResultRequestDto), "motivoTermino", "Surrender", "surrender")]
    [InlineData(typeof(RemakeRequestDto), "decisaoPicks", "PreservarPicks", "preservarpicks")]
    [InlineData(typeof(CorrectMatchRequestDto), "motivoTermino", "Normal", "NORMAL")]
    public void T042EnumFields_ShouldAcceptOnlyExactCaseSensitiveOpenApiStrings(
        Type requestType,
        string property,
        string exactValue,
        string wrongCaseValue)
    {
        var exactJson = EnumRequestJson(requestType, property, exactValue);
        var wrongCaseJson = EnumRequestJson(requestType, property, wrongCaseValue);

        JsonSerializer.Deserialize(exactJson, requestType, JsonOptions).Should().NotBeNull();
        var act = () => JsonSerializer.Deserialize(wrongCaseJson, requestType, JsonOptions);
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Commands_ShouldCarryRouteIdsAndSharedSeriesVersionOutsideRequestBodies()
    {
        var seriesId = Guid.NewGuid();
        var matchId = Guid.NewGuid();
        var reason = new ReasonRequestDto("Operacao auditavel");
        var result = new ConfirmResultRequestDto(Guid.NewGuid(), MotivoTerminoPartida.Normal);

        new StartSeriesCommand(seriesId, 7).Should().BeEquivalentTo(new { SeriesId = seriesId, ExpectedVersion = 7L });
        new CancelSeriesCommand(seriesId, reason, 8).Should().BeEquivalentTo(
            new { SeriesId = seriesId, Request = reason, ExpectedVersion = 8L });
        new AnnulSeriesCommand(seriesId, reason, 9).Should().BeEquivalentTo(
            new { SeriesId = seriesId, Request = reason, ExpectedVersion = 9L });
        new AddNextMatchCommand(seriesId, 10).Should().BeEquivalentTo(
            new { SeriesId = seriesId, ExpectedVersion = 10L });
        new ConfirmMatchResultCommand(matchId, result, 11).Should().BeEquivalentTo(
            new { MatchId = matchId, Request = result, ExpectedVersion = 11L });
    }

    [Fact]
    public void CreateDailySeriesValidator_ShouldAcceptExactDailyContractAndRejectInvalidSidesOrDataLocal()
    {
        var validator = new CreateSeriesRequestDtoValidator();
        validator.Validate(DailyRequest()).IsValid.Should().BeTrue();

        var duplicatedSide = Guid.NewGuid();
        validator.Validate(DailyRequest(sideIds: [duplicatedSide, duplicatedSide])).IsValid.Should().BeFalse();
        validator.Validate(DailyRequest(dataLocal: new Optional<DateOnly?>(null))).IsValid.Should().BeFalse();
    }

    [Fact]
    public void SeriesTransitionValidators_ShouldRequireResourceVersionAndAuditedReasons()
    {
        new StartSeriesCommandValidator().Validate(new StartSeriesCommand(Guid.Empty, -1)).IsValid.Should().BeFalse();
        var reasonValidator = new ReasonRequestDtoValidator();
        reasonValidator.Validate(new ReasonRequestDto(" ")).IsValid.Should().BeFalse();
        reasonValidator.Validate(new ReasonRequestDto(new string('x', 501))).IsValid.Should().BeFalse();
        reasonValidator.Validate(new ReasonRequestDto("Motivo valido")).IsValid.Should().BeTrue();

        new CancelSeriesCommandValidator(reasonValidator)
            .Validate(new CancelSeriesCommand(Guid.NewGuid(), new ReasonRequestDto("Motivo"), 0))
            .IsValid.Should().BeTrue();
        new AnnulSeriesCommandValidator(reasonValidator)
            .Validate(new AnnulSeriesCommand(Guid.NewGuid(), new ReasonRequestDto("Motivo"), 0))
            .IsValid.Should().BeTrue();
    }

    [Fact]
    public void RegisterPicksValidator_ShouldRequireFivePlusFiveDistinctPositiveChampions()
    {
        var validator = new RegisterPicksRequestDtoValidator();
        var valid = PicksRequest(1, 6);

        validator.Validate(valid).IsValid.Should().BeTrue();
        validator.Validate(PicksRequest(1, 5)).IsValid.Should().BeFalse("champions cannot repeat across sides");
        validator.Validate(new RegisterPicksRequestDto(
        [
            new MatchSidePicksRequestDto(Guid.NewGuid(), [1, 2, 3, 4]),
            new MatchSidePicksRequestDto(Guid.NewGuid(), [6, 7, 8, 9, 10]),
        ])).IsValid.Should().BeFalse();
    }

    [Fact]
    public void PicksValidators_ShouldRejectNullSideEntriesWithoutThrowing()
    {
        var register = JsonSerializer.Deserialize<RegisterPicksRequestDto>(
            $$"""{"lados":[null,{"ladoSerieId":"{{Guid.NewGuid()}}","championIds":[1,2,3,4,5]}]}""",
            JsonOptions)!;
        var correction = JsonSerializer.Deserialize<CorrectMatchRequestDto>(
            $$"""{"lados":[null,{"ladoSerieId":"{{Guid.NewGuid()}}","championIds":[1,2,3,4,5]}],"justificativa":"Correcao auditada"}""",
            JsonOptions)!;

        var registerAct = () => new RegisterPicksRequestDtoValidator().Validate(register);
        var correctionAct = () => new CorrectMatchRequestDtoValidator().Validate(correction);

        registerAct.Should().NotThrow().Which.IsValid.Should().BeFalse();
        correctionAct.Should().NotThrow().Which.IsValid.Should().BeFalse();
        registerAct().Errors.Should().OnlyContain(failure => IsKnownMessageCode(failure.ErrorMessage));
        correctionAct().Errors.Should().OnlyContain(failure => IsKnownMessageCode(failure.ErrorMessage));
    }

    [Theory]
    [InlineData(MotivoTerminoPartida.Normal)]
    [InlineData(MotivoTerminoPartida.Surrender)]
    public void ResultValidator_ShouldAcceptNormalAndSurrender(MotivoTerminoPartida reason)
    {
        new ConfirmResultRequestDtoValidator()
            .Validate(new ConfirmResultRequestDto(Guid.NewGuid(), reason)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void RemakeAnnulAndCorrectionValidators_ShouldEnforceDecisionsAndCorrectionShape()
    {
        var remakeValidator = new RemakeRequestDtoValidator();
        remakeValidator.Validate(new RemakeRequestDto(
            DecisaoPicksRemake.PreservarPicks, "Falha antes do inicio")).IsValid.Should().BeTrue();
        remakeValidator.Validate(new RemakeRequestDto(
            (DecisaoPicksRemake)99, "Falha antes do inicio")).IsValid.Should().BeFalse();

        var annulValidator = new AnnulMatchRequestDtoValidator();
        annulValidator.Validate(new AnnulMatchRequestDto(" ", false)).IsValid.Should().BeFalse();
        annulValidator.Validate(new AnnulMatchRequestDto("Fato invalido", false)).IsValid.Should().BeTrue();

        var correctionValidator = new CorrectMatchRequestDtoValidator();
        correctionValidator.Validate(new CorrectMatchRequestDto(
            Justificativa: "Correcao auditada")).IsValid.Should().BeFalse();
        correctionValidator.Validate(new CorrectMatchRequestDto(
            LadoVencedorId: Guid.NewGuid(),
            Justificativa: "Correcao auditada")).IsValid.Should().BeFalse("winner and termination reason form one correction");
        correctionValidator.Validate(new CorrectMatchRequestDto(
            Lados: new Optional<IReadOnlyCollection<MatchSidePicksRequestDto>?>(PicksRequest(1, 6).Lados),
            Justificativa: "Correcao auditada")).IsValid.Should().BeTrue();

        var picks = new Optional<IReadOnlyCollection<MatchSidePicksRequestDto>?>(PicksRequest(1, 6).Lados);
        correctionValidator.Validate(new CorrectMatchRequestDto(
            Lados: picks,
            LadoVencedorId: Guid.NewGuid(),
            Justificativa: "Correcao auditada")).IsValid.Should().BeFalse(
            "picks cannot mask a supplied result missing motivoTermino");
        correctionValidator.Validate(new CorrectMatchRequestDto(
            Lados: picks,
            MotivoTermino: MotivoTerminoPartida.Normal,
            Justificativa: "Correcao auditada")).IsValid.Should().BeFalse(
            "picks cannot mask a supplied result missing ladoVencedorId");
    }

    [Fact]
    public void Validators_ShouldExposeOnlyStableLocalizedMessageCodes()
    {
        var failures = new[]
        {
            new CreateSeriesRequestDtoValidator().Validate(DailyRequest(sideIds: [])).Errors,
            new ReasonRequestDtoValidator().Validate(new ReasonRequestDto(null)).Errors,
            new RegisterPicksRequestDtoValidator().Validate(new RegisterPicksRequestDto([])).Errors,
            new ConfirmResultRequestDtoValidator().Validate(new ConfirmResultRequestDto(Guid.Empty, default)).Errors,
            new RemakeRequestDtoValidator().Validate(new RemakeRequestDto(default, null)).Errors,
            new AnnulMatchRequestDtoValidator().Validate(new AnnulMatchRequestDto(null, default)).Errors,
            new CorrectMatchRequestDtoValidator().Validate(new CorrectMatchRequestDto()).Errors,
        }.SelectMany(errors => errors)
            .Select(failure => failure.ErrorMessage)
            .ToArray();

        failures.Should().NotBeEmpty();
        var knownCodes = typeof(MessageCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(field => field.GetValue(null) as string)
            .Where(code => code is not null)
            .ToHashSet();
        failures.Where(message => !knownCodes.Contains(message)).Should().BeEmpty();
    }

    public static TheoryData<Type> HttpRequestDtoTypes => new()
    {
        typeof(CreateSeriesRequestDto),
        typeof(ReasonRequestDto),
        typeof(RegisterPicksRequestDto),
        typeof(MatchSidePicksRequestDto),
        typeof(ConfirmResultRequestDto),
        typeof(RemakeRequestDto),
        typeof(AnnulMatchRequestDto),
        typeof(CorrectMatchRequestDto),
    };

    private static IEnumerable<PropertyInfo> RequestDtoProperties() => RequestTypes
        .SelectMany(type => type.GetProperties());

    private static CreateSeriesRequestDto DailyRequest(
        Optional<DateOnly?>? dataLocal = null,
        IReadOnlyCollection<Guid>? sideIds = null) => new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            EventoId: null,
            SerieTipo.DiariaTemporaria,
            new DateTimeOffset(2026, 7, 29, 2, 30, 0, TimeSpan.Zero),
            dataLocal ?? new Optional<DateOnly?>(new DateOnly(2026, 7, 28)),
            Guid.NewGuid(),
            sideIds ?? [Guid.NewGuid(), Guid.NewGuid()]);

    private static RegisterPicksRequestDto PicksRequest(int firstSideChampion, int secondSideChampion) => new(
    [
        new MatchSidePicksRequestDto(Guid.NewGuid(), Enumerable.Range(firstSideChampion, 5).ToArray()),
        new MatchSidePicksRequestDto(Guid.NewGuid(), Enumerable.Range(secondSideChampion, 5).ToArray()),
    ]);

    private static string EnumRequestJson(Type requestType, string property, string value)
    {
        if (requestType == typeof(CreateSeriesRequestDto))
        {
            return $$"""
                {
                  "seasonId":"{{Guid.NewGuid()}}",
                  "versaoRegrasId":"{{Guid.NewGuid()}}",
                  "{{property}}":"{{value}}",
                  "agendadaPara":"2026-07-29T02:30:00Z",
                  "ladoOrigemIds":["{{Guid.NewGuid()}}","{{Guid.NewGuid()}}"]
                }
                """;
        }

        if (requestType == typeof(ConfirmResultRequestDto))
        {
            return $$"""{"ladoVencedorId":"{{Guid.NewGuid()}}","{{property}}":"{{value}}"}""";
        }

        if (requestType == typeof(RemakeRequestDto))
        {
            return $$"""{"{{property}}":"{{value}}","justificativa":"Falha valida"}""";
        }

        return $$"""{"ladoVencedorId":"{{Guid.NewGuid()}}","{{property}}":"{{value}}","justificativa":"Correcao auditada"}""";
    }

    private static bool IsKnownMessageCode(string message) =>
        typeof(MessageCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Any(field => string.Equals(field.GetValue(null) as string, message, StringComparison.Ordinal));

    private static readonly Type[] RequestTypes =
    [
        typeof(CreateSeriesRequestDto),
        typeof(ReasonRequestDto),
        typeof(RegisterPicksRequestDto),
        typeof(MatchSidePicksRequestDto),
        typeof(ConfirmResultRequestDto),
        typeof(RemakeRequestDto),
        typeof(AnnulMatchRequestDto),
        typeof(CorrectMatchRequestDto),
    ];
}
