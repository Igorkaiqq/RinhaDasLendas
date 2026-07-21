using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using RinhaDasLendas.Api.Filters;
using RinhaDasLendas.Api.Services;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Infrastructure.Identity;
using RinhaDasLendas.Infrastructure.Persistence;

namespace RinhaDasLendas.Tests.Integration;

public sealed class DraftMontagemBehaviorIntegrationTests
{
    [Fact]
    public async Task DoisClaimsConcorrentes_DevemConcederExatamenteUmClaim()
    {
        await using var factory = new PostgreSqlComposeApiFactory();
        var draftId = await factory.SeedPendingPublicationAsync();

        var payloads = await SendConcurrentClaimsAsync(factory, draftId);

        AssertSingleWinnerAndCurrentLoser(payloads);
    }

    [Fact]
    public async Task DoisClaimsConcorrentesSemPublicacaoPreexistente_DevemRetornarEstadoAtualAoPerdedor()
    {
        await using var factory = new PostgreSqlComposeApiFactory();

        for (var attempt = 0; attempt < 10; attempt++)
        {
            var draftId = await factory.SeedDraftWithoutPublicationAsync();

            var payloads = await SendConcurrentClaimsAsync(factory, draftId);

            AssertSingleWinnerAndCurrentLoser(payloads);
        }
    }

    [Fact]
    public async Task ConclusaoComClaimDivergente_DeveRetornarCodigoEstavel()
    {
        await using var factory = new PostgreSqlComposeApiFactory();
        using var client = factory.CreateBotClient();
        var draftId = await factory.SeedPendingPublicationAsync();
        var claimResponse = await client.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{draftId}/discord/publicacoes/claim",
            new { Tipo = "Presenca" });
        claimResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{draftId}/discord/publicacao",
            new
            {
                Tipo = "Presenca",
                ClaimId = Guid.NewGuid(),
                DiscordGuildId = "guild-1",
                DiscordChannelId = "channel-1",
                MessageId = "message-1",
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error.Should().NotBeNull();
        error!.MessageCode.Should().Be(MessageCodes.DiscordPublicationClaimMismatch);
    }

    [Fact]
    public async Task ClaimExpirado_DeveExigirReconciliacaoESerRecusado()
    {
        await using var factory = new PostgreSqlComposeApiFactory();
        using var client = factory.CreateBotClient();
        var draftId = await factory.SeedPendingPublicationAsync();
        var firstClaim = await client.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{draftId}/discord/publicacoes/claim",
            new { Tipo = "Presenca" });
        firstClaim.StatusCode.Should().Be(HttpStatusCode.OK);
        await factory.ExpireClaimAsync(draftId);

