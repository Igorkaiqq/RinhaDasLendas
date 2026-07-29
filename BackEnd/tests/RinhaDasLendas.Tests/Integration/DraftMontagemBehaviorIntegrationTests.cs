using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Collections.Concurrent;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;
using RinhaDasLendas.Api.Filters;
using RinhaDasLendas.Api.Services;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Models;
using RinhaDasLendas.Domain.Repositories;
using RinhaDasLendas.Infrastructure.Identity;
using RinhaDasLendas.Infrastructure.Messages;
using RinhaDasLendas.Infrastructure.Persistence;
using RinhaDasLendas.Infrastructure.Repositories;
using RinhaDasLendas.Tests.Infrastructure;

namespace RinhaDasLendas.Tests.Integration;

public sealed class DraftMontagemBehaviorIntegrationTests
{
    [Fact]
    public async Task DuasConfirmacoesHttpConcorrentes_DevemRetornarSemErroInternoEUmaPresenca()
    {
        await using var factory = new PostgreSqlComposeApiFactory();
        var (draftId, userId) = await factory.SeedPresenceAsync();
        factory.ArmPresenceConcurrency(draftId);
        using var firstClient = factory.CreateUserClient(userId);
        using var secondClient = factory.CreateUserClient(userId);
        var responses = await Task.WhenAll(
            firstClient.PostAsJsonAsync(
                $"/api/v1/draft-montagens/{draftId}/presencas/confirmar",
                new { UsuarioId = userId, DiscordUserId = (string?)null, Origem = "Web" }),
            secondClient.PostAsJsonAsync(
                $"/api/v1/draft-montagens/{draftId}/presencas/confirmar",
                new { UsuarioId = userId, DiscordUserId = (string?)null, Origem = "Web" }));

        responses.Should().OnlyContain(response => (int)response.StatusCode < 500);
        responses.Should().OnlyContain(response => response.StatusCode == HttpStatusCode.OK);
        foreach (var response in responses)
        {
            AssertPublicProjection(await response.Content.ReadAsStringAsync());
        }
        var payloads = await Task.WhenAll(responses.Select(response => response.Content.ReadFromJsonAsync<DraftMontagemResponseDto>()));
        payloads.Should().OnlyContain(payload => payload != null
            && payload.Presencas.Count(presence => presence.UsuarioId == userId && presence.Status == DraftMontagemPresencaStatus.Confirmada.ToString()) == 1);
        (await factory.CountConfirmedPresencesAsync(draftId, userId)).Should().Be(1);
        factory.LoadedVersions.Should().HaveCount(2).And.OnlyContain(version => version == factory.LoadedVersions[0]);
        factory.SaveObservedLoadedCounts.Should().Equal(2, 2);
        factory.SaveResults.Should().ContainSingle(result => result == DraftMontagemSaveResultado.Persistido);
        factory.SaveResults.Should().ContainSingle(result => result != DraftMontagemSaveResultado.Persistido);
        factory.PresenceConfirmedEffects.Should().Be(1);
        factory.RealtimeEffects.Should().Be(1);
    }

    [Fact]
    public async Task DoisCancelamentosHttpConcorrentes_DevemRetornarSucessoEUmEfeito()
    {
        await using var factory = new PostgreSqlComposeApiFactory();
        var (draftId, userId) = await factory.SeedPresenceAsync(confirmed: true);
        factory.ArmPresenceConcurrency(draftId);
        using var firstClient = factory.CreateUserClient(userId);
        using var secondClient = factory.CreateUserClient(userId);

        var responses = await Task.WhenAll(
            firstClient.PostAsJsonAsync(
                $"/api/v1/draft-montagens/{draftId}/presencas/cancelar",
                new { UsuarioId = userId, DiscordUserId = (string?)null }),
            secondClient.PostAsJsonAsync(
                $"/api/v1/draft-montagens/{draftId}/presencas/cancelar",
                new { UsuarioId = userId, DiscordUserId = (string?)null }));

        responses.Should().OnlyContain(response => response.StatusCode == HttpStatusCode.OK);
        foreach (var response in responses)
        {
            AssertPublicProjection(await response.Content.ReadAsStringAsync());
        }
        var payloads = await Task.WhenAll(responses.Select(response => response.Content.ReadFromJsonAsync<DraftMontagemResponseDto>()));
        payloads.Should().OnlyContain(payload => payload != null
            && payload.Presencas.Single(presence => presence.UsuarioId == userId).Status == DraftMontagemPresencaStatus.Cancelada.ToString());
        (await factory.GetPresenceStatusAsync(draftId, userId)).Should().Be(DraftMontagemPresencaStatus.Cancelada);
        factory.LoadedVersions.Should().HaveCount(2).And.OnlyContain(version => version == factory.LoadedVersions[0]);
        factory.SaveObservedLoadedCounts.Should().Equal(2, 2);
        factory.SaveResults.Should().ContainSingle(result => result == DraftMontagemSaveResultado.Persistido);
        factory.SaveResults.Should().ContainSingle(result => result == DraftMontagemSaveResultado.ConflitoDeVersao);
        factory.PresenceCancelledEffects.Should().Be(1);
        factory.RealtimeEffects.Should().Be(1);
    }

    [Fact]
    public async Task TrySaveChangesAsync_DevePropagarViolacaoUnicaQueNaoSejaDePresenca()
    {
        await using var factory = new PostgreSqlComposeApiFactory();

        var act = () => factory.SaveDuplicateUserNamesThroughDraftRepositoryAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task ReloadByIdAsync_DeveLimparTrackingAntesDeRecarregar()
    {
        await using var factory = new PostgreSqlComposeApiFactory();
        var (draftId, _) = await factory.SeedPresenceAsync();

        var result = await factory.MutateWithoutSavingAndReloadAsync(draftId);

        result.OriginalDetached.Should().BeTrue();
        result.DifferentInstance.Should().BeTrue();
        result.ReloadedClosingTime.Should().BeNull();
    }

    [Fact]
    public async Task ListActiveForDiscordAsync_DeveIncluirTimesRepublicadosFinalizadosSemGuild()
    {
        await using var factory = new PostgreSqlComposeApiFactory();
        var draftId = await factory.SeedTerminalDraftAsync(DraftMontagemStatus.Finalizada, withPendingTeamsPublication: true);

        var drafts = await factory.ListActiveForDiscordAsync();

        drafts.Select(draft => draft.Id).Should().Contain(draftId);
    }

    [Fact]
    public async Task ListActiveForDiscordAsync_DeveIncluirPendenteAntigoAposMaisDeCinquentaRecentes()
    {
        await using var factory = new PostgreSqlComposeApiFactory();
        var oldPendingDraftId = await factory.SeedOldPendingPublicationAfterRecentDraftsAsync(51);

        var drafts = await factory.ListActiveForDiscordAsync();

        drafts.Select(draft => draft.Id).Should().Contain(oldPendingDraftId);
    }

    [Fact]
    public async Task ListActiveForDiscordAsync_NaoDeveIncluirHistoricoFinalizadoIrrelevante()
    {
        await using var factory = new PostgreSqlComposeApiFactory();
        var draftId = await factory.SeedTerminalDraftAsync(DraftMontagemStatus.Finalizada, withPendingTeamsPublication: false);

        var drafts = await factory.ListActiveForDiscordAsync();

        drafts.Select(draft => draft.Id).Should().NotContain(draftId);
    }

    [Fact]
    public async Task ListActiveForDiscordAsync_DeveIncluirPublicacaoAcionavelEmQualquerStatusTerminal()
    {
        await using var factory = new PostgreSqlComposeApiFactory();
        var draftId = await factory.SeedTerminalDraftAsync(DraftMontagemStatus.Cancelada, withPendingTeamsPublication: true);

        var drafts = await factory.ListActiveForDiscordAsync();

        drafts.Select(draft => draft.Id).Should().Contain(draftId);
    }

    [Fact]
    public async Task DoisClaimsConcorrentes_DevemConcederExatamenteUmClaim()
    {
        await using var factory = new PostgreSqlComposeApiFactory();
        var draftId = await factory.SeedPendingPublicationAsync();

        var payloads = await SendConcurrentClaimsAsync(factory, draftId);

        AssertSingleWinnerAndCurrentLoser(payloads);
    }

    [Fact]
    public async Task DoisClaimsConcorrentesSemPublicacaoPreexistente_DevemRetornarEstadoAtualAoPerdedor()
    {
        await using var factory = new PostgreSqlComposeApiFactory();

        for (var attempt = 0; attempt < 10; attempt++)
        {
            var draftId = await factory.SeedDraftWithoutPublicationAsync();

            var payloads = await SendConcurrentClaimsAsync(factory, draftId);

            AssertSingleWinnerAndCurrentLoser(payloads);
        }
    }

    [Fact]
    public async Task ConclusaoComClaimDivergente_DeveRetornarCodigoEstavel()
    {
        await using var factory = new PostgreSqlComposeApiFactory();
        using var client = factory.CreateBotClient();
        var draftId = await factory.SeedPendingPublicationAsync();
        var claimResponse = await client.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{draftId}/discord/publicacoes/claim",
            new { Tipo = "Presenca" });
        claimResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{draftId}/discord/publicacao",
            new
            {
                Tipo = "Presenca",
                ClaimId = Guid.NewGuid(),
                DiscordGuildId = "guild-1",
                DiscordChannelId = "channel-1",
                MessageId = "message-1",
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error.Should().NotBeNull();
        error!.MessageCode.Should().Be(MessageCodes.DiscordPublicationClaimMismatch);
    }

    [Fact]
    public async Task ClaimExpirado_DeveExigirReconciliacaoESerRecusado()
    {
        await using var factory = new PostgreSqlComposeApiFactory();
        using var client = factory.CreateBotClient();
        var draftId = await factory.SeedPendingPublicationAsync();
        var firstClaim = await client.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{draftId}/discord/publicacoes/claim",
            new { Tipo = "Presenca" });
        firstClaim.StatusCode.Should().Be(HttpStatusCode.OK);
        await factory.ExpireClaimAsync(draftId);

        var secondClaim = await client.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{draftId}/discord/publicacoes/claim",
            new { Tipo = "Presenca" });

