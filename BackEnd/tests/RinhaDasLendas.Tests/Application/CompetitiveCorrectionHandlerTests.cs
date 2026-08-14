using FluentAssertions;
using FluentValidation;
using Moq;
using RinhaDasLendas.Application.Commands.Partidas;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Handlers.Partidas;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Security;
using RinhaDasLendas.Application.Validators;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Tests.Application;

public sealed class CompetitiveCorrectionHandlerTests
{
    private static readonly DateTimeOffset Agora = new(2026, 8, 11, 1, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CorrigirPartida_DeDoisAZeroParaUmAUm_SemConfirmarAnulacao_DeveRecusarSemPersistir()
    {
        var fixture = new HandlerFixture();
        var scenario = fixture.CriarSerieMd3Concluida(1, 1);
        fixture.SetupSerie(scenario.Serie);

        var act = () => fixture.CreateCorrectHandler().Handle(
            fixture.CriarComandoCorrecao(
                scenario.Partidas[1].Id,
                scenario.LadoDoisId,
                scenario.Serie.Versao,
                anularSerieSeInconclusiva: false),
            CancellationToken.None);

        (await act.Should().ThrowAsync<DomainException>()).Which.MessageCode
            .Should().Be(MessageCodes.CorrectionAnnulConfirmationRequired);
        scenario.Serie.Estado.Should().Be(SerieEstado.Concluida);
        scenario.Serie.Resultado.VitoriasLadoUm.Should().Be(2);
        scenario.Serie.Resultado.VitoriasLadoDois.Should().Be(0);
        fixture.VerifyNoWrite();
    }

    [Fact]
    public async Task CorrigirPartida_DeDoisAZeroParaUmAUm_ComConfirmacaoExplicita_DeveInverterVencedorEAnularSerie()
    {
        var fixture = new HandlerFixture();
        var scenario = fixture.CriarSerieMd3Concluida(1, 1);
        fixture.SetupSerie(scenario.Serie);
        var versaoAnterior = scenario.Serie.Versao;
        fixture.ExpectOrderedWrites(
            audit => audit.Acao == AcaoAuditoriaCompetitiva.PartidaCorrigida,
            audit => audit.Acao == AcaoAuditoriaCompetitiva.SerieAnulada);

        var result = await fixture.CreateCorrectHandler().Handle(
            fixture.CriarComandoCorrecao(
                scenario.Partidas[1].Id,
                scenario.LadoDoisId,
                versaoAnterior,
                anularSerieSeInconclusiva: true),
            CancellationToken.None);

        scenario.Partidas[1].LadoVencedorId.Should().Be(scenario.LadoDoisId);
        result.Estado.Should().Be(SerieEstado.Anulada);
        result.LadoVencedorId.Should().BeNull();
        result.Resultado.VitoriasLadoUm.Should().Be(1);
        result.Resultado.VitoriasLadoDois.Should().Be(1);
        result.Versao.Should().Be(versaoAnterior + 1, "a ETag da Serie deve mudar uma unica vez por correcao");
        result.AtualizadaPorUsuarioId.Should().Be(fixture.ActorId);
        fixture.VerifyCorrectionAudit(
            scenario.Partidas[1].Id,
            scenario.Serie.Id,
            scenario.LadoUmId,
            scenario.LadoDoisId,
            includeSeriesAnnulment: true);
        fixture.VerifySingleSave();
    }

    [Fact]
    public async Task CorrigirPartidaDecisiva_ComPlacarAindaConclusivo_DeveInverterVencedorDaSerieSemAnular()
    {
        var fixture = new HandlerFixture();
        var scenario = fixture.CriarSerieMd3Concluida(1, 2, 1);
        fixture.SetupSerie(scenario.Serie);
        fixture.ExpectOrderedWrites(audit => audit.Acao == AcaoAuditoriaCompetitiva.PartidaCorrigida);

        var result = await fixture.CreateCorrectHandler().Handle(
            fixture.CriarComandoCorrecao(
                scenario.Partidas[2].Id,
                scenario.LadoDoisId,
                scenario.Serie.Versao,
                anularSerieSeInconclusiva: false),
            CancellationToken.None);

        result.Estado.Should().Be(SerieEstado.Concluida);
        result.LadoVencedorId.Should().Be(scenario.LadoDoisId);
        result.Resultado.VitoriasLadoUm.Should().Be(1);
        result.Resultado.VitoriasLadoDois.Should().Be(2);
        result.AtualizadaPorUsuarioId.Should().Be(fixture.ActorId);
        fixture.VerifyCorrectionAudit(
            scenario.Partidas[2].Id,
            scenario.Serie.Id,
            scenario.LadoUmId,
            scenario.LadoDoisId,
            includeSeriesAnnulment: false);
        fixture.VerifySingleSave();
    }

    [Fact]
    public async Task AnularPartidaDecisiva_DeSerieConcluida_DeveExigirConfirmacaoExplicita()
    {
        var fixture = new HandlerFixture();
        var scenario = fixture.CriarSerieMd3Concluida(1, 1);
        fixture.SetupSerie(scenario.Serie);
        var command = new AnnulMatchCommand(
            scenario.Partidas[1].Id,
            new AnnulMatchRequestDto(fixture.JustificativaTecnica, AnularSerieSeInconclusiva: false),
            scenario.Serie.Versao);

        var act = () => fixture.CreateAnnulHandler().Handle(command, CancellationToken.None);

        (await act.Should().ThrowAsync<DomainException>()).Which.MessageCode
            .Should().Be(MessageCodes.CorrectionAnnulConfirmationRequired);
        scenario.Partidas[1].Estado.Should().Be(PartidaEstado.Confirmada);
        scenario.Serie.Estado.Should().Be(SerieEstado.Concluida);
        fixture.VerifyNoWrite();
    }

    [Fact]
    public async Task AnularPartidaDecisiva_ComConfirmacao_DeveAnularPartidaESerieNaMesmaPersistencia()
    {
        var fixture = new HandlerFixture();
        var scenario = fixture.CriarSerieMd3Concluida(1, 1);
        fixture.SetupSerie(scenario.Serie);
        var versaoAnterior = scenario.Serie.Versao;
        fixture.ExpectOrderedWrites(
            audit => audit.Acao == AcaoAuditoriaCompetitiva.PartidaAnulada,
            audit => audit.Acao == AcaoAuditoriaCompetitiva.SerieAnulada);
        var command = new AnnulMatchCommand(
            scenario.Partidas[1].Id,
            new AnnulMatchRequestDto(fixture.JustificativaTecnica, AnularSerieSeInconclusiva: true),
            versaoAnterior);

        var result = await fixture.CreateAnnulHandler().Handle(command, CancellationToken.None);

        scenario.Partidas[1].Estado.Should().Be(PartidaEstado.Anulada);
        result.Estado.Should().Be(SerieEstado.Anulada);
        result.Resultado.VitoriasLadoUm.Should().Be(1);
        result.Resultado.VitoriasLadoDois.Should().Be(0);
        result.Versao.Should().Be(versaoAnterior + 1);
        result.AtualizadaPorUsuarioId.Should().Be(fixture.ActorId);
        fixture.VerifyAnnulmentAudit(scenario.Partidas[1].Id, scenario.Serie.Id);
        fixture.VerifySingleSave();
    }

    [Fact]
    public async Task CorrigirPartida_ComJustificativaTecnica_DeveSolicitarAutorizacaoEspecifica()
    {
        var fixture = new HandlerFixture();
        var scenario = fixture.CriarSerieMd3Concluida(1, 2, 1);
        fixture.SetupSerie(scenario.Serie);
        fixture.ExpectOrderedWrites(audit => audit.Acao == AcaoAuditoriaCompetitiva.PartidaCorrigida);
        CompetitiveAuthorizationContext? observedContext = null;
        fixture.Authorization
            .Setup(service => service.AuthorizeAsync(
                It.IsAny<CompetitiveAuthorizationContext>(),
                It.IsAny<CancellationToken>()))
            .Callback<CompetitiveAuthorizationContext, CancellationToken>((context, _) => observedContext = context)
            .ReturnsAsync(true);

        await fixture.CreateCorrectHandler().Handle(
            fixture.CriarComandoCorrecao(
                scenario.Partidas[2].Id,
                scenario.LadoDoisId,
                scenario.Serie.Versao,
                anularSerieSeInconclusiva: false),
            CancellationToken.None);

        observedContext.Should().Be(new CompetitiveAuthorizationContext(
            AuthPermissions.CanFinalizeMatches,
            "CorrecaoTecnica",
            nameof(Partida),
            scenario.Serie.Tipo.ToString(),
            SeasonState: null,
            HasStartedSeries: true,
            HasRequiredJustification: true));
        fixture.VerifySingleSave();
    }

    [Fact]
    public async Task AnularPartida_ComJustificativaTecnica_DeveSolicitarContextoDeAutorizacaoCompleto()
    {
        var fixture = new HandlerFixture();
        var scenario = fixture.CriarSerieMd3Concluida(1, 1);
        fixture.SetupSerie(scenario.Serie);
        fixture.ExpectOrderedWrites(
            audit => audit.Acao == AcaoAuditoriaCompetitiva.PartidaAnulada,
            audit => audit.Acao == AcaoAuditoriaCompetitiva.SerieAnulada);
        CompetitiveAuthorizationContext? observedContext = null;
        fixture.Authorization
            .Setup(service => service.AuthorizeAsync(
                It.IsAny<CompetitiveAuthorizationContext>(),
                It.IsAny<CancellationToken>()))
            .Callback<CompetitiveAuthorizationContext, CancellationToken>((context, _) => observedContext = context)
            .ReturnsAsync(true);

        await fixture.CreateAnnulHandler().Handle(
            new AnnulMatchCommand(
                scenario.Partidas[1].Id,
                new AnnulMatchRequestDto(fixture.JustificativaTecnica, AnularSerieSeInconclusiva: true),
                scenario.Serie.Versao),
            CancellationToken.None);

        observedContext.Should().Be(new CompetitiveAuthorizationContext(
            AuthPermissions.CanFinalizeMatches,
            "AnulacaoTecnica",
            nameof(Partida),
            scenario.Serie.Tipo.ToString(),
            SeasonState: null,
            HasStartedSeries: true,
            HasRequiredJustification: true));
        fixture.VerifySingleSave();
    }

    [Fact]
    public async Task CorrigirPartida_SemJustificativaTecnicaOuSemAutorizacao_DeveRecusarSemPersistir()
    {
        var fixture = new HandlerFixture();
        var scenario = fixture.CriarSerieMd3Concluida(1, 2, 1);
        fixture.SetupSerie(scenario.Serie);
        var semJustificativa = new CorrectMatchCommand(
            scenario.Partidas[2].Id,
            new CorrectMatchRequestDto(
                Lados: null,
                LadoVencedorId: scenario.LadoDoisId,
                MotivoTermino: MotivoTerminoPartida.Normal,
                Justificativa: " ",
                AnularSerieSeInconclusiva: false),
            scenario.Serie.Versao);

        var invalidAct = () => fixture.CreateCorrectHandler().Handle(semJustificativa, CancellationToken.None);

        await invalidAct.Should().ThrowAsync<ValidationException>();
        fixture.Authorization.Verify(service => service.AuthorizeAsync(
            It.IsAny<CompetitiveAuthorizationContext>(), It.IsAny<CancellationToken>()), Times.Never);
        fixture.VerifyNoWrite();

        fixture.Authorization
            .Setup(service => service.AuthorizeAsync(
                It.IsAny<CompetitiveAuthorizationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var unauthorizedAct = () => fixture.CreateCorrectHandler().Handle(
            fixture.CriarComandoCorrecao(
                scenario.Partidas[2].Id,
                scenario.LadoDoisId,
                scenario.Serie.Versao,
                anularSerieSeInconclusiva: false),
            CancellationToken.None);

        (await unauthorizedAct.Should().ThrowAsync<DomainException>()).Which.MessageCode
            .Should().Be(MessageCodes.CompetitiveAccessDenied);
        fixture.VerifyNoWrite();
    }

    [Fact]
    public async Task CorrigirPartida_ComETagObsoleta_DeveRecusarAntesDeAuditarOuSalvar()
    {
        var fixture = new HandlerFixture();
        var scenario = fixture.CriarSerieMd3Concluida(1, 2, 1);
        fixture.SetupSerie(scenario.Serie);

        var act = () => fixture.CreateCorrectHandler().Handle(
            fixture.CriarComandoCorrecao(
                scenario.Partidas[2].Id,
                scenario.LadoDoisId,
                scenario.Serie.Versao + 1,
                anularSerieSeInconclusiva: false),
            CancellationToken.None);

        (await act.Should().ThrowAsync<DomainException>()).Which.MessageCode
            .Should().Be(MessageCodes.CompetitiveResourceVersionStale);
        fixture.VerifyNoWrite();
    }

    private sealed class HandlerFixture
    {
        private readonly Guid _actorId = Guid.NewGuid();

        public HandlerFixture()
        {
            Actor.SetupGet(actor => actor.UserId).Returns(_actorId);
            Actor.SetupGet(actor => actor.Roles).Returns([AuthRoles.SuperAdmin]);
            Clock.Setup(clock => clock.GetUtcNow()).Returns(Agora);
            Authorization
                .Setup(service => service.AuthorizeAsync(
                    It.IsAny<CompetitiveAuthorizationContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
        }

        public string JustificativaTecnica => "Correção técnica autorizada após revisão do resultado.";
        public Guid ActorId => _actorId;
        public Mock<ISerieRepository> SeriesRepository { get; } = new();
        public Mock<ICompetitiveUnitOfWork> UnitOfWork { get; } = new(MockBehavior.Strict);
        public Mock<ICurrentActor> Actor { get; } = new();
        public Mock<ICompetitiveAuthorizationService> Authorization { get; } = new();
        public Mock<ICompetitiveAuditRepository> AuditRepository { get; } = new(MockBehavior.Strict);
        public Mock<TimeProvider> Clock { get; } = new();
        public List<RegistroAuditoriaCompetitiva> StagedAudits { get; } = [];

        public void SetupSerie(Serie serie)
        {
            SeriesRepository
                .Setup(repository => repository.GetAggregateByPartidaIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(serie);
        }

        public CorrectMatchCommand CriarComandoCorrecao(
            Guid partidaId,
            Guid novoVencedorId,
            long expectedVersion,
            bool anularSerieSeInconclusiva) => new(
                partidaId,
                new CorrectMatchRequestDto(
                    Lados: null,
                    LadoVencedorId: novoVencedorId,
                    MotivoTermino: MotivoTerminoPartida.Normal,
                    Justificativa: JustificativaTecnica,
                    AnularSerieSeInconclusiva: anularSerieSeInconclusiva),
                expectedVersion);

        public CorrectMatchCommandHandler CreateCorrectHandler() => new(
            SeriesRepository.Object,
            UnitOfWork.Object,
            Actor.Object,
            Authorization.Object,
            AuditRepository.Object,
            Clock.Object,
            new CorrectMatchCommandValidator(new CorrectMatchRequestDtoValidator()));

        public AnnulMatchCommandHandler CreateAnnulHandler() => new(
            SeriesRepository.Object,
            UnitOfWork.Object,
            Actor.Object,
            Authorization.Object,
            AuditRepository.Object,
            Clock.Object,
            new AnnulMatchCommandValidator(new AnnulMatchRequestDtoValidator()));

        public void ExpectOrderedWrites(params Func<RegistroAuditoriaCompetitiva, bool>[] expectedAudits)
        {
            var sequence = new MockSequence();
            foreach (var expectedAudit in expectedAudits)
            {
                AuditRepository
                    .InSequence(sequence)
                    .Setup(repository => repository.AddAsync(
                        It.Is<RegistroAuditoriaCompetitiva>(audit => expectedAudit(audit)),
                        It.IsAny<CancellationToken>()))
                    .Callback<RegistroAuditoriaCompetitiva, CancellationToken>((audit, _) => StagedAudits.Add(audit))
                    .Returns(Task.CompletedTask);
            }

            UnitOfWork
                .InSequence(sequence)
                .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        public CompletedSeriesScenario CriarSerieMd3Concluida(params int[] vencedores)
        {
            var serie = CriarSerie();
            serie.Iniciar(_actorId, Agora.AddHours(-1));
            var lados = serie.Lados.OrderBy(lado => lado.Ordem).ToArray();
            var partidas = new List<Partida>();

            for (var index = 0; index < vencedores.Length; index++)
            {
                var partida = serie.AdicionarPartida(_actorId, Agora.AddMinutes(index * 2 - 20));
                serie.RegistrarPicks(
                    partida.Id,
                    lados.SelectMany((lado, ladoIndex) => Enumerable.Range(1, 5)
                        .Select(ordem => (
                            lado.Id,
                            ChampionId: index * 10 + ladoIndex * 5 + ordem,
                            Ordem: ordem)))
                        .ToArray(),
                    _actorId,
                    Agora.AddMinutes(index * 2 - 20));
                serie.ConfirmarPartida(
                    partida.Id,
                    lados[vencedores[index] - 1].Id,
                    MotivoTerminoPartida.Normal,
                    _actorId,
                    Agora.AddMinutes(index * 2 - 19));
                partidas.Add(partida);
            }

            serie.Estado.Should().Be(SerieEstado.Concluida, "the test fixture must represent a completed Series");
            return new CompletedSeriesScenario(serie, partidas, lados[0].Id, lados[1].Id);
        }

        public void VerifyCorrectionAudit(
            Guid partidaId,
            Guid serieId,
            Guid previousWinnerId,
            Guid correctedWinnerId,
            bool includeSeriesAnnulment)
        {
            StagedAudits.Should().HaveCount(includeSeriesAnnulment ? 2 : 1);
            var matchAudit = StagedAudits[0];
            AssertFullAudit(
                matchAudit,
                RecursoCompetitivoTipo.Partida,
                partidaId,
                AcaoAuditoriaCompetitiva.PartidaCorrigida);
            matchAudit.ValorAnterior!.Campos[CampoSnapshotAuditoria.LadoVencedorId].Should().Be(previousWinnerId);
            matchAudit.ValorPosterior!.Campos[CampoSnapshotAuditoria.LadoVencedorId].Should().Be(correctedWinnerId);
            matchAudit.ValorAnterior.Campos[CampoSnapshotAuditoria.EstadoPartida].Should().Be(PartidaEstado.Confirmada);
            matchAudit.ValorPosterior.Campos[CampoSnapshotAuditoria.EstadoPartida].Should().Be(PartidaEstado.Confirmada);
            matchAudit.ValorAnterior.Campos[CampoSnapshotAuditoria.MotivoTerminoPartida]
                .Should().Be(MotivoTerminoPartida.Normal);
            matchAudit.ValorPosterior.Campos[CampoSnapshotAuditoria.MotivoTerminoPartida]
                .Should().Be(MotivoTerminoPartida.Normal);

            if (includeSeriesAnnulment)
            {
                var seriesAudit = StagedAudits[1];
                AssertFullAudit(
                    seriesAudit,
                    RecursoCompetitivoTipo.Serie,
                    serieId,
                    AcaoAuditoriaCompetitiva.SerieAnulada);
                seriesAudit.CorrelationId.Should().Be(matchAudit.CorrelationId);
                seriesAudit.ValorAnterior!.Campos[CampoSnapshotAuditoria.EstadoSerie].Should().Be(SerieEstado.Concluida);
                seriesAudit.ValorPosterior!.Campos[CampoSnapshotAuditoria.EstadoSerie].Should().Be(SerieEstado.Anulada);
            }
        }

        public void VerifyAnnulmentAudit(Guid partidaId, Guid serieId)
        {
            StagedAudits.Should().HaveCount(2);
            AssertFullAudit(
                StagedAudits[0],
                RecursoCompetitivoTipo.Partida,
                partidaId,
                AcaoAuditoriaCompetitiva.PartidaAnulada);
            AssertFullAudit(
                StagedAudits[1],
                RecursoCompetitivoTipo.Serie,
                serieId,
                AcaoAuditoriaCompetitiva.SerieAnulada);
            StagedAudits.Select(audit => audit.CorrelationId).Distinct().Should().ContainSingle();
            StagedAudits[0].ValorAnterior!.Campos[CampoSnapshotAuditoria.EstadoPartida].Should().Be(PartidaEstado.Confirmada);
            StagedAudits[0].ValorPosterior!.Campos[CampoSnapshotAuditoria.EstadoPartida].Should().Be(PartidaEstado.Anulada);
            StagedAudits[1].ValorAnterior!.Campos[CampoSnapshotAuditoria.EstadoSerie].Should().Be(SerieEstado.Concluida);
            StagedAudits[1].ValorPosterior!.Campos[CampoSnapshotAuditoria.EstadoSerie].Should().Be(SerieEstado.Anulada);
        }

        public void VerifySingleSave() =>
            UnitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        private void AssertFullAudit(
            RegistroAuditoriaCompetitiva audit,
            RecursoCompetitivoTipo resourceType,
            Guid resourceId,
            AcaoAuditoriaCompetitiva action)
        {
            audit.RecursoTipo.Should().Be(resourceType);
            audit.RecursoId.Should().Be(resourceId);
            audit.Acao.Should().Be(action);
            audit.AtorUsuarioId.Should().Be(_actorId, "o ator autenticado e a unica fonte confiavel");
            audit.Capacidade.Should().Be(AuthPermissions.CanFinalizeMatches);
            audit.Justificativa.Should().Be(JustificativaTecnica);
            audit.ValorAnterior.Should().NotBeNull();
            audit.ValorPosterior.Should().NotBeNull();
            audit.CorrelationId.Should().NotBeEmpty();
            audit.OcorridoEm.Should().Be(Agora);
        }

        public void VerifyNoWrite()
        {
            AuditRepository.Verify(repository => repository.AddAsync(
                It.IsAny<RegistroAuditoriaCompetitiva>(), It.IsAny<CancellationToken>()), Times.Never);
            UnitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        private static Serie CriarSerie()
        {
            var lados = new[]
            {
                new LadoSerie(Guid.NewGuid(), 1, LadoSerieTipo.TimeOficial, Guid.NewGuid(), "Azul", "AZL", null, null, []),
                new LadoSerie(Guid.NewGuid(), 2, LadoSerieTipo.TimeOficial, Guid.NewGuid(), "Vermelho", "VRM", null, null, []),
            };

            return new Serie(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                eventoId: null,
                draftMontagemId: null,
                SerieTipo.ConfrontoOficial,
                SerieFormato.Md3,
                ModoDraft.Padrao,
                fearlessHabilitado: false,
                Agora.AddHours(-2),
                dataLocal: null,
                Guid.NewGuid(),
                Agora.AddHours(-3),
                lados);
        }
    }

    private sealed record CompletedSeriesScenario(
        Serie Serie,
        IReadOnlyList<Partida> Partidas,
        Guid LadoUmId,
        Guid LadoDoisId);
}
