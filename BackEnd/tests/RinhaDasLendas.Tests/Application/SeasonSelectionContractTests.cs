using FluentAssertions;
using Moq;
using RinhaDasLendas.Application.Commands.Seasons;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Handlers.Competicoes;
using RinhaDasLendas.Application.Handlers.Seasons;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Queries.Competicoes;
using RinhaDasLendas.Application.Queries.Seasons;
using RinhaDasLendas.Application.Security;
using RinhaDasLendas.Application.Validators;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Repositories;
using RinhaDasLendas.Domain.Rules;

namespace RinhaDasLendas.Tests.Application;

public sealed class SeasonSelectionContractTests
{
    private static readonly DateTimeOffset Agora = new(2026, 7, 28, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AtivarSeason_EmEscoposConcorrentes_DeveEncerrarAnteriorEConfirmarUmUnicoVencedor()
    {
        var ator = new CurrentActor(Guid.NewGuid());
        var anterior = NovaSeason("Season anterior", 1, new DateOnly(2026, 1, 1), new DateOnly(2026, 4, 1), ator.UserId!.Value);
        var primeira = NovaSeason("Season 2", 2, new DateOnly(2026, 4, 1), new DateOnly(2026, 7, 1), ator.UserId.Value);
        var segunda = NovaSeason("Season 3", 3, new DateOnly(2026, 7, 1), new DateOnly(2026, 10, 1), ator.UserId.Value);
        var persistence = new ConcurrentSeasonPersistence(anterior, [primeira, segunda], ator.UserId.Value, Agora, 7);
        var primeiroEscopo = persistence.CreateRequestScope();
        var segundoEscopo = persistence.CreateRequestScope();
        primeiroEscopo.Calendar.Should().NotBeSameAs(segundoEscopo.Calendar);
        primeiroEscopo.Seasons[anterior.Id].Should().NotBeSameAs(segundoEscopo.Seasons[anterior.Id]);
        primeiroEscopo.Seasons[primeira.Id].Should().NotBeSameAs(segundoEscopo.Seasons[primeira.Id]);
        var primeiroHandler = CreateActivationHandler(primeiroEscopo, primeiroEscopo, ator);
        var segundoHandler = CreateActivationHandler(segundoEscopo, segundoEscopo, ator);

        var primeiraAtivacao = Record.ExceptionAsync(() => primeiroHandler.Handle(new AtivarSeasonCommand(primeira.Id, 7), CancellationToken.None));
        var segundaAtivacao = Record.ExceptionAsync(() => segundoHandler.Handle(new AtivarSeasonCommand(segunda.Id, 7), CancellationToken.None));
        var resultados = await Task.WhenAll(primeiraAtivacao, segundaAtivacao);

        resultados.Should().ContainSingle(item => item == null);
        var conflito = resultados.Should().ContainSingle(item => item is DomainException).Subject.Should().BeOfType<DomainException>().Subject;
        conflito.MessageCode.Should().Be(MessageCodes.CompetitiveCalendarVersionStale);
        persistence.SuccessfulCommits.Should().Be(1);
        persistence.CalendarVersion.Should().Be(8);
        persistence.GetSeason(anterior.Id).Estado.Should().Be(SeasonEstado.Encerrada);
        persistence.GetSeasons().Should().ContainSingle(item => item.Estado == SeasonEstado.Ativa);
        persistence.ActiveSeasonId.Should().Be(persistence.GetSeasons().Single(item => item.Estado == SeasonEstado.Ativa).Id);
    }

    [Fact]
    public async Task AtivarSeason_ComExpectedVersionObsoleta_DeveRecusarSemSalvar()
    {
        var ator = new CurrentActor(Guid.NewGuid());
        var season = NovaSeason("Season planejada", 2, atorId: ator.UserId!.Value);
        var calendario = new CalendarioCompetitivo(Guid.NewGuid(), ator.UserId.Value, Agora);
        SetProperty(calendario, nameof(CalendarioCompetitivo.Versao), 7L);
        var repository = new Mock<ICalendarioCompetitivoRepository>();
        var unitOfWork = new Mock<ICompetitiveUnitOfWork>();
        repository.Setup(item => item.GetWithSeasonsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(calendario);
        repository.Setup(item => item.GetSeasonByIdAsync(season.Id, It.IsAny<CancellationToken>())).ReturnsAsync(season);
        var handler = CreateActivationHandler(repository.Object, unitOfWork.Object, ator);

        var act = () => handler.Handle(new AtivarSeasonCommand(season.Id, ExpectedVersion: 6), CancellationToken.None);

        var exception = (await act.Should().ThrowAsync<DomainException>()).Which;
        exception.MessageCode.Should().Be(MessageCodes.CompetitiveCalendarVersionStale);
        season.Estado.Should().Be(SeasonEstado.Planejada);
        calendario.Versao.Should().Be(7);
        unitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AtualizarSeason_Planejada_DeveValidarSobreposicaoEOrdemAntesDePersistir()
    {
        var ator = new CurrentActor(Guid.NewGuid());
        var season = NovaSeason("Season original", 1, new DateOnly(2026, 1, 1), new DateOnly(2026, 4, 1), ator.UserId!.Value);
        var repository = new Mock<ICalendarioCompetitivoRepository>();
        var serieRepository = new Mock<ISerieRepository>();
        var unitOfWork = new Mock<ICompetitiveUnitOfWork>();
        var novoInicio = new DateOnly(2026, 1, 15);
        var novoFim = new DateOnly(2026, 4, 15);
        repository.Setup(item => item.GetSeasonByIdAsync(season.Id, It.IsAny<CancellationToken>())).ReturnsAsync(season);
        repository.Setup(item => item.ListSeasonsAsync(null, null, 1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync([season]);
        repository
            .Setup(item => item.ExistsOverlappingSeasonAsync(novoInicio, novoFim, season.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository
            .Setup(item => item.ExistsSeasonOrderAsync(2026, 1, season.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        serieRepository
            .Setup(item => item.HasConfirmedSeriesOutsidePeriodAsync(season.Id, novoInicio, novoFim, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var handler = CreateUpdateHandler(repository.Object, serieRepository.Object, unitOfWork.Object, ator);
        var request = new UpdateSeasonRequestDto("Season atualizada", 2026, 1, novoInicio, novoFim);

        var result = await handler.Handle(new UpdateSeasonCommand(season.Id, request, season.Versao), CancellationToken.None);

        result.Nome.Should().Be("Season atualizada");
        result.DataInicio.Should().Be(novoInicio);
        result.DataFimExclusiva.Should().Be(novoFim);
        repository.Verify(
            item => item.ExistsOverlappingSeasonAsync(novoInicio, novoFim, season.Id, It.IsAny<CancellationToken>()),
            Times.Once);
        repository.Verify(
            item => item.ExistsSeasonOrderAsync(2026, 1, season.Id, It.IsAny<CancellationToken>()),
            Times.Once);
        serieRepository.Verify(
            item => item.HasConfirmedSeriesOutsidePeriodAsync(season.Id, novoInicio, novoFim, It.IsAny<CancellationToken>()),
            Times.Once);
        unitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AtualizarSeason_ExcluindoConfrontoConfirmadoDoPeriodo_DeveRecusarSemSalvar()
    {
        var ator = new CurrentActor(Guid.NewGuid());
        var inicioOriginal = new DateOnly(2026, 1, 1);
        var fimOriginal = new DateOnly(2026, 7, 1);
        var novoInicio = new DateOnly(2026, 2, 1);
        var novoFim = new DateOnly(2026, 6, 1);
        var season = NovaSeason("Season com confronto", 1, inicioOriginal, fimOriginal, ator.UserId!.Value);
        var repository = new Mock<ICalendarioCompetitivoRepository>();
        var serieRepository = new Mock<ISerieRepository>();
        var unitOfWork = new Mock<ICompetitiveUnitOfWork>();
        repository.Setup(item => item.GetSeasonByIdAsync(season.Id, It.IsAny<CancellationToken>())).ReturnsAsync(season);
        repository.Setup(item => item.ListSeasonsAsync(null, null, 1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync([season]);
        repository
            .Setup(item => item.ExistsOverlappingSeasonAsync(novoInicio, novoFim, season.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository
            .Setup(item => item.ExistsSeasonOrderAsync(2026, 1, season.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        serieRepository
            .Setup(item => item.HasConfirmedSeriesOutsidePeriodAsync(season.Id, novoInicio, novoFim, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var handler = CreateUpdateHandler(repository.Object, serieRepository.Object, unitOfWork.Object, ator);
        var request = new UpdateSeasonRequestDto("Season com confronto", 2026, 1, novoInicio, novoFim);

        var act = () => handler.Handle(new UpdateSeasonCommand(season.Id, request, season.Versao), CancellationToken.None);

        var exception = (await act.Should().ThrowAsync<DomainException>()).Which;
        exception.MessageCode.Should().Be(MessageCodes.SeasonPeriodInvalid);
        season.DataInicio.Should().Be(inicioOriginal);
        season.DataFimExclusiva.Should().Be(fimOriginal);
        serieRepository.Verify(
            item => item.HasConfirmedSeriesOutsidePeriodAsync(season.Id, novoInicio, novoFim, It.IsAny<CancellationToken>()),
            Times.Once);
        unitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EncerrarSeason_Ativa_DeveLimparSeasonAtualEPreservarSeasonEncerrada()
    {
        var ator = new CurrentActor(Guid.NewGuid());
        var season = NovaSeason("Season ativa", 1, atorId: ator.UserId!.Value);
        SetProperty(season, nameof(Season.Estado), SeasonEstado.Ativa);
        var calendario = new CalendarioCompetitivo(Guid.NewGuid(), ator.UserId.Value, Agora);
        SetProperty(calendario, nameof(CalendarioCompetitivo.SeasonAtivaId), season.Id);
        SetProperty(calendario, nameof(CalendarioCompetitivo.Versao), 4L);
        var repository = new Mock<ICalendarioCompetitivoRepository>();
        var unitOfWork = new Mock<ICompetitiveUnitOfWork>();
        repository.Setup(item => item.GetWithSeasonsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(calendario);
        repository.Setup(item => item.GetSeasonByIdAsync(season.Id, It.IsAny<CancellationToken>())).ReturnsAsync(season);
        var handler = new EncerrarSeasonCommandHandler(
            repository.Object,
            unitOfWork.Object,
            ator,
            new AllowAuthorization(),
            new NoopAuditRepository(),
            new FixedClock(),
            new EncerrarSeasonCommandValidator());

        var result = await handler.Handle(new EncerrarSeasonCommand(season.Id, 4), CancellationToken.None);

        result.Estado.Should().Be(SeasonEstado.Encerrada);
        season.Estado.Should().Be(SeasonEstado.Encerrada);
        calendario.SeasonAtivaId.Should().BeNull();
        calendario.Versao.Should().Be(5);
        unitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListarCompeticoes_ComSelecaoPadrao_DeveAplicarSomenteSeasonAtual()
    {
        var season = NovaSeason("Season atual", 1);
        var calendarioRepository = new Mock<ICalendarioCompetitivoRepository>();
        var competicaoRepository = new Mock<ICompeticaoRepository>();
        calendarioRepository.Setup(item => item.GetActiveSeasonAsync(It.IsAny<CancellationToken>())).ReturnsAsync(season);
        competicaoRepository
            .Setup(item => item.ListAsync(
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.ToHashSet().SetEquals(new[] { season.Id })),
                1,
                20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        competicaoRepository
            .Setup(item => item.CountAsync(
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.ToHashSet().SetEquals(new[] { season.Id })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        var handler = CreateCompetitionQueryHandler(
            calendarioRepository.Object,
            competicaoRepository.Object,
            new PassthroughCompetitiveQuerySnapshot());

        var result = await handler.Handle(new GetCompeticoesQuery(SelecaoSazonal.Padrao(), 1, 20), CancellationToken.None);

        result.CalendarioConfigurado.Should().BeTrue();
        result.TemporadaAtual!.Id.Should().Be(season.Id);
        result.SeasonsIncluidas.Should().ContainSingle(item => item.Id == season.Id);
        competicaoRepository.VerifyAll();
    }

    [Fact]
    public async Task ListarCompeticoes_ComSelecaoPadraoSemSeasonAtiva_DeveRetornarVazioSemFallback()
    {
        var calendarioRepository = new Mock<ICalendarioCompetitivoRepository>();
        var competicaoRepository = new Mock<ICompeticaoRepository>();
        calendarioRepository.Setup(item => item.GetActiveSeasonAsync(It.IsAny<CancellationToken>())).ReturnsAsync((Season?)null);
        var handler = CreateCompetitionQueryHandler(
            calendarioRepository.Object,
            competicaoRepository.Object,
            new PassthroughCompetitiveQuerySnapshot());

        var result = await handler.Handle(new GetCompeticoesQuery(SelecaoSazonal.Padrao(), 1, 20), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.CalendarioConfigurado.Should().BeFalse();
        result.TemporadaAtual.Should().BeNull();
        result.SeasonsIncluidas.Should().BeEmpty();
        competicaoRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ListarCompeticoes_ComSelecaoEspecifica_DeveTratarIdsComoConjunto()
    {
        var primeira = NovaSeason("Season 1", 1);
        var segunda = NovaSeason("Season 2", 2);
        var selection = SelecaoSazonal.Especifica([segunda.Id, primeira.Id, segunda.Id]);
        var calendarioRepository = new Mock<ICalendarioCompetitivoRepository>();
        var competicaoRepository = new Mock<ICompeticaoRepository>();
        calendarioRepository
            .Setup(item => item.ListSeasonsAsync(
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.ToHashSet().SetEquals(new[] { primeira.Id, segunda.Id })),
                null,
                1,
                100,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([primeira, segunda]);
        competicaoRepository
            .Setup(item => item.ListAsync(
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.ToHashSet().SetEquals(new[] { primeira.Id, segunda.Id })),
                1,
                20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        competicaoRepository
            .Setup(item => item.CountAsync(
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.ToHashSet().SetEquals(new[] { primeira.Id, segunda.Id })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        var handler = CreateCompetitionQueryHandler(
            calendarioRepository.Object,
            competicaoRepository.Object,
            new PassthroughCompetitiveQuerySnapshot());

        var result = await handler.Handle(new GetCompeticoesQuery(selection, 1, 20), CancellationToken.None);

        selection.SeasonIds.Should().BeEquivalentTo([primeira.Id, segunda.Id]);
        selection.SeasonIds.Should().OnlyHaveUniqueItems();
        result.SeasonsIncluidas.Select(item => item.Id).Should().BeEquivalentTo([primeira.Id, segunda.Id]);
        competicaoRepository.VerifyAll();
    }

    [Fact]
    public void SelecaoEspecifica_SemIds_DeveRecusarComCodigoEstavel()
    {
        var act = () => SelecaoSazonal.Especifica([]);

        var exception = act.Should().Throw<DomainException>().Which;
        exception.MessageCode.Should().Be(MessageCodes.ValidationError);
    }

    [Fact]
    public void SelecaoSazonal_ComIdsETodas_DeveRecusarOpcoesMutuamenteExclusivas()
    {
        var act = () => SelecaoSazonal.Criar([Guid.NewGuid()], todas: true);

        var exception = act.Should().Throw<DomainException>().Which;
        exception.MessageCode.Should().Be(MessageCodes.SeasonalFilterConflict);
    }

    [Fact]
    public async Task ListarCompeticoes_ComTodasAsSeasons_DeveIncluirCatalogoCompletoNoMesmoSnapshot()
    {
        var seasons = Enumerable.Range(0, 101)
            .Select(index => NovaSeasonAnual(2009 + index, Guid.NewGuid()))
            .ToArray();
        var primeira = seasons[0];
        var segunda = seasons[1];
        var semCompeticoes = seasons[^1];
        var atorId = Guid.NewGuid();
        var competicoes = new[]
        {
            new Competicao(primeira.Id, "Competicao 1", "C1", false, atorId, Agora),
            new Competicao(segunda.Id, "Competicao 2", "C2", false, atorId, Agora),
            new Competicao(primeira.Id, "Competicao 3", "C3", false, atorId, Agora)
        };
        var all = SelecaoSazonal.Todas();
        var calendarioRepository = new Mock<ICalendarioCompetitivoRepository>();
        var competicaoRepository = new Mock<ICompeticaoRepository>();
        var querySnapshot = new PassthroughCompetitiveQuerySnapshot();
        competicaoRepository
            .Setup(item => item.ListAsync(
                It.Is<IReadOnlyCollection<Guid>>(seasonIds => seasonIds.Count == 0),
                1,
                20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(competicoes);
        competicaoRepository
            .Setup(item => item.CountAsync(
                It.Is<IReadOnlyCollection<Guid>>(seasonIds => seasonIds.Count == 0),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(competicoes.Length);
        calendarioRepository
            .Setup(item => item.ListSeasonsAsync(
                null,
                null,
                1,
                100,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(seasons[..100]);
        calendarioRepository
            .Setup(item => item.ListSeasonsAsync(
                null,
                null,
                2,
                100,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(seasons[100..]);
        calendarioRepository
            .Setup(item => item.CountSeasonsAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(101);
        var handler = CreateCompetitionQueryHandler(
            calendarioRepository.Object,
            competicaoRepository.Object,
            querySnapshot);

        var result = await handler.Handle(new GetCompeticoesQuery(all, 1, 20), CancellationToken.None);

        result.Items.Should().HaveCount(3);
        result.SeasonsIncluidas.Should().HaveCount(101);
        result.SeasonsIncluidas.Select(item => item.Id).Should().BeEquivalentTo(seasons.Select(item => item.Id));
        result.SeasonsIncluidas.Should().ContainSingle(item => item.Id == semCompeticoes.Id);
        querySnapshot.Executions.Should().Be(1);
        calendarioRepository.Verify(item => item.CountSeasonsAsync(null, null, It.IsAny<CancellationToken>()), Times.Once);
        calendarioRepository.Verify(item => item.ListSeasonsAsync(null, null, 1, 100, It.IsAny<CancellationToken>()), Times.Once);
        calendarioRepository.Verify(item => item.ListSeasonsAsync(null, null, 2, 100, It.IsAny<CancellationToken>()), Times.Once);
        competicaoRepository.VerifyAll();
    }

    [Fact]
    public async Task ListarCatalogoAdministrativoDeSeasons_DevePaginarMaisDeCemSemFiltroImplicito()
    {
        var seasons = Enumerable.Range(0, 125)
            .Select(index => NovaSeasonAnual(2009 + index, Guid.NewGuid()))
            .ToArray();
        var atual = seasons[17];
        SetProperty(atual, nameof(Season.Estado), SeasonEstado.Ativa);
        var calendar = new CalendarioCompetitivo(Guid.NewGuid(), Guid.NewGuid(), Agora);
        SetProperty(calendar, nameof(CalendarioCompetitivo.Versao), 9L);
        var repository = new Mock<ICalendarioCompetitivoRepository>();
        repository.Setup(item => item.GetCalendarAsync(It.IsAny<CancellationToken>())).ReturnsAsync(calendar);
        repository.Setup(item => item.GetActiveSeasonAsync(It.IsAny<CancellationToken>())).ReturnsAsync(atual);
        repository
            .Setup(item => item.ListSeasonsAsync(
                It.Is<IReadOnlyCollection<Guid>>(seasonIds => seasonIds.Count == 0),
                null,
                1,
                100,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(seasons[..100]);
        repository
            .Setup(item => item.ListSeasonsAsync(
                It.Is<IReadOnlyCollection<Guid>>(seasonIds => seasonIds.Count == 0),
                null,
                2,
                100,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(seasons[100..]);
        repository
            .Setup(item => item.CountSeasonsAsync(
                It.Is<IReadOnlyCollection<Guid>>(seasonIds => seasonIds.Count == 0),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(125);
        var handler = new GetSeasonsQueryHandler(
            repository.Object,
            new PassthroughCompetitiveQuerySnapshot(),
            new GetSeasonsQueryValidator());

        var primeiraPagina = await handler.Handle(new GetSeasonsQuery(null, 1, 100), CancellationToken.None);
        var segundaPagina = await handler.Handle(new GetSeasonsQuery(null, 2, 100), CancellationToken.None);

        primeiraPagina.Items.Should().HaveCount(100);
        segundaPagina.Items.Should().HaveCount(25);
        primeiraPagina.Items.Concat(segundaPagina.Items).Select(item => item.Id).Should().BeEquivalentTo(seasons.Select(item => item.Id));
        primeiraPagina.TotalItems.Should().Be(125);
        segundaPagina.TotalItems.Should().Be(125);
        primeiraPagina.TemporadaAtual!.Id.Should().Be(atual.Id);
        primeiraPagina.VersaoCalendario.Should().Be(9);
        repository.Verify(item => item.GetCalendarAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        repository.Verify(item => item.GetWithSeasonsAsync(It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(item => item.ListSeasonsAsync(
            It.Is<IReadOnlyCollection<Guid>>(seasonIds => seasonIds.Count == 0),
            null,
            It.IsAny<int>(),
            100,
            It.IsAny<CancellationToken>()), Times.Exactly(2));
        repository.Verify(item => item.CountSeasonsAsync(
            It.Is<IReadOnlyCollection<Guid>>(seasonIds => seasonIds.Count == 0),
            null,
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    private static Season NovaSeason(
        string nome,
        int ordem,
        DateOnly? inicio = null,
        DateOnly? fimExclusivo = null,
        Guid? atorId = null) =>
        new(
            nome,
            2026,
            ordem,
            inicio ?? new DateOnly(2026, ordem, 1),
            fimExclusivo ?? new DateOnly(2026, ordem + 1, 1),
            atorId ?? Guid.NewGuid(),
            Agora);

    private static Season NovaSeasonAnual(int ano, Guid id)
    {
        var season = new Season(
            $"Season {ano}",
            ano,
            1,
            new DateOnly(ano, 1, 1),
            new DateOnly(ano + 1, 1, 1),
            Guid.NewGuid(),
            Agora);
        SetProperty(season, nameof(Season.Id), id);
        return season;
    }

    private static void SetProperty<T>(object target, string propertyName, T value) =>
        target.GetType().GetProperty(propertyName)!.SetValue(target, value);

    private static AtivarSeasonCommandHandler CreateActivationHandler(
        ICalendarioCompetitivoRepository repository,
        ICompetitiveUnitOfWork unitOfWork,
        ICurrentActor actor) =>
        new(
            repository,
            unitOfWork,
            actor,
            new AllowAuthorization(),
            new NoopAuditRepository(),
            new FixedClock(),
            new AtivarSeasonCommandValidator());

    private static UpdateSeasonCommandHandler CreateUpdateHandler(
        ICalendarioCompetitivoRepository repository,
        ISerieRepository serieRepository,
        ICompetitiveUnitOfWork unitOfWork,
        ICurrentActor actor) =>
        new(
            repository,
            serieRepository,
            unitOfWork,
            actor,
            new AllowAuthorization(),
            new NoopAuditRepository(),
            new FixedClock(),
            new UpdateSeasonCommandValidator(new UpdateSeasonRequestDtoValidator()));

    private static GetCompeticoesQueryHandler CreateCompetitionQueryHandler(
        ICalendarioCompetitivoRepository calendarRepository,
        ICompeticaoRepository competitionRepository,
        ICompetitiveQuerySnapshot querySnapshot)
    {
        var seriesRepository = new Mock<ISerieRepository>();
        seriesRepository.Setup(item => item.ListCompetitionIdsWithStartedSeriesAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        return new(
            calendarRepository,
            competitionRepository,
            seriesRepository.Object,
            new AllowAuthorization(),
            querySnapshot,
            new GetCompeticoesQueryValidator(),
            new GetSeasonCompeticoesQueryValidator(),
            new GetCompeticaoByIdQueryValidator());
    }

    private sealed record CurrentActor(Guid? UserId) : ICurrentActor
    {
        public IReadOnlyCollection<string> Roles => [];
    }

    private sealed class AllowAuthorization : ICompetitiveAuthorizationService
    {
        public Task<bool> AuthorizeAsync(
            CompetitiveAuthorizationContext context,
            CancellationToken cancellationToken) => Task.FromResult(true);

        public Task<IReadOnlyCollection<string>> GetAllowedActionsAsync(
            IReadOnlyCollection<CompetitiveAuthorizationContext> actions,
            CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<string>>([]);
    }

    private sealed class NoopAuditRepository : ICompetitiveAuditRepository
    {
        public Task AddAsync(RegistroAuditoriaCompetitiva registro, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<IReadOnlyCollection<RegistroAuditoriaCompetitiva>> ListByResourceAsync(
            RecursoCompetitivoTipo recursoTipo,
            Guid recursoId,
            int page,
            int pageSize,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<int> CountByResourceAsync(
            RecursoCompetitivoTipo recursoTipo,
            Guid recursoId,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class FixedClock : ISystemClock
    {
        public DateTimeOffset UtcNow => Agora;
    }

    private sealed class PassthroughCompetitiveQuerySnapshot : ICompetitiveQuerySnapshot
    {
        public int Executions { get; private set; }

        public Task<T> ExecuteAsync<T>(
            Func<CancellationToken, Task<T>> query,
            CancellationToken cancellationToken)
        {
            Executions++;
            return query(cancellationToken);
        }
    }

    private sealed class ConcurrentSeasonPersistence
    {
        private readonly object sync = new();
        private readonly TaskCompletionSource bothCommitsReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Dictionary<Guid, SeasonSnapshot> seasons;
        private int commitsReady;
        private long calendarVersion;

        public ConcurrentSeasonPersistence(
            Season activeSeason,
            IReadOnlyCollection<Season> plannedSeasons,
            Guid actorId,
            DateTimeOffset now,
            long version)
        {
            CalendarId = Guid.NewGuid();
            ActorId = actorId;
            Now = now;
            ActiveSeasonId = activeSeason.Id;
            calendarVersion = version;
            seasons = plannedSeasons
                .Append(activeSeason)
                .Select(SeasonSnapshot.FromSeason)
                .ToDictionary(item => item.Id);
            seasons[activeSeason.Id] = seasons[activeSeason.Id] with { Estado = SeasonEstado.Ativa };
        }

        public Guid CalendarId { get; }
        public Guid ActorId { get; }
        public DateTimeOffset Now { get; }
        public Guid? ActiveSeasonId { get; private set; }
        public int SuccessfulCommits { get; private set; }
        public long CalendarVersion
        {
            get
            {
                lock (sync)
                {
                    return calendarVersion;
                }
            }
        }

        public RequestScope CreateRequestScope()
        {
            lock (sync)
            {
                return new RequestScope(this, calendarVersion, ActiveSeasonId, seasons.Values.ToArray());
            }
        }

        public SeasonSnapshot GetSeason(Guid id)
        {
            lock (sync)
            {
                return seasons[id];
            }
        }

        public IReadOnlyCollection<SeasonSnapshot> GetSeasons()
        {
            lock (sync)
            {
                return seasons.Values.ToArray();
            }
        }

        private async Task CommitAsync(RequestScope scope, CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref commitsReady) == 2)
            {
                bothCommitsReady.SetResult();
            }

            await bothCommitsReady.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);

            lock (sync)
            {
                if (scope.LoadedCalendarVersion != calendarVersion)
                {
                    throw new DomainException(MessageCodes.CompetitiveCalendarVersionStale);
                }

                var expectedAggregateVersion = scope.LoadedCalendarVersion + 1;
                if (scope.Calendar.Versao != expectedAggregateVersion)
                {
                    throw new InvalidOperationException("The aggregate must increment the competitive calendar version before persistence.");
                }

                calendarVersion = scope.Calendar.Versao;
                ActiveSeasonId = scope.Calendar.SeasonAtivaId;
                foreach (var season in scope.Seasons.Values)
                {
                    seasons[season.Id] = SeasonSnapshot.FromSeason(season);
                }

                SuccessfulCommits++;
            }
        }

        public sealed class RequestScope : ICalendarioCompetitivoRepository, ICompetitiveUnitOfWork
        {
            private readonly ConcurrentSeasonPersistence persistence;

            public RequestScope(
                ConcurrentSeasonPersistence persistence,
                long loadedCalendarVersion,
                Guid? activeSeasonId,
                IReadOnlyCollection<SeasonSnapshot> snapshots)
            {
                this.persistence = persistence;
                LoadedCalendarVersion = loadedCalendarVersion;
                Calendar = new CalendarioCompetitivo(persistence.CalendarId, persistence.ActorId, persistence.Now);
                SetProperty(Calendar, nameof(CalendarioCompetitivo.Versao), loadedCalendarVersion);
                SetProperty(Calendar, nameof(CalendarioCompetitivo.SeasonAtivaId), activeSeasonId);
                Seasons = snapshots.ToDictionary(item => item.Id, item => item.ToSeason());
            }

            public long LoadedCalendarVersion { get; }
            public CalendarioCompetitivo Calendar { get; }
            public Dictionary<Guid, Season> Seasons { get; }

            public Task AcquireBootstrapLockAsync(CancellationToken cancellationToken) => Task.CompletedTask;

            public Task<CalendarioCompetitivo?> GetWithSeasonsAsync(CancellationToken cancellationToken) =>
                Task.FromResult<CalendarioCompetitivo?>(Calendar);

            public Task<CalendarioCompetitivo?> GetCalendarAsync(CancellationToken cancellationToken) =>
                Task.FromResult<CalendarioCompetitivo?>(Calendar);

            public Task<Season?> GetSeasonByIdAsync(Guid seasonId, CancellationToken cancellationToken) =>
                Task.FromResult(Seasons.GetValueOrDefault(seasonId));

            public Task<Season?> GetActiveSeasonAsync(CancellationToken cancellationToken) =>
                Task.FromResult(Calendar.SeasonAtivaId is Guid id ? Seasons.GetValueOrDefault(id) : null);

            public Task SaveChangesAsync(CancellationToken cancellationToken) =>
                persistence.CommitAsync(this, cancellationToken);

            public Task<IReadOnlyCollection<Season>> ListSeasonsAsync(
                IReadOnlyCollection<Guid>? seasonIds,
                SeasonEstado? estado,
                int page,
                int pageSize,
                CancellationToken cancellationToken) => throw new NotSupportedException();

            public Task<int> CountSeasonsAsync(
                IReadOnlyCollection<Guid>? seasonIds,
                SeasonEstado? estado,
                CancellationToken cancellationToken) => throw new NotSupportedException();

            public Task<bool> ExistsOverlappingSeasonAsync(
                DateOnly dataInicio,
                DateOnly dataFimExclusiva,
                Guid? excludedSeasonId,
                CancellationToken cancellationToken) => throw new NotSupportedException();

            public Task<bool> ExistsSeasonOrderAsync(
                int ano,
                int ordemNoAno,
                Guid? excludedSeasonId,
                CancellationToken cancellationToken) => throw new NotSupportedException();

            public Task AddAsync(CalendarioCompetitivo calendario, CancellationToken cancellationToken) =>
                throw new NotSupportedException();

            public Task AddSeasonAsync(Season season, CancellationToken cancellationToken) =>
                throw new NotSupportedException();
        }

        public sealed record SeasonSnapshot(
            Guid Id,
            string Nome,
            int Ano,
            int OrdemNoAno,
            DateOnly DataInicio,
            DateOnly DataFimExclusiva,
            SeasonEstado Estado,
            long Versao,
            Guid CriadaPorUsuarioId,
            DateTimeOffset CriadaEm)
        {
            public static SeasonSnapshot FromSeason(Season season) =>
                new(
                    season.Id,
                    season.Nome,
                    season.Ano,
                    season.OrdemNoAno,
                    season.DataInicio,
                    season.DataFimExclusiva,
                    season.Estado,
                    season.Versao,
                    season.CriadaPorUsuarioId,
                    season.CriadaEm);

            public Season ToSeason()
            {
                var season = new Season(Nome, Ano, OrdemNoAno, DataInicio, DataFimExclusiva, CriadaPorUsuarioId, CriadaEm);
                SetProperty(season, nameof(Season.Id), Id);
                SetProperty(season, nameof(Season.Estado), Estado);
                SetProperty(season, nameof(Season.Versao), Versao);
                return season;
            }
        }
    }
}
