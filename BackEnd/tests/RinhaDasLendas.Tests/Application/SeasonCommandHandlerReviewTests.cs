using System.Net;
using System.Text.Json;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RinhaDasLendas.Api.Filters;
using RinhaDasLendas.Application.Commands.Seasons;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Handlers.Seasons;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Security;
using RinhaDasLendas.Application.Validators;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Repositories;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.ValueObjects;

namespace RinhaDasLendas.Tests.Application;

public sealed class SeasonCommandHandlerReviewTests
{
    private static readonly Guid ActorId = Guid.Parse("10000000-0000-4000-8000-000000000001");
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-08-02T12:00:00Z");

    [Fact]
    public void EverySeasonHandler_ShouldRequireAuthorizationAndAudit()
    {
        var handlerTypes = new[]
        {
            typeof(CreateSeasonCommandHandler),
            typeof(UpdateSeasonCommandHandler),
            typeof(AtivarSeasonCommandHandler),
            typeof(EncerrarSeasonCommandHandler)
        };

        foreach (var handlerType in handlerTypes)
        {
            var constructor = handlerType.GetConstructors().Should().ContainSingle().Subject;
            var dependencies = constructor.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
            dependencies.Should().Contain(typeof(ICompetitiveAuthorizationService));
            dependencies.Should().Contain(typeof(ICompetitiveAuditRepository));
        }
    }

