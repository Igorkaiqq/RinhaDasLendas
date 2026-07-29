using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RinhaDasLendas.Api.Services;
using RinhaDasLendas.Application.Commands.AgendamentosPresenca;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Handlers.AgendamentosPresenca;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Models;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Tests.Services;

public sealed class AgendamentoPresencaTask5RevisionTests
{
    private static readonly Guid UserId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly DateOnly Today = new(2026, 7, 24);
    private static readonly DateTimeOffset Publication = new(2026, 7, 24, 21, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Closure = new(2026, 7, 24, 23, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task InvalidTimezoneMustPersistTerminalFailureBeforeAdvancingAndNotRepeatNextCycle()
    {
        var schedule = CreateSchedule(Today.AddDays(-1));
        var repository = BaseRepository(schedule);
        repository.Setup(item => item.TryUpsertFailedTimeZoneOccurrenceAsync(
                schedule.Id, Today, 1, DiaSemanaIso.Sexta, schedule.HorarioPublicacaoLocal,
                schedule.HorarioEncerramentoLocal, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgendamentoPresencaOccurrenceWriteResult(
                OcorrenciaAgendamentoPresencaStatus.Falha, true));
        var handler = CreateHandler(repository, new ConstantClock(Publication), new InvalidTimeZone());

        var first = await handler.Handle(new(Publication), CancellationToken.None);
        var second = await handler.Handle(new(Publication), CancellationToken.None);

        first.Falhas.Should().Be(1);
        second.Falhas.Should().Be(0);
        schedule.UltimaDataAvaliada.Should().Be(Today);
        repository.Verify(item => item.TryUpsertFailedTimeZoneOccurrenceAsync(
            schedule.Id, Today, 1, DiaSemanaIso.Sexta, schedule.HorarioPublicacaoLocal,
            schedule.HorarioEncerramentoLocal, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(item => item.TryCompleteWithDraftAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DraftMontagem>(), It.IsAny<DateTimeOffset>(),
            It.IsAny<CancellationToken>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task FreshClockCrossingClosureAfterClaimMustMarkMissedInsteadOfCreatingDraft()
    {
        var schedule = CreateSchedule(Today.AddDays(-1));
        var repository = BaseRepository(schedule);
        repository.Setup(item => item.TryClaimOccurrenceAsync(
                schedule.Id, Today, Publication, Closure, It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((Guid _, DateOnly _, DateTimeOffset _, DateTimeOffset _, Guid claimId,
                DateTimeOffset _, DateTimeOffset _, CancellationToken _, string _, string _) =>
                new AgendamentoPresencaOcorrenciaClaim(
                    Guid.NewGuid(), claimId, true, OcorrenciaAgendamentoPresencaStatus.Processando,
                    "Snapshot", "Observacao snapshot"));
        repository.Setup(item => item.TryMarkClaimedOccurrenceMissedAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), Closure, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgendamentoPresencaOccurrenceWriteResult(
                OcorrenciaAgendamentoPresencaStatus.Perdida, true));
        var clock = new SequenceClock(
            Publication, Publication, Publication, Publication, Publication, Publication, Closure, Closure);
        var handler = CreateHandler(repository, clock);

        var result = await handler.Handle(new(Publication), CancellationToken.None);

        result.Perdidas.Should().Be(1);
        result.Criadas.Should().Be(0);
        repository.Verify(item => item.TryCompleteWithDraftAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DraftMontagem>(), It.IsAny<DateTimeOffset>(),
            It.IsAny<CancellationToken>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExistingOccurrenceMustBuildDraftFromPersistedSnapshotsAfterScheduleEdit()
    {
        var schedule = CreateSchedule(Today.AddDays(-1));
        schedule.Editar("Nome editado", "Observacao editada", schedule.HorarioPublicacaoLocal,
            schedule.HorarioEncerramentoLocal, [DiaSemanaIso.Sexta], UserId, Publication.AddMinutes(-1));
        var repository = BaseRepository(schedule);
        repository.Setup(item => item.TryClaimOccurrenceAsync(
                schedule.Id, Today, Publication, Closure, It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((Guid _, DateOnly _, DateTimeOffset _, DateTimeOffset _, Guid claimId,
                DateTimeOffset _, DateTimeOffset _, CancellationToken _, string _, string _) =>
                new AgendamentoPresencaOcorrenciaClaim(
                    Guid.NewGuid(), claimId, true, OcorrenciaAgendamentoPresencaStatus.Processando,
                    "Nome original", "Observacao original"));
        DraftMontagem? completedDraft = null;
        repository.Setup(item => item.TryCompleteWithDraftAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DraftMontagem>(), It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback<Guid, Guid, DraftMontagem, DateTimeOffset, CancellationToken, string, string>(
                (_, _, draft, _, _, _, _) => completedDraft = draft)
            .ReturnsAsync(true);
        var handler = CreateHandler(repository, new ConstantClock(Publication));

        await handler.Handle(new(Publication), CancellationToken.None);

        completedDraft.Should().NotBeNull();
        completedDraft!.Nome.Should().Be("Nome original - 24/07/2026");
        completedDraft.Observacoes.Should().Be("Observacao original");
        completedDraft.CicloVersao.Should().Be(DraftMontagemCicloVersao.ModoPosPresenca);
        completedDraft.Modo.Should().BeNull();
        completedDraft.Times.Should().BeEmpty();
    }

    [Fact]
    public async Task TransientConfigurationFailureMustNotBlockDraftOrAdvanceMarkerAndMustDiagnoseSafely()
    {
        var schedule = CreateSchedule(Today.AddDays(-1));
        var repository = BaseRepository(schedule);
        var discord = new Mock<IDiscordConfigurationService>();
        discord.Setup(item => item.GetAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("sensitive-value"));
        var diagnostics = new Mock<IAgendamentoPresencaDiagnostics>();
        var handler = CreateHandler(repository, new ConstantClock(Publication),
            discord: discord.Object, diagnostics: diagnostics.Object);

        var result = await handler.Handle(new(Publication), CancellationToken.None);

        result.Falhas.Should().Be(1);
        schedule.UltimaDataAvaliada.Should().Be(Today.AddDays(-1));
        diagnostics.Verify(item => item.RecordFailure(
            AgendamentoPresencaDiagnosticStage.DiscordConfiguration,
            nameof(InvalidOperationException),
            MessageCodes.PresenceScheduleOccurrenceConflict), Times.Once);
        diagnostics.Invocations.SelectMany(invocation => invocation.Arguments)
            .Select(argument => argument?.ToString())
            .Should().NotContain(value => value != null
                && value.Contains("sensitive-value", StringComparison.Ordinal));
        repository.Verify(item => item.TryUpsertBlockedOccurrenceAsync(
            It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(),
            It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MissingDisabledAndIncompleteConfigurationMustBlockWithMV098()
    {
        DiscordConfigurationDto?[] configurations =
        [
            null,
            ValidConfiguration() with { BotEnabled = false },
            ValidConfiguration() with { PresenceChannelId = "" }
        ];

        foreach (var configuration in configurations)
        {
            var schedule = CreateSchedule(Today.AddDays(-1));
            var repository = BaseRepository(schedule);
            repository.Setup(item => item.TryUpsertBlockedOccurrenceAsync(
                    schedule.Id, Today, Publication, Closure,
                    MessageCodes.PresenceScheduleDiscordUnavailable,
                    It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AgendamentoPresencaOccurrenceWriteResult(
                    OcorrenciaAgendamentoPresencaStatus.Bloqueada, true));
            var discord = new Mock<IDiscordConfigurationService>();
            discord.Setup(item => item.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(configuration);

            var result = await CreateHandler(
                    repository, new ConstantClock(Publication), discord: discord.Object)
                .Handle(new(Publication), CancellationToken.None);

            result.Bloqueadas.Should().Be(1);
            schedule.UltimaDataAvaliada.Should().Be(Today);
        }
    }

    [Fact]
    public async Task ExpiredDateMustBecomeMissedEvenWhenConfigurationLookupFailsTransiently()
    {
        var schedule = CreateSchedule(Today.AddDays(-1));
        var repository = BaseRepository(schedule);
        repository.Setup(item => item.TryUpsertMissedOccurrenceAsync(
                schedule.Id, Today, Publication, Closure, MessageCodes.PresenceScheduleWindowExpired,
                Closure, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgendamentoPresencaOccurrenceWriteResult(
                OcorrenciaAgendamentoPresencaStatus.Perdida, true));
        var discord = new Mock<IDiscordConfigurationService>();
        discord.Setup(item => item.GetAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("sensitive-value"));

        var result = await CreateHandler(
                repository, new ConstantClock(Closure), discord: discord.Object)
            .Handle(new(Closure), CancellationToken.None);

        result.Perdidas.Should().Be(1);
        schedule.UltimaDataAvaliada.Should().Be(Today);
    }

    [Fact]
    public async Task BlockedOccurrenceMustNotCountAsEvaluatedScheduleOrRewriteWhenUnavailable()
    {
        var schedule = CreateSchedule(Today);
        var occurrence = OcorrenciaAgendamentoPresenca.Bloqueada(
            schedule.Id, Today, Publication, Closure, MessageCodes.PresenceScheduleDiscordUnavailable, Publication,
            schedule.Nome, schedule.Observacao);
        var repository = BaseRepository(schedule);
        repository.Setup(item => item.ListBlockedAsync(It.IsAny<DateTimeOffset>(), 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync([occurrence]);
        var metrics = new Mock<IAgendamentoPresencaMetrics>();
        var unavailableDiscord = new Mock<IDiscordConfigurationService>();
        unavailableDiscord.Setup(item => item.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((DiscordConfigurationDto?)null);
        var handler = CreateHandler(repository, new ConstantClock(Publication.AddMinutes(1)),
            discord: unavailableDiscord.Object, metrics: metrics.Object);

        var result = await handler.Handle(new(Publication), CancellationToken.None);

        result.Avaliadas.Should().Be(0);
        result.Bloqueadas.Should().Be(0);
        metrics.Verify(item => item.RecordEvaluated(), Times.Never);
        repository.Verify(item => item.TryUpsertBlockedOccurrenceAsync(
            It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(),
            It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LargeBacklogMustBoundDatesPerCycleAndEventuallyFinishWithoutAgeCutoff()
    {
        var marker = new DateOnly(2026, 7, 19);
        var schedule = new AgendamentoPresenca(
            "Agenda", null, new TimeOnly(10, 0), new TimeOnly(11, 0),
            [DiaSemanaIso.Segunda, DiaSemanaIso.Terca, DiaSemanaIso.Quarta, DiaSemanaIso.Quinta,
                DiaSemanaIso.Sexta, DiaSemanaIso.Sabado, DiaSemanaIso.Domingo],
            marker, UserId, Publication.AddDays(-10));
        var repository = BaseRepository(schedule);
        repository.Setup(item => item.TryUpsertMissedOccurrenceAsync(
                schedule.Id, It.IsAny<DateOnly>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(),
                MessageCodes.PresenceScheduleWindowExpired, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgendamentoPresencaOccurrenceWriteResult(
                OcorrenciaAgendamentoPresencaStatus.Perdida, true));
        var options = new AgendamentoPresencaProcessingOptions
        {
            MaxBlockedPerCycle = 10,
            MaxSchedulesPerCycle = 10,
            MaxDatesPerSchedulePerCycle = 2
        };
        var handler = CreateHandler(repository, new ConstantClock(Closure), options: options);

        await handler.Handle(new(Closure), CancellationToken.None);
        schedule.UltimaDataAvaliada.Should().Be(marker.AddDays(2));
        await handler.Handle(new(Closure), CancellationToken.None);
        schedule.UltimaDataAvaliada.Should().Be(marker.AddDays(4));
        await handler.Handle(new(Closure), CancellationToken.None);

        schedule.UltimaDataAvaliada.Should().Be(Today);
        repository.Verify(item => item.TryUpsertMissedOccurrenceAsync(
            schedule.Id, It.IsAny<DateOnly>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(),
            MessageCodes.PresenceScheduleWindowExpired, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Exactly(5));
    }

    [Fact]
    public async Task PersistentFailuresBeforeLimitMustNotStarveShortDueScheduleAcrossCycles()
    {
        var first = CreateSchedule(Today.AddDays(-1));
        var second = CreateSchedule(Today.AddDays(-1));
        var shortDue = CreateSchedule(Today.AddDays(-1));
        var repository = new Mock<IAgendamentoPresencaRepository>();
        repository.Setup(item => item.ListBlockedAsync(It.IsAny<DateTimeOffset>(), 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        repository.Setup(item => item.ListCandidatesAsync(
                It.IsAny<DateTimeOffset>(), null, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync([first, second]);
        repository.Setup(item => item.ListCandidatesAsync(
                It.IsAny<DateTimeOffset>(), second.Id, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync([shortDue, first]);
        repository.Setup(item => item.GetProcessingCandidateAsync(first.Id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("persistent"));
        repository.Setup(item => item.GetProcessingCandidateAsync(second.Id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("persistent"));
        repository.Setup(item => item.GetProcessingCandidateAsync(shortDue.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgendamentoPresencaProcessingCandidate(shortDue, 1));
        repository.Setup(item => item.TryClaimOccurrenceAsync(
                shortDue.Id, Today, Publication, Closure, It.IsAny<Guid>(), Publication.AddMinutes(5),
                Publication, It.IsAny<CancellationToken>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((Guid _, DateOnly _, DateTimeOffset _, DateTimeOffset _, Guid claimId,
                DateTimeOffset _, DateTimeOffset _, CancellationToken _, string _, string _) =>
                new AgendamentoPresencaOcorrenciaClaim(
                    Guid.NewGuid(), claimId, true, OcorrenciaAgendamentoPresencaStatus.Processando,
                    shortDue.Nome, shortDue.Observacao));
        repository.Setup(item => item.TryCompleteWithDraftAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DraftMontagem>(), Publication,
                It.IsAny<CancellationToken>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        repository.Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var handler = CreateHandler(repository, new ConstantClock(Publication), options: new()
        {
            MaxBlockedPerCycle = 10,
            MaxSchedulesPerCycle = 2,
            MaxDatesPerSchedulePerCycle = 1
        });

        var firstCycle = await handler.Handle(new(Publication), CancellationToken.None);
        var secondCycle = await handler.Handle(new(Publication, firstCycle.Cursor), CancellationToken.None);

        firstCycle.Falhas.Should().Be(2);
        secondCycle.Criadas.Should().Be(1);
        shortDue.UltimaDataAvaliada.Should().Be(Today);
    }

    [Fact]
    public async Task CancellationMidBatchMustPersistCompletedMarkerAndResumeRemainingDatesLater()
    {
        var marker = new DateOnly(2026, 7, 21);
        var schedule = new AgendamentoPresenca(
            "Agenda", null, new TimeOnly(10, 0), new TimeOnly(11, 0),
            [DiaSemanaIso.Terca, DiaSemanaIso.Quarta, DiaSemanaIso.Quinta],
            marker, UserId, Publication.AddDays(-10));
        var repository = BaseRepository(schedule);
        using var cancellation = new CancellationTokenSource();
        var writes = 0;
        repository.Setup(item => item.TryUpsertMissedOccurrenceAsync(
                schedule.Id, It.IsAny<DateOnly>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(),
                MessageCodes.PresenceScheduleWindowExpired, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                if (Interlocked.Increment(ref writes) == 1)
                {
                    cancellation.Cancel();
                }

                return new AgendamentoPresencaOccurrenceWriteResult(
                    OcorrenciaAgendamentoPresencaStatus.Perdida, true);
            });
        var handler = CreateHandler(repository, new ConstantClock(Closure));

        await handler.Invoking(value => value.Handle(new(Closure), cancellation.Token))
            .Should().ThrowAsync<OperationCanceledException>();
        schedule.UltimaDataAvaliada.Should().Be(marker.AddDays(1));

        await handler.Handle(new(Closure), CancellationToken.None);
        schedule.UltimaDataAvaliada.Should().Be(Today);
        writes.Should().Be(2);
    }

    [Fact]
    public async Task RealBackgroundServiceMustRunPeriodicLoopAndStopDuringTimerWait()
    {
        var cycles = 0;
        var twoCycles = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var sender = new Mock<ISender>();
        sender.Setup(item => item.Send(It.IsAny<ProcessarAgendamentosPresencaDevidosCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                if (Interlocked.Increment(ref cycles) == 2)
                {
                    twoCycles.TrySetResult();
                }

                return new AgendamentoPresencaCycleResult(0, 0, 0, 0, 0);
            });
        using var provider = new ServiceCollection().AddScoped(_ => sender.Object).BuildServiceProvider();
        var service = new AgendamentoPresencaExecutionService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            new ConstantClock(Publication),
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PresenceSchedule:IntervalSeconds"] = "1"
            }).Build(),
            NullLogger<AgendamentoPresencaExecutionService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await twoCycles.Task.WaitAsync(TimeSpan.FromSeconds(3));
        await service.StopAsync(CancellationToken.None);
        var countAtStop = cycles;
        await Task.Delay(1100);

        cycles.Should().Be(countAtStop);
        countAtStop.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task BackgroundServiceMustCarryCandidateAndBlockedCursorsAcrossCycles()
    {
        var cursor = Guid.NewGuid();
        var blockedCursor = Guid.NewGuid();
        var commands = new List<ProcessarAgendamentosPresencaDevidosCommand>();
        var sender = new Mock<ISender>();
        sender.Setup(item => item.Send(
                It.IsAny<ProcessarAgendamentosPresencaDevidosCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProcessarAgendamentosPresencaDevidosCommand command, CancellationToken _) =>
            {
                commands.Add(command);
                return new AgendamentoPresencaCycleResult(0, 0, 0, 0, 0, cursor, blockedCursor);
            });
        using var provider = new ServiceCollection().AddScoped(_ => sender.Object).BuildServiceProvider();
        var service = new AgendamentoPresencaExecutionService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            new ConstantClock(Publication),
            new ConfigurationBuilder().Build(),
            NullLogger<AgendamentoPresencaExecutionService>.Instance);

        await service.RunCycleAsync(CancellationToken.None);
        await service.RunCycleAsync(CancellationToken.None);

        commands.Should().HaveCount(2);
        commands[0].Cursor.Should().BeNull();
        commands[0].BlockedCursor.Should().BeNull();
        commands[1].Cursor.Should().Be(cursor);
        commands[1].BlockedCursor.Should().Be(blockedCursor);
    }

    private static Mock<IAgendamentoPresencaRepository> BaseRepository(AgendamentoPresenca schedule)
    {
        var repository = new Mock<IAgendamentoPresencaRepository>();
        repository.Setup(item => item.ListCandidatesAsync(
                It.IsAny<DateTimeOffset>(), It.IsAny<Guid?>(), 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => schedule.UltimaDataAvaliada < Today ? [schedule] : []);
        repository.Setup(item => item.GetProcessingCandidateAsync(schedule.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgendamentoPresencaProcessingCandidate(schedule, 1));
        repository.Setup(item => item.ListBlockedAsync(It.IsAny<DateTimeOffset>(), 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        repository.Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        return repository;
    }

    private static ProcessarAgendamentosPresencaDevidosCommandHandler CreateHandler(
        Mock<IAgendamentoPresencaRepository> repository,
        ISystemClock clock,
        IAgendamentoPresencaTimeZone? timezone = null,
        DiscordConfigurationDto? configuration = default,
        IDiscordConfigurationService? discord = null,
        IAgendamentoPresencaMetrics? metrics = null,
        IAgendamentoPresencaDiagnostics? diagnostics = null,
        AgendamentoPresencaProcessingOptions? options = null)
    {
        if (discord is null)
        {
            var discordMock = new Mock<IDiscordConfigurationService>();
            discordMock.Setup(item => item.GetAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(configuration ?? ValidConfiguration());
            discord = discordMock.Object;
        }

        return new ProcessarAgendamentosPresencaDevidosCommandHandler(
            repository.Object,
            timezone ?? new FixedTimeZone(),
            discord,
            metrics ?? Mock.Of<IAgendamentoPresencaMetrics>(),
            diagnostics ?? Mock.Of<IAgendamentoPresencaDiagnostics>(),
            clock,
            options ?? new AgendamentoPresencaProcessingOptions
            {
                MaxBlockedPerCycle = 10,
                MaxSchedulesPerCycle = 10,
                MaxDatesPerSchedulePerCycle = 10
            });
    }

    private static AgendamentoPresenca CreateSchedule(DateOnly marker) => new(
        "Agenda", "Observacao", new TimeOnly(18, 0), new TimeOnly(20, 0), [DiaSemanaIso.Sexta],
        marker, UserId, Publication.AddDays(-1));

    private static DiscordConfigurationDto ValidConfiguration() => new(
        Guid.NewGuid(), "guild", "presence", "news", "admin", "draft", "results", true);

    private sealed class FixedTimeZone : IAgendamentoPresencaTimeZone
    {
        public DateOnly GetLocalDate(DateTimeOffset instant) => DateOnly.FromDateTime(instant.AddHours(-3).DateTime);
        public DateTimeOffset ToUtc(DateOnly date, TimeOnly time) => new(date.ToDateTime(time).AddHours(3), TimeSpan.Zero);
    }

    private sealed class InvalidTimeZone : IAgendamentoPresencaTimeZone
    {
        public DateOnly GetLocalDate(DateTimeOffset instant) => Today;
        public DateTimeOffset ToUtc(DateOnly date, TimeOnly time) =>
            throw new DomainException(MessageCodes.PresenceScheduleTimeZoneInvalid);
    }

    private sealed record ConstantClock(DateTimeOffset UtcNow) : ISystemClock;

    private sealed class SequenceClock(params DateTimeOffset[] values) : ISystemClock
    {
        private int index;
        public DateTimeOffset UtcNow => values[Math.Min(Interlocked.Increment(ref index) - 1, values.Length - 1)];
    }
}
