using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Moq;
using RinhaDasLendas.Application.Commands.DraftMontagens;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Enums;
using RinhaDasLendas.Application.Handlers.DraftMontagens;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Validators;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Models;
using RinhaDasLendas.Domain.Repositories;
using RinhaDasLendas.Infrastructure.Persistence;
using RinhaDasLendas.Infrastructure.Identity;
using RinhaDasLendas.Tests.Infrastructure;

namespace RinhaDasLendas.Tests.Integration;

public sealed class DraftMontagemPublicationVersionIntegrationTests
{
    [Fact]
    public async Task ClaimConcorrenteDeveIncrementarVersaoUmaVezERetornarStampExatoSomenteAoVencedor()
    {
        await using var factory = new PublicationApiFactory();
        var initial = await factory.SeedPendingPublicationAsync();
        var now = DateTimeOffset.UtcNow;

        var results = await Task.WhenAll(
            factory.ClaimAsync(initial.Id, Guid.NewGuid(), now),
            factory.ClaimAsync(initial.Id, Guid.NewGuid(), now));

        results.Count(result => result.Claim?.Adquirido == true).Should().Be(1);
        results.Count(result => result.VersionStamp is not null).Should().Be(1);
        var persisted = await factory.GetStampAsync(initial.Id);
        persisted.VersaoEstado.Should().Be(initial.VersaoEstado + 1);
        results.Single(result => result.VersionStamp is not null).VersionStamp.Should().Be(persisted);
    }

    [Fact]
    public async Task SucessoEFalhaDevemAtualizarPaiAtomicamenteERetornarStampExato()
    {
        await using var factory = new PublicationApiFactory();
        var successDraft = await factory.SeedPendingPublicationAsync();
        var failureDraft = await factory.SeedPendingPublicationAsync();
        var now = DateTimeOffset.UtcNow;
        var successClaimId = Guid.NewGuid();
        var failureClaimId = Guid.NewGuid();
        var successClaim = await factory.ClaimAsync(successDraft.Id, successClaimId, now);
        var failureClaim = await factory.ClaimAsync(failureDraft.Id, failureClaimId, now);

        var success = await factory.CompleteAsync(successDraft.Id, successClaimId, now.AddSeconds(1));
        var failure = await factory.FailAsync(failureDraft.Id, failureClaimId, now.AddSeconds(1));

        success.Should().Be(await factory.GetStampAsync(successDraft.Id));
        failure.Should().Be(await factory.GetStampAsync(failureDraft.Id));
        success!.VersaoEstado.Should().Be(successClaim.VersionStamp!.VersaoEstado + 1);
        failure!.VersaoEstado.Should().Be(failureClaim.VersionStamp!.VersaoEstado + 1);
        success.DataAtualizacao.Should().BeCloseTo(now.AddSeconds(1), TimeSpan.FromMicroseconds(1));
        failure.DataAtualizacao.Should().BeCloseTo(now.AddSeconds(1), TimeSpan.FromMicroseconds(1));
        (await factory.GetPersistenceStateAsync(successDraft.Id, DraftMontagemPublicacaoDiscordTipo.Presenca)).Status
            .Should().Be(DraftMontagemPublicacaoDiscordStatus.Publicada);
        (await factory.GetPersistenceStateAsync(failureDraft.Id, DraftMontagemPublicacaoDiscordTipo.Presenca)).Status
            .Should().Be(DraftMontagemPublicacaoDiscordStatus.Falha);
    }

    [Fact]
    public async Task TransicoesSemEfeitoDevemRetornarNullSemAlterarVersaoOuData()
    {
        await using var factory = new PublicationApiFactory();
        var initial = await factory.SeedPendingPublicationAsync();
        var before = await factory.GetPersistenceStateAsync(
            initial.Id, DraftMontagemPublicacaoDiscordTipo.Presenca);

        var completion = await factory.CompleteAsync(initial.Id, Guid.NewGuid(), DateTimeOffset.UtcNow);
        var failure = await factory.FailAsync(initial.Id, Guid.NewGuid(), DateTimeOffset.UtcNow);

        completion.Should().BeNull();
        failure.Should().BeNull();
        (await factory.GetPersistenceStateAsync(initial.Id, DraftMontagemPublicacaoDiscordTipo.Presenca))
            .Should().Be(before);
    }

