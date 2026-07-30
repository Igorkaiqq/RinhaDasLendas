using FluentAssertions;
using Moq;
using RinhaDasLendas.Application.Commands.DraftMontagens;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Enums;
using RinhaDasLendas.Application.Handlers.DraftMontagens;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Validators;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Repositories;
using RinhaDasLendas.Tests.Jogadores;

namespace RinhaDasLendas.Tests.Application;

public sealed class DraftMontagemRealtimeMutationBehaviorMatrixTests
{
    public static TheoryData<MutationHandler> Handlers => new(Enum.GetValues<MutationHandler>());

    [Theory]
    [MemberData(nameof(Handlers))]
    public async Task MutacaoVisivel_DeveSalvarAntesDePublicarUmaVezEPreservarRetorno(MutationHandler handler)
    {
        var scenario = CreateScenario(handler);

        var result = await scenario.ExecuteAsync();

        scenario.Sequence.Should().Equal("save", "publish");
        scenario.Publisher.Verify(
            item => item.PublishAfterCommitAsync(scenario.Draft.Id, scenario.Availability),
            Times.Once);
        result.Should().BeOfType(scenario.ResultType);
        GetResultDraftId(result).Should().Be(scenario.Draft.Id);
        GetResultStatus(result).Should().Be(scenario.Draft.Status.ToString());
    }

