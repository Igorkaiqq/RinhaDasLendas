using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using RinhaDasLendas.Api.Filters;
using RinhaDasLendas.Api.Observability;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Infrastructure.Identity;
using RinhaDasLendas.Infrastructure.Persistence;
using RinhaDasLendas.Tests.Infrastructure;
using Xunit.Abstractions;

namespace RinhaDasLendas.Tests.Integration;

public sealed class DraftMontagemRealtimeMultiClientIntegrationTests(ITestOutputHelper output)
{
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
        output.WriteLine("Archive delivery: {0:F0} ms; restore delivery: {1:F0} ms", archiveElapsed.TotalMilliseconds, restoreElapsed.TotalMilliseconds);

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

    private static HubConnection CreateHubConnection(RealtimeApiFactory factory, Guid userId) =>
        new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, "/hubs/draft-montagens"), options =>
            {
                options.Transports = HttpTransportType.LongPolling;
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.AccessTokenProvider = () => Task.FromResult<string?>(SecurityApiFactory.CreateJwt(userId, AuthRoles.Jogador));
            })
            .Build();

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
}
