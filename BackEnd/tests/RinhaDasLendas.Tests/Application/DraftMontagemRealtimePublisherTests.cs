using System.Diagnostics;
using System.Reflection;
using FluentAssertions;
using Moq;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Enums;
using RinhaDasLendas.Application.Handlers.DraftMontagens;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Services;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Tests.Application;

public sealed class DraftMontagemRealtimePublisherTests
{
    [Fact]
    public void Telemetria_NaoDeveReceberExceptionCompleta()
    {
        var parameters = typeof(IDraftMontagemRealtimeTelemetry)
            .GetMethod(nameof(IDraftMontagemRealtimeTelemetry.RecordFailure))!
            .GetParameters();

        parameters.Should().NotContain(parameter => typeof(Exception).IsAssignableFrom(parameter.ParameterType));
        parameters.Should().ContainSingle(parameter => parameter.Name == "failureType" && parameter.ParameterType == typeof(string));
    }

    [Fact]
    public async Task PublicacaoNormal_DeveRecarregarEEnviarSnapshotCompartilhado()
    {
        var draft = CreateDraft();
        var repository = new Mock<IDraftMontagemRepository>();
        repository
            .Setup(item => item.ReloadByIdAsync(draft.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(draft);
        var notifier = new Mock<IDraftMontagemRealtimeNotifier>();
        DraftMontagemRealtimeSnapshotDto? published = null;
        notifier
            .Setup(item => item.SharedStateUpdatedAsync(draft.Id, It.IsAny<DraftMontagemRealtimeSnapshotDto>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, DraftMontagemRealtimeSnapshotDto, CancellationToken>((_, snapshot, _) => published = snapshot)
            .Returns(Task.CompletedTask);
        var telemetry = new TestTelemetry();
        var publisher = new DraftMontagemRealtimePublisher(repository.Object, notifier.Object, telemetry);

        await publisher.PublishAfterCommitAsync(draft.Id);

        published.Should().NotBeNull();
        published!.Montagem.Id.Should().Be(draft.Id);
        published.GetType().GetProperty("CanCurrentUserPick").Should().BeNull();
        telemetry.Failures.Should().BeEmpty();
        repository.Verify(item => item.ReloadByIdIncludingArchivedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        notifier.Verify(item => item.ArchivedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        notifier.Verify(item => item.RestoredAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PublicacaoPosCommit_NaoDeveDependerDoTokenCanceladoDaRequest()
    {
        using var requestCancellation = new CancellationTokenSource();
        await requestCancellation.CancelAsync();
        var draft = CreateDraft();
        var repository = new Mock<IDraftMontagemRepository>();
        repository
            .Setup(item => item.ReloadByIdAsync(draft.Id, It.Is<CancellationToken>(token => token != requestCancellation.Token && !token.IsCancellationRequested)))
            .ReturnsAsync(draft);
        var notifier = new Mock<IDraftMontagemRealtimeNotifier>();
        notifier
            .Setup(item => item.SharedStateUpdatedAsync(
                draft.Id,
                It.IsAny<DraftMontagemRealtimeSnapshotDto>(),
                It.Is<CancellationToken>(token => token != requestCancellation.Token && !token.IsCancellationRequested)))
            .Returns(Task.CompletedTask);
        var publisher = new DraftMontagemRealtimePublisher(repository.Object, notifier.Object, new TestTelemetry());

        await publisher.PublishAfterCommitAsync(draft.Id);

        notifier.Verify(item => item.SharedStateUpdatedAsync(
            draft.Id,
            It.IsAny<DraftMontagemRealtimeSnapshotDto>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TimeoutInterno_DeveCancelarEmCincoSegundosEAbsorverFalha()
    {
        var draft = CreateDraft();
        var repository = new Mock<IDraftMontagemRepository>();
        repository
            .Setup(item => item.ReloadByIdAsync(draft.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(draft);
        var notifier = new Mock<IDraftMontagemRealtimeNotifier>();
        notifier
            .Setup(item => item.SharedStateUpdatedAsync(draft.Id, It.IsAny<DraftMontagemRealtimeSnapshotDto>(), It.IsAny<CancellationToken>()))
            .Returns<Guid, DraftMontagemRealtimeSnapshotDto, CancellationToken>((_, _, token) => Task.Delay(Timeout.InfiniteTimeSpan, token));
        var telemetry = new TestTelemetry();
        var publisher = new DraftMontagemRealtimePublisher(repository.Object, notifier.Object, telemetry);
        var stopwatch = Stopwatch.StartNew();

        await publisher.PublishAfterCommitAsync(draft.Id);

        stopwatch.Elapsed.Should().BeGreaterThanOrEqualTo(TimeSpan.FromSeconds(4.5));
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(7));
        telemetry.Failures.Should().ContainSingle(failure =>
            failure.Operation == nameof(IDraftMontagemRealtimeNotifier.SharedStateUpdatedAsync)
            && failure.FailureType == nameof(OperationCanceledException));
    }

    [Theory]
    [InlineData(DraftMontagemAvailabilityChange.Archived)]
    [InlineData(DraftMontagemAvailabilityChange.Restored)]
    public async Task MudancaDeDisponibilidade_DeveRecarregarCorretamenteEEnviarEvento(
        DraftMontagemAvailabilityChange availability)
    {
        var draft = CreateDraft();
        var repository = new Mock<IDraftMontagemRepository>();
        repository
            .Setup(item => item.ReloadByIdAsync(draft.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(draft);
        repository
            .Setup(item => item.ReloadByIdIncludingArchivedAsync(draft.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(draft);
        var notifier = new Mock<IDraftMontagemRealtimeNotifier>();
        notifier
            .Setup(item => item.SharedStateUpdatedAsync(draft.Id, It.IsAny<DraftMontagemRealtimeSnapshotDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notifier.Setup(item => item.ArchivedAsync(draft.Id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        notifier.Setup(item => item.RestoredAsync(draft.Id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var publisher = new DraftMontagemRealtimePublisher(repository.Object, notifier.Object, new TestTelemetry());

        await publisher.PublishAfterCommitAsync(draft.Id, availability);

        repository.Verify(
            item => item.ReloadByIdIncludingArchivedAsync(draft.Id, It.IsAny<CancellationToken>()),
            availability == DraftMontagemAvailabilityChange.Archived ? Times.Once : Times.Never);
        repository.Verify(
            item => item.ReloadByIdAsync(draft.Id, It.IsAny<CancellationToken>()),
            availability == DraftMontagemAvailabilityChange.Restored ? Times.Once : Times.Never);
        notifier.Verify(
            item => item.ArchivedAsync(draft.Id, It.IsAny<CancellationToken>()),
            availability == DraftMontagemAvailabilityChange.Archived ? Times.Once : Times.Never);
        notifier.Verify(
            item => item.RestoredAsync(draft.Id, It.IsAny<CancellationToken>()),
            availability == DraftMontagemAvailabilityChange.Restored ? Times.Once : Times.Never);
    }

    [Fact]
    public async Task FalhaDeEnvio_DeveSerAbsorvidaEObservadaSemImpedirEventoDeDisponibilidade()
    {
        var draft = CreateDraft();
        var repository = new Mock<IDraftMontagemRepository>();
        repository
            .Setup(item => item.ReloadByIdIncludingArchivedAsync(draft.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(draft);
        var notifier = new Mock<IDraftMontagemRealtimeNotifier>();
        notifier
            .Setup(item => item.SharedStateUpdatedAsync(draft.Id, It.IsAny<DraftMontagemRealtimeSnapshotDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("transport failure"));
        notifier
            .Setup(item => item.ArchivedAsync(draft.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var telemetry = new TestTelemetry();
        var publisher = new DraftMontagemRealtimePublisher(repository.Object, notifier.Object, telemetry);

        var act = () => publisher.PublishAfterCommitAsync(draft.Id, DraftMontagemAvailabilityChange.Archived);

        await act.Should().NotThrowAsync();
        notifier.Verify(item => item.ArchivedAsync(draft.Id, It.IsAny<CancellationToken>()), Times.Once);
        telemetry.Failures.Should().ContainSingle(failure =>
            failure.Operation == nameof(IDraftMontagemRealtimeNotifier.SharedStateUpdatedAsync)
            && failure.FailureType == nameof(InvalidOperationException));
    }

    [Fact]
    public async Task FalhaNoReload_DeveSerAbsorvidaEImpedirQualquerEnvio()
    {
        var draftId = Guid.NewGuid();
        var repository = new Mock<IDraftMontagemRepository>();
        repository
            .Setup(item => item.ReloadByIdAsync(draftId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("reload failure"));
        var notifier = new Mock<IDraftMontagemRealtimeNotifier>(MockBehavior.Strict);
        var telemetry = new TestTelemetry();
        var publisher = new DraftMontagemRealtimePublisher(repository.Object, notifier.Object, telemetry);

        var act = () => publisher.PublishAfterCommitAsync(draftId);

        await act.Should().NotThrowAsync();
        telemetry.Failures.Should().ContainSingle(failure =>
            failure.Operation == nameof(IDraftMontagemRepository.ReloadByIdAsync)
            && failure.FailureType == nameof(InvalidOperationException));
        VerifyNoSend(notifier);
    }

    [Fact]
    public async Task TimeoutNoReload_DeveCancelarEmCincoSegundosSemEnviar()
    {
        var draftId = Guid.NewGuid();
        var repository = new Mock<IDraftMontagemRepository>();
        repository
            .Setup(item => item.ReloadByIdAsync(draftId, It.IsAny<CancellationToken>()))
            .Returns<Guid, CancellationToken>(async (_, token) =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                return null;
            });
        var notifier = new Mock<IDraftMontagemRealtimeNotifier>(MockBehavior.Strict);
        var telemetry = new TestTelemetry();
        var publisher = new DraftMontagemRealtimePublisher(repository.Object, notifier.Object, telemetry);
        var stopwatch = Stopwatch.StartNew();

        var act = () => publisher.PublishAfterCommitAsync(draftId);

        await act.Should().NotThrowAsync();
        stopwatch.Elapsed.Should().BeGreaterThanOrEqualTo(TimeSpan.FromSeconds(4.5));
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(7));
        telemetry.Failures.Should().ContainSingle(failure =>
            failure.Operation == nameof(IDraftMontagemRepository.ReloadByIdAsync)
            && failure.FailureType == nameof(OperationCanceledException));
        VerifyNoSend(notifier);
    }

    [Fact]
    public async Task FalhaNoMapper_DeveSerAbsorvidaEImpedirQualquerEnvio()
    {
        var draft = CreateDraft();
        var participantes = (List<DraftMontagemParticipante>)typeof(DraftMontagem)
            .GetField("_participantes", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(draft)!;
        participantes.Add(null!);
        var repository = new Mock<IDraftMontagemRepository>();
        repository
            .Setup(item => item.ReloadByIdAsync(draft.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(draft);
        var notifier = new Mock<IDraftMontagemRealtimeNotifier>(MockBehavior.Strict);
        var telemetry = new TestTelemetry();
        var publisher = new DraftMontagemRealtimePublisher(repository.Object, notifier.Object, telemetry);

        var act = () => publisher.PublishAfterCommitAsync(draft.Id);

        await act.Should().NotThrowAsync();
        telemetry.Failures.Should().ContainSingle(failure =>
            failure.Operation == nameof(DraftMontagemRealtimeStateFactory.CreateShared)
            && failure.FailureType == nameof(NullReferenceException));
        VerifyNoSend(notifier);
    }

    [Fact]
    public async Task FalhaNaTelemetria_NaoDeveEscaparNemImpedirProximoEnvioValido()
    {
        var draft = CreateDraft();
        var repository = new Mock<IDraftMontagemRepository>();
        repository
            .Setup(item => item.ReloadByIdIncludingArchivedAsync(draft.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(draft);
        var notifier = new Mock<IDraftMontagemRealtimeNotifier>();
        notifier
            .Setup(item => item.SharedStateUpdatedAsync(draft.Id, It.IsAny<DraftMontagemRealtimeSnapshotDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("transport failure"));
        notifier
            .Setup(item => item.ArchivedAsync(draft.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var telemetry = new Mock<IDraftMontagemRealtimeTelemetry>();
        telemetry
            .Setup(item => item.RecordFailure(
                It.IsAny<Guid>(),
                It.IsAny<long?>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<string>()))
            .Throws(new InvalidOperationException("telemetry failure"));
        var publisher = new DraftMontagemRealtimePublisher(repository.Object, notifier.Object, telemetry.Object);

        var act = () => publisher.PublishAfterCommitAsync(draft.Id, DraftMontagemAvailabilityChange.Archived);

        await act.Should().NotThrowAsync();
        notifier.Verify(item => item.ArchivedAsync(draft.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static DraftMontagem CreateDraft()
        => new("Rinha", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);

    private static void VerifyNoSend(Mock<IDraftMontagemRealtimeNotifier> notifier)
    {
        notifier.Verify(item => item.SharedStateUpdatedAsync(
            It.IsAny<Guid>(),
            It.IsAny<DraftMontagemRealtimeSnapshotDto>(),
            It.IsAny<CancellationToken>()), Times.Never);
        notifier.Verify(item => item.ArchivedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        notifier.Verify(item => item.RestoredAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private sealed class TestTelemetry : IDraftMontagemRealtimeTelemetry
    {
        public List<Failure> Failures { get; } = [];

        public void RecordFailure(
            Guid draftId,
            long? stateVersion,
            string operation,
            long elapsedMilliseconds,
            string failureType)
        {
            Failures.Add(new Failure(draftId, stateVersion, operation, elapsedMilliseconds, failureType));
        }
    }

    private sealed record Failure(
        Guid DraftId,
        long? StateVersion,
        string Operation,
        long ElapsedMilliseconds,
        string FailureType);
}
