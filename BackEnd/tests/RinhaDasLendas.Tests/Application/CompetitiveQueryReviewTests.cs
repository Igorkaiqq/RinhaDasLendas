using FluentAssertions;
using FluentValidation;
using Moq;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Queries.Competicoes;
using RinhaDasLendas.Application.Queries.Seasons;
using RinhaDasLendas.Application.Validators;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Repositories;
using RinhaDasLendas.Domain.Rules;

namespace RinhaDasLendas.Tests.Application;

public sealed class CompetitiveQueryReviewTests
{
    [Theory]
    [InlineData(typeof(GetSeasonsQueryHandler))]
    [InlineData(typeof(GetSeasonByIdQueryHandler))]
    [InlineData(typeof(GetCompeticoesQueryHandler))]
    public void QueryHandlers_ShouldNotDeclareOptionalDependencies(Type handlerType)
    {
        var constructor = handlerType.GetConstructors().Should().ContainSingle().Subject;

        constructor.GetParameters().Should().OnlyContain(parameter => !parameter.IsOptional);
    }

    [Fact]
    public void CalendarRepository_ShouldExposeLightweightCalendarRead()
    {
        typeof(ICalendarioCompetitivoRepository)
            .GetMethod("GetCalendarAsync")
            .Should().NotBeNull();
    }

    [Fact]
    public async Task GetSeasons_ShouldRejectUndefinedStateThroughLocalizedValidator()
    {
        var handler = new GetSeasonsQueryHandler(
            Mock.Of<ICalendarioCompetitivoRepository>(),
            new PassthroughSnapshot(),
            new GetSeasonsQueryValidator());

        var act = () => handler.Handle(
            new GetSeasonsQuery((SeasonEstado)int.MaxValue, 1, 20),
            CancellationToken.None);

        var exception = (await act.Should().ThrowAsync<ValidationException>()).Which;
        exception.Errors.Should().ContainSingle(error => error.ErrorMessage == MessageCodes.ValidationError);
    }

    [Fact]
    public async Task GetCompetitions_ShouldRejectNullSelectionThroughLocalizedValidator()
    {
        var handler = new GetCompeticoesQueryHandler(
            Mock.Of<ICalendarioCompetitivoRepository>(),
            Mock.Of<ICompeticaoRepository>(),
            Mock.Of<ISerieRepository>(),
            Mock.Of<ICompetitiveAuthorizationService>(),
            new PassthroughSnapshot(),
            new GetCompeticoesQueryValidator(),
            new GetSeasonCompeticoesQueryValidator(),
            new GetCompeticaoByIdQueryValidator());

        var act = () => handler.Handle(
            new GetCompeticoesQuery(null!, 1, 20),
            CancellationToken.None);

        var exception = (await act.Should().ThrowAsync<ValidationException>()).Which;
        exception.Errors.Should().ContainSingle(error => error.ErrorMessage == MessageCodes.ValidationError);
    }

    [Fact]
    public async Task GetCompetitions_ShouldLoadStartedCompetitionIdsOncePerPage()
    {
        var actorId = Guid.NewGuid();
        var season = new Season(
            "Season", 2026, 1, new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1),
            actorId, DateTimeOffset.UtcNow);
        var competitions = new[]
        {
            new Competicao(season.Id, "A", "A", false, actorId, DateTimeOffset.UtcNow),
            new Competicao(season.Id, "B", "B", false, actorId, DateTimeOffset.UtcNow)
        };
        var calendarRepository = new Mock<ICalendarioCompetitivoRepository>();
        calendarRepository.Setup(item => item.GetActiveSeasonAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((Season?)null);
        calendarRepository.Setup(item => item.ListSeasonsAsync(
                It.IsAny<IReadOnlyCollection<Guid>?>(), null, 1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync([season]);
        var competitionRepository = new Mock<ICompeticaoRepository>();
        competitionRepository.Setup(item => item.ListAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(), 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(competitions);
        competitionRepository.Setup(item => item.CountAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(competitions.Length);
        var seriesRepository = new Mock<ISerieRepository>(MockBehavior.Strict);
        seriesRepository.Setup(item => item.ListCompetitionIdsWithStartedSeriesAsync(
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 2),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([competitions[0].Id]);
        var authorization = new Mock<ICompetitiveAuthorizationService>();
        authorization.Setup(item => item.GetAllowedActionsAsync(
                It.IsAny<IReadOnlyCollection<RinhaDasLendas.Application.Security.CompetitiveAuthorizationContext>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var handler = new GetCompeticoesQueryHandler(
            calendarRepository.Object,
            competitionRepository.Object,
            seriesRepository.Object,
            authorization.Object,
            new PassthroughSnapshot(),
            new GetCompeticoesQueryValidator(),
            new GetSeasonCompeticoesQueryValidator(),
            new GetCompeticaoByIdQueryValidator());

        await handler.Handle(
            new GetCompeticoesQuery(SelecaoSazonal.Especifica([season.Id]), 1, 20),
            CancellationToken.None);

        seriesRepository.Verify(item => item.ListCompetitionIdsWithStartedSeriesAsync(
            It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Once);
        seriesRepository.Verify(item => item.HasStartedSeriesAsync(
            It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task PagedQueries_ShouldRejectOutOfBoundsPageThroughLocalizedValidator(int page, int pageSize)
    {
        var handler = new GetCompeticoesQueryHandler(
            Mock.Of<ICalendarioCompetitivoRepository>(),
            Mock.Of<ICompeticaoRepository>(),
            Mock.Of<ISerieRepository>(),
            Mock.Of<ICompetitiveAuthorizationService>(),
            new PassthroughSnapshot(),
            new GetCompeticoesQueryValidator(),
            new GetSeasonCompeticoesQueryValidator(),
            new GetCompeticaoByIdQueryValidator());

        var act = () => handler.Handle(
            new GetCompeticoesQuery(SelecaoSazonal.Padrao(), page, pageSize),
            CancellationToken.None);

        var exception = (await act.Should().ThrowAsync<ValidationException>()).Which;
        exception.Errors.Should().OnlyContain(error => error.ErrorMessage == MessageCodes.ValidationError);
    }

    private sealed class PassthroughSnapshot : ICompetitiveQuerySnapshot
    {
        public Task<T> ExecuteAsync<T>(
            Func<CancellationToken, Task<T>> query,
            CancellationToken cancellationToken) => query(cancellationToken);
    }
}
