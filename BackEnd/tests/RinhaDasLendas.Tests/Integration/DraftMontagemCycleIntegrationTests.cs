using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RinhaDasLendas.Application.Commands.DraftMontagens;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Infrastructure.Identity;
using RinhaDasLendas.Infrastructure.Persistence;
using RinhaDasLendas.Infrastructure.Repositories;
using RinhaDasLendas.Tests.Infrastructure;

namespace RinhaDasLendas.Tests.Integration;

public sealed class DraftMontagemCycleIntegrationTests
{
    [Fact]
    public async Task JornadaV2Manual_DeveFecharPresencaSalvarLayoutCompletoEFinalizar()
    {
        await using var factory = new DraftMontagemCycleApiFactory();
        var fixture = await factory.SeedV2PresenceDraftAsync();
        using var admin = factory.CreateRoleClient(fixture.AdminUserId, AuthRoles.Admin);

        var closed = await PostAndReadAsync<DraftMontagemResponseDto>(
            admin,
            $"/api/v1/draft-montagens/{fixture.DraftId}/encerrar-presenca",
            new { ContinuarComMenosDez = true, TamanhoEquipe = 2 });
        closed.Status.Should().Be(nameof(DraftMontagemStatus.PresencaEncerrada));
        closed.Modo.Should().BeNull();

        var manual = await PatchAndReadAsync<DraftMontagemResponseDto>(
            admin,
            $"/api/v1/draft-montagens/{fixture.DraftId}/modo",
            new { Modo = nameof(DraftMontagemModo.Manual) });
        manual.Status.Should().Be(nameof(DraftMontagemStatus.Aberta));
        manual.Times.Should().HaveCount(2);

        var starters = manual.Livres.ToList();
        var layout = new
        {
            Times = manual.Times.Select((team, index) => new
            {
                TimeId = team.Id,
                team.Nome,
                CapitaoId = (Guid?)null,
                Jogadores = starters.Skip(index * 2).Take(2).Select((player, order) => new
                {
                    player.JogadorId,
                    Ordem = order + 1,
                    RotaContextual = (string?)null,
                }),
            }),
            Livres = Array.Empty<object>(),
            Reservas = manual.Reservas.Select((player, order) => new { player.JogadorId, Ordem = order + 1, RotaContextual = (string?)null }),
        };
        var saved = await PutAndReadAsync<DraftMontagemResponseDto>(admin, $"/api/v1/draft-montagens/{fixture.DraftId}/layout", layout);
        saved.Times.Should().OnlyContain(team => team.CapitaoId == null && team.Jogadores.Count == 2);
        saved.Livres.Should().BeEmpty();

        var finalized = await PatchAndReadAsync<DraftMontagemResponseDto>(admin, $"/api/v1/draft-montagens/{fixture.DraftId}/finalizar", null);
        finalized.Status.Should().Be(nameof(DraftMontagemStatus.Finalizada));
    }

