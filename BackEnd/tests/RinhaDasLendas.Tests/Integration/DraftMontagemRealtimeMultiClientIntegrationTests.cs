using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using RinhaDasLendas.Api.Filters;
using RinhaDasLendas.Api.Observability;
using RinhaDasLendas.Api.Services;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Infrastructure.Identity;
using RinhaDasLendas.Infrastructure.Persistence;
using RinhaDasLendas.Tests.Infrastructure;

namespace RinhaDasLendas.Tests.Integration;

public sealed class DraftMontagemRealtimeMultiClientIntegrationTests
{
    [Fact]
    public async Task CicloCompletoDevePersistirEConvergirDoisClientesComCapacidadePersonalizadaESilencioEmNoOp()
    {
        await using var factory = new DraftMontagemCycleApiFactory(useRealPublisher: true);
        var fixture = await factory.SeedV2PresenceDraftAsync();
        await using var firstHub = CreateHubConnection(factory, fixture.Players[0].UserId, AuthRoles.Capitao);
        await using var secondHub = CreateHubConnection(factory, fixture.Players[1].UserId, AuthRoles.Capitao);
        var firstEvents = new ConcurrentQueue<ReceivedSnapshot>();
        var secondEvents = new ConcurrentQueue<ReceivedSnapshot>();
        long firstEventCount = 0;
        long secondEventCount = 0;
        firstHub.On<DraftMontagemRealtimeSnapshotDto>("DraftMontagemStateUpdated", state =>
            firstEvents.Enqueue(new(state, Stopwatch.GetTimestamp(), Interlocked.Increment(ref firstEventCount))));
        secondHub.On<DraftMontagemRealtimeSnapshotDto>("DraftMontagemStateUpdated", state =>
            secondEvents.Enqueue(new(state, Stopwatch.GetTimestamp(), Interlocked.Increment(ref secondEventCount))));
        await Task.WhenAll(firstHub.StartAsync(), secondHub.StartAsync());
        await Task.WhenAll(
            firstHub.InvokeAsync("JoinDraftMontagem", fixture.DraftId),
            secondHub.InvokeAsync("JoinDraftMontagem", fixture.DraftId));
        using var admin = factory.CreateRoleClient(fixture.AdminUserId, AuthRoles.Admin);
        using var firstCaptain = factory.CreateRoleClient(fixture.Players[0].UserId, AuthRoles.Capitao);
        using var secondCaptain = factory.CreateRoleClient(fixture.Players[1].UserId, AuthRoles.Capitao);
        using var reserveCaptain = factory.CreateRoleClient(fixture.Players[4].UserId, AuthRoles.Capitao);

        await AssertHttpTransitionAsync(() => reserveCaptain.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/presencas/cancelar", new { }), false, false);
        await AssertHttpTransitionAsync(() => reserveCaptain.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/presencas/confirmar", new { Origem = "Web" }), false, false);

        var noOpBaseline = CaptureBaseline();
        var versionBeforeNoOp = (await factory.GetDraftAsync(fixture.DraftId)).VersaoEstado;
        (await reserveCaptain.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/presencas/confirmar", new { Origem = "Web" })).EnsureSuccessStatusCode();
        await Task.Delay(200);
        CaptureBaseline().Should().Be(noOpBaseline);
        factory.CommitRecorder.IsEmpty.Should().BeTrue();
        firstEvents.Should().BeEmpty();
        secondEvents.Should().BeEmpty();
        (await factory.GetDraftAsync(fixture.DraftId)).VersaoEstado.Should().Be(versionBeforeNoOp);

        using var bot = factory.CreateBotClient();
        await factory.ConfigurePendingPublicationAsync(fixture.DraftId, DraftMontagemPublicacaoDiscordTipo.Presenca);
        var claim = await ClaimPublicationAsync(DraftMontagemPublicacaoDiscordTipo.Presenca);
        await AssertPublicationHttpTransitionAsync(() => bot.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/discord/publicacao",
            new RegistrarPublicacaoDiscordDraftMontagemRequestDto("Presenca", claim, "guild", "channel", "message")));

        await factory.ConfigurePendingPublicationAsync(fixture.DraftId, DraftMontagemPublicacaoDiscordTipo.TimesDefinidos);
        var failedClaim = await ClaimPublicationAsync(DraftMontagemPublicacaoDiscordTipo.TimesDefinidos);
        await AssertPublicationHttpTransitionAsync(() => bot.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/discord/publicacao/falha",
            new RegistrarFalhaPublicacaoDiscordDraftMontagemRequestDto("TimesDefinidos", failedClaim, "guild", "channel", "failure")));
        await AssertHttpTransitionAsync(() => admin.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/discord/publicacoes/republicar",
            new RepublicarPublicacaoDiscordDraftMontagemRequestDto(DraftMontagemPublicacaoDiscordTipo.TimesDefinidos, "Reprocessar")), false, false);
        var reconciliationClaim = await ClaimPublicationAsync(DraftMontagemPublicacaoDiscordTipo.TimesDefinidos);
        await factory.ExpirePublicationClaimAsync(fixture.DraftId, DraftMontagemPublicacaoDiscordTipo.TimesDefinidos);
        var reconciliation = new DraftMontagemPublicationReconciliationService(
            factory.Services.GetRequiredService<IServiceScopeFactory>(),
            factory.Services.GetRequiredService<IConfiguration>(),
            NullLogger<DraftMontagemPublicationReconciliationService>.Instance);
        var reconciliationBaseline = CaptureBaseline();
        (await reconciliation.RunCycleAsync(CancellationToken.None)).Should().Be(1);
        var reconciledVersion = (await factory.GetDraftAsync(fixture.DraftId)).VersaoEstado;
        await AssertVersionAsync(reconciledVersion, false, false, reconciliationBaseline);
        reconciliationClaim.Should().NotBeEmpty();

        await AssertHttpTransitionAsync(() => admin.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/encerrar-presenca", new { ContinuarComMenosDez = true, TamanhoEquipe = 2 }), false, false);
        await AssertHttpTransitionAsync(() => admin.PatchAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/modo", new { Modo = nameof(DraftMontagemModo.TempoReal) }), false, false);
        await AssertHttpTransitionAsync(() => admin.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/capitaes", new { CapitaesIds = new[] { fixture.Players[0].PlayerId, fixture.Players[1].PlayerId } }), false, false);
        await AssertHttpTransitionAsync(() => admin.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/ordem-escolha",
            new { Modo = nameof(DraftMontagemOrdemEscolhaModo.Manual), CapitaesIds = new[] { fixture.Players[0].PlayerId, fixture.Players[1].PlayerId } }), false, false);
        await AssertHttpTransitionAsync(() => admin.PostAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/iniciar-tempo-real", null), true, false);

        await factory.ExpireCurrentTurnAsync(fixture.DraftId);
        var timeoutBaseline = CaptureBaseline();
        var timeoutState = (await factory.AdvanceCurrentTimeoutAsync(fixture.DraftId))!;
        await AssertVersionAsync(timeoutState.Montagem.VersaoEstado, false, true, timeoutBaseline);
        await AssertHttpTransitionAsync(() => secondCaptain.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/picks", new { JogadorId = fixture.Players[2].PlayerId }), true, false);

        var current = await factory.GetDraftWithGraphAsync(fixture.DraftId);
        var firstTeam = current.Times.Single(team => team.CapitaoId == fixture.Players[0].PlayerId);
        await AssertHttpTransitionAsync(() => admin.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/reservas/substituir",
            new
            {
                TimeId = firstTeam.Id,
                JogadorSaiuId = fixture.Players[0].PlayerId,
                ReservaEntrouId = fixture.Players[4].PlayerId,
                NovoCapitaoId = fixture.Players[4].PlayerId,
                Motivo = "Substituicao operacional",
            }), false, false);
        var finalVersion = await AssertHttpTransitionAsync(() => reserveCaptain.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/picks", new { JogadorId = fixture.Players[3].PlayerId }), false, false);
        (await factory.GetDraftAsync(fixture.DraftId)).Status.Should().Be(DraftMontagemStatus.Finalizada);

        var rejectedBefore = CaptureBaseline();
        var rejectedVersion = (await factory.GetDraftAsync(fixture.DraftId)).VersaoEstado;
        var rejected = await admin.PatchAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/modo", new { Modo = nameof(DraftMontagemModo.Manual) });
        ((int)rejected.StatusCode).Should().BeGreaterThanOrEqualTo(400);
        await Task.Delay(200);
        CaptureBaseline().Should().Be(rejectedBefore);
        factory.CommitRecorder.IsEmpty.Should().BeTrue();
        firstEvents.Should().BeEmpty();
        secondEvents.Should().BeEmpty();
        (await factory.GetDraftAsync(fixture.DraftId)).VersaoEstado.Should().Be(rejectedVersion);

        var archivedVersion = await AssertHttpTransitionAsync(() => admin.PatchAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/arquivar",
            new ArquivarDraftMontagemRequestDto("Arquivamento operacional", finalVersion)), false, false, false);
        await AssertHttpTransitionAsync(() => admin.PatchAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/restaurar",
            new RestaurarDraftMontagemRequestDto(archivedVersion)), false, false);

        factory.CommitRecorder.IsEmpty.Should().BeTrue();
        firstEvents.Should().BeEmpty();
        secondEvents.Should().BeEmpty();
        factory.CommitRecorder.TotalCount.Should().Be(Volatile.Read(ref firstEventCount));
        factory.CommitRecorder.TotalCount.Should().Be(Volatile.Read(ref secondEventCount));

        async Task<long> AssertHttpTransitionAsync(
            Func<Task<HttpResponseMessage>> send,
            bool firstCanPick,
            bool secondCanPick,
            bool personalizedStateAvailable = true)
        {
            var baseline = CaptureBaseline();
            using var response = await send();
            response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = json.RootElement;
            var version = root.TryGetProperty("montagem", out var montagemElement)
                ? montagemElement.GetProperty("versaoEstado").GetInt64()
                : root.GetProperty("versaoEstado").GetInt64();
            await AssertVersionAsync(version, firstCanPick, secondCanPick, baseline, personalizedStateAvailable);
            return version;
        }

        async Task<Guid> ClaimPublicationAsync(DraftMontagemPublicacaoDiscordTipo type)
        {
            var baseline = CaptureBaseline();
            using var response = await bot.PostAsJsonAsync(
                $"/api/v1/draft-montagens/{fixture.DraftId}/discord/publicacoes/claim",
                new AdquirirClaimPublicacaoDiscordDraftMontagemRequestDto(type.ToString()));
            response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
            var result = (await response.Content.ReadFromJsonAsync<ClaimPublicacaoDiscordResponseDto>())!;
            result.Adquirido.Should().BeTrue();
            var version = (await factory.GetDraftAsync(fixture.DraftId)).VersaoEstado;
            await AssertVersionAsync(version, false, false, baseline);
            return result.ClaimId!.Value;
        }

        async Task AssertPublicationHttpTransitionAsync(Func<Task<HttpResponseMessage>> send)
        {
            var baseline = CaptureBaseline();
            using var response = await send();
            response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
            var version = (await factory.GetDraftAsync(fixture.DraftId)).VersaoEstado;
            await AssertVersionAsync(version, false, false, baseline);
        }

        TransitionBaseline CaptureBaseline() => new(
            factory.CommitRecorder.TotalCount,
            Volatile.Read(ref firstEventCount),
            Volatile.Read(ref secondEventCount));

        async Task AssertVersionAsync(
            long version,
            bool firstCanPick,
            bool secondCanPick,
            TransitionBaseline baseline,
            bool personalizedStateAvailable = true)
        {
            await WaitForExactDeltasAsync(
                () => factory.CommitRecorder.TotalCount - baseline.PublisherCount,
                () => Volatile.Read(ref firstEventCount) - baseline.FirstClientCount,
                () => Volatile.Read(ref secondEventCount) - baseline.SecondClientCount);
            (factory.CommitRecorder.TotalCount - baseline.PublisherCount).Should().Be(1);
            (Volatile.Read(ref firstEventCount) - baseline.FirstClientCount).Should().Be(1);
            (Volatile.Read(ref secondEventCount) - baseline.SecondClientCount).Should().Be(1);
            factory.CommitRecorder.TryDequeue(out var commit).Should().BeTrue();
            firstEvents.TryDequeue(out var firstEvent).Should().BeTrue();
            secondEvents.TryDequeue(out var secondEvent).Should().BeTrue();
            commit.Should().NotBeNull();
            firstEvent.Should().NotBeNull();
            secondEvent.Should().NotBeNull();
            commit!.DraftId.Should().Be(fixture.DraftId);
            commit.Version.Should().Be(version);
            firstEvent!.State.Montagem.VersaoEstado.Should().Be(version);
            secondEvent!.State.Montagem.VersaoEstado.Should().Be(version);
            firstEvent.Ordinal.Should().Be(commit.Ordinal);
            secondEvent.Ordinal.Should().Be(commit.Ordinal);
            firstEvent.Timestamp.Should().BeGreaterThanOrEqualTo(commit.Timestamp);
            secondEvent.Timestamp.Should().BeGreaterThanOrEqualTo(commit.Timestamp);
            Stopwatch.GetElapsedTime(commit.Timestamp, firstEvent.Timestamp).Should().BeLessThanOrEqualTo(TimeSpan.FromSeconds(2));
            Stopwatch.GetElapsedTime(commit.Timestamp, secondEvent.Timestamp).Should().BeLessThanOrEqualTo(TimeSpan.FromSeconds(2));
            factory.CommitRecorder.IsEmpty.Should().BeTrue();
            firstEvents.Should().BeEmpty();
            secondEvents.Should().BeEmpty();
            (await factory.GetDraftAsync(fixture.DraftId)).VersaoEstado.Should().Be(version);
            if (personalizedStateAvailable)
            {
                await AssertPersonalizedStateAsync(firstCaptain, fixture.DraftId, version, firstCanPick);
                await AssertPersonalizedStateAsync(secondCaptain, fixture.DraftId, version, secondCanPick);
            }
        }
    }

    [Fact]
    public async Task DoisClientesDevemReceberSnapshotsCompartilhadosDeArquivoERestauroEmAteDoisSegundos()
    {
        await using var factory = new RealtimeApiFactory();
        var fixture = await factory.SeedDraftAsync();
        await using var first = CreateHubConnection(factory, fixture.FirstViewerId);
        await using var second = CreateHubConnection(factory, fixture.SecondViewerId);
        var firstSnapshots = new ConcurrentQueue<DraftMontagemRealtimeSnapshotDto>();
        var secondSnapshots = new ConcurrentQueue<DraftMontagemRealtimeSnapshotDto>();
        var archived = new ConcurrentBag<Guid>();
        var restored = new ConcurrentBag<Guid>();
        var archiveBarrier = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var restoreBarrier = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        RegisterHandlers(first, firstSnapshots, archived, restored, archiveBarrier, restoreBarrier);
        RegisterHandlers(second, secondSnapshots, archived, restored, archiveBarrier, restoreBarrier);
        await Task.WhenAll(first.StartAsync(), second.StartAsync());
        await Task.WhenAll(
            first.InvokeAsync("JoinDraftMontagem", fixture.DraftId),
            second.InvokeAsync("JoinDraftMontagem", fixture.DraftId));
        using var admin = factory.CreateJwtClient(fixture.AdminUserId, AuthRoles.Admin);

        var eventStopwatch = Stopwatch.StartNew();
        var archiveResponse = await admin.PatchAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/arquivar",
            new ArquivarDraftMontagemRequestDto("Operacao multicliente", fixture.InitialVersion));
        archiveResponse.StatusCode.Should().Be(HttpStatusCode.OK, await archiveResponse.Content.ReadAsStringAsync());
        var archiveResult = (await archiveResponse.Content.ReadFromJsonAsync<DraftMontagemArquivamentoResultadoDto>())!;
        await archiveBarrier.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var archiveElapsed = eventStopwatch.Elapsed;
        archiveElapsed.Should().BeLessThanOrEqualTo(TimeSpan.FromSeconds(2));

        eventStopwatch.Restart();
        var restoreResponse = await admin.PatchAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/restaurar",
            new RestaurarDraftMontagemRequestDto(archiveResult.VersaoEstado));
        restoreResponse.StatusCode.Should().Be(HttpStatusCode.OK, await restoreResponse.Content.ReadAsStringAsync());
        var restoreResult = (await restoreResponse.Content.ReadFromJsonAsync<DraftMontagemArquivamentoResultadoDto>())!;
        await restoreBarrier.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var restoreElapsed = eventStopwatch.Elapsed;
        restoreElapsed.Should().BeLessThanOrEqualTo(TimeSpan.FromSeconds(2));

        firstSnapshots.Select(item => item.Montagem.VersaoEstado).Should().Contain([archiveResult.VersaoEstado, restoreResult.VersaoEstado]);
        secondSnapshots.Select(item => item.Montagem.VersaoEstado).Should().Contain([archiveResult.VersaoEstado, restoreResult.VersaoEstado]);
        archived.Count(id => id == fixture.DraftId).Should().Be(2);
        restored.Count(id => id == fixture.DraftId).Should().Be(2);
        restoreResult.VersaoEstado.Should().BeGreaterThan(archiveResult.VersaoEstado);
        typeof(DraftMontagemRealtimeSnapshotDto).GetProperties().Select(property => property.Name)
            .Should().NotContain(nameof(DraftMontagemRealtimeStateDto.CanCurrentUserPick));
    }

