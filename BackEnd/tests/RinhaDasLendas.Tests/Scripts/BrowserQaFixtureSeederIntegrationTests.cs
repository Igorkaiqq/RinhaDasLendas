using System.Runtime.CompilerServices;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using RinhaDasLendas.BrowserQaFixture;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Infrastructure.Persistence;

namespace RinhaDasLendas.Tests.Scripts;

public sealed class BrowserQaFixtureSeederIntegrationTests
{
    private const string RequiredMigration = "20260731012844_AddDraftMontagemSystemActor";
    private const string PreviousMigration = "20260729124041_CorrigirNucleoCicloDraft";

    [Fact]
    public async Task SeedAsync_ShouldRefuseDatabaseWithoutQaPrefixWithoutMutatingIt()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync("rinha_fixture_unsafe_", RequiredMigration);

        var action = () => BrowserQaFixtureSeeder.SeedAsync(CreateOptions(database.ConnectionString));

        await action.Should().ThrowAsync<InvalidOperationException>();
        await using var context = database.CreateContext();
        (await context.Users.CountAsync()).Should().Be(0);
        (await context.DraftMontagens.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task SeedAsync_ShouldRefuseDatabaseBeforeRequiredMigration()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync(QaPrefix(), PreviousMigration);

        var action = () => BrowserQaFixtureSeeder.SeedAsync(CreateOptions(database.ConnectionString));

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SeedAsync_ShouldRefuseDatabaseWithFutureMigrationMarkerWithoutMutatingIt()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync(QaPrefix(), RequiredMigration);
        await database.ExecuteAsync("""
            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            VALUES ('99991231235959_FutureSchema', '10.0.0')
            """);

        var action = () => BrowserQaFixtureSeeder.SeedAsync(CreateOptions(database.ConnectionString));

        await action.Should().ThrowAsync<InvalidOperationException>();
        await using var context = database.CreateContext();
        (await context.Users.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task SeedAsync_ShouldRefusePopulatedDatabase()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync(QaPrefix(), RequiredMigration);
        await using (var context = database.CreateContext())
        {
            context.DraftMontagens.Add(DraftMontagem.CriarPorPresenca("Draft existente", null, 2));
            await context.SaveChangesAsync();
        }

        var action = () => BrowserQaFixtureSeeder.SeedAsync(CreateOptions(database.ConnectionString));

        await action.Should().ThrowAsync<InvalidOperationException>();
        await using var verification = database.CreateContext();
        (await verification.DraftMontagens.CountAsync()).Should().Be(1);
        (await verification.Users.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task SeedAsync_ShouldPersistExactUsersPlayersRolesAndDraftRelationships()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync(QaPrefix(), RequiredMigration);
        var options = CreateOptions(database.ConnectionString);

        var metadata = await BrowserQaFixtureSeeder.SeedAsync(options);

        await using var context = database.CreateContext();
        var users = await context.Users.AsNoTracking().ToListAsync();
        var players = await context.Jogadores.AsNoTracking().ToListAsync();
        var roles = await context.Roles.AsNoTracking().ToDictionaryAsync(role => role.Name!, role => role.Id);
        var userRoles = await context.UserRoles.AsNoTracking().ToListAsync();
        var presenceDraft = await context.DraftMontagens
            .AsNoTracking()
            .Include(draft => draft.Presencas)
            .SingleAsync(draft => draft.Id == metadata.PresenceDraftId);
        var manualDraft = await context.DraftMontagens
            .AsNoTracking()
            .Include(draft => draft.Times)
            .Include(draft => draft.Participantes)
            .SingleAsync(draft => draft.Id == metadata.ManualDraftId);

        users.Should().HaveCount(5);
        players.Should().HaveCount(5).And.OnlyContain(player => player.UsuarioId.HasValue);
        players.Select(player => player.UsuarioId!.Value).Should().BeEquivalentTo(users.Select(user => user.Id));
        roles.Keys.Should().BeEquivalentTo(AuthRoles.Levels.Keys);
        userRoles.Should().HaveCount(7);
        userRoles.Where(item => item.RoleId == roles[AuthRoles.SuperAdmin]).Select(item => item.UserId)
            .Should().BeEquivalentTo([options.AdminAId, options.AdminBId]);
        var reserveCaptainUserId = players.Single(player => player.Id == metadata.PlayerIds[4]).UsuarioId!.Value;
        userRoles.Where(item => item.RoleId == roles[AuthRoles.Capitao]).Select(item => item.UserId)
            .Should().BeEquivalentTo([options.AdminAId, options.AdminBId, reserveCaptainUserId]);
        presenceDraft.Status.Should().Be(DraftMontagemStatus.PresencaAberta);
        presenceDraft.Presencas.Select(item => (item.UsuarioId, item.JogadorId)).Should()
            .BeEquivalentTo(players.Select(player => (player.UsuarioId!.Value, player.Id)));
        manualDraft.Status.Should().Be(DraftMontagemStatus.Aberta);
        manualDraft.Modo.Should().Be(DraftMontagemModo.Manual);
        manualDraft.Times.Should().HaveCount(2);
        manualDraft.Participantes.Select(item => item.JogadorId).Should()
            .BeEquivalentTo(metadata.PlayerIds.Take(4));
    }

    [Fact]
    public async Task SeedAsync_ShouldRefuseSecondRunWithoutAddingRows()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync(QaPrefix(), RequiredMigration);
        var options = CreateOptions(database.ConnectionString);
        await BrowserQaFixtureSeeder.SeedAsync(options);

        var action = () => BrowserQaFixtureSeeder.SeedAsync(options);

        await action.Should().ThrowAsync<InvalidOperationException>();
        await using var context = database.CreateContext();
        (await context.Users.CountAsync()).Should().Be(5);
        (await context.DraftMontagens.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task SeedAsync_ShouldRollbackRolesWhenPersistenceFails()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync(QaPrefix(), RequiredMigration);
        int roleCountBefore;
        await using (var baseline = database.CreateContext())
        {
            roleCountBefore = await baseline.Roles.CountAsync();
        }

        var options = CreateOptions(database.ConnectionString) with
        {
            AdminBEmail = "qa-admin-a@example.test",
        };

        var action = () => BrowserQaFixtureSeeder.SeedAsync(options);

        await action.Should().ThrowAsync<DbUpdateException>();
        await using var context = database.CreateContext();
        (await context.Roles.CountAsync()).Should().Be(roleCountBefore);
        (await context.Users.CountAsync()).Should().Be(0);
        (await context.DraftMontagens.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ReadmeTimeoutBlock_ShouldExecuteAgainstDisposableQaDatabase()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync(QaPrefix(), RequiredMigration);
        var metadata = await BrowserQaFixtureSeeder.SeedAsync(CreateOptions(database.ConnectionString));
        var originalExpiration = DateTimeOffset.UtcNow.AddMinutes(5);
        await database.SetRealtimeTurnAsync(metadata.PresenceDraftId, originalExpiration);
        var sql = ReadTimeoutSql().Replace("<presenceDraftId>", metadata.PresenceDraftId.ToString(), StringComparison.Ordinal);

        sql.TrimStart().Should().StartWith("DO $$");
        await database.ExecuteAsync(sql);

        (await database.GetTurnExpirationAsync(metadata.PresenceDraftId)).Should().BeBefore(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task ReadmeTimeoutBlock_ShouldRefuseNonQaDatabaseWithoutMutation()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync("rinha_fixture_readme_", RequiredMigration);
        var draft = DraftMontagem.CriarPorPresenca("Draft de validação SQL", null, 2);
        await using (var context = database.CreateContext())
        {
            context.DraftMontagens.Add(draft);
            await context.SaveChangesAsync();
        }

        var originalExpiration = DateTimeOffset.UtcNow.AddMinutes(5);
        await database.SetRealtimeTurnAsync(draft.Id, originalExpiration);
        var sql = ReadTimeoutSql().Replace("<presenceDraftId>", draft.Id.ToString(), StringComparison.Ordinal);

        var action = () => database.ExecuteAsync(sql);

        await action.Should().ThrowAsync<PostgresException>();
        (await database.GetTurnExpirationAsync(draft.Id)).Should().BeCloseTo(originalExpiration, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task ReadmeTimeoutBlock_ShouldRefuseQaDraftOutsideExpectedStateWithoutMutation()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync(QaPrefix(), RequiredMigration);
        var metadata = await BrowserQaFixtureSeeder.SeedAsync(CreateOptions(database.ConnectionString));
        var originalExpiration = DateTimeOffset.UtcNow.AddMinutes(5);
        await database.SetRealtimeTurnAsync(metadata.PresenceDraftId, originalExpiration);
        await database.ExecuteAsync($"UPDATE draft_montagens SET status = 'PresencaAberta' WHERE id = '{metadata.PresenceDraftId}'");
        var sql = ReadTimeoutSql().Replace("<presenceDraftId>", metadata.PresenceDraftId.ToString(), StringComparison.Ordinal);

        var action = () => database.ExecuteAsync(sql);

        await action.Should().ThrowAsync<PostgresException>();
        (await database.GetTurnExpirationAsync(metadata.PresenceDraftId)).Should().BeCloseTo(originalExpiration, TimeSpan.FromMilliseconds(1));
    }

    private static string ReadTimeoutSql([CallerFilePath] string sourceFile = "")
    {
        var readmePath = Path.GetFullPath("../../../scripts/qa/README.md", Path.GetDirectoryName(sourceFile)!);
        var readme = File.ReadAllText(readmePath);
        var start = readme.IndexOf("```sql", StringComparison.Ordinal) + "```sql".Length;
        var end = readme.IndexOf("```", start, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo("```sql".Length);
        end.Should().BeGreaterThan(start);
        return readme[start..end].Trim();
    }

    private static string QaPrefix() => "rinha_feature029_browser_qa_test_";

    private static BrowserQaFixtureOptions CreateOptions(string connectionString) => new(
        connectionString,
        Guid.NewGuid(),
        Guid.NewGuid(),
        "qa-admin-a@example.test",
        "qa-admin-b@example.test",
        "external-password-a",
        "external-password-b");

    private sealed class PostgreSqlTestDatabase : IAsyncDisposable
    {
        private readonly string _adminConnectionString;
        private readonly string _databaseName;

        private PostgreSqlTestDatabase(string adminConnectionString, string databaseName)
        {
            _adminConnectionString = adminConnectionString;
            _databaseName = databaseName;
            ConnectionString = new NpgsqlConnectionStringBuilder(adminConnectionString)
            {
                Database = databaseName,
            }.ConnectionString;
        }

        public string ConnectionString { get; }

        public static async Task<PostgreSqlTestDatabase> CreateAsync(string prefix, string migration)
        {
            var adminConnectionString = BuildAdminConnectionString();
            var databaseName = $"{prefix}{Guid.NewGuid():N}";
            await using var connection = new NpgsqlConnection(adminConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
            await command.ExecuteNonQueryAsync();
            var database = new PostgreSqlTestDatabase(adminConnectionString, databaseName);
            try
            {
                await using var context = database.CreateContext();
                await context.Database.GetService<IMigrator>().MigrateAsync(migration);
                return database;
            }
            catch
            {
                await database.DisposeAsync();
                throw;
            }
        }

        public RinhaDasLendasDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<RinhaDasLendasDbContext>()
                .UseNpgsql(ConnectionString)
                .Options;
            return new RinhaDasLendasDbContext(options);
        }

        public async Task ExecuteAsync(string sql)
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();
        }

        public async Task SetRealtimeTurnAsync(Guid draftId, DateTimeOffset expiration)
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                UPDATE draft_montagens
                SET status = 'Aberta', modo = 'TempoReal', turno_expira_em = @expiration
                WHERE id = @id
                """;
            command.Parameters.AddWithValue("expiration", expiration);
            command.Parameters.AddWithValue("id", draftId);
            (await command.ExecuteNonQueryAsync()).Should().Be(1);
        }

        public async Task<DateTimeOffset> GetTurnExpirationAsync(Guid draftId)
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT turno_expira_em FROM draft_montagens WHERE id = @id";
            command.Parameters.AddWithValue("id", draftId);
            var value = await command.ExecuteScalarAsync();
            return value switch
            {
                DateTimeOffset dateTimeOffset => dateTimeOffset,
                DateTime dateTime => new DateTimeOffset(dateTime),
                _ => throw new InvalidOperationException("A expiração do turno não foi retornada pelo PostgreSQL."),
            };
        }

        public async ValueTask DisposeAsync()
        {
            await using var connection = new NpgsqlConnection(_adminConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)";
            await command.ExecuteNonQueryAsync();
        }

        private static string BuildAdminConnectionString() =>
            $"Host={Environment.GetEnvironmentVariable("TEST_POSTGRES_HOST") ?? "localhost"};Port={Environment.GetEnvironmentVariable("TEST_POSTGRES_PORT") ?? "5432"};Database=postgres;Username=postgres;Password=postgres";
    }
}
