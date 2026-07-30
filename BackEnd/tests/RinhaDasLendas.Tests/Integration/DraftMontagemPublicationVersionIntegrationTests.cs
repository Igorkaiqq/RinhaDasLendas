using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
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
    }

    [Fact]
    public async Task TransicoesSemEfeitoDevemRetornarNullSemAlterarVersaoOuData()
    {
        await using var factory = new PublicationApiFactory();
        var initial = await factory.SeedPendingPublicationAsync();
        var before = await factory.GetStampAsync(initial.Id);

        var completion = await factory.CompleteAsync(initial.Id, Guid.NewGuid(), DateTimeOffset.UtcNow);
        var failure = await factory.FailAsync(initial.Id, Guid.NewGuid(), DateTimeOffset.UtcNow);

        completion.Should().BeNull();
        failure.Should().BeNull();
        (await factory.GetStampAsync(initial.Id)).Should().Be(before);
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
        var persisted = await factory.GetStampAsync(draftId);
        var noOp = await factory.RepublishAsync(draftId);

        stamp.Should().Be(persisted);
        noOp.Should().BeNull();
        (await factory.GetStampAsync(draftId)).Should().Be(persisted);
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

        public async Task<Guid> SeedFailedPublicationAsync()
        {
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
            var userId = Guid.NewGuid();
            dbContext.Users.Add(new ApplicationUser
            {
                Id = userId,
                Nome = "Operador",
                UserName = $"publication-{userId:N}",
                NormalizedUserName = $"PUBLICATION-{userId:N}",
            });
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

        public async Task<DraftMontagemPublicacaoClaimResult> ClaimAsync(Guid draftId, Guid claimId, DateTimeOffset now)
        {
            await using var scope = Services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<IDraftMontagemRepository>()
                .TryClaimPublicacaoDiscordAsync(draftId, DraftMontagemPublicacaoDiscordTipo.Presenca, claimId, now.AddMinutes(5), now, CancellationToken.None)
                ?? throw new InvalidOperationException("Publication claim fixture was not found.");
        }

        public async Task<DraftMontagemVersionStamp?> CompleteAsync(Guid draftId, Guid claimId, DateTimeOffset now)
        {
            await using var scope = Services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<IDraftMontagemRepository>()
                .TryConcluirPublicacaoDiscordAsync(draftId, DraftMontagemPublicacaoDiscordTipo.Presenca, claimId, "guild", "channel", "message", now, CancellationToken.None);
        }

        public async Task<DraftMontagemVersionStamp?> FailAsync(Guid draftId, Guid claimId, DateTimeOffset now)
        {
            await using var scope = Services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<IDraftMontagemRepository>()
                .TryRegistrarFalhaPublicacaoDiscordAsync(draftId, DraftMontagemPublicacaoDiscordTipo.Presenca, claimId, "guild", "channel", "error", now, CancellationToken.None);
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

        public async Task ExpirePresenceWindowAsync(Guid draftId)
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "UPDATE draft_montagens SET horario_encerramento_presenca = clock_timestamp() - INTERVAL '1 minute' WHERE id = @draftId";
            command.Parameters.AddWithValue("draftId", draftId);
            await command.ExecuteNonQueryAsync();
        }
    }
}
