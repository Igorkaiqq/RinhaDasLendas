using Npgsql;

namespace RinhaDasLendas.BrowserQaFixture;

public sealed record BrowserQaFixtureOptions(
    string ConnectionString,
    Guid AdminAId,
    Guid AdminBId,
    string AdminAEmail,
    string AdminBEmail,
    string AdminAPassword,
    string AdminBPassword)
{
    public static BrowserQaFixtureOptions FromEnvironment(IReadOnlyDictionary<string, string?> environment)
    {
        var connectionString = Required(environment, "QA_DATABASE_CONNECTION");
        var connection = new NpgsqlConnectionStringBuilder(connectionString);
        var database = connection.Database;
        if (string.IsNullOrWhiteSpace(database)
            || !database.StartsWith("rinha_feature029_browser_qa_", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("QA_DATABASE_CONNECTION deve apontar para um banco exclusivo com prefixo rinha_feature029_browser_qa_.");
        }

        var adminAId = RequiredGuid(environment, "QA_ADMIN_A_ID");
        var adminBId = RequiredGuid(environment, "QA_ADMIN_B_ID");
        if (adminAId == adminBId)
        {
            throw new InvalidOperationException("QA_ADMIN_A_ID e QA_ADMIN_B_ID devem ser distintos.");
        }

        var adminAEmail = Required(environment, "QA_ADMIN_A_EMAIL");
        var adminBEmail = Required(environment, "QA_ADMIN_B_EMAIL");
        if (string.Equals(adminAEmail, adminBEmail, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("QA_ADMIN_A_EMAIL e QA_ADMIN_B_EMAIL devem ser distintos.");
        }

        return new BrowserQaFixtureOptions(
            connectionString,
            adminAId,
            adminBId,
            adminAEmail,
            adminBEmail,
            Required(environment, "QA_ADMIN_A_PASSWORD"),
            Required(environment, "QA_ADMIN_B_PASSWORD"));
    }

    private static string Required(IReadOnlyDictionary<string, string?> environment, string key)
    {
        if (!environment.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"A variável {key} é obrigatória.");
        }

        return value.Trim();
    }

    private static Guid RequiredGuid(IReadOnlyDictionary<string, string?> environment, string key)
    {
        var value = Required(environment, key);
        return Guid.TryParse(value, out var id)
            ? id
            : throw new InvalidOperationException($"A variável {key} deve conter um UUID válido.");
    }
}