    [Fact]
    public async Task JornadaV2TempoReal_DeveCobrirTimeoutPicksSubstituicaoExplicitaEFinalizacao()
    {
        await using var factory = new DraftMontagemCycleApiFactory();
        var fixture = await factory.SeedV2PresenceDraftAsync();
        using var admin = factory.CreateRoleClient(fixture.AdminUserId, AuthRoles.Admin);

        await PostAndReadAsync<DraftMontagemResponseDto>(admin, $"/api/v1/draft-montagens/{fixture.DraftId}/encerrar-presenca", new { ContinuarComMenosDez = true, TamanhoEquipe = 2 });
        await PatchAndReadAsync<DraftMontagemResponseDto>(admin, $"/api/v1/draft-montagens/{fixture.DraftId}/modo", new { Modo = nameof(DraftMontagemModo.TempoReal) });
        await PostAndReadAsync<DraftMontagemResponseDto>(admin, $"/api/v1/draft-montagens/{fixture.DraftId}/capitaes", new { CapitaesIds = new[] { fixture.Players[0].PlayerId, fixture.Players[1].PlayerId } });
        var ordered = await PostAndReadAsync<DraftMontagemResponseDto>(admin, $"/api/v1/draft-montagens/{fixture.DraftId}/ordem-escolha", new
        {
            Modo = nameof(DraftMontagemOrdemEscolhaModo.Manual),
            CapitaesIds = new[] { fixture.Players[0].PlayerId, fixture.Players[1].PlayerId },
        });
        ordered.Status.Should().Be(nameof(DraftMontagemStatus.OrdemDefinida));

        var started = await PostAndReadAsync<DraftMontagemRealtimeStateDto>(admin, $"/api/v1/draft-montagens/{fixture.DraftId}/iniciar-tempo-real", null);
        started.Montagem.TurnoAtualCapitaoId.Should().Be(fixture.Players[0].PlayerId);

        var timedOut = await factory.ExpireCurrentTurnAndAdvanceAsync(fixture.DraftId);
        timedOut.Montagem.Escolhas.Should().ContainSingle(choice => choice.Tipo == nameof(DraftMontagemEscolhaTipo.Timeout));
        timedOut.Montagem.TurnoAtualCapitaoId.Should().Be(fixture.Players[1].PlayerId);

        using var secondCaptain = factory.CreateRoleClient(fixture.Players[1].UserId, AuthRoles.Capitao);
        var afterSecondPick = await PostAndReadAsync<DraftMontagemRealtimeStateDto>(secondCaptain, $"/api/v1/draft-montagens/{fixture.DraftId}/picks", new { JogadorId = fixture.Players[2].PlayerId });
        afterSecondPick.Montagem.TurnoAtualCapitaoId.Should().Be(fixture.Players[0].PlayerId);

        var firstTeam = afterSecondPick.Montagem.Times.Single(team => team.CapitaoId == fixture.Players[0].PlayerId);
        var substituted = await PostAndReadAsync<DraftMontagemRealtimeStateDto>(admin, $"/api/v1/draft-montagens/{fixture.DraftId}/reservas/substituir", new
        {
            TimeId = firstTeam.Id,
            JogadorSaiuId = fixture.Players[0].PlayerId,
            ReservaEntrouId = fixture.Players[4].PlayerId,
            NovoCapitaoId = fixture.Players[4].PlayerId,
            Motivo = "troca tática",
        });
        substituted.Montagem.TurnoAtualCapitaoId.Should().Be(fixture.Players[4].PlayerId);
        substituted.Montagem.Times.Single(team => team.Id == firstTeam.Id).CapitaoId.Should().Be(fixture.Players[4].PlayerId);

        using var reserveCaptain = factory.CreateRoleClient(fixture.Players[4].UserId, AuthRoles.Capitao);
        var finalized = await PostAndReadAsync<DraftMontagemRealtimeStateDto>(reserveCaptain, $"/api/v1/draft-montagens/{fixture.DraftId}/picks", new { JogadorId = fixture.Players[3].PlayerId });
        finalized.Montagem.Status.Should().Be(nameof(DraftMontagemStatus.Finalizada));
        finalized.Montagem.TurnoAtualCapitaoId.Should().BeNull();
        finalized.Montagem.Escolhas.Should().HaveCount(3);
        finalized.Montagem.Substituicoes.Should().ContainSingle();
    }

