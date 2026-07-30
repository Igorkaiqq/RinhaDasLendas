using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Tests.Integration;

public sealed class DraftMontagemCycleAuthorizationIntegrationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReaberturaV2ELegada_DeveRecusarModeradorSemMutacao(bool legado)
    {
        await using var factory = new DraftMontagemCycleApiFactory();
        var fixture = legado
            ? await factory.SeedLegacyOpenDraftAsync(DraftMontagemStatus.PresencaEncerrada)
            : await factory.SeedV2PresenceDraftAsync();
        if (!legado)
        {
            using var admin = factory.CreateRoleClient(fixture.AdminUserId, AuthRoles.Admin);
            await DraftMontagemCycleIntegrationTests.PostAndReadAsync<object>(admin, $"/api/v1/draft-montagens/{fixture.DraftId}/encerrar-presenca", new { ContinuarComMenosDez = true, TamanhoEquipe = 2 });
        }
        var before = await factory.GetDraftAsync(fixture.DraftId);
        using var moderator = factory.CreateRoleClient(Guid.NewGuid(), AuthRoles.Moderador);

        var response = await moderator.PatchAsync($"/api/v1/draft-montagens/{fixture.DraftId}/reabrir-presenca", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var after = await factory.GetDraftAsync(fixture.DraftId);
        after.Status.Should().Be(before.Status);
        after.VersaoEstado.Should().Be(before.VersaoEstado);
    }

    [Theory]
    [InlineData(AuthRoles.Admin, HttpStatusCode.OK)]
    [InlineData(AuthRoles.SuperAdmin, HttpStatusCode.OK)]
    [InlineData(AuthRoles.Moderador, HttpStatusCode.Forbidden)]
    [InlineData(AuthRoles.Jogador, HttpStatusCode.Forbidden)]
    public async Task EscolhaDeModo_DeveAplicarMatrizAdminPlus(string role, HttpStatusCode expected)
    {
        await using var factory = new DraftMontagemCycleApiFactory();
        var fixture = await factory.SeedV2PresenceDraftAsync();
        using var admin = factory.CreateRoleClient(fixture.AdminUserId, AuthRoles.Admin);
        await DraftMontagemCycleIntegrationTests.PostAndReadAsync<object>(admin, $"/api/v1/draft-montagens/{fixture.DraftId}/encerrar-presenca", new { ContinuarComMenosDez = true, TamanhoEquipe = 2 });
        using var client = factory.CreateRoleClient(Guid.NewGuid(), role);

        var response = await client.PatchAsJsonAsync($"/api/v1/draft-montagens/{fixture.DraftId}/modo", new { Modo = "Manual" });

        response.StatusCode.Should().Be(expected);
    }

    [Fact]
    public async Task EscolhaDeModo_DeveRecusarAnonimoEBot()
    {
        await using var factory = new DraftMontagemCycleApiFactory();
        var fixture = await factory.SeedV2PresenceDraftAsync();
        using var admin = factory.CreateRoleClient(fixture.AdminUserId, AuthRoles.Admin);
        await DraftMontagemCycleIntegrationTests.PostAndReadAsync<object>(admin, $"/api/v1/draft-montagens/{fixture.DraftId}/encerrar-presenca", new { ContinuarComMenosDez = true, TamanhoEquipe = 2 });
        using var anonymous = factory.CreateAnonymousClient();
        using var bot = factory.CreateBotClient();

        (await anonymous.PatchAsJsonAsync($"/api/v1/draft-montagens/{fixture.DraftId}/modo", new { Modo = "Manual" })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await bot.PatchAsJsonAsync($"/api/v1/draft-montagens/{fixture.DraftId}/modo", new { Modo = "Manual" })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task BotValido_DeveReceberForbiddenEmTodasAsOperacoesAdminPlus()
    {
        await using var factory = new DraftMontagemCycleApiFactory();
        var fixture = await factory.SeedV2PresenceDraftAsync();
        using var bot = factory.CreateBotClient();
        var routes = new (HttpMethod Method, string Path, object? Payload)[]
        {
            (HttpMethod.Get, $"/api/v1/draft-montagens/{fixture.DraftId}/administracao", null),
            (HttpMethod.Patch, $"/api/v1/draft-montagens/{fixture.DraftId}/modo", new { Modo = "Manual" }),
            (HttpMethod.Post, $"/api/v1/draft-montagens/{fixture.DraftId}/capitaes", new { CapitaesIds = Array.Empty<Guid>() }),
            (HttpMethod.Post, $"/api/v1/draft-montagens/{fixture.DraftId}/ordem-escolha", new { Modo = "Sorteado", CapitaesIds = Array.Empty<Guid>() }),
            (HttpMethod.Post, $"/api/v1/draft-montagens/{fixture.DraftId}/iniciar-tempo-real", null),
            (HttpMethod.Post, $"/api/v1/draft-montagens/{fixture.DraftId}/reservas/substituir", new { TimeId = Guid.NewGuid(), JogadorSaiuId = Guid.NewGuid(), ReservaEntrouId = Guid.NewGuid() }),
            (HttpMethod.Put, $"/api/v1/draft-montagens/{fixture.DraftId}/layout", new { Times = Array.Empty<object>(), Livres = Array.Empty<object>(), Reservas = Array.Empty<object>() }),
            (HttpMethod.Post, $"/api/v1/draft-montagens/{fixture.DraftId}/capitaes/sortear", null),
            (HttpMethod.Patch, $"/api/v1/draft-montagens/{fixture.DraftId}/finalizar", null),
        };

        foreach (var route in routes)
        {
            using var request = new HttpRequestMessage(route.Method, route.Path)
            {
                Content = route.Payload is null ? null : JsonContent.Create(route.Payload),
            };
            var response = await bot.SendAsync(request);
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden, route.Path);
        }
    }
}
