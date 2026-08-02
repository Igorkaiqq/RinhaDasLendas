using FluentAssertions;
using FluentValidation;
using Moq;
using System.Text.Json;
using System.Text.Json.Serialization;
using RinhaDasLendas.Application.Commands.Competicoes;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Handlers.Competicoes;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Security;
using RinhaDasLendas.Application.Validators;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Tests.Application;

public sealed class CompetitionCommandHandlerTests
{
    private static readonly DateTimeOffset Agora = new(2026, 7, 30, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CriarCompeticao_DeveAutorizarComEstadoConfiavelAuditarESalvarUmaVez()
    {
        var fixture = new HandlerFixture();
        var season = fixture.NovaSeason();
        CompetitiveAuthorizationContext? authorizationContext = null;
        fixture.CalendarRepository
            .Setup(repository => repository.GetSeasonByIdAsync(season.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(season);
        fixture.SeriesRepository
            .Setup(repository => repository.HasStartedSeriesAsync(season.Id, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        fixture.Authorization
            .Setup(service => service.AuthorizeAsync(It.IsAny<CompetitiveAuthorizationContext>(), It.IsAny<CancellationToken>()))
            .Callback<CompetitiveAuthorizationContext, CancellationToken>((context, _) => authorizationContext = context)
            .ReturnsAsync(true);
        var handler = fixture.CreateCreateHandler();

        var result = await handler.Handle(
            new CreateCompetitionCommand(
                season.Id,
                new CreateCompetitionRequestDto(" Circuito Diário ", " CD ", true)),
            CancellationToken.None);

        result.Nome.Should().Be("Circuito Diário");
        result.Codigo.Should().Be("CD");
        authorizationContext.Should().Be(new CompetitiveAuthorizationContext(
            AuthPermissions.CanManageCompetitions,
            "ConfigurarCompeticao",
            "Competicao",
            null,
            SeasonEstado.Planejada.ToString(),
            false,
            false));
        fixture.CompetitionRepository.Verify(
            repository => repository.AddAsync(result, It.IsAny<CancellationToken>()),
            Times.Once);
        fixture.AuditRepository.Verify(repository => repository.AddAsync(
            It.Is<RegistroAuditoriaCompetitiva>(audit =>
                audit.RecursoTipo == RecursoCompetitivoTipo.Competicao
                && audit.RecursoId == result.Id
                && audit.Acao == AcaoAuditoriaCompetitiva.CompeticaoCriada
                && audit.Capacidade == AuthPermissions.CanManageCompetitions
                && audit.ValorAnterior == null
                && audit.ValorPosterior != null),
            It.IsAny<CancellationToken>()), Times.Once);
        fixture.UnitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CriarCompeticao_ComCodigoDuplicadoProativo_DeveRetornarMesmoConflitoDaCorrida()
    {
        var fixture = new HandlerFixture();
        var season = fixture.NovaSeason();
        fixture.CalendarRepository
            .Setup(repository => repository.GetSeasonByIdAsync(season.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(season);
        fixture.SeriesRepository
            .Setup(repository => repository.HasStartedSeriesAsync(season.Id, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        fixture.Authorization
            .Setup(service => service.AuthorizeAsync(It.IsAny<CompetitiveAuthorizationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        fixture.CompetitionRepository
            .Setup(repository => repository.ExistsCodeAsync(season.Id, "COPA", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => fixture.CreateCreateHandler().Handle(
            new CreateCompetitionCommand(
                season.Id,
                new CreateCompetitionRequestDto("Copa", "COPA", false)),
            CancellationToken.None);

        (await act.Should().ThrowAsync<DomainException>()).Which.MessageCode
            .Should().Be(MessageCodes.CompetitionCodeConflict);
        fixture.UnitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AtualizarCompeticao_ComETagObsoleta_DeveRecusarSemMutacaoAuditoriaOuSave()
    {
        var fixture = new HandlerFixture();
        var season = fixture.NovaSeason();
        var competition = fixture.NovaCompeticao(season.Id);
        fixture.CalendarRepository
            .Setup(repository => repository.GetSeasonByIdAsync(season.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(season);
        fixture.CompetitionRepository
            .Setup(repository => repository.GetWithRoundsAndRulesAsync(competition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(competition);
        fixture.SeriesRepository
            .Setup(repository => repository.HasStartedSeriesAsync(season.Id, competition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        fixture.Authorization
            .Setup(service => service.AuthorizeAsync(It.IsAny<CompetitiveAuthorizationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var handler = fixture.CreateUpdateHandler();

        var act = () => handler.Handle(
            new UpdateCompetitionCommand(
                competition.Id,
                new UpdateCompetitionRequestDto(Nome: "Nome alterado"),
                ExpectedVersion: competition.Versao + 1),
            CancellationToken.None);

        var exception = (await act.Should().ThrowAsync<DomainException>()).Which;
        exception.MessageCode.Should().Be(MessageCodes.CompetitiveResourceVersionStale);
        competition.Nome.Should().Be("Competição");
        fixture.AuditRepository.Verify(
            repository => repository.AddAsync(It.IsAny<RegistroAuditoriaCompetitiva>(), It.IsAny<CancellationToken>()),
            Times.Never);
        fixture.UnitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReordenarRodadas_DeveAlterarTodoAgregadoAuditarUmaVezESalvarUmaVez()
    {
        var fixture = new HandlerFixture();
        var season = fixture.NovaSeason();
        var competition = fixture.NovaCompeticao(season.Id);
        var first = competition.AdicionarRodada("Primeira", 1, Agora);
        var second = competition.AdicionarRodada("Segunda", 2, Agora);
        fixture.SetupCompetitionScope(season, competition);
        var handler = fixture.CreateReorderHandler();

        var result = await handler.Handle(
            new ReorderRoundsCommand(
                competition.Id,
                new ReorderRoundsRequestDto([second.Id, first.Id]),
                competition.Versao),
            CancellationToken.None);

        result.Select(round => round.Id).Should().Equal(second.Id, first.Id);
        result.Select(round => round.Ordem).Should().Equal(1, 2);
        fixture.AuditRepository.Verify(repository => repository.AddAsync(
            It.Is<RegistroAuditoriaCompetitiva>(audit =>
                audit.Acao == AcaoAuditoriaCompetitiva.RodadasReordenadas
                && audit.RecursoTipo == RecursoCompetitivoTipo.Competicao
                && audit.ValorAnterior != null
                && audit.ValorPosterior != null),
            It.IsAny<CancellationToken>()), Times.Once);
        fixture.UnitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CriarRodada_DeveUsarCondicoesDoEscopoConfiavelAuditarESalvarUmaVez()
    {
        var fixture = new HandlerFixture();
        var season = fixture.NovaSeason();
        var competition = fixture.NovaCompeticao(season.Id);
        fixture.SetupCompetitionScope(season, competition);
        var handler = fixture.CreateRoundHandler();

        var result = await handler.Handle(
            new CreateRoundCommand(
                competition.Id,
                new CreateRoundRequestDto("Final", 1),
                competition.Versao),
            CancellationToken.None);

        result.CompeticaoId.Should().Be(competition.Id);
        competition.Rodadas.Should().ContainSingle().Which.Should().BeSameAs(result);
        fixture.CompetitionRepository.Verify(
            repository => repository.AddRoundAsync(result, It.IsAny<CancellationToken>()), Times.Once);
        fixture.Authorization.Verify(service => service.AuthorizeAsync(
            It.Is<CompetitiveAuthorizationContext>(context =>
                context.Capability == AuthPermissions.CanManageCompetitions
                && context.Operation == "ConfigurarRodada"
                && context.ResourceType == "Rodada"
                && context.SeasonState == SeasonEstado.Planejada.ToString()
                && !context.HasStartedSeries),
            It.IsAny<CancellationToken>()), Times.Once);
        fixture.AuditRepository.Verify(repository => repository.AddAsync(
            It.Is<RegistroAuditoriaCompetitiva>(audit =>
                audit.RecursoTipo == RecursoCompetitivoTipo.Rodada
                && audit.RecursoId == result.Id
                && audit.Acao == AcaoAuditoriaCompetitiva.RodadaCriada),
            It.IsAny<CancellationToken>()), Times.Once);
        fixture.UnitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CriarRodada_ComETagObsoleta_DeveRecusarSemMutacaoAuditoriaOuSave()
    {
        var fixture = new HandlerFixture();
        var season = fixture.NovaSeason();
        var competition = fixture.NovaCompeticao(season.Id);
        fixture.SetupCompetitionScope(season, competition);
        var handler = fixture.CreateRoundHandler();

        var act = () => handler.Handle(
            new CreateRoundCommand(
                competition.Id,
                new CreateRoundRequestDto("Final", 1),
                competition.Versao + 1),
            CancellationToken.None);

        var exception = (await act.Should().ThrowAsync<DomainException>()).Which;
        exception.MessageCode.Should().Be(MessageCodes.CompetitiveResourceVersionStale);
        competition.Rodadas.Should().BeEmpty();
        fixture.AuditRepository.Verify(
            repository => repository.AddAsync(It.IsAny<RegistroAuditoriaCompetitiva>(), It.IsAny<CancellationToken>()),
            Times.Never);
        fixture.UnitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CriarRodada_ComOrdemDuplicadaProativa_DeveRetornarMesmoConflitoDaCorrida()
    {
        var fixture = new HandlerFixture();
        var season = fixture.NovaSeason();
        var competition = fixture.NovaCompeticao(season.Id);
        competition.AdicionarRodada("Existente", 1, Agora);
        fixture.SetupCompetitionScope(season, competition);

        var act = () => fixture.CreateRoundHandler().Handle(
            new CreateRoundCommand(
                competition.Id,
                new CreateRoundRequestDto("Duplicada", 1),
                competition.Versao),
            CancellationToken.None);

        (await act.Should().ThrowAsync<DomainException>()).Which.MessageCode
            .Should().Be(MessageCodes.RoundOrderConflict);
        fixture.AuditRepository.Verify(
            repository => repository.AddAsync(It.IsAny<RegistroAuditoriaCompetitiva>(), It.IsAny<CancellationToken>()),
            Times.Never);
        fixture.UnitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PublicarRegrasGeraisEDaCompeticao_DeveManterSequenciasEEscoposIndependentes()
    {
        var fixture = new HandlerFixture();
        var season = fixture.NovaSeason();
        var competition = fixture.NovaCompeticao(season.Id);
        fixture.Authorization
            .Setup(service => service.AuthorizeAsync(
                It.Is<CompetitiveAuthorizationContext>(context =>
                    context.Operation == "Publicar" && context.ResourceType == "Regra"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        fixture.CalendarRepository
            .Setup(repository => repository.GetSeasonByIdAsync(season.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(season);
        fixture.CompetitionRepository
            .Setup(repository => repository.GetNextGeneralRulesNumberAsync(season.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        fixture.CompetitionRepository
            .Setup(repository => repository.GetWithRoundsAndRulesAsync(competition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(competition);
        var generalHandler = fixture.CreatePublishSeasonRulesHandler();
        var competitionHandler = fixture.CreatePublishCompetitionRulesHandler();

        var general = await generalHandler.Handle(
            new PublishSeasonRulesCommand(
                season.Id,
                new PublishRulesRequestDto(SerieFormato.Md3, ModoDraft.Padrao),
                season.Versao),
            CancellationToken.None);
        var specific = await competitionHandler.Handle(
            new PublishCompetitionRulesCommand(
                competition.Id,
                new PublishRulesRequestDto(SerieFormato.Md5, ModoDraft.Fearless),
                competition.Versao),
            CancellationToken.None);

        general.Numero.Should().Be(1);
        general.CompeticaoId.Should().BeNull();
        specific.Numero.Should().Be(1);
        specific.CompeticaoId.Should().Be(competition.Id);
        fixture.CompetitionRepository.Verify(
            repository => repository.AddRulesVersionAsync(general, It.IsAny<CancellationToken>()), Times.Once);
        fixture.CompetitionRepository.Verify(
            repository => repository.AddRulesVersionAsync(specific, It.IsAny<CancellationToken>()), Times.Once);
        fixture.AuditRepository.Verify(
            repository => repository.AddAsync(
                It.Is<RegistroAuditoriaCompetitiva>(audit =>
                    audit.Acao == AcaoAuditoriaCompetitiva.RegrasGeraisSeasonPublicadas),
                It.IsAny<CancellationToken>()),
            Times.Once);
        fixture.AuditRepository.Verify(
            repository => repository.AddAsync(
                It.Is<RegistroAuditoriaCompetitiva>(audit =>
                    audit.Acao == AcaoAuditoriaCompetitiva.RegrasCompeticaoPublicadas),
                It.IsAny<CancellationToken>()),
            Times.Once);
        fixture.UnitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public void Handlers_NaoDevemAssumirIdempotenciaDoMiddleware()
    {
        var handlerTypes = new[]
        {
            typeof(CreateCompetitionCommandHandler),
            typeof(UpdateCompetitionCommandHandler),
            typeof(CreateRoundCommandHandler),
            typeof(ReorderRoundsCommandHandler),
            typeof(PublishSeasonRulesCommandHandler),
            typeof(PublishCompetitionRulesCommandHandler)
        };

        handlerTypes.SelectMany(type => type.GetConstructors().SelectMany(constructor => constructor.GetParameters()))
            .Should().NotContain(parameter => parameter.ParameterType == typeof(IIdempotencyService));
    }

    [Fact]
    public void Validators_DevemUsarCodigosEstaveisParaTodosOsContratosDeEscrita()
    {
        var results = new[]
        {
            new CreateCompetitionRequestDtoValidator().Validate(new CreateCompetitionRequestDto("", "", false)),
            new UpdateCompetitionRequestDtoValidator().Validate(new UpdateCompetitionRequestDto()),
            new CreateRoundRequestDtoValidator().Validate(new CreateRoundRequestDto("", 0)),
            new ReorderRoundsRequestDtoValidator().Validate(new ReorderRoundsRequestDto([Guid.Empty, Guid.Empty])),
            new PublishRulesRequestDtoValidator().Validate(
                new PublishRulesRequestDto((SerieFormato)999, (ModoDraft)999))
        };

        results.Should().OnlyContain(result => !result.IsValid);
        results.SelectMany(result => result.Errors)
            .Should().OnlyContain(error => error.ErrorMessage.StartsWith("M", StringComparison.Ordinal));
    }

    [Fact]
    public void UpdateCompetitionPatch_DeveDistinguirCampoOmitidoDeNullExplicito()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var omitted = JsonSerializer.Deserialize<UpdateCompetitionRequestDto>("{}", options);
        var explicitNull = JsonSerializer.Deserialize<UpdateCompetitionRequestDto>("{\"nome\":null}", options);
        var present = JsonSerializer.Deserialize<UpdateCompetitionRequestDto>("{\"nome\":\"Novo nome\"}", options);
        var validator = new UpdateCompetitionRequestDtoValidator();

        omitted!.Nome.HasValue.Should().BeFalse();
        explicitNull!.Nome.HasValue.Should().BeTrue();
        explicitNull.Nome.Value.Should().BeNull();
        present!.Nome.HasValue.Should().BeTrue();
        present.Nome.Value.Should().Be("Novo nome");
        validator.Validate(omitted).IsValid.Should().BeFalse();
        validator.Validate(explicitNull).IsValid.Should().BeFalse();
        validator.Validate(present).IsValid.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(HttpBoundRequestDtos))]
    public void HttpBoundRequestDto_DeveDesserializarPayloadValido(Type requestType, string validJson)
    {
        var options = HttpJsonOptions();

        JsonSerializer.Deserialize(validJson, requestType, options).Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(HttpBoundRequestDtos))]
    public void HttpBoundRequestDto_DeveRecusarPropriedadeJsonDesconhecidaSemPoliticaGlobal(
        Type requestType,
        string validJson)
    {
        var options = HttpJsonOptions();
        var jsonWithUnknownProperty = validJson[..^1] + ",\"inesperado\":true}";

        var act = () => JsonSerializer.Deserialize(jsonWithUnknownProperty, requestType, options);

        act.Should().Throw<JsonException>();
        options.UnmappedMemberHandling.Should().Be(JsonUnmappedMemberHandling.Skip);
    }

    public static TheoryData<Type, string> HttpBoundRequestDtos => new()
    {
        { typeof(CreateCompetitionRequestDto), "{\"nome\":\"Copa\",\"codigo\":\"COPA\",\"circuitoDiario\":false}" },
        { typeof(UpdateCompetitionRequestDto), "{\"nome\":\"Novo nome\"}" },
        { typeof(CreateRoundRequestDto), "{\"nome\":\"Final\",\"ordem\":1}" },
        { typeof(ReorderRoundsRequestDto), "{\"rodadaIds\":[\"00000000-0000-4000-8000-000000000001\"]}" },
        { typeof(PublishRulesRequestDto), "{\"formato\":\"Md3\",\"modoDraft\":\"Padrao\"}" }
    };

    private static JsonSerializerOptions HttpJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private sealed class HandlerFixture
    {
        public Mock<ICalendarioCompetitivoRepository> CalendarRepository { get; } = new();
        public Mock<ICompeticaoRepository> CompetitionRepository { get; } = new();
        public Mock<ISerieRepository> SeriesRepository { get; } = new();
        public Mock<ICompetitiveUnitOfWork> UnitOfWork { get; } = new();
        public Mock<ICompetitiveAuthorizationService> Authorization { get; } = new();
        public Mock<ICompetitiveAuditRepository> AuditRepository { get; } = new();
        public Mock<ICurrentActor> Actor { get; } = new();
        public Mock<ISystemClock> Clock { get; } = new();

        public HandlerFixture()
        {
            Actor.SetupGet(actor => actor.UserId).Returns(Guid.NewGuid());
            Actor.SetupGet(actor => actor.Roles).Returns([]);
            Clock.SetupGet(clock => clock.UtcNow).Returns(Agora);
        }

        public Season NovaSeason() => new(
            "Season",
            2026,
            1,
            new DateOnly(2026, 1, 1),
            new DateOnly(2027, 1, 1),
            Actor.Object.UserId!.Value,
            Agora);

        public Competicao NovaCompeticao(Guid seasonId) => new(
            seasonId,
            "Competição",
            "COMP",
            false,
            Actor.Object.UserId!.Value,
            Agora);

        public void SetupCompetitionScope(Season season, Competicao competition)
        {
            CalendarRepository
                .Setup(repository => repository.GetSeasonByIdAsync(season.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(season);
            CompetitionRepository
                .Setup(repository => repository.GetWithRoundsAndRulesAsync(competition.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(competition);
            SeriesRepository
                .Setup(repository => repository.HasStartedSeriesAsync(season.Id, competition.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            Authorization
                .Setup(service => service.AuthorizeAsync(It.IsAny<CompetitiveAuthorizationContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
        }

        public CreateCompetitionCommandHandler CreateCreateHandler() => new(
            CalendarRepository.Object,
            CompetitionRepository.Object,
            SeriesRepository.Object,
            UnitOfWork.Object,
            Actor.Object,
            Authorization.Object,
            AuditRepository.Object,
            Clock.Object,
            new CreateCompetitionCommandValidator(new CreateCompetitionRequestDtoValidator()));

        public UpdateCompetitionCommandHandler CreateUpdateHandler() => new(
            CalendarRepository.Object,
            CompetitionRepository.Object,
            SeriesRepository.Object,
            UnitOfWork.Object,
            Actor.Object,
            Authorization.Object,
            AuditRepository.Object,
            Clock.Object,
            new UpdateCompetitionCommandValidator(new UpdateCompetitionRequestDtoValidator()));

        public ReorderRoundsCommandHandler CreateReorderHandler() => new(
            CalendarRepository.Object,
            CompetitionRepository.Object,
            SeriesRepository.Object,
            UnitOfWork.Object,
            Actor.Object,
            Authorization.Object,
            AuditRepository.Object,
            Clock.Object,
            new ReorderRoundsCommandValidator(new ReorderRoundsRequestDtoValidator()));

        public CreateRoundCommandHandler CreateRoundHandler() => new(
            CalendarRepository.Object,
            CompetitionRepository.Object,
            SeriesRepository.Object,
            UnitOfWork.Object,
            Actor.Object,
            Authorization.Object,
            AuditRepository.Object,
            Clock.Object,
            new CreateRoundCommandValidator(new CreateRoundRequestDtoValidator()));

        public PublishSeasonRulesCommandHandler CreatePublishSeasonRulesHandler() => new(
            CalendarRepository.Object,
            CompetitionRepository.Object,
            UnitOfWork.Object,
            Actor.Object,
            Authorization.Object,
            AuditRepository.Object,
            Clock.Object,
            new PublishSeasonRulesCommandValidator(new PublishRulesRequestDtoValidator()));

        public PublishCompetitionRulesCommandHandler CreatePublishCompetitionRulesHandler() => new(
            CalendarRepository.Object,
            CompetitionRepository.Object,
            UnitOfWork.Object,
            Actor.Object,
            Authorization.Object,
            AuditRepository.Object,
            Clock.Object,
            new PublishCompetitionRulesCommandValidator(new PublishRulesRequestDtoValidator()));
    }
}
