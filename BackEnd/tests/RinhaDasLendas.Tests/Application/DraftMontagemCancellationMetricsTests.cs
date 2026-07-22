using System.Diagnostics.Metrics;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RinhaDasLendas.Api.Observability;
using RinhaDasLendas.Application.Commands.DraftMontagens;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Handlers.DraftMontagens;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Validators;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Tests.Application;

public sealed class DraftMontagemCancellationMetricsTests
{
    [Fact]
    public async Task CancelamentoDeveRegistrarMetricaAposPersistenciaEAntesDaNotificacao()
    {
        var id = Guid.NewGuid();
        var persisted = false;
        var metricRecorded = false;
        var montagem = CreateDraft();
        var repository = CreateRepository(id, montagem);
        repository.Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => persisted = true)
            .Returns(Task.CompletedTask);
        var metrics = new Mock<IDraftMontagemMetrics>();
        metrics.Setup(item => item.RecordDraftCancelled(id)).Callback(() =>
        {
            persisted.Should().BeTrue();
            metricRecorded = true;
        });
        var notifier = new Mock<IDraftMontagemRealtimeNotifier>();
        notifier.Setup(item => item.StateUpdatedAsync(id, It.IsAny<DraftMontagemRealtimeStateDto>(), It.IsAny<CancellationToken>()))
            .Callback(() => metricRecorded.Should().BeTrue())
            .Returns(Task.CompletedTask);
        var handler = CreateHandler(repository.Object, notifier.Object, metrics.Object);

        await handler.Handle(
            new CancelarDraftMontagemCommand(id, new CancelarDraftMontagemRequestDto("motivo administrativo")),
            CancellationToken.None);

        metrics.Verify(item => item.RecordDraftCancelled(id), Times.Once);
        notifier.Verify(item => item.StateUpdatedAsync(id, It.IsAny<DraftMontagemRealtimeStateDto>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task FalhaNaPersistenciaNaoDeveRegistrarMetricaNemNotificar()
    {
        var id = Guid.NewGuid();
        var repository = CreateRepository(id, CreateDraft());
        repository.Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("persistence failure"));
        var metrics = new Mock<IDraftMontagemMetrics>();
        var notifier = new Mock<IDraftMontagemRealtimeNotifier>();
        var handler = CreateHandler(repository.Object, notifier.Object, metrics.Object);

        var act = () => handler.Handle(
            new CancelarDraftMontagemCommand(id, new CancelarDraftMontagemRequestDto("motivo administrativo")),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        metrics.Verify(item => item.RecordDraftCancelled(It.IsAny<Guid>()), Times.Never);
        notifier.Verify(item => item.StateUpdatedAsync(
            It.IsAny<Guid>(),
            It.IsAny<DraftMontagemRealtimeStateDto>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CancelamentoRepetidoNaoDevePersistirNemDuplicarMetrica()
    {
        var id = Guid.NewGuid();
        var montagem = CreateDraft();
        montagem.Cancelar("cancelamento anterior", Guid.NewGuid());
        var repository = CreateRepository(id, montagem);
        var metrics = new Mock<IDraftMontagemMetrics>();
        var handler = CreateHandler(repository.Object, Mock.Of<IDraftMontagemRealtimeNotifier>(), metrics.Object);

        var act = () => handler.Handle(
            new CancelarDraftMontagemCommand(id, new CancelarDraftMontagemRequestDto("novo motivo")),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
        repository.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        metrics.Verify(item => item.RecordDraftCancelled(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public void AdapterDeveRegistrarAcaoDeCancelamentoSomenteComIdentificadorDoDraft()
    {
        var id = Guid.NewGuid();
        var measurements = new List<(long Value, Dictionary<string, object?> Tags)>();
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Name == "rinha_draft_actions_total")
                {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            }
        };
        listener.SetMeasurementEventCallback<long>((_, value, tags, _) =>
        {
            var capturedTags = new Dictionary<string, object?>();
            foreach (var tag in tags)
            {
                capturedTags[tag.Key] = tag.Value;
            }

            if (capturedTags.TryGetValue("draft_id", out var draftId) && draftId?.ToString() == id.ToString())
            {
                measurements.Add((value, capturedTags));
            }
        });
        listener.Start();
        using var serviceProvider = new ServiceCollection().AddMetrics().BuildServiceProvider();
        var metrics = new DraftMontagemMetrics(new ApiMetrics(serviceProvider.GetRequiredService<IMeterFactory>()));

        metrics.RecordDraftCancelled(id);

        measurements.Should().ContainSingle();
        measurements[0].Value.Should().Be(1);
        measurements[0].Tags.Should().BeEquivalentTo(new Dictionary<string, object?>
        {
            ["draft_id"] = id.ToString(),
            ["action"] = "draft_cancelled"
        });
    }

    private static CancelarDraftMontagemCommandHandler CreateHandler(
        IDraftMontagemRepository repository,
        IDraftMontagemRealtimeNotifier notifier,
        IDraftMontagemMetrics metrics) =>
        new(repository, new CancelarDraftMontagemValidator(), new TestCurrentUser(Guid.NewGuid()), notifier, metrics);

    private static Mock<IDraftMontagemRepository> CreateRepository(Guid id, DraftMontagem montagem)
    {
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(montagem);
        repository.Setup(item => item.GetJogadorByUsuarioIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Jogador?)null);
        return repository;
    }

    private static DraftMontagem CreateDraft() =>
        new("Rinha", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);

    private sealed record TestCurrentUser(Guid? UserId) : ICurrentUser
    {
        public IReadOnlyCollection<string> Roles => [];
        public string? IpAddress => null;
        public string? UserAgent => null;
    }
}
