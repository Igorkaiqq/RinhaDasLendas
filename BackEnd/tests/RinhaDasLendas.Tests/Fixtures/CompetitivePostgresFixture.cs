using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using RinhaDasLendas.Infrastructure.Persistence;

namespace RinhaDasLendas.Tests.Fixtures;

internal sealed class CompetitivePostgresFixture : IAsyncDisposable
{
    private const string DatabasePrefix = "rinha_competitive_";
    private readonly string _adminConnectionString;
    private readonly string _databaseName;
    private bool _disposed;

    private CompetitivePostgresFixture(
        string databaseName,
        string adminConnectionString,
        string connectionString)
    {
        _databaseName = databaseName;
        _adminConnectionString = adminConnectionString;
        ConnectionString = connectionString;
    }

    internal string ConnectionString { get; }

    internal static Task<CompetitivePostgresFixture> CreateAsync(
        CancellationToken cancellationToken = default) =>
        CreateAtMigrationAsync(null, cancellationToken);

    internal static Task<CompetitivePostgresFixture> CreateEmptyAsync(
        CancellationToken cancellationToken = default) =>
        CreateCoreAsync(false, null, cancellationToken);

    internal static Task<CompetitivePostgresFixture> CreateAtMigrationAsync(
        string? migration,
        CancellationToken cancellationToken = default) =>
        CreateCoreAsync(true, migration, cancellationToken);

    internal RinhaDasLendasDbContext CreateContext(params IInterceptor[] interceptors)
    {
        var builder = new DbContextOptionsBuilder<RinhaDasLendasDbContext>()
            .UseNpgsql(ConnectionString);

        if (interceptors.Length > 0)
        {
            builder.AddInterceptors(interceptors);
        }

        return new RinhaDasLendasDbContext(builder.Options);
    }

    internal async Task<NpgsqlConnection> OpenConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(ConnectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await DropDatabaseAsync(_adminConnectionString, _databaseName);
        _disposed = true;
    }

    private static async Task<CompetitivePostgresFixture> CreateCoreAsync(
        bool migrate,
        string? migration,
        CancellationToken cancellationToken)
    {
        var adminConnectionString = BuildAdminConnectionString();
        var databaseName = $"{DatabasePrefix}{Guid.NewGuid():N}";
        var connectionString = new NpgsqlConnectionStringBuilder(adminConnectionString)
        {
            Database = databaseName,
        }.ConnectionString;

        var fixture = new CompetitivePostgresFixture(databaseName, adminConnectionString, connectionString);

        try
        {
            await CreateDatabaseAsync(adminConnectionString, databaseName, cancellationToken);

            if (migrate)
            {
                await using var context = fixture.CreateContext();
                if (migration is null)
                {
                    await context.Database.MigrateAsync(cancellationToken);
                }
                else
                {
                    await context.Database.GetService<IMigrator>().MigrateAsync(migration, cancellationToken);
                }
            }

            return fixture;
        }
        catch
        {
            try
            {
                await fixture.DisposeAsync();
            }
            catch
            {
                // Preserve the initialization failure after making the best-effort DROP attempt.
            }

            throw;
        }
    }

    private static async Task CreateDatabaseAsync(
        string adminConnectionString,
        string databaseName,
        CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task DropDatabaseAsync(string adminConnectionString, string databaseName)
    {
        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"DROP DATABASE IF EXISTS \"{databaseName}\" WITH (FORCE)";
        await command.ExecuteNonQueryAsync();
    }

    private static string BuildAdminConnectionString()
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = Environment.GetEnvironmentVariable("TEST_POSTGRES_HOST") ?? "localhost",
            Port = int.Parse(
                Environment.GetEnvironmentVariable("TEST_POSTGRES_PORT") ?? "5432",
                CultureInfo.InvariantCulture),
            Database = Environment.GetEnvironmentVariable("TEST_POSTGRES_DB") ?? "postgres",
            Username = Environment.GetEnvironmentVariable("TEST_POSTGRES_USER") ?? "postgres",
            Password = Environment.GetEnvironmentVariable("TEST_POSTGRES_PASSWORD") ?? "postgres",
        };

        return builder.ConnectionString;
    }
}
