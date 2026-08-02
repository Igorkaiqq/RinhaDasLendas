using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RinhaDasLendas.Api.Filters;
using RinhaDasLendas.Api.Hubs;
using RinhaDasLendas.Api.Services;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Infrastructure.Persistence;
using RinhaDasLendas.Tests.Infrastructure;

namespace RinhaDasLendas.Tests.Integration;

public sealed class DraftMontagensHubAuthorizationIntegrationTests
{
    [Fact]
    public async Task RealtimeDeveDistinguir401DeHandshake403DePoliticaE404DeRecursoOculto()
    {
        await using var factory = new DraftMontagemCycleApiFactory();
        var active = await factory.SeedV2PresenceDraftAsync();
        var archived = await factory.SeedV2PresenceDraftAsync();
        await ArchiveAsync(factory, archived.DraftId, archived.AdminUserId);
        using var anonymous = factory.CreateAnonymousClient();
        using var player = factory.CreateRoleClient(active.Players[0].UserId, AuthRoles.Jogador);

        (await anonymous.GetAsync($"/api/v1/draft-montagens/{active.DraftId}/realtime-state")).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);
        (await player.GetAsync($"/api/v1/draft-montagens/{active.DraftId}/administracao")).StatusCode
            .Should().Be(HttpStatusCode.Forbidden);
        (await player.GetAsync($"/api/v1/draft-montagens/{archived.DraftId}/realtime-state")).StatusCode
            .Should().Be(HttpStatusCode.NotFound);
        (await player.GetAsync($"/api/v1/draft-montagens/{Guid.NewGuid()}/realtime-state")).StatusCode
            .Should().Be(HttpStatusCode.NotFound);

