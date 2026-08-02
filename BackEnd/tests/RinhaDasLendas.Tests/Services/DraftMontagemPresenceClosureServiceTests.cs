using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
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
using RinhaDasLendas.Infrastructure.Persistence;
using RinhaDasLendas.Infrastructure.Repositories;

namespace RinhaDasLendas.Tests.Services;

public sealed class DraftMontagemPresenceClosureServiceTests
{
    [Fact]
    public void CandidatoDeEncerramentoDePresencaDeveConterSomenteId()
    {
        typeof(DraftMontagemPresenceClosureCandidate).GetProperties()
            .Select(property => property.Name)
            .Should().Equal(nameof(DraftMontagemPresenceClosureCandidate.Id));
    }

    [Fact]
    public void ConsultaDeCandidatosDePresencaDeveProjetarSomenteIdSemIncludes()
    {
        var options = new DbContextOptionsBuilder<RinhaDasLendasDbContext>()
            .UseNpgsql("Host=localhost;Database=projection_only;Username=test;Password=test")
            .Options;
        using var context = new RinhaDasLendasDbContext(options);

        var sql = DraftMontagemRepository
            .BuildExpiredPresenceCandidatesQuery(context.DraftMontagens, DateTimeOffset.UtcNow, 20)
            .ToQueryString();

        sql.Should().Contain("SELECT d.id");
        var normalizedSql = sql.ToUpperInvariant();
        normalizedSql.Should().NotContain("JOIN");
        normalizedSql.Should().NotContain("DRAFT_MONTAGEM_PRESENCAS");
        normalizedSql.Should().NotContain("DRAFT_MONTAGEM_PUBLICACOES_DISCORD");
        normalizedSql.Should().NotContain("DRAFT_MONTAGEM_ACOES_ADMINISTRATIVAS");
    }

    [Fact]
    public async Task CicloDeveCriarUmScopeDeComandoPorIdEContinuarAposFalhaGenericaComLogSeguro()
    {
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var senders = new List<Mock<ISender>>();
        using var provider = BuildProvider(
            [new(firstId), new(secondId)],
            senders,
            (_, index, _) => index == 0
                ? Task.FromException(new InvalidOperationException("sensitive-value"))
                : Task.CompletedTask);
        var logger = new RecordingLogger<DraftMontagemPresenceClosureService>();
        var service = CreateService(provider, logger);

        var processed = await service.RunCycleAsync(CancellationToken.None);

        processed.Should().Be(1);
        senders.SelectMany(sender => sender.Invocations)
            .Select(invocation => ((EncerrarPresencaDraftMontagemAutomaticamenteCommand)invocation.Arguments[0]).Id)
            .Should().Equal(firstId, secondId);
        logger.Entries.Should().ContainSingle(entry =>
            entry.Level == LogLevel.Error
            && entry.Message.Contains(firstId.ToString(), StringComparison.Ordinal)
            && entry.Message.Contains(nameof(InvalidOperationException), StringComparison.Ordinal));
        logger.Entries.Select(entry => entry.Message).Should().NotContain(message =>
            message.Contains("sensitive-value", StringComparison.Ordinal));
        logger.Entries.Should().OnlyContain(entry => entry.Exception == null);
    }

    [Fact]
    public async Task CicloDeveObservarConflitoDePersistenciaEContinuarNoProximoId()
    {
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var conflict = new DbUpdateConcurrencyException("sensitive-conflict");
        var senders = new List<Mock<ISender>>();
        using var provider = BuildProvider(
            [new(firstId), new(secondId)],
            senders,
            (_, index, _) => index == 0 ? Task.FromException(conflict) : Task.CompletedTask);
        var logger = new RecordingLogger<DraftMontagemPresenceClosureService>();

        var processed = await CreateService(provider, logger).RunCycleAsync(CancellationToken.None);

        processed.Should().Be(1);
        senders.Should().HaveCount(2);
        logger.Entries.Should().ContainSingle(entry =>
            entry.Level == LogLevel.Error
            && entry.Message.Contains(firstId.ToString(), StringComparison.Ordinal)
            && entry.Message.Contains(nameof(DbUpdateConcurrencyException), StringComparison.Ordinal)
            && !entry.Message.Contains("sensitive-conflict", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CicloDeveDescartarScopeDeScanAntesDosScopesDistintosDeComando()
    {
        var candidates = new DraftMontagemPresenceClosureCandidate[] { new(Guid.NewGuid()), new(Guid.NewGuid()) };
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.ListExpiredPresenceAsync(It.IsAny<DateTimeOffset>(), 20, It.IsAny<CancellationToken>()))
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
                sender.Setup(item => item.Send(
                        It.IsAny<EncerrarPresencaDraftMontagemAutomaticamenteCommand>(),
                        It.IsAny<CancellationToken>()))
                    .Returns((EncerrarPresencaDraftMontagemAutomaticamenteCommand _, CancellationToken _) =>
                    {
                        trace.Record($"send-command-{index + 1}");
                        trace.Active.Should().Equal(probe.Id);
                        return Task.CompletedTask;
                    });
                return sender.Object;
            })
            .BuildServiceProvider();

        await CreateService(provider).RunCycleAsync(CancellationToken.None);

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
        var senders = new List<Mock<ISender>>();
        using var provider = BuildProvider(
            [new(Guid.NewGuid()), new(Guid.NewGuid())],
            senders,
            (_, index, _) => index == 0
                ? Task.FromCanceled(new CancellationToken(true))
                : Task.CompletedTask);

        var processed = await CreateService(provider).RunCycleAsync(CancellationToken.None);

        processed.Should().Be(1);
        senders.Should().HaveCount(2);
    }

