using System.Net.Http.Json;
using FluentAssertions;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Tests.Integration;

public sealed class DraftMontagemLegacyCompatibilityIntegrationTests
{
    [Theory]
    [InlineData(DraftMontagemStatus.PresencaAberta)]
    [InlineData(DraftMontagemStatus.PresencaEncerrada)]
    [InlineData(DraftMontagemStatus.Aberta)]
    [InlineData(DraftMontagemStatus.Finalizada)]
    [InlineData(DraftMontagemStatus.Cancelada)]
    public async Task DraftV1_DevePreservarEstadosAtivosETerminais(DraftMontagemStatus status)
    {
        await using var factory = new DraftMontagemCycleApiFactory();
        var fixture = await factory.SeedLegacyOpenDraftAsync(status);
        using var admin = factory.CreateRoleClient(fixture.AdminUserId, AuthRoles.Admin);

        var response = await admin.GetAsync($"/api/v1/draft-montagens/{fixture.DraftId}");

        response.EnsureSuccessStatusCode();
        var draft = await response.Content.ReadFromJsonAsync<DraftMontagemResponseDto>();
        draft!.CicloVersao.Should().Be(nameof(DraftMontagemCicloVersao.Legado));
        draft.Status.Should().Be(status.ToString());
    }

    [Fact]
    public async Task DraftV1Aberto_DevePreservarInicioPickESubstituicaoSemNovoCapitao()
    {
        await using var factory = new DraftMontagemCycleApiFactory();
        var fixture = await factory.SeedLegacyOpenDraftAsync();
        using var admin = factory.CreateRoleClient(fixture.AdminUserId, AuthRoles.Admin);

        var started = await DraftMontagemCycleIntegrationTests.PostAndReadAsync<DraftMontagemRealtimeStateDto>(
            admin,
            $"/api/v1/draft-montagens/{fixture.DraftId}/iniciar-tempo-real",
            null);
        started.Montagem.CicloVersao.Should().Be(nameof(DraftMontagemCicloVersao.Legado));
        started.Montagem.Status.Should().Be(nameof(DraftMontagemStatus.Aberta));

        var captainId = started.Montagem.TurnoAtualCapitaoId!.Value;
        var captain = fixture.Players.Single(player => player.PlayerId == captainId);
        using var captainClient = factory.CreateRoleClient(captain.UserId, AuthRoles.Jogador);
        var freePlayer = started.Montagem.Livres.First();
        var picked = await DraftMontagemCycleIntegrationTests.PostAndReadAsync<DraftMontagemRealtimeStateDto>(
            captainClient,
            $"/api/v1/draft-montagens/{fixture.DraftId}/picks",
            new { freePlayer.JogadorId });
        picked.Montagem.Escolhas.Should().ContainSingle();

        var team = picked.Montagem.Times.First(item => item.Jogadores.Any(player => !player.Capitao));
        var outgoing = team.Jogadores.First(player => !player.Capitao);
        var reserveId = fixture.Players[4].PlayerId;
        var substituted = await DraftMontagemCycleIntegrationTests.PostAndReadAsync<DraftMontagemRealtimeStateDto>(
            admin,
            $"/api/v1/draft-montagens/{fixture.DraftId}/reservas/substituir",
            new { TimeId = team.Id, JogadorSaiuId = outgoing.JogadorId, ReservaEntrouId = reserveId, Motivo = (string?)null });

        substituted.Montagem.Substituicoes.Should().ContainSingle();
        substituted.Montagem.Times.Single(item => item.Id == team.Id).Jogadores.Select(player => player.JogadorId).Should().Contain(reserveId);
    }

    [Fact]
    public async Task DraftV1_DeveTransferirCapitaniaParaReservaQuandoCapitaoSaiSemNovoCapitaoId()
    {
        await using var factory = new DraftMontagemCycleApiFactory();
        var fixture = await factory.SeedLegacyOpenDraftAsync();
        using var admin = factory.CreateRoleClient(fixture.AdminUserId, AuthRoles.Admin);
        var started = await DraftMontagemCycleIntegrationTests.PostAndReadAsync<DraftMontagemRealtimeStateDto>(
            admin,
            $"/api/v1/draft-montagens/{fixture.DraftId}/iniciar-tempo-real",
            null);
        var outgoingCaptainId = started.Montagem.TurnoAtualCapitaoId!.Value;
        var team = started.Montagem.Times.Single(item => item.CapitaoId == outgoingCaptainId);
        var reserve = fixture.Players[4];

        var substituted = await DraftMontagemCycleIntegrationTests.PostAndReadAsync<DraftMontagemRealtimeStateDto>(
            admin,
            $"/api/v1/draft-montagens/{fixture.DraftId}/reservas/substituir",
            new { TimeId = team.Id, JogadorSaiuId = outgoingCaptainId, ReservaEntrouId = reserve.PlayerId, Motivo = (string?)null });

        substituted.Montagem.TurnoAtualCapitaoId.Should().Be(reserve.PlayerId);
        var resultingTeam = substituted.Montagem.Times.Single(item => item.Id == team.Id);
        resultingTeam.CapitaoId.Should().Be(reserve.PlayerId);
        resultingTeam.Jogadores.Single(item => item.JogadorId == reserve.PlayerId).Capitao.Should().BeTrue();
    }
}