    [Fact]
    public void MetricasRealtimeDevemUsarSomenteEventoEResultadoLimitados()
    {
        var measurements = new List<(string Name, Dictionary<string, object?> Tags)>();
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == "RinhaDasLendas.Api")
                {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, _, tags, _) =>
            measurements.Add((instrument.Name, Capture(tags))));
        listener.Start();
        using var provider = new ServiceCollection().AddMetrics().BuildServiceProvider();
        var metrics = new ApiMetrics(provider.GetRequiredService<IMeterFactory>());
        var telemetry = new DraftMontagemRealtimeTelemetry(
            NullLogger<DraftMontagemRealtimeTelemetry>.Instance,
            metrics);
        var draftId = Guid.NewGuid();

        telemetry.RecordFailure(draftId, 42, "unbounded-operation", 125, "SensitiveExceptionName");

        var failure = measurements.Single(item => item.Name == "rinha_draft_realtime_publication_failures_total");
        failure.Tags.Should().BeEquivalentTo(new Dictionary<string, object?>
        {
            ["event"] = "unknown",
            ["outcome"] = "failure",
        });
        measurements.SelectMany(item => item.Tags.Keys).Should().NotContain(["draft_id", "state_version", "operation", "failure_type"]);
        measurements.SelectMany(item => item.Tags.Values).Should().NotContain([draftId.ToString(), 42L, "unbounded-operation", "SensitiveExceptionName"]);
    }

    [Fact]
    public async Task RepositoriosDeCandidatosDevemExecutarPostgreSqlIdOnlyEElegivelSemJoins()
    {
        await using var factory = new DraftMontagemCycleApiFactory();
        var realtimeFixture = await factory.SeedV2PresenceDraftAsync();
        var presenceId = await factory.SeedExpiredPresenceCandidateAsync();
        using var admin = factory.CreateRoleClient(realtimeFixture.AdminUserId, AuthRoles.Admin);
        await DraftMontagemCycleIntegrationTests.PostAndReadAsync<DraftMontagemResponseDto>(
            admin,
            $"/api/v1/draft-montagens/{realtimeFixture.DraftId}/encerrar-presenca",
            new { ContinuarComMenosDez = true, TamanhoEquipe = 2 });
        await DraftMontagemCycleIntegrationTests.PatchAndReadAsync<DraftMontagemResponseDto>(
            admin,
            $"/api/v1/draft-montagens/{realtimeFixture.DraftId}/modo",
            new { Modo = nameof(DraftMontagemModo.TempoReal) });
        await DraftMontagemCycleIntegrationTests.PostAndReadAsync<DraftMontagemResponseDto>(
            admin,
            $"/api/v1/draft-montagens/{realtimeFixture.DraftId}/capitaes",
            new { CapitaesIds = new[] { realtimeFixture.Players[0].PlayerId, realtimeFixture.Players[1].PlayerId } });
        await DraftMontagemCycleIntegrationTests.PostAndReadAsync<DraftMontagemResponseDto>(
            admin,
            $"/api/v1/draft-montagens/{realtimeFixture.DraftId}/ordem-escolha",
            new
            {
                Modo = nameof(DraftMontagemOrdemEscolhaModo.Manual),
                CapitaesIds = new[] { realtimeFixture.Players[0].PlayerId, realtimeFixture.Players[1].PlayerId },
            });
        await DraftMontagemCycleIntegrationTests.PostAndReadAsync<DraftMontagemRealtimeStateDto>(
            admin,
            $"/api/v1/draft-montagens/{realtimeFixture.DraftId}/iniciar-tempo-real",
            null);

        var capture = await factory.ExecuteCandidateQueriesAsync(DateTimeOffset.UtcNow.AddHours(1));

        capture.Realtime.Select(item => item.Id).Should().Contain(realtimeFixture.DraftId);
        capture.Presence.Select(item => item.Id).Should().Contain(presenceId);
        capture.CommandTexts.Should().HaveCount(2);
        foreach (var sql in capture.CommandTexts)
        {
            var normalized = sql.ToUpperInvariant();
            normalized.Should().Contain("SELECT D.ID");
            normalized.Should().NotContain("JOIN");
            normalized.Should().NotContainAny(
                "DRAFT_MONTAGEM_TIMES",
                "DRAFT_MONTAGEM_PARTICIPANTES",
                "DRAFT_MONTAGEM_PRESENCAS",
                "DRAFT_MONTAGEM_ESCOLHAS",
                "DRAFT_MONTAGEM_SUBSTITUICOES",
                "DRAFT_MONTAGEM_PUBLICACOES_DISCORD",
                "DRAFT_MONTAGEM_ACOES_ADMINISTRATIVAS");
        }
    }

    [Theory]
    [InlineData((int)ControlledNotifierBehavior.Success)]
    [InlineData((int)ControlledNotifierBehavior.Failure)]
    [InlineData((int)ControlledNotifierBehavior.Timeout)]
    public async Task EndpointRealDevePersistirESucederMesmoComNotifierBemSucedidoFalhandoOuExpirando(
        int behaviorValue)
    {
        var behavior = (ControlledNotifierBehavior)behaviorValue;
        var notifier = new ControlledDraftMontagemRealtimeNotifier(behavior);
        await using var factory = new DraftMontagemCycleApiFactory(useRealPublisher: true, notifier);
        var fixture = await factory.SeedV2PresenceDraftAsync();
        using var player = factory.CreateRoleClient(fixture.Players[4].UserId, AuthRoles.Jogador);
        var stopwatch = Stopwatch.StartNew();

        using var response = await player.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/presencas/cancelar",
            new { });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await factory.IsPresenceConfirmedAsync(fixture.DraftId, fixture.Players[4].UserId)).Should().BeFalse();
        await notifier.Entered.WaitAsync(TimeSpan.FromSeconds(2));
        if (behavior == ControlledNotifierBehavior.Timeout)
        {
            stopwatch.Elapsed.Should().BeGreaterThanOrEqualTo(TimeSpan.FromSeconds(5));
            stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(7));
            notifier.PublicationToken.IsCancellationRequested.Should().BeTrue();
        }
    }

    [Fact]
    public async Task CancelamentoDaRequestAposCommitNaoDeveCancelarTokenDoPublisherNemReverterPersistencia()
    {
        var notifier = new ControlledDraftMontagemRealtimeNotifier(ControlledNotifierBehavior.Gate);
        await using var factory = new DraftMontagemCycleApiFactory(useRealPublisher: true, notifier);
        var fixture = await factory.SeedV2PresenceDraftAsync();
        using var player = factory.CreateRoleClient(fixture.Players[4].UserId, AuthRoles.Jogador);
        using var cancellation = new CancellationTokenSource();
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/draft-montagens/{fixture.DraftId}/presencas/cancelar")
        {
            Content = JsonContent.Create(new { }),
        };

        var sending = player.SendAsync(request, cancellation.Token);
        await notifier.Entered.WaitAsync(TimeSpan.FromSeconds(2));
        (await factory.IsPresenceConfirmedAsync(fixture.DraftId, fixture.Players[4].UserId)).Should().BeFalse();
        cancellation.Cancel();
        await Task.Delay(100);
        notifier.PublicationToken.IsCancellationRequested.Should().BeFalse();
        notifier.Release();
        try
        {
            using var response = await sending;
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        catch (OperationCanceledException)
        {
            cancellation.IsCancellationRequested.Should().BeTrue();
        }
        notifier.PublicationToken.IsCancellationRequested.Should().BeFalse();
    }

    private static void RegisterHandlers(
        HubConnection connection,
        ConcurrentQueue<DraftMontagemRealtimeSnapshotDto> snapshots,
        ConcurrentBag<Guid> archived,
        ConcurrentBag<Guid> restored,
        TaskCompletionSource archiveBarrier,
        TaskCompletionSource restoreBarrier)
    {
        connection.On<DraftMontagemRealtimeSnapshotDto>("DraftMontagemStateUpdated", snapshots.Enqueue);
        connection.On<Guid>("DraftMontagemArchived", id =>
        {
            archived.Add(id);
            if (archived.Count == 2) archiveBarrier.TrySetResult();
        });
        connection.On<Guid>("DraftMontagemRestored", id =>
        {
            restored.Add(id);
            if (restored.Count == 2) restoreBarrier.TrySetResult();
        });
    }

    private static HubConnection CreateHubConnection(SecurityApiFactory factory, Guid userId, params string[] roles) =>
        new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, "/hubs/draft-montagens"), options =>
            {
                options.Transports = HttpTransportType.LongPolling;
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.AccessTokenProvider = () => Task.FromResult<string?>(SecurityApiFactory.CreateJwt(userId, roles.Length == 0 ? [AuthRoles.Jogador] : roles));
            })
            .Build();

    private static async Task AssertPersonalizedStateAsync(HttpClient client, Guid draftId, long version, bool canPick)
    {
        var state = await client.GetFromJsonAsync<DraftMontagemRealtimeStateDto>($"/api/v1/draft-montagens/{draftId}/realtime-state");
        state.Should().NotBeNull();
        state!.Montagem.VersaoEstado.Should().Be(version);
        state.CanCurrentUserPick.Should().Be(canPick);
    }

    private static async Task WaitForExactDeltasAsync(
        Func<long> publisherDelta,
        Func<long> firstClientDelta,
        Func<long> secondClientDelta)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(2);
        while (DateTimeOffset.UtcNow < deadline)
        {
            var deltas = new[] { publisherDelta(), firstClientDelta(), secondClientDelta() };
            deltas.Should().OnlyContain(delta => delta <= 1, "duplicates must fail immediately");
            if (deltas.All(delta => delta == 1)) return;
            await Task.Delay(10);
        }
        throw new TimeoutException(
            $"Expected exact publisher/client deltas of one; observed {publisherDelta()}/{firstClientDelta()}/{secondClientDelta()}.");
    }

    private static Dictionary<string, object?> Capture(ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        var result = new Dictionary<string, object?>();
        foreach (var tag in tags) result[tag.Key] = tag.Value;
        return result;
    }

    private sealed class RealtimeApiFactory : SecurityApiFactory
    {
        public RealtimeApiFactory() : base(useIsolatedPostgreSql: true) { }

        public async Task<RealtimeFixture> SeedDraftAsync()
        {
            _ = CreateAnonymousClient();
            await using var scope = Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
            var adminUserId = Guid.NewGuid();
            db.Users.Add(new ApplicationUser
            {
                Id = adminUserId,
                Nome = "Operador multicliente",
                UserName = $"realtime-{adminUserId:N}",
                NormalizedUserName = $"REALTIME-{adminUserId:N}",
                Ativo = true,
            });
            var draft = new DraftMontagem("Draft multicliente", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
            db.DraftMontagens.Add(draft);
            await db.SaveChangesAsync();
            return new RealtimeFixture(draft.Id, draft.VersaoEstado, adminUserId, Guid.NewGuid(), Guid.NewGuid());
        }
    }

    private sealed record RealtimeFixture(
        Guid DraftId,
        long InitialVersion,
        Guid AdminUserId,
        Guid FirstViewerId,
        Guid SecondViewerId);

    private sealed record TransitionBaseline(long PublisherCount, long FirstClientCount, long SecondClientCount);

    private sealed record ReceivedSnapshot(DraftMontagemRealtimeSnapshotDto State, long Timestamp, long Ordinal);
}