    [Fact]
    public async Task CicloDevePropagarCancelamentoDoHostSemProcessarProximoId()
    {
        using var cancellation = new CancellationTokenSource();
        var senders = new List<Mock<ISender>>();
        using var provider = BuildProvider(
            [new(Guid.NewGuid()), new(Guid.NewGuid())],
            senders,
            (_, _, _) =>
            {
                cancellation.Cancel();
                return Task.FromCanceled(cancellation.Token);
            });

        await CreateService(provider).Invoking(service => service.RunCycleAsync(cancellation.Token))
            .Should().ThrowAsync<OperationCanceledException>();
        senders.Should().ContainSingle();
    }

    [Fact]
    public async Task ComandoDeveRecarregarEIgnorarCandidatoQueNaoEstaMaisExpirado()
    {
        var montagem = CriarPresencaAberta(10, DateTimeOffset.UtcNow.AddMinutes(1));
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.ReloadByIdAsync(montagem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(montagem);
        var publisher = new Mock<IDraftMontagemRealtimePublisher>();
        var metrics = new Mock<IDraftMontagemMetrics>();
        var handler = new EncerrarPresencaDraftMontagemAutomaticamenteCommandHandler(
            repository.Object, publisher.Object, metrics.Object);

        await handler.Handle(new(montagem.Id), CancellationToken.None);

        repository.Verify(item => item.ReloadByIdAsync(montagem.Id, CancellationToken.None), Times.Once);
        repository.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        publisher.Verify(item => item.PublishAfterCommitAsync(
            It.IsAny<Guid>(), It.IsAny<DraftMontagemSnapshotScope>(), It.IsAny<DraftMontagemAvailabilityChange>()), Times.Never);
    }

    [Fact]
    public async Task ComandoDeveEncerrarDezPresencasNaOrdemSavePublishMetrica()
    {
        var montagem = CriarPresencaAberta(10, DateTimeOffset.UtcNow.AddMinutes(-1));
        var sequence = new List<string>();
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.ReloadByIdAsync(montagem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(montagem);
        repository.Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => sequence.Add("save"));
        var publisher = new Mock<IDraftMontagemRealtimePublisher>();
        publisher.Setup(item => item.PublishAfterCommitAsync(
                montagem.Id, DraftMontagemSnapshotScope.Active, DraftMontagemAvailabilityChange.None))
            .Callback(() => sequence.Add("publish"));
        var metrics = new Mock<IDraftMontagemMetrics>();
        metrics.Setup(item => item.RecordPresenceClosed(montagem.Id))
            .Callback(() => sequence.Add("metric"));
        var handler = new EncerrarPresencaDraftMontagemAutomaticamenteCommandHandler(
            repository.Object, publisher.Object, metrics.Object);

        await handler.Handle(new(montagem.Id), CancellationToken.None);

        montagem.Status.Should().Be(DraftMontagemStatus.PresencaEncerrada);
        montagem.AcoesAdministrativas.Should().BeEmpty();
        sequence.Should().Equal("save", "publish", "metric");
        metrics.Verify(item => item.RecordDraftCancelled(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ComandoDeveCancelarMenosDezPresencasComUmaUnicaAuditoriaSystem()
    {
        var montagem = CriarPresencaAberta(9, DateTimeOffset.UtcNow.AddMinutes(-1));
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.ReloadByIdAsync(montagem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(montagem);
        var publisher = new Mock<IDraftMontagemRealtimePublisher>();
        var metrics = new Mock<IDraftMontagemMetrics>();
        var handler = new EncerrarPresencaDraftMontagemAutomaticamenteCommandHandler(
            repository.Object, publisher.Object, metrics.Object);

        await handler.Handle(new(montagem.Id), CancellationToken.None);

        montagem.Status.Should().Be(DraftMontagemStatus.Cancelada);
        montagem.AcoesAdministrativas.Should().ContainSingle();
        montagem.AcoesAdministrativas.Single().ResponsavelTipo.Should().Be(DraftMontagemActorType.System);
        montagem.AcoesAdministrativas.Single().ResponsavelUsuarioId.Should().BeNull();
        repository.Verify(item => item.SaveChangesAsync(CancellationToken.None), Times.Once);
        publisher.Verify(item => item.PublishAfterCommitAsync(
            montagem.Id, DraftMontagemSnapshotScope.Active, DraftMontagemAvailabilityChange.None), Times.Once);
        metrics.Verify(item => item.RecordDraftCancelled(montagem.Id), Times.Once);
        metrics.Verify(item => item.RecordPresenceClosed(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ComandoDevePropagarConflitoDeSaveSemPublicarNemRegistrarMetricaDeSucesso()
    {
        var montagem = CriarPresencaAberta(10, DateTimeOffset.UtcNow.AddMinutes(-1));
        var conflict = new DbUpdateConcurrencyException();
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.ReloadByIdAsync(montagem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(montagem);
        repository.Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(conflict);
        var publisher = new Mock<IDraftMontagemRealtimePublisher>();
        var metrics = new Mock<IDraftMontagemMetrics>();
        var handler = new EncerrarPresencaDraftMontagemAutomaticamenteCommandHandler(
            repository.Object, publisher.Object, metrics.Object);

        var thrown = await handler.Invoking(item => item.Handle(new(montagem.Id), CancellationToken.None))
            .Should().ThrowAsync<DbUpdateConcurrencyException>();

        thrown.Which.Should().BeSameAs(conflict);
        publisher.Verify(item => item.PublishAfterCommitAsync(
            It.IsAny<Guid>(), It.IsAny<DraftMontagemSnapshotScope>(), It.IsAny<DraftMontagemAvailabilityChange>()), Times.Never);
        metrics.Verify(item => item.RecordPresenceClosed(It.IsAny<Guid>()), Times.Never);
        metrics.Verify(item => item.RecordDraftCancelled(It.IsAny<Guid>()), Times.Never);
    }

    private static ServiceProvider BuildProvider(
        IReadOnlyCollection<DraftMontagemPresenceClosureCandidate> candidates,
        ICollection<Mock<ISender>> senders,
        Func<EncerrarPresencaDraftMontagemAutomaticamenteCommand, int, CancellationToken, Task> send)
    {
        var repository = new Mock<IDraftMontagemRepository>();
        repository.Setup(item => item.ListExpiredPresenceAsync(
                It.IsAny<DateTimeOffset>(), 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(candidates);
        var senderIndex = 0;
        return new ServiceCollection()
            .AddSingleton(repository.Object)
            .AddScoped<ISender>(_ =>
            {
                var index = senderIndex++;
                var sender = new Mock<ISender>();
                sender.Setup(item => item.Send(
                        It.IsAny<EncerrarPresencaDraftMontagemAutomaticamenteCommand>(),
                        It.IsAny<CancellationToken>()))
                    .Returns((EncerrarPresencaDraftMontagemAutomaticamenteCommand command, CancellationToken token) =>
                        send(command, index, token));
                senders.Add(sender);
                return sender.Object;
            })
            .BuildServiceProvider();
    }

    private static DraftMontagemPresenceClosureService CreateService(
        IServiceProvider provider,
        ILogger<DraftMontagemPresenceClosureService>? logger = null) => new(
        provider.GetRequiredService<IServiceScopeFactory>(),
        logger ?? NullLogger<DraftMontagemPresenceClosureService>.Instance);

    private static DraftMontagem CriarPresencaAberta(int confirmados, DateTimeOffset encerramento)
    {
        var montagem = DraftMontagem.CriarPorPresenca("Rinha", null, 5);
        for (var index = 0; index < confirmados; index++)
        {
            montagem.ConfirmarPresenca(Guid.NewGuid(), Guid.NewGuid(), null, DraftMontagemPresencaOrigem.Web);
        }

        montagem.ConfigurarEncerramentoPresenca(encerramento);
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
