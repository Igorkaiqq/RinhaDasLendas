using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RinhaDasLendas.Api.Services;
using RinhaDasLendas.Application.Commands.DraftMontagens;
using RinhaDasLendas.Application.Enums;
using RinhaDasLendas.Application.Handlers.DraftMontagens;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Models;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Tests.Services;

public sealed class DraftMontagemTurnTimerServiceTests
{
    [Fact]
    public async Task CicloDeveCriarUmScopeDeComandoPorIdEContinuarAposFalhaGenerica()
    {
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var senders = new List<Mock<ISender>>();
        using var provider = BuildProvider(
            [new(firstId), new(secondId)],
            senders,
            (_, index, _) => index == 0 ? Task.FromException(new InvalidOperationException("item failure")) : Task.CompletedTask);
        var service = CreateService(provider);

        var processed = await service.RunCycleAsync(CancellationToken.None);

        processed.Should().Be(1);
        senders.Should().HaveCount(2);
        senders.SelectMany(sender => sender.Invocations)
            .Select(invocation => ((ProcessarTurnoDraftMontagemExpiradoCommand)invocation.Arguments[0]).Id)
            .Should().Equal(firstId, secondId);
    }

    [Fact]
    public async Task CicloDeveObservarConflitoDePersistenciaEContinuarNoProximoId()
    {
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var conflict = new DbUpdateConcurrencyException();
        var senders = new List<Mock<ISender>>();
        using var provider = BuildProvider(
            [new(firstId), new(secondId)],
            senders,
            (_, index, _) => index == 0 ? Task.FromException(conflict) : Task.CompletedTask);
        var logger = new RecordingLogger<DraftMontagemTurnTimerService>();
        var service = CreateService(provider, logger);

        var processed = await service.RunCycleAsync(CancellationToken.None);

        processed.Should().Be(1);
        senders.SelectMany(sender => sender.Invocations)
            .Select(invocation => ((ProcessarTurnoDraftMontagemExpiradoCommand)invocation.Arguments[0]).Id)
            .Should().Equal(firstId, secondId);
        logger.Entries.Should().ContainSingle(entry =>
            entry.Level == LogLevel.Error
            && ReferenceEquals(entry.Exception, conflict)
            && entry.Message.Contains(firstId.ToString(), StringComparison.Ordinal));
    }