    [Fact]
    public async Task ReabrirV2AposEscolhaTempoReal_DeveExigirNovoModoERecalcularRecorteNoFechamentoSeguinte()
    {
        await using var factory = new DraftMontagemCycleApiFactory();
        var fixture = await factory.SeedV2PresenceDraftAsync();
        using var admin = factory.CreateRoleClient(fixture.AdminUserId, AuthRoles.Admin);
        await PostAndReadAsync<DraftMontagemResponseDto>(admin, $"/api/v1/draft-montagens/{fixture.DraftId}/encerrar-presenca", new { ContinuarComMenosDez = true, TamanhoEquipe = 2 });
        await PatchAndReadAsync<DraftMontagemResponseDto>(admin, $"/api/v1/draft-montagens/{fixture.DraftId}/modo", new { Modo = nameof(DraftMontagemModo.TempoReal) });

        var reopened = await PatchAndReadAsync<DraftMontagemResponseDto>(admin, $"/api/v1/draft-montagens/{fixture.DraftId}/reabrir-presenca", null);

        reopened.Status.Should().Be(nameof(DraftMontagemStatus.PresencaAberta));
        reopened.Modo.Should().BeNull();
        reopened.Times.Should().BeEmpty();
        reopened.Livres.Should().BeEmpty();
        reopened.Reservas.Should().BeEmpty();
        reopened.Presencas.Should().HaveCount(5);

        var closedAgain = await PostAndReadAsync<DraftMontagemResponseDto>(admin, $"/api/v1/draft-montagens/{fixture.DraftId}/encerrar-presenca", new { ContinuarComMenosDez = true, TamanhoEquipe = 2 });
        closedAgain.Modo.Should().BeNull();
        var selectedAgain = await PatchAndReadAsync<DraftMontagemResponseDto>(admin, $"/api/v1/draft-montagens/{fixture.DraftId}/modo", new { Modo = nameof(DraftMontagemModo.TempoReal) });
        selectedAgain.Livres.Should().HaveCount(4);
        selectedAgain.Reservas.Should().ContainSingle();
    }

    [Fact]
    public async Task SubstituirCapitaoAntesDoInicio_DevePermitirDefinirOrdemEIniciarDepois()
    {
        await using var factory = new DraftMontagemCycleApiFactory();
        var fixture = await factory.SeedV2PresenceDraftAsync();
        using var admin = factory.CreateRoleClient(fixture.AdminUserId, AuthRoles.Admin);
        await PostAndReadAsync<DraftMontagemResponseDto>(admin, $"/api/v1/draft-montagens/{fixture.DraftId}/encerrar-presenca", new { ContinuarComMenosDez = true, TamanhoEquipe = 2 });
        await PatchAndReadAsync<DraftMontagemResponseDto>(admin, $"/api/v1/draft-montagens/{fixture.DraftId}/modo", new { Modo = nameof(DraftMontagemModo.TempoReal) });
        var captains = await PostAndReadAsync<DraftMontagemResponseDto>(admin, $"/api/v1/draft-montagens/{fixture.DraftId}/capitaes", new { CapitaesIds = new[] { fixture.Players[0].PlayerId, fixture.Players[1].PlayerId } });
        var team = captains.Times.Single(item => item.CapitaoId == fixture.Players[0].PlayerId);

        var substituted = await PostAndReadAsync<DraftMontagemRealtimeStateDto>(admin, $"/api/v1/draft-montagens/{fixture.DraftId}/reservas/substituir", new
        {
            TimeId = team.Id,
            JogadorSaiuId = fixture.Players[0].PlayerId,
            ReservaEntrouId = fixture.Players[4].PlayerId,
            NovoCapitaoId = fixture.Players[4].PlayerId,
            Motivo = "capitão inelegível",
        });
        substituted.Montagem.Status.Should().Be(nameof(DraftMontagemStatus.CapitaesDefinidos));

        await PostAndReadAsync<DraftMontagemResponseDto>(admin, $"/api/v1/draft-montagens/{fixture.DraftId}/ordem-escolha", new
        {
            Modo = nameof(DraftMontagemOrdemEscolhaModo.Manual),
            CapitaesIds = new[] { fixture.Players[4].PlayerId, fixture.Players[1].PlayerId },
        });
        var started = await PostAndReadAsync<DraftMontagemRealtimeStateDto>(admin, $"/api/v1/draft-montagens/{fixture.DraftId}/iniciar-tempo-real", null);
        started.Montagem.Status.Should().Be(nameof(DraftMontagemStatus.Aberta));
        started.Montagem.TurnoAtualCapitaoId.Should().Be(fixture.Players[4].PlayerId);
    }