    [Fact]
    public async Task ExpiracaoDeJanelaDuranteClaimDeveRetornarStampDaFalhaTerminal()
    {
        await using var factory = new PublicationApiFactory();
        var initial = await factory.SeedPendingPublicationAsync();
        await factory.ExpirePresenceWindowAsync(initial.Id);

        var result = await factory.ClaimAsync(initial.Id, Guid.NewGuid(), DateTimeOffset.UtcNow);

        result.Claim.Should().NotBeNull();
        result.Claim!.Adquirido.Should().BeFalse();
        result.Claim.Status.Should().Be(DraftMontagemPublicacaoDiscordStatus.Falha);
        result.VersionStamp.Should().Be(await factory.GetStampAsync(initial.Id));
        result.VersionStamp!.VersaoEstado.Should().Be(initial.VersaoEstado + 1);
    }

    [Fact]
    public async Task ReconciliacaoDeExpiradosDeveIncrementarUmaVezPorPaiERetornarStampsExatos()
    {
        await using var factory = new PublicationApiFactory();
        var initial = await factory.SeedClaimedPublicationsAsync();

        var stamps = await factory.ReconcileAsync(DateTimeOffset.UtcNow);

        stamps.Should().ContainSingle();
        stamps.Single().Should().Be(await factory.GetStampAsync(initial.Id));
        stamps.Single().VersaoEstado.Should().Be(initial.VersaoEstado + 1);
        (await factory.ReconcileAsync(DateTimeOffset.UtcNow)).Should().BeEmpty();
        (await factory.GetStampAsync(initial.Id)).Should().Be(stamps.Single());
    }

    [Fact]
    public async Task RepublicacaoDeveRetornarStampExatoENoOpDevePreservarEstado()
    {
        await using var factory = new PublicationApiFactory();
        var draftId = await factory.SeedFailedPublicationAsync();

        var stamp = await factory.RepublishAsync(draftId);
        var persisted = await factory.GetPersistenceStateAsync(
            draftId, DraftMontagemPublicacaoDiscordTipo.Presenca);
        var noOp = await factory.RepublishAsync(draftId);

        stamp.Should().Be(persisted.Stamp);
        noOp.Should().BeNull();
        (await factory.GetPersistenceStateAsync(draftId, DraftMontagemPublicacaoDiscordTipo.Presenca))
            .Should().Be(persisted);
    }

    [Fact]
    public async Task CancelamentoArquivadoClaimSucessoEFalhaDevemAtualizarFilhoEPaiAtomicamente()
    {
        await using var factory = new PublicationApiFactory();
        var successInitial = await factory.SeedArchivedCancellationAsync();
        var failureInitial = await factory.SeedArchivedCancellationAsync();
        var now = DateTimeOffset.UtcNow;
        var successClaimId = Guid.NewGuid();
        var failureClaimId = Guid.NewGuid();

        var successClaim = await factory.ClaimAsync(
            successInitial.Id, successClaimId, now, DraftMontagemPublicacaoDiscordTipo.Cancelamento);
        var failureClaim = await factory.ClaimAsync(
            failureInitial.Id, failureClaimId, now, DraftMontagemPublicacaoDiscordTipo.Cancelamento);

        (await factory.GetPersistenceStateAsync(successInitial.Id, DraftMontagemPublicacaoDiscordTipo.Cancelamento)).Status
            .Should().Be(DraftMontagemPublicacaoDiscordStatus.EmAndamento);
        successClaim.VersionStamp.Should().Be(await factory.GetStampAsync(successInitial.Id));
        failureClaim.VersionStamp.Should().Be(await factory.GetStampAsync(failureInitial.Id));

        var success = await factory.CompleteAsync(
            successInitial.Id, successClaimId, now.AddSeconds(1), DraftMontagemPublicacaoDiscordTipo.Cancelamento);
        var failure = await factory.FailAsync(
            failureInitial.Id, failureClaimId, now.AddSeconds(1), DraftMontagemPublicacaoDiscordTipo.Cancelamento);

        var successState = await factory.GetPersistenceStateAsync(
            successInitial.Id, DraftMontagemPublicacaoDiscordTipo.Cancelamento);
        var failureState = await factory.GetPersistenceStateAsync(
            failureInitial.Id, DraftMontagemPublicacaoDiscordTipo.Cancelamento);
        successState.Status.Should().Be(DraftMontagemPublicacaoDiscordStatus.Publicada);
        failureState.Status.Should().Be(DraftMontagemPublicacaoDiscordStatus.Falha);
        success.Should().Be(successState.Stamp);
        failure.Should().Be(failureState.Stamp);
        success!.VersaoEstado.Should().Be(successInitial.VersaoEstado + 2);
        failure!.VersaoEstado.Should().Be(failureInitial.VersaoEstado + 2);
    }

