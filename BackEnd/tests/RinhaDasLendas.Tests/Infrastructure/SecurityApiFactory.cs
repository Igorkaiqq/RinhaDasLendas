using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using RinhaDasLendas.Api.Services;
using RinhaDasLendas.Domain.Constants;

namespace RinhaDasLendas.Tests.Infrastructure;

internal class SecurityApiFactory : WebApplicationFactory<Program>
{
    internal const string BotToken = "integration-test-internal-token-with-at-least-thirty-two-characters";
    private const string JwtKey = "integration-test-jwt-key-with-at-least-thirty-two-characters";
    private const string Issuer = "RinhaDasLendas.Tests";
    private const string Audience = "RinhaDasLendas.Tests.Client";
    private readonly string? _databaseName;
    private bool _databaseCreated;

    protected SecurityApiFactory(bool useIsolatedPostgreSql = false)
    {
        if (!useIsolatedPostgreSql)
        {
            return;
        }

        _databaseName = $"rinha_security_{Guid.NewGuid():N}";
        ConnectionString = new NpgsqlConnectionStringBuilder(BuildAdminConnectionString())
        {
            Database = _databaseName,
        }.ConnectionString;
        CreateDatabaseAsync().GetAwaiter().GetResult();
    }

    protected string? ConnectionString { get; }

    internal HttpClient CreateAnonymousClient() => CreateClient();

    internal HttpClient CreatePresenceScheduleModeratorClient(Guid userId) => CreateJwtClient(userId, AuthRoles.Moderador);

    internal HttpClient CreatePresenceSchedulePlayerClient(Guid userId) => CreateJwtClient(userId, AuthRoles.Jogador);

    internal HttpClient CreateBotClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(BotInternalAuthOptions.HeaderName, BotToken);
        return client;
    }

    internal HttpClient CreateInvalidBotClient(string token = "invalid-internal-token")
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(BotInternalAuthOptions.HeaderName, token);
        return client;
    }

    internal HttpClient CreateJwtClient(Guid? userId, params string[] roles)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwt(userId, roles));
        return client;
    }

    internal HttpClient CreateMalformedJwtClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "malformed-token");
        return client;
    }

    internal HttpClient CreateExpiredJwtClient(Guid userId, params string[] roles)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateJwt(userId, roles, DateTime.UtcNow.AddMinutes(-2)));
        return client;
    }

    internal HttpClient CreateInvalidSignatureJwtClient(Guid userId, params string[] roles)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateJwt(userId, roles, DateTime.UtcNow.AddMinutes(5), "invalid-signing-key-with-at-least-thirty-two-characters"));
        return client;
    }

    internal static string CreateJwt(Guid? userId, params string[] roles)
    {
        return CreateJwt(userId, roles, DateTime.UtcNow.AddMinutes(5));
    }

    private static string CreateJwt(Guid? userId, string[] roles, DateTime expires, string key = JwtKey)
    {
        var claims = roles.Select(role => new Claim(ClaimTypes.Role, role)).ToList();
        if (userId.HasValue)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
        }

        var token = new JwtSecurityToken(
            Issuer,
            Audience,
            claims,
            expires: expires,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder
            .UseEnvironment("IntegrationTesting")
            .UseSetting("Authentication:Jwt:Issuer", Issuer)
            .UseSetting("Authentication:Jwt:Audience", Audience)
            .UseSetting("Authentication:Jwt:Key", JwtKey)
            .UseSetting("DiscordBot:InternalToken", BotToken);
        if (ConnectionString is null)
        {
            return;
        }

        builder
            .UseSetting("ConnectionStrings:RinhaDasLendas", ConnectionString)
            .UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:RinhaDasLendas"] = ConnectionString,
                ["ConnectionStrings:DefaultConnection"] = ConnectionString,
            }));
    }

    public new async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        if (!_databaseCreated || _databaseName is null)
        {
            return;
        }

        await using var connection = new NpgsqlConnection(BuildAdminConnectionString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)";
        await command.ExecuteNonQueryAsync();
    }

    private async Task CreateDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(BuildAdminConnectionString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE \"{_databaseName}\"";
        await command.ExecuteNonQueryAsync();
        _databaseCreated = true;
    }

    private static string BuildAdminConnectionString() =>
        $"Host={Environment.GetEnvironmentVariable("TEST_POSTGRES_HOST") ?? "localhost"};Port={Environment.GetEnvironmentVariable("TEST_POSTGRES_PORT") ?? "5432"};Database=postgres;Username=postgres;Password=postgres";
}