        var secondClaim = await client.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{draftId}/discord/publicacoes/claim",
            new { Tipo = "Presenca" });

        secondClaim.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await ReadJsonAsync(secondClaim);
        payload.RootElement.GetProperty("adquirido").GetBoolean().Should().BeFalse();
        payload.RootElement.GetProperty("status").GetString().Should().Be("RequerReconciliacao");
        (await factory.GetPublicationStatusAsync(draftId)).Should().Be("RequerReconciliacao");
    }

    [Fact]
    public async Task Claim_DeveAceitarSomenteAutenticacaoInternaDoBot()
    {
        await using var factory = new PostgreSqlComposeApiFactory();
        var draftId = await factory.SeedPendingPublicationAsync();
        using var anonymousClient = factory.CreateClient();
        using var userClient = factory.CreateClient();
        userClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateJwt());

        var anonymousResponse = await anonymousClient.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{draftId}/discord/publicacoes/claim",
            new { Tipo = "Presenca" });
        var userResponse = await userClient.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{draftId}/discord/publicacoes/claim",
            new { Tipo = "Presenca" });

        anonymousResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        userResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var completionResponse = await anonymousClient.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{draftId}/discord/publicacao",
            new { Tipo = "Presenca", ClaimId = Guid.NewGuid(), MessageId = "message-1" });
        var failureResponse = await anonymousClient.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{draftId}/discord/publicacao/falha",
            new { Tipo = "Presenca", ClaimId = Guid.NewGuid(), ErroCodigo = "Timeout" });
        completionResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        failureResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ConclusaoEFalha_DevemExigirEConsumirClaimAtivo()
    {
        await using var factory = new PostgreSqlComposeApiFactory();
        using var client = factory.CreateBotClient();
        var publishedDraftId = await factory.SeedPendingPublicationAsync();
        var failedDraftId = await factory.SeedPendingPublicationAsync();
        var publishedClaimId = await AcquireClaimIdAsync(client, publishedDraftId);
        var failedClaimId = await AcquireClaimIdAsync(client, failedDraftId);

        var completionResponse = await client.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{publishedDraftId}/discord/publicacao",
            new
            {
                Tipo = "Presenca",
                ClaimId = publishedClaimId,
                DiscordGuildId = "guild-1",
                DiscordChannelId = "channel-1",
                MessageId = "message-1",
            });
        var failureResponse = await client.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{failedDraftId}/discord/publicacao/falha",
            new
            {
                Tipo = "Presenca",
                ClaimId = failedClaimId,
                DiscordGuildId = "guild-1",
                DiscordChannelId = "channel-1",
                ErroCodigo = "Timeout",
            });

        completionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        failureResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await factory.GetPublicationStatusAsync(publishedDraftId)).Should().Be("Publicada");
        (await factory.GetPublicationStatusAsync(failedDraftId)).Should().Be("Falha");
    }

    [Theory]
    [InlineData("Invalido")]
    [InlineData("999")]
    public async Task ClaimComTipoInvalido_DeveRetornarValidacaoLocalizada(string tipo)
    {
        await using var factory = new PostgreSqlComposeApiFactory();
        using var client = factory.CreateBotClient();
        var draftId = await factory.SeedPendingPublicationAsync();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{draftId}/discord/publicacoes/claim",
            new { Tipo = tipo });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error.Should().NotBeNull();
        error!.MessageCode.Should().Be(MessageCodes.ValidationError);
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    private static async Task<JsonDocument[]> SendConcurrentClaimsAsync(PostgreSqlComposeApiFactory factory, Guid draftId)
    {
        using var firstClient = factory.CreateBotClient();
        using var secondClient = factory.CreateBotClient();
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var readyCount = 0;

        async Task<HttpResponseMessage> SendAsync(HttpClient client)
        {
            if (Interlocked.Increment(ref readyCount) == 2)
            {
                ready.SetResult();
            }

            await release.Task;
            return await client.PostAsJsonAsync(
                $"/api/v1/draft-montagens/{draftId}/discord/publicacoes/claim",
                new { Tipo = "Presenca" });
        }

        var requests = new[] { SendAsync(firstClient), SendAsync(secondClient) };
        await ready.Task;
        release.SetResult();
        var responses = await Task.WhenAll(requests);
        responses.Should().OnlyContain(response => response.StatusCode == HttpStatusCode.OK);
        return await Task.WhenAll(responses.Select(ReadJsonAsync));
    }

    private static void AssertSingleWinnerAndCurrentLoser(JsonDocument[] payloads)
    {
        payloads.Count(payload => payload.RootElement.GetProperty("adquirido").GetBoolean()).Should().Be(1);
        var loser = payloads.Single(payload => !payload.RootElement.GetProperty("adquirido").GetBoolean());
        loser.RootElement.GetProperty("status").GetString().Should().Be("EmAndamento");
        loser.RootElement.GetProperty("claimId").ValueKind.Should().Be(JsonValueKind.Null);
    }

    private static async Task<Guid> AcquireClaimIdAsync(HttpClient client, Guid draftId)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{draftId}/discord/publicacoes/claim",
            new { Tipo = "Presenca" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var payload = await ReadJsonAsync(response);
        return payload.RootElement.GetProperty("claimId").GetGuid();
    }

    private sealed class PostgreSqlComposeApiFactory : WebApplicationFactory<Program>
    {
        private const string AdminConnection = "Host=postgres;Port=5432;Database=postgres;Username=postgres;Password=postgres";
        private const string Issuer = "RinhaDasLendas.DraftBehaviorTests";
        private const string Audience = "RinhaDasLendas.DraftBehaviorTests.Client";
        private const string JwtKey = "draft-behavior-tests-jwt-key-with-at-least-thirty-two-characters";
        private const string BotToken = "draft-behavior-tests-internal-token-with-at-least-thirty-two-characters";
        private readonly string _databaseName = $"rinha_draft_behavior_{Guid.NewGuid():N}";
        private readonly string _connectionString;
        private bool _databaseCreated;

        public PostgreSqlComposeApiFactory()
        {
            _connectionString = $"Host=postgres;Port=5432;Database={_databaseName};Username=postgres;Password=postgres";
            CreateDatabaseAsync().GetAwaiter().GetResult();
        }

        public HttpClient CreateBotClient()
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Add(BotInternalAuthOptions.HeaderName, BotToken);
            return client;
        }

        public string CreateJwt()
        {
            using var scope = Services.CreateScope();
            var userId = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>().Users
                .Select(user => user.Id)
                .First();
            var token = new JwtSecurityToken(
                Issuer,
                Audience,
                [
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Role, AuthRoles.Admin),
                ],
                expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey)),
                    SecurityAlgorithms.HmacSha256));
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<Guid> SeedPendingPublicationAsync()
        {
            _ = CreateClient();
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
            var responsibleUserId = await dbContext.Users.Select(user => user.Id).FirstOrDefaultAsync();
            if (responsibleUserId == Guid.Empty)
            {
                responsibleUserId = Guid.NewGuid();
                dbContext.Users.Add(new ApplicationUser
                {
                    Id = responsibleUserId,
                    Nome = "Usuario de teste",
                    UserName = $"draft-test-{responsibleUserId:N}",
                    NormalizedUserName = $"DRAFT-TEST-{responsibleUserId:N}",
                    Email = $"draft-test-{responsibleUserId:N}@example.com",
                    NormalizedEmail = $"DRAFT-TEST-{responsibleUserId:N}@EXAMPLE.COM",
                });
                await dbContext.SaveChangesAsync();
            }
            var draft = new DraftMontagem("Draft de teste", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
            draft.SolicitarRepublicacaoDiscord(
                DraftMontagemPublicacaoDiscordTipo.Presenca,
                responsibleUserId,
                "Preparar publicacao para teste",
                DateTimeOffset.UtcNow);
            dbContext.DraftMontagens.Add(draft);
            await dbContext.SaveChangesAsync();
            return draft.Id;
        }

        public async Task<Guid> SeedDraftWithoutPublicationAsync()
        {
            _ = CreateClient();
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
            var draft = new DraftMontagem("Draft sem publicacao", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
            dbContext.DraftMontagens.Add(draft);
            await dbContext.SaveChangesAsync();
            return draft.Id;
        }

        public async Task ExpireClaimAsync(Guid draftId)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                UPDATE draft_montagem_publicacoes_discord
                SET claim_expira_em = NOW() - INTERVAL '1 minute'
                WHERE draft_montagem_id = @draftId AND tipo = 'Presenca'
                """;
            command.Parameters.AddWithValue("draftId", draftId);
            await command.ExecuteNonQueryAsync();
        }

        public async Task<string?> GetPublicationStatusAsync(Guid draftId)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT status
                FROM draft_montagem_publicacoes_discord
                WHERE draft_montagem_id = @draftId AND tipo = 'Presenca'
                """;
            command.Parameters.AddWithValue("draftId", draftId);
            return (string?)await command.ExecuteScalarAsync();
        }

        public new async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
            if (!_databaseCreated)
            {
                return;
            }

            await using var connection = new NpgsqlConnection(AdminConnection);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)";
            await command.ExecuteNonQueryAsync();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder
                .UseEnvironment("IntegrationTesting")
                .UseSetting("DiscordBot:InternalToken", BotToken)
                .UseSetting("Authentication:Jwt:Issuer", Issuer)
                .UseSetting("Authentication:Jwt:Audience", Audience)
                .UseSetting("Authentication:Jwt:Key", JwtKey)
                .UseSetting("ConnectionStrings:RinhaDasLendas", _connectionString)
                .UseSetting("ConnectionStrings:DefaultConnection", _connectionString);
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:RinhaDasLendas"] = _connectionString,
                    ["ConnectionStrings:DefaultConnection"] = _connectionString,
                }));
        }

        private async Task CreateDatabaseAsync()
        {
            await using var connection = new NpgsqlConnection(AdminConnection);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE \"{_databaseName}\"";
            await command.ExecuteNonQueryAsync();
            _databaseCreated = true;
        }
    }
}