    [Fact]
    public async Task ReconciliacaoDeCancelamentoArquivadoDeveAtualizarFilhoEPaiUmaVez()
    {
        await using var factory = new PublicationApiFactory();
        var initial = await factory.SeedArchivedCancellationAsync();
        var claim = await factory.ClaimAsync(
            initial.Id,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            DraftMontagemPublicacaoDiscordTipo.Cancelamento);
        await factory.ExpireClaimAsync(initial.Id, DraftMontagemPublicacaoDiscordTipo.Cancelamento);

        var stamps = await factory.ReconcileAsync(DateTimeOffset.UtcNow);

        stamps.Should().ContainSingle().Which.Should().Be(await factory.GetStampAsync(initial.Id));
        stamps.Single().VersaoEstado.Should().Be(claim.VersionStamp!.VersaoEstado + 1);
        (await factory.GetPersistenceStateAsync(initial.Id, DraftMontagemPublicacaoDiscordTipo.Cancelamento)).Status
            .Should().Be(DraftMontagemPublicacaoDiscordStatus.RequerReconciliacao);
        (await factory.ReconcileAsync(DateTimeOffset.UtcNow)).Should().BeEmpty();
    }

    [Fact]
    public async Task FalhaNoUpdateDoPaiDeveReverterAlteracaoDaPublicacao()
    {
        await using var factory = new PublicationApiFactory();
        var initial = await factory.SeedPendingPublicationAsync();
        var claimId = Guid.NewGuid();
        await factory.ClaimAsync(initial.Id, claimId, DateTimeOffset.UtcNow);
        var before = await factory.GetPersistenceStateAsync(
            initial.Id, DraftMontagemPublicacaoDiscordTipo.Presenca);
        await factory.InstallParentUpdateFailureAsync(initial.Id);

        var act = () => factory.CompleteAsync(initial.Id, claimId, DateTimeOffset.UtcNow.AddSeconds(1));

        await act.Should().ThrowAsync<PostgresException>();
        var after = await factory.GetPersistenceStateAsync(
            initial.Id, DraftMontagemPublicacaoDiscordTipo.Presenca);
        after.Should().Be(before);
        after.Status.Should().Be(DraftMontagemPublicacaoDiscordStatus.EmAndamento);
    }

    [Fact]
    public async Task ConflitoENoOpDeRepublicacaoNaoDevemPersistirFilhoAuditoriaVersaoOuEventoParcial()
    {
        await using var factory = new PublicationApiFactory();
        var draftId = await factory.SeedFailedPublicationAsync();

        var result = await factory.ExecuteRepublicationConflictAndNoOpAsync(draftId);

        result.Conflict.Should().BeTrue();
        result.FirstPublishCalls.Should().Be(1);
        result.ConflictPublishCalls.Should().Be(0);
        result.NoOpPublishCalls.Should().Be(0);
        result.AfterConflict.Should().Be(result.AfterFirst);
        result.AfterNoOp.Should().Be(result.AfterFirst);
        result.AfterFirst.Status.Should().Be(DraftMontagemPublicacaoDiscordStatus.Pendente);
        result.AfterFirst.AuditCount.Should().Be(result.Before.AuditCount + 1);
        result.AfterFirst.Stamp.VersaoEstado.Should().Be(result.Before.Stamp.VersaoEstado + 1);
    }