    [Fact]
    public async Task CreateSeason_WhenAuthorizationIsDenied_ShouldNotReadOrMutatePersistence()
    {
        var repository = new Mock<ICalendarioCompetitivoRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<ICompetitiveUnitOfWork>(MockBehavior.Strict);
        var audit = new Mock<ICompetitiveAuditRepository>(MockBehavior.Strict);
        var authorization = new Mock<ICompetitiveAuthorizationService>();
        CompetitiveAuthorizationContext? observedContext = null;
        authorization
            .Setup(service => service.AuthorizeAsync(It.IsAny<CompetitiveAuthorizationContext>(), It.IsAny<CancellationToken>()))
            .Callback<CompetitiveAuthorizationContext, CancellationToken>((context, _) => observedContext = context)
            .ReturnsAsync(false);
        var handler = new CreateSeasonCommandHandler(
            repository.Object,
            unitOfWork.Object,
            new Actor(ActorId),
            authorization.Object,
            audit.Object,
            new Clock(Now),
            new CreateSeasonCommandValidator(new CreateSeasonRequestDtoValidator()));
        var command = new CreateSeasonCommand(new CreateSeasonRequestDto(
            "Season protegida", 2026, 1, new DateOnly(2026, 1, 1), new DateOnly(2026, 5, 1)));

        var act = () => handler.Handle(command, CancellationToken.None);

        var exception = (await act.Should().ThrowAsync<DomainException>()).Which;
        exception.MessageCode.Should().Be(MessageCodes.CompetitiveAccessDenied);
        observedContext.Should().Be(new CompetitiveAuthorizationContext(
            AuthPermissions.CanManageSeasons,
            "CriarSeason",
            nameof(Season),
            null,
            null,
            false,
            false));
        repository.VerifyNoOtherCalls();
        audit.VerifyNoOtherCalls();
        unitOfWork.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateSeason_WhenYearOrderAlreadyExists_ShouldUseStableConflictCode()
    {
        var repository = PermissiveCalendarRepository();
        repository.Setup(item => item.ExistsSeasonOrderAsync(
                2026, 1, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var handler = new CreateSeasonCommandHandler(
            repository.Object,
            Mock.Of<ICompetitiveUnitOfWork>(),
            new Actor(ActorId),
            AllowedAuthorization().Object,
            Mock.Of<ICompetitiveAuditRepository>(),
            new Clock(Now),
            new CreateSeasonCommandValidator(new CreateSeasonRequestDtoValidator()));
        var command = new CreateSeasonCommand(new CreateSeasonRequestDto(
            "Season duplicada", 2026, 1, new DateOnly(2026, 1, 1), new DateOnly(2026, 5, 1)));

        var action = () => handler.Handle(command, CancellationToken.None);

        var exception = (await action.Should().ThrowAsync<DomainException>()).Which;
        exception.MessageCode.Should().Be(MessageCodes.SeasonOrderConflict);
    }

    [Fact]
    public async Task UpdateSeason_WithPartialPatch_ShouldPreserveOmittedValuesAndAuditBeforeSingleSave()
    {
        var season = NewSeason();
        var repository = PermissiveCalendarRepository();
        repository.Setup(item => item.GetSeasonByIdAsync(season.Id, It.IsAny<CancellationToken>())).ReturnsAsync(season);
        repository.Setup(item => item.ListSeasonsAsync(null, null, 1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync([season]);
        var series = new Mock<ISerieRepository>();
        series.Setup(item => item.HasConfirmedSeriesOutsidePeriodAsync(
                season.Id, season.DataInicio, season.DataFimExclusiva, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var authorization = AllowedAuthorization();
        var audit = new Mock<ICompetitiveAuditRepository>();
        RegistroAuditoriaCompetitiva? savedAudit = null;
        var unitOfWork = new Mock<ICompetitiveUnitOfWork>();
        var sequence = new MockSequence();
        audit.InSequence(sequence)
            .Setup(item => item.AddAsync(It.IsAny<RegistroAuditoriaCompetitiva>(), It.IsAny<CancellationToken>()))
            .Callback<RegistroAuditoriaCompetitiva, CancellationToken>((record, _) => savedAudit = record)
            .Returns(Task.CompletedTask);
        unitOfWork.InSequence(sequence)
            .Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var handler = new UpdateSeasonCommandHandler(
            repository.Object,
            series.Object,
            unitOfWork.Object,
            new Actor(ActorId),
            authorization.Object,
            audit.Object,
            new Clock(Now),
            new UpdateSeasonCommandValidator(new UpdateSeasonRequestDtoValidator()));

        var result = await handler.Handle(
            new UpdateSeasonCommand(season.Id, new UpdateSeasonRequestDto(Nome: "Nome parcial"), season.Versao),
            CancellationToken.None);

        result.Nome.Should().Be("Nome parcial");
        result.Ano.Should().Be(2026);
        result.OrdemNoAno.Should().Be(1);
        result.DataInicio.Should().Be(new DateOnly(2026, 1, 1));
        result.DataFimExclusiva.Should().Be(new DateOnly(2026, 5, 1));
        result.AtualizadaPorUsuarioId.Should().Be(ActorId);
        savedAudit!.ValorAnterior!.Campos[CampoSnapshotAuditoria.Nome].Should().Be("Season 2026-1");
        savedAudit.ValorAnterior.Campos[CampoSnapshotAuditoria.Ano].Should().Be(2026);
        savedAudit.ValorPosterior!.Campos[CampoSnapshotAuditoria.Nome].Should().Be("Nome parcial");
        savedAudit.ValorPosterior.Campos[CampoSnapshotAuditoria.Ano].Should().Be(2026);
        audit.Verify(item => item.AddAsync(
            It.Is<RegistroAuditoriaCompetitiva>(record => record.AtorUsuarioId == ActorId),
            It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateSeason_ShouldValidateChronologyBeyondFirstHundredSeasons()
    {
        var repository = PermissiveCalendarRepository();
        var firstPage = Enumerable.Range(0, 100)
            .Select(index => NewSeason(3000 + index, 1, new DateOnly(3000 + index, 1, 1), new DateOnly(3001 + index, 1, 1)))
            .ToArray();
        var chronologicallyPrevious = NewSeason(
            2026,
            1,
            new DateOnly(2026, 3, 1),
            new DateOnly(2026, 4, 1));
        repository.Setup(item => item.ListSeasonsAsync(null, null, 1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(firstPage);
        repository.Setup(item => item.ListSeasonsAsync(null, null, 2, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync([chronologicallyPrevious]);
        var unitOfWork = new Mock<ICompetitiveUnitOfWork>(MockBehavior.Strict);
        var audit = new Mock<ICompetitiveAuditRepository>(MockBehavior.Strict);
        var handler = new CreateSeasonCommandHandler(
            repository.Object,
            unitOfWork.Object,
            new Actor(ActorId),
            AllowedAuthorization().Object,
            audit.Object,
            new Clock(Now),
            new CreateSeasonCommandValidator(new CreateSeasonRequestDtoValidator()));
        var command = new CreateSeasonCommand(new CreateSeasonRequestDto(
            "Segunda Season", 2026, 2, new DateOnly(2026, 2, 1), new DateOnly(2026, 3, 1)));

        var act = () => handler.Handle(command, CancellationToken.None);

        var exception = (await act.Should().ThrowAsync<DomainException>()).Which;
        exception.MessageCode.Should().Be(MessageCodes.ValidationError);
        repository.Verify(item => item.ListSeasonsAsync(null, null, 2, 100, It.IsAny<CancellationToken>()), Times.Once);
        audit.VerifyNoOtherCalls();
        unitOfWork.VerifyNoOtherCalls();
    }

    [Fact]
    public void UpdateSeasonValidator_ShouldRequireAtLeastOnePatchProperty()
    {
        var validator = new UpdateSeasonCommandValidator(new UpdateSeasonRequestDtoValidator());

        validator.Validate(new UpdateSeasonCommand(Guid.NewGuid(), new UpdateSeasonRequestDto(), 0)).IsValid.Should().BeFalse();
        validator.Validate(new UpdateSeasonCommand(Guid.NewGuid(), new UpdateSeasonRequestDto(Ano: 2027), 0)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void SeasonAuditSnapshot_ShouldValidateTypedNameAndYear()
    {
        var valid = SnapshotAuditoriaRedigido.Criar(new Dictionary<CampoSnapshotAuditoria, object?>
        {
            [CampoSnapshotAuditoria.Nome] = "  Season segura  ",
            [CampoSnapshotAuditoria.Ano] = 2026
        });

        valid.Campos[CampoSnapshotAuditoria.Nome].Should().Be("Season segura");
        valid.Campos[CampoSnapshotAuditoria.Ano].Should().Be(2026);
        var invalidName = () => SnapshotAuditoriaRedigido.Criar(new Dictionary<CampoSnapshotAuditoria, object?>
        {
            [CampoSnapshotAuditoria.Nome] = new string('S', 121)
        });
        var invalidYear = () => SnapshotAuditoriaRedigido.Criar(new Dictionary<CampoSnapshotAuditoria, object?>
        {
            [CampoSnapshotAuditoria.Ano] = 2008
        });
        invalidName.Should().Throw<DomainException>();
        invalidYear.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(MessageCodes.CompetitiveCalendarVersionStale)]
    [InlineData(MessageCodes.CompetitiveResourceVersionStale)]
    public async Task ApiExceptionMiddleware_ShouldMapCompetitiveConcurrencyToLocalizedConflict(string messageCode)
    {
        var messages = new Mock<IMessageProvider>();
        messages.Setup(item => item.GetMessage(messageCode)).Returns($"localized-{messageCode}");
        var middleware = new ApiExceptionMiddleware(
            _ => throw new DomainException(messageCode),
            NullLogger<ApiExceptionMiddleware>.Instance,
            messages.Object);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.Conflict);
        context.Response.Body.Position = 0;
        var response = await JsonSerializer.DeserializeAsync<ApiErrorResponse>(
            context.Response.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        response!.MessageCode.Should().Be(messageCode);
        response.Message.Should().Be($"localized-{messageCode}");
    }

    [Fact]
    public async Task ApiExceptionMiddleware_ShouldMapSeasonNotFoundToLocalizedNotFound()
    {
        var messages = new Mock<IMessageProvider>();
        messages.Setup(item => item.GetMessage(MessageCodes.SeasonNotFound)).Returns("Temporada não encontrada");
        var middleware = new ApiExceptionMiddleware(
            _ => throw new DomainException(MessageCodes.SeasonNotFound),
            NullLogger<ApiExceptionMiddleware>.Instance,
            messages.Object);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        context.Response.Body.Position = 0;
        var response = await JsonSerializer.DeserializeAsync<ApiErrorResponse>(
            context.Response.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        response!.MessageCode.Should().Be(MessageCodes.SeasonNotFound);
        response.Message.Should().Be("Temporada não encontrada");
    }

    private static Mock<ICalendarioCompetitivoRepository> PermissiveCalendarRepository()
    {
        var repository = new Mock<ICalendarioCompetitivoRepository>();
        repository.Setup(item => item.ExistsOverlappingSeasonAsync(
                It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository.Setup(item => item.ExistsSeasonOrderAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        return repository;
    }

    private static Mock<ICompetitiveAuthorizationService> AllowedAuthorization()
    {
        var authorization = new Mock<ICompetitiveAuthorizationService>();
        authorization.Setup(item => item.AuthorizeAsync(
                It.IsAny<CompetitiveAuthorizationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        return authorization;
    }

    private static Season NewSeason(
        int year = 2026,
        int order = 1,
        DateOnly? start = null,
        DateOnly? exclusiveEnd = null) =>
        new(
            $"Season {year}-{order}",
            year,
            order,
            start ?? new DateOnly(year, 1, 1),
            exclusiveEnd ?? new DateOnly(year, 5, 1),
            ActorId,
            Now.AddDays(-1));

    private sealed record Actor(Guid? UserId) : ICurrentActor
    {
        public IReadOnlyCollection<string> Roles => [AuthRoles.Presidente];
    }

    private sealed record Clock(DateTimeOffset UtcNow) : ISystemClock;
}
