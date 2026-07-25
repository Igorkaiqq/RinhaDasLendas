using FluentAssertions;
using FluentValidation;
using Moq;
using RinhaDasLendas.Application.Commands.DraftMontagens;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Handlers.DraftMontagens;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Validators;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Repositories;
using RinhaDasLendas.Tests.Jogadores;

namespace RinhaDasLendas.Tests.Handlers.DraftMontagens;

public sealed class PresencaConcurrencyHandlerTests
{
    [Theory]
    [InlineData(DraftMontagemSaveResultado.ConflitoDeVersao)]
    [InlineData(DraftMontagemSaveResultado.ConflitoDePresencaConfirmada)]
    public async Task Confirmar_conflito_com_estado_desejado_deve_retornar_sucesso_sem_efeitos(DraftMontagemSaveResultado saveResult)
    {
        var userId = Guid.NewGuid();
        var jogador = JogadorTestData.JogadorAtivo();
        jogador.VincularUsuario(userId);
        var initial = NewDraft();
        var reloaded = NewDraft();
        reloaded.ConfirmarPresenca(userId, jogador.Id, null, DraftMontagemPresencaOrigem.Web);
        var (repository, notifier, metrics) = Setup(initial, reloaded, jogador, saveResult);
        var handler = new ConfirmarPresencaDraftMontagemCommandHandler(
            repository.Object,
            new TestCurrentUser(userId),
            Mock.Of<IDiscordIdentityLookupService>(),
            new ConfirmarPresencaDraftMontagemValidator(),
            notifier.Object,
            metrics.Object);

        var result = await handler.Handle(
            new ConfirmarPresencaDraftMontagemCommand(initial.Id, new ConfirmarPresencaDraftMontagemRequestDto(userId, null, "Web")),
            CancellationToken.None);

        result.Should().NotBeNull();
        repository.Verify(instance => instance.TrySaveChangesAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
        repository.Verify(instance => instance.ReloadByIdAsync(initial.Id, It.IsAny<CancellationToken>()), Moq.Times.Once);
        notifier.VerifyNoOtherCalls();
        metrics.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(DraftMontagemSaveResultado.ConflitoDeVersao)]
    [InlineData(DraftMontagemSaveResultado.ConflitoDePresencaConfirmada)]
    public async Task Confirmar_conflito_com_estado_divergente_deve_retornar_MV088(DraftMontagemSaveResultado saveResult)
    {
        var userId = Guid.NewGuid();
        var jogador = JogadorTestData.JogadorAtivo();
        jogador.VincularUsuario(userId);
        var initial = NewDraft();
        var (repository, notifier, metrics) = Setup(initial, NewDraft(), jogador, saveResult);
        var handler = new ConfirmarPresencaDraftMontagemCommandHandler(
            repository.Object,
            new TestCurrentUser(userId),
            Mock.Of<IDiscordIdentityLookupService>(),
            new ConfirmarPresencaDraftMontagemValidator(),
            notifier.Object,
            metrics.Object);

        var act = () => handler.Handle(
            new ConfirmarPresencaDraftMontagemCommand(initial.Id, new ConfirmarPresencaDraftMontagemRequestDto(userId, null, "Web")),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().Where(exception => exception.Message == MessageCodes.PresencePersistenceConflict);
        repository.Verify(instance => instance.TrySaveChangesAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
        repository.Verify(instance => instance.ReloadByIdAsync(initial.Id, It.IsAny<CancellationToken>()), Moq.Times.Once);
        notifier.VerifyNoOtherCalls();
        metrics.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(DraftMontagemSaveResultado.ConflitoDeVersao)]
    [InlineData(DraftMontagemSaveResultado.ConflitoDePresencaConfirmada)]
    public async Task Cancelar_conflito_com_estado_desejado_deve_retornar_sucesso_sem_efeitos(DraftMontagemSaveResultado saveResult)
    {
        var userId = Guid.NewGuid();
        var jogador = JogadorTestData.JogadorAtivo();
        jogador.VincularUsuario(userId);
        var initial = ConfirmedDraft(userId, jogador.Id);
        var reloaded = ConfirmedDraft(userId, jogador.Id);
        reloaded.CancelarPresenca(userId);
        var (repository, notifier, metrics) = Setup(initial, reloaded, jogador, saveResult);
        var handler = new CancelarPresencaDraftMontagemCommandHandler(
            repository.Object,
            new TestCurrentUser(userId),
            Mock.Of<IDiscordIdentityLookupService>(),
            notifier.Object,
            metrics.Object);

        var result = await handler.Handle(
            new CancelarPresencaDraftMontagemCommand(initial.Id, new CancelarPresencaDraftMontagemRequestDto(userId, null)),
            CancellationToken.None);

        result.Should().NotBeNull();
        repository.Verify(instance => instance.TrySaveChangesAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
        repository.Verify(instance => instance.ReloadByIdAsync(initial.Id, It.IsAny<CancellationToken>()), Moq.Times.Once);
        notifier.VerifyNoOtherCalls();
        metrics.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(DraftMontagemSaveResultado.ConflitoDeVersao)]
    [InlineData(DraftMontagemSaveResultado.ConflitoDePresencaConfirmada)]
    public async Task Cancelar_conflito_com_estado_divergente_deve_retornar_MV088(DraftMontagemSaveResultado saveResult)
    {
        var userId = Guid.NewGuid();
        var jogador = JogadorTestData.JogadorAtivo();
        jogador.VincularUsuario(userId);
        var initial = ConfirmedDraft(userId, jogador.Id);
        var reloaded = ConfirmedDraft(userId, jogador.Id);
        var (repository, notifier, metrics) = Setup(initial, reloaded, jogador, saveResult);
        var handler = new CancelarPresencaDraftMontagemCommandHandler(
            repository.Object,
            new TestCurrentUser(userId),
            Mock.Of<IDiscordIdentityLookupService>(),
            notifier.Object,
            metrics.Object);

        var act = () => handler.Handle(
            new CancelarPresencaDraftMontagemCommand(initial.Id, new CancelarPresencaDraftMontagemRequestDto(userId, null)),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().Where(exception => exception.Message == MessageCodes.PresencePersistenceConflict);
        repository.Verify(instance => instance.TrySaveChangesAsync(It.IsAny<CancellationToken>()), Moq.Times.Once);
        repository.Verify(instance => instance.ReloadByIdAsync(initial.Id, It.IsAny<CancellationToken>()), Moq.Times.Once);
        notifier.VerifyNoOtherCalls();
        metrics.VerifyNoOtherCalls();
    }

    private static (Mock<IDraftMontagemRepository> Repository, Mock<IDraftMontagemRealtimeNotifier> Notifier, Mock<IDraftMontagemMetrics> Metrics) Setup(
        DraftMontagem initial,
        DraftMontagem reloaded,
        Jogador jogador,
        DraftMontagemSaveResultado saveResult)
    {
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(instance => instance.GetByIdAsync(initial.Id, It.IsAny<CancellationToken>())).ReturnsAsync(initial);
        repository.Setup(instance => instance.GetJogadorByUsuarioIdAsync(jogador.UsuarioId!.Value, It.IsAny<CancellationToken>())).ReturnsAsync(jogador);
        repository.Setup(instance => instance.TrySaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(saveResult);
        repository.Setup(instance => instance.ReloadByIdAsync(initial.Id, It.IsAny<CancellationToken>())).ReturnsAsync(reloaded);
        return (repository, new Mock<IDraftMontagemRealtimeNotifier>(MockBehavior.Strict), new Mock<IDraftMontagemMetrics>(MockBehavior.Strict));
    }

    private static DraftMontagem NewDraft() => new("Draft", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);

    private static DraftMontagem ConfirmedDraft(Guid userId, Guid jogadorId)
    {
        var draft = NewDraft();
        draft.ConfirmarPresenca(userId, jogadorId, null, DraftMontagemPresencaOrigem.Web);
        return draft;
    }

    private sealed record TestCurrentUser(Guid? UserId) : ICurrentUser
    {
        public IReadOnlyCollection<string> Roles => [AuthRoles.Jogador];
        public string? IpAddress => null;
        public string? UserAgent => null;
    }
}
