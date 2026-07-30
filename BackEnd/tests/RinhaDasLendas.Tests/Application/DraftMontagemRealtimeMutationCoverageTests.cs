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

namespace RinhaDasLendas.Tests.Application;

public sealed class DraftMontagemRealtimeMutationCoverageTests
{
    public static TheoryData<Type> HandlersNaoPublicacao => new()
    {
        typeof(ConfirmarPresencaDraftMontagemCommandHandler),
        typeof(CancelarPresencaDraftMontagemCommandHandler),
        typeof(AdicionarPresencaManualDraftMontagemCommandHandler),
        typeof(RemoverPresencaManualDraftMontagemCommandHandler),
        typeof(EncerrarPresencaDraftMontagemCommandHandler),
        typeof(ReabrirPresencaDraftMontagemCommandHandler),
        typeof(SelecionarModoDraftMontagemCommandHandler),
        typeof(DefinirCapitaesDraftMontagemCommandHandler),
        typeof(SortearCapitaesDraftMontagemCommandHandler),
        typeof(DefinirOrdemEscolhaDraftMontagemCommandHandler),
        typeof(IniciarDraftMontagemTempoRealCommandHandler),
        typeof(RegistrarPickDraftMontagemCommandHandler),
        typeof(AvancarTurnoDraftMontagemTimeoutCommandHandler),
        typeof(SubstituirReservaDraftMontagemCommandHandler),
        typeof(SalvarLayoutDraftMontagemCommandHandler),
        typeof(FinalizarDraftMontagemCommandHandler),
        typeof(CancelarDraftMontagemCommandHandler),
        typeof(ArquivarDraftMontagemCommandHandler),
        typeof(RestaurarDraftMontagemCommandHandler),
    };

    [Theory]
    [MemberData(nameof(HandlersNaoPublicacao))]
    public void HandlerDeMutacaoVisivel_DeveConsumirPublisherCompartilhadoSemNotifierDireto(Type handlerType)
    {
        var dependencies = handlerType.GetConstructors().Single().GetParameters().Select(parameter => parameter.ParameterType);

        dependencies.Should().Contain(typeof(IDraftMontagemRealtimePublisher));
        dependencies.Should().NotContain(typeof(IDraftMontagemRealtimeNotifier));
    }

    [Fact]
    public async Task EncerrarPresenca_DeveSalvarAntesDePublicarSemRepassarTokenDaRequest()
    {
        var draft = DraftMontagem.CriarPorPresenca("Rinha", null, 2);
        for (var index = 0; index < 4; index++)
        {
            draft.ConfirmarPresenca(Guid.NewGuid(), Guid.NewGuid(), null, DraftMontagemPresencaOrigem.Web);
        }

        var sequence = new List<string>();
        var repository = RepositoryFor(draft);
        repository.Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => sequence.Add("save"))
            .Returns(Task.CompletedTask);
        var publisher = new Mock<IDraftMontagemRealtimePublisher>();
        publisher.Setup(item => item.PublishAfterCommitAsync(draft.Id, It.IsAny<DraftMontagemAvailabilityChange>()))
            .Callback(() => sequence.Add("publish"))
            .Returns(Task.CompletedTask);
        using var requestCancellation = new CancellationTokenSource();
        var handler = new EncerrarPresencaDraftMontagemCommandHandler(
            repository.Object,
            new EncerrarPresencaDraftMontagemValidator(),
            Mock.Of<IDraftMontagemMetrics>(),
            publisher.Object);

        await handler.Handle(
            new EncerrarPresencaDraftMontagemCommand(draft.Id, new EncerrarPresencaDraftMontagemRequestDto(true, 2)),
            requestCancellation.Token);