        secondClaim.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadJsonAsync(secondClaim);
        payload.RootElement.GetProperty("adquirido").GetBoolean().Should().BeFalse();
        payload.RootElement.GetProperty("status").GetString().Should().Be("RequerReconciliacao");
        (await factory.GetPublicationStatusAsync(draftId)).Should().Be("RequerReconciliacao");
    }

    [Fact]
    public async Task ClaimExpirado_DeveExigirReconciliacaoSemNovaAquisicao()
    {
        await using var factory = new PostgreSqlComposeApiFactory();
        using var client = factory.CreateBotClient();
        var draftId = await factory.SeedPendingPublicationAsync();
        var claimResponse = await client.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{draftId}/discord/publicacoes/claim",
            new { Tipo = "Presenca" });
        claimResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        await factory.ExpireClaimAsync(draftId);

        var reconciled = await factory.ReconcileExpiredClaimsAsync();

        reconciled.Should().Be(1);
        (await factory.GetPublicationStatusAsync(draftId)).Should().Be("RequerReconciliacao");
    }

    [Fact]
    public async Task ExpiracoesDeMultiplasPublicacoes_DevemRetornarIdsDistintosEAtualizarTodosOsEstados()
    {
        await using var factory = new PostgreSqlComposeApiFactory();
        var (primeiroDraftId, segundoDraftId) = await factory.SeedMultipleExpiredPublicationsAsync();

        var reconciledIds = await factory.ReconcileExpiredClaimIdsAsync();
        var states = await factory.GetPublicationStatesAsync([primeiroDraftId, segundoDraftId]);

        reconciledIds.Should().HaveCount(2);
        reconciledIds.Should().BeEquivalentTo([primeiroDraftId, segundoDraftId]);
        states.Should().BeEquivalentTo(
        [
            (primeiroDraftId, DraftMontagemPublicacaoDiscordTipo.Presenca, DraftMontagemPublicacaoDiscordStatus.RequerReconciliacao),
            (primeiroDraftId, DraftMontagemPublicacaoDiscordTipo.TimesDefinidos, DraftMontagemPublicacaoDiscordStatus.RequerReconciliacao),
            (segundoDraftId, DraftMontagemPublicacaoDiscordTipo.Presenca, DraftMontagemPublicacaoDiscordStatus.RequerReconciliacao),
        ]);
    }

    [Fact]
    public async Task Claim_DeveAceitarSomenteAutenticacaoInternaDoBot()
    {
        await using var factory = new PostgreSqlComposeApiFactory();
        var draftId = await factory.SeedPendingPublicationAsync();
        using var anonymousClient = factory.CreateClient();
        using var userClient = factory.CreateJwtClient(Guid.NewGuid(), AuthRoles.Admin);

        var anonymousResponse = await anonymousClient.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{draftId}/discord/publicacoes/claim",
            new { Tipo = "Presenca" });
        var userResponse = await userClient.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{draftId}/discord/publicacoes/claim",
            new { Tipo = "Presenca" });

        anonymousResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        userResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var completionResponse = await anonymousClient.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{draftId}/discord/publicacao",
            new { Tipo = "Presenca", ClaimId = Guid.NewGuid(), MessageId = "message-1" });
        var failureResponse = await anonymousClient.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{draftId}/discord/publicacao/falha",
            new { Tipo = "Presenca", ClaimId = Guid.NewGuid(), ErroCodigo = "Timeout" });
        completionResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        failureResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ConclusaoEFalha_DevemExigirEConsumirClaimAtivo()
    {
        await using var factory = new PostgreSqlComposeApiFactory();
        using var client = factory.CreateBotClient();
        var publishedDraftId = await factory.SeedPendingPublicationAsync();
        var failedDraftId = await factory.SeedPendingPublicationAsync();
        var publishedClaimId = await AcquireClaimIdAsync(client, publishedDraftId);
        var failedClaimId = await AcquireClaimIdAsync(client, failedDraftId);

        var completionResponse = await client.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{publishedDraftId}/discord/publicacao",
            new
            {
                Tipo = "Presenca",
                ClaimId = publishedClaimId,
                DiscordGuildId = "guild-1",
                DiscordChannelId = "channel-1",
                MessageId = "message-1",
            });
        var failureResponse = await client.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{failedDraftId}/discord/publicacao/falha",
            new
            {
                Tipo = "Presenca",
                ClaimId = failedClaimId,
                DiscordGuildId = "guild-1",
                DiscordChannelId = "channel-1",
                ErroCodigo = "Timeout",
            });

        completionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        failureResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var completion = await ReadJsonAsync(completionResponse);
        using var failure = await ReadJsonAsync(failureResponse);
        AssertBotOperationalProjection(completion.RootElement);
        AssertBotOperationalProjection(failure.RootElement);
        (await factory.GetPublicationStatusAsync(publishedDraftId)).Should().Be("Publicada");
        (await factory.GetPublicationStatusAsync(failedDraftId)).Should().Be("Falha");
    }

    [Fact]
    public async Task FalhaDeDeadlineAposExpiracaoDoClaim_DeveUsarCasExatoSemReconciliacao()
    {
        await using var factory = new PostgreSqlComposeApiFactory();
        using var client = factory.CreateBotClient();
        var deadline = DateTimeOffset.UtcNow.AddMinutes(2);
        var draftId = await factory.SeedPendingPublicationAsync(deadline);
        var claimId = await AcquireClaimIdAsync(client, draftId);
        var claimed = await factory.GetPublicationPersistenceStateAsync(draftId);
        claimed.ClaimExpiresAt.Should().BeCloseTo(deadline, TimeSpan.FromMilliseconds(1));
        await factory.ExpirePresenceWindowAndClaimAsync(draftId);

        var failurePayload = new
        {
            Tipo = "Presenca",
            ClaimId = claimId,
            DiscordGuildId = "guild-1",
            DiscordChannelId = "channel-1",
            ErroCodigo = "PRESENCE_DEADLINE_EXPIRED",
        };
        var response = await client.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{draftId}/discord/publicacao/falha",
            failurePayload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var failed = await factory.GetPublicationPersistenceStateAsync(draftId);
        failed.Status.Should().Be("Falha");
        failed.ErrorCode.Should().Be("PRESENCE_DEADLINE_EXPIRED");
        failed.MessageId.Should().BeNull();
        failed.ClaimExpiresAt.Should().BeNull();
        (await client.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{draftId}/discord/publicacao/falha",
            failurePayload)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await factory.GetPublicationPersistenceStateAsync(draftId)).Status.Should().Be("Falha");

        foreach (var invalid in new[]
        {
            (Type: "Presenca", DivergentClaim: false, Code: "Timeout"),
            (Type: "Presenca", DivergentClaim: true, Code: "PRESENCE_DEADLINE_EXPIRED"),
            (Type: "ChamadaPresenca", DivergentClaim: false, Code: "PRESENCE_DEADLINE_EXPIRED"),
        })
        {
            var invalidDraftId = await factory.SeedPendingPublicationAsync(DateTimeOffset.UtcNow.AddMinutes(2));
            var invalidClaimId = await AcquireClaimIdAsync(client, invalidDraftId);
            await factory.ExpirePresenceWindowAndClaimAsync(invalidDraftId);
            var invalidResponse = await client.PostAsJsonAsync(
                $"/api/v1/draft-montagens/{invalidDraftId}/discord/publicacao/falha",
                new
                {
                    Tipo = invalid.Type,
                    ClaimId = invalid.DivergentClaim ? Guid.NewGuid() : invalidClaimId,
                    ErroCodigo = invalid.Code,
                });

            invalidResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await factory.GetPublicationPersistenceStateAsync(invalidDraftId)).Status
                .Should().NotBe("Falha");
        }
    }

    [Theory]
    [InlineData("Invalido")]
    [InlineData("1")]
    [InlineData("2")]
    [InlineData("3")]
    [InlineData("999")]
    public async Task ClaimComTipoInvalido_DeveRetornarValidacaoLocalizada(string tipo)
    {
        await using var factory = new PostgreSqlComposeApiFactory();
        using var client = factory.CreateBotClient();
        var draftId = await factory.SeedPendingPublicationAsync();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{draftId}/discord/publicacoes/claim",
            new { Tipo = tipo });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error.Should().NotBeNull();
        error!.MessageCode.Should().Be(MessageCodes.ValidationError);
        (await factory.GetPublicationClaimStateAsync(draftId)).Should().Be(("Pendente", null));
    }

    [Fact]
    public async Task AdicaoManualSemMotivo_NaoDeveMutarDraft()
    {
        await using var factory = new PostgreSqlComposeApiFactory();
        var fixture = await factory.SeedAdministrativeDraftAsync();
        using var client = factory.CreateUserClient(fixture.ExecutorUserId);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/presencas/manual",
            new { JogadorId = fixture.TargetPlayerId, Motivo = "   " });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error.Should().NotBeNull();
        error!.MessageCode.Should().Be(MessageCodes.ValidationError);
        (await factory.GetAdministrativeMutationStateAsync(fixture.DraftId)).Should().Be((0, 0, DraftMontagemStatus.PresencaAberta));
    }

    [Fact]
    public async Task CancelamentoSemNameIdentifier_NaoDeveMutarDraft()
    {
        await using var factory = new PostgreSqlComposeApiFactory();
        var fixture = await factory.SeedAdministrativeDraftAsync();
        using var client = factory.CreateAdminClientWithoutNameIdentifier();

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/cancelar",
            new { Motivo = "evento cancelado" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error.Should().NotBeNull();
        error!.MessageCode.Should().Be(MessageCodes.UnauthorizedAccess);
        (await factory.GetAdministrativeMutationStateAsync(fixture.DraftId)).Should().Be((0, 0, DraftMontagemStatus.PresencaAberta));
    }

    [Fact]
    public async Task AdicaoManualValida_DevePersistirExecutorAlvoEMotivo()
    {
        await using var factory = new PostgreSqlComposeApiFactory();
        var fixture = await factory.SeedAdministrativeDraftAsync();
        using var client = factory.CreateUserClient(fixture.ExecutorUserId);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/presencas/manual",
            new { JogadorId = fixture.TargetPlayerId, Motivo = "  convidado pelo organizador  " });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        AssertPublicProjection(await response.Content.ReadAsStringAsync());
        var action = await factory.GetAdministrativeActionAsync(fixture.DraftId);
        action.Should().Be(("AdicaoPresencaManual", fixture.ExecutorUserId, fixture.TargetPlayerId, "convidado pelo organizador"));
        (await factory.GetAdministrativeMutationStateAsync(fixture.DraftId)).Should().Be((1, 1, DraftMontagemStatus.PresencaAberta));
    }

    [Fact]
    public async Task PublicacaoComPayloadInvalido_NaoDeveConsumirClaim()
    {
        await using var factory = new PostgreSqlComposeApiFactory();
        using var client = factory.CreateBotClient();
        var draftId = await factory.SeedPendingPublicationAsync();
        var claimId = await AcquireClaimIdAsync(client, draftId);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{draftId}/discord/publicacao",
            new
            {
                Tipo = "Presenca",
                ClaimId = claimId,
                DiscordGuildId = new string('1', 41),
                DiscordChannelId = "channel-1",
                MessageId = "message-1",
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<ApiErrorResponse>())!.MessageCode.Should().Be(MessageCodes.ValidationError);
        (await factory.GetPublicationClaimStateAsync(draftId)).Should().Be(("EmAndamento", claimId));
    }

    [Fact]
    public async Task FalhaPublicacaoComPayloadInvalido_NaoDeveConsumirClaim()
    {
        await using var factory = new PostgreSqlComposeApiFactory();
        using var client = factory.CreateBotClient();
        var draftId = await factory.SeedPendingPublicationAsync();
        var claimId = await AcquireClaimIdAsync(client, draftId);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{draftId}/discord/publicacao/falha",
            new { Tipo = "Presenca", ClaimId = claimId, ErroCodigo = new string('e', 121) });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<ApiErrorResponse>())!.MessageCode.Should().Be(MessageCodes.ValidationError);
        (await factory.GetPublicationClaimStateAsync(draftId)).Should().Be(("EmAndamento", claimId));
    }

    [Fact]
    public async Task AcoesAdministrativasSemExecutor_NaoDevemMutarDrafts()
    {
        await using var factory = new PostgreSqlComposeApiFactory();
        using var client = factory.CreateAdminClientWithoutNameIdentifier();
        var addition = await factory.SeedAdministrativeDraftAsync();
        var removal = await factory.SeedAdministrativeDraftAsync(confirmed: true);
        var republication = await factory.SeedAdministrativeDraftAsync();

        var responses = new[]
        {
            await client.PostAsJsonAsync($"/api/v1/draft-montagens/{addition.DraftId}/presencas/manual", new { JogadorId = addition.TargetPlayerId, Motivo = "convite" }),
            await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/draft-montagens/{removal.DraftId}/presencas/{removal.TargetPlayerId}")
            {
                Content = JsonContent.Create(new { Motivo = "ausencia" }),
            }),
            await client.PostAsJsonAsync($"/api/v1/draft-montagens/{republication.DraftId}/discord/publicacoes/republicar", new { Tipo = "Presenca", Motivo = "mensagem removida" }),
        };

        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await response.Content.ReadFromJsonAsync<ApiErrorResponse>())!.MessageCode.Should().Be(MessageCodes.UnauthorizedAccess);
        }
        (await factory.GetAdministrativeMutationStateAsync(addition.DraftId)).Should().Be((0, 0, DraftMontagemStatus.PresencaAberta));
        (await factory.GetAdministrativeMutationStateAsync(removal.DraftId)).Should().Be((1, 0, DraftMontagemStatus.PresencaAberta));
        (await factory.GetConfirmedPresenceCountByPlayerAsync(removal.DraftId, removal.TargetPlayerId)).Should().Be(1);
        (await factory.GetAdministrativeMutationStateAsync(republication.DraftId)).Should().Be((0, 0, DraftMontagemStatus.PresencaAberta));
        (await factory.GetPublicationCountAsync(republication.DraftId)).Should().Be(0);
    }

    [Fact]
    public async Task MotivosInvalidos_NaoDevemMutarAcoesAdministrativas()
    {
        await using var factory = new PostgreSqlComposeApiFactory();
        var cancellation = await factory.SeedAdministrativeDraftAsync();
        var addition = await factory.SeedAdministrativeDraftAsync();
        var removal = await factory.SeedAdministrativeDraftAsync(confirmed: true);
        var republication = await factory.SeedAdministrativeDraftAsync();
        using var client = factory.CreateUserClient(cancellation.ExecutorUserId);

        var responses = new[]
        {
            await client.PatchAsJsonAsync($"/api/v1/draft-montagens/{cancellation.DraftId}/cancelar", new { Motivo = " " }),
            await client.PostAsJsonAsync($"/api/v1/draft-montagens/{addition.DraftId}/presencas/manual", new { JogadorId = addition.TargetPlayerId, Motivo = (string?)null }),
            await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/draft-montagens/{removal.DraftId}/presencas/{removal.TargetPlayerId}")
            {
                Content = JsonContent.Create(new { Motivo = string.Empty }),
            }),
            await client.PostAsJsonAsync($"/api/v1/draft-montagens/{republication.DraftId}/discord/publicacoes/republicar", new { Tipo = "Presenca", Motivo = new string('a', 501) }),
        };

        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await response.Content.ReadFromJsonAsync<ApiErrorResponse>())!.MessageCode.Should().Be(MessageCodes.ValidationError);
        }
        (await factory.GetAdministrativeMutationStateAsync(cancellation.DraftId)).Should().Be((0, 0, DraftMontagemStatus.PresencaAberta));
        (await factory.GetAdministrativeMutationStateAsync(addition.DraftId)).Should().Be((0, 0, DraftMontagemStatus.PresencaAberta));
        (await factory.GetAdministrativeMutationStateAsync(removal.DraftId)).Should().Be((1, 0, DraftMontagemStatus.PresencaAberta));
        (await factory.GetConfirmedPresenceCountByPlayerAsync(removal.DraftId, removal.TargetPlayerId)).Should().Be(1);
        (await factory.GetPublicationCountAsync(republication.DraftId)).Should().Be(0);
    }

    [Fact]
    public async Task ConsultasPublicas_NaoDevemExporAuditoriaOuDadosOperacionais()
    {
        await using var factory = new PostgreSqlComposeApiFactory();
        var fixture = await factory.SeedProjectionDraftAsync();
        using var client = factory.CreatePlayerClient(fixture.PlayerUserId);

        var detailResponse = await client.GetAsync($"/api/v1/draft-montagens/{fixture.DraftId}");
        var listResponse = await client.GetAsync("/api/v1/draft-montagens?pageSize=100");
        var realtimeResponse = await client.GetAsync($"/api/v1/draft-montagens/{fixture.DraftId}/realtime-state");

        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        realtimeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        AssertPublicProjection(await detailResponse.Content.ReadAsStringAsync());
        AssertPublicProjection(await listResponse.Content.ReadAsStringAsync());
        AssertPublicProjection(await realtimeResponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ConsultaAdministrativa_DeveExigirPermissaoERetornarAuditoriaEOperacaoCompletas()
    {
        await using var factory = new PostgreSqlComposeApiFactory();
        var fixture = await factory.SeedProjectionDraftAsync();
        using var playerClient = factory.CreatePlayerClient(fixture.PlayerUserId);
        using var adminClient = factory.CreateUserClient(fixture.AdminUserId);

        var forbiddenResponse = await playerClient.GetAsync($"/api/v1/draft-montagens/{fixture.DraftId}/administracao");
        var adminResponse = await adminClient.GetAsync($"/api/v1/draft-montagens/{fixture.DraftId}/administracao");

        forbiddenResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        adminResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await adminResponse.Content.ReadAsStringAsync();
        json.Should().Contain("acoesAdministrativas");
        json.Should().Contain("responsavelUsuarioId");
        json.Should().Contain("jogadorAlvoId");
        json.Should().Contain("motivo");
        json.Should().Contain("discordGuildId");
        json.Should().Contain("discordPresenceMessageId");
        json.Should().Contain("discordUserId");
        json.Should().Contain("guildId");
        json.Should().Contain("channelId");
        json.Should().Contain("messageId");
        json.Should().Contain("ultimoErroCodigo");
        json.Should().Contain("claimId");
    }

    [Fact]
    public async Task ConsultaDoBot_DeveRetornarSomenteContratoOperacionalCompativel()
    {
        await using var factory = new PostgreSqlComposeApiFactory();
        var fixture = await factory.SeedProjectionDraftAsync();
        using var client = factory.CreateBotClient();

        var response = await client.GetAsync("/api/v1/draft-montagens/ativos");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = await ReadJsonAsync(response);
        var draft = document.RootElement.EnumerateArray().Single(item => item.GetProperty("id").GetGuid() == fixture.DraftId);
        AssertBotOperationalProjection(draft);
    }

    [Fact]
    public async Task ConfirmacaoECancelamentoDiscord_DevemRetornarContratoOperacionalMinimo()
    {
        await using var factory = new PostgreSqlComposeApiFactory();
        var fixture = await factory.SeedProjectionDraftAsync();
        using var client = factory.CreateBotClient();

        var cancellationResponse = await client.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/discord/presencas/cancelar",
            new { DiscordUserId = "discord-user-secreto" });
        var confirmationResponse = await client.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/discord/presencas/confirmar",
            new { DiscordUserId = "discord-user-secreto", Origem = "Discord" });

        cancellationResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        confirmationResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var cancellation = await ReadJsonAsync(cancellationResponse);
        using var confirmation = await ReadJsonAsync(confirmationResponse);
        AssertBotOperationalProjection(cancellation.RootElement);
        AssertBotOperationalProjection(confirmation.RootElement);
    }

    [Fact]
    public async Task DezenoveConfirmacoes_DevemFormarTresTimesQuatroReservasEDarInicioAoDraftTempoReal()
    {
        await using var factory = new PostgreSqlComposeApiFactory();
        var fixture = await factory.SeedDraftJourneyAsync();
        await ConfirmPlayersAsync(factory, fixture.DraftId, fixture.Players.Take(19));
        using var admin = factory.CreateUserClient(fixture.AdminUserId);

        var closeResponse = await admin.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/encerrar-presenca",
            new EncerrarPresencaDraftMontagemRequestDto(false, 5));
        closeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var closed = await closeResponse.Content.ReadFromJsonAsync<DraftMontagemResponseDto>();
        closed.Should().NotBeNull();
        closed!.Presencas.Count(presence => presence.Status == DraftMontagemPresencaStatus.Confirmada.ToString()).Should().Be(19);
        closed.QuantidadeTimes.Should().Be(3);
        closed.QuantidadeReservas.Should().Be(4);

        var captains = fixture.Players.Take(3).Select(player => player.PlayerId).ToList();
        var captainsResponse = await admin.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/capitaes",
            new DefinirCapitaesDraftMontagemRequestDto(captains));
        captainsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await captainsResponse.Content.ReadFromJsonAsync<DraftMontagemResponseDto>())!.Status
            .Should().Be(DraftMontagemStatus.CapitaesDefinidos.ToString());

        var orderResponse = await admin.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/ordem-escolha",
            new DefinirOrdemEscolhaDraftMontagemRequestDto(DraftMontagemOrdemEscolhaModo.Manual.ToString(), captains));
        orderResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var ordered = await orderResponse.Content.ReadFromJsonAsync<DraftMontagemResponseDto>();
        ordered!.Status.Should().Be(DraftMontagemStatus.Aberta.ToString());
        ordered.OrdemEscolhaModo.Should().Be(DraftMontagemOrdemEscolhaModo.Manual.ToString());

        var startResponse = await admin.PostAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/iniciar-tempo-real",
            null);
        startResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var started = await startResponse.Content.ReadFromJsonAsync<DraftMontagemRealtimeStateDto>();
        started!.Montagem.Modo.Should().Be(DraftMontagemModo.TempoReal.ToString());
        started.Montagem.TurnoAtualCapitaoId.Should().Be(captains[0]);
        started.Montagem.TurnoSequencia.Should().Be(1);
    }

    [Fact]
    public async Task DezenoveConfirmacoes_AposReabrirEConfirmarVigesimoDevemFormarQuatroTimesSemReservas()
    {
        await using var factory = new PostgreSqlComposeApiFactory();
        var fixture = await factory.SeedDraftJourneyAsync();
        await ConfirmPlayersAsync(factory, fixture.DraftId, fixture.Players.Take(19));
        using var admin = factory.CreateUserClient(fixture.AdminUserId);
        var closeRoute = $"/api/v1/draft-montagens/{fixture.DraftId}/encerrar-presenca";

        var firstCloseResponse = await admin.PostAsJsonAsync(
            closeRoute,
            new EncerrarPresencaDraftMontagemRequestDto(false, 5));
        firstCloseResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstClose = await firstCloseResponse.Content.ReadFromJsonAsync<DraftMontagemResponseDto>();
        firstClose!.QuantidadeTimes.Should().Be(3);
        firstClose.QuantidadeReservas.Should().Be(4);

        var reopenResponse = await admin.PatchAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/reabrir-presenca",
            null);
        reopenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var reopened = await reopenResponse.Content.ReadFromJsonAsync<DraftMontagemResponseDto>();
        reopened!.Status.Should().Be(DraftMontagemStatus.PresencaAberta.ToString());
        reopened.Presencas.Count(presence => presence.Status == DraftMontagemPresencaStatus.Confirmada.ToString()).Should().Be(19);
        reopened.QuantidadeTimes.Should().Be(0);
        reopened.QuantidadeReservas.Should().Be(0);

        await ConfirmPlayersAsync(factory, fixture.DraftId, fixture.Players.Skip(19));
        var secondCloseResponse = await admin.PostAsJsonAsync(
            closeRoute,
            new EncerrarPresencaDraftMontagemRequestDto(false, 5));
        secondCloseResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondClose = await secondCloseResponse.Content.ReadFromJsonAsync<DraftMontagemResponseDto>();
        secondClose!.Presencas.Count(presence => presence.Status == DraftMontagemPresencaStatus.Confirmada.ToString()).Should().Be(20);
        secondClose.QuantidadeTimes.Should().Be(4);
        secondClose.QuantidadeReservas.Should().Be(0);
    }

    [Fact]
    public async Task DuasReaberturasHttpConcorrentes_DevemPersistirUmaTransicaoEAuditoriaSemPerderPresencas()
    {
        await using var factory = new PostgreSqlComposeApiFactory();
        var fixture = await factory.SeedDraftJourneyAsync();
        await ConfirmPlayersAsync(factory, fixture.DraftId, fixture.Players.Take(19));
        using var setupClient = factory.CreateUserClient(fixture.AdminUserId);
        var closeResponse = await setupClient.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/encerrar-presenca",
            new EncerrarPresencaDraftMontagemRequestDto(false, 5));
        closeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        factory.ArmPresenceConcurrency(fixture.DraftId);
        using var firstClient = factory.CreateUserClient(fixture.AdminUserId);
        using var secondClient = factory.CreateUserClient(fixture.AdminUserId);
        firstClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US");
        secondClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US");

        var responses = await Task.WhenAll(
            firstClient.PatchAsync($"/api/v1/draft-montagens/{fixture.DraftId}/reabrir-presenca", null),
            secondClient.PatchAsync($"/api/v1/draft-montagens/{fixture.DraftId}/reabrir-presenca", null));

        responses.Count(response => response.StatusCode == HttpStatusCode.OK).Should().Be(1);
        responses.Count(response => response.StatusCode == HttpStatusCode.Conflict).Should().Be(1);
        var error = await responses.Single(response => response.StatusCode == HttpStatusCode.Conflict)
            .Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.MessageCode.Should().Be(MessageCodes.DraftStateConflict);
        error.Message.Should().Be(new ResourceMessageProvider().GetMessage(MessageCodes.DraftStateConflict, "en-US"));
        var state = await factory.GetReopenStateAsync(fixture.DraftId);
        state.Status.Should().Be(DraftMontagemStatus.PresencaAberta);
        state.ConfirmedPresences.Should().Be(19);
        state.ReopenActions.Should().Be(1);
        factory.RealtimeEffects.Should().Be(1);
    }

    [Fact]
    public async Task ReaberturaComPrazoVencido_DevePermanecerAbertaAposCicloDeEncerramentoAutomatico()
    {
        await using var factory = new PostgreSqlComposeApiFactory();
        var fixture = await factory.SeedDraftJourneyAsync(DateTimeOffset.UtcNow.AddMinutes(-1));
        await ConfirmPlayersAsync(factory, fixture.DraftId, fixture.Players.Take(19));
        using var admin = factory.CreateUserClient(fixture.AdminUserId);
        (await admin.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/encerrar-presenca",
            new EncerrarPresencaDraftMontagemRequestDto(false, 5))).StatusCode.Should().Be(HttpStatusCode.OK);

        (await admin.PatchAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/reabrir-presenca",
            null)).StatusCode.Should().Be(HttpStatusCode.OK);
        await factory.RunPresenceClosureCycleAsync();

        var state = await factory.GetReopenStateAsync(fixture.DraftId);
        state.Status.Should().Be(DraftMontagemStatus.PresencaAberta);
        state.PresenceDeadline.Should().BeNull();
        state.ConfirmedPresences.Should().Be(19);
        state.ReopenActions.Should().Be(1);
    }

    private static async Task ConfirmPlayersAsync(
        PostgreSqlComposeApiFactory factory,
        Guid draftId,
        IEnumerable<(Guid UserId, Guid PlayerId)> players)
    {
        foreach (var player in players)
        {
            using var client = factory.CreatePlayerClient(player.UserId);
            var response = await client.PostAsJsonAsync(
                $"/api/v1/draft-montagens/{draftId}/presencas/confirmar",
                new ConfirmarPresencaDraftMontagemRequestDto(player.UserId, null, DraftMontagemPresencaOrigem.Web.ToString()));
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    private static void AssertPublicProjection(string json)
    {
        json.Should().NotContain("acoesAdministrativas");
        json.Should().NotContain("motivoCancelamento");
        json.Should().NotContain("responsavelUsuarioId");
        json.Should().NotContain("jogadorAlvoId");
        json.Should().NotContain("discordGuildId");
        json.Should().NotContain("discordPresenceMessageId");
        json.Should().NotContain("discordUserId");
        json.Should().NotContain("guildId");
        json.Should().NotContain("channelId");
        json.Should().NotContain("messageId");
        json.Should().NotContain("ultimoErroCodigo");
        json.Should().NotContain("claimId");
        json.Should().NotContain("publicadaEm");
        json.Should().NotContain("ultimaTentativaEm");
    }

    private static void AssertBotOperationalProjection(JsonElement draft)
    {
        draft.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(
        [
            "id",
            "nome",
            "status",
            "horarioEncerramentoPresenca",
            "discordPresenceMessageId",
            "publicacoesDiscord",
            "presencas",
            "times",
            "reservas",
            "arquivado",
            "versaoEstado",
        ]);
        var publications = draft.GetProperty("publicacoesDiscord");
        if (publications.GetArrayLength() > 0)
        {
            publications[0].EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(["tipo", "status"]);
        }

        var presences = draft.GetProperty("presencas");
        if (presences.GetArrayLength() > 0)
        {
            presences[0].EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(["nomeExibicao", "status"]);
        }
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    private static async Task<JsonDocument[]> SendConcurrentClaimsAsync(PostgreSqlComposeApiFactory factory, Guid draftId)
    {
        using var firstClient = factory.CreateBotClient();
        using var secondClient = factory.CreateBotClient();
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var readyCount = 0;

        async Task<HttpResponseMessage> SendAsync(HttpClient client)
        {
            if (Interlocked.Increment(ref readyCount) == 2)
            {
                ready.SetResult();
            }

            await release.Task;
            return await client.PostAsJsonAsync(
                $"/api/v1/draft-montagens/{draftId}/discord/publicacoes/claim",
                new { Tipo = "Presenca" });
        }

        var requests = new[] { SendAsync(firstClient), SendAsync(secondClient) };
        await ready.Task;
        release.SetResult();
        var responses = await Task.WhenAll(requests);
        responses.Should().OnlyContain(response => response.StatusCode == HttpStatusCode.OK);
        return await Task.WhenAll(responses.Select(ReadJsonAsync));
    }

    private static void AssertSingleWinnerAndCurrentLoser(JsonDocument[] payloads)
    {
        payloads.Count(payload => payload.RootElement.GetProperty("adquirido").GetBoolean()).Should().Be(1);
        var loser = payloads.Single(payload => !payload.RootElement.GetProperty("adquirido").GetBoolean());
        loser.RootElement.GetProperty("status").GetString().Should().Be("EmAndamento");
        loser.RootElement.GetProperty("claimId").ValueKind.Should().Be(JsonValueKind.Null);
    }

    private static async Task<Guid> AcquireClaimIdAsync(HttpClient client, Guid draftId)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{draftId}/discord/publicacoes/claim",
            new { Tipo = "Presenca" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var payload = await ReadJsonAsync(response);
        return payload.RootElement.GetProperty("claimId").GetGuid();
    }

    private sealed class PostgreSqlComposeApiFactory : SecurityApiFactory
    {
        private readonly PresenceConcurrencyCoordinator _presenceConcurrency = new();
        private readonly PresenceEffectCounter _presenceEffects = new();

        public IReadOnlyList<long> LoadedVersions => _presenceConcurrency.LoadedVersions;
        public IReadOnlyList<DraftMontagemSaveResultado> SaveResults => _presenceConcurrency.SaveResults;
        public IReadOnlyList<int> SaveObservedLoadedCounts => _presenceConcurrency.SaveObservedLoadedCounts;
        public int PresenceConfirmedEffects => _presenceEffects.PresenceConfirmed;
        public int PresenceCancelledEffects => _presenceEffects.PresenceCancelled;
        public int RealtimeEffects => _presenceEffects.Realtime;

        public PostgreSqlComposeApiFactory() : base(useIsolatedPostgreSql: true) { }

        public HttpClient CreateUserClient(Guid userId)
        {
            return CreateJwtClient(userId, AuthRoles.Admin);
        }

        public HttpClient CreatePlayerClient(Guid userId)
        {
            return CreateJwtClient(userId, AuthRoles.Jogador);
        }

        public HttpClient CreateAdminClientWithoutNameIdentifier()
        {
            return CreateJwtClient(null, AuthRoles.Admin);
        }

        public void ArmPresenceConcurrency(Guid draftId)
        {
            _presenceConcurrency.Arm(draftId);
            _presenceEffects.Reset();
        }

        public async Task<(Guid DraftId, Guid AdminUserId, Guid PlayerUserId)> SeedProjectionDraftAsync()
        {
            _ = CreateClient();
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
            var adminUserId = Guid.NewGuid();
            var playerUserId = Guid.NewGuid();
            dbContext.Users.AddRange(
                new ApplicationUser
                {
                    Id = adminUserId,
                    Nome = "Administrador da projecao",
                    UserName = $"projection-admin-{adminUserId:N}",
                    NormalizedUserName = $"PROJECTION-ADMIN-{adminUserId:N}",
                },
                new ApplicationUser
                {
                    Id = playerUserId,
                    Nome = "Jogador da projecao",
                    UserName = $"projection-player-{playerUserId:N}",
                    NormalizedUserName = $"PROJECTION-PLAYER-{playerUserId:N}",
                });
            dbContext.VinculosDiscord.Add(new VinculoDiscord
            {
                UsuarioId = playerUserId,
                DiscordUserId = "discord-user-secreto",
                DiscordUsername = "projection-player",
            });
            var jogador = new Jogador(
                "Jogador da projecao",
                null,
                "projection#1234",
                null,
                null,
                null,
                Elo.Ouro,
                Divisao.II,
                new[]
                {
                    new PreferenciaRota(Rota.Top, 1, false),
                    new PreferenciaRota(Rota.Jungle, 2, false),
                    new PreferenciaRota(Rota.Mid, 3, false),
                    new PreferenciaRota(Rota.Adc, 4, false),
                    new PreferenciaRota(Rota.Support, 5, false),
                });
            jogador.VincularUsuario(playerUserId);
            var draft = new DraftMontagem("Draft de projecao", "observacao publica", 5, DraftMontagemCriterioCapitaes.Manual, [], []);
            draft.ConfirmarPresenca(playerUserId, jogador.Id, "discord-user-secreto", DraftMontagemPresencaOrigem.Discord);
            var claimId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;
            draft.IniciarTentativaPublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.Presenca, "guild-secreta", "channel-secreto", claimId, now.AddMinutes(5), now);
            draft.RegistrarPublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.Presenca, claimId, "guild-secreta", "channel-secreto", "message-secreta", now.AddMinutes(1));
            draft.SolicitarRepublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.Presenca, adminUserId, "motivo administrativo", now.AddMinutes(2), confirmarAusenciaPublicacao: true);
            dbContext.Jogadores.Add(jogador);
            dbContext.DraftMontagens.Add(draft);
            await dbContext.SaveChangesAsync();
            return (draft.Id, adminUserId, playerUserId);
        }

        public async Task<(Guid DraftId, Guid ExecutorUserId, Guid TargetPlayerId)> SeedAdministrativeDraftAsync(bool confirmed = false)
        {
            _ = CreateClient();
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
            var executorUserId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            dbContext.Users.AddRange(
                new ApplicationUser
                {
                    Id = executorUserId,
                    Nome = "Organizador",
                    UserName = $"admin-{executorUserId:N}",
                    NormalizedUserName = $"ADMIN-{executorUserId:N}",
                },
                new ApplicationUser
                {
                    Id = targetUserId,
                    Nome = "Jogador alvo",
                    UserName = $"target-{targetUserId:N}",
                    NormalizedUserName = $"TARGET-{targetUserId:N}",
                });
            var jogador = new Jogador(
                "Jogador alvo",
                null,
                "target#1234",
                null,
                null,
                null,
                Elo.Ouro,
                Divisao.II,
                new[]
                {
                    new PreferenciaRota(Rota.Top, 1, false),
                    new PreferenciaRota(Rota.Jungle, 2, false),
                    new PreferenciaRota(Rota.Mid, 3, false),
                    new PreferenciaRota(Rota.Adc, 4, false),
                    new PreferenciaRota(Rota.Support, 5, false),
                });
            jogador.VincularUsuario(targetUserId);
            var draft = new DraftMontagem("Draft administrativo", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
            if (confirmed)
            {
                draft.ConfirmarPresenca(targetUserId, jogador.Id, null, DraftMontagemPresencaOrigem.Web);
            }
            dbContext.Jogadores.Add(jogador);
            dbContext.DraftMontagens.Add(draft);
            await dbContext.SaveChangesAsync();
            return (draft.Id, executorUserId, jogador.Id);
        }

        public async Task<(int Presences, int Actions, DraftMontagemStatus Status)> GetAdministrativeMutationStateAsync(Guid draftId)
        {
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
            var presences = await dbContext.DraftMontagemPresencas.AsNoTracking().CountAsync(item => item.DraftMontagemId == draftId);
            var actions = await dbContext.DraftMontagemAcoesAdministrativas.AsNoTracking().CountAsync(item => item.DraftMontagemId == draftId);
            var status = await dbContext.DraftMontagens.AsNoTracking().Where(item => item.Id == draftId).Select(item => item.Status).SingleAsync();
            return (presences, actions, status);
        }

        public async Task<(string Type, Guid ExecutorUserId, Guid TargetPlayerId, string? Reason)> GetAdministrativeActionAsync(Guid draftId)
        {
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
            return await dbContext.DraftMontagemAcoesAdministrativas
                .AsNoTracking()
                .Where(item => item.DraftMontagemId == draftId)
                .Select(item => new ValueTuple<string, Guid, Guid, string?>(item.Tipo, item.ResponsavelUsuarioId, item.JogadorAlvoId!.Value, item.Motivo))
                .SingleAsync();
        }

        public async Task<(string Status, Guid? ClaimId)> GetPublicationClaimStateAsync(Guid draftId)
        {
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
            return await dbContext.DraftMontagemPublicacoesDiscord
                .AsNoTracking()
                .Where(item => item.DraftMontagemId == draftId && item.Tipo == DraftMontagemPublicacaoDiscordTipo.Presenca)
                .Select(item => new ValueTuple<string, Guid?>(item.Status.ToString(), item.ClaimId))
                .SingleAsync();
        }

        public async Task<int> GetConfirmedPresenceCountByPlayerAsync(Guid draftId, Guid playerId)
        {
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
            return await dbContext.DraftMontagemPresencas.AsNoTracking().CountAsync(
                item => item.DraftMontagemId == draftId && item.JogadorId == playerId && item.Status == DraftMontagemPresencaStatus.Confirmada);
        }

        public async Task<int> GetPublicationCountAsync(Guid draftId)
        {
            await using var scope = Services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>()
                .DraftMontagemPublicacoesDiscord.AsNoTracking().CountAsync(item => item.DraftMontagemId == draftId);
        }

        public async Task<(Guid DraftId, Guid UserId)> SeedPresenceAsync(bool confirmed = false)
        {
            _ = CreateClient();
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
            var userId = Guid.NewGuid();
            dbContext.Users.Add(new ApplicationUser
            {
                Id = userId,
                Nome = "Usuario de presenca",
                UserName = $"presence-test-{userId:N}",
                NormalizedUserName = $"PRESENCE-TEST-{userId:N}",
                Email = $"presence-test-{userId:N}@example.com",
                NormalizedEmail = $"PRESENCE-TEST-{userId:N}@EXAMPLE.COM",
            });
            var jogador = new Jogador(
                "Jogador de presenca",
                null,
                "presence#1234",
                null,
                null,
                null,
                Elo.Ouro,
                Divisao.II,
                new[]
                {
                    new PreferenciaRota(Rota.Top, 1, false),
                    new PreferenciaRota(Rota.Jungle, 2, false),
                    new PreferenciaRota(Rota.Mid, 3, false),
                    new PreferenciaRota(Rota.Adc, 4, false),
                    new PreferenciaRota(Rota.Support, 5, false),
                });
            jogador.VincularUsuario(userId);
            var draft = new DraftMontagem("Draft de presenca", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
            if (confirmed)
            {
                draft.ConfirmarPresenca(userId, jogador.Id, null, DraftMontagemPresencaOrigem.Web);
            }
            dbContext.Jogadores.Add(jogador);
            dbContext.DraftMontagens.Add(draft);
            await dbContext.SaveChangesAsync();
            return (draft.Id, userId);
        }

        public async Task<(Guid DraftId, Guid AdminUserId, IReadOnlyList<(Guid UserId, Guid PlayerId)> Players)> SeedDraftJourneyAsync(DateTimeOffset? presenceDeadline = null)
        {
            _ = CreateClient();
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
            var adminUserId = Guid.NewGuid();
            dbContext.Users.Add(new ApplicationUser
            {
                Id = adminUserId,
                Nome = "Administrador da jornada",
                UserName = $"journey-admin-{adminUserId:N}",
                NormalizedUserName = $"JOURNEY-ADMIN-{adminUserId:N}",
            });
            var players = new List<(Guid UserId, Guid PlayerId)>();
            for (var index = 1; index <= 20; index++)
            {
                var userId = Guid.NewGuid();
                var player = new Jogador(
                    $"Jogador {index}",
                    null,
                    $"journey{index}#1234",
                    null,
                    null,
                    null,
                    Elo.Ouro,
                    Divisao.II,
                    new[]
                    {
                        new PreferenciaRota(Rota.Top, 1, false),
                        new PreferenciaRota(Rota.Jungle, 2, false),
                        new PreferenciaRota(Rota.Mid, 3, false),
                        new PreferenciaRota(Rota.Adc, 4, false),
                        new PreferenciaRota(Rota.Support, 5, false),
                    });
                player.VincularUsuario(userId);
                dbContext.Users.Add(new ApplicationUser
                {
                    Id = userId,
                    Nome = $"Jogador {index}",
                    UserName = $"journey-player-{index}-{userId:N}",
                    NormalizedUserName = $"JOURNEY-PLAYER-{index}-{userId:N}",
                });
                dbContext.Jogadores.Add(player);
                players.Add((userId, player.Id));
            }

            var draft = new DraftMontagem("Jornada de 19 e 20 jogadores", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
            typeof(DraftMontagem)
                .GetProperty(nameof(DraftMontagem.CicloVersao))!
                .SetValue(draft, DraftMontagemCicloVersao.Legado);
            if (presenceDeadline is not null)
            {
                draft.ConfigurarEncerramentoPresenca(presenceDeadline.Value);
            }
            dbContext.DraftMontagens.Add(draft);
            await dbContext.SaveChangesAsync();
            return (draft.Id, adminUserId, players);
        }

        public async Task<(DraftMontagemStatus Status, DateTimeOffset? PresenceDeadline, int ConfirmedPresences, int ReopenActions)> GetReopenStateAsync(Guid draftId)
        {
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
            var draft = await dbContext.DraftMontagens.AsNoTracking()
                .Where(item => item.Id == draftId)
                .Select(item => new { item.Status, item.HorarioEncerramentoPresenca })
                .SingleAsync();
            var confirmedPresences = await dbContext.DraftMontagemPresencas.AsNoTracking()
                .CountAsync(item => item.DraftMontagemId == draftId && item.Status == DraftMontagemPresencaStatus.Confirmada);
            var reopenActions = await dbContext.DraftMontagemAcoesAdministrativas.AsNoTracking()
                .CountAsync(item => item.DraftMontagemId == draftId && item.Tipo == "ReaberturaPresenca");
            return (draft.Status, draft.HorarioEncerramentoPresenca, confirmedPresences, reopenActions);
        }

        public async Task RunPresenceClosureCycleAsync()
        {
            var service = Services.GetServices<IHostedService>().OfType<DraftMontagemPresenceClosureService>().Single();
            await service.RunCycleAsync(CancellationToken.None);
        }

        public async Task<int> CountConfirmedPresencesAsync(Guid draftId, Guid userId)
        {
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
            return await dbContext.DraftMontagemPresencas
                .AsNoTracking()
                .CountAsync(presence => presence.DraftMontagemId == draftId
                    && presence.UsuarioId == userId
                    && presence.Status == DraftMontagemPresencaStatus.Confirmada);
        }

        public async Task<DraftMontagemPresencaStatus?> GetPresenceStatusAsync(Guid draftId, Guid userId)
        {
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
            return await dbContext.DraftMontagemPresencas
                .AsNoTracking()
                .Where(presence => presence.DraftMontagemId == draftId && presence.UsuarioId == userId)
                .Select(presence => (DraftMontagemPresencaStatus?)presence.Status)
                .SingleOrDefaultAsync();
        }

        public async Task SaveDuplicateUserNamesThroughDraftRepositoryAsync()
        {
            _ = CreateClient();
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
            var repository = scope.ServiceProvider.GetRequiredService<RinhaDasLendas.Domain.Repositories.IDraftMontagemRepository>();
            var normalizedName = $"DUPLICATE-{Guid.NewGuid():N}";
            dbContext.Users.AddRange(
                new ApplicationUser { Id = Guid.NewGuid(), Nome = "Primeiro", UserName = normalizedName, NormalizedUserName = normalizedName },
                new ApplicationUser { Id = Guid.NewGuid(), Nome = "Segundo", UserName = normalizedName, NormalizedUserName = normalizedName });

            await repository.TrySaveChangesAsync(CancellationToken.None);
        }

        public async Task<(bool OriginalDetached, bool DifferentInstance, DateTimeOffset? ReloadedClosingTime)> MutateWithoutSavingAndReloadAsync(Guid draftId)
        {
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
            var repository = scope.ServiceProvider.GetRequiredService<IDraftMontagemRepository>();
            var original = await repository.GetByIdAsync(draftId, CancellationToken.None)
                ?? throw new InvalidOperationException("Draft test fixture was not found.");
            original.ConfigurarEncerramentoPresenca(DateTimeOffset.UtcNow.AddHours(1));

            var reloaded = await repository.ReloadByIdAsync(draftId, CancellationToken.None)
                ?? throw new InvalidOperationException("Draft test fixture was not reloaded.");

            return (
                dbContext.Entry(original).State == EntityState.Detached,
                !ReferenceEquals(original, reloaded),
                reloaded.HorarioEncerramentoPresenca);
        }

        public async Task<Guid> SeedPendingPublicationAsync(DateTimeOffset? presenceDeadline = null)
        {
            _ = CreateClient();
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
            var responsibleUserId = await dbContext.Users.Select(user => user.Id).FirstOrDefaultAsync();
            if (responsibleUserId == Guid.Empty)
            {
                responsibleUserId = Guid.NewGuid();
                dbContext.Users.Add(new ApplicationUser
                {
                    Id = responsibleUserId,
                    Nome = "Usuario de teste",
                    UserName = $"draft-test-{responsibleUserId:N}",
                    NormalizedUserName = $"DRAFT-TEST-{responsibleUserId:N}",
                    Email = $"draft-test-{responsibleUserId:N}@example.com",
                    NormalizedEmail = $"DRAFT-TEST-{responsibleUserId:N}@EXAMPLE.COM",
                });
                await dbContext.SaveChangesAsync();
            }
            var draft = new DraftMontagem("Draft de teste", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
            draft.ConfigurarEncerramentoPresenca(presenceDeadline ?? DateTimeOffset.UtcNow.AddHours(1));
            draft.SolicitarRepublicacaoDiscord(
                DraftMontagemPublicacaoDiscordTipo.Presenca,
                responsibleUserId,
                "Preparar publicacao para teste",
                DateTimeOffset.UtcNow);
            dbContext.DraftMontagens.Add(draft);
            await dbContext.SaveChangesAsync();
            return draft.Id;
        }

        public async Task<Guid> SeedDraftWithoutPublicationAsync()
        {
            _ = CreateClient();
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
            var draft = new DraftMontagem("Draft sem publicacao", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
            draft.ConfigurarEncerramentoPresenca(DateTimeOffset.UtcNow.AddHours(1));
            dbContext.DraftMontagens.Add(draft);
            await dbContext.SaveChangesAsync();
            return draft.Id;
        }

        public async Task<Guid> SeedTerminalDraftAsync(DraftMontagemStatus status, bool withPendingTeamsPublication)
        {
            _ = CreateClient();
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
            var draft = new DraftMontagem("Draft finalizado operacional", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
            if (withPendingTeamsPublication)
            {
                var responsibleUserId = Guid.NewGuid();
                dbContext.Users.Add(new ApplicationUser
                {
                    Id = responsibleUserId,
                    Nome = "Operador Discord",
                    UserName = $"discord-operator-{responsibleUserId:N}",
                    NormalizedUserName = $"DISCORD-OPERATOR-{responsibleUserId:N}",
                });
                draft.SolicitarRepublicacaoDiscord(
                    DraftMontagemPublicacaoDiscordTipo.TimesDefinidos,
                    responsibleUserId,
                    "Republicar times",
                    DateTimeOffset.UtcNow.AddDays(-2));
            }
            dbContext.DraftMontagens.Add(draft);
            await dbContext.SaveChangesAsync();
            await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE draft_montagens
                SET status = {status.ToString()}, discord_guild_id = NULL, data_atualizacao = {DateTimeOffset.UtcNow.AddDays(-2)}
                WHERE id = {draft.Id}
                """);
            return draft.Id;
        }

        public async Task<Guid> SeedOldPendingPublicationAfterRecentDraftsAsync(int recentDraftCount)
        {
            var oldPendingDraftId = await SeedTerminalDraftAsync(DraftMontagemStatus.Finalizada, withPendingTeamsPublication: true);
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
            var recentDrafts = Enumerable.Range(1, recentDraftCount)
                .Select(index => new DraftMontagem($"Draft recente {index}", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []))
                .ToArray();
            dbContext.DraftMontagens.AddRange(recentDrafts);
            await dbContext.SaveChangesAsync();
            return oldPendingDraftId;
        }

        public async Task<IReadOnlyCollection<DraftMontagem>> ListActiveForDiscordAsync()
        {
            await using var scope = Services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<IDraftMontagemRepository>()
                .ListActiveForDiscordAsync(CancellationToken.None);
        }

        public async Task ExpireClaimAsync(Guid draftId)
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                UPDATE draft_montagem_publicacoes_discord
                SET claim_expira_em = NOW() - INTERVAL '1 minute'
                WHERE draft_montagem_id = @draftId AND tipo = 'Presenca'
                """;
            command.Parameters.AddWithValue("draftId", draftId);
            await command.ExecuteNonQueryAsync();
        }

        public async Task ExpirePresenceWindowAndClaimAsync(Guid draftId)
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                UPDATE draft_montagens
                SET horario_encerramento_presenca = clock_timestamp() - INTERVAL '1 minute'
                WHERE id = @draftId;

                UPDATE draft_montagem_publicacoes_discord
                SET claim_expira_em = clock_timestamp() - INTERVAL '1 minute'
                WHERE draft_montagem_id = @draftId AND tipo = 'Presenca';
                """;
            command.Parameters.AddWithValue("draftId", draftId);
            await command.ExecuteNonQueryAsync();
        }

        public async Task<(string Status, string? ErrorCode, DateTimeOffset? ClaimExpiresAt, string? MessageId)> GetPublicationPersistenceStateAsync(Guid draftId)
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT status, ultimo_erro_codigo, claim_expira_em, message_id
                FROM draft_montagem_publicacoes_discord
                WHERE draft_montagem_id = @draftId AND tipo = 'Presenca'
                """;
            command.Parameters.AddWithValue("draftId", draftId);
            await using var reader = await command.ExecuteReaderAsync();
            (await reader.ReadAsync()).Should().BeTrue();
            return (
                reader.GetString(0),
                reader.IsDBNull(1) ? null : reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetFieldValue<DateTimeOffset>(2),
                reader.IsDBNull(3) ? null : reader.GetString(3));
        }

        public async Task<int> ReconcileExpiredClaimsAsync()
        {
            return (await ReconcileExpiredClaimIdsAsync()).Count;
        }

        public async Task<IReadOnlyCollection<Guid>> ReconcileExpiredClaimIdsAsync()
        {
            await using var scope = Services.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<RinhaDasLendas.Domain.Repositories.IDraftMontagemRepository>();
            return await repository.MarcarPublicacoesExpiradasParaReconciliacaoAsync(DateTimeOffset.UtcNow, CancellationToken.None);
        }

        public async Task<(Guid FirstDraftId, Guid SecondDraftId)> SeedMultipleExpiredPublicationsAsync()
        {
            _ = CreateClient();
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
            var agora = DateTimeOffset.UtcNow.AddMinutes(-10);
            var expiraEm = agora.AddMinutes(5);
            var primeiroDraft = new DraftMontagem("Draft com duas publicacoes", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
            primeiroDraft.ConfigurarEncerramentoPresenca(DateTimeOffset.UtcNow.AddHours(1));
            primeiroDraft.IniciarTentativaPublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.Presenca, null, null, Guid.NewGuid(), expiraEm, agora);
            primeiroDraft.IniciarTentativaPublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.TimesDefinidos, null, null, Guid.NewGuid(), expiraEm, agora);
            var segundoDraft = new DraftMontagem("Draft com uma publicacao", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
            segundoDraft.ConfigurarEncerramentoPresenca(DateTimeOffset.UtcNow.AddHours(1));
            segundoDraft.IniciarTentativaPublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.Presenca, null, null, Guid.NewGuid(), expiraEm, agora);
            dbContext.DraftMontagens.AddRange(primeiroDraft, segundoDraft);
            await dbContext.SaveChangesAsync();
            return (primeiroDraft.Id, segundoDraft.Id);
        }

        public async Task<IReadOnlyCollection<(Guid DraftId, DraftMontagemPublicacaoDiscordTipo Type, DraftMontagemPublicacaoDiscordStatus Status)>> GetPublicationStatesAsync(IReadOnlyCollection<Guid> draftIds)
        {
            await using var scope = Services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>()
                .DraftMontagemPublicacoesDiscord
                .AsNoTracking()
                .Where(publicacao => draftIds.Contains(publicacao.DraftMontagemId))
                .Select(publicacao => new ValueTuple<Guid, DraftMontagemPublicacaoDiscordTipo, DraftMontagemPublicacaoDiscordStatus>(
                    publicacao.DraftMontagemId,
                    publicacao.Tipo,
                    publicacao.Status))
                .ToListAsync();
        }

        public async Task<string?> GetPublicationStatusAsync(Guid draftId)
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT status
                FROM draft_montagem_publicacoes_discord
                WHERE draft_montagem_id = @draftId AND tipo = 'Presenca'
                """;
            command.Parameters.AddWithValue("draftId", draftId);
            return (string?)await command.ExecuteScalarAsync();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureServices(services =>
            {
                services.Replace(ServiceDescriptor.Scoped<IDraftMontagemRepository>(serviceProvider =>
                    new CoordinatedDraftMontagemRepository(
                        new DraftMontagemRepository(serviceProvider.GetRequiredService<RinhaDasLendasDbContext>()),
                        _presenceConcurrency)));
                services.Replace(ServiceDescriptor.Scoped<IDraftMontagemRealtimeNotifier>(_ => _presenceEffects));
                services.Replace(ServiceDescriptor.Scoped<IDraftMontagemMetrics>(_ => _presenceEffects));
            });
        }

    }

    private sealed class PresenceConcurrencyCoordinator
    {
        private readonly ConcurrentQueue<long> _loadedVersions = new();
        private readonly ConcurrentQueue<DraftMontagemSaveResultado> _saveResults = new();
        private readonly ConcurrentQueue<int> _saveObservedLoadedCounts = new();
        private readonly object _sync = new();
        private TaskCompletionSource _release = NewCompletionSource();
        private Guid? _draftId;
        private int _loadedCount;

        public IReadOnlyList<long> LoadedVersions => _loadedVersions.ToArray();
        public IReadOnlyList<DraftMontagemSaveResultado> SaveResults => _saveResults.ToArray();
        public IReadOnlyList<int> SaveObservedLoadedCounts => _saveObservedLoadedCounts.ToArray();

        public void Arm(Guid draftId)
        {
            lock (_sync)
            {
                _draftId = draftId;
                _loadedCount = 0;
                _release = NewCompletionSource();
                _loadedVersions.Clear();
                _saveResults.Clear();
                _saveObservedLoadedCounts.Clear();
            }
        }

        public async Task AfterLoadAsync(Guid draftId, long version, CancellationToken cancellationToken)
        {
            Task releaseTask;
            lock (_sync)
            {
                if (_draftId != draftId || _loadedCount >= 2)
                {
                    return;
                }

                _loadedVersions.Enqueue(version);
                _loadedCount++;
                if (_loadedCount == 2)
                {
                    _release.TrySetResult();
                }

                releaseTask = _release.Task;
            }

            await releaseTask.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
        }

        public void RecordSaveResult(DraftMontagemSaveResultado result)
        {
            if (_draftId is not null)
            {
                _saveObservedLoadedCounts.Enqueue(Volatile.Read(ref _loadedCount));
                _saveResults.Enqueue(result);
            }
        }

        private static TaskCompletionSource NewCompletionSource() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class PresenceEffectCounter : IDraftMontagemRealtimeNotifier, IDraftMontagemMetrics
    {
        private int _presenceConfirmed;
        private int _presenceCancelled;
        private int _realtime;

        public int PresenceConfirmed => Volatile.Read(ref _presenceConfirmed);
        public int PresenceCancelled => Volatile.Read(ref _presenceCancelled);
        public int Realtime => Volatile.Read(ref _realtime);

        public void Reset()
        {
            Volatile.Write(ref _presenceConfirmed, 0);
            Volatile.Write(ref _presenceCancelled, 0);
            Volatile.Write(ref _realtime, 0);
        }

        public Task StateUpdatedAsync(Guid draftMontagemId, DraftMontagemRealtimeStateDto state, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _realtime);
            return Task.CompletedTask;
        }

        public Task ArchivedAsync(Guid draftMontagemId, CancellationToken cancellationToken) => Task.CompletedTask;

        public void RecordPresenceConfirmed(Guid draftMontagemId, string origin) => Interlocked.Increment(ref _presenceConfirmed);
        public void RecordPresenceCancelled(Guid draftMontagemId, string origin) => Interlocked.Increment(ref _presenceCancelled);
        public void RecordPresenceClosed(Guid draftMontagemId) { }
        public void RecordDiscordPublication(Guid draftMontagemId, string type, string status) { }
        public void RecordPick(Guid draftMontagemId, string type) { }
        public void RecordDraftTimeout(Guid draftMontagemId) { }
        public void RecordDraftCancelled(Guid draftMontagemId) { }
    }

    private sealed class CoordinatedDraftMontagemRepository(
        IDraftMontagemRepository inner,
        PresenceConcurrencyCoordinator coordinator) : IDraftMontagemRepository
    {
        public Task AddAsync(DraftMontagem montagem, CancellationToken cancellationToken) => inner.AddAsync(montagem, cancellationToken);

        public async Task<DraftMontagem?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            var montagem = await inner.GetByIdAsync(id, cancellationToken);
            if (montagem is not null)
            {
                await coordinator.AfterLoadAsync(id, montagem.VersaoEstado, cancellationToken);
            }

            return montagem;
        }

        public Task<DraftMontagem?> ReloadByIdAsync(Guid id, CancellationToken cancellationToken) => inner.ReloadByIdAsync(id, cancellationToken);
        public Task<DraftMontagem?> GetByIdIncludingArchivedAsync(Guid id, CancellationToken cancellationToken) => inner.GetByIdIncludingArchivedAsync(id, cancellationToken);
        public Task<DraftMontagem?> ReloadByIdIncludingArchivedAsync(Guid id, CancellationToken cancellationToken) => inner.ReloadByIdIncludingArchivedAsync(id, cancellationToken);
        public Task<IReadOnlyCollection<DraftMontagem>> ListExpiredRealtimeAsync(DateTimeOffset now, int limit, CancellationToken cancellationToken) => inner.ListExpiredRealtimeAsync(now, limit, cancellationToken);
        public Task<IReadOnlyCollection<DraftMontagem>> ListExpiredPresenceAsync(DateTimeOffset now, int limit, CancellationToken cancellationToken) => inner.ListExpiredPresenceAsync(now, limit, cancellationToken);
        public Task<IReadOnlyCollection<DraftMontagem>> ListActiveForDiscordAsync(CancellationToken cancellationToken) => inner.ListActiveForDiscordAsync(cancellationToken);
        public Task<IReadOnlyCollection<DraftMontagem>> ListAsync(string? search, DraftMontagemStatus? status, bool includeCancelled, bool includeArchived, int page, int pageSize, CancellationToken cancellationToken) => inner.ListAsync(search, status, includeCancelled, includeArchived, page, pageSize, cancellationToken);
        public Task<int> CountAsync(string? search, DraftMontagemStatus? status, bool includeCancelled, bool includeArchived, CancellationToken cancellationToken) => inner.CountAsync(search, status, includeCancelled, includeArchived, cancellationToken);
        public Task<IReadOnlyCollection<Jogador>> GetJogadoresByIdsAsync(IReadOnlyCollection<Guid> jogadoresIds, CancellationToken cancellationToken) => inner.GetJogadoresByIdsAsync(jogadoresIds, cancellationToken);
        public Task<IReadOnlyCollection<Guid>> GetCapitaesElegiveisIdsAsync(IReadOnlyCollection<Guid> jogadoresIds, CancellationToken cancellationToken) => inner.GetCapitaesElegiveisIdsAsync(jogadoresIds, cancellationToken);
        public Task<Jogador?> GetJogadorByUsuarioIdAsync(Guid usuarioId, CancellationToken cancellationToken) => inner.GetJogadorByUsuarioIdAsync(usuarioId, cancellationToken);
        public Task<IReadOnlyCollection<Jogador>> SearchJogadoresElegiveisParaPresencaManualAsync(Guid draftMontagemId, string? search, int page, int pageSize, CancellationToken cancellationToken) => inner.SearchJogadoresElegiveisParaPresencaManualAsync(draftMontagemId, search, page, pageSize, cancellationToken);
        public Task<int> CountJogadoresElegiveisParaPresencaManualAsync(Guid draftMontagemId, string? search, CancellationToken cancellationToken) => inner.CountJogadoresElegiveisParaPresencaManualAsync(draftMontagemId, search, cancellationToken);
        public Task<DraftMontagemPublicacaoClaim?> TryClaimPublicacaoDiscordAsync(Guid draftMontagemId, DraftMontagemPublicacaoDiscordTipo tipo, Guid claimId, DateTimeOffset expiraEm, DateTimeOffset agora, CancellationToken cancellationToken) => inner.TryClaimPublicacaoDiscordAsync(draftMontagemId, tipo, claimId, expiraEm, agora, cancellationToken);
        public Task<bool> TryConcluirPublicacaoDiscordAsync(Guid draftMontagemId, DraftMontagemPublicacaoDiscordTipo tipo, Guid claimId, string? guildId, string? channelId, string messageId, DateTimeOffset agora, CancellationToken cancellationToken) => inner.TryConcluirPublicacaoDiscordAsync(draftMontagemId, tipo, claimId, guildId, channelId, messageId, agora, cancellationToken);
        public Task<bool> TryRegistrarFalhaPublicacaoDiscordAsync(Guid draftMontagemId, DraftMontagemPublicacaoDiscordTipo tipo, Guid claimId, string? guildId, string? channelId, string? erroCodigo, DateTimeOffset agora, CancellationToken cancellationToken) => inner.TryRegistrarFalhaPublicacaoDiscordAsync(draftMontagemId, tipo, claimId, guildId, channelId, erroCodigo, agora, cancellationToken);
        public Task<IReadOnlyCollection<Guid>> MarcarPublicacoesExpiradasParaReconciliacaoAsync(DateTimeOffset agora, CancellationToken cancellationToken) => inner.MarcarPublicacoesExpiradasParaReconciliacaoAsync(agora, cancellationToken);

        public async Task<DraftMontagemSaveResultado> TrySaveChangesAsync(CancellationToken cancellationToken)
        {
            var result = await inner.TrySaveChangesAsync(cancellationToken);
            coordinator.RecordSaveResult(result);
            return result;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => inner.SaveChangesAsync(cancellationToken);
    }
}