    [Fact]
    public async Task RepublicacaoDeCancelamentoArquivadoDevePersistirUmaVezENoOpDeveManterFilhoPaiAuditoriaEEventos()
    {
        await using var factory = new PublicationApiFactory();
        var initial = await factory.SeedArchivedCancellationAsync(failed: true);

        var result = await factory.ExecuteArchivedCancellationRepublicationAndNoOpAsync(initial.Id);

        result.AfterFirst.Status.Should().Be(DraftMontagemPublicacaoDiscordStatus.Pendente);
        result.AfterFirst.Stamp.VersaoEstado.Should().Be(result.Before.Stamp.VersaoEstado + 1);
        result.AfterFirst.AuditCount.Should().Be(result.Before.AuditCount + 1);
        result.AfterNoOp.Should().Be(result.AfterFirst);
        result.PublishCalls.Should().Be(1);
        result.PublishedScope.Should().Be(DraftMontagemSnapshotScope.IncludingArchived);
        result.PublishedAvailability.Should().Be(DraftMontagemAvailabilityChange.None);
    }

    private sealed class PublicationApiFactory : SecurityApiFactory
    {
        public PublicationApiFactory() : base(useIsolatedPostgreSql: true)
        {
            _ = CreateClient();
        }

        public async Task<DraftMontagemVersionStamp> SeedPendingPublicationAsync()
        {
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
            var draft = new DraftMontagem("Draft de publicacao", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
            draft.ConfigurarEncerramentoPresenca(DateTimeOffset.UtcNow.AddHours(1));
            draft.ConfigurarPublicacaoDiscordPendente(DraftMontagemPublicacaoDiscordTipo.Presenca, null, null, DateTimeOffset.UtcNow);
            dbContext.DraftMontagens.Add(draft);
            await dbContext.SaveChangesAsync();
            return new(draft.Id, draft.VersaoEstado, draft.DataAtualizacao);
        }

        public async Task<DraftMontagemVersionStamp> SeedClaimedPublicationsAsync()
        {
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
            var now = DateTimeOffset.UtcNow.AddMinutes(-10);
            var draft = new DraftMontagem("Draft expirado", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
            draft.ConfigurarEncerramentoPresenca(DateTimeOffset.UtcNow.AddHours(1));
            draft.IniciarTentativaPublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.Presenca, null, null, Guid.NewGuid(), now.AddMinutes(1), now);
            draft.IniciarTentativaPublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.TimesDefinidos, null, null, Guid.NewGuid(), now.AddMinutes(1), now);
            dbContext.DraftMontagens.Add(draft);
            await dbContext.SaveChangesAsync();
            return new(draft.Id, draft.VersaoEstado, draft.DataAtualizacao);
        }