        sequence.Should().Equal("save", "publish");
        publisher.Verify(item => item.PublishAfterCommitAsync(draft.Id, DraftMontagemAvailabilityChange.None), Times.Once);
    }

    [Fact]
    public async Task SelecionarMesmoModo_DeveSerNoOpSemSalvarOuPublicar()
    {
        var players = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToList();
        var draft = DraftMontagem.CriarPorPresenca("Rinha", null, 2);
        foreach (var playerId in players)
        {
            draft.ConfirmarPresenca(Guid.NewGuid(), playerId, null, DraftMontagemPresencaOrigem.Web);
        }
        draft.EncerrarPresenca(true, 2);
        draft.SelecionarModo(DraftMontagemModo.Manual, players.ToHashSet());
        var repository = RepositoryFor(draft);
        var publisher = new Mock<IDraftMontagemRealtimePublisher>();
        var handler = new SelecionarModoDraftMontagemCommandHandler(
            repository.Object,
            new SelecionarModoDraftMontagemValidator(),
            publisher.Object);

        await handler.Handle(
            new SelecionarModoDraftMontagemCommand(draft.Id, new SelecionarModoDraftMontagemRequestDto("Manual")),
            CancellationToken.None);

        repository.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        publisher.Verify(item => item.PublishAfterCommitAsync(It.IsAny<Guid>(), It.IsAny<DraftMontagemAvailabilityChange>()), Times.Never);
    }

    [Fact]
    public async Task FinalizarComConflito_DevePublicarZeroVezes()
    {
        var players = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToList();
        var draft = new DraftMontagem("Rinha", null, 2, DraftMontagemCriterioCapitaes.Manual, players, players.Take(2).ToList());
        draft.SalvarLayout(
            draft.Times.Select((team, index) => new DraftMontagemLayoutTime(
                team.Id,
                team.Nome,
                team.CapitaoId,
                new[] { players[index], players[index + 2] }.Select((playerId, order) =>
                    new DraftMontagemLayoutParticipante(playerId, order + 1, null)).ToList())).ToList(),
            [],
            []);
        var repository = RepositoryFor(draft);
        repository.Setup(item => item.TrySaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(DraftMontagemSaveResultado.ConflitoDeVersao);
        var publisher = new Mock<IDraftMontagemRealtimePublisher>();
        var handler = new FinalizarDraftMontagemCommandHandler(repository.Object, publisher.Object);

        var act = () => handler.Handle(new FinalizarDraftMontagemCommand(draft.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage(MessageCodes.DraftStateConflict);
        publisher.Verify(item => item.PublishAfterCommitAsync(It.IsAny<Guid>(), It.IsAny<DraftMontagemAvailabilityChange>()), Times.Never);
    }

    [Fact]
    public async Task ArquivarJaArquivado_DeveSerNoOpSemSalvarOuPublicar()
    {
        var userId = Guid.NewGuid();
        var draft = new DraftMontagem("Rinha", null, 2, DraftMontagemCriterioCapitaes.Manual, [], []);
        draft.Arquivar("motivo", userId, DateTimeOffset.UtcNow);
        var repository = RepositoryFor(draft, includingArchived: true);
        var publisher = new Mock<IDraftMontagemRealtimePublisher>();
        var handler = new ArquivarDraftMontagemCommandHandler(
            repository.Object,
            new ArquivarDraftMontagemValidator(),
            new CurrentUser(userId),
            publisher.Object);

        await handler.Handle(
            new ArquivarDraftMontagemCommand(draft.Id, new ArquivarDraftMontagemRequestDto("motivo", draft.VersaoEstado)),
            CancellationToken.None);

        repository.Verify(item => item.TrySaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        publisher.Verify(item => item.PublishAfterCommitAsync(It.IsAny<Guid>(), It.IsAny<DraftMontagemAvailabilityChange>()), Times.Never);
    }

    [Fact]
    public async Task SalvarLayoutComVersaoBaseDefasada_DeveConflitarAntesDeMutarSalvarOuPublicar()
    {
        var players = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToList();
        var draft = new DraftMontagem("Rinha", null, 2, DraftMontagemCriterioCapitaes.Manual, players, []);
        var initialVersion = draft.VersaoEstado;
        var initialTeams = draft.Times.Select(team => (team.Id, team.Nome)).ToList();
        var repository = RepositoryFor(draft);
        var publisher = new Mock<IDraftMontagemRealtimePublisher>();
        var request = new SalvarLayoutDraftMontagemRequestDto(
            initialTeams.Select((team, index) => new DraftMontagemLayoutTimeDto(
                team.Id,
                $"Alterado {index}",
                null,
                players.Skip(index * 2).Take(2).Select((playerId, order) =>
                    new DraftMontagemLayoutParticipanteDto(playerId, order + 1, null)).ToList())).ToList(),
            [],
            [],
            initialVersion + 1);
        var handler = new SalvarLayoutDraftMontagemCommandHandler(repository.Object, new SalvarLayoutDraftMontagemValidator(), publisher.Object);

        var act = () => handler.Handle(new SalvarLayoutDraftMontagemCommand(draft.Id, request), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage(MessageCodes.DraftStateConflict);
        draft.VersaoEstado.Should().Be(initialVersion);
        draft.Times.Select(team => (team.Id, team.Nome)).Should().Equal(initialTeams);
        repository.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        publisher.Verify(item => item.PublishAfterCommitAsync(It.IsAny<Guid>(), It.IsAny<DraftMontagemAvailabilityChange>()), Times.Never);
    }

    [Fact]
    public void ValidatorDeLayout_DeveExigirVersaoBase()
    {
        var request = new SalvarLayoutDraftMontagemRequestDto([], [], [], null);

        var result = new SalvarLayoutDraftMontagemValidator().Validate(request);

        result.Errors.Should().Contain(error =>
            error.PropertyName == nameof(SalvarLayoutDraftMontagemRequestDto.VersaoEstado)
            && error.ErrorMessage == MessageCodes.FieldRequired);
    }

    private static Mock<IDraftMontagemRepository> RepositoryFor(DraftMontagem draft, bool includingArchived = false)
    {
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.GetByIdAsync(draft.Id, It.IsAny<CancellationToken>())).ReturnsAsync(draft);
        repository.Setup(item => item.ReloadByIdAsync(draft.Id, It.IsAny<CancellationToken>())).ReturnsAsync(draft);
        if (includingArchived)
        {
            repository.Setup(item => item.GetByIdIncludingArchivedAsync(draft.Id, It.IsAny<CancellationToken>())).ReturnsAsync(draft);
            repository.Setup(item => item.ReloadByIdIncludingArchivedAsync(draft.Id, It.IsAny<CancellationToken>())).ReturnsAsync(draft);
        }
        return repository;
    }

    private sealed record CurrentUser(Guid? UserId) : ICurrentUser
    {
        public IReadOnlyCollection<string> Roles => [];
        public string? IpAddress => null;
        public string? UserAgent => null;
    }
}
