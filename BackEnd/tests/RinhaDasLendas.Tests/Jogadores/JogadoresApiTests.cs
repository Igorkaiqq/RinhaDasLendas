using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Tests.Jogadores;

public sealed class JogadoresApiTests
{
    [Fact]
    public async Task PostJogadores_ShouldReturnCreatedPlayer()
    {
        await using var factory = new JogadoresApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/jogadores", JogadorTestData.CreateRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var jogador = await response.Content.ReadFromJsonAsync<JogadorResponseDto>();
        jogador.Should().NotBeNull();
        jogador!.Status.Should().Be("Ativo");
    }

    [Fact]
    public async Task PutPreferenciasRotas_ShouldPersistValidPreferences()
    {
        await using var factory = new JogadoresApiFactory();
        var client = factory.CreateClient();
        var createdResponse = await client.PostAsJsonAsync("/api/v1/jogadores", JogadorTestData.CreateRequest());
        var created = await createdResponse.Content.ReadFromJsonAsync<JogadorResponseDto>();
        var request = new UpdatePreferenciasRotasRequestDto([
            new("Mid", 1, false),
            new("Adc", 2, false),
            new("Jungle", 3, false),
            new("Top", 4, true),
            new("Support", 5, false)
        ]);

        var response = await client.PutAsJsonAsync($"/api/v1/jogadores/{created!.Id}/preferencias-rotas", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var jogador = await response.Content.ReadFromJsonAsync<JogadorResponseDto>();
        jogador!.Preferencias.First().Rota.Should().Be("Mid");
        jogador.Preferencias.Single(preferencia => preferencia.NaoJogoNemLascando).Rota.Should().Be("Top");
    }

    [Fact]
    public async Task PatchInativarAndGetJogadores_ShouldKeepPlayerButFilterActiveList()
    {
        await using var factory = new JogadoresApiFactory();
        var client = factory.CreateClient();
        var createdResponse = await client.PostAsJsonAsync("/api/v1/jogadores", JogadorTestData.CreateRequest());
        var created = await createdResponse.Content.ReadFromJsonAsync<JogadorResponseDto>();

        var patchResponse = await client.PatchAsync($"/api/v1/jogadores/{created!.Id}/inativar", null);
        var allResponse = await client.GetFromJsonAsync<PaginatedResponseDto<JogadorResponseDto>>("/api/v1/jogadores");
        var activeResponse = await client.GetFromJsonAsync<PaginatedResponseDto<JogadorResponseDto>>("/api/v1/jogadores?somenteAtivos=true");
        var byIdResponse = await client.GetFromJsonAsync<JogadorResponseDto>($"/api/v1/jogadores/{created.Id}");

        patchResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        allResponse!.Items.Should().ContainSingle(player => player.Id == created.Id && player.Status == "Inativo");
        activeResponse!.Items.Should().BeEmpty();
        byIdResponse!.Status.Should().Be("Inativo");
    }

    private sealed class JogadoresApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IJogadorRepository>();
                services.AddSingleton<IJogadorRepository, InMemoryJogadorRepository>();
            });
        }
    }
}
