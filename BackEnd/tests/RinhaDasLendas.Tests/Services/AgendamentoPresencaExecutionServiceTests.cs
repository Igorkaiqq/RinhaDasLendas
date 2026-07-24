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

public sealed class AgendamentoPresencaExecutionServiceTests
{
    private static readonly Guid Responsavel = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly DateOnly Hoje = new(2026, 7, 24);
    private static readonly DateTimeOffset Publicacao = new(2026, 7, 24, 21, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Encerramento = new(2026, 7, 24, 23, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AntesDaPublicacao_DeveManterDataPendenteSemOcorrencia()
    {
        var repository = CreateRepository();
        var agenda = CreateAgenda(Hoje.AddDays(-1), Publicacao.AddDays(-1), [DiaSemanaIso.Sexta]);
        SetupCandidates(repository, agenda);
        var handler = CreateHandler(repository, Publicacao.AddMinutes(-1));

        var result = await handler.Handle(new(Publicacao.AddMinutes(-1)), CancellationToken.None);

        result.Should().Be(new AgendamentoPresencaCycleResult(1, 0, 0, 0, 0));
        agenda.UltimaDataAvaliada.Should().Be(Hoje.AddDays(-1));
        repository.Verify(item => item.TryClaimOccurrenceAsync(
            It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(),
            It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExatamenteNaPublicacao_DeveCriarDraftEPublicacaoAtomicamenteComClaimDeCincoMinutos()
    {
        var repository = CreateRepository();
        var agenda = CreateAgenda(Hoje.AddDays(-1), Publicacao, [DiaSemanaIso.Sexta]);
        DraftMontagem? draft = null;
        SetupCandidates(repository, agenda);
        repository.Setup(item => item.TryClaimOccurrenceAsync(
                agenda.Id, Hoje, Publicacao, Encerramento, It.IsAny<Guid>(), Publicacao.AddMinutes(5), Publicacao,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid agendaId, DateOnly date, DateTimeOffset publication, DateTimeOffset closure,
                Guid claimId, DateTimeOffset expiresAt, DateTimeOffset now, CancellationToken _) =>
                new AgendamentoPresencaOcorrenciaClaim(
                    Guid.NewGuid(), claimId, true, OcorrenciaAgendamentoPresencaStatus.Processando,
                    agenda.Nome, agenda.Observacao));
        repository.Setup(item => item.TryCompleteWithDraftAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DraftMontagem>(), Publicacao, It.IsAny<CancellationToken>()))
            .Callback<Guid, Guid, DraftMontagem, DateTimeOffset, CancellationToken>((_, _, value, _, _) => draft = value)
            .ReturnsAsync(true);
        var handler = CreateHandler(repository, Publicacao);

        var result = await handler.Handle(new(Publicacao), CancellationToken.None);

        result.Should().Be(new AgendamentoPresencaCycleResult(1, 1, 0, 0, 0));
        agenda.UltimaDataAvaliada.Should().Be(Hoje);
        draft.Should().NotBeNull();
        draft!.Nome.Should().Be("Agenda semanal - 24/07/2026");
        draft.HorarioEncerramentoPresenca.Should().Be(Encerramento);
        draft.DiscordGuildId.Should().Be("guild-1");
    }

    [Fact]
    public async Task ConfiguracaoAusente_DeveBloquearSemAdquirirClaimOuCriarDraft()
    {
        var repository = CreateRepository();
        var agenda = CreateAgenda(Hoje.AddDays(-1), Publicacao, [DiaSemanaIso.Sexta]);
        SetupCandidates(repository, agenda);
        repository.Setup(item => item.TryUpsertBlockedOccurrenceAsync(
                agenda.Id, Hoje, Publicacao, Encerramento, MessageCodes.PresenceScheduleDiscordUnavailable,
                Publicacao, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgendamentoPresencaOccurrenceWriteResult(OcorrenciaAgendamentoPresencaStatus.Bloqueada, true));
        var handler = CreateHandler(repository, Publicacao, configurationAvailable: false);

        var result = await handler.Handle(new(Publicacao), CancellationToken.None);

        result.Should().Be(new AgendamentoPresencaCycleResult(1, 0, 1, 0, 0));
        agenda.UltimaDataAvaliada.Should().Be(Hoje);
        repository.Verify(item => item.TryClaimOccurrenceAsync(
            It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(),
            It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(item => item.TryCompleteWithDraftAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DraftMontagem>(), It.IsAny<DateTimeOffset>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BloqueadaComMarcadorAvancado_DeveSerReadquiridaQuandoConfiguracaoVoltar()
    {
        var repository = CreateRepository();
        var agenda = CreateAgenda(Hoje, Publicacao.AddDays(-1), [DiaSemanaIso.Sexta]);
        var occurrence = OcorrenciaAgendamentoPresenca.Bloqueada(
            agenda.Id, Hoje, Publicacao, Encerramento, MessageCodes.PresenceScheduleDiscordUnavailable, Publicacao);
        repository.Setup(item => item.ListBlockedAsync(Publicacao.AddMinutes(30), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([occurrence]);
        repository.Setup(item => item.GetByIdAsync(agenda.Id, false, It.IsAny<CancellationToken>())).ReturnsAsync(agenda);
        repository.Setup(item => item.TryClaimOccurrenceAsync(
                agenda.Id, Hoje, Publicacao, Encerramento, It.IsAny<Guid>(), Publicacao.AddMinutes(35),
                Publicacao.AddMinutes(30), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid agendaId, DateOnly date, DateTimeOffset publication, DateTimeOffset closure,
                Guid claimId, DateTimeOffset expiresAt, DateTimeOffset now, CancellationToken _) =>
                new AgendamentoPresencaOcorrenciaClaim(occurrence.Id, claimId, true));
        repository.Setup(item => item.TryCompleteWithDraftAsync(
                occurrence.Id, It.IsAny<Guid>(), It.IsAny<DraftMontagem>(), Publicacao.AddMinutes(30),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var handler = CreateHandler(repository, Publicacao.AddMinutes(30));

        var result = await handler.Handle(new(Publicacao.AddMinutes(30)), CancellationToken.None);

        result.Criadas.Should().Be(1);
        repository.Verify(item => item.ListBlockedAsync(Publicacao.AddMinutes(30), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(item => item.ListCandidatesAsync(Hoje, It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BloqueadaDevePermanecerBloqueadaSemConfiguracaoESerPerdidaAposEncerramento()
    {
        var repository = CreateRepository();
        var agenda = CreateAgenda(Hoje, Publicacao.AddDays(-1), [DiaSemanaIso.Sexta]);
        var occurrence = OcorrenciaAgendamentoPresenca.Bloqueada(
            agenda.Id, Hoje, Publicacao, Encerramento, MessageCodes.PresenceScheduleDiscordUnavailable, Publicacao);
        repository.Setup(item => item.ListBlockedAsync(Publicacao.AddMinutes(30), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([occurrence]);
        repository.Setup(item => item.TryUpsertBlockedOccurrenceAsync(
                agenda.Id, Hoje, Publicacao, Encerramento, MessageCodes.PresenceScheduleDiscordUnavailable,
                Publicacao.AddMinutes(30), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgendamentoPresencaOccurrenceWriteResult(OcorrenciaAgendamentoPresencaStatus.Bloqueada, true));
        var blocked = await CreateHandler(repository, Publicacao.AddMinutes(30), configurationAvailable: false)
            .Handle(new(Publicacao.AddMinutes(30)), CancellationToken.None);

        repository.Setup(item => item.ListBlockedAsync(Encerramento, It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync([occurrence]);
        repository.Setup(item => item.TryUpsertMissedOccurrenceAsync(
                agenda.Id, Hoje, Publicacao, Encerramento, MessageCodes.PresenceScheduleWindowExpired,
                Encerramento, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgendamentoPresencaOccurrenceWriteResult(OcorrenciaAgendamentoPresencaStatus.Perdida, true));
        var missed = await CreateHandler(repository, Encerramento, configurationAvailable: false)
            .Handle(new(Encerramento), CancellationToken.None);

        blocked.Bloqueadas.Should().Be(0);
        missed.Perdidas.Should().Be(1);
    }

    [Fact]
    public async Task ReinicioAposTresDias_DeveClassificarTodasAsDatasSemHorizonteEAvancarMonotonicamente()
    {
        var now = new DateTimeOffset(2026, 7, 24, 23, 30, 0, TimeSpan.Zero);
        var repository = CreateRepository();
        var agenda = new AgendamentoPresenca(
            "Agenda semanal", null, new TimeOnly(15, 0), new TimeOnly(17, 0),
            [DiaSemanaIso.Segunda, DiaSemanaIso.Terca, DiaSemanaIso.Quarta],
            new DateOnly(2026, 7, 19), Responsavel, new DateTimeOffset(2026, 7, 19, 18, 0, 0, TimeSpan.Zero));
        SetupCandidates(repository, agenda);
        repository.Setup(item => item.TryUpsertMissedOccurrenceAsync(
                agenda.Id, It.IsAny<DateOnly>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(),
                MessageCodes.PresenceScheduleWindowExpired, now, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgendamentoPresencaOccurrenceWriteResult(OcorrenciaAgendamentoPresencaStatus.Perdida, true));
        var handler = CreateHandler(repository, now);

        var result = await handler.Handle(new(now), CancellationToken.None);

        result.Perdidas.Should().Be(3);
        agenda.UltimaDataAvaliada.Should().Be(Hoje);
        repository.Verify(item => item.TryUpsertMissedOccurrenceAsync(
            agenda.Id, It.IsAny<DateOnly>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(),
            MessageCodes.PresenceScheduleWindowExpired, now, It.IsAny<CancellationToken>()), Times.Exactly(3));
        repository.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(5));
    }

    [Fact]
    public async Task RestartAposCommitDaPerdaAntesDoMarcador_DeveReconhecerTerminalEAvancar()
    {
        var now = Encerramento;
        var repository = CreateRepository();
        var agenda = CreateAgenda(Hoje.AddDays(-1), Publicacao, [DiaSemanaIso.Sexta]);
        var terminal = OcorrenciaAgendamentoPresenca.Bloqueada(
            agenda.Id, Hoje, Publicacao, Encerramento, MessageCodes.PresenceScheduleDiscordUnavailable, Publicacao);
        terminal.MarcarPerdida(MessageCodes.PresenceScheduleWindowExpired, Encerramento);
        SetupCandidates(repository, agenda);
        repository.Setup(item => item.TryUpsertMissedOccurrenceAsync(
                agenda.Id, Hoje, Publicacao, Encerramento, MessageCodes.PresenceScheduleWindowExpired,
                now, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgendamentoPresencaOccurrenceWriteResult(OcorrenciaAgendamentoPresencaStatus.Perdida, false));
        repository.Setup(item => item.GetOccurrenceAsync(agenda.Id, Hoje, It.IsAny<CancellationToken>()))
            .ReturnsAsync(terminal);

        await CreateHandler(repository, now).Handle(new(now), CancellationToken.None);

        agenda.UltimaDataAvaliada.Should().Be(Hoje);
    }

    [Fact]
    public async Task AtivacaoPosteriorDeveBloquearSomenteODiaSemCriarOcorrencia()
    {
        var repository = CreateRepository();
        var agenda = CreateAgenda(Hoje.AddDays(-1), Publicacao.AddSeconds(1), [DiaSemanaIso.Sexta]);
        SetupCandidates(repository, agenda);
        var handler = CreateHandler(repository, Publicacao.AddMinutes(30));

        var result = await handler.Handle(new(Publicacao.AddMinutes(30)), CancellationToken.None);

        result.Should().Be(new AgendamentoPresencaCycleResult(1, 0, 0, 0, 0));
        agenda.UltimaDataAvaliada.Should().Be(Hoje);
        repository.Verify(item => item.TryUpsertMissedOccurrenceAsync(
            It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(),
            It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TimezoneInvalido_DevePersistirMV096SemDraftEAvancarMarcador()
    {
        var repository = CreateRepository();
        var metrics = new Mock<IAgendamentoPresencaMetrics>();
        var agenda = CreateAgenda(Hoje.AddDays(-1), Publicacao.AddDays(-1), [DiaSemanaIso.Sexta]);
        SetupCandidates(repository, agenda);
        repository.Setup(item => item.TryUpsertFailedTimeZoneOccurrenceAsync(
                agenda.Id, Hoje, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgendamentoPresencaOccurrenceWriteResult(
                OcorrenciaAgendamentoPresencaStatus.Falha, true));
        var handler = CreateHandler(repository, Publicacao, metrics: metrics, timezone: new InvalidTimeZone());

        var result = await handler.Handle(new(Publicacao), CancellationToken.None);

        result.Falhas.Should().Be(1);
        agenda.UltimaDataAvaliada.Should().Be(Hoje);
        metrics.Verify(item => item.RecordFailure(MessageCodes.PresenceScheduleTimeZoneInvalid), Times.Once);
        repository.Verify(item => item.TryCompleteWithDraftAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DraftMontagem>(), It.IsAny<DateTimeOffset>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FalhaDeUmaAgendaNaoDeveInterromperAsOutras()
    {
        var repository = CreateRepository();
        var first = CreateAgenda(Hoje.AddDays(-1), Publicacao, [DiaSemanaIso.Sexta], "Agenda A");
        var second = CreateAgenda(Hoje.AddDays(-1), Publicacao, [DiaSemanaIso.Sexta], "Agenda B");
        SetupCandidates(repository, first, second);
        repository.Setup(item => item.TryClaimOccurrenceAsync(
                first.Id, It.IsAny<DateOnly>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<Guid>(),
                It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("technical failure"));
        repository.Setup(item => item.TryClaimOccurrenceAsync(
                second.Id, Hoje, Publicacao, Encerramento, It.IsAny<Guid>(), Publicacao.AddMinutes(5), Publicacao,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid agendaId, DateOnly date, DateTimeOffset publication, DateTimeOffset closure,
                Guid claimId, DateTimeOffset expiresAt, DateTimeOffset now, CancellationToken _) =>
                new AgendamentoPresencaOcorrenciaClaim(
                    Guid.NewGuid(), claimId, true, OcorrenciaAgendamentoPresencaStatus.Processando,
                    second.Nome, second.Observacao));
        repository.Setup(item => item.TryCompleteWithDraftAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DraftMontagem>(), Publicacao, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var handler = CreateHandler(repository, Publicacao);

        var result = await handler.Handle(new(Publicacao), CancellationToken.None);

        result.Falhas.Should().Be(1);
        result.Criadas.Should().Be(1);
        first.UltimaDataAvaliada.Should().Be(Hoje.AddDays(-1));
        second.UltimaDataAvaliada.Should().Be(Hoje);
    }

    [Fact]
    public async Task ClaimJaAdquiridoOuTerminal_DeveEvitarDuplicacaoERegistrarConflito()
    {
        var repository = CreateRepository();
        var metrics = new Mock<IAgendamentoPresencaMetrics>();
        var agenda = CreateAgenda(Hoje.AddDays(-1), Publicacao, [DiaSemanaIso.Sexta]);
        SetupCandidates(repository, agenda);
        repository.Setup(item => item.TryClaimOccurrenceAsync(
                agenda.Id, Hoje, Publicacao, Encerramento, It.IsAny<Guid>(), Publicacao.AddMinutes(5), Publicacao,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgendamentoPresencaOcorrenciaClaim(
                Guid.NewGuid(), Guid.NewGuid(), false, OcorrenciaAgendamentoPresencaStatus.Criada));
        var handler = CreateHandler(repository, Publicacao, metrics: metrics);

        var result = await handler.Handle(new(Publicacao), CancellationToken.None);

        result.Criadas.Should().Be(0);
        agenda.UltimaDataAvaliada.Should().Be(Hoje);
        metrics.Verify(item => item.RecordConflict(MessageCodes.PresenceScheduleOccurrenceConflict), Times.Once);
    }

    [Fact]
    public async Task ClaimEmProcessamentoNaoDeveAvancarMarcadorParaPermitirRetomadaAposCrash()
    {
        var repository = CreateRepository();
        var agenda = CreateAgenda(Hoje.AddDays(-1), Publicacao, [DiaSemanaIso.Sexta]);
        SetupCandidates(repository, agenda);
        repository.Setup(item => item.TryClaimOccurrenceAsync(
                agenda.Id, Hoje, Publicacao, Encerramento, It.IsAny<Guid>(), Publicacao.AddMinutes(5), Publicacao,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgendamentoPresencaOcorrenciaClaim(
                Guid.NewGuid(), Guid.NewGuid(), false, OcorrenciaAgendamentoPresencaStatus.Processando));

        await CreateHandler(repository, Publicacao).Handle(new(Publicacao), CancellationToken.None);

        agenda.UltimaDataAvaliada.Should().Be(Hoje.AddDays(-1));
    }

    [Fact]
    public async Task RunCycleAsync_DeveUsarRelogioECriarEscopoMesmoSemConfiguracaoDeIntervalo()
    {
        var expected = new AgendamentoPresencaCycleResult(2, 1, 0, 1, 0);
        var sender = new Mock<ISender>();
        sender.Setup(item => item.Send(
                It.Is<ProcessarAgendamentosPresencaDevidosCommand>(command => command.Agora == Publicacao),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        using var provider = new ServiceCollection().AddScoped(_ => sender.Object).BuildServiceProvider();
        var service = new AgendamentoPresencaExecutionService(
            provider.GetRequiredService<IServiceScopeFactory>(), new TestClock(Publicacao),
            new ConfigurationBuilder().Build(), NullLogger<AgendamentoPresencaExecutionService>.Instance);

        var result = await service.RunCycleAsync(CancellationToken.None);

        result.Should().Be(expected);
        sender.VerifyAll();
    }

    [Fact]
    public async Task RunCycleAsync_DeveSerializarChamadasConcorrentesERespeitarCancelamento()
    {
        var active = 0;
        var maxActive = 0;
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var sender = new Mock<ISender>();
        sender.Setup(item => item.Send(It.IsAny<ProcessarAgendamentosPresencaDevidosCommand>(), It.IsAny<CancellationToken>()))
            .Returns<ProcessarAgendamentosPresencaDevidosCommand, CancellationToken>(async (_, ct) =>
            {
                var current = Interlocked.Increment(ref active);
                maxActive = Math.Max(maxActive, current);
                entered.TrySetResult();
                await release.Task.WaitAsync(ct);
                Interlocked.Decrement(ref active);
                return new AgendamentoPresencaCycleResult(0, 0, 0, 0, 0);
            });
        using var provider = new ServiceCollection().AddScoped(_ => sender.Object).BuildServiceProvider();
        var service = new AgendamentoPresencaExecutionService(
            provider.GetRequiredService<IServiceScopeFactory>(), new TestClock(Publicacao),
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PresenceSchedule:IntervalSeconds"] = "1"
            }).Build(), NullLogger<AgendamentoPresencaExecutionService>.Instance);
        using var cancellation = new CancellationTokenSource();

        var first = service.RunCycleAsync(cancellation.Token);
        await entered.Task;
        var second = service.RunCycleAsync(cancellation.Token);
        await Task.Delay(50);
        maxActive.Should().Be(1);

        cancellation.Cancel();
        await first.Invoking(task => task).Should().ThrowAsync<OperationCanceledException>();
        await second.Invoking(task => task).Should().ThrowAsync<OperationCanceledException>();
        release.TrySetResult();
    }

    private static Mock<IAgendamentoPresencaRepository> CreateRepository()
    {
        var repository = new Mock<IAgendamentoPresencaRepository>();
        repository.Setup(item => item.ListCandidatesAsync(It.IsAny<DateOnly>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        repository.Setup(item => item.ListBlockedAsync(It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        repository.Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        return repository;
    }

    private static void SetupCandidates(
        Mock<IAgendamentoPresencaRepository> repository,
        params AgendamentoPresenca[] schedules)
    {
        repository.Setup(item => item.ListCandidatesAsync(
                It.IsAny<DateOnly>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedules);
        foreach (var schedule in schedules)
        {
            repository.Setup(item => item.GetByIdAsync(schedule.Id, true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(schedule);
        }
    }

    private static ProcessarAgendamentosPresencaDevidosCommandHandler CreateHandler(
        Mock<IAgendamentoPresencaRepository> repository,
        DateTimeOffset now,
        bool configurationAvailable = true,
        Mock<IAgendamentoPresencaMetrics>? metrics = null,
        IAgendamentoPresencaTimeZone? timezone = null)
    {
        var discord = new Mock<IDiscordConfigurationService>();
        discord.Setup(item => item.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(configurationAvailable ? ValidConfiguration() : null);

        return new ProcessarAgendamentosPresencaDevidosCommandHandler(
            repository.Object,
            timezone ?? new FixedTimeZone(),
            discord.Object,
            (metrics ?? new Mock<IAgendamentoPresencaMetrics>()).Object,
            Mock.Of<IAgendamentoPresencaDiagnostics>(),
            new TestClock(now),
            new AgendamentoPresencaProcessingOptions());
    }

    private static DiscordConfigurationDto ValidConfiguration() => new(
        Guid.NewGuid(), "guild-1", "presence-1", "news-1", "admin-1", "draft-1", "results-1", true);

    private static AgendamentoPresenca CreateAgenda(
        DateOnly marker,
        DateTimeOffset activatedAt,
        IReadOnlyCollection<DiaSemanaIso> days,
        string name = "Agenda semanal") => new(
        name, null, new TimeOnly(18, 0), new TimeOnly(20, 0), days, marker, Responsavel, activatedAt);

    private sealed class FixedTimeZone : IAgendamentoPresencaTimeZone
    {
        public DateOnly GetLocalDate(DateTimeOffset instant) => DateOnly.FromDateTime(instant.AddHours(-3).DateTime);

        public DateTimeOffset ToUtc(DateOnly date, TimeOnly time) =>
            new(date.ToDateTime(time).AddHours(3), TimeSpan.Zero);
    }

    private sealed class InvalidTimeZone : IAgendamentoPresencaTimeZone
    {
        public DateOnly GetLocalDate(DateTimeOffset instant) => Hoje;

        public DateTimeOffset ToUtc(DateOnly date, TimeOnly time) =>
            throw new DomainException(MessageCodes.PresenceScheduleTimeZoneInvalid);
    }

    private sealed record TestClock(DateTimeOffset UtcNow) : ISystemClock;
}