    [Fact]
    public async Task ProjecaoAdminReal_DeveExcluirReservaDaSelecaoInicialEIncluiLaNaSubstituicao()
    {
        await using var factory = new DraftMontagemCycleApiFactory();
        var fixture = await factory.SeedV2PresenceDraftAsync();
        using var admin = factory.CreateRoleClient(fixture.AdminUserId, AuthRoles.Admin);
        await PostAndReadAsync<DraftMontagemResponseDto>(admin, $"/api/v1/draft-montagens/{fixture.DraftId}/encerrar-presenca", new { ContinuarComMenosDez = true, TamanhoEquipe = 2 });
        await PatchAndReadAsync<DraftMontagemResponseDto>(admin, $"/api/v1/draft-montagens/{fixture.DraftId}/modo", new { Modo = nameof(DraftMontagemModo.TempoReal) });

        var projection = await admin.GetFromJsonAsync<DraftMontagemAdminResponseDto>($"/api/v1/draft-montagens/{fixture.DraftId}/administracao");

        projection.Should().NotBeNull();
        projection!.CapitaesElegiveisIds.Should().BeEquivalentTo(fixture.Players.Take(2).Select(player => player.PlayerId));
        projection.CapitaesElegiveisIds.Should().NotContain(fixture.Players[4].PlayerId);
        projection.CapitaesElegiveisSubstituicaoIds.Should().BeEquivalentTo(new[]
        {
            fixture.Players[0].PlayerId,
            fixture.Players[1].PlayerId,
            fixture.Players[4].PlayerId,
        });
    }