    [Theory]
    [MemberData(nameof(Handlers))]
    public async Task RepeticaoNoOpRejeitadaOuConflitante_DevePublicarZeroVezes(MutationHandler handler)
    {
        var scenario = CreateScenario(handler);
        await scenario.ExecuteAsync();
        scenario.Sequence.Clear();
        scenario.Repository.Invocations.Clear();
        scenario.Publisher.Invocations.Clear();
        scenario.PrepareNoPublish();

        object? result = null;
        DomainException? exception = null;
        try
        {
            result = await scenario.ExecuteAsync();
        }
        catch (DomainException caught)
        {
            exception = caught;
        }

        switch (scenario.NoPublishOutcome)
        {
            case NoPublishOutcome.NoOp:
                exception.Should().BeNull();
                result.Should().NotBeNull();
                break;
            case NoPublishOutcome.Rejected:
                exception.Should().NotBeNull();
                break;
            case NoPublishOutcome.Conflict:
                exception.Should().NotBeNull();
                exception!.MessageCode.Should().Be(MessageCodes.DraftStateConflict);
                break;
        }

        scenario.Sequence.Should().BeEmpty();
        scenario.Repository.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        scenario.Repository.Verify(item => item.TrySaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        scenario.Publisher.Verify(
            item => item.PublishAfterCommitAsync(It.IsAny<Guid>(), It.IsAny<DraftMontagemAvailabilityChange>()),
            Times.Never);
    }

    private static MutationScenario CreateScenario(MutationHandler handler)
    {
        return handler switch
        {
            MutationHandler.ConfirmarPresenca => ConfirmPresenceScenario(),
            MutationHandler.CancelarPresenca => CancelPresenceScenario(),
            MutationHandler.AdicionarPresencaManual => AddManualPresenceScenario(),
            MutationHandler.RemoverPresencaManual => RemoveManualPresenceScenario(),
            MutationHandler.EncerrarPresenca => ClosePresenceScenario(),
            MutationHandler.ReabrirPresenca => ReopenPresenceScenario(),
            MutationHandler.SelecionarModo => SelectModeScenario(),
            MutationHandler.DefinirCapitaes => DefineCaptainsScenario(),
            MutationHandler.SortearCapitaes => DrawCaptainsScenario(),
            MutationHandler.DefinirOrdemEscolha => DefinePickOrderScenario(),
            MutationHandler.IniciarTempoReal => StartRealtimeScenario(),
            MutationHandler.RegistrarPick => RegisterPickScenario(),
            MutationHandler.AvancarTimeout => AdvanceTimeoutScenario(),
            MutationHandler.SubstituirReserva => SubstituteReserveScenario(),
            MutationHandler.SalvarLayout => SaveLayoutScenario(),
            MutationHandler.Finalizar => FinalizeScenario(),
            MutationHandler.Cancelar => CancelScenario(),
            MutationHandler.Arquivar => ArchiveScenario(),
            MutationHandler.Restaurar => RestoreScenario(),
            _ => throw new ArgumentOutOfRangeException(nameof(handler), handler, null),
        };
    }

    private static MutationScenario ConfirmPresenceScenario()
    {
        var fixture = PresenceFixture.Create(1, 2, confirm: false);
        var scenario = MutationScenario.Create(fixture.Draft, fixture.Players, typeof(DraftMontagemResponseDto));
        scenario.ExecuteAsync = async () => await new ConfirmarPresencaDraftMontagemCommandHandler(
            scenario.Repository.Object,
            new CurrentUser(fixture.Players[0].UserId),
            Mock.Of<IDiscordIdentityLookupService>(),
            new ConfirmarPresencaDraftMontagemValidator(),
            scenario.Publisher.Object,
            Mock.Of<IDraftMontagemMetrics>()).Handle(
                new ConfirmarPresencaDraftMontagemCommand(
                    fixture.Draft.Id,
                    new ConfirmarPresencaDraftMontagemRequestDto(fixture.Players[0].UserId, null, nameof(DraftMontagemPresencaOrigem.Web))),
                CancellationToken.None);
        scenario.NoPublishOutcome = NoPublishOutcome.NoOp;
        return scenario;
    }

    private static MutationScenario CancelPresenceScenario()
    {
        var fixture = PresenceFixture.Create(1, 2);
        var scenario = MutationScenario.Create(fixture.Draft, fixture.Players, typeof(DraftMontagemResponseDto));
        scenario.ExecuteAsync = async () => await new CancelarPresencaDraftMontagemCommandHandler(
            scenario.Repository.Object,
            new CurrentUser(fixture.Players[0].UserId),
            Mock.Of<IDiscordIdentityLookupService>(),
            scenario.Publisher.Object,
            Mock.Of<IDraftMontagemMetrics>()).Handle(
                new CancelarPresencaDraftMontagemCommand(
                    fixture.Draft.Id,
                    new CancelarPresencaDraftMontagemRequestDto(fixture.Players[0].UserId, null)),
                CancellationToken.None);
        scenario.NoPublishOutcome = NoPublishOutcome.NoOp;
        return scenario;
    }

    private static MutationScenario AddManualPresenceScenario()
    {
        var fixture = PresenceFixture.Create(1, 2, confirm: false);
        var adminId = Guid.NewGuid();
        var scenario = MutationScenario.Create(fixture.Draft, fixture.Players, typeof(DraftMontagemResponseDto));
        scenario.ExecuteAsync = async () => await new AdicionarPresencaManualDraftMontagemCommandHandler(
            scenario.Repository.Object,
            new AdicionarPresencaManualDraftMontagemValidator(),
            new CurrentUser(adminId),
            scenario.Publisher.Object,
            Mock.Of<IDraftMontagemMetrics>()).Handle(
                new AdicionarPresencaManualDraftMontagemCommand(
                    fixture.Draft.Id,
                    new AdicionarPresencaManualDraftMontagemRequestDto(fixture.Players[0].Player.Id, "motivo")),
                CancellationToken.None);
        return scenario;
    }

    private static MutationScenario RemoveManualPresenceScenario()
    {
        var fixture = PresenceFixture.Create(1, 2, confirm: false);
        var adminId = Guid.NewGuid();
        fixture.Draft.AdicionarPresencaManual(fixture.Players[0].UserId, fixture.Players[0].Player.Id, adminId, "motivo");
        var scenario = MutationScenario.Create(fixture.Draft, fixture.Players, typeof(DraftMontagemResponseDto));
        scenario.ExecuteAsync = async () => await new RemoverPresencaManualDraftMontagemCommandHandler(
            scenario.Repository.Object,
            new RemoverPresencaManualDraftMontagemValidator(),
            new CurrentUser(adminId),
            scenario.Publisher.Object,
            Mock.Of<IDraftMontagemMetrics>()).Handle(
                new RemoverPresencaManualDraftMontagemCommand(
                    fixture.Draft.Id,
                    new RemoverPresencaManualDraftMontagemRequestDto(fixture.Players[0].Player.Id, "motivo")),
                CancellationToken.None);
        return scenario;
    }

    private static MutationScenario ClosePresenceScenario()
    {
        var fixture = PresenceFixture.Create(5, 2);
        var scenario = MutationScenario.Create(fixture.Draft, fixture.Players, typeof(DraftMontagemResponseDto));
        scenario.ExecuteAsync = async () => await new EncerrarPresencaDraftMontagemCommandHandler(
            scenario.Repository.Object,
            new EncerrarPresencaDraftMontagemValidator(),
            Mock.Of<IDraftMontagemMetrics>(),
            scenario.Publisher.Object).Handle(
                new EncerrarPresencaDraftMontagemCommand(
                    fixture.Draft.Id,
                    new EncerrarPresencaDraftMontagemRequestDto(true, 2)),
                CancellationToken.None);
        return scenario;
    }

    private static MutationScenario ReopenPresenceScenario()
    {
        var fixture = PresenceFixture.CreateClosed(5, 2);
        var adminId = Guid.NewGuid();
        var scenario = MutationScenario.Create(fixture.Draft, fixture.Players, typeof(DraftMontagemResponseDto));
        scenario.ExecuteAsync = async () => await new ReabrirPresencaDraftMontagemCommandHandler(
            scenario.Repository.Object,
            new CurrentUser(adminId),
            scenario.Publisher.Object).Handle(new ReabrirPresencaDraftMontagemCommand(fixture.Draft.Id), CancellationToken.None);
        return scenario;
    }

    private static MutationScenario SelectModeScenario()
    {
        var fixture = PresenceFixture.CreateClosed(5, 2);
        var scenario = MutationScenario.Create(fixture.Draft, fixture.Players, typeof(DraftMontagemResponseDto));
        scenario.ExecuteAsync = async () => await new SelecionarModoDraftMontagemCommandHandler(
            scenario.Repository.Object,
            new SelecionarModoDraftMontagemValidator(),
            scenario.Publisher.Object).Handle(
                new SelecionarModoDraftMontagemCommand(
                    fixture.Draft.Id,
                new SelecionarModoDraftMontagemRequestDto(nameof(DraftMontagemModo.Manual))),
                CancellationToken.None);
        scenario.NoPublishOutcome = NoPublishOutcome.NoOp;
        return scenario;
    }

    private static MutationScenario DefineCaptainsScenario()
    {
        var fixture = PresenceFixture.CreateWithMode(5, 2, DraftMontagemModo.TempoReal);
        var captains = fixture.Players.Take(2).Select(player => player.Player.Id).ToList();
        var scenario = MutationScenario.Create(fixture.Draft, fixture.Players, typeof(DraftMontagemResponseDto));
        scenario.ExecuteAsync = async () => await new DefinirCapitaesDraftMontagemCommandHandler(
            scenario.Repository.Object,
            new DefinirCapitaesDraftMontagemValidator(),
            scenario.Publisher.Object).Handle(
                new DefinirCapitaesDraftMontagemCommand(
                    fixture.Draft.Id,
                    new DefinirCapitaesDraftMontagemRequestDto(captains)),
                CancellationToken.None);
        return scenario;
    }

    private static MutationScenario DrawCaptainsScenario()
    {
        var fixture = PresenceFixture.Create(4, 2, confirm: false);
        var playersIds = fixture.Players.Select(player => player.Player.Id).ToList();
        var draft = new DraftMontagem(
            "Rinha",
            null,
            2,
            DraftMontagemCriterioCapitaes.Manual,
            playersIds,
            playersIds.Take(2).ToList());
        var scenario = MutationScenario.Create(draft, fixture.Players, typeof(DraftMontagemResponseDto));
        scenario.ExecuteAsync = async () => await new SortearCapitaesDraftMontagemCommandHandler(
            scenario.Repository.Object,
            scenario.Publisher.Object).Handle(new SortearCapitaesDraftMontagemCommand(draft.Id), CancellationToken.None);
        scenario.PrepareNoPublish = () => typeof(DraftMontagem).GetProperty(nameof(DraftMontagem.Status))!
            .SetValue(draft, DraftMontagemStatus.Finalizada);
        return scenario;
    }

    private static MutationScenario DefinePickOrderScenario()
    {
        var fixture = PresenceFixture.CreateWithCaptains(5, 2);
        var captains = fixture.Draft.Times.Select(team => team.CapitaoId!.Value).ToList();
        var scenario = MutationScenario.Create(fixture.Draft, fixture.Players, typeof(DraftMontagemResponseDto));
        scenario.ExecuteAsync = async () => await new DefinirOrdemEscolhaDraftMontagemCommandHandler(
            scenario.Repository.Object,
            new DefinirOrdemEscolhaDraftMontagemValidator(),
            scenario.Publisher.Object).Handle(
                new DefinirOrdemEscolhaDraftMontagemCommand(
                    fixture.Draft.Id,
                    new DefinirOrdemEscolhaDraftMontagemRequestDto(nameof(DraftMontagemOrdemEscolhaModo.Manual), captains)),
                CancellationToken.None);
        return scenario;
    }

    private static MutationScenario StartRealtimeScenario()
    {
        var fixture = PresenceFixture.CreateOrdered(5, 2);
        var scenario = MutationScenario.Create(fixture.Draft, fixture.Players, typeof(DraftMontagemRealtimeStateDto));
        scenario.ExecuteAsync = async () => await new IniciarDraftMontagemTempoRealCommandHandler(
            scenario.Repository.Object,
            new CurrentUser(null),
            scenario.Publisher.Object).Handle(new IniciarDraftMontagemTempoRealCommand(fixture.Draft.Id), CancellationToken.None);
        return scenario;
    }

    private static MutationScenario RegisterPickScenario()
    {
        var fixture = PresenceFixture.CreateStarted(5, 2);
        var captain = fixture.Players.Single(player => player.Player.Id == fixture.Draft.TurnoAtualCapitaoId);
        var pickedId = fixture.Draft.Participantes.First(item => item.Estado == DraftMontagemParticipanteEstado.Livre).JogadorId;
        var scenario = MutationScenario.Create(fixture.Draft, fixture.Players, typeof(DraftMontagemRealtimeStateDto));
        scenario.ExecuteAsync = async () => await new RegistrarPickDraftMontagemCommandHandler(
            scenario.Repository.Object,
            new CurrentUser(captain.UserId),
            new RegistrarPickDraftMontagemValidator(),
            scenario.Publisher.Object,
            Mock.Of<IDraftMontagemMetrics>()).Handle(
                new RegistrarPickDraftMontagemCommand(
                    fixture.Draft.Id,
                    new RegistrarPickDraftMontagemRequestDto(pickedId)),
                CancellationToken.None);
        return scenario;
    }

    private static MutationScenario AdvanceTimeoutScenario()
    {
        var fixture = PresenceFixture.CreateStarted(5, 2);
        typeof(DraftMontagem).GetProperty(nameof(DraftMontagem.TurnoExpiraEm))!
            .SetValue(fixture.Draft, DateTimeOffset.UtcNow.AddSeconds(-1));
        var scenario = MutationScenario.Create(fixture.Draft, fixture.Players, typeof(DraftMontagemRealtimeStateDto));
        scenario.ExecuteAsync = async () => await new AvancarTurnoDraftMontagemTimeoutCommandHandler(
            scenario.Repository.Object,
            new CurrentUser(null),
            scenario.Publisher.Object,
            Mock.Of<IDraftMontagemMetrics>()).Handle(
                new AvancarTurnoDraftMontagemTimeoutCommand(fixture.Draft.Id),
                CancellationToken.None);
        scenario.NoPublishOutcome = NoPublishOutcome.NoOp;
        return scenario;
    }

    private static MutationScenario SubstituteReserveScenario()
    {
        var fixture = PresenceFixture.CreateStarted(5, 2);
        var team = fixture.Draft.Times.Single(item => item.CapitaoId == fixture.Draft.TurnoAtualCapitaoId);
        var reserveId = fixture.Draft.Participantes.Single(item => item.Estado == DraftMontagemParticipanteEstado.Reserva).JogadorId;
        var adminId = Guid.NewGuid();
        var request = new SubstituirReservaDraftMontagemRequestDto(
            team.Id,
            team.CapitaoId!.Value,
            reserveId,
            reserveId,
            "motivo");
        var scenario = MutationScenario.Create(fixture.Draft, fixture.Players, typeof(DraftMontagemRealtimeStateDto));
        scenario.ExecuteAsync = async () => await new SubstituirReservaDraftMontagemCommandHandler(
            scenario.Repository.Object,
            new CurrentUser(adminId),
            new SubstituirReservaDraftMontagemValidator(),
            scenario.Publisher.Object).Handle(
                new SubstituirReservaDraftMontagemCommand(fixture.Draft.Id, request),
                CancellationToken.None);
        return scenario;
    }

    private static MutationScenario SaveLayoutScenario()
    {
        var fixture = PresenceFixture.CreateWithMode(5, 2, DraftMontagemModo.Manual);
        var request = CreateLayoutRequest(fixture.Draft);
        var scenario = MutationScenario.Create(fixture.Draft, fixture.Players, typeof(DraftMontagemResponseDto));
        scenario.ExecuteAsync = async () => await new SalvarLayoutDraftMontagemCommandHandler(
            scenario.Repository.Object,
            new SalvarLayoutDraftMontagemValidator(),
            scenario.Publisher.Object).Handle(
                new SalvarLayoutDraftMontagemCommand(fixture.Draft.Id, request),
                CancellationToken.None);
        scenario.NoPublishOutcome = NoPublishOutcome.Conflict;
        return scenario;
    }

    private static MutationScenario FinalizeScenario()
    {
        var fixture = PresenceFixture.CreateWithMode(4, 2, DraftMontagemModo.Manual);
        var request = CreateLayoutRequest(fixture.Draft);
        fixture.Draft.SalvarLayout(
            request.Times.Select(DraftMontagemHandlerHelpersForTests.ToDomain).ToList(),
            request.Livres.Select(DraftMontagemHandlerHelpersForTests.ToDomain).ToList(),
            request.Reservas.Select(DraftMontagemHandlerHelpersForTests.ToDomain).ToList());
        var scenario = MutationScenario.Create(fixture.Draft, fixture.Players, typeof(DraftMontagemResponseDto));
        scenario.ExecuteAsync = async () => await new FinalizarDraftMontagemCommandHandler(
            scenario.Repository.Object,
            scenario.Publisher.Object).Handle(new FinalizarDraftMontagemCommand(fixture.Draft.Id), CancellationToken.None);
        return scenario;
    }

    private static MutationScenario CancelScenario()
    {
        var fixture = PresenceFixture.Create(0, 2);
        var scenario = MutationScenario.Create(fixture.Draft, fixture.Players, typeof(DraftMontagemResponseDto));
        scenario.ExecuteAsync = async () => await new CancelarDraftMontagemCommandHandler(
            scenario.Repository.Object,
            new CancelarDraftMontagemValidator(),
            new CurrentUser(Guid.NewGuid()),
            scenario.Publisher.Object,
            Mock.Of<IDraftMontagemMetrics>()).Handle(
                new CancelarDraftMontagemCommand(fixture.Draft.Id, new CancelarDraftMontagemRequestDto("motivo")),
                CancellationToken.None);
        return scenario;
    }

    private static MutationScenario ArchiveScenario()
    {
        var fixture = PresenceFixture.Create(0, 2);
        var adminId = Guid.NewGuid();
        fixture.Draft.Cancelar("motivo", adminId);
        var request = new ArquivarDraftMontagemRequestDto("motivo", fixture.Draft.VersaoEstado);
        var scenario = MutationScenario.Create(
            fixture.Draft,
            fixture.Players,
            typeof(DraftMontagemArquivamentoResultadoDto),
            DraftMontagemAvailabilityChange.Archived);
        scenario.ExecuteAsync = async () => await new ArquivarDraftMontagemCommandHandler(
            scenario.Repository.Object,
            new ArquivarDraftMontagemValidator(),
            new CurrentUser(adminId),
            scenario.Publisher.Object).Handle(
                new ArquivarDraftMontagemCommand(fixture.Draft.Id, request),
                CancellationToken.None);
        scenario.NoPublishOutcome = NoPublishOutcome.NoOp;
        return scenario;
    }

    private static MutationScenario RestoreScenario()
    {
        var fixture = PresenceFixture.Create(0, 2);
        var adminId = Guid.NewGuid();
        fixture.Draft.Cancelar("motivo", adminId);
        fixture.Draft.Arquivar("motivo", adminId, DateTimeOffset.UtcNow);
        var request = new RestaurarDraftMontagemRequestDto(fixture.Draft.VersaoEstado);
        var scenario = MutationScenario.Create(
            fixture.Draft,
            fixture.Players,
            typeof(DraftMontagemArquivamentoResultadoDto),
            DraftMontagemAvailabilityChange.Restored);
        scenario.ExecuteAsync = async () => await new RestaurarDraftMontagemCommandHandler(
            scenario.Repository.Object,
            new RestaurarDraftMontagemValidator(),
            new CurrentUser(adminId),
            scenario.Publisher.Object).Handle(
                new RestaurarDraftMontagemCommand(fixture.Draft.Id, request),
                CancellationToken.None);
        scenario.NoPublishOutcome = NoPublishOutcome.NoOp;
        return scenario;
    }

    private static SalvarLayoutDraftMontagemRequestDto CreateLayoutRequest(DraftMontagem draft)
    {
        var starters = draft.Participantes.Where(item => item.Estado == DraftMontagemParticipanteEstado.Livre).ToList();
        return new SalvarLayoutDraftMontagemRequestDto(
            draft.Times.Select((team, index) => new DraftMontagemLayoutTimeDto(
                team.Id,
                team.Nome,
                null,
                starters.Skip(index * draft.TamanhoEquipe).Take(draft.TamanhoEquipe).Select((player, order) =>
                    new DraftMontagemLayoutParticipanteDto(player.JogadorId, order + 1, null)).ToList())).ToList(),
            [],
            draft.Participantes.Where(item => item.Estado == DraftMontagemParticipanteEstado.Reserva).Select((player, order) =>
                new DraftMontagemLayoutParticipanteDto(player.JogadorId, order + 1, null)).ToList(),
            draft.VersaoEstado);
    }

    private static Guid GetResultDraftId(object? result) => result switch
    {
        DraftMontagemResponseDto response => response.Id,
        DraftMontagemRealtimeStateDto state => state.Montagem.Id,
        DraftMontagemArquivamentoResultadoDto archive => archive.Id,
        _ => Guid.Empty,
    };

    private static string? GetResultStatus(object? result) => result switch
    {
        DraftMontagemResponseDto response => response.Status,
        DraftMontagemRealtimeStateDto state => state.Montagem.Status,
        DraftMontagemArquivamentoResultadoDto archive => archive.Status,
        _ => null,
    };

    public enum MutationHandler
    {
        ConfirmarPresenca,
        CancelarPresenca,
        AdicionarPresencaManual,
        RemoverPresencaManual,
        EncerrarPresenca,
        ReabrirPresenca,
        SelecionarModo,
        DefinirCapitaes,
        SortearCapitaes,
        DefinirOrdemEscolha,
        IniciarTempoReal,
        RegistrarPick,
        AvancarTimeout,
        SubstituirReserva,
        SalvarLayout,
        Finalizar,
        Cancelar,
        Arquivar,
        Restaurar,
    }

    public enum NoPublishOutcome
    {
        NoOp,
        Rejected,
        Conflict,
    }

    private sealed class MutationScenario
    {
        private MutationScenario(
            DraftMontagem draft,
            Mock<IDraftMontagemRepository> repository,
            Mock<IDraftMontagemRealtimePublisher> publisher,
            List<string> sequence,
            Type resultType,
            DraftMontagemAvailabilityChange availability)
        {
            Draft = draft;
            Repository = repository;
            Publisher = publisher;
            Sequence = sequence;
            ResultType = resultType;
            Availability = availability;
        }

        public DraftMontagem Draft { get; }
        public Mock<IDraftMontagemRepository> Repository { get; }
        public Mock<IDraftMontagemRealtimePublisher> Publisher { get; }
        public List<string> Sequence { get; }
        public Type ResultType { get; }
        public DraftMontagemAvailabilityChange Availability { get; }
        public Func<Task<object?>> ExecuteAsync { get; set; } = null!;
        public Action PrepareNoPublish { get; set; } = () => { };
        public NoPublishOutcome NoPublishOutcome { get; set; } = NoPublishOutcome.Rejected;

        public static MutationScenario Create(
            DraftMontagem draft,
            IReadOnlyCollection<TestPlayer> players,
            Type resultType,
            DraftMontagemAvailabilityChange availability = DraftMontagemAvailabilityChange.None)
        {
            var sequence = new List<string>();
            var repository = new Mock<IDraftMontagemRepository>();
            repository.Setup(item => item.GetByIdAsync(draft.Id, It.IsAny<CancellationToken>())).ReturnsAsync(draft);
            repository.Setup(item => item.ReloadByIdAsync(draft.Id, It.IsAny<CancellationToken>())).ReturnsAsync(draft);
            repository.Setup(item => item.GetByIdIncludingArchivedAsync(draft.Id, It.IsAny<CancellationToken>())).ReturnsAsync(draft);
            repository.Setup(item => item.ReloadByIdIncludingArchivedAsync(draft.Id, It.IsAny<CancellationToken>())).ReturnsAsync(draft);
            repository.Setup(item => item.GetJogadoresByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyCollection<Guid> ids, CancellationToken _) => players.Where(player => ids.Contains(player.Player.Id)).Select(player => player.Player).ToList());
            repository.Setup(item => item.GetCapitaesElegiveisIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyCollection<Guid> ids, CancellationToken _) => ids);
            repository.Setup(item => item.GetJogadorByUsuarioIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid userId, CancellationToken _) => players.SingleOrDefault(player => player.UserId == userId)?.Player);
            repository.Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Callback(() => sequence.Add("save"))
                .Returns(Task.CompletedTask);
            repository.Setup(item => item.TrySaveChangesAsync(It.IsAny<CancellationToken>()))
                .Callback(() => sequence.Add("save"))
                .ReturnsAsync(DraftMontagemSaveResultado.Persistido);
            var publisher = new Mock<IDraftMontagemRealtimePublisher>();
            publisher.Setup(item => item.PublishAfterCommitAsync(draft.Id, It.IsAny<DraftMontagemAvailabilityChange>()))
                .Callback(() => sequence.Add("publish"))
                .Returns(Task.CompletedTask);
            return new MutationScenario(draft, repository, publisher, sequence, resultType, availability);
        }
    }

    private sealed record PresenceFixture(DraftMontagem Draft, IReadOnlyList<TestPlayer> Players)
    {
        public static PresenceFixture Create(int count, int teamSize, bool confirm = true)
        {
            var players = Enumerable.Range(1, count).Select(index =>
            {
                var player = JogadorTestData.JogadorAtivo($"Jogador {index}");
                var userId = Guid.NewGuid();
                player.VincularUsuario(userId);
                return new TestPlayer(userId, player);
            }).ToList();
            var draft = DraftMontagem.CriarPorPresenca("Rinha", null, teamSize);
            if (confirm)
            {
                foreach (var player in players)
                {
                    draft.ConfirmarPresenca(player.UserId, player.Player.Id, null, DraftMontagemPresencaOrigem.Web);
                }
            }
            return new PresenceFixture(draft, players);
        }

        public static PresenceFixture CreateClosed(int count, int teamSize)
        {
            var fixture = Create(count, teamSize);
            fixture.Draft.EncerrarPresenca(true, teamSize);
            return fixture;
        }

        public static PresenceFixture CreateWithMode(int count, int teamSize, DraftMontagemModo mode)
        {
            var fixture = CreateClosed(count, teamSize);
            fixture.Draft.SelecionarModo(mode, fixture.Players.Select(player => player.Player.Id).ToHashSet());
            return fixture;
        }

        public static PresenceFixture CreateWithCaptains(int count, int teamSize)
        {
            var fixture = CreateWithMode(count, teamSize, DraftMontagemModo.TempoReal);
            var captains = fixture.Players.Take(2).Select(player => player.Player.Id).ToList();
            fixture.Draft.DefinirCapitaes(captains, captains.ToHashSet());
            return fixture;
        }

        public static PresenceFixture CreateOrdered(int count, int teamSize)
        {
            var fixture = CreateWithCaptains(count, teamSize);
            var captains = fixture.Draft.Times.Select(team => team.CapitaoId!.Value).ToList();
            fixture.Draft.DefinirOrdemEscolha(DraftMontagemOrdemEscolhaModo.Manual, captains);
            return fixture;
        }

        public static PresenceFixture CreateStarted(int count, int teamSize)
        {
            var fixture = CreateOrdered(count, teamSize);
            fixture.Draft.IniciarTempoReal(
                DateTimeOffset.UtcNow,
                fixture.Draft.Times.Select(team => team.CapitaoId!.Value).ToHashSet());
            return fixture;
        }
    }

    private sealed record TestPlayer(Guid UserId, Jogador Player);

    private sealed record CurrentUser(Guid? UserId) : ICurrentUser
    {
        public IReadOnlyCollection<string> Roles => [];
        public string? IpAddress => null;
        public string? UserAgent => null;
    }

    private static class DraftMontagemHandlerHelpersForTests
    {
        public static DraftMontagemLayoutTime ToDomain(DraftMontagemLayoutTimeDto time) => new(
            time.TimeId,
            time.Nome,
            time.CapitaoId,
            time.Jogadores.Select(ToDomain).ToList());

        public static DraftMontagemLayoutParticipante ToDomain(DraftMontagemLayoutParticipanteDto player) =>
            new(player.JogadorId, player.Ordem, null);
    }
}