        public async Task<DraftMontagemVersionStamp> SeedArchivedCancellationAsync(bool failed = false)
        {
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
            var user = CreateUser();
            var draft = new DraftMontagem("Draft arquivado", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
            var now = DateTimeOffset.UtcNow;
            draft.Arquivar("motivo", user.Id, now);
            if (failed)
            {
                var claimId = Guid.NewGuid();
                draft.IniciarTentativaPublicacaoDiscord(
                    DraftMontagemPublicacaoDiscordTipo.Cancelamento,
                    null,
                    null,
                    claimId,
                    now.AddMinutes(5),
                    now.AddSeconds(1));
                draft.RegistrarFalhaPublicacaoDiscord(
                    DraftMontagemPublicacaoDiscordTipo.Cancelamento,
                    claimId,
                    null,
                    null,
                    "erro",
                    now.AddSeconds(2));
            }
            dbContext.Users.Add(user);
            dbContext.DraftMontagens.Add(draft);
            await dbContext.SaveChangesAsync();
            return new(draft.Id, draft.VersaoEstado, draft.DataAtualizacao);
        }

        public async Task<Guid> SeedFailedPublicationAsync()
        {
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
            var user = CreateUser();
            var userId = user.Id;
            dbContext.Users.Add(user);
            var now = DateTimeOffset.UtcNow;
            var claimId = Guid.NewGuid();
            var draft = new DraftMontagem("Draft com falha", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
            draft.ConfigurarEncerramentoPresenca(now.AddHours(1));
            draft.IniciarTentativaPublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.Presenca, null, null, claimId, now.AddMinutes(5), now);
            draft.RegistrarFalhaPublicacaoDiscord(DraftMontagemPublicacaoDiscordTipo.Presenca, claimId, null, null, "erro", now.AddSeconds(1));
            dbContext.DraftMontagens.Add(draft);
            await dbContext.SaveChangesAsync();
            return draft.Id;
        }

        public async Task<DraftMontagemPublicacaoClaimResult> ClaimAsync(
            Guid draftId,
            Guid claimId,
            DateTimeOffset now,
            DraftMontagemPublicacaoDiscordTipo tipo = DraftMontagemPublicacaoDiscordTipo.Presenca)
        {
            await using var scope = Services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<IDraftMontagemRepository>()
                .TryClaimPublicacaoDiscordAsync(draftId, tipo, claimId, now.AddMinutes(5), now, CancellationToken.None)
                ?? throw new InvalidOperationException("Publication claim fixture was not found.");
        }

        public async Task<DraftMontagemVersionStamp?> CompleteAsync(
            Guid draftId,
            Guid claimId,
            DateTimeOffset now,
            DraftMontagemPublicacaoDiscordTipo tipo = DraftMontagemPublicacaoDiscordTipo.Presenca)
        {
            await using var scope = Services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<IDraftMontagemRepository>()
                .TryConcluirPublicacaoDiscordAsync(draftId, tipo, claimId, "guild", "channel", "message", now, CancellationToken.None);
        }

        public async Task<DraftMontagemVersionStamp?> FailAsync(
            Guid draftId,
            Guid claimId,
            DateTimeOffset now,
            DraftMontagemPublicacaoDiscordTipo tipo = DraftMontagemPublicacaoDiscordTipo.Presenca)
        {
            await using var scope = Services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<IDraftMontagemRepository>()
                .TryRegistrarFalhaPublicacaoDiscordAsync(draftId, tipo, claimId, "guild", "channel", "error", now, CancellationToken.None);
        }

        public async Task<IReadOnlyCollection<DraftMontagemVersionStamp>> ReconcileAsync(DateTimeOffset now)
        {
            await using var scope = Services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<IDraftMontagemRepository>()
                .MarcarPublicacoesExpiradasParaReconciliacaoAsync(now, CancellationToken.None);
        }

        public async Task<DraftMontagemVersionStamp?> RepublishAsync(Guid draftId)
        {
            await using var scope = Services.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<IDraftMontagemRepository>();
            var dbContext = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
            var userId = await dbContext.Users.Select(user => user.Id).SingleAsync();
            var draft = await repository.GetByIdAsync(draftId, CancellationToken.None);
            var stamp = draft!.SolicitarRepublicacaoDiscord(
                DraftMontagemPublicacaoDiscordTipo.Presenca,
                userId,
                "republicar",
                DateTimeOffset.UtcNow);
            if (stamp is not null)
            {
                await repository.SaveChangesAsync(CancellationToken.None);
            }
            return stamp;
        }

        public async Task<DraftMontagemVersionStamp> GetStampAsync(Guid draftId)
        {
            await using var scope = Services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>()
                .DraftMontagens.AsNoTracking()
                .Where(draft => draft.Id == draftId)
                .Select(draft => new DraftMontagemVersionStamp(draft.Id, draft.VersaoEstado, draft.DataAtualizacao))
                .SingleAsync();
        }

        public async Task<PublicationPersistenceState> GetPersistenceStateAsync(
            Guid draftId,
            DraftMontagemPublicacaoDiscordTipo tipo)
        {
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
            var parent = await dbContext.DraftMontagens.AsNoTracking()
                .Where(draft => draft.Id == draftId)
                .Select(draft => new DraftMontagemVersionStamp(draft.Id, draft.VersaoEstado, draft.DataAtualizacao))
                .SingleAsync();
            var publication = await dbContext.DraftMontagemPublicacoesDiscord.AsNoTracking()
                .Where(item => item.DraftMontagemId == draftId && item.Tipo == tipo)
                .Select(item => new { item.Status, item.ClaimId })
                .SingleAsync();
            var auditCount = await dbContext.DraftMontagemAcoesAdministrativas.AsNoTracking()
                .CountAsync(item => item.DraftMontagemId == draftId);
            return new PublicationPersistenceState(publication.Status, publication.ClaimId, parent, auditCount);
        }

        public async Task ExpireClaimAsync(Guid draftId, DraftMontagemPublicacaoDiscordTipo tipo)
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "UPDATE draft_montagem_publicacoes_discord SET claim_expira_em = clock_timestamp() - INTERVAL '1 minute' WHERE draft_montagem_id = @draftId AND tipo = @tipo";
            command.Parameters.AddWithValue("draftId", draftId);
            command.Parameters.AddWithValue("tipo", tipo.ToString());
            await command.ExecuteNonQueryAsync();
        }

