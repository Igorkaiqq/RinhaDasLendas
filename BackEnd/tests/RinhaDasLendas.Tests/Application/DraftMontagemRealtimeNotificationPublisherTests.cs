using FluentAssertions;
using Moq;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Handlers.DraftMontagens;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Tests.Application;

public sealed class DraftMontagemRealtimeNotificationPublisherTests
{
    [Fact]
    public async Task DeveTentarTodosERepetirSomenteIdQueFalhou()
    {
        var primeiroId = Guid.NewGuid();
        var segundoId = Guid.NewGuid();
        var repository = CreateRepository(primeiroId, segundoId);
        var notifier = new Mock<IDraftMontagemRealtimeNotifier>();
        var ordem = new List<Guid>();
        var tentativasPrimeiro = 0;
        notifier.Setup(item => item.StateUpdatedAsync(
                primeiroId,
                It.IsAny<DraftMontagemRealtimeStateDto>(),
                It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                ordem.Add(primeiroId);
                return tentativasPrimeiro++ == 0
                    ? Task.FromException(new InvalidOperationException("falha transitoria"))
                    : Task.CompletedTask;
            });
        notifier.Setup(item => item.StateUpdatedAsync(
                segundoId,
                It.IsAny<DraftMontagemRealtimeStateDto>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => ordem.Add(segundoId))
            .Returns(Task.CompletedTask);

        await DraftMontagemRealtimeNotificationPublisher.PublishReloadedAsync(
            [primeiroId, segundoId],
            repository.Object,
            notifier.Object,
            CancellationToken.None);

        notifier.Verify(item => item.StateUpdatedAsync(primeiroId, It.IsAny<DraftMontagemRealtimeStateDto>(), CancellationToken.None), Times.Exactly(2));
        notifier.Verify(item => item.StateUpdatedAsync(segundoId, It.IsAny<DraftMontagemRealtimeStateDto>(), CancellationToken.None), Times.Once);
        ordem.Should().Equal(primeiroId, segundoId, primeiroId);
    }

    [Fact]
    public async Task FalhaPersistenteDeveProcessarTodosEAgruparErroAposRetry()
    {
        var primeiroId = Guid.NewGuid();
        var segundoId = Guid.NewGuid();
        var repository = CreateRepository(primeiroId, segundoId);
        var notifier = new Mock<IDraftMontagemRealtimeNotifier>();
        notifier.Setup(item => item.StateUpdatedAsync(
                primeiroId,
                It.IsAny<DraftMontagemRealtimeStateDto>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("falha persistente"));
        notifier.Setup(item => item.StateUpdatedAsync(
                segundoId,
                It.IsAny<DraftMontagemRealtimeStateDto>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var act = () => DraftMontagemRealtimeNotificationPublisher.PublishReloadedAsync(
            [primeiroId, segundoId],
            repository.Object,
            notifier.Object,
            CancellationToken.None);

        var exception = await act.Should().ThrowAsync<AggregateException>();
        exception.Which.InnerExceptions.Should().ContainSingle()
            .Which.Message.Should().Be("falha persistente");
        notifier.Verify(item => item.StateUpdatedAsync(primeiroId, It.IsAny<DraftMontagemRealtimeStateDto>(), CancellationToken.None), Times.Exactly(2));
        notifier.Verify(item => item.StateUpdatedAsync(segundoId, It.IsAny<DraftMontagemRealtimeStateDto>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task SemIdsNaoDeveRecarregarNemNotificar()
    {
        var repository = new Mock<IDraftMontagemRepository>();
        var notifier = new Mock<IDraftMontagemRealtimeNotifier>();

        await DraftMontagemRealtimeNotificationPublisher.PublishReloadedAsync(
            [],
            repository.Object,
            notifier.Object,
            CancellationToken.None);

        repository.Verify(item => item.ReloadByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        notifier.Verify(item => item.StateUpdatedAsync(It.IsAny<Guid>(), It.IsAny<DraftMontagemRealtimeStateDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Mock<IDraftMontagemRepository> CreateRepository(params Guid[] ids)
    {
        var repository = new Mock<IDraftMontagemRepository>();
        foreach (var id in ids)
        {
            repository.Setup(item => item.ReloadByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DraftMontagem("Rinha", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []));
        }

        return repository;
    }
}