        await using var anonymousHub = CreateHubConnection(factory, "pt-BR");
        var handshake = () => anonymousHub.StartAsync();
        var exception = await handshake.Should().ThrowAsync<HttpRequestException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("pt-BR", "Este draft não está disponível para sincronização em tempo real")]
    [InlineData("en-US", "This draft is not available for real-time synchronization")]
    public async Task GetEJoin_DevemCompartilharAutorizacaoERejeicaoSemAssociarNegadosAoGrupo(
        string culture,
        string expectedMessage)
    {
        await using var factory = new DraftMontagemCycleApiFactory();
        var active = await factory.SeedV2PresenceDraftAsync();
        var archived = await factory.SeedV2PresenceDraftAsync();
        await ArchiveAsync(factory, archived.DraftId, archived.AdminUserId);
        var missingId = Guid.NewGuid();

        using var humanHttp = factory.CreateRoleClient(active.AdminUserId, AuthRoles.Jogador);
        using var botHttp = factory.CreateBotClient();
        using var noUserIdHttp = factory.CreateRoleClient(null, AuthRoles.Jogador);
        SetCulture(culture, humanHttp, botHttp, noUserIdHttp);

        var allowedHttp = await humanHttp.GetAsync($"/api/v1/draft-montagens/{active.DraftId}/realtime-state");
        allowedHttp.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var json = JsonDocument.Parse(await allowedHttp.Content.ReadAsStringAsync()))
        {
            json.RootElement.TryGetProperty("montagem", out _).Should().BeTrue();
            json.RootElement.TryGetProperty("serverNow", out _).Should().BeTrue();
            json.RootElement.TryGetProperty("canCurrentUserPick", out _).Should().BeTrue();
        }

        var deniedHttp = new[]
        {
            await botHttp.GetAsync($"/api/v1/draft-montagens/{active.DraftId}/realtime-state"),
            await noUserIdHttp.GetAsync($"/api/v1/draft-montagens/{active.DraftId}/realtime-state"),
            await humanHttp.GetAsync($"/api/v1/draft-montagens/{archived.DraftId}/realtime-state"),
            await humanHttp.GetAsync($"/api/v1/draft-montagens/{missingId}/realtime-state"),
        };

        foreach (var response in deniedHttp)
        {
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
            error.Should().NotBeNull();
            error!.Message.Should().Be(expectedMessage);
            error.MessageCode.Should().Be(MessageCodes.DraftRealtimeUnavailable);
            response.Dispose();
        }

        var humanToken = SecurityApiFactory.CreateJwt(active.AdminUserId, AuthRoles.Jogador);
        var noUserIdToken = SecurityApiFactory.CreateJwt(null, AuthRoles.Jogador);
        var allowedHub = CreateHubConnection(factory, culture, humanToken);
        var deniedHubs = new[]
        {
            (Connection: CreateHubConnection(factory, culture, bot: true), DraftId: active.DraftId),
            (Connection: CreateHubConnection(factory, culture, noUserIdToken), DraftId: active.DraftId),
            (Connection: CreateHubConnection(factory, culture, humanToken), DraftId: archived.DraftId),
            (Connection: CreateHubConnection(factory, culture, humanToken), DraftId: missingId),
        };

        try
        {
            await allowedHub.StartAsync();
            foreach (var denied in deniedHubs)
            {
                await denied.Connection.StartAsync();
            }

            await allowedHub.InvokeAsync("JoinDraftMontagem", active.DraftId);
            var rejectionMessages = new List<string>();
            foreach (var denied in deniedHubs)
            {
                var action = () => denied.Connection.InvokeAsync("JoinDraftMontagem", denied.DraftId);
                var exception = await action.Should().ThrowAsync<HubException>();
                rejectionMessages.Add(exception.Which.Message);
            }

            rejectionMessages.Should().OnlyContain(message => message == rejectionMessages[0]);
            rejectionMessages[0].Should().Contain(expectedMessage);

            var allowedProbe = new TaskCompletionSource<Guid>(TaskCreationOptions.RunContinuationsAsynchronously);
            allowedHub.On<Guid>("AuthorizationProbe", draftId => allowedProbe.TrySetResult(draftId));
            var deniedProbes = new ConcurrentBag<Guid>();
            var barriers = deniedHubs.ToDictionary(
                denied => denied.Connection.ConnectionId!,
                denied => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
            foreach (var denied in deniedHubs)
            {
                denied.Connection.On<Guid>("AuthorizationProbe", deniedProbes.Add);
                denied.Connection.On("AuthorizationBarrier", () => barriers[denied.Connection.ConnectionId!].TrySetResult());
            }

            var hubContext = factory.Services.GetRequiredService<IHubContext<DraftMontagensHub>>();
            foreach (var draftId in deniedHubs.Select(item => item.DraftId).Distinct())
            {
                await hubContext.Clients.Group(DraftMontagensHub.GroupName(draftId)).SendAsync("AuthorizationProbe", draftId);
            }

            foreach (var denied in deniedHubs)
            {
                await hubContext.Clients.Client(denied.Connection.ConnectionId!).SendAsync("AuthorizationBarrier");
            }

            (await allowedProbe.Task.WaitAsync(TimeSpan.FromSeconds(5))).Should().Be(active.DraftId);
            await Task.WhenAll(barriers.Values.Select(barrier => barrier.Task)).WaitAsync(TimeSpan.FromSeconds(5));
            deniedProbes.Should().BeEmpty();
        }
        finally
        {
            await allowedHub.DisposeAsync();
            foreach (var denied in deniedHubs)
            {
                await denied.Connection.DisposeAsync();
            }
        }
    }

    private static HubConnection CreateHubConnection(
        DraftMontagemCycleApiFactory factory,
        string culture,
        string? accessToken = null,
        bool bot = false)
    {
        return new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, "/hubs/draft-montagens"), options =>
            {
                options.Transports = HttpTransportType.LongPolling;
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.Headers["Accept-Language"] = culture;
                if (accessToken is not null)
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
                }

                if (bot)
                {
                    options.Headers[BotInternalAuthOptions.HeaderName] = SecurityApiFactory.BotToken;
                }
            })
            .Build();
    }

    private static async Task ArchiveAsync(DraftMontagemCycleApiFactory factory, Guid draftId, Guid userId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
        var draft = await db.DraftMontagens.SingleAsync(item => item.Id == draftId);
        draft.Arquivar("Motivo de teste", userId, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync();
    }

    private static void SetCulture(string culture, params HttpClient[] clients)
    {
        foreach (var client in clients)
        {
            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(culture);
        }
    }
}
