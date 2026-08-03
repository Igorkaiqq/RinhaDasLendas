using System.Data;
using Microsoft.EntityFrameworkCore;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Infrastructure.Identity;
using RinhaDasLendas.Infrastructure.Persistence;

namespace RinhaDasLendas.BrowserQaFixture;

public static class BrowserQaFixtureSeeder
{
    private const string DatabasePrefix = "rinha_feature029_browser_qa_";
    private const string RequiredMigration = "20260731012844_AddDraftMontagemSystemActor";

    public static async Task<BrowserQaFixtureMetadata> SeedAsync(
        BrowserQaFixtureOptions options,
        CancellationToken cancellationToken = default)
    {
        var dbOptions = new DbContextOptionsBuilder<RinhaDasLendasDbContext>()
            .UseNpgsql(options.ConnectionString)
            .Options;
        await using var db = new RinhaDasLendasDbContext(dbOptions);
        await db.Database.OpenConnectionAsync(cancellationToken);
        var databaseName = db.Database.GetDbConnection().Database;
        if (!databaseName.StartsWith(DatabasePrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"O banco descartável deve usar o prefixo {DatabasePrefix}.");
        }

        var knownMigrations = db.Database.GetMigrations().ToArray();
        var appliedMigrations = (await db.Database.GetAppliedMigrationsAsync(cancellationToken)).ToArray();
        var pendingMigrations = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();
        if (knownMigrations.LastOrDefault() != RequiredMigration
            || appliedMigrations.LastOrDefault() != RequiredMigration
            || !appliedMigrations.SequenceEqual(knownMigrations, StringComparer.Ordinal)
            || pendingMigrations.Length != 0)
        {
            throw new InvalidOperationException(
                $"O banco descartável deve conter exatamente a sequência de migrations até {RequiredMigration}, sem migrations pendentes ou posteriores.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        if (await db.Users.AnyAsync(cancellationToken) || await db.DraftMontagens.AnyAsync(cancellationToken))
        {
            throw new InvalidOperationException("A fixture exige banco descartável sem usuários e drafts.");
        }

        var roleIds = await EnsureRolesAsync(db, cancellationToken);
        var seed = BrowserQaFixtureSeed.Create(options, roleIds);
        db.Users.AddRange(seed.Users);
        db.Jogadores.AddRange(seed.Players);
        db.UserRoles.AddRange(seed.UserRoles);
        db.DraftMontagens.AddRange(seed.PresenceDraft, seed.ManualDraft);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return seed.Metadata;
    }

    private static async Task<IReadOnlyDictionary<string, Guid>> EnsureRolesAsync(
        RinhaDasLendasDbContext db,
        CancellationToken cancellationToken)
    {
        var existing = await db.Roles.ToDictionaryAsync(role => role.Name!, role => role.Id, StringComparer.Ordinal, cancellationToken);
        foreach (var role in AuthRoles.Levels)
        {
            if (existing.ContainsKey(role.Key))
            {
                continue;
            }

            var entity = new ApplicationRole
            {
                Id = Guid.NewGuid(),
                Name = role.Key,
                NormalizedName = role.Key.ToUpperInvariant(),
                NivelHierarquico = role.Value,
                ConcurrencyStamp = Guid.NewGuid().ToString("N"),
            };
            db.Roles.Add(entity);
            existing.Add(role.Key, entity.Id);
        }

        return existing;
    }
}