        public async Task InstallParentUpdateFailureAsync(Guid draftId)
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE OR REPLACE FUNCTION fail_test_draft_parent_update() RETURNS trigger AS $$
                BEGIN
                    IF NEW.id = @draftId THEN
                        RAISE EXCEPTION 'forced parent update failure';
                    END IF;
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
                CREATE TRIGGER fail_test_draft_parent_update
                BEFORE UPDATE ON draft_montagens
                FOR EACH ROW EXECUTE FUNCTION fail_test_draft_parent_update();
                """;
            command.Parameters.AddWithValue("draftId", draftId);
            await command.ExecuteNonQueryAsync();
        }

        public async Task<RepublicationConcurrencyResult> ExecuteRepublicationConflictAndNoOpAsync(Guid draftId)
        {
            await using var firstScope = Services.CreateAsyncScope();
            await using var conflictScope = Services.CreateAsyncScope();
            var firstRepository = firstScope.ServiceProvider.GetRequiredService<IDraftMontagemRepository>();
            var conflictRepository = conflictScope.ServiceProvider.GetRequiredService<IDraftMontagemRepository>();
            var firstDraft = await firstRepository.GetByIdAsync(draftId, CancellationToken.None)
                ?? throw new InvalidOperationException("First republication fixture was not found.");
            var conflictDraft = await conflictRepository.GetByIdAsync(draftId, CancellationToken.None)
                ?? throw new InvalidOperationException("Conflict republication fixture was not found.");
            var userId = await firstScope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>()
                .Users.Select(user => user.Id).SingleAsync();
            var before = await GetPersistenceStateAsync(draftId, DraftMontagemPublicacaoDiscordTipo.Presenca);
            var firstPublisher = new Mock<IDraftMontagemRealtimePublisher>();
            var conflictPublisher = new Mock<IDraftMontagemRealtimePublisher>();
            var firstHandler = CreateRepublicationHandler(firstRepository, firstPublisher.Object, userId);
            var conflictHandler = CreateRepublicationHandler(conflictRepository, conflictPublisher.Object, userId);
            var command = new RepublicarPublicacaoDiscordDraftMontagemCommand(
                draftId,
                new RepublicarPublicacaoDiscordDraftMontagemRequestDto(
                    DraftMontagemPublicacaoDiscordTipo.Presenca,
                    "republicar"));

            await firstHandler.Handle(command, CancellationToken.None);
            var afterFirst = await GetPersistenceStateAsync(draftId, DraftMontagemPublicacaoDiscordTipo.Presenca);
            var conflict = false;
            try
            {
                await conflictHandler.Handle(command, CancellationToken.None);
            }
            catch (DomainException)
            {
                conflict = true;
            }
            var afterConflict = await GetPersistenceStateAsync(draftId, DraftMontagemPublicacaoDiscordTipo.Presenca);

            await using var noOpScope = Services.CreateAsyncScope();
            var noOpRepository = noOpScope.ServiceProvider.GetRequiredService<IDraftMontagemRepository>();
            var noOpPublisher = new Mock<IDraftMontagemRealtimePublisher>();
            var noOpHandler = CreateRepublicationHandler(noOpRepository, noOpPublisher.Object, userId);
            await noOpHandler.Handle(command, CancellationToken.None);
            var afterNoOp = await GetPersistenceStateAsync(draftId, DraftMontagemPublicacaoDiscordTipo.Presenca);

            return new RepublicationConcurrencyResult(
                before,
                afterFirst,
                afterConflict,
                afterNoOp,
                conflict,
                firstPublisher.Invocations.Count,
                conflictPublisher.Invocations.Count,
                noOpPublisher.Invocations.Count);
        }

        public async Task<ArchivedRepublicationResult> ExecuteArchivedCancellationRepublicationAndNoOpAsync(Guid draftId)
        {
            await using var scope = Services.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<IDraftMontagemRepository>();
            var userId = await scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>()
                .Users.Select(user => user.Id).SingleAsync();
            var publisher = new Mock<IDraftMontagemRealtimePublisher>();
            var handler = new RepublicarCancelamentoDraftArquivadoCommandHandler(
                repository,
                new CurrentUser(userId),
                publisher.Object);
            var before = await GetPersistenceStateAsync(
                draftId, DraftMontagemPublicacaoDiscordTipo.Cancelamento);

            await handler.Handle(new RepublicarCancelamentoDraftArquivadoCommand(draftId), CancellationToken.None);
            var afterFirst = await GetPersistenceStateAsync(
                draftId, DraftMontagemPublicacaoDiscordTipo.Cancelamento);
            await handler.Handle(new RepublicarCancelamentoDraftArquivadoCommand(draftId), CancellationToken.None);
            var afterNoOp = await GetPersistenceStateAsync(
                draftId, DraftMontagemPublicacaoDiscordTipo.Cancelamento);
            var publication = publisher.Invocations.Single();

            return new ArchivedRepublicationResult(
                before,
                afterFirst,
                afterNoOp,
                publisher.Invocations.Count,
                (DraftMontagemSnapshotScope)publication.Arguments[1],
                (DraftMontagemAvailabilityChange)publication.Arguments[2]);
        }

        public async Task ExpirePresenceWindowAsync(Guid draftId)
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "UPDATE draft_montagens SET horario_encerramento_presenca = clock_timestamp() - INTERVAL '1 minute' WHERE id = @draftId";
            command.Parameters.AddWithValue("draftId", draftId);
            await command.ExecuteNonQueryAsync();
        }

        private static RepublicarPublicacaoDiscordDraftMontagemCommandHandler CreateRepublicationHandler(
            IDraftMontagemRepository repository,
            IDraftMontagemRealtimePublisher publisher,
            Guid userId) => new(
                repository,
                new RepublicarPublicacaoDiscordDraftMontagemValidator(),
                new CurrentUser(userId),
                Mock.Of<IDraftMontagemMetrics>(),
                publisher);

        private static ApplicationUser CreateUser()
        {
            var userId = Guid.NewGuid();
            return new ApplicationUser
            {
                Id = userId,
                Nome = "Operador",
                UserName = $"publication-{userId:N}",
                NormalizedUserName = $"PUBLICATION-{userId:N}",
            };
        }
    }

    private sealed record PublicationPersistenceState(
        DraftMontagemPublicacaoDiscordStatus Status,
        Guid? ClaimId,
        DraftMontagemVersionStamp Stamp,
        int AuditCount);

    private sealed record RepublicationConcurrencyResult(
        PublicationPersistenceState Before,
        PublicationPersistenceState AfterFirst,
        PublicationPersistenceState AfterConflict,
        PublicationPersistenceState AfterNoOp,
        bool Conflict,
        int FirstPublishCalls,
        int ConflictPublishCalls,
        int NoOpPublishCalls);

    private sealed record ArchivedRepublicationResult(
        PublicationPersistenceState Before,
        PublicationPersistenceState AfterFirst,
        PublicationPersistenceState AfterNoOp,
        int PublishCalls,
        DraftMontagemSnapshotScope PublishedScope,
        DraftMontagemAvailabilityChange PublishedAvailability);

    private sealed record CurrentUser(Guid? UserId) : ICurrentUser
    {
        public IReadOnlyCollection<string> Roles => [];
        public string? IpAddress => null;
        public string? UserAgent => null;
    }
}
