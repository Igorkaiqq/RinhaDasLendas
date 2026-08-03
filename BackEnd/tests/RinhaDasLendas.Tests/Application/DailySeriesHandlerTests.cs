using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using RinhaDasLendas.Application.Commands.Partidas;
using RinhaDasLendas.Application.Commands.Series;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Handlers.Partidas;
using RinhaDasLendas.Application.Handlers.Series;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Security;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Repositories;
using RinhaDasLendas.Infrastructure.Persistence;
using RinhaDasLendas.Infrastructure.Repositories;
using RinhaDasLendas.Tests.Fixtures;
using RinhaDasLendas.Tests.Jogadores;

namespace RinhaDasLendas.Tests.Application;

public sealed class DailySeriesHandlerTests
{
    private static readonly DateTimeOffset Agora = new(2026, 7, 29, 3, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset AgendadaPara = new(2026, 7, 29, 2, 30, 0, TimeSpan.Zero);
    private static readonly DateOnly DataLocalSaoPaulo = new(2026, 7, 28);

    [Fact]
    public async Task CriarDiaria_DeDraftFinalizadoEArquivado_DevePreservarArquivoECapturarSnapshotsAtomicamente()
    {
        var contexto = NovoContextoDiario(arquivarDraft: true);
        Serie? adicionada = null;
        RegistroAuditoriaCompetitiva? auditoria = null;
        var serieRepository = new Mock<ISerieRepository>(MockBehavior.Strict);
        var auditRepository = new Mock<ICompetitiveAuditRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<ICompetitiveUnitOfWork>(MockBehavior.Strict);
        serieRepository
            .Setup(item => item.ExistsForDraftAsync(contexto.Draft.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var sequence = new MockSequence();
        serieRepository
            .InSequence(sequence)
            .Setup(item => item.AddAsync(It.IsAny<Serie>(), It.IsAny<CancellationToken>()))
            .Callback<Serie, CancellationToken>((serie, _) => adicionada = serie)
            .Returns(Task.CompletedTask);
        auditRepository
            .InSequence(sequence)
            .Setup(item => item.AddAsync(It.IsAny<RegistroAuditoriaCompetitiva>(), It.IsAny<CancellationToken>()))
            .Callback<RegistroAuditoriaCompetitiva, CancellationToken>((registro, _) => auditoria = registro)
            .Returns(Task.CompletedTask);
        unitOfWork
            .InSequence(sequence)
            .Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var handler = CriarHandlerDeSerie(contexto, serieRepository, auditRepository, unitOfWork);

        var result = await handler.Handle(NovoComandoDeCriacao(contexto), CancellationToken.None);

        result.Should().BeSameAs(adicionada);
        result.SeasonId.Should().Be(contexto.Season.Id);
        result.CompeticaoId.Should().Be(contexto.Competicao.Id);
        result.RodadaId.Should().Be(contexto.Rodada.Id);
        result.VersaoRegrasId.Should().Be(contexto.Regras.Id);
        result.Formato.Should().Be(contexto.Regras.Formato);
        result.ModoDraft.Should().Be(contexto.Regras.ModoDraft);
        result.AgendadaPara.Should().Be(AgendadaPara);
        result.DataLocal.Should().Be(DataLocalSaoPaulo);
        result.CriadaPorUsuarioId.Should().Be(contexto.Ator.UserId);
        result.AtualizadaPorUsuarioId.Should().Be(contexto.Ator.UserId);
        result.DraftMontagemId.Should().Be(contexto.Draft.Id);
        result.Lados.Select(item => item.OrigemId).Should().BeEquivalentTo(contexto.Draft.Times.Select(item => item.Id));
        result.Lados.Select(item => item.CapitaoJogadorId).Should().BeEquivalentTo(contexto.CapitaesIds);
        result.Lados.SelectMany(item => item.ParticipantesEsperados).Select(item => item.JogadorId)
            .Should().BeEquivalentTo(contexto.Jogadores.Select(item => item.Id));
        result.Lados.SelectMany(item => item.ParticipantesEsperados).Select(item => item.NomeSnapshot)
            .Should().BeEquivalentTo(contexto.Jogadores.Select(item => item.Nome));
        contexto.Authorization.LastContext.Should().Be(new CompetitiveAuthorizationContext(
            AuthPermissions.CanManageMatches,
            "CriarSerie",
            nameof(Serie),
            SerieTipo.DiariaTemporaria.ToString(),
            SeasonEstado.Ativa.ToString(),
            HasStartedSeries: false,
            HasRequiredJustification: false));
        auditoria.Should().NotBeNull();
        auditoria!.RecursoTipo.Should().Be(RecursoCompetitivoTipo.Serie);
        auditoria.RecursoId.Should().Be(result.Id);
        auditoria.Acao.Should().Be(AcaoAuditoriaCompetitiva.SerieCriada);
        auditoria.AtorUsuarioId.Should().Be(contexto.Ator.UserId, "o ator autenticado deve ser a unica fonte confiavel");
        auditoria.Capacidade.Should().Be(AuthPermissions.CanManageMatches);
        auditoria.Justificativa.Should().BeNull();
        auditoria.ValorAnterior.Should().BeNull();
        auditoria.ValorPosterior.Should().NotBeNull();
        auditoria.CorrelationId.Should().NotBeEmpty();
        auditoria.OcorridoEm.Should().Be(Agora);
        auditoria.ValorPosterior!.Campos.Should().Contain(new Dictionary<CampoSnapshotAuditoria, object?>
        {
            [CampoSnapshotAuditoria.Id] = result.Id,
            [CampoSnapshotAuditoria.SeasonId] = contexto.Season.Id,
            [CampoSnapshotAuditoria.CompeticaoId] = contexto.Competicao.Id,
            [CampoSnapshotAuditoria.RodadaId] = contexto.Rodada.Id,
            [CampoSnapshotAuditoria.VersaoRegrasId] = contexto.Regras.Id,
            [CampoSnapshotAuditoria.DraftMontagemId] = contexto.Draft.Id,
            [CampoSnapshotAuditoria.TipoSerie] = SerieTipo.DiariaTemporaria,
            [CampoSnapshotAuditoria.FormatoSerie] = contexto.Regras.Formato,
            [CampoSnapshotAuditoria.ModoDraft] = contexto.Regras.ModoDraft,
            [CampoSnapshotAuditoria.EstadoSerie] = SerieEstado.Agendada,
            [CampoSnapshotAuditoria.AgendadaPara] = AgendadaPara,
            [CampoSnapshotAuditoria.DataLocal] = DataLocalSaoPaulo,
        });
        contexto.Draft.Arquivado.Should().BeTrue();
        contexto.Draft.Status.Should().Be(DraftMontagemStatus.Finalizada);
        contexto.DraftRepository.Verify(
            item => item.GetByIdIncludingArchivedAsync(contexto.Draft.Id, CancellationToken.None),
            Times.Once);
        contexto.DraftRepository.Verify(
            item => item.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        auditRepository.Verify(item => item.AddAsync(It.IsAny<RegistroAuditoriaCompetitiva>(), CancellationToken.None), Times.Once);
        unitOfWork.Verify(item => item.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task CriarDiaria_NoLimiteUtc_DeveDerivarDataLocalEmAmericaSaoPaulo()
    {
        var contexto = NovoContextoDiario();
        var serieRepository = NovoSerieRepositoryParaCriacao(contexto.Draft.Id);
        var handler = CriarHandlerDeSerie(
            contexto,
            serieRepository,
            NovoAuditRepository(),
            NovoUnitOfWork());

        var result = await handler.Handle(NovoComandoDeCriacao(contexto), CancellationToken.None);

        AgendadaPara.Date.Should().Be(new DateTime(2026, 7, 29));
        result.DataLocal.Should().Be(DataLocalSaoPaulo,
            "02:30 UTC ainda pertence ao dia anterior em America/Sao_Paulo");
    }

    [Fact]
    public async Task CriarDiaria_ComDataLocalDiferenteDaDerivadaDeAgendadaPara_DeveRecusarComCodigoEstavel()
    {
        var contexto = NovoContextoDiario();
        var serieRepository = new Mock<ISerieRepository>();
        var auditRepository = new Mock<ICompetitiveAuditRepository>();
        var unitOfWork = new Mock<ICompetitiveUnitOfWork>();
        var handler = CriarHandlerDeSerie(contexto, serieRepository, auditRepository, unitOfWork);

        var act = () => handler.Handle(
            NovoComandoDeCriacao(contexto, dataLocal: DataLocalSaoPaulo.AddDays(1)),
            CancellationToken.None);

        var exception = (await act.Should().ThrowAsync<DomainException>()).Which;
        exception.MessageCode.Should().Be(MessageCodes.DailySeriesDateInvalid);
        serieRepository.Verify(item => item.AddAsync(It.IsAny<Serie>(), It.IsAny<CancellationToken>()), Times.Never);
        auditRepository.Verify(item => item.AddAsync(It.IsAny<RegistroAuditoriaCompetitiva>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CriarDiaria_ComDraftNaoFinalizado_DeveRecusarSemStageNemCommit()
    {
        var contexto = NovoContextoDiario(finalizarDraft: false);
        var serieRepository = new Mock<ISerieRepository>();
        var auditRepository = new Mock<ICompetitiveAuditRepository>();
        var unitOfWork = new Mock<ICompetitiveUnitOfWork>();
        var handler = CriarHandlerDeSerie(contexto, serieRepository, auditRepository, unitOfWork);

        var act = () => handler.Handle(NovoComandoDeCriacao(contexto), CancellationToken.None);

        var exception = (await act.Should().ThrowAsync<DomainException>()).Which;
        exception.MessageCode.Should().Be(MessageCodes.DailySeriesDraftInvalid);
        serieRepository.Verify(item => item.AddAsync(It.IsAny<Serie>(), It.IsAny<CancellationToken>()), Times.Never);
        auditRepository.Verify(item => item.AddAsync(It.IsAny<RegistroAuditoriaCompetitiva>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("Ausente")]
    [InlineData("Duplicado")]
    [InlineData("OutroTime")]
    [InlineData("NaoParticipante")]
    public async Task CriarDiaria_ComCapitaoInvalido_DeveRecusarSemStageNemCommit(string caso)
    {
        var contexto = NovoContextoDiario();
        var times = contexto.Draft.Times.OrderBy(item => item.Ordem).ToArray();
        var capitaoInvalido = caso switch
        {
            "Ausente" => (Guid?)null,
            "Duplicado" => times[0].CapitaoId,
            "OutroTime" => contexto.Draft.Participantes
                .First(item => item.TimeId == times[1].Id && item.JogadorId != times[1].CapitaoId)
                .JogadorId,
            "NaoParticipante" => Guid.NewGuid(),
            _ => throw new InvalidOperationException(caso),
        };
        var timeAlterado = caso == "Duplicado" ? times[1] : times[0];
        SetProperty(timeAlterado, nameof(DraftMontagemTime.CapitaoId), capitaoInvalido);
        var serieRepository = new Mock<ISerieRepository>();
        var auditRepository = new Mock<ICompetitiveAuditRepository>();
        var unitOfWork = new Mock<ICompetitiveUnitOfWork>();
        var handler = CriarHandlerDeSerie(contexto, serieRepository, auditRepository, unitOfWork);

        var act = () => handler.Handle(NovoComandoDeCriacao(contexto), CancellationToken.None);

        var exception = (await act.Should().ThrowAsync<DomainException>()).Which;
        exception.MessageCode.Should().Be(MessageCodes.DailySeriesCaptainsInvalid);
        serieRepository.Verify(item => item.AddAsync(It.IsAny<Serie>(), It.IsAny<CancellationToken>()), Times.Never);
        auditRepository.Verify(item => item.AddAsync(It.IsAny<RegistroAuditoriaCompetitiva>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CriarDiaria_ComSeasonInativa_DeveRecusarAtomicamente()
    {
        var contexto = NovoContextoDiario();
        SetProperty(contexto.Season, nameof(Season.Estado), SeasonEstado.Planejada);
        contexto.CalendarioRepository
            .Setup(item => item.GetActiveSeasonAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((Season?)null);
        var serieRepository = new Mock<ISerieRepository>();
        var auditRepository = new Mock<ICompetitiveAuditRepository>();
        var unitOfWork = new Mock<ICompetitiveUnitOfWork>();
        var handler = CriarHandlerDeSerie(contexto, serieRepository, auditRepository, unitOfWork);

        var act = () => handler.Handle(NovoComandoDeCriacao(contexto), CancellationToken.None);

        var exception = (await act.Should().ThrowAsync<DomainException>()).Which;
        exception.MessageCode.Should().Be(MessageCodes.ActiveSeasonNotFound);
        serieRepository.Verify(item => item.AddAsync(It.IsAny<Serie>(), It.IsAny<CancellationToken>()), Times.Never);
        auditRepository.Verify(item => item.AddAsync(It.IsAny<RegistroAuditoriaCompetitiva>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CriarDiaria_ComRegraDeOutraSeasonECompeticao_DeveRecusarAtomicamente()
    {
        var contexto = NovoContextoDiario();
        var regraDeOutroEscopo = new VersaoRegras(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            SerieFormato.Md5,
            ModoDraft.Padrao,
            contexto.Ator.UserId!.Value,
            Agora);
        contexto.CompeticaoRepository
            .Setup(item => item.GetRulesVersionAsync(regraDeOutroEscopo.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(regraDeOutroEscopo);
        var serieRepository = new Mock<ISerieRepository>();
        var auditRepository = new Mock<ICompetitiveAuditRepository>();
        var unitOfWork = new Mock<ICompetitiveUnitOfWork>();
        var handler = CriarHandlerDeSerie(contexto, serieRepository, auditRepository, unitOfWork);

        var act = () => handler.Handle(
            NovoComandoDeCriacao(contexto, regraDeOutroEscopo.Id),
            CancellationToken.None);

        var exception = (await act.Should().ThrowAsync<DomainException>()).Which;
        exception.MessageCode.Should().Be(MessageCodes.RulesVersionSeasonMismatch);
        serieRepository.Verify(item => item.AddAsync(It.IsAny<Serie>(), It.IsAny<CancellationToken>()), Times.Never);
        auditRepository.Verify(item => item.AddAsync(It.IsAny<RegistroAuditoriaCompetitiva>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OperacoesConcorrentesEmPartidasDiferentes_ComPostgresEMesmaEtag_DevemPersistirUmVencedorERollbackDoPerdedor()
    {
        await using var database = await CompetitivePostgresFixture.CreateAsync();
        var seed = await SeedConcurrentSeriesAsync(database);
        var saveBarrier = new ConcurrentSaveBarrier(2);
        await using var primeiroContexto = database.CreateContext(saveBarrier);
        await using var segundoContexto = database.CreateContext(saveBarrier);
        var primeiroHandler = new RegisterMatchPicksCommandHandler(
            new SerieRepository(primeiroContexto),
            new CompetitiveUnitOfWork(primeiroContexto),
            new CurrentActor(seed.ActorId),
            new AllowAuthorization(),
            new CompetitiveAuditRepository(primeiroContexto),
            new FixedClock(),
            ValidValidator<RegisterMatchPicksCommand>());
        var segundoHandler = new RegisterMatchPicksCommandHandler(
            new SerieRepository(segundoContexto),
            new CompetitiveUnitOfWork(segundoContexto),
            new CurrentActor(seed.ActorId),
            new AllowAuthorization(),
            new CompetitiveAuditRepository(segundoContexto),
            new FixedClock(),
            ValidValidator<RegisterMatchPicksCommand>());

        var primeiraOperacao = Record.ExceptionAsync(() => primeiroHandler.Handle(
            NovoComandoDePicks(seed.PrimeiraPartidaId, seed.LadoIds, seed.SeriesVersion, 1),
            CancellationToken.None));
        var segundaOperacao = Record.ExceptionAsync(() => segundoHandler.Handle(
            NovoComandoDePicks(seed.SegundaPartidaId, seed.LadoIds, seed.SeriesVersion, 101),
            CancellationToken.None));
        var resultados = await Task.WhenAll(primeiraOperacao, segundaOperacao);

        resultados.Should().ContainSingle(item => item is null);
        var conflito = resultados.Should().ContainSingle(item => item is DomainException).Subject
            .Should().BeOfType<DomainException>().Subject;
        conflito.MessageCode.Should().Be(MessageCodes.CompetitiveResourceVersionStale);
        await using var assertionContext = database.CreateContext();
        var persistedSeries = await assertionContext.Series.AsNoTracking().SingleAsync(item => item.Id == seed.SeriesId);
        var persistedPicks = await assertionContext.PicksPartidas.AsNoTracking()
            .Where(item => item.PartidaId == seed.PrimeiraPartidaId || item.PartidaId == seed.SegundaPartidaId)
            .ToArrayAsync();
        var persistedAudits = await assertionContext.RegistrosAuditoriaCompetitiva.AsNoTracking()
            .Where(item => item.RecursoId == seed.PrimeiraPartidaId || item.RecursoId == seed.SegundaPartidaId)
            .ToArrayAsync();
        persistedSeries.Versao.Should().Be(seed.SeriesVersion + 1);
        persistedPicks.Should().HaveCount(10);
        var winningMatchIds = persistedPicks.Select(item => item.PartidaId).Distinct().ToArray();
        winningMatchIds.Should().ContainSingle();
        var winningMatchId = winningMatchIds.Single();
        var losingMatchId = winningMatchId == seed.PrimeiraPartidaId
            ? seed.SegundaPartidaId
            : seed.PrimeiraPartidaId;
        persistedPicks.Should().NotContain(item => item.PartidaId == losingMatchId);
        persistedAudits.Should().ContainSingle();
        var persistedAudit = persistedAudits.Single();
        persistedAudit.RecursoTipo.Should().Be(RecursoCompetitivoTipo.Partida);
        persistedAudit.RecursoId.Should().Be(winningMatchId);
        persistedAudit.Acao.Should().Be(AcaoAuditoriaCompetitiva.PicksPartidaRegistrados);
        persistedAudit.AtorUsuarioId.Should().Be(seed.ActorId);
        persistedAudit.Capacidade.Should().Be(AuthPermissions.CanManageMatches);
        persistedAudit.ValorAnterior.Should().NotBeNull();
        persistedAudit.ValorPosterior.Should().NotBeNull();
        persistedAudit.CorrelationId.Should().NotBeEmpty();
        persistedAudit.OcorridoEm.Should().Be(Agora);
        persistedAudits.Should().NotContain(item => item.RecursoId == losingMatchId,
            "a auditoria staged pelo escopo perdedor deve ser revertida com o agregado");
    }

    private static CreateSeriesCommandHandler CriarHandlerDeSerie(
        DailyContext contexto,
        Mock<ISerieRepository> serieRepository,
        Mock<ICompetitiveAuditRepository> auditRepository,
        Mock<ICompetitiveUnitOfWork> unitOfWork) =>
        new(
            serieRepository.Object,
            contexto.DraftRepository.Object,
            contexto.CalendarioRepository.Object,
            contexto.CompeticaoRepository.Object,
            unitOfWork.Object,
            contexto.Ator,
            contexto.Authorization,
            auditRepository.Object,
            new FixedClock(),
            ValidValidator<CreateSeriesCommand>());

    private static CreateSeriesCommand NovoComandoDeCriacao(
        DailyContext contexto,
        Guid? versaoRegrasId = null,
        DateOnly? dataLocal = null) =>
        new(new CreateSeriesRequestDto(
            contexto.Season.Id,
            contexto.Competicao.Id,
            contexto.Rodada.Id,
            versaoRegrasId ?? contexto.Regras.Id,
            EventoId: null,
            SerieTipo.DiariaTemporaria,
            AgendadaPara,
            dataLocal ?? DataLocalSaoPaulo,
            contexto.Draft.Id,
            contexto.Draft.Times.OrderBy(item => item.Ordem).Select(item => item.Id).ToArray()));

    private static RegisterMatchPicksCommand NovoComandoDePicks(
        Guid partidaId,
        IReadOnlyList<Guid> ladoIds,
        long expectedVersion,
        int primeiroChampionId)
    {
        return new RegisterMatchPicksCommand(
            partidaId,
            new RegisterPicksRequestDto(
            [
                new MatchSidePicksRequestDto(ladoIds[0], Enumerable.Range(primeiroChampionId, 5).ToArray()),
                new MatchSidePicksRequestDto(ladoIds[1], Enumerable.Range(primeiroChampionId + 5, 5).ToArray()),
            ]),
            expectedVersion);
    }

    private static DailyContext NovoContextoDiario(bool finalizarDraft = true, bool arquivarDraft = false)
    {
        var ator = new CurrentActor(Guid.NewGuid());
        var jogadores = Enumerable.Range(1, 10)
            .Select(index => JogadorTestData.JogadorAtivo($"Jogador {index}"))
            .ToArray();
        var capitaesIds = jogadores.Take(2).Select(item => item.Id).ToArray();
        var draft = new DraftMontagem(
            "Draft diario",
            null,
            5,
            DraftMontagemCriterioCapitaes.Manual,
            jogadores.Select(item => item.Id).ToArray(),
            capitaesIds);
        DistribuirJogadores(draft, jogadores.Select(item => item.Id).ToArray());
        if (finalizarDraft)
        {
            draft.Finalizar();
        }

        if (arquivarDraft)
        {
            draft.Arquivar("Encerrado administrativamente", ator.UserId!.Value, Agora.AddMinutes(-10));
        }

        var season = new Season(
            "Season atual",
            2026,
            3,
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 10, 1),
            ator.UserId!.Value,
            Agora.AddDays(-30));
        SetProperty(season, nameof(Season.Estado), SeasonEstado.Ativa);
        var competicao = new Competicao(
            season.Id,
            "Circuito diario",
            "DIARIO",
            circuitoDiario: true,
            ator.UserId.Value,
            Agora.AddDays(-20));
        var rodada = competicao.AdicionarRodada("Rodada atual", 1, Agora.AddDays(-19), ator.UserId.Value);
        var regras = competicao.PublicarRegras(
            SerieFormato.Md3,
            ModoDraft.Fearless,
            competicao.Versao,
            ator.UserId.Value,
            Agora.AddDays(-18));
        var draftRepository = new Mock<IDraftMontagemRepository>();
        draftRepository
            .Setup(item => item.GetByIdIncludingArchivedAsync(draft.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(draft);
        draftRepository
            .Setup(item => item.GetJogadoresByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(jogadores);
        var calendarioRepository = new Mock<ICalendarioCompetitivoRepository>();
        calendarioRepository.Setup(item => item.GetActiveSeasonAsync(It.IsAny<CancellationToken>())).ReturnsAsync(season);
        calendarioRepository.Setup(item => item.GetSeasonByIdAsync(season.Id, It.IsAny<CancellationToken>())).ReturnsAsync(season);
        var competicaoRepository = new Mock<ICompeticaoRepository>();
        competicaoRepository
            .Setup(item => item.GetWithRoundsAndRulesAsync(competicao.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(competicao);
        competicaoRepository
            .Setup(item => item.GetRulesVersionAsync(regras.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(regras);
        return new(
            ator,
            jogadores,
            capitaesIds,
            draft,
            season,
            competicao,
            rodada,
            regras,
            draftRepository,
            calendarioRepository,
            competicaoRepository,
            new RecordingAuthorization());
    }

    private static void DistribuirJogadores(DraftMontagem draft, IReadOnlyList<Guid> jogadoresIds)
    {
        var times = draft.Times.OrderBy(item => item.Ordem).ToArray();
        var layouts = times.Select((time, index) => new DraftMontagemLayoutTime(
            time.Id,
            time.Nome,
            time.CapitaoId,
            jogadoresIds.Skip(index * 5).Take(5)
                .Select((jogadorId, ordem) => new DraftMontagemLayoutParticipante(jogadorId, ordem + 1, null))
                .ToArray()))
            .ToArray();
        draft.SalvarLayout(layouts, [], []);
    }

    private static Mock<ISerieRepository> NovoSerieRepositoryParaCriacao(Guid draftId)
    {
        var repository = new Mock<ISerieRepository>();
        repository.Setup(item => item.ExistsForDraftAsync(draftId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        repository.Setup(item => item.AddAsync(It.IsAny<Serie>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        return repository;
    }

    private static Mock<ICompetitiveAuditRepository> NovoAuditRepository()
    {
        var repository = new Mock<ICompetitiveAuditRepository>();
        repository.Setup(item => item.AddAsync(It.IsAny<RegistroAuditoriaCompetitiva>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return repository;
    }

    private static Mock<ICompetitiveUnitOfWork> NovoUnitOfWork()
    {
        var unitOfWork = new Mock<ICompetitiveUnitOfWork>();
        unitOfWork.Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        return unitOfWork;
    }

    private static IValidator<T> ValidValidator<T>()
    {
        var validator = new Mock<IValidator<T>>();
        validator
            .Setup(item => item.ValidateAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        return validator.Object;
    }

    private static void SetProperty<T>(object target, string propertyName, T value) =>
        target.GetType().GetProperty(propertyName)!.SetValue(target, value);

    private static async Task<ConcurrentSeriesSeed> SeedConcurrentSeriesAsync(CompetitivePostgresFixture database)
    {
        var seed = new ConcurrentSeriesSeed(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            SeriesVersion: 7);
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO usuarios
                (id, nome, ativo, data_cadastro, data_atualizacao, email_confirmed, phone_number_confirmed,
                 two_factor_enabled, lockout_enabled, access_failed_count)
            VALUES (@actor_id, 'Ator concorrente', TRUE, NOW(), NOW(), FALSE, FALSE, FALSE, FALSE, 0);

            INSERT INTO seasons
                (id, nome, ano, ordem_no_ano, data_inicio, data_fim_exclusiva, estado, versao,
                 criada_em, atualizada_em, ativada_em, criada_por_usuario_id, atualizada_por_usuario_id)
            VALUES (@season_id, 'Season concorrente', 2026, 1, DATE '2026-01-01', DATE '2027-01-01',
                    'Ativa', 1, NOW(), NOW(), NOW(), @actor_id, @actor_id);

            INSERT INTO versoes_regras
                (id, season_id, numero, formato, modo_draft, publicada_em, publicada_por_usuario_id)
            VALUES (@rules_id, @season_id, 1, 'Md3', 'Padrao', NOW(), @actor_id);

            INSERT INTO series
                (id, season_id, versao_regras_id, tipo, formato, modo_draft, fearless_habilitado,
                 estado, agendada_para, revisao_necessaria, versao, criada_em, atualizada_em,
                 criada_por_usuario_id, atualizada_por_usuario_id)
            VALUES (@series_id, @season_id, @rules_id, 'Amistoso', 'Md3', 'Padrao', FALSE,
                    'EmAndamento', TIMESTAMPTZ '2026-07-29 02:30:00+00', FALSE, @series_version,
                    NOW(), NOW(), @actor_id, @actor_id);

            INSERT INTO lados_series (id, serie_id, ordem, tipo, origem_id, nome_snapshot)
            VALUES
                (@side_one_id, @series_id, 1, 'TimeOficial', gen_random_uuid(), 'Lado um'),
                (@side_two_id, @series_id, 2, 'TimeOficial', gen_random_uuid(), 'Lado dois');

            INSERT INTO partidas
                (id, serie_id, ordem, estado, conflito_fearless, versao, criada_em, atualizada_em)
            VALUES
                (@first_match_id, @series_id, 1, 'Rascunho', FALSE, 0, NOW(), NOW()),
                (@second_match_id, @series_id, 2, 'Rascunho', FALSE, 0, NOW(), NOW());
            """;
        command.Parameters.AddWithValue("actor_id", seed.ActorId);
        command.Parameters.AddWithValue("season_id", seed.SeasonId);
        command.Parameters.AddWithValue("rules_id", seed.RulesId);
        command.Parameters.AddWithValue("series_id", seed.SeriesId);
        command.Parameters.AddWithValue("series_version", seed.SeriesVersion);
        command.Parameters.AddWithValue("side_one_id", seed.LadoIds[0]);
        command.Parameters.AddWithValue("side_two_id", seed.LadoIds[1]);
        command.Parameters.AddWithValue("first_match_id", seed.PrimeiraPartidaId);
        command.Parameters.AddWithValue("second_match_id", seed.SegundaPartidaId);
        await command.ExecuteNonQueryAsync();
        return seed;
    }

    private sealed record DailyContext(
        CurrentActor Ator,
        IReadOnlyCollection<Jogador> Jogadores,
        IReadOnlyCollection<Guid> CapitaesIds,
        DraftMontagem Draft,
        Season Season,
        Competicao Competicao,
        Rodada Rodada,
        VersaoRegras Regras,
        Mock<IDraftMontagemRepository> DraftRepository,
        Mock<ICalendarioCompetitivoRepository> CalendarioRepository,
        Mock<ICompeticaoRepository> CompeticaoRepository,
        RecordingAuthorization Authorization);

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

    private sealed class RecordingAuthorization : ICompetitiveAuthorizationService
    {
        public CompetitiveAuthorizationContext? LastContext { get; private set; }

        public Task<bool> AuthorizeAsync(
            CompetitiveAuthorizationContext context,
            CancellationToken cancellationToken)
        {
            LastContext = context;
            return Task.FromResult(true);
        }

        public Task<IReadOnlyCollection<string>> GetAllowedActionsAsync(
            IReadOnlyCollection<CompetitiveAuthorizationContext> actions,
            CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<string>>([]);
    }

    private sealed class FixedClock : ISystemClock
    {
        public DateTimeOffset UtcNow => Agora;
    }

    private sealed record ConcurrentSeriesSeed(
        Guid ActorId,
        Guid SeasonId,
        Guid RulesId,
        Guid SeriesId,
        Guid PrimeiraPartidaId,
        Guid SegundaPartidaId,
        Guid PrimeiroLadoId,
        Guid SegundoLadoId,
        long SeriesVersion)
    {
        public IReadOnlyList<Guid> LadoIds => [PrimeiroLadoId, SegundoLadoId];
    }

    private sealed class ConcurrentSaveBarrier(int participants) : SaveChangesInterceptor
    {
        private readonly TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int arrivals;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref arrivals) == participants)
            {
                ready.SetResult();
            }

            await ready.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            return result;
        }
    }
}