    [Fact]
    public async Task EscolhasDeModoConcorrentes_DevemPersistirUmaTransicaoERecusarAOutra()
    {
        await using var factory = new DraftMontagemCycleApiFactory();
        var fixture = await factory.SeedV2PresenceDraftAsync();
        using var setup = factory.CreateRoleClient(fixture.AdminUserId, AuthRoles.Admin);
        await PostAndReadAsync<DraftMontagemResponseDto>(setup, $"/api/v1/draft-montagens/{fixture.DraftId}/encerrar-presenca", new { ContinuarComMenosDez = true, TamanhoEquipe = 2 });
        using var first = factory.CreateRoleClient(fixture.AdminUserId, AuthRoles.Admin);
        using var second = factory.CreateRoleClient(fixture.AdminUserId, AuthRoles.Admin);

        var responses = await Task.WhenAll(
            first.PatchAsJsonAsync($"/api/v1/draft-montagens/{fixture.DraftId}/modo", new { Modo = nameof(DraftMontagemModo.Manual) }),
            second.PatchAsJsonAsync($"/api/v1/draft-montagens/{fixture.DraftId}/modo", new { Modo = nameof(DraftMontagemModo.TempoReal) }));

        var responseBodies = await Task.WhenAll(responses.Select(response => response.Content.ReadAsStringAsync()));
        responses.Count(response => response.StatusCode == HttpStatusCode.OK).Should().Be(1, string.Join(Environment.NewLine, responseBodies));
        responses.Count(response => response.StatusCode == HttpStatusCode.Conflict).Should().Be(1, string.Join(Environment.NewLine, responseBodies));
        responses.Should().OnlyContain(response => (int)response.StatusCode < 500, string.Join(Environment.NewLine, responseBodies));
        (await factory.GetDraftAsync(fixture.DraftId)).VersaoEstado.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ViolacaoEstruturalSemVersaoDefasada_DevePermanecerErroReal()
    {
        await using var factory = new DraftMontagemCycleApiFactory();
        var fixture = await factory.SeedV2PresenceDraftAsync();
        using var admin = factory.CreateRoleClient(fixture.AdminUserId, AuthRoles.Admin);
        await PostAndReadAsync<DraftMontagemResponseDto>(admin, $"/api/v1/draft-montagens/{fixture.DraftId}/encerrar-presenca", new { ContinuarComMenosDez = true, TamanhoEquipe = 2 });
        await PatchAndReadAsync<DraftMontagemResponseDto>(admin, $"/api/v1/draft-montagens/{fixture.DraftId}/modo", new { Modo = nameof(DraftMontagemModo.Manual) });

        var act = () => factory.SaveDuplicateParticipantWithoutStaleVersionAsync(fixture.DraftId);

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task IniciosConcorrentes_DevemTerUmVencedorENenhumErroInterno()
    {
        await using var factory = new DraftMontagemCycleApiFactory();
        var fixture = await factory.SeedV2PresenceDraftAsync();
        using var setup = factory.CreateRoleClient(fixture.AdminUserId, AuthRoles.Admin);
        await PrepareOrderedRealtimeAsync(setup, fixture);
        using var first = factory.CreateRoleClient(fixture.AdminUserId, AuthRoles.Admin);
        using var second = factory.CreateRoleClient(fixture.AdminUserId, AuthRoles.Admin);

        var responses = await Task.WhenAll(
            first.PostAsync($"/api/v1/draft-montagens/{fixture.DraftId}/iniciar-tempo-real", null),
            second.PostAsync($"/api/v1/draft-montagens/{fixture.DraftId}/iniciar-tempo-real", null));

        AssertSingleWinnerWithoutServerError(responses);
        var persisted = await factory.GetDraftWithGraphAsync(fixture.DraftId);
        persisted.Status.Should().Be(DraftMontagemStatus.Aberta);
        persisted.TurnoSequencia.Should().Be(1);
        persisted.Escolhas.Should().BeEmpty();
    }

    [Fact]
    public async Task PickETimeoutConcorrentes_DevemPersistirSomenteTimeoutSemErroInterno()
    {
        await using var factory = new DraftMontagemCycleApiFactory();
        var fixture = await factory.SeedV2PresenceDraftAsync();
        using var admin = factory.CreateRoleClient(fixture.AdminUserId, AuthRoles.Admin);
        await PrepareOrderedRealtimeAsync(admin, fixture);
        await PostAndReadAsync<DraftMontagemRealtimeStateDto>(admin, $"/api/v1/draft-montagens/{fixture.DraftId}/iniciar-tempo-real", null);
        await factory.ExpireCurrentTurnAsync(fixture.DraftId);
        using var captain = factory.CreateRoleClient(fixture.Players[0].UserId, AuthRoles.Capitao);

        var pickTask = captain.PostAsJsonAsync($"/api/v1/draft-montagens/{fixture.DraftId}/picks", new { JogadorId = fixture.Players[2].PlayerId });
        var timeoutTask = factory.AdvanceCurrentTimeoutAsync(fixture.DraftId);
        await Task.WhenAll(pickTask, timeoutTask);
        var pickResponse = await pickTask;
        var timeout = await timeoutTask;

        ((int)pickResponse.StatusCode).Should().BeLessThan(500);
        pickResponse.StatusCode.Should().NotBe(HttpStatusCode.OK);
        timeout.Should().NotBeNull();
        var persisted = await factory.GetDraftWithGraphAsync(fixture.DraftId);
        persisted.Escolhas.Should().ContainSingle(choice => choice.Tipo == DraftMontagemEscolhaTipo.Timeout);
        persisted.Participantes.Single(item => item.JogadorId == fixture.Players[2].PlayerId).Estado.Should().Be(DraftMontagemParticipanteEstado.Livre);
    }

    [Fact]
    public async Task SubstituicoesConcorrentes_DevemTerUmVencedorENenhumErroInterno()
    {
        await using var factory = new DraftMontagemCycleApiFactory();
        var fixture = await factory.SeedV2PresenceDraftAsync();
        using var setup = factory.CreateRoleClient(fixture.AdminUserId, AuthRoles.Admin);
        await PrepareOrderedRealtimeAsync(setup, fixture);
        var started = await PostAndReadAsync<DraftMontagemRealtimeStateDto>(setup, $"/api/v1/draft-montagens/{fixture.DraftId}/iniciar-tempo-real", null);
        var team = started.Montagem.Times.Single(item => item.CapitaoId == fixture.Players[0].PlayerId);
        var payload = new
        {
            TimeId = team.Id,
            JogadorSaiuId = fixture.Players[0].PlayerId,
            ReservaEntrouId = fixture.Players[4].PlayerId,
            NovoCapitaoId = fixture.Players[4].PlayerId,
            Motivo = "concorrência",
        };
        using var first = factory.CreateRoleClient(fixture.AdminUserId, AuthRoles.Admin);
        using var second = factory.CreateRoleClient(fixture.AdminUserId, AuthRoles.Admin);

        var responses = await Task.WhenAll(
            first.PostAsJsonAsync($"/api/v1/draft-montagens/{fixture.DraftId}/reservas/substituir", payload),
            second.PostAsJsonAsync($"/api/v1/draft-montagens/{fixture.DraftId}/reservas/substituir", payload));

        AssertSingleWinnerWithoutServerError(responses);
        var persisted = await factory.GetDraftWithGraphAsync(fixture.DraftId);
        persisted.Substituicoes.Should().ContainSingle();
        persisted.Times.Single(item => item.Id == team.Id).CapitaoId.Should().Be(fixture.Players[4].PlayerId);
    }

    [Fact]
    public async Task FinalizacoesConcorrentes_DevemTerUmVencedorEEstadoTerminalUnico()
    {
        await using var factory = new DraftMontagemCycleApiFactory();
        var fixture = await factory.SeedV2PresenceDraftAsync();
        using var setup = factory.CreateRoleClient(fixture.AdminUserId, AuthRoles.Admin);
        var manual = await PrepareCompleteManualAsync(setup, fixture);
        manual.Status.Should().Be(nameof(DraftMontagemStatus.Aberta));
        using var first = factory.CreateRoleClient(fixture.AdminUserId, AuthRoles.Admin);
        using var second = factory.CreateRoleClient(fixture.AdminUserId, AuthRoles.Admin);

        var responses = await Task.WhenAll(
            first.PatchAsync($"/api/v1/draft-montagens/{fixture.DraftId}/finalizar", null),
            second.PatchAsync($"/api/v1/draft-montagens/{fixture.DraftId}/finalizar", null));

        AssertSingleWinnerWithoutServerError(responses);
        (await factory.GetDraftAsync(fixture.DraftId)).Status.Should().Be(DraftMontagemStatus.Finalizada);
    }

    private static async Task PrepareOrderedRealtimeAsync(HttpClient admin, CycleFixture fixture)
    {
        await PostAndReadAsync<DraftMontagemResponseDto>(admin, $"/api/v1/draft-montagens/{fixture.DraftId}/encerrar-presenca", new { ContinuarComMenosDez = true, TamanhoEquipe = 2 });
        await PatchAndReadAsync<DraftMontagemResponseDto>(admin, $"/api/v1/draft-montagens/{fixture.DraftId}/modo", new { Modo = nameof(DraftMontagemModo.TempoReal) });
        await PostAndReadAsync<DraftMontagemResponseDto>(admin, $"/api/v1/draft-montagens/{fixture.DraftId}/capitaes", new { CapitaesIds = new[] { fixture.Players[0].PlayerId, fixture.Players[1].PlayerId } });
        await PostAndReadAsync<DraftMontagemResponseDto>(admin, $"/api/v1/draft-montagens/{fixture.DraftId}/ordem-escolha", new
        {
            Modo = nameof(DraftMontagemOrdemEscolhaModo.Manual),
            CapitaesIds = new[] { fixture.Players[0].PlayerId, fixture.Players[1].PlayerId },
        });
    }

    private static async Task<DraftMontagemResponseDto> PrepareCompleteManualAsync(HttpClient admin, CycleFixture fixture)
    {
        await PostAndReadAsync<DraftMontagemResponseDto>(admin, $"/api/v1/draft-montagens/{fixture.DraftId}/encerrar-presenca", new { ContinuarComMenosDez = true, TamanhoEquipe = 2 });
        var manual = await PatchAndReadAsync<DraftMontagemResponseDto>(admin, $"/api/v1/draft-montagens/{fixture.DraftId}/modo", new { Modo = nameof(DraftMontagemModo.Manual) });
        var starters = manual.Livres.ToList();
        return await PutAndReadAsync<DraftMontagemResponseDto>(admin, $"/api/v1/draft-montagens/{fixture.DraftId}/layout", new
        {
            Times = manual.Times.Select((team, index) => new
            {
                TimeId = team.Id,
                team.Nome,
                CapitaoId = (Guid?)null,
                Jogadores = starters.Skip(index * 2).Take(2).Select((player, order) => new { player.JogadorId, Ordem = order + 1, RotaContextual = (string?)null }),
            }),
            Livres = Array.Empty<object>(),
            Reservas = manual.Reservas.Select((player, order) => new { player.JogadorId, Ordem = order + 1, RotaContextual = (string?)null }),
        });
    }

    private static void AssertSingleWinnerWithoutServerError(IReadOnlyCollection<HttpResponseMessage> responses)
    {
        responses.Count(response => response.StatusCode == HttpStatusCode.OK).Should().Be(1);
        responses.Count(response => response.StatusCode != HttpStatusCode.OK).Should().Be(1);
        responses.Should().OnlyContain(response => (int)response.StatusCode < 500);
    }

    internal static async Task<T> PostAndReadAsync<T>(HttpClient client, string route, object? payload)
    {
        var response = payload is null ? await client.PostAsync(route, null) : await client.PostAsJsonAsync(route, payload);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    internal static async Task<T> PatchAndReadAsync<T>(HttpClient client, string route, object? payload)
    {
        var response = payload is null ? await client.PatchAsync(route, null) : await client.PatchAsJsonAsync(route, payload);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private static async Task<T> PutAndReadAsync<T>(HttpClient client, string route, object payload)
    {
        var response = await client.PutAsJsonAsync(route, payload);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
}

internal sealed class DraftMontagemCycleApiFactory : SecurityApiFactory
{
    public DraftMontagemCycleApiFactory() : base(useIsolatedPostgreSql: true) { }

    public HttpClient CreateRoleClient(Guid? userId, params string[] roles) => CreateJwtClient(userId, roles);

    public async Task<CycleFixture> SeedV2PresenceDraftAsync()
    {
        _ = CreateAnonymousClient();
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
        var captainRoleId = await db.Roles.Where(role => role.Name == AuthRoles.Capitao).Select(role => role.Id).SingleAsync();
        var adminUserId = Guid.NewGuid();
        db.Users.Add(CreateUser(adminUserId, "Administrador do ciclo"));
        var players = new List<CyclePlayer>();
        for (var index = 1; index <= 5; index++)
        {
            var userId = Guid.NewGuid();
            var user = CreateUser(userId, $"Jogador ciclo {index}");
            var player = CreatePlayer(index, userId);
            db.Users.Add(user);
            db.Jogadores.Add(player);
            players.Add(new CyclePlayer(userId, player.Id));
            if (index is 1 or 2 or 5)
            {
                db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = userId, RoleId = captainRoleId });
            }
        }

        var draft = DraftMontagem.CriarPorPresenca("Ciclo integrado v2", null, 2);
        foreach (var player in players)
        {
            draft.ConfirmarPresenca(player.UserId, player.PlayerId, null, DraftMontagemPresencaOrigem.Web);
        }
        db.DraftMontagens.Add(draft);
        await db.SaveChangesAsync();
        return new CycleFixture(draft.Id, adminUserId, players);
    }

    public async Task<DraftMontagemRealtimeStateDto> ExpireCurrentTurnAndAdvanceAsync(Guid draftId)
    {
        await ExpireCurrentTurnAsync(draftId);
        return (await AdvanceCurrentTimeoutAsync(draftId))!;
    }

    public async Task ExpireCurrentTurnAsync(Guid draftId)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
        var draft = await db.DraftMontagens.SingleAsync(item => item.Id == draftId);
        typeof(DraftMontagem).GetProperty(nameof(DraftMontagem.TurnoExpiraEm))!.SetValue(draft, DateTimeOffset.UtcNow.AddSeconds(-1));
        await db.SaveChangesAsync();
    }

    public async Task<DraftMontagemRealtimeStateDto?> AdvanceCurrentTimeoutAsync(Guid draftId)
    {
        await using var scope = Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>()
            .Send(new AvancarTurnoDraftMontagemTimeoutCommand(draftId));
    }

    public async Task<DraftMontagem> GetDraftAsync(Guid draftId)
    {
        await using var scope = Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>()
            .DraftMontagens.AsNoTracking().SingleAsync(item => item.Id == draftId);
    }

    public async Task<DraftMontagem> GetDraftWithGraphAsync(Guid draftId)
    {
        await using var scope = Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>()
            .DraftMontagens.AsNoTracking()
            .Include(item => item.Times)
            .Include(item => item.Participantes)
            .Include(item => item.Escolhas)
            .Include(item => item.Substituicoes)
            .SingleAsync(item => item.Id == draftId);
    }

    public async Task SaveDuplicateParticipantWithoutStaleVersionAsync(Guid draftId)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
        var draft = await db.DraftMontagens.Include(item => item.Participantes).SingleAsync(item => item.Id == draftId);
        var existing = draft.Participantes.First();
        var participants = (List<DraftMontagemParticipante>)typeof(DraftMontagem)
            .GetField("_participantes", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(draft)!;
        participants.Add(new DraftMontagemParticipante(existing.JogadorId, DraftMontagemParticipanteEstado.Livre, existing.Ordem + 100));

        await new DraftMontagemRepository(db).SaveChangesAsync(CancellationToken.None);
    }

    public async Task<CycleFixture> SeedLegacyOpenDraftAsync(DraftMontagemStatus? forcedStatus = null)
    {
        _ = CreateAnonymousClient();
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
        var adminUserId = Guid.NewGuid();
        db.Users.Add(CreateUser(adminUserId, "Administrador legado"));
        var players = new List<CyclePlayer>();
        for (var index = 1; index <= 5; index++)
        {
            var userId = Guid.NewGuid();
            var player = CreatePlayer(index + 10, userId);
            db.Users.Add(CreateUser(userId, $"Jogador legado {index}"));
            db.Jogadores.Add(player);
            players.Add(new CyclePlayer(userId, player.Id));
        }

        var draft = new DraftMontagem(
            "Draft legado ativo",
            null,
            2,
            DraftMontagemCriterioCapitaes.Manual,
            players.Select(player => player.PlayerId).ToList(),
            players.Take(2).Select(player => player.PlayerId).ToList());
        typeof(DraftMontagem).GetProperty(nameof(DraftMontagem.CicloVersao))!.SetValue(draft, DraftMontagemCicloVersao.Legado);
        if (forcedStatus is not null)
        {
            typeof(DraftMontagem).GetProperty(nameof(DraftMontagem.Status))!.SetValue(draft, forcedStatus.Value);
        }
        db.DraftMontagens.Add(draft);
        await db.SaveChangesAsync();
        return new CycleFixture(draft.Id, adminUserId, players);
    }

    private static ApplicationUser CreateUser(Guid id, string name) => new()
    {
        Id = id,
        Nome = name,
        UserName = $"cycle-{id:N}",
        NormalizedUserName = $"CYCLE-{id:N}",
        Ativo = true,
    };

    private static Jogador CreatePlayer(int index, Guid userId)
    {
        var player = new Jogador(
            $"Jogador {index}", null, $"cycle{index}#1234", null, null, null, Elo.Ouro, Divisao.II,
            [
                new PreferenciaRota(Rota.Top, 1, false),
                new PreferenciaRota(Rota.Jungle, 2, false),
                new PreferenciaRota(Rota.Mid, 3, false),
                new PreferenciaRota(Rota.Adc, 4, false),
                new PreferenciaRota(Rota.Support, 5, false),
            ]);
        player.VincularUsuario(userId);
        return player;
    }
}

internal sealed record CyclePlayer(Guid UserId, Guid PlayerId);
internal sealed record CycleFixture(Guid DraftId, Guid AdminUserId, IReadOnlyList<CyclePlayer> Players);