    [Fact]
    public async Task CicloDeveDescartarScopeDeScanAntesDosScopesDistintosDeComando()
    {
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var candidates = new DraftMontagemRealtimeCandidate[] { new(firstId), new(secondId) };
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.ListExpiredRealtimeAsync(It.IsAny<DateTimeOffset>(), 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(candidates);
        var trace = new ScopedProbeTrace();
        var commandIndex = 0;
        using var provider = new ServiceCollection()
            .AddSingleton(trace)
            .AddScoped<ScopedDbContextProbe>()
            .AddScoped<IDraftMontagemRepository>(services =>
            {
                services.GetRequiredService<ScopedDbContextProbe>().Activate("scan");
                return repository.Object;
            })
            .AddScoped<ISender>(services =>
            {
                var index = commandIndex++;
                var probe = services.GetRequiredService<ScopedDbContextProbe>();
                probe.Activate($"command-{index + 1}");
                var sender = new Mock<ISender>();
                sender.Setup(item => item.Send(It.IsAny<ProcessarTurnoDraftMontagemExpiradoCommand>(), It.IsAny<CancellationToken>()))
                    .Returns((ProcessarTurnoDraftMontagemExpiradoCommand _, CancellationToken _) =>
                    {
                        trace.Record($"send-command-{index + 1}");
                        trace.Active.Should().Equal(probe.Id);
                        return Task.CompletedTask;
                    });
                return sender.Object;
            })
            .BuildServiceProvider();
        var service = CreateService(provider);

        await service.RunCycleAsync(CancellationToken.None);

        trace.Events.Should().Equal(
            "create-scan",
            "dispose-scan",
            "create-command-1",
            "send-command-1",
            "dispose-command-1",
            "create-command-2",
            "send-command-2",
            "dispose-command-2");
        trace.CommandProbeIds.Should().OnlyHaveUniqueItems().And.HaveCount(2);
        trace.Active.Should().BeEmpty();
    }

    [Fact]
    public async Task CicloDeveObservarCancelamentoNaoOriginadoPeloHostEContinuar()
    {
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var senders = new List<Mock<ISender>>();
        using var provider = BuildProvider(
            [new(firstId), new(secondId)],
            senders,
            (_, index, _) => index == 0 ? Task.FromCanceled(new CancellationToken(true)) : Task.CompletedTask);
        var service = CreateService(provider);

        var processed = await service.RunCycleAsync(CancellationToken.None);

        processed.Should().Be(1);
        senders.Should().HaveCount(2);
    }

    [Fact]
    public async Task CicloDevePropagarCancelamentoDoHostSemProcessarProximoId()
    {
        var cancellation = new CancellationTokenSource();
        var senders = new List<Mock<ISender>>();
        using var provider = BuildProvider(
            [new(Guid.NewGuid()), new(Guid.NewGuid())],
            senders,
            (_, _, _) =>
            {
                cancellation.Cancel();
                return Task.FromCanceled(cancellation.Token);
            });
        var service = CreateService(provider);

        await service.Invoking(item => item.RunCycleAsync(cancellation.Token))
            .Should().ThrowAsync<OperationCanceledException>();
        senders.Should().ContainSingle();
    }

    [Fact]
    public async Task ComandoDeveRecarregarEIgnorarCandidatoQueNaoEstaMaisExpirado()
    {
        var montagem = CriarTempoRealIniciado(DateTimeOffset.UtcNow);
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.ReloadByIdAsync(montagem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(montagem);
        var publisher = new Mock<IDraftMontagemRealtimePublisher>();
        var metrics = new Mock<IDraftMontagemMetrics>();
        var handler = new ProcessarTurnoDraftMontagemExpiradoCommandHandler(repository.Object, publisher.Object, metrics.Object);

        await handler.Handle(new(montagem.Id, TimeSpan.FromHours(2)), CancellationToken.None);

        repository.Verify(item => item.ReloadByIdAsync(montagem.Id, CancellationToken.None), Times.Once);
        repository.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        publisher.Verify(item => item.PublishAfterCommitAsync(
            It.IsAny<Guid>(), It.IsAny<DraftMontagemSnapshotScope>(), It.IsAny<DraftMontagemAvailabilityChange>()), Times.Never);
    }

    [Fact]
    public async Task ComandoDeveRegistrarTimeoutSemAuditoriaAdministrativaExtra()
    {
        var montagem = CriarTempoRealIniciado(DateTimeOffset.UtcNow.AddMinutes(-1));
        var sequence = new List<string>();
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.ReloadByIdAsync(montagem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(montagem);
        repository.Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => sequence.Add("save"));
        var publisher = new Mock<IDraftMontagemRealtimePublisher>();
        publisher.Setup(item => item.PublishAfterCommitAsync(
                montagem.Id,
                DraftMontagemSnapshotScope.Active,
                DraftMontagemAvailabilityChange.None))
            .Callback(() => sequence.Add("publish"));
        var metrics = new Mock<IDraftMontagemMetrics>();
        metrics.Setup(item => item.RecordDraftTimeout(montagem.Id))
            .Callback(() => sequence.Add("metric"));
        var handler = new ProcessarTurnoDraftMontagemExpiradoCommandHandler(repository.Object, publisher.Object, metrics.Object);

        await handler.Handle(new(montagem.Id, TimeSpan.FromHours(2)), CancellationToken.None);

        montagem.Escolhas.Should().ContainSingle(escolha => escolha.Tipo == DraftMontagemEscolhaTipo.Timeout);
        montagem.AcoesAdministrativas.Should().BeEmpty();
        repository.Verify(item => item.SaveChangesAsync(CancellationToken.None), Times.Once);
        publisher.Verify(item => item.PublishAfterCommitAsync(montagem.Id, DraftMontagemSnapshotScope.Active, DraftMontagemAvailabilityChange.None), Times.Once);
        metrics.Verify(item => item.RecordDraftTimeout(montagem.Id), Times.Once);
        metrics.Verify(item => item.RecordDraftCancelled(It.IsAny<Guid>()), Times.Never);
        sequence.Should().Equal("save", "publish", "metric");
    }

    [Fact]
    public async Task ComandoDevePropagarConflitoDeSaveSemPublicarNemRegistrarMetricaDeSucesso()
    {
        var montagem = CriarTempoRealIniciado(DateTimeOffset.UtcNow.AddMinutes(-1));
        var conflict = new DbUpdateConcurrencyException();
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.ReloadByIdAsync(montagem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(montagem);
        repository.Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(conflict);
        var publisher = new Mock<IDraftMontagemRealtimePublisher>();
        var metrics = new Mock<IDraftMontagemMetrics>();
        var handler = new ProcessarTurnoDraftMontagemExpiradoCommandHandler(repository.Object, publisher.Object, metrics.Object);

        var thrown = await handler.Invoking(item => item.Handle(
                new(montagem.Id, TimeSpan.FromHours(2)),
                CancellationToken.None))
            .Should().ThrowAsync<DbUpdateConcurrencyException>();

        thrown.Which.Should().BeSameAs(conflict);
        publisher.Verify(item => item.PublishAfterCommitAsync(
            It.IsAny<Guid>(), It.IsAny<DraftMontagemSnapshotScope>(), It.IsAny<DraftMontagemAvailabilityChange>()), Times.Never);
        metrics.Verify(item => item.RecordDraftTimeout(It.IsAny<Guid>()), Times.Never);
        metrics.Verify(item => item.RecordDraftCancelled(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ComandoDeveCancelarDuracaoMaximaComUmaUnicaAuditoriaSystem()
    {
        var montagem = CriarTempoRealIniciado(DateTimeOffset.UtcNow.AddHours(-3));
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.ReloadByIdAsync(montagem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(montagem);
        var publisher = new Mock<IDraftMontagemRealtimePublisher>();
        var metrics = new Mock<IDraftMontagemMetrics>();
        var handler = new ProcessarTurnoDraftMontagemExpiradoCommandHandler(repository.Object, publisher.Object, metrics.Object);

        await handler.Handle(new(montagem.Id, TimeSpan.FromHours(2)), CancellationToken.None);

        montagem.Status.Should().Be(DraftMontagemStatus.Cancelada);
        montagem.Escolhas.Should().BeEmpty();
        montagem.AcoesAdministrativas.Should().ContainSingle();
        montagem.AcoesAdministrativas.Single().ResponsavelTipo.Should().Be(DraftMontagemActorType.System);
        montagem.AcoesAdministrativas.Single().ResponsavelUsuarioId.Should().BeNull();
        repository.Verify(item => item.SaveChangesAsync(CancellationToken.None), Times.Once);
        publisher.Verify(item => item.PublishAfterCommitAsync(montagem.Id, DraftMontagemSnapshotScope.Active, DraftMontagemAvailabilityChange.None), Times.Once);
        metrics.Verify(item => item.RecordDraftCancelled(montagem.Id), Times.Once);
        metrics.Verify(item => item.RecordDraftTimeout(It.IsAny<Guid>()), Times.Never);
    }

    private static ServiceProvider BuildProvider(
        IReadOnlyCollection<DraftMontagemRealtimeCandidate> candidates,
        ICollection<Mock<ISender>> senders,
        Func<ProcessarTurnoDraftMontagemExpiradoCommand, int, CancellationToken, Task> send)
    {
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.ListExpiredRealtimeAsync(It.IsAny<DateTimeOffset>(), 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(candidates);
        var senderIndex = 0;
        return new ServiceCollection()
            .AddSingleton(repository.Object)
            .AddScoped<ISender>(_ =>
            {
                var index = senderIndex++;
                var sender = new Mock<ISender>();
                sender.Setup(item => item.Send(It.IsAny<ProcessarTurnoDraftMontagemExpiradoCommand>(), It.IsAny<CancellationToken>()))
                    .Returns((ProcessarTurnoDraftMontagemExpiradoCommand command, CancellationToken token) => send(command, index, token));
                senders.Add(sender);
                return sender.Object;
            })
            .BuildServiceProvider();
    }

    private static DraftMontagemTurnTimerService CreateService(
        IServiceProvider provider,
        ILogger<DraftMontagemTurnTimerService>? logger = null)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DraftMontagem:RealtimeMaxDurationMinutes"] = "120",
        }).Build();
        return new DraftMontagemTurnTimerService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            configuration,
            logger ?? NullLogger<DraftMontagemTurnTimerService>.Instance);
    }

    private static DraftMontagem CriarTempoRealIniciado(DateTimeOffset inicio)
    {
        var montagem = DraftMontagem.CriarPorPresenca("Rinha", null, 2);
        var jogadoresIds = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToList();
        foreach (var jogadorId in jogadoresIds)
        {
            montagem.ConfirmarPresenca(Guid.NewGuid(), jogadorId, null, DraftMontagemPresencaOrigem.Web);
        }

        montagem.EncerrarPresenca(true, 2);
        montagem.SelecionarModo(DraftMontagemModo.TempoReal, jogadoresIds.ToHashSet());
        var capitaesIds = jogadoresIds.Take(2).ToList();
        montagem.DefinirCapitaes(capitaesIds, capitaesIds.ToHashSet());
        montagem.DefinirOrdemEscolha(DraftMontagemOrdemEscolhaModo.Manual, capitaesIds);
        montagem.IniciarTempoReal(inicio, capitaesIds.ToHashSet());
        return montagem;
    }

    private sealed class ScopedProbeTrace
    {
        private readonly List<Guid> active = [];

        public List<string> Events { get; } = [];
        public List<Guid> CommandProbeIds { get; } = [];
        public IReadOnlyCollection<Guid> Active => active;

        public void Activate(Guid id, string role)
        {
            active.Add(id);
            Events.Add($"create-{role}");
            if (role.StartsWith("command-", StringComparison.Ordinal))
            {
                CommandProbeIds.Add(id);
            }
        }

        public void Record(string value) => Events.Add(value);

        public void Dispose(Guid id, string role)
        {
            active.Remove(id).Should().BeTrue();
            Events.Add($"dispose-{role}");
        }
    }

    private sealed class ScopedDbContextProbe(ScopedProbeTrace trace) : IDisposable
    {
        private string? role;

        public Guid Id { get; } = Guid.NewGuid();

        public void Activate(string value)
        {
            role.Should().BeNull();
            role = value;
            trace.Activate(Id, value);
        }

        public void Dispose()
        {
            if (role is not null)
            {
                trace.Dispose(Id, role);
            }
        }
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception), exception));
        }
    }
}
