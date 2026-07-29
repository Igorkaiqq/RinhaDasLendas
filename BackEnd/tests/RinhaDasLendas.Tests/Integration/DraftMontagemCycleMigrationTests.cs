using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Infrastructure.Persistence;

namespace RinhaDasLendas.Tests.Integration;

public sealed class DraftMontagemCycleMigrationTests
{
    private const string PreviousMigration = "20260726104907_AddDraftMontagemArchiving";

    [Fact]
    public async Task Migration_DevePreservarLegadoEUsarCicloV2ComModoNuloEmNovosRegistros()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAtMigrationAsync(PreviousMigration);
        var legacyId = Guid.NewGuid();
        const DraftMontagemModo modoAntes = DraftMontagemModo.TempoReal;

        await using (var before = database.CreateContext())
        {
            await InsertDraftAsync(before, legacyId, modoAntes);
            await before.Database.MigrateAsync();
        }

        var novoId = Guid.NewGuid();
        await using (var migrated = database.CreateContext())
        {
            await InsertDraftAsync(migrated, novoId, null);
        }

        await using var verification = database.CreateContext();
        var legacy = await verification.DraftMontagens.SingleAsync(draft => draft.Id == legacyId);
        var novo = await verification.DraftMontagens.SingleAsync(draft => draft.Id == novoId);

        legacy.CicloVersao.Should().Be(DraftMontagemCicloVersao.Legado);
        legacy.Modo.Should().Be(modoAntes);
        novo.CicloVersao.Should().Be(DraftMontagemCicloVersao.ModoPosPresenca);
        novo.Modo.Should().BeNull();
    }

    private static Task<int> InsertDraftAsync(
        RinhaDasLendasDbContext context,
        Guid id,
        DraftMontagemModo? modo)
    {
        return context.Database.ExecuteSqlInterpolatedAsync($$"""
            INSERT INTO draft_montagens
                (id, nome, status, modo, tamanho_equipe, quantidade_times, quantidade_reservas,
                 criterio_capitaes, duracao_turno_segundos, versao_estado, data_cadastro, data_atualizacao)
            VALUES
                ({{id}}, 'Draft de migration', 'PresencaAberta', {{(modo == null ? null : modo.ToString())}},
                 5, 0, 0, 'Manual', 30, 0, NOW(), NOW())
            """);
    }

    private sealed class PostgreSqlTestDatabase : IAsyncDisposable
    {
        private readonly string _databaseName;
        private readonly string _adminConnectionString;

        private PostgreSqlTestDatabase(string databaseName, string adminConnectionString, string connectionString)
        {
            _databaseName = databaseName;
            _adminConnectionString = adminConnectionString;
            ConnectionString = connectionString;
        }

        private string ConnectionString { get; }

        public static async Task<PostgreSqlTestDatabase> CreateAtMigrationAsync(string migration)
        {
            var host = Environment.GetEnvironmentVariable("TEST_POSTGRES_HOST") ?? "localhost";
            var port = Environment.GetEnvironmentVariable("TEST_POSTGRES_PORT") ?? "5432";
            var adminConnectionString = $"Host={host};Port={port};Database=postgres;Username=postgres;Password=postgres";
            var databaseName = $"rinha_draft_cycle_{Guid.NewGuid():N}";

            await using var connection = new NpgsqlConnection(adminConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
            await command.ExecuteNonQueryAsync();

            var connectionString = new NpgsqlConnectionStringBuilder(adminConnectionString)
            {
                Database = databaseName,
            }.ConnectionString;
            var database = new PostgreSqlTestDatabase(databaseName, adminConnectionString, connectionString);
            await using var context = database.CreateContext();
            await context.Database.GetService<IMigrator>().MigrateAsync(migration);
            return database;
        }

        public RinhaDasLendasDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<RinhaDasLendasDbContext>()
                .UseNpgsql(ConnectionString)
                .Options;
            return new RinhaDasLendasDbContext(options);
        }

        public async ValueTask DisposeAsync()
        {
            await using var connection = new NpgsqlConnection(_adminConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)";
            await command.ExecuteNonQueryAsync();
        }
    }
}
