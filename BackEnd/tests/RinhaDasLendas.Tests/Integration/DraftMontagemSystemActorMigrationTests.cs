using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Models;
using RinhaDasLendas.Infrastructure.Persistence;
using RinhaDasLendas.Infrastructure.Identity;
using RinhaDasLendas.Tests.Infrastructure;

namespace RinhaDasLendas.Tests.Integration;

public sealed class DraftMontagemSystemActorMigrationTests
{
    private const string PreviousMigration = "20260729124041_CorrigirNucleoCicloDraft";

    [Fact]
    public void Actor_DeveRepresentarUsuarioESistemaSemIdentidadeFicticia()
    {
        var usuarioId = Guid.NewGuid();

        DraftMontagemActor.User(usuarioId).Should().BeEquivalentTo(new { Tipo = DraftMontagemActorType.User, UsuarioId = (Guid?)usuarioId });
        DraftMontagemActor.System().Should().BeEquivalentTo(new { Tipo = DraftMontagemActorType.System, UsuarioId = (Guid?)null });
        var usuarioInvalido = () => DraftMontagemActor.User(Guid.Empty);
        usuarioInvalido.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AcaoAdministrativaEDto_DevemPreservarAutoriaSistemicaSemUsuario()
    {
        var usuarioId = Guid.NewGuid();
        var acaoUsuario = new DraftMontagemAcaoAdministrativa(
            "RepublicacaoDiscord:Presenca",
            DraftMontagemActor.User(usuarioId),
            null);
        var acao = new DraftMontagemAcaoAdministrativa(
            "EncerramentoPresencaAutomatico",
            DraftMontagemActor.System(),
            null);

        var dtoUsuario = DraftMontagemAcaoAdministrativaResponseDto.FromEntity(acaoUsuario);
        var dto = DraftMontagemAcaoAdministrativaResponseDto.FromEntity(acao);

        acaoUsuario.ResponsavelTipo.Should().Be(DraftMontagemActorType.User);
        acaoUsuario.ResponsavelUsuarioId.Should().Be(usuarioId);
        dtoUsuario.ResponsavelTipo.Should().Be(DraftMontagemActorType.User);
        dtoUsuario.ResponsavelUsuarioId.Should().Be(usuarioId);
        acao.ResponsavelTipo.Should().Be(DraftMontagemActorType.System);
        acao.ResponsavelUsuarioId.Should().BeNull();
        dto.ResponsavelTipo.Should().Be(DraftMontagemActorType.System);
        dto.ResponsavelUsuarioId.Should().BeNull();
    }

    [Fact]
    public async Task Migration_DeveFazerBackfillEAceitarSomenteCombinacoesValidasPreservandoFk()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAtMigrationAsync(PreviousMigration);
        var usuarioId = Guid.NewGuid();
        var draftId = Guid.NewGuid();
        var acaoLegadaId = Guid.NewGuid();

        await using (var before = database.CreateContext())
        {
            await InsertUserDraftAndLegacyActionAsync(before, usuarioId, draftId, acaoLegadaId);
            await before.Database.MigrateAsync();
        }

        await using var migrated = database.CreateContext();
        var legacyActorType = await migrated.Database
            .SqlQuery<string>($"SELECT responsavel_tipo AS \"Value\" FROM draft_montagem_acoes_administrativas WHERE id = {acaoLegadaId}")
            .SingleAsync();
        legacyActorType.Should().Be("User");

        await InsertActionAsync(migrated, Guid.NewGuid(), draftId, "System", null);

        var systemWithUser = () => InsertActionAsync(migrated, Guid.NewGuid(), draftId, "System", usuarioId);
        var userWithoutUser = () => InsertActionAsync(migrated, Guid.NewGuid(), draftId, "User", null);
        var unknownType = () => InsertActionAsync(migrated, Guid.NewGuid(), draftId, "Robot", null);
        var missingUser = () => InsertActionAsync(migrated, Guid.NewGuid(), draftId, "User", Guid.NewGuid());

        (await systemWithUser.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        (await userWithoutUser.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        (await unknownType.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        (await missingUser.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(PostgresErrorCodes.ForeignKeyViolation);
    }

    [Fact]
    public async Task Rollback_DeveRecusarAcaoSistemicaComGuardaOperacionalAntesDoDdl()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAtMigrationAsync(PreviousMigration);
        var usuarioId = Guid.NewGuid();
        var draftId = Guid.NewGuid();

        await using var context = database.CreateContext();
        await InsertUserDraftAndLegacyActionAsync(context, usuarioId, draftId, Guid.NewGuid());
        await context.Database.MigrateAsync();
        await InsertActionAsync(context, Guid.NewGuid(), draftId, "System", null);

        var rollback = () => context.Database.GetService<IMigrator>().MigrateAsync(PreviousMigration);

        var exception = (await rollback.Should().ThrowAsync<PostgresException>()).Which;
        exception.SqlState.Should().Be(PostgresErrorCodes.RaiseException);
        exception.MessageText.Should().Be(
            "Cannot downgrade draft system actor schema: System audit rows exist. Keep the forward-compatible additive schema and use roll-forward.");

        var actorTypeColumnStillExists = await context.Database
            .SqlQuery<bool>($"""
                SELECT EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE table_name = 'draft_montagem_acoes_administrativas'
                      AND column_name = 'responsavel_tipo') AS "Value"
                """)
            .SingleAsync();
        actorTypeColumnStillExists.Should().BeTrue();
    }

    [Fact]
    public async Task Rollback_DeveConcluirAntesDaPrimeiraAcaoSistemica()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAtMigrationAsync(PreviousMigration);
        var usuarioId = Guid.NewGuid();
        var draftId = Guid.NewGuid();

        await using var context = database.CreateContext();
        await InsertUserDraftAndLegacyActionAsync(context, usuarioId, draftId, Guid.NewGuid());
        await context.Database.MigrateAsync();

        await context.Database.GetService<IMigrator>().MigrateAsync(PreviousMigration);

        var actorTypeColumnExists = await context.Database
            .SqlQuery<bool>($"""
                SELECT EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE table_name = 'draft_montagem_acoes_administrativas'
                      AND column_name = 'responsavel_tipo') AS "Value"
                """)
            .SingleAsync();
        actorTypeColumnExists.Should().BeFalse();
        (await context.Database
            .SqlQuery<Guid>($"SELECT responsavel_usuario_id AS \"Value\" FROM draft_montagem_acoes_administrativas WHERE draft_montagem_id = {draftId}")
            .SingleAsync()).Should().Be(usuarioId);
    }

    [Fact]
    public async Task EfEEndpointAdministrativo_DevemPreservarEExporAtoresUserESystem()
    {
        await using var factory = new SystemActorApiFactory();
        var fixture = await factory.SeedActorsAsync();

        await using (var reloadedContext = factory.CreateContext())
        {
            var reloaded = await reloadedContext.DraftMontagens
                .AsNoTracking()
                .Include(draft => draft.AcoesAdministrativas)
                .SingleAsync(draft => draft.Id == fixture.DraftId);

            reloaded.AcoesAdministrativas.Should().ContainSingle(action =>
                action.ResponsavelTipo == DraftMontagemActorType.User &&
                action.ResponsavelUsuarioId == fixture.UserId);
            reloaded.AcoesAdministrativas.Should().ContainSingle(action =>
                action.ResponsavelTipo == DraftMontagemActorType.System &&
                action.ResponsavelUsuarioId == null);
        }

        using var admin = factory.CreateAdminClient(fixture.UserId);
        using var response = await admin.GetAsync($"/api/v1/draft-montagens/{fixture.DraftId}/administracao");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var actions = json.RootElement.GetProperty("acoesAdministrativas").EnumerateArray().ToList();
        var userAction = actions.Single(action => action.GetProperty("responsavelTipo").GetString() == "User");
        var systemAction = actions.Single(action => action.GetProperty("responsavelTipo").GetString() == "System");
        userAction.GetProperty("responsavelUsuarioId").GetGuid().Should().Be(fixture.UserId);
        systemAction.GetProperty("responsavelUsuarioId").ValueKind.Should().Be(JsonValueKind.Null);
    }

    private static Task<int> InsertUserDraftAndLegacyActionAsync(
        RinhaDasLendasDbContext context,
        Guid usuarioId,
        Guid draftId,
        Guid acaoId)
    {
        return context.Database.ExecuteSqlInterpolatedAsync($$"""
            INSERT INTO usuarios
                (id, nome, ativo, data_cadastro, data_atualizacao, email_confirmed,
                 phone_number_confirmed, two_factor_enabled, lockout_enabled, access_failed_count)
            VALUES
                ({{usuarioId}}, 'Usuário de teste', TRUE, NOW(), NOW(), FALSE, FALSE, FALSE, FALSE, 0);

            INSERT INTO draft_montagens
                (id, nome, status, modo, ciclo_versao, tamanho_equipe, quantidade_times,
                 quantidade_reservas, criterio_capitaes, duracao_turno_segundos,
                 versao_estado, data_cadastro, data_atualizacao)
            VALUES
                ({{draftId}}, 'Draft de migration', 'Aberta', 'Manual', 1, 5, 2,
                 0, 'Manual', 30, 0, NOW(), NOW());

            INSERT INTO draft_montagem_acoes_administrativas
                (id, draft_montagem_id, tipo, responsavel_usuario_id, motivo, registrado_em)
            VALUES
                ({{acaoId}}, {{draftId}}, 'AcaoLegada', {{usuarioId}}, 'Histórico preservado', NOW());
            """);
    }

    private static Task<int> InsertActionAsync(
        RinhaDasLendasDbContext context,
        Guid id,
        Guid draftId,
        string actorType,
        Guid? userId)
    {
        return context.Database.ExecuteSqlInterpolatedAsync($$"""
            INSERT INTO draft_montagem_acoes_administrativas
                (id, draft_montagem_id, tipo, responsavel_tipo, responsavel_usuario_id, registrado_em)
            VALUES
                ({{id}}, {{draftId}}, 'AcaoTeste', {{actorType}}, {{userId}}, NOW())
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
            var databaseName = $"rinha_draft_actor_{Guid.NewGuid():N}";

            await using var connection = new NpgsqlConnection(adminConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
            await command.ExecuteNonQueryAsync();

            var connectionString = new NpgsqlConnectionStringBuilder(adminConnectionString) { Database = databaseName }.ConnectionString;
            var database = new PostgreSqlTestDatabase(databaseName, adminConnectionString, connectionString);
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

        public async ValueTask DisposeAsync()
        {
            await using var connection = new NpgsqlConnection(_adminConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)";
            await command.ExecuteNonQueryAsync();
        }
    }

    private sealed class SystemActorApiFactory : SecurityApiFactory
    {
        public SystemActorApiFactory() : base(useIsolatedPostgreSql: true)
        {
        }

        public HttpClient CreateAdminClient(Guid userId) => CreateJwtClient(userId, AuthRoles.Admin);

        public RinhaDasLendasDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<RinhaDasLendasDbContext>()
                .UseNpgsql(ConnectionString)
                .Options;
            return new RinhaDasLendasDbContext(options);
        }

        public async Task<(Guid DraftId, Guid UserId)> SeedActorsAsync()
        {
            _ = CreateClient();
            await using var context = CreateContext();
            var userId = Guid.NewGuid();
            context.Users.Add(new ApplicationUser
            {
                Id = userId,
                Nome = "Administrador",
                UserName = $"system-actor-{userId:N}",
                NormalizedUserName = $"SYSTEM-ACTOR-{userId:N}",
            });
            var draft = new DraftMontagem("Draft com autoria", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
            draft.SolicitarRepublicacaoDiscord(
                DraftMontagemPublicacaoDiscordTipo.Presenca,
                userId,
                "Ação de usuário",
                DateTimeOffset.UtcNow,
                confirmarAusenciaPublicacao: true);
            draft.Cancelar("Ação automática", DraftMontagemActor.System());
            context.DraftMontagens.Add(draft);
            await context.SaveChangesAsync();
            return (draft.Id, userId);
        }
    }
}
