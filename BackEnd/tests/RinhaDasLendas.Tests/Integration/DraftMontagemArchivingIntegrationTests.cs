using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RinhaDasLendas.Api.Filters;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Infrastructure.Identity;
using RinhaDasLendas.Infrastructure.Persistence;
using RinhaDasLendas.Infrastructure.Repositories;
using RinhaDasLendas.Tests.Infrastructure;

namespace RinhaDasLendas.Tests.Integration;

public sealed class DraftMontagemArchivingIntegrationTests
{
    [Fact]
    public async Task RepublicacaoGenerica_NaoDevePermitirCancelamentoParaModeradorEnquantoEndpointDedicadoPermiteAdmin()
    {
        await using var factory = new ArchivingApiFactory();
        var fixture = await factory.SeedActiveDraftAsync();
        using var admin = factory.CreateAdminClient(fixture.UserId);
        var archiveResponse = await admin.PatchAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/arquivar",
            new ArquivarDraftMontagemRequestDto("motivo", fixture.Version));
        var archived = (await archiveResponse.Content.ReadFromJsonAsync<DraftMontagemArquivamentoResultadoDto>())!;
        (await admin.PatchAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/restaurar",
            new RestaurarDraftMontagemRequestDto(archived.VersaoEstado))).EnsureSuccessStatusCode();
        using var moderator = factory.CreateModeratorClient(fixture.UserId);

        var generic = await moderator.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/discord/publicacoes/republicar",
            new RepublicarPublicacaoDiscordDraftMontagemRequestDto(
                DraftMontagemPublicacaoDiscordTipo.Cancelamento,
                "tentativa indevida"));

        generic.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = (await generic.Content.ReadFromJsonAsync<ApiErrorResponse>())!;
        error.Errors.Should().ContainSingle("Use o endpoint administrativo de cancelamento para republicar esta publicação");

        moderator.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US");
        var genericEnglish = await moderator.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/discord/publicacoes/republicar",
            new RepublicarPublicacaoDiscordDraftMontagemRequestDto(
                DraftMontagemPublicacaoDiscordTipo.Cancelamento,
                "invalid retry"));
        genericEnglish.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await genericEnglish.Content.ReadFromJsonAsync<ApiErrorResponse>())!.Errors.Should().ContainSingle(
            "Use the administrative cancellation endpoint to republish this publication");

        var dedicatedAsModerator = await moderator.PostAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/discord/publicacoes/cancelamento/republicar",
            null);
        dedicatedAsModerator.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var dedicatedAsAdmin = await admin.PostAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/discord/publicacoes/cancelamento/republicar",
            null);
        dedicatedAsAdmin.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task FalhaDePersistenciaAposMutacao_DeveManterTodoEstadoAnteriorSemEfeitosParciais()
    {
        await using var factory = new ArchivingApiFactory();
        var fixture = await factory.SeedActiveDraftAsync(withExistingCancellation: true);
        var before = await factory.GetArchiveStateAsync(fixture.DraftId);
        using var admin = factory.CreateAdminClient(fixture.UserId);

        var response = await admin.PatchAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/arquivar",
            new ArquivarDraftMontagemRequestDto("nao deve persistir", fixture.Version));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var after = await factory.GetArchiveStateAsync(fixture.DraftId);
        after.Status.Should().Be(before.Status);
        after.Archived.Should().BeFalse();
        after.ArchivedAt.Should().BeNull();
        after.ArchivedBy.Should().BeNull();
        after.Reason.Should().BeNull();
        after.ActionTypes.Should().BeEquivalentTo(before.ActionTypes);
        after.Publications.Should().BeEquivalentTo(before.Publications);
    }

    [Fact]
    public async Task DraftArquivado_DeveFicarOcultoDeTotaisBuscaStatusDetalheEOperacao()
    {
        await using var factory = new ArchivingApiFactory();
        var fixture = await factory.SeedActiveDraftAsync();
        using var admin = factory.CreateAdminClient(fixture.UserId);
        (await admin.PatchAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/arquivar",
            new ArquivarDraftMontagemRequestDto("ocultar", fixture.Version))).EnsureSuccessStatusCode();

        foreach (var route in new[]
        {
            "/api/v1/draft-montagens?includeCancelled=true",
            "/api/v1/draft-montagens?includeCancelled=true&search=Draft%20para%20arquivar",
            "/api/v1/draft-montagens?status=Cancelada",
        })
        {
            var page = await admin.GetFromJsonAsync<PaginatedResponseDto<DraftMontagemResumoDto>>(route);
            page!.TotalItems.Should().Be(0);
            page.Items.Should().BeEmpty();
        }

        var detail = await admin.GetAsync($"/api/v1/draft-montagens/{fixture.DraftId}");
        await AssertNotFoundAsync(detail);
        var operation = await admin.PatchAsync($"/api/v1/draft-montagens/{fixture.DraftId}/finalizar", null);
        await AssertNotFoundAsync(operation);
    }

    [Fact]
    public async Task ArquivamentosHttpConcorrentesParaMesmoEstado_DevemConvergirEm200PreservandoPrimeiroAutorEMotivo()
    {
        await using var factory = new ArchivingApiFactory();
        var fixture = await factory.SeedActiveDraftAsync();
        var secondUserId = await factory.SeedAdminAsync();
        using var first = factory.CreateAdminClient(fixture.UserId);
        using var second = factory.CreateAdminClient(secondUserId);

        var responses = await Task.WhenAll(
            first.PatchAsJsonAsync(
                $"/api/v1/draft-montagens/{fixture.DraftId}/arquivar",
                new ArquivarDraftMontagemRequestDto("primeiro", fixture.Version)),
            second.PatchAsJsonAsync(
                $"/api/v1/draft-montagens/{fixture.DraftId}/arquivar",
                new ArquivarDraftMontagemRequestDto("segundo", fixture.Version)));

        responses.Should().OnlyContain(response => response.StatusCode == HttpStatusCode.OK);
        var state = await factory.GetArchiveStateAsync(fixture.DraftId);
        var firstWon = state.Reason == "primeiro" && state.ArchivedBy == fixture.UserId;
        var secondWon = state.Reason == "segundo" && state.ArchivedBy == secondUserId;
        (firstWon || secondWon).Should().BeTrue();
        state.ActionTypes.Should().BeEquivalentTo(["CancelamentoPorArquivamento", "Arquivamento"]);
        state.Publications.Should().ContainSingle(item => item.Type == DraftMontagemPublicacaoDiscordTipo.Cancelamento);
    }

    [Fact]
    public async Task Arquivamento_DevePersistirEstadoAuditoriasEPublicacaoAtomicamenteEOcultarDaListaNormal()
    {
        await using var factory = new ArchivingApiFactory();
        var fixture = await factory.SeedActiveDraftAsync();
        using var client = factory.CreateAdminClient(fixture.UserId);

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/arquivar",
            new ArquivarDraftMontagemRequestDto(" motivo atomico ", fixture.Version));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var state = await factory.GetArchiveStateAsync(fixture.DraftId);
        state.Status.Should().Be(DraftMontagemStatus.Cancelada);
        state.Archived.Should().BeTrue();
        state.Reason.Should().Be("motivo atomico");
        state.ActionTypes.Should().BeEquivalentTo(["CancelamentoPorArquivamento", "Arquivamento"]);
        state.Publications.Should().ContainSingle().Which.Should().Be(
            (DraftMontagemPublicacaoDiscordTipo.Cancelamento, DraftMontagemPublicacaoDiscordStatus.Pendente));

        var list = await client.GetFromJsonAsync<PaginatedResponseDto<DraftMontagemResumoDto>>("/api/v1/draft-montagens?includeCancelled=true");
        list!.Items.Should().NotContain(item => item.Id == fixture.DraftId);
    }

    [Fact]
    public async Task DoisArquivamentosComMesmaVersao_DevemPersistirSomentePrimeiroMotivoEConjuntoDeEventos()
    {
        await using var factory = new ArchivingApiFactory();
        var fixture = await factory.SeedActiveDraftAsync();
        await using var firstContext = factory.CreateContext();
        await using var secondContext = factory.CreateContext();
        var firstRepository = new DraftMontagemRepository(firstContext);
        var secondRepository = new DraftMontagemRepository(secondContext);
        var first = (await firstRepository.GetByIdIncludingArchivedAsync(fixture.DraftId, CancellationToken.None))!;
        var second = (await secondRepository.GetByIdIncludingArchivedAsync(fixture.DraftId, CancellationToken.None))!;
        var now = DateTimeOffset.UtcNow;
        first.Arquivar("primeiro", fixture.UserId, now);
        second.Arquivar("segundo", fixture.UserId, now.AddSeconds(1));

        (await firstRepository.TrySaveChangesAsync(CancellationToken.None)).Should().Be(DraftMontagemSaveResultado.Persistido);
        (await secondRepository.TrySaveChangesAsync(CancellationToken.None)).Should().Be(DraftMontagemSaveResultado.ConflitoDeVersao);

        var state = await factory.GetArchiveStateAsync(fixture.DraftId);
        state.Reason.Should().Be("primeiro");
        state.ActionTypes.Should().HaveCount(2);
        state.Publications.Should().ContainSingle();
    }

    [Fact]
    public async Task RestauracaoComVersaoAnteriorAoArquivamento_DeveRetornarConflito()
    {
        await using var factory = new ArchivingApiFactory();
        var fixture = await factory.SeedActiveDraftAsync();
        using var client = factory.CreateAdminClient(fixture.UserId);
        (await client.PatchAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/arquivar",
            new ArquivarDraftMontagemRequestDto("motivo", fixture.Version))).EnsureSuccessStatusCode();

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/restaurar",
            new RestaurarDraftMontagemRequestDto(fixture.Version));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadFromJsonAsync<ApiErrorResponse>())!.MessageCode.Should().Be(MessageCodes.DraftStateConflict);
    }

    [Fact]
    public async Task ArquivamentoConcorrenteComOperacao_DeveFazerOperacaoPerderPorConflitoSemEstadoHibrido()
    {
        await using var factory = new ArchivingApiFactory();
        var fixture = await factory.SeedActiveDraftAsync();
        await using var archiveContext = factory.CreateContext();
        await using var operationContext = factory.CreateContext();
        var archiveRepository = new DraftMontagemRepository(archiveContext);
        var operationRepository = new DraftMontagemRepository(operationContext);
        var archiveDraft = (await archiveRepository.GetByIdIncludingArchivedAsync(fixture.DraftId, CancellationToken.None))!;
        var operationDraft = (await operationRepository.GetByIdAsync(fixture.DraftId, CancellationToken.None))!;
        archiveDraft.Arquivar("corrida", fixture.UserId, DateTimeOffset.UtcNow);
        operationDraft.ConfigurarEncerramentoPresenca(DateTimeOffset.UtcNow.AddHours(2));

        (await archiveRepository.TrySaveChangesAsync(CancellationToken.None)).Should().Be(DraftMontagemSaveResultado.Persistido);
        var operationSave = () => operationRepository.SaveChangesAsync(CancellationToken.None);
        await operationSave.Should().ThrowAsync<DomainException>().WithMessage(MessageCodes.DraftStateConflict);

        var state = await factory.GetArchiveStateAsync(fixture.DraftId);
        state.Archived.Should().BeTrue();
        state.Status.Should().Be(DraftMontagemStatus.Cancelada);
    }

    [Fact]
    public async Task CancelamentoPendente_DeveContinuarEntregavelAposRestauracaoSemReativarPublicacoesOperacionais()
    {
        await using var factory = new ArchivingApiFactory();
        var fixture = await factory.SeedActiveDraftAsync(withPendingPresence: true);
        using var admin = factory.CreateAdminClient(fixture.UserId);
        var archiveResponse = await admin.PatchAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/arquivar",
            new ArquivarDraftMontagemRequestDto("motivo", fixture.Version));
        var archived = (await archiveResponse.Content.ReadFromJsonAsync<DraftMontagemArquivamentoResultadoDto>())!;
        (await admin.PatchAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/restaurar",
            new RestaurarDraftMontagemRequestDto(archived.VersaoEstado))).EnsureSuccessStatusCode();

        using var bot = factory.CreateBotClient();
        var active = await bot.GetFromJsonAsync<IReadOnlyCollection<DraftMontagemDiscordOperationalDto>>("/api/v1/draft-montagens/ativos");
        var candidate = active!.Should().ContainSingle(item => item.Id == fixture.DraftId).Which;
        candidate.Arquivado.Should().BeFalse();
        candidate.Status.Should().Be(DraftMontagemStatus.Cancelada.ToString());
        candidate.PublicacoesDiscord.Should().ContainSingle().Which.Tipo.Should().Be(DraftMontagemPublicacaoDiscordTipo.Cancelamento.ToString());

        var operationalClaimResponse = await bot.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/discord/publicacoes/claim",
            new { Tipo = DraftMontagemPublicacaoDiscordTipo.Presenca.ToString() });
        operationalClaimResponse.EnsureSuccessStatusCode();
        (await operationalClaimResponse.Content.ReadFromJsonAsync<ClaimPublicacaoDiscordResponseDto>())!.Adquirido.Should().BeFalse();

        var claimResponse = await bot.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/discord/publicacoes/claim",
            new { Tipo = DraftMontagemPublicacaoDiscordTipo.Cancelamento.ToString() });
        claimResponse.EnsureSuccessStatusCode();
        var claim = (await claimResponse.Content.ReadFromJsonAsync<ClaimPublicacaoDiscordResponseDto>())!;
        claim.Adquirido.Should().BeTrue();

        var complete = await bot.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/discord/publicacao",
            new RegistrarPublicacaoDiscordDraftMontagemRequestDto(
                DraftMontagemPublicacaoDiscordTipo.Cancelamento.ToString(), claim.ClaimId!.Value, "guild", "channel", "message"));
        complete.StatusCode.Should().Be(HttpStatusCode.OK);
        (await factory.GetCancellationPublicationStatusAsync(fixture.DraftId)).Should().Be(DraftMontagemPublicacaoDiscordStatus.Publicada);
    }

    [Fact]
    public async Task CancelamentoRestaurado_DeveAceitarFalhaRepublicacaoENovoClaim()
    {
        await using var factory = new ArchivingApiFactory();
        var fixture = await factory.SeedActiveDraftAsync();
        using var admin = factory.CreateAdminClient(fixture.UserId);
        var archiveResponse = await admin.PatchAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/arquivar",
            new ArquivarDraftMontagemRequestDto("motivo", fixture.Version));
        var archived = (await archiveResponse.Content.ReadFromJsonAsync<DraftMontagemArquivamentoResultadoDto>())!;
        (await admin.PatchAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/restaurar",
            new RestaurarDraftMontagemRequestDto(archived.VersaoEstado))).EnsureSuccessStatusCode();

        using var bot = factory.CreateBotClient();
        var claimResponse = await bot.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/discord/publicacoes/claim",
            new { Tipo = DraftMontagemPublicacaoDiscordTipo.Cancelamento.ToString() });
        var claim = (await claimResponse.Content.ReadFromJsonAsync<ClaimPublicacaoDiscordResponseDto>())!;
        var failure = await bot.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/discord/publicacao/falha",
            new RegistrarFalhaPublicacaoDiscordDraftMontagemRequestDto(
                DraftMontagemPublicacaoDiscordTipo.Cancelamento.ToString(), claim.ClaimId!.Value, "guild", "channel", "temporaria"));
        failure.StatusCode.Should().Be(HttpStatusCode.OK);
        (await factory.GetCancellationPublicationStatusAsync(fixture.DraftId)).Should().Be(DraftMontagemPublicacaoDiscordStatus.Falha);

        var republish = await admin.PostAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/discord/publicacoes/cancelamento/republicar",
            null);
        republish.StatusCode.Should().Be(HttpStatusCode.OK);
        (await factory.GetCancellationPublicationStatusAsync(fixture.DraftId)).Should().Be(DraftMontagemPublicacaoDiscordStatus.Pendente);

        var secondClaim = await bot.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{fixture.DraftId}/discord/publicacoes/claim",
            new { Tipo = DraftMontagemPublicacaoDiscordTipo.Cancelamento.ToString() });
        secondClaim.StatusCode.Should().Be(HttpStatusCode.OK);
        (await secondClaim.Content.ReadFromJsonAsync<ClaimPublicacaoDiscordResponseDto>())!.Adquirido.Should().BeTrue();
    }

    private sealed class ArchivingApiFactory : SecurityApiFactory
    {
        public ArchivingApiFactory() : base(useIsolatedPostgreSql: true)
        {
        }

        public HttpClient CreateAdminClient(Guid userId) => CreateJwtClient(userId, AuthRoles.Admin);

        public HttpClient CreateModeratorClient(Guid userId) => CreateJwtClient(userId, AuthRoles.Moderador);

        public RinhaDasLendasDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<RinhaDasLendasDbContext>().UseNpgsql(ConnectionString).Options;
            return new RinhaDasLendasDbContext(options);
        }

        public async Task<(Guid DraftId, Guid UserId, long Version)> SeedActiveDraftAsync(
            bool withPendingPresence = false,
            bool withExistingCancellation = false)
        {
            _ = CreateClient();
            await using var context = CreateContext();
            var userId = Guid.NewGuid();
            context.Users.Add(new ApplicationUser
            {
                Id = userId,
                Nome = "Administrador",
                UserName = $"archive-{userId:N}",
                NormalizedUserName = $"ARCHIVE-{userId:N}",
            });
            var draft = new DraftMontagem("Draft para arquivar", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
            if (withPendingPresence)
            {
                draft.ConfigurarEncerramentoPresenca(DateTimeOffset.UtcNow.AddHours(1));
                draft.ConfigurarPublicacaoDiscordPendente(
                    DraftMontagemPublicacaoDiscordTipo.Presenca, "guild", "presence-channel", DateTimeOffset.UtcNow);
            }
            if (withExistingCancellation)
            {
                draft.ConfigurarPublicacaoDiscordPendente(
                    DraftMontagemPublicacaoDiscordTipo.Cancelamento, "guild", "cancel-channel", DateTimeOffset.UtcNow);
            }
            context.DraftMontagens.Add(draft);
            await context.SaveChangesAsync();
            return (draft.Id, userId, draft.VersaoEstado);
        }

        public async Task<(DraftMontagemStatus Status, bool Archived, DateTimeOffset? ArchivedAt, Guid? ArchivedBy, string? Reason, IReadOnlyCollection<string> ActionTypes, IReadOnlyCollection<(DraftMontagemPublicacaoDiscordTipo Type, DraftMontagemPublicacaoDiscordStatus Status)> Publications)> GetArchiveStateAsync(Guid draftId)
        {
            await using var context = CreateContext();
            var draft = await context.DraftMontagens.AsNoTracking().SingleAsync(item => item.Id == draftId);
            var actions = await context.DraftMontagemAcoesAdministrativas.AsNoTracking()
                .Where(item => item.DraftMontagemId == draftId).OrderBy(item => item.RegistradoEm).Select(item => item.Tipo).ToListAsync();
            var publications = await context.DraftMontagemPublicacoesDiscord.AsNoTracking()
                .Where(item => item.DraftMontagemId == draftId)
                .Select(item => new ValueTuple<DraftMontagemPublicacaoDiscordTipo, DraftMontagemPublicacaoDiscordStatus>(item.Tipo, item.Status))
                .ToListAsync();
            return (draft.Status, draft.Arquivado, draft.ArquivadoEm, draft.ArquivadoPorUsuarioId, draft.MotivoArquivamento, actions, publications);
        }

        public async Task<Guid> SeedAdminAsync()
        {
            await using var context = CreateContext();
            var userId = Guid.NewGuid();
            context.Users.Add(new ApplicationUser
            {
                Id = userId,
                Nome = "Segundo administrador",
                UserName = $"archive-second-{userId:N}",
                NormalizedUserName = $"ARCHIVE-SECOND-{userId:N}",
            });
            await context.SaveChangesAsync();
            return userId;
        }

        public async Task<DraftMontagemPublicacaoDiscordStatus> GetCancellationPublicationStatusAsync(Guid draftId)
        {
            await using var context = CreateContext();
            return await context.DraftMontagemPublicacoesDiscord.AsNoTracking()
                .Where(item => item.DraftMontagemId == draftId && item.Tipo == DraftMontagemPublicacaoDiscordTipo.Cancelamento)
                .Select(item => item.Status)
                .SingleAsync();
        }
    }

    private static async Task AssertNotFoundAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadFromJsonAsync<ApiErrorResponse>())!.MessageCode.Should().Be(MessageCodes.DraftMontagemNotFound);
    }
}
