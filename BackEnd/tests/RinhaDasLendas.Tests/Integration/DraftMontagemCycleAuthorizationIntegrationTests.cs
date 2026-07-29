using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RinhaDasLendas.Domain.Constants;

namespace RinhaDasLendas.Tests.Integration;

public sealed class DraftMontagemCycleAuthorizationIntegrationTests
{
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
        (await bot.PatchAsJsonAsync($"/api/v1/draft-montagens/{fixture.DraftId}/modo", new { Modo = "Manual" })).StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }
}
