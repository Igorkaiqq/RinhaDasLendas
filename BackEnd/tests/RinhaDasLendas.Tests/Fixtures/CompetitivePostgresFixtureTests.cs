using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace RinhaDasLendas.Tests.Fixtures;

public sealed class CompetitivePostgresFixtureTests
{
    [Fact]
    public async Task CreateEmptyAsync_ShouldOpenApplicationContextAndDropDatabaseOnDisposal()
    {
        string connectionString;
        string databaseName;

        await using (var fixture = await CompetitivePostgresFixture.CreateEmptyAsync())
        {
            connectionString = fixture.ConnectionString;
            databaseName = new NpgsqlConnectionStringBuilder(connectionString).Database!;
            await using var context = fixture.CreateContext();
            (await context.Database.CanConnectAsync()).Should().BeTrue();
        }

        var adminConnectionString = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Database = "postgres",
        }.ConnectionString;
        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM pg_database WHERE datname = $1";
        command.Parameters.AddWithValue(databaseName);

        Convert.ToInt64(await command.ExecuteScalarAsync()).Should().Be(0);
    }
}
