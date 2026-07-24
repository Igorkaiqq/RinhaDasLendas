using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RinhaDasLendas.Api.Filters;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Infrastructure.Messages;
using RinhaDasLendas.Infrastructure.Persistence;
using RinhaDasLendas.Tests.Infrastructure;

namespace RinhaDasLendas.Tests.Integration;

public sealed class EndpointCoverageIntegrationTests
{
    private static readonly ResourceMessageProvider Messages = new();
    private readonly List<string> _errors = [];

    [Fact]
    public async Task PresenceScheduleEndpoints_ShouldEnforcePermissionMatrixAndTrustedAuthorship()
    {
        await using var factory = new PostgreSqlApiFactory();
        var userId = factory.GetExistingUserId();
        using var anonymous = factory.CreateAnonymousClient();
        using var player = factory.CreatePresenceSchedulePlayerClient(userId);
        using var moderator = factory.CreatePresenceScheduleModeratorClient(userId);
        using var admin = factory.CreateAdminClient();
        const string route = "/api/v1/discord/agendamentos-presenca";

        (await anonymous.GetAsync(route)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await player.GetAsync(route)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await moderator.GetAsync(route)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await admin.GetAsync(route)).StatusCode.Should().Be(HttpStatusCode.OK);

        var protectedRequests = new Func<HttpClient, Task<HttpResponseMessage>>[]
        {
            client => client.GetAsync(route),
            client => client.PostAsJsonAsync(route, ValidPresenceSchedulePayload("Protegida")),
            client => client.GetAsync($"{route}/{Guid.NewGuid()}"),
            client => client.PutAsJsonAsync($"{route}/{Guid.NewGuid()}", ValidPresenceSchedulePayload("Protegida")),
            client => client.PostAsync($"{route}/{Guid.NewGuid()}/pausar", null),
            client => client.PostAsync($"{route}/{Guid.NewGuid()}/reativar", null),
            client => client.DeleteAsync($"{route}/{Guid.NewGuid()}"),
            client => client.GetAsync($"{route}/{Guid.NewGuid()}/ocorrencias"),
        };
        foreach (var request in protectedRequests)
        {
            (await request(anonymous)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await request(player)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        var forgedAuthor = Guid.NewGuid();
        var payload = new
        {
            nome = "Agenda API",
            observacao = "Sem dados operacionais",
            diasSemana = new[] { "Sexta" },
            horarioPublicacao = "18:00",
            horarioEncerramento = "20:00",
            responsavelUsuarioId = forgedAuthor,
            criadoPorUsuarioId = forgedAuthor,
        };
        var create = await moderator.PostAsJsonAsync(route, payload);

        create.StatusCode.Should().Be(HttpStatusCode.Created);
        create.Headers.Location.Should().NotBeNull();
        var createJson = await create.Content.ReadAsStringAsync();
        createJson.Should().Contain("\"horarioPublicacao\":\"18:00\"")
            .And.Contain("\"horarioEncerramento\":\"20:00\"");
        using var createdJson = JsonDocument.Parse(createJson);
        var createdId = createdJson.RootElement.GetProperty("id").GetGuid();
        await factory.AssertScheduleAuthorAsync(createdId, userId, forgedAuthor);
        await AssertSafePresenceScheduleResponseAsync(create);

        var detail = await moderator.GetAsync($"{route}/{createdId}");
        detail.StatusCode.Should().Be(HttpStatusCode.OK);
        await AssertSafePresenceScheduleResponseAsync(detail);

        (await moderator.PutAsJsonAsync($"{route}/{createdId}", new
        {
            nome = "Agenda API editada",
            observacao = (string?)null,
            diasSemana = new[] { "Sexta" },
            horarioPublicacao = "18:00",
            horarioEncerramento = "20:00",
        })).StatusCode.Should().Be(HttpStatusCode.OK);
        (await moderator.PostAsync($"{route}/{createdId}/pausar", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await moderator.PostAsync($"{route}/{createdId}/reativar", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        var occurrences = await moderator.GetAsync($"{route}/{createdId}/ocorrencias?page=1&pageSize=20");
        occurrences.StatusCode.Should().Be(HttpStatusCode.OK);
        await AssertSafePresenceScheduleResponseAsync(occurrences);
        (await moderator.DeleteAsync($"{route}/{createdId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await moderator.GetAsync($"{route}/{createdId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PresenceScheduleEndpoints_ShouldReturnContractValidationAndNotFoundErrors()
    {
        await using var factory = new PostgreSqlApiFactory();
        using var moderator = factory.CreatePresenceScheduleModeratorClient(factory.GetExistingUserId());
        const string route = "/api/v1/discord/agendamentos-presenca";

        var invalidPage = await moderator.GetAsync($"{route}?page=0&pageSize=101");
        invalidPage.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var invalid = await moderator.PostAsJsonAsync(route, new
        {
            nome = "",
            observacao = (string?)null,
            diasSemana = Array.Empty<string>(),
            horarioPublicacao = "20:00",
            horarioEncerramento = "18:00",
        });
        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var validationError = await invalid.Content.ReadFromJsonAsync<ApiErrorResponse>();
        validationError!.Errors.Should().Contain(M(MessageCodes.PresenceScheduleNameRequired));

        var missingId = Guid.NewGuid();
        foreach (var response in new[]
        {
            await moderator.GetAsync($"{route}/{missingId}"),
            await moderator.PutAsJsonAsync($"{route}/{missingId}", new
            {
                nome = "Agenda valida",
                observacao = (string?)null,
                diasSemana = new[] { "Sexta" },
                horarioPublicacao = "18:00",
                horarioEncerramento = "20:00",
            }),
            await moderator.PostAsync($"{route}/{missingId}/pausar", null),
            await moderator.PostAsync($"{route}/{missingId}/reativar", null),
            await moderator.GetAsync($"{route}/{missingId}/ocorrencias"),
            await moderator.DeleteAsync($"{route}/{missingId}"),
        })
        {
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await response.Content.ReadFromJsonAsync<ApiErrorResponse>())!.MessageCode.Should().Be(MessageCodes.PresenceScheduleNotFound);
        }
    }

    [Fact]
    public async Task PresenceScheduleEndpoints_ShouldAllowAdminCrudAndStableTwoPageListing()
    {
        await using var factory = new PostgreSqlApiFactory();
        using var admin = factory.CreateAdminClient();
        const string route = "/api/v1/discord/agendamentos-presenca";
        var ids = new List<Guid>();
        foreach (var name in new[] { "Agenda empatada", "Agenda empatada", "Agenda pausada" })
        {
            var response = await admin.PostAsJsonAsync(route, ValidPresenceSchedulePayload(name));
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            ids.Add(json.RootElement.GetProperty("id").GetGuid());
        }

        (await admin.PostAsync($"{route}/{ids[2]}/pausar", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        var firstPage = await admin.GetAsync($"{route}?page=1&pageSize=2");
        var secondPage = await admin.GetAsync($"{route}?page=2&pageSize=2");
        firstPage.StatusCode.Should().Be(HttpStatusCode.OK);
        secondPage.StatusCode.Should().Be(HttpStatusCode.OK);
        using var firstJson = JsonDocument.Parse(await firstPage.Content.ReadAsStringAsync());
        using var secondJson = JsonDocument.Parse(await secondPage.Content.ReadAsStringAsync());
        firstJson.RootElement.GetProperty("totalItems").GetInt32().Should().Be(3);
        firstJson.RootElement.GetProperty("totalPages").GetInt32().Should().Be(2);
        var listedIds = firstJson.RootElement.GetProperty("items").EnumerateArray()
            .Concat(secondJson.RootElement.GetProperty("items").EnumerateArray())
            .Select(item => item.GetProperty("id").GetGuid())
            .ToArray();
        listedIds.Should().HaveCount(3).And.OnlyHaveUniqueItems();
        listedIds.Should().BeEquivalentTo(ids);
        secondJson.RootElement.GetProperty("items").EnumerateArray().Single()
            .GetProperty("status").GetString().Should().Be("Pausado");

        (await admin.GetAsync($"{route}/{ids[0]}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await admin.PutAsJsonAsync($"{route}/{ids[0]}", ValidPresenceSchedulePayload("Agenda admin editada"))).StatusCode.Should().Be(HttpStatusCode.OK);
        (await admin.PostAsync($"{route}/{ids[2]}/reativar", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await admin.GetAsync($"{route}/{ids[0]}/ocorrencias?page=1&pageSize=20")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await admin.DeleteAsync($"{route}/{ids[0]}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private static async Task AssertSafePresenceScheduleResponseAsync(HttpResponseMessage response)
    {
        var json = (await response.Content.ReadAsStringAsync()).ToLowerInvariant();
        json.Should().NotContainAny(
            "claimid", "claimexpiresat", "discordguildid", "channelid", "messageid", "token",
            "responsavelusuarioid", "criadoporusuarioid", "ultimatentativaem", "payload", "stacktrace");
    }

    private static object ValidPresenceSchedulePayload(string name) => new
    {
        nome = name,
        observacao = (string?)null,
        diasSemana = new[] { "Sexta" },
        horarioPublicacao = "18:00",
        horarioEncerramento = "20:00",
    };

    [Fact]
    public async Task ClosedPresenceMutation_ShouldReturnSpecificDomainMessageCode()
    {
        await using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateAdminClient();
        var createRequest = new CreateDraftMontagemRequestDto(
            $"Montagem Presenca {Guid.NewGuid():N}",
            null,
            5,
            false,
            null,
            null,
            [],
            []);

        var createResponse = await client.PostAsJsonAsync("/api/v1/draft-montagens", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<DraftMontagemResponseDto>();
        created.Should().NotBeNull();
        var closeRequest = new EncerrarPresencaDraftMontagemRequestDto(true, 5);
        var closeResponse = await client.PostAsJsonAsync($"/api/v1/draft-montagens/{created!.Id}/encerrar-presenca", closeRequest);
        closeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var repeatedMutationResponse = await client.PostAsJsonAsync($"/api/v1/draft-montagens/{created.Id}/encerrar-presenca", closeRequest);

        repeatedMutationResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await repeatedMutationResponse.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error.Should().NotBeNull();
        error!.MessageCode.Should().Be(MessageCodes.PresenceAlreadyClosed);
        error.Message.Should().Be(M(MessageCodes.PresenceAlreadyClosed));
    }

    [Fact]
    public async Task CriticalEndpointFlows_ShouldExecuteAndGenerateEndpointInventory()
    {
        await using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateAdminClient();
        var discoveredEndpoints = DiscoverEndpoints(factory);

        try
        {
            await ExecuteJogadoresFlowAsync(factory, client);
            await ExecuteTimesFlowAsync(client);
            await ExecuteDraftsFlowAsync(client);
            await ExecuteDraftMontagensFlowAsync(client);
            await ExecuteDiscordFlowAsync(client);
        }
        catch (Exception exception)
        {
            _errors.Add(exception.ToString());
            throw;
        }
        finally
        {
            WriteEndpointInventory(discoveredEndpoints);
        }
    }

    private async Task ExecuteJogadoresFlowAsync(PostgreSqlApiFactory factory, HttpClient client)
    {
        var createRequest = CreateRequest($"Teste Integracao {Guid.NewGuid():N}");
        var createResponse = await client.PostAsJsonAsync("/api/v1/jogadores", createRequest);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<JogadorResponseDto>();
        created.Should().NotBeNull();
        created!.Id.Should().NotBeEmpty();
        created.NomeExibicao.Should().Be(createRequest.NomeExibicao);
        created.Preferencias.Should().HaveCount(5);

        await AssertPersistedAsync(factory, created.Id, jogador =>
        {
            jogador.NomeExibicao.Should().Be(createRequest.NomeExibicao);
            jogador.Preferencias.Should().HaveCount(5);
        });

        const int pageSize = 100;
        int jogadoresAnteriores;
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
            jogadoresAnteriores = await dbContext.Jogadores.CountAsync(jogador => jogador.NomeExibicao.CompareTo(created.NomeExibicao) < 0);
        }

        var paginaDoJogador = (jogadoresAnteriores / pageSize) + 1;
        var listResponse = await client.GetAsync($"/api/v1/jogadores?page={paginaDoJogador}&pageSize={pageSize}");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await listResponse.Content.ReadFromJsonAsync<PaginatedResponseDto<JogadorResponseDto>>();
        list.Should().NotBeNull();
        list!.Page.Should().Be(paginaDoJogador);
        list.PageSize.Should().Be(pageSize);
        list.Items.Should().Contain(jogador => jogador.Id == created.Id);

        var getByIdResponse = await client.GetAsync($"/api/v1/jogadores/{created.Id}");
        getByIdResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var found = await getByIdResponse.Content.ReadFromJsonAsync<JogadorResponseDto>();
        found.Should().NotBeNull();
        found!.Id.Should().Be(created.Id);
        found.Preferencias.Should().HaveCount(5);

        var updateRequest = new JogadorUpdateRequestDto(
            $"{createRequest.NomeExibicao} Atualizado",
            "Maria Souza",
            "maria#4321",
            "Maria#BR1",
            "https://www.op.gg/summoners/br/Maria-BR1",
            "https://www.deeplol.gg/summoner/br/Maria-BR1",
            "Platina",
            "IV");

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/jogadores/{created.Id}/dados-basicos", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<JogadorResponseDto>();
        updated.Should().NotBeNull();
        updated!.NomeExibicao.Should().Be(updateRequest.NomeExibicao);
        updated.Elo.Should().Be(updateRequest.Elo);
        updated.Divisao.Should().Be(updateRequest.Divisao);

        await AssertPersistedAsync(factory, created.Id, jogador =>
        {
            jogador.NomeExibicao.Should().Be(updateRequest.NomeExibicao);
            jogador.Elo.Should().Be(Elo.Platina);
            jogador.Divisao.Should().Be(Divisao.IV);
        });

        var preferenciasRequest = new UpdatePreferenciasRotasRequestDto([
            new("Mid", 1, false),
            new("Adc", 2, false),
            new("Jungle", 3, false),
            new("Top", 4, true),
            new("Support", 5, false)
        ]);

        var preferenciasResponse = await client.PutAsJsonAsync($"/api/v1/jogadores/{created.Id}/preferencias-rotas", preferenciasRequest);
        preferenciasResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var preferenciasUpdated = await preferenciasResponse.Content.ReadFromJsonAsync<JogadorResponseDto>();
        preferenciasUpdated.Should().NotBeNull();
        preferenciasUpdated!.Preferencias.Should().ContainSingle(preferencia => preferencia.Rota == "Top" && preferencia.NaoJogoNemLascando);

        await AssertPersistedAsync(factory, created.Id, jogador =>
        {
            jogador.Preferencias.Should().ContainSingle(preferencia => preferencia.Rota == Rota.Top && preferencia.NaoJogoNemLascando);
            jogador.Preferencias.Should().ContainSingle(preferencia => preferencia.Rota == Rota.Mid && preferencia.Prioridade == 1);
        });

        var inativarResponse = await client.PatchAsync($"/api/v1/jogadores/{created.Id}/inativar", null);
        inativarResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await AssertPersistedAsync(factory, created.Id, jogador => jogador.Status.Should().Be(JogadorStatus.Inativo));
    }

    private static async Task ExecuteTimesFlowAsync(HttpClient client)
    {
        var jogadorA = await CreateJogadorAsync(client, $"Jogador Time {Guid.NewGuid():N}");
        var jogadorB = await CreateJogadorAsync(client, $"Jogador Time {Guid.NewGuid():N}");

        var createRequest = new CreateTimeRequestDto(
            $"Time Integracao {Guid.NewGuid():N}",
            $"TI{Random.Shared.Next(1000, 9999)}",
            "Time criado pelo teste de integracao",
            jogadorA.Id,
            [jogadorA.Id, jogadorB.Id]);

        var createResponse = await client.PostAsJsonAsync("/api/v1/times", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<TimeResponseDto>();
        created.Should().NotBeNull();
        created!.Nome.Should().Be(createRequest.Nome);
        created.Membros.Should().HaveCount(2);

        var listResponse = await client.GetAsync("/api/v1/times?page=1&pageSize=20");

        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await listResponse.Content.ReadFromJsonAsync<PaginatedResponseDto<TimeResponseDto>>();
        list.Should().NotBeNull();
        list!.Page.Should().Be(1);
        list.PageSize.Should().Be(20);
        list.TotalItems.Should().BeGreaterThanOrEqualTo(1);
        list.TotalPages.Should().BeGreaterThanOrEqualTo(1);
        list.Items.Should().Contain(time => time.Id == created.Id);

        var getByIdResponse = await client.GetAsync($"/api/v1/times/{created.Id}");
        getByIdResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var found = await getByIdResponse.Content.ReadFromJsonAsync<TimeResponseDto>();
        found.Should().NotBeNull();
        found!.Id.Should().Be(created.Id);

        var updateRequest = new UpdateTimeRequestDto(
            $"{createRequest.Nome} Atualizado",
            createRequest.Tag,
            "Time atualizado pelo teste de integracao",
            jogadorB.Id,
            [jogadorA.Id, jogadorB.Id]);
        var updateResponse = await client.PutAsJsonAsync($"/api/v1/times/{created.Id}", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<TimeResponseDto>();
        updated.Should().NotBeNull();
        updated!.Nome.Should().Be(updateRequest.Nome);
        updated.Capitao.Should().NotBeNull();
        updated.Capitao!.Id.Should().Be(jogadorB.Id);

        var inativarResponse = await client.PatchAsync($"/api/v1/times/{created.Id}/inativar", null);
        inativarResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var inactive = await inativarResponse.Content.ReadFromJsonAsync<TimeResponseDto>();
        inactive.Should().NotBeNull();
        inactive!.Status.Should().Be("Inativo");

        var reativarResponse = await client.PatchAsync($"/api/v1/times/{created.Id}/reativar", null);
        reativarResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var active = await reativarResponse.Content.ReadFromJsonAsync<TimeResponseDto>();
        active.Should().NotBeNull();
        active!.Status.Should().Be("Ativo");
    }

    private static async Task ExecuteDraftsFlowAsync(HttpClient client)
    {
        var jogadores = new[]
        {
            await CreateJogadorAsync(client, $"Jogador Draft {Guid.NewGuid():N}"),
            await CreateJogadorAsync(client, $"Jogador Draft {Guid.NewGuid():N}"),
            await CreateJogadorAsync(client, $"Jogador Draft {Guid.NewGuid():N}"),
            await CreateJogadorAsync(client, $"Jogador Draft {Guid.NewGuid():N}")
        };

        var createRequest = new CreateDraftRequestDto(
            $"Draft Integracao {Guid.NewGuid():N}",
            "Draft criado pelo teste de integracao",
            2,
            false,
            jogadores[0].Id,
            jogadores[1].Id,
            false,
            "TimeA",
            jogadores.Select(jogador => jogador.Id).ToList());

        var createResponse = await client.PostAsJsonAsync("/api/v1/drafts", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<DraftResponseDto>();
        created.Should().NotBeNull();
        created!.Status.Should().Be("Aberto");
        created.Disponiveis.Should().HaveCount(2);

        var listResponse = await client.GetAsync("/api/v1/drafts?page=1&pageSize=20");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await listResponse.Content.ReadFromJsonAsync<PaginatedResponseDto<DraftResponseDto>>();
        list.Should().NotBeNull();
        list!.Items.Should().Contain(draft => draft.Id == created.Id);

        var getByIdResponse = await client.GetAsync($"/api/v1/drafts/{created.Id}");
        getByIdResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var pickResponse = await client.PostAsJsonAsync($"/api/v1/drafts/{created.Id}/picks", new RegistrarPickDraftRequestDto(jogadores[2].Id));
        pickResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var picked = await pickResponse.Content.ReadFromJsonAsync<DraftResponseDto>();
        picked.Should().NotBeNull();
        picked!.Escolhas.Should().ContainSingle();

        var cancelResponse = await client.PatchAsJsonAsync($"/api/v1/drafts/{created.Id}/cancelar", new CancelarDraftRequestDto("Teste finalizado"));
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var canceled = await cancelResponse.Content.ReadFromJsonAsync<DraftResponseDto>();
        canceled.Should().NotBeNull();
        canceled!.Status.Should().Be("Cancelado");
    }

    private static async Task ExecuteDraftMontagensFlowAsync(HttpClient client)
    {
        var jogadores = new[]
        {
            await CreateJogadorAsync(client, $"Jogador Montagem {Guid.NewGuid():N}"),
            await CreateJogadorAsync(client, $"Jogador Montagem {Guid.NewGuid():N}"),
            await CreateJogadorAsync(client, $"Jogador Montagem {Guid.NewGuid():N}"),
            await CreateJogadorAsync(client, $"Jogador Montagem {Guid.NewGuid():N}"),
            await CreateJogadorAsync(client, $"Jogador Montagem {Guid.NewGuid():N}"),
            await CreateJogadorAsync(client, $"Jogador Montagem {Guid.NewGuid():N}")
        };

        var createRequest = new CreateDraftMontagemRequestDto(
            $"Montagem Integracao {Guid.NewGuid():N}",
            "Montagem criada pelo teste de integracao",
            3,
            false,
            null,
            null,
            [jogadores[0].Id, jogadores[1].Id],
            jogadores.Select(jogador => jogador.Id).ToList());

        var createResponse = await client.PostAsJsonAsync("/api/v1/draft-montagens", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<DraftMontagemResponseDto>();
        created.Should().NotBeNull();
        created!.QuantidadeTimes.Should().Be(2);
        created.QuantidadeReservas.Should().Be(0);

        var listResponse = await client.GetAsync("/api/v1/draft-montagens?page=1&pageSize=20");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getByIdResponse = await client.GetAsync($"/api/v1/draft-montagens/{created.Id}");
        getByIdResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var realtimeStateResponse = await client.GetAsync($"/api/v1/draft-montagens/{created.Id}/realtime-state");
        realtimeStateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var activeForDiscordRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/draft-montagens/ativos");
        activeForDiscordRequest.Headers.Add("X-Rinha-Internal-Token", SecurityApiFactory.BotToken);
        var activeForDiscordResponse = await client.SendAsync(activeForDiscordRequest);
        activeForDiscordResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var firstFree = created.Livres.First();
        var secondFree = created.Livres.Skip(1).First();
        var layoutRequest = new SalvarLayoutDraftMontagemRequestDto(
            created.Times.Select((time, index) => new DraftMontagemLayoutTimeDto(
                time.Id,
                time.Nome,
                time.CapitaoId,
                time.Jogadores.Select((jogador, jogadorIndex) => new DraftMontagemLayoutParticipanteDto(jogador.JogadorId, jogadorIndex + 1, null))
                    .Append(new DraftMontagemLayoutParticipanteDto(index == 0 ? firstFree.JogadorId : secondFree.JogadorId, time.Jogadores.Count + 1, index == 0 ? "Mid" : "Support"))
                    .ToList())).ToList(),
            created.Livres.Where(jogador => jogador.JogadorId != firstFree.JogadorId && jogador.JogadorId != secondFree.JogadorId).Select((jogador, index) => new DraftMontagemLayoutParticipanteDto(jogador.JogadorId, index + 1, null)).ToList(),
            []);

        var saveLayoutResponse = await client.PutAsJsonAsync($"/api/v1/draft-montagens/{created.Id}/layout", layoutRequest);
        saveLayoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var saved = await saveLayoutResponse.Content.ReadFromJsonAsync<DraftMontagemResponseDto>();
        saved.Should().NotBeNull();
        saved!.Times.SelectMany(time => time.Jogadores).Should().Contain(jogador => jogador.RotaContextual == "Mid");

        var drawResponse = await client.PostAsync($"/api/v1/draft-montagens/{created.Id}/capitaes/sortear", null);
        drawResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var finalizeResponse = await client.PatchAsync($"/api/v1/draft-montagens/{created.Id}/finalizar", null);
        finalizeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var cancelCreateResponse = await client.PostAsJsonAsync("/api/v1/draft-montagens", createRequest with { Nome = $"Montagem Cancelar {Guid.NewGuid():N}" });
        cancelCreateResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var cancelTarget = await cancelCreateResponse.Content.ReadFromJsonAsync<DraftMontagemResponseDto>();
        cancelTarget.Should().NotBeNull();

        var cancelResponse = await client.PatchAsJsonAsync($"/api/v1/draft-montagens/{cancelTarget!.Id}/cancelar", new CancelarDraftMontagemRequestDto("Teste cancelado"));
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task ExecuteDiscordFlowAsync(HttpClient client)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/discord/configuracoes");
        request.Headers.Add("X-Rinha-Internal-Token", SecurityApiFactory.BotToken);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);

        using var linkRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/usuarios/discord/discord-test-user/vinculo");
        linkRequest.Headers.Add("X-Rinha-Internal-Token", SecurityApiFactory.BotToken);
        var linkResponse = await client.SendAsync(linkRequest);
        linkResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task<JogadorResponseDto> CreateJogadorAsync(HttpClient client, string nome)
    {
        var request = CreateRequest(nome);
        var response = await client.PostAsJsonAsync("/api/v1/jogadores", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<JogadorResponseDto>();
        created.Should().NotBeNull();
        return created!;
    }

    private static async Task AssertPersistedAsync(PostgreSqlApiFactory factory, Guid jogadorId, Action<RinhaDasLendas.Domain.Entities.Jogador> assertion)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
        var jogador = await dbContext.Jogadores
            .Include(entity => entity.Preferencias)
            .SingleOrDefaultAsync(entity => entity.Id == jogadorId);

        jogador.Should().NotBeNull();
        assertion(jogador!);
    }

    private static IReadOnlyCollection<EndpointDescription> DiscoverEndpoints(PostgreSqlApiFactory factory)
    {
        var provider = factory.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>();

        return provider.ApiDescriptionGroups.Items
            .SelectMany(group => group.Items)
            .Where(api => api.ActionDescriptor.RouteValues.ContainsKey("controller"))
            .Select(api => new EndpointDescription(
                EndpointKey.From(api.HttpMethod ?? string.Empty, api.RelativePath ?? string.Empty),
                api.HttpMethod ?? string.Empty,
                NormalizePath(api.RelativePath ?? string.Empty),
                api.ParameterDescriptions
                    .Select(parameter => new EndpointParameter(
                        parameter.Name,
                        parameter.Source?.DisplayName ?? "Unknown",
                        FriendlyTypeName(parameter.Type)))
                    .ToList(),
                api.ParameterDescriptions
                    .Where(parameter => string.Equals(parameter.Source?.DisplayName, "Body", StringComparison.OrdinalIgnoreCase))
                    .Select(parameter => FriendlyTypeName(parameter.Type))
                    .Distinct()
                    .ToList(),
                api.SupportedResponseTypes
                    .Select(response => new EndpointResponse(
                        response.StatusCode,
                        FriendlyTypeName(response.Type)))
                    .Distinct()
                    .OrderBy(response => response.StatusCode)
                    .ToList()))
            .OrderBy(endpoint => endpoint.HttpMethod)
            .ThenBy(endpoint => endpoint.Route)
            .ToList();
    }

    private void WriteEndpointInventory(IReadOnlyCollection<EndpointDescription> discoveredEndpoints)
    {
        var report = new StringBuilder();
        report.AppendLine($"# {M(MessageCodes.TestDiscoveredEndpoints)}");
        report.AppendLine();
        report.AppendLine($"{M(MessageCodes.TestGeneratedAtUtc)}: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine();
        AppendEndpointSection(report, M(MessageCodes.TestDiscoveredEndpoints), discoveredEndpoints);
        report.AppendLine($"## {M(MessageCodes.TestErrorsFound)}");
        report.AppendLine();

        if (_errors.Count == 0)
        {
            report.AppendLine(M(MessageCodes.TestNoErrorsFound));
        }
        else
        {
            foreach (var error in _errors)
            {
                report.AppendLine("```text");
                report.AppendLine(error);
                report.AppendLine("```");
            }
        }

        var reportDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestResults"));
        Directory.CreateDirectory(reportDirectory);
        File.WriteAllText(Path.Combine(reportDirectory, "endpoint-inventory-report.md"), report.ToString());
    }

    private static void AppendEndpointSection(StringBuilder report, string title, IReadOnlyCollection<EndpointDescription> endpoints)
    {
        report.AppendLine($"## {title}");
        report.AppendLine();

        if (endpoints.Count == 0)
        {
            report.AppendLine(M(MessageCodes.TestNoEndpoint));
            report.AppendLine();
            return;
        }

        foreach (var endpoint in endpoints)
        {
            report.AppendLine($"### {endpoint.HttpMethod} {endpoint.Route}");
            report.AppendLine();
            report.AppendLine($"- {M(MessageCodes.TestParameters)}: {FormatParameters(endpoint.Parameters)}");
            report.AppendLine($"- {M(MessageCodes.TestInputDtos)}: {FormatList(endpoint.InputDtos)}");
            report.AppendLine($"- {M(MessageCodes.TestOutputDtos)}: {FormatResponses(endpoint.Responses)}");
            report.AppendLine();
        }
    }

    private static string FormatParameters(IReadOnlyCollection<EndpointParameter> parameters)
    {
        return parameters.Count == 0
            ? M(MessageCodes.TestNone)
            : string.Join(", ", parameters.Select(parameter => $"{parameter.Name} ({parameter.Source}: {parameter.TypeName})"));
    }

    private static string FormatList(IReadOnlyCollection<string> values)
    {
        return values.Count == 0 ? M(MessageCodes.TestNone) : string.Join(", ", values);
    }

    private static string FormatResponses(IReadOnlyCollection<EndpointResponse> responses)
    {
        return responses.Count == 0
            ? M(MessageCodes.TestNotDeclared)
            : string.Join(", ", responses.Select(response => $"{response.StatusCode} => {response.TypeName}"));
    }

    private static JogadorCreateRequestDto CreateRequest(string nome)
    {
        var suffix = Regex.Replace(nome, "[^A-Za-z0-9]", string.Empty);
        suffix = suffix.Length > 12 ? suffix[..12] : suffix;

        return new JogadorCreateRequestDto(
            nome,
            "Joao Silva",
            $"joao-{suffix}#1234",
            $"{suffix}#BR1",
            $"https://www.op.gg/summoners/br/{suffix}-BR1",
            $"https://www.deeplol.gg/summoner/br/{suffix}-BR1",
            "Ouro",
            "II",
            [
                new("Top", 1, false),
                new("Jungle", 2, false),
                new("Mid", 3, false),
                new("Adc", 4, false),
                new("Support", 5, false)
            ]);
    }

    private static string NormalizePath(string route)
    {
        var path = route.Split('?')[0].Trim('/');
        path = Regex.Replace(path, "{([^}:]+):[^}]+}", "{$1}");
        return $"/{path}";
    }

    private static string FriendlyTypeName(Type? type)
    {
        if (type is null || type == typeof(void))
        {
            return "Nenhum";
        }

        if (!type.IsGenericType)
        {
            return type.Name;
        }

        var genericTypeName = type.Name[..type.Name.IndexOf('`')];
        var genericArguments = string.Join(", ", type.GetGenericArguments().Select(FriendlyTypeName));
        return $"{genericTypeName}<{genericArguments}>";
    }

    private sealed class PostgreSqlApiFactory : SecurityApiFactory
    {
        public PostgreSqlApiFactory() : base(useIsolatedPostgreSql: true) { }

        public HttpClient CreateAdminClient()
        {
            return CreateJwtClient(GetExistingUserId(), AuthRoles.Admin);
        }

        public Guid GetExistingUserId()
        {
            using var scope = Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>().Users
                .Select(user => user.Id)
                .First();
        }

        public async Task AssertScheduleAuthorAsync(Guid scheduleId, Guid expectedUserId, Guid forgedUserId)
        {
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
            var schedule = await dbContext.AgendamentosPresenca
                .Include(item => item.Historicos)
                .SingleAsync(item => item.Id == scheduleId);
            schedule.CriadoPorUsuarioId.Should().Be(expectedUserId).And.NotBe(forgedUserId);
            schedule.Historicos.Should().OnlyContain(item => item.ResponsavelUsuarioId == expectedUserId);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder
                .UseSetting("Authentication:BootstrapSuperAdmin:Enabled", "true")
                .UseSetting("Authentication:BootstrapSuperAdmin:Email", "integration-admin@example.com")
                .UseSetting("Authentication:BootstrapSuperAdmin:Senha", "IntegrationAdmin123!");
        }
    }

    private readonly record struct EndpointKey(string HttpMethod, string Route)
    {
        public static EndpointKey From(string httpMethod, string route)
        {
            return new EndpointKey(httpMethod.ToUpperInvariant(), NormalizePath(route));
        }
    }

    private sealed record EndpointDescription(
        EndpointKey Key,
        string HttpMethod,
        string Route,
        IReadOnlyCollection<EndpointParameter> Parameters,
        IReadOnlyCollection<string> InputDtos,
        IReadOnlyCollection<EndpointResponse> Responses)
    {
        public string DisplayName => $"{HttpMethod} {Route}";
    }

    private sealed record EndpointParameter(string Name, string Source, string TypeName);

    private sealed record EndpointResponse(int StatusCode, string TypeName);

    private static string M(string code, params object[] args)
    {
        var message = Messages.GetMessage(code, "pt-BR");
        return args.Length == 0 ? message : string.Format(message, args);
    }
}
