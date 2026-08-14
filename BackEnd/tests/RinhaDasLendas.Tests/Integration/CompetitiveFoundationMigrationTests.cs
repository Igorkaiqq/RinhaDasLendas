using System.Data.Common;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Tests.Fixtures;

namespace RinhaDasLendas.Tests.Integration;

public sealed partial class CompetitiveFoundationMigrationTests
{
    private const string TargetMigration = "20260728000000_AddCompetitiveSeasonFoundation";

    private static readonly IReadOnlyDictionary<string, string[]> ExpectedColumns =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["calendarios_competitivos"] =
                ["id", "season_ativa_id", "versao", "atualizado_em", "atualizado_por_usuario_id"],
            ["seasons"] =
            [
                "id", "nome", "ano", "ordem_no_ano", "data_inicio", "data_fim_exclusiva", "estado", "versao",
                "criada_em", "atualizada_em", "ativada_em", "encerrada_em", "criada_por_usuario_id",
                "atualizada_por_usuario_id",
            ],
            ["competicoes"] =
            [
                "id", "season_id", "nome", "codigo", "circuito_diario", "versao", "criada_em", "atualizada_em",
                "criada_por_usuario_id", "atualizada_por_usuario_id",
            ],
            ["rodadas"] = ["id", "competicao_id", "nome", "ordem", "versao", "criada_em", "atualizada_em"],
            ["versoes_regras"] =
                ["id", "competicao_escopo_id", "season_id", "competicao_id", "numero", "formato", "modo_draft", "publicada_em", "publicada_por_usuario_id"],
            ["eventos_competitivos"] =
            [
                "id", "season_id", "nome", "modo_draft", "versao", "criado_em", "atualizado_em",
                "criado_por_usuario_id", "atualizado_por_usuario_id",
            ],
            ["evento_times"] = ["id", "evento_id", "time_id", "ordem", "nome_snapshot", "tag_snapshot"],
            ["series"] =
            [
                "id", "competicao_escopo_id", "season_id", "competicao_id", "rodada_id", "versao_regras_id", "evento_id",
                "draft_montagem_id", "tipo", "formato", "modo_draft", "fearless_habilitado", "estado",
                "agendada_para", "data_local", "lado_vencedor_id", "revisao_necessaria", "versao", "criada_em",
                "atualizada_em", "concluida_em", "criada_por_usuario_id", "atualizada_por_usuario_id",
            ],
            ["lados_series"] =
            [
                "id", "serie_id", "ordem", "tipo", "origem_id", "nome_snapshot", "tag_snapshot",
                "capitao_jogador_id", "capitao_nome_snapshot",
            ],
            ["participantes_esperados_series"] =
                ["id", "lado_serie_id", "jogador_id", "nome_snapshot", "ordem"],
            ["partidas"] =
            [
                "id", "serie_id", "ordem", "estado", "lado_vencedor_id", "motivo_termino", "decisao_picks_remake",
                "conflito_fearless", "versao", "criada_em", "atualizada_em", "confirmada_em",
            ],
            ["picks_partidas"] =
                ["id", "partida_id", "lado_serie_id", "champion_id", "ordem", "versao_fato", "valido", "registrado_em", "serie_id"],
            ["registros_auditoria_competitiva"] =
            [
                "id", "recurso_tipo", "recurso_id", "acao", "ator_usuario_id", "capacidade", "justificativa",
                "valor_anterior", "valor_posterior", "correlation_id", "ocorrido_em",
            ],
            ["operacoes_idempotentes"] =
            [
                "id", "ator_usuario_id", "metodo", "rota", "chave", "request_hash", "status_code", "recurso_tipo",
                "recurso_id", "resposta_minima", "criada_em", "expira_em",
            ],
        };

    private static readonly IReadOnlyDictionary<string, string[]> ExpectedRequiredColumns =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["calendarios_competitivos"] = ["id", "versao", "atualizado_em", "atualizado_por_usuario_id"],
            ["seasons"] =
            [
                "id", "nome", "ano", "ordem_no_ano", "data_inicio", "data_fim_exclusiva", "estado", "versao",
                "criada_em", "atualizada_em", "criada_por_usuario_id", "atualizada_por_usuario_id",
            ],
            ["competicoes"] =
            [
                "id", "season_id", "nome", "codigo", "circuito_diario", "versao", "criada_em", "atualizada_em",
                "criada_por_usuario_id", "atualizada_por_usuario_id",
            ],
            ["rodadas"] = ["id", "competicao_id", "nome", "ordem", "versao", "criada_em", "atualizada_em"],
            ["versoes_regras"] =
                ["id", "competicao_escopo_id", "season_id", "numero", "formato", "modo_draft", "publicada_em", "publicada_por_usuario_id"],
            ["eventos_competitivos"] =
            [
                "id", "season_id", "nome", "modo_draft", "versao", "criado_em", "atualizado_em",
                "criado_por_usuario_id", "atualizado_por_usuario_id",
            ],
            ["evento_times"] = ["id", "evento_id", "time_id", "ordem", "nome_snapshot"],
            ["series"] =
            [
                "id", "competicao_escopo_id", "season_id", "versao_regras_id", "tipo", "formato", "modo_draft", "fearless_habilitado",
                "estado", "agendada_para", "revisao_necessaria", "versao", "criada_em", "atualizada_em",
                "criada_por_usuario_id", "atualizada_por_usuario_id",
            ],
            ["lados_series"] = ["id", "serie_id", "ordem", "tipo", "origem_id", "nome_snapshot"],
            ["participantes_esperados_series"] = ["id", "lado_serie_id", "jogador_id", "nome_snapshot", "ordem"],
            ["partidas"] =
                ["id", "serie_id", "ordem", "estado", "conflito_fearless", "versao", "criada_em", "atualizada_em"],
            ["picks_partidas"] =
                ["id", "partida_id", "lado_serie_id", "champion_id", "ordem", "versao_fato", "valido", "registrado_em", "serie_id"],
            ["registros_auditoria_competitiva"] =
            [
                "id", "recurso_tipo", "recurso_id", "acao", "ator_usuario_id", "capacidade", "correlation_id",
                "ocorrido_em",
            ],
            ["operacoes_idempotentes"] =
            [
                "id", "ator_usuario_id", "metodo", "rota", "chave", "request_hash", "status_code", "recurso_tipo",
                "resposta_minima", "criada_em", "expira_em",
            ],
        };

    private static readonly string[] ExpectedUuidPrimaryKeys =
        ExpectedColumns.Keys.Select(table => $"{table}.id:uuid").Order(StringComparer.Ordinal).ToArray();

    private static readonly ExpectedForeignKey[] ExpectedForeignKeyDefinitions =
    [
        new("fk_calendarios_competitivos_season_ativa_id", "calendarios_competitivos", ["season_ativa_id"], "seasons", ["id"]),
        new("fk_calendarios_competitivos_atualizado_por_usuario_id", "calendarios_competitivos", ["atualizado_por_usuario_id"], "usuarios", ["id"]),
        new("fk_seasons_criada_por_usuario_id", "seasons", ["criada_por_usuario_id"], "usuarios", ["id"]),
        new("fk_seasons_atualizada_por_usuario_id", "seasons", ["atualizada_por_usuario_id"], "usuarios", ["id"]),
        new("fk_competicoes_season_id", "competicoes", ["season_id"], "seasons", ["id"]),
        new("fk_competicoes_criada_por_usuario_id", "competicoes", ["criada_por_usuario_id"], "usuarios", ["id"]),
        new("fk_competicoes_atualizada_por_usuario_id", "competicoes", ["atualizada_por_usuario_id"], "usuarios", ["id"]),
        new("fk_rodadas_competicao_id", "rodadas", ["competicao_id"], "competicoes", ["id"]),
        new("fk_versoes_regras_season_id", "versoes_regras", ["season_id"], "seasons", ["id"]),
        new("fk_versoes_regras_competicao_id", "versoes_regras", ["competicao_id", "season_id"], "competicoes", ["id", "season_id"]),
        new("fk_versoes_regras_publicada_por_usuario_id", "versoes_regras", ["publicada_por_usuario_id"], "usuarios", ["id"]),
        new("fk_eventos_competitivos_season_id", "eventos_competitivos", ["season_id"], "seasons", ["id"]),
        new("fk_eventos_competitivos_criado_por_usuario_id", "eventos_competitivos", ["criado_por_usuario_id"], "usuarios", ["id"]),
        new("fk_eventos_competitivos_atualizado_por_usuario_id", "eventos_competitivos", ["atualizado_por_usuario_id"], "usuarios", ["id"]),
        new("fk_evento_times_evento_id", "evento_times", ["evento_id"], "eventos_competitivos", ["id"]),
        new("fk_evento_times_time_id", "evento_times", ["time_id"], "times", ["id"]),
        new("fk_series_season_id", "series", ["season_id"], "seasons", ["id"]),
        new("fk_series_competicao_id", "series", ["competicao_id", "season_id"], "competicoes", ["id", "season_id"]),
        new("fk_series_rodada_id", "series", ["rodada_id", "competicao_id"], "rodadas", ["id", "competicao_id"]),
        new("fk_series_versao_regras_id", "series", ["versao_regras_id", "season_id"], "versoes_regras", ["id", "season_id"]),
        new("fk_series_versao_regras_competicao_id", "series", ["versao_regras_id", "competicao_escopo_id"], "versoes_regras", ["id", "competicao_escopo_id"]),
        new("fk_series_evento_id", "series", ["evento_id", "season_id"], "eventos_competitivos", ["id", "season_id"]),
        new("fk_series_draft_montagem_id", "series", ["draft_montagem_id"], "draft_montagens", ["id"]),
        new("fk_series_lado_vencedor_id", "series", ["lado_vencedor_id", "id"], "lados_series", ["id", "serie_id"]),
        new("fk_series_criada_por_usuario_id", "series", ["criada_por_usuario_id"], "usuarios", ["id"]),
        new("fk_series_atualizada_por_usuario_id", "series", ["atualizada_por_usuario_id"], "usuarios", ["id"]),
        new("fk_lados_series_serie_id", "lados_series", ["serie_id"], "series", ["id"]),
        new("fk_lados_series_capitao_jogador_id", "lados_series", ["capitao_jogador_id"], "jogadores", ["id"]),
        new("fk_participantes_esperados_series_lado_serie_id", "participantes_esperados_series", ["lado_serie_id"], "lados_series", ["id"]),
        new("fk_participantes_esperados_series_jogador_id", "participantes_esperados_series", ["jogador_id"], "jogadores", ["id"]),
        new("fk_partidas_serie_id", "partidas", ["serie_id"], "series", ["id"]),
        new("fk_partidas_lado_vencedor_id", "partidas", ["lado_vencedor_id", "serie_id"], "lados_series", ["id", "serie_id"]),
        new("fk_picks_partidas_partida_id", "picks_partidas", ["partida_id", "serie_id"], "partidas", ["id", "serie_id"]),
        new("fk_picks_partidas_lado_serie_id", "picks_partidas", ["lado_serie_id", "serie_id"], "lados_series", ["id", "serie_id"]),
        new("fk_registros_auditoria_competitiva_ator_usuario_id", "registros_auditoria_competitiva", ["ator_usuario_id"], "usuarios", ["id"]),
        new("fk_operacoes_idempotentes_ator_usuario_id", "operacoes_idempotentes", ["ator_usuario_id"], "usuarios", ["id"]),
    ];

    private static readonly string[] ExpectedForeignKeys = ExpectedForeignKeyDefinitions
        .SelectMany(foreignKey => foreignKey.Columns.Zip(
            foreignKey.PrincipalColumns,
            (column, principalColumn) => $"{foreignKey.Table}.{column}->{foreignKey.PrincipalTable}.{principalColumn}"))
        .ToArray();

    private static readonly string[] ExpectedNonPrimaryIndexes = BuildExpectedNonPrimaryIndexes();

    private static readonly string[] ExpectedNonPrimaryConstraints = BuildExpectedNonPrimaryConstraints();

    private static readonly IReadOnlyDictionary<string, string> ExpectedColumnTypes = BuildExpectedColumnTypes();

    private static string[] FeatureTables => ExpectedColumns.Keys.Order(StringComparer.Ordinal).ToArray();

    [Fact]
    public async Task Migration_DeveSerAUltimaDaAssemblyESubirSchemaVazio()
    {
        await using var database = await CompetitivePostgresFixture.CreateEmptyAsync();
        await using var context = database.CreateContext();
        _ = AssertTargetMigrationOrder(context);

        await context.GetService<IMigrator>().MigrateAsync(TargetMigration);

        (await ReadAppliedMigrationsAsync(database)).Should().ContainInOrder(TargetMigration);
        await AssertExactSchemaCatalogAsync(database);
    }

    [Fact]
    public async Task Migration_DeveSubirDoPredecessorImediatoSemAlterarDadosAnteriores()
    {
        await using var database = await CompetitivePostgresFixture.CreateEmptyAsync();
        await using var context = database.CreateContext();
        var predecessor = AssertTargetMigrationOrder(context);
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(predecessor);
        var seeded = await SeedPredecessorDataAsync(database);

        await migrator.MigrateAsync(TargetMigration);

        await AssertPredecessorDataAsync(database, seeded);
        (await ReadAppliedMigrationsAsync(database)).Should().ContainInOrder(predecessor, TargetMigration);
        await AssertExactSchemaCatalogAsync(database);
    }

    [Fact]
    public async Task Migration_DeveFazerDowngradeCompletoEPreservarDadosDoPredecessor()
    {
        await using var database = await CompetitivePostgresFixture.CreateEmptyAsync();
        await using var context = database.CreateContext();
        var predecessor = AssertTargetMigrationOrder(context);
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(predecessor);
        var seeded = await SeedPredecessorDataAsync(database);
        var predecessorCatalog = await CapturePublicSchemaCatalogAsync(database);
        await migrator.MigrateAsync(TargetMigration);

        await migrator.MigrateAsync(predecessor);

        await AssertPredecessorDataAsync(database, seeded);
        (await ReadAppliedMigrationsAsync(database)).Should().NotContain(TargetMigration);
        (await CapturePublicSchemaCatalogAsync(database)).Should().Equal(predecessorCatalog);
        (await QueryAsync(
            database,
            "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_name = ANY (@tables)",
            new NpgsqlParameter<string[]>("tables", FeatureTables))).Should().BeEmpty();
        (await QueryAsync(
            database,
            "SELECT tablename || '.' || indexname FROM pg_indexes WHERE schemaname = 'public' AND tablename = ANY (@tables)",
            new NpgsqlParameter<string[]>("tables", FeatureTables))).Should().BeEmpty();
        (await QueryAsync(
            database,
            """
            SELECT constraint_record.conname
            FROM pg_constraint constraint_record
            LEFT JOIN pg_class child ON child.oid = constraint_record.conrelid
            LEFT JOIN pg_class parent ON parent.oid = constraint_record.confrelid
            WHERE child.relname = ANY (@tables) OR parent.relname = ANY (@tables)
            """,
            new NpgsqlParameter<string[]>("tables", FeatureTables))).Should().BeEmpty();
    }

    [Fact]
    public async Task SchemaCompetitivo_DeveTerCatalogosExatosENomesSnakeCase()
    {
        await using var database = await CreateTargetDatabaseAsync();

        await AssertExactSchemaCatalogAsync(database);

        var identifiers = await QueryAsync(
            database,
            """
            SELECT identifier
            FROM (
                SELECT table_name AS identifier
                FROM information_schema.tables
                WHERE table_schema = 'public' AND table_name = ANY (@tables)
                UNION ALL
                SELECT column_name
                FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name = ANY (@tables)
                UNION ALL
                SELECT indexname
                FROM pg_indexes
                WHERE schemaname = 'public' AND tablename = ANY (@tables)
                UNION ALL
                SELECT constraint_record.conname
                FROM pg_constraint constraint_record
                JOIN pg_class table_record ON table_record.oid = constraint_record.conrelid
                JOIN pg_namespace schema_record ON schema_record.oid = table_record.relnamespace
                WHERE schema_record.nspname = 'public' AND table_record.relname = ANY (@tables)
            ) identifiers
            ORDER BY identifier
            """,
            new NpgsqlParameter<string[]>("tables", FeatureTables));
        identifiers.Should().OnlyContain(identifier => SnakeCaseRegex().IsMatch(identifier));
    }

    [Fact]
    public async Task FksCompetitivas_DevemSerExatasEUsarRestrictOuNoAction()
    {
        await using var database = await CreateTargetDatabaseAsync();
        var foreignKeys = await ReadForeignKeysAsync(database);

        foreignKeys.Select(foreignKey => foreignKey[..foreignKey.LastIndexOf(':')])
            .Should().BeEquivalentTo(ExpectedForeignKeys);
        foreignKeys.Should().OnlyContain(foreignKey =>
            foreignKey.EndsWith(":a", StringComparison.Ordinal)
            || foreignKey.EndsWith(":r", StringComparison.Ordinal));
        foreignKeys.Should().NotContain(foreignKey =>
            foreignKey.EndsWith(":c", StringComparison.Ordinal)
            || foreignKey.EndsWith(":n", StringComparison.Ordinal)
            || foreignKey.EndsWith(":d", StringComparison.Ordinal));

        var foreignKeysWithoutIndexedPrefix = await QueryAsync(
            database,
            """
            SELECT constraint_record.conname
            FROM pg_constraint constraint_record
            WHERE constraint_record.contype = 'f'
              AND constraint_record.conrelid::regclass::text = ANY (@tables)
              AND NOT EXISTS (
                  SELECT 1
                  FROM pg_index index_record
                  WHERE index_record.indrelid = constraint_record.conrelid
                    AND (
                        index_record.indkey::text = array_to_string(constraint_record.conkey, ' ')
                        OR index_record.indkey::text LIKE array_to_string(constraint_record.conkey, ' ') || ' %'
                    )
              )
            ORDER BY constraint_record.conname
            """,
            new NpgsqlParameter<string[]>("tables", FeatureTables));
        foreignKeysWithoutIndexedPrefix.Should().BeEmpty();
    }

    [Fact]
    public async Task ConstraintsEIndicesCompetitivos_DevemTerCatalogoExato()
    {
        await using var database = await CreateTargetDatabaseAsync();
        var indexes = await QueryAsync(
            database,
            """
            SELECT indexname || ':' || indexdef
            FROM pg_indexes index_catalog
            JOIN pg_class table_record ON table_record.relname = index_catalog.tablename
            JOIN pg_namespace schema_record ON schema_record.oid = table_record.relnamespace AND schema_record.nspname = index_catalog.schemaname
            JOIN pg_index index_record ON index_record.indexrelid = to_regclass(index_catalog.schemaname || '.' || index_catalog.indexname)
            WHERE index_catalog.schemaname = 'public'
              AND index_catalog.tablename = ANY (@tables)
              AND NOT index_record.indisprimary
            ORDER BY index_catalog.indexname
            """,
            new NpgsqlParameter<string[]>("tables", FeatureTables));
        indexes.Select(NormalizeCatalogDefinition).Should().Equal(ExpectedNonPrimaryIndexes);

        var constraints = await QueryAsync(
            database,
            """
            SELECT constraint_record.conname || ':' || constraint_record.contype::text || ':' || pg_get_constraintdef(constraint_record.oid, true)
            FROM pg_constraint constraint_record
            JOIN pg_class table_record ON table_record.oid = constraint_record.conrelid
            JOIN pg_namespace schema_record ON schema_record.oid = table_record.relnamespace
            WHERE schema_record.nspname = 'public'
              AND table_record.relname = ANY (@tables)
              AND constraint_record.contype <> 'p'
            ORDER BY constraint_record.conname
            """,
            new NpgsqlParameter<string[]>("tables", FeatureTables));
        constraints.Select(NormalizeConstraintDefinition).Should().Equal(ExpectedNonPrimaryConstraints);
    }

    [Fact]
    public async Task Season_DeveRecusarIntervaloInvalido()
    {
        await using var database = await CreateTargetDatabaseAsync();
        var actorId = await SeedActorAsync(database);

        Func<Task> insertAction = () => InsertSeasonAsync(
            database, actorId, Guid.NewGuid(), "Inválida", 2026, 1, "2026-02-01", "2026-02-01", "Planejada");
        await AssertPostgresFailureAsync(insertAction, PostgresErrorCodes.CheckViolation);

        var seasonId = Guid.NewGuid();
        await InsertSeasonAsync(database, actorId, seasonId, "Válida", 2026, 1, "2026-01-01", "2026-02-01", "Planejada");
        Func<Task> updateAction = () => ExecuteAsync(
            database,
            "UPDATE seasons SET data_fim_exclusiva = data_inicio WHERE id = @id",
            new NpgsqlParameter("id", seasonId));

        await AssertPostgresFailureAsync(updateAction, PostgresErrorCodes.CheckViolation);
    }

    [Fact]
    public async Task Season_DeveRecusarAnoEOrdemDuplicados()
    {
        await using var database = await CreateTargetDatabaseAsync();
        var actorId = await SeedActorAsync(database);
        await InsertSeasonAsync(database, actorId, Guid.NewGuid(), "Primeira", 2026, 1, "2026-01-01", "2026-02-01", "Planejada");

        Func<Task> action = () => InsertSeasonAsync(
            database, actorId, Guid.NewGuid(), "Duplicada", 2026, 1, "2026-02-01", "2026-03-01", "Planejada");

        await AssertPostgresFailureAsync(action, PostgresErrorCodes.UniqueViolation);
    }

    [Fact]
    public async Task Season_DeveRecusarDuasAtivas()
    {
        await using var database = await CreateTargetDatabaseAsync();
        var actorId = await SeedActorAsync(database);
        await InsertSeasonAsync(database, actorId, Guid.NewGuid(), "Ativa 1", 2026, 1, "2026-01-01", "2026-02-01", "Ativa");

        Func<Task> action = () => InsertSeasonAsync(
            database, actorId, Guid.NewGuid(), "Ativa 2", 2026, 2, "2026-02-01", "2026-03-01", "Ativa");

        await AssertPostgresFailureAsync(action, PostgresErrorCodes.UniqueViolation);
    }

    [Fact]
    public async Task Season_DeveRecusarSobreposicaoEAceitarFronteiraFimInicio()
    {
        await using var database = await CreateTargetDatabaseAsync();
        var actorId = await SeedActorAsync(database);
        await InsertSeasonAsync(database, actorId, Guid.NewGuid(), "Base", 2026, 1, "2026-01-01", "2026-02-01", "Planejada");
        var boundarySeasonId = Guid.NewGuid();
        await InsertSeasonAsync(database, actorId, boundarySeasonId, "Fronteira", 2026, 2, "2026-02-01", "2026-03-01", "Planejada");

        Func<Task> insertAction = () => InsertSeasonAsync(
            database, actorId, Guid.NewGuid(), "Sobreposta", 2026, 3, "2026-01-31", "2026-02-15", "Planejada");
        await AssertPostgresFailureAsync(insertAction, PostgresErrorCodes.ExclusionViolation);

        Func<Task> updateAction = () => ExecuteAsync(
            database,
            "UPDATE seasons SET data_inicio = DATE '2026-01-31' WHERE id = @id",
            new NpgsqlParameter("id", boundarySeasonId));

        await AssertPostgresFailureAsync(updateAction, PostgresErrorCodes.ExclusionViolation);
        (await ScalarAsync<long>(database, "SELECT COUNT(*) FROM seasons")).Should().Be(2);
    }

    [Fact]
    public async Task Rodada_DeveRecusarOrdemDuplicadaNaCompeticao()
    {
        await using var database = await CreateTargetDatabaseAsync();
        var foundation = await SeedCompetitionAsync(database, false);
        await InsertRoundAsync(database, foundation.CompetitionId, Guid.NewGuid(), "Rodada 1", 1);

        Func<Task> action = () => InsertRoundAsync(database, foundation.CompetitionId, Guid.NewGuid(), "Rodada repetida", 1);

        await AssertPostgresFailureAsync(action, PostgresErrorCodes.UniqueViolation);
    }

    [Fact]
    public async Task Competicao_DeveRecusarDoisCircuitosDiariosNaSeason()
    {
        await using var database = await CreateTargetDatabaseAsync();
        var foundation = await SeedCompetitionAsync(database, true);

        Func<Task> action = () => InsertCompetitionAsync(
            database, foundation.ActorId, foundation.SeasonId, Guid.NewGuid(), "Circuito 2", "circuito-2", true);

        await AssertPostgresFailureAsync(action, PostgresErrorCodes.UniqueViolation);
    }

    [Fact]
    public async Task Competicao_DeveReservarUuidZeroParaEscoposGerais()
    {
        await using var database = await CreateTargetDatabaseAsync();
        var foundation = await SeedCompetitionAsync(database, false);

        Func<Task> action = () => InsertCompetitionAsync(
            database, foundation.ActorId, foundation.SeasonId, Guid.Empty, "Escopo geral", "zero", false);

        await AssertPostgresFailureAsync(action, PostgresErrorCodes.CheckViolation);
    }

    [Fact]
    public async Task Competicao_DeveRecusarCodigoDuplicadoSemDiferenciarMaiusculasNaSeasonEAceitarEmOutraSeason()
    {
        await using var database = await CreateTargetDatabaseAsync();
        var foundation = await SeedCompetitionAsync(database, false);

        Func<Task> duplicate = () => InsertCompetitionAsync(
            database, foundation.ActorId, foundation.SeasonId, Guid.NewGuid(), "Código repetido", "BASE", false);
        await AssertPostgresFailureAsync(duplicate, PostgresErrorCodes.UniqueViolation);

        var secondSeasonId = Guid.NewGuid();
        await InsertSeasonAsync(
            database, foundation.ActorId, secondSeasonId, "Season seguinte", 2027, 1, "2027-01-01", "2028-01-01", "Planejada");
        await InsertCompetitionAsync(
            database, foundation.ActorId, secondSeasonId, Guid.NewGuid(), "Código isolado", "base", false);
        (await ScalarAsync<long>(database, "SELECT COUNT(*) FROM competicoes WHERE codigo = 'base'")).Should().Be(2);
    }


    [Fact]
    public async Task VersaoRegras_DeveIsolarNumeroPorEscopoGeralECompeticao()
    {
        await using var database = await CreateTargetDatabaseAsync();
        var foundation = await SeedCompetitionAsync(database, false);
        await InsertRulesAsync(database, foundation.ActorId, foundation.SeasonId, null, Guid.NewGuid(), 1);

        Func<Task> duplicateGeneral = () =>
            InsertRulesAsync(database, foundation.ActorId, foundation.SeasonId, null, Guid.NewGuid(), 1);
        await AssertPostgresFailureAsync(duplicateGeneral, PostgresErrorCodes.UniqueViolation);

        await InsertRulesAsync(
            database, foundation.ActorId, foundation.SeasonId, foundation.CompetitionId, Guid.NewGuid(), 1);
        Func<Task> duplicateCompetition = () => InsertRulesAsync(
            database, foundation.ActorId, foundation.SeasonId, foundation.CompetitionId, Guid.NewGuid(), 1);
        await AssertPostgresFailureAsync(duplicateCompetition, PostgresErrorCodes.UniqueViolation);

        var secondCompetitionId = Guid.NewGuid();
        await InsertCompetitionAsync(
            database, foundation.ActorId, foundation.SeasonId, secondCompetitionId, "Competição 2", "segunda", false);
        await InsertRulesAsync(database, foundation.ActorId, foundation.SeasonId, secondCompetitionId, Guid.NewGuid(), 1);
        (await ScalarAsync<long>(database, "SELECT COUNT(*) FROM versoes_regras WHERE numero = 1")).Should().Be(3);
    }

    [Fact]
    public async Task LadoSerie_DeveRecusarOrdemDuplicadaEAceitarAsDuasOrdens()
    {
        await using var database = await CreateTargetDatabaseAsync();
        var foundation = await SeedSeriesAsync(database);
        await InsertSideAsync(database, foundation.FirstSeriesId, Guid.NewGuid(), 1, "Azul");

        Func<Task> duplicate = () =>
            InsertSideAsync(database, foundation.FirstSeriesId, Guid.NewGuid(), 1, "Azul repetido");
        await AssertPostgresFailureAsync(duplicate, PostgresErrorCodes.UniqueViolation);

        await InsertSideAsync(database, foundation.FirstSeriesId, Guid.NewGuid(), 2, "Vermelho");
        (await ScalarAsync<long>(
            database,
            "SELECT COUNT(*) FROM lados_series WHERE serie_id = @serie_id",
            new NpgsqlParameter("serie_id", foundation.FirstSeriesId))).Should().Be(2);
    }

    [Fact]
    public async Task Idempotencia_DeveSerUnicaNaIdentidadeCompletaEIsoladaPorCadaDimensao()
    {
        await using var database = await CreateTargetDatabaseAsync();
        var firstActorId = await SeedActorAsync(database);
        var secondActorId = await SeedActorAsync(database);
        await InsertIdempotencyAsync(database, firstActorId, "POST", "/series", "key-1");

        Func<Task> duplicate = () => InsertIdempotencyAsync(database, firstActorId, "POST", "/series", "key-1");
        await AssertPostgresFailureAsync(duplicate, PostgresErrorCodes.UniqueViolation);

        await InsertIdempotencyAsync(database, secondActorId, "POST", "/series", "key-1");
        await InsertIdempotencyAsync(database, firstActorId, "PUT", "/series", "key-1");
        await InsertIdempotencyAsync(database, firstActorId, "POST", "/partidas", "key-1");
        await InsertIdempotencyAsync(database, firstActorId, "POST", "/series", "key-2");
        (await ScalarAsync<long>(database, "SELECT COUNT(*) FROM operacoes_idempotentes")).Should().Be(5);
    }

    [Fact]
    public async Task Partida_DevePertencerAUmaUnicaSerie()
    {
        await using var database = await CreateTargetDatabaseAsync();
        var foundation = await SeedSeriesAsync(database);
        var matchId = Guid.NewGuid();
        await InsertMatchAsync(database, foundation.FirstSeriesId, matchId, 1);

        Func<Task> duplicateOrder = () => InsertMatchAsync(database, foundation.FirstSeriesId, Guid.NewGuid(), 1);
        await AssertPostgresFailureAsync(duplicateOrder, PostgresErrorCodes.UniqueViolation);

        Func<Task> duplicateIdentityInSecondSeries = () =>
            InsertMatchAsync(database, foundation.SecondSeriesId, matchId, 1);
        await AssertPostgresFailureAsync(duplicateIdentityInSecondSeries, PostgresErrorCodes.UniqueViolation);
        (await ScalarAsync<Guid>(database, "SELECT serie_id FROM partidas WHERE id = @id", new NpgsqlParameter("id", matchId)))
            .Should().Be(foundation.FirstSeriesId);
    }

    [Fact]
    public async Task VersaoRegras_DeveRecusarCompeticaoDeOutraSeason()
    {
        await using var database = await CreateTargetDatabaseAsync();
        var actorId = await SeedActorAsync(database);
        var firstSeasonId = Guid.NewGuid();
        var secondSeasonId = Guid.NewGuid();
        await InsertSeasonAsync(database, actorId, firstSeasonId, "Season 1", 2026, 1, "2026-01-01", "2027-01-01", "Planejada");
        await InsertSeasonAsync(database, actorId, secondSeasonId, "Season 2", 2027, 1, "2027-01-01", "2028-01-01", "Planejada");
        var secondCompetitionId = Guid.NewGuid();
        await InsertCompetitionAsync(database, actorId, secondSeasonId, secondCompetitionId, "Competição 2", "segunda", false);

        Func<Task> action = () => InsertRulesAsync(
            database, actorId, firstSeasonId, secondCompetitionId, Guid.NewGuid(), 1);

        await AssertPostgresFailureAsync(action, PostgresErrorCodes.ForeignKeyViolation);
    }

    [Fact]
    public async Task Serie_DeveRecusarRelacionamentosDeOutraSeasonOuCompeticao()
    {
        await using var database = await CreateTargetDatabaseAsync();
        var actorId = await SeedActorAsync(database);
        var firstSeasonId = Guid.NewGuid();
        var secondSeasonId = Guid.NewGuid();
        await InsertSeasonAsync(database, actorId, firstSeasonId, "Season 1", 2026, 1, "2026-01-01", "2027-01-01", "Planejada");
        await InsertSeasonAsync(database, actorId, secondSeasonId, "Season 2", 2027, 1, "2027-01-01", "2028-01-01", "Planejada");
        var firstCompetitionId = Guid.NewGuid();
        var secondCompetitionId = Guid.NewGuid();
        var otherFirstSeasonCompetitionId = Guid.NewGuid();
        await InsertCompetitionAsync(database, actorId, firstSeasonId, firstCompetitionId, "Competição 1", "primeira", false);
        await InsertCompetitionAsync(database, actorId, secondSeasonId, secondCompetitionId, "Competição 2", "segunda", false);
        await InsertCompetitionAsync(database, actorId, firstSeasonId, otherFirstSeasonCompetitionId, "Competição 3", "terceira", false);
        var firstRoundId = Guid.NewGuid();
        var secondRoundId = Guid.NewGuid();
        await InsertRoundAsync(database, firstCompetitionId, firstRoundId, "Rodada 1", 1);
        await InsertRoundAsync(database, secondCompetitionId, secondRoundId, "Rodada 2", 1);
        var firstRulesId = Guid.NewGuid();
        var secondRulesId = Guid.NewGuid();
        var otherCompetitionRulesId = Guid.NewGuid();
        await InsertRulesAsync(database, actorId, firstSeasonId, firstCompetitionId, firstRulesId, 1);
        await InsertRulesAsync(database, actorId, secondSeasonId, secondCompetitionId, secondRulesId, 1);
        await InsertRulesAsync(database, actorId, firstSeasonId, otherFirstSeasonCompetitionId, otherCompetitionRulesId, 1);
        var secondEventId = Guid.NewGuid();
        await InsertEventAsync(database, actorId, secondSeasonId, secondEventId, "Evento 2", "Padrao");
        var foundation = (actorId, firstSeasonId, firstCompetitionId);

        Func<Task> crossSeasonCompetition = () => InsertSeriesAsync(
            database, (actorId, firstSeasonId, secondCompetitionId), secondRoundId, secondRulesId,
            Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(1));
        Func<Task> crossCompetitionRound = () => InsertSeriesAsync(
            database, foundation, secondRoundId, firstRulesId,
            Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(2));
        Func<Task> crossSeasonRules = () => InsertSeriesAsync(
            database, foundation, firstRoundId, secondRulesId,
            Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(3));
        Func<Task> crossCompetitionRules = () => InsertSeriesAsync(
            database, foundation, firstRoundId, otherCompetitionRulesId,
            Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(4));
        Func<Task> crossSeasonEvent = () => InsertSeriesAsync(
            database, foundation, firstRoundId, firstRulesId,
            Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(5), secondEventId);

        await AssertPostgresFailureAsync(crossSeasonCompetition, PostgresErrorCodes.ForeignKeyViolation);
        await AssertPostgresFailureAsync(crossCompetitionRound, PostgresErrorCodes.ForeignKeyViolation);
        await AssertPostgresFailureAsync(crossSeasonRules, PostgresErrorCodes.ForeignKeyViolation);
        await AssertPostgresFailureAsync(crossCompetitionRules, PostgresErrorCodes.ForeignKeyViolation);
        await AssertPostgresFailureAsync(crossSeasonEvent, PostgresErrorCodes.ForeignKeyViolation);
    }

    [Fact]
    public async Task VencedoresDeSerieEPartida_DevemPertencerAPropriaSerie()
    {
        await using var database = await CreateTargetDatabaseAsync();
        var foundation = await SeedSeriesAsync(database);
        var firstSideId = Guid.NewGuid();
        var secondSeriesSideId = Guid.NewGuid();
        await InsertSideAsync(database, foundation.FirstSeriesId, firstSideId, 1, "Primeiro lado");
        await InsertSideAsync(database, foundation.SecondSeriesId, secondSeriesSideId, 1, "Outro lado");
        var matchId = Guid.NewGuid();
        await InsertMatchAsync(database, foundation.FirstSeriesId, matchId, 1);

        Func<Task> seriesWinner = () => ExecuteAsync(
            database,
            "UPDATE series SET lado_vencedor_id = @side_id WHERE id = @series_id",
            new NpgsqlParameter("side_id", secondSeriesSideId),
            new NpgsqlParameter("series_id", foundation.FirstSeriesId));
        Func<Task> matchWinner = () => ExecuteAsync(
            database,
            "UPDATE partidas SET estado = 'Confirmada', lado_vencedor_id = @side_id, motivo_termino = 'Normal', confirmada_em = now() WHERE id = @match_id",
            new NpgsqlParameter("side_id", secondSeriesSideId),
            new NpgsqlParameter("match_id", matchId));

        await AssertPostgresFailureAsync(seriesWinner, PostgresErrorCodes.ForeignKeyViolation);
        await AssertPostgresFailureAsync(matchWinner, PostgresErrorCodes.ForeignKeyViolation);
    }

    [Fact]
    public async Task Pick_DevePertencerAMesmaSerieDaPartidaEDoLado()
    {
        await using var database = await CreateTargetDatabaseAsync();
        var foundation = await SeedSeriesAsync(database);
        var firstSideId = Guid.NewGuid();
        var secondSeriesSideId = Guid.NewGuid();
        await InsertSideAsync(database, foundation.FirstSeriesId, firstSideId, 1, "Primeiro lado");
        await InsertSideAsync(database, foundation.SecondSeriesId, secondSeriesSideId, 1, "Outro lado");
        var matchId = Guid.NewGuid();
        await InsertMatchAsync(database, foundation.FirstSeriesId, matchId, 1);

        Func<Task> action = () => InsertPickAsync(
            database, matchId, foundation.FirstSeriesId, secondSeriesSideId, 1, 11, 1, true);

        await AssertPostgresFailureAsync(action, PostgresErrorCodes.ForeignKeyViolation);
    }

    [Fact]
    public async Task PicksValidos_DevemSerUnicosPorSlotECampeaoEPreservarHistoricoInvalido()
    {
        await using var database = await CreateTargetDatabaseAsync();
        var foundation = await SeedSeriesAsync(database);
        var firstSideId = Guid.NewGuid();
        var secondSideId = Guid.NewGuid();
        await InsertSideAsync(database, foundation.FirstSeriesId, firstSideId, 1, "Azul");
        await InsertSideAsync(database, foundation.FirstSeriesId, secondSideId, 2, "Vermelho");
        var matchId = Guid.NewGuid();
        await InsertMatchAsync(database, foundation.FirstSeriesId, matchId, 1);
        await InsertPickAsync(database, matchId, foundation.FirstSeriesId, firstSideId, 1, 11, 1, true);

        Func<Task> duplicateSlot = () => InsertPickAsync(
            database, matchId, foundation.FirstSeriesId, firstSideId, 1, 22, 2, true);
        Func<Task> duplicateChampion = () => InsertPickAsync(
            database, matchId, foundation.FirstSeriesId, secondSideId, 1, 11, 1, true);

        await AssertPostgresFailureAsync(duplicateSlot, PostgresErrorCodes.UniqueViolation);
        await AssertPostgresFailureAsync(duplicateChampion, PostgresErrorCodes.UniqueViolation);
        await InsertPickAsync(database, matchId, foundation.FirstSeriesId, firstSideId, 1, 11, 2, false);
        await InsertPickAsync(database, matchId, foundation.FirstSeriesId, firstSideId, 1, 11, 3, false);
        (await ScalarAsync<long>(database, "SELECT COUNT(*) FROM picks_partidas")).Should().Be(3);
    }

    [Fact]
    public async Task Season_DeveRecusarAnoForaDoIntervaloSuportado()
    {
        await using var database = await CreateTargetDatabaseAsync();
        var actorId = await SeedActorAsync(database);

        Func<Task> beforeLeague = () => InsertSeasonAsync(
            database, actorId, Guid.NewGuid(), "Antiga", 2008, 1, "2008-01-01", "2009-01-01", "Planejada");
        Func<Task> beyondLimit = () => InsertSeasonAsync(
            database, actorId, Guid.NewGuid(), "Futura", 10000, 1, "9998-01-01", "9999-01-01", "Planejada");

        await AssertPostgresFailureAsync(beforeLeague, PostgresErrorCodes.CheckViolation);
        await AssertPostgresFailureAsync(beyondLimit, PostgresErrorCodes.CheckViolation);
    }

    [Fact]
    public async Task EnumsPersistidos_DevemRecusarValoresDesconhecidosEEventoDeveSerPadrao()
    {
        await using var database = await CreateTargetDatabaseAsync();
        var foundation = await SeedCompetitionAsync(database, false);
        var roundId = Guid.NewGuid();
        await InsertRoundAsync(database, foundation.CompetitionId, roundId, "Rodada", 1);
        var rulesId = Guid.NewGuid();
        await InsertRulesAsync(database, foundation.ActorId, foundation.SeasonId, foundation.CompetitionId, rulesId, 1);
        var seriesId = Guid.NewGuid();
        await InsertSeriesAsync(database, foundation, roundId, rulesId, seriesId, DateTimeOffset.UtcNow.AddDays(1));
        var sideId = Guid.NewGuid();
        await InsertSideAsync(database, seriesId, sideId, 1, "Azul");
        var matchId = Guid.NewGuid();
        await InsertMatchAsync(database, seriesId, matchId, 1);

        var invalidUpdates = new[]
        {
            "UPDATE seasons SET estado = 'Desconhecida' WHERE id = '" + foundation.SeasonId + "'",
            "UPDATE versoes_regras SET formato = 'Md7' WHERE id = '" + rulesId + "'",
            "UPDATE versoes_regras SET modo_draft = 'Aleatorio' WHERE id = '" + rulesId + "'",
            "UPDATE series SET tipo = 'Torneio' WHERE id = '" + seriesId + "'",
            "UPDATE series SET formato = 'Md7' WHERE id = '" + seriesId + "'",
            "UPDATE series SET modo_draft = 'Aleatorio' WHERE id = '" + seriesId + "'",
            "UPDATE series SET estado = 'Desconhecida' WHERE id = '" + seriesId + "'",
            "UPDATE lados_series SET tipo = 'Desconhecido' WHERE id = '" + sideId + "'",
            "UPDATE partidas SET estado = 'Desconhecida' WHERE id = '" + matchId + "'",
            "UPDATE partidas SET motivo_termino = 'Desconhecido' WHERE id = '" + matchId + "'",
            "UPDATE partidas SET decisao_picks_remake = 'Desconhecida' WHERE id = '" + matchId + "'",
        };

        foreach (var sql in invalidUpdates)
        {
            await AssertPostgresFailureAsync(() => ExecuteAsync(database, sql), PostgresErrorCodes.CheckViolation);
        }

        await AssertPostgresFailureAsync(
            () => InsertEventAsync(database, foundation.ActorId, foundation.SeasonId, Guid.NewGuid(), "Evento", "Fearless"),
            PostgresErrorCodes.CheckViolation);
        await AssertPostgresFailureAsync(
            () => InsertAuditAsync(database, foundation.ActorId, "Desconhecido", "TemporadaCriada"),
            PostgresErrorCodes.CheckViolation);
        await AssertPostgresFailureAsync(
            () => InsertAuditAsync(database, foundation.ActorId, "Season", "Desconhecida"),
            PostgresErrorCodes.CheckViolation);
        await AssertPostgresFailureAsync(
            () => InsertIdempotencyAsync(database, foundation.ActorId, "POST", "/series", "invalid-enum", "Desconhecido"),
            PostgresErrorCodes.CheckViolation);
    }

    [Fact]
    public async Task EfCore_DevePropagarSerieDoPickPelaNavegacaoDoAgregadoEPersistirShadowFk()
    {
        await using var database = await CreateTargetDatabaseAsync();
        var foundation = await SeedSeriesAsync(database);
        var firstSideId = Guid.NewGuid();
        var secondSideId = Guid.NewGuid();
        await InsertSideAsync(database, foundation.FirstSeriesId, firstSideId, 1, "Azul");
        await InsertSideAsync(database, foundation.FirstSeriesId, secondSideId, 2, "Vermelho");
        await using var context = database.CreateContext();
        var series = await context.Series
            .Include(item => item.Lados)
            .Include(item => item.Partidas)
            .SingleAsync(item => item.Id == foundation.FirstSeriesId);
        var match = new Partida(series.Id, 1, DateTimeOffset.UtcNow);
        var pick = new PickPartida(match, firstSideId, 266, 1, DateTimeOffset.UtcNow);

        context.Partidas.Add(match);
        context.PicksPartidas.Add(pick);

        series.Partidas.Should().ContainSingle().Which.Should().BeSameAs(match);
        match.Picks.Should().ContainSingle().Which.Should().BeSameAs(pick);
        context.Entry(pick).Property<Guid>("SerieId").CurrentValue.Should().Be(series.Id);

        await context.SaveChangesAsync();

        (await ScalarAsync<Guid>(
            database,
            "SELECT serie_id FROM picks_partidas WHERE id = @id",
            new NpgsqlParameter("id", pick.Id))).Should().Be(series.Id);
    }

    [Fact]
    public async Task EfCore_DeveInserirRegrasESerieSemEscreverEscoposGerados()
    {
        await using var database = await CreateTargetDatabaseAsync();
        var foundation = await SeedCompetitionAsync(database, false);
        var roundId = Guid.NewGuid();
        await InsertRoundAsync(database, foundation.CompetitionId, roundId, "Rodada EF", 1);
        var commands = new CommandCaptureInterceptor();
        await using var context = database.CreateContext(commands);
        var rules = new VersaoRegras(
            foundation.SeasonId,
            foundation.CompetitionId,
            1,
            SerieFormato.Md3,
            ModoDraft.Padrao,
            foundation.ActorId,
            DateTimeOffset.UtcNow);
        context.VersoesRegras.Add(rules);

        await context.SaveChangesAsync();

        context.Entry(rules).Property<Guid>("CompeticaoEscopoId").CurrentValue
            .Should().Be(foundation.CompetitionId);
        AssertGeneratedScopeWasNotWritten(commands.CommandTexts, "versoes_regras");

        var sides = new[]
        {
            new LadoSerie(Guid.NewGuid(), 1, LadoSerieTipo.TimeOficial, Guid.NewGuid(), "Azul", null, null, null, []),
            new LadoSerie(Guid.NewGuid(), 2, LadoSerieTipo.TimeOficial, Guid.NewGuid(), "Vermelho", null, null, null, []),
        };
        var series = new Serie(
            foundation.SeasonId,
            foundation.CompetitionId,
            roundId,
            rules.Id,
            null,
            null,
            SerieTipo.ConfrontoOficial,
            SerieFormato.Md3,
            ModoDraft.Padrao,
            false,
            DateTimeOffset.UtcNow.AddDays(1),
            null,
            foundation.ActorId,
            DateTimeOffset.UtcNow,
            sides);
        context.Series.Add(series);

        await context.SaveChangesAsync();

        context.Entry(series).Property<Guid>("CompeticaoEscopoId").CurrentValue
            .Should().Be(foundation.CompetitionId);
        AssertGeneratedScopeWasNotWritten(commands.CommandTexts, "series");
        (await ScalarAsync<Guid>(
            database,
            "SELECT competicao_escopo_id FROM series WHERE id = @id",
            new NpgsqlParameter("id", series.Id))).Should().Be(foundation.CompetitionId);
    }

    private static void AssertGeneratedScopeWasNotWritten(IEnumerable<string> commands, string table)
    {
        var insert = commands.Should().ContainSingle(command => command.StartsWith($"INSERT INTO {table}", StringComparison.Ordinal))
            .Which;
        insert[..insert.IndexOf("VALUES", StringComparison.Ordinal)]
            .Should().NotContain("competicao_escopo_id");
    }

    private static IReadOnlyDictionary<string, string> BuildExpectedColumnTypes()
    {
        var types = new Dictionary<string, string>(StringComparer.Ordinal);

        Add("uuid",
            "calendarios_competitivos.id", "calendarios_competitivos.season_ativa_id", "calendarios_competitivos.atualizado_por_usuario_id",
            "seasons.id", "seasons.criada_por_usuario_id", "seasons.atualizada_por_usuario_id",
            "competicoes.id", "competicoes.season_id", "competicoes.criada_por_usuario_id", "competicoes.atualizada_por_usuario_id",
            "rodadas.id", "rodadas.competicao_id",
            "versoes_regras.id", "versoes_regras.competicao_escopo_id", "versoes_regras.season_id", "versoes_regras.competicao_id", "versoes_regras.publicada_por_usuario_id",
            "eventos_competitivos.id", "eventos_competitivos.season_id", "eventos_competitivos.criado_por_usuario_id",
            "eventos_competitivos.atualizado_por_usuario_id", "evento_times.id", "evento_times.evento_id", "evento_times.time_id",
            "series.id", "series.competicao_escopo_id", "series.season_id", "series.competicao_id", "series.rodada_id", "series.versao_regras_id",
            "series.evento_id", "series.draft_montagem_id", "series.lado_vencedor_id", "series.criada_por_usuario_id",
            "series.atualizada_por_usuario_id", "lados_series.id", "lados_series.serie_id", "lados_series.origem_id",
            "lados_series.capitao_jogador_id", "participantes_esperados_series.id", "participantes_esperados_series.lado_serie_id",
            "participantes_esperados_series.jogador_id", "partidas.id", "partidas.serie_id", "partidas.lado_vencedor_id",
            "picks_partidas.id", "picks_partidas.partida_id", "picks_partidas.serie_id", "picks_partidas.lado_serie_id",
            "registros_auditoria_competitiva.id", "registros_auditoria_competitiva.recurso_id",
            "registros_auditoria_competitiva.ator_usuario_id", "registros_auditoria_competitiva.correlation_id",
            "operacoes_idempotentes.id", "operacoes_idempotentes.ator_usuario_id", "operacoes_idempotentes.recurso_id");
        Add("bigint", "calendarios_competitivos.versao", "seasons.versao", "competicoes.versao", "rodadas.versao",
            "eventos_competitivos.versao", "series.versao", "partidas.versao");
        Add("integer", "seasons.ano", "seasons.ordem_no_ano", "rodadas.ordem", "versoes_regras.numero",
            "evento_times.ordem", "lados_series.ordem", "participantes_esperados_series.ordem", "partidas.ordem",
            "picks_partidas.champion_id", "picks_partidas.ordem", "picks_partidas.versao_fato", "operacoes_idempotentes.status_code");
        Add("boolean", "competicoes.circuito_diario", "series.fearless_habilitado", "series.revisao_necessaria",
            "partidas.conflito_fearless", "picks_partidas.valido");
        Add("date", "seasons.data_inicio", "seasons.data_fim_exclusiva", "series.data_local");
        Add("timestamp with time zone", "calendarios_competitivos.atualizado_em", "seasons.criada_em", "seasons.atualizada_em",
            "seasons.ativada_em", "seasons.encerrada_em", "competicoes.criada_em", "competicoes.atualizada_em",
            "rodadas.criada_em", "rodadas.atualizada_em", "versoes_regras.publicada_em", "eventos_competitivos.criado_em",
            "eventos_competitivos.atualizado_em", "series.agendada_para", "series.criada_em", "series.atualizada_em",
            "series.concluida_em", "partidas.criada_em", "partidas.atualizada_em", "partidas.confirmada_em",
            "picks_partidas.registrado_em", "registros_auditoria_competitiva.ocorrido_em",
            "operacoes_idempotentes.criada_em", "operacoes_idempotentes.expira_em");
        Add("character varying(120)", "seasons.nome", "competicoes.nome", "eventos_competitivos.nome");
        Add("character varying(100)", "evento_times.nome_snapshot", "lados_series.nome_snapshot",
            "lados_series.capitao_nome_snapshot", "participantes_esperados_series.nome_snapshot");
        Add("character varying(80)", "rodadas.nome", "registros_auditoria_competitiva.recurso_tipo",
            "registros_auditoria_competitiva.acao", "registros_auditoria_competitiva.capacidade",
            "operacoes_idempotentes.recurso_tipo");
        Add("character varying(40)", "competicoes.codigo");
        Add("character varying(30)", "series.tipo", "partidas.decisao_picks_remake");
        Add("character varying(20)", "seasons.estado", "versoes_regras.modo_draft", "eventos_competitivos.modo_draft",
            "series.modo_draft", "series.estado", "lados_series.tipo", "partidas.estado", "partidas.motivo_termino");
        Add("character varying(10)", "versoes_regras.formato", "evento_times.tag_snapshot", "series.formato",
            "lados_series.tag_snapshot", "operacoes_idempotentes.metodo");
        Add("character varying(500)", "registros_auditoria_competitiva.justificativa", "operacoes_idempotentes.rota");
        Add("character varying(200)", "operacoes_idempotentes.chave");
        Add("character varying(128)", "operacoes_idempotentes.request_hash");
        Add("text", "registros_auditoria_competitiva.valor_anterior", "registros_auditoria_competitiva.valor_posterior",
            "operacoes_idempotentes.resposta_minima");

        types.Should().HaveCount(ExpectedColumns.Sum(pair => pair.Value.Length));
        return types;

        void Add(string type, params string[] columns)
        {
            foreach (var column in columns)
            {
                types.Add(column, type);
            }
        }
    }

    private static string[] BuildExpectedNonPrimaryIndexes() =>
    [
        "ak_competicoes_id_season_id:CREATE UNIQUE INDEX ak_competicoes_id_season_id ON public.competicoes USING btree (id, season_id)",
        "ak_eventos_competitivos_id_season_id:CREATE UNIQUE INDEX ak_eventos_competitivos_id_season_id ON public.eventos_competitivos USING btree (id, season_id)",
        "ak_lados_series_id_serie_id:CREATE UNIQUE INDEX ak_lados_series_id_serie_id ON public.lados_series USING btree (id, serie_id)",
        "ak_partidas_id_serie_id:CREATE UNIQUE INDEX ak_partidas_id_serie_id ON public.partidas USING btree (id, serie_id)",
        "ak_rodadas_id_competicao_id:CREATE UNIQUE INDEX ak_rodadas_id_competicao_id ON public.rodadas USING btree (id, competicao_id)",
        "ak_versoes_regras_id_competicao_escopo_id:CREATE UNIQUE INDEX ak_versoes_regras_id_competicao_escopo_id ON public.versoes_regras USING btree (id, competicao_escopo_id)",
        "ak_versoes_regras_id_season_id:CREATE UNIQUE INDEX ak_versoes_regras_id_season_id ON public.versoes_regras USING btree (id, season_id)",
        "ex_seasons_periodo:CREATE INDEX ex_seasons_periodo ON public.seasons USING gist (daterange(data_inicio, data_fim_exclusiva, '[)'::text))",
        "ix_calendarios_competitivos_atualizado_por_usuario_id:CREATE INDEX ix_calendarios_competitivos_atualizado_por_usuario_id ON public.calendarios_competitivos USING btree (atualizado_por_usuario_id)",
        "ix_calendarios_competitivos_season_ativa_id:CREATE INDEX ix_calendarios_competitivos_season_ativa_id ON public.calendarios_competitivos USING btree (season_ativa_id)",
        "ix_competicoes_atualizada_por_usuario_id:CREATE INDEX ix_competicoes_atualizada_por_usuario_id ON public.competicoes USING btree (atualizada_por_usuario_id)",
        "ix_competicoes_criada_por_usuario_id:CREATE INDEX ix_competicoes_criada_por_usuario_id ON public.competicoes USING btree (criada_por_usuario_id)",
        "ix_evento_times_time_id:CREATE INDEX ix_evento_times_time_id ON public.evento_times USING btree (time_id)",
        "ix_eventos_competitivos_atualizado_por_usuario_id:CREATE INDEX ix_eventos_competitivos_atualizado_por_usuario_id ON public.eventos_competitivos USING btree (atualizado_por_usuario_id)",
        "ix_eventos_competitivos_criado_por_usuario_id:CREATE INDEX ix_eventos_competitivos_criado_por_usuario_id ON public.eventos_competitivos USING btree (criado_por_usuario_id)",
        "ix_eventos_competitivos_season_id:CREATE INDEX ix_eventos_competitivos_season_id ON public.eventos_competitivos USING btree (season_id)",
        "ix_lados_series_capitao_jogador_id:CREATE INDEX ix_lados_series_capitao_jogador_id ON public.lados_series USING btree (capitao_jogador_id)",
        "ix_participantes_esperados_series_jogador_id:CREATE INDEX ix_participantes_esperados_series_jogador_id ON public.participantes_esperados_series USING btree (jogador_id)",
        "ix_participantes_esperados_series_lado_serie_id:CREATE INDEX ix_participantes_esperados_series_lado_serie_id ON public.participantes_esperados_series USING btree (lado_serie_id)",
        "ix_partidas_lado_vencedor_id_serie_id:CREATE INDEX ix_partidas_lado_vencedor_id_serie_id ON public.partidas USING btree (lado_vencedor_id, serie_id)",
        "ix_picks_partidas_lado_serie_id_serie_id:CREATE INDEX ix_picks_partidas_lado_serie_id_serie_id ON public.picks_partidas USING btree (lado_serie_id, serie_id)",
        "ix_picks_partidas_partida_id_serie_id:CREATE INDEX ix_picks_partidas_partida_id_serie_id ON public.picks_partidas USING btree (partida_id, serie_id)",
        "ix_registros_auditoria_competitiva_ator_usuario_id:CREATE INDEX ix_registros_auditoria_competitiva_ator_usuario_id ON public.registros_auditoria_competitiva USING btree (ator_usuario_id)",
        "ix_rodadas_competicao_id:CREATE INDEX ix_rodadas_competicao_id ON public.rodadas USING btree (competicao_id)",
        "ix_seasons_atualizada_por_usuario_id:CREATE INDEX ix_seasons_atualizada_por_usuario_id ON public.seasons USING btree (atualizada_por_usuario_id)",
        "ix_seasons_criada_por_usuario_id:CREATE INDEX ix_seasons_criada_por_usuario_id ON public.seasons USING btree (criada_por_usuario_id)",
        "ix_series_atualizada_por_usuario_id:CREATE INDEX ix_series_atualizada_por_usuario_id ON public.series USING btree (atualizada_por_usuario_id)",
        "ix_series_competicao_id_season_id:CREATE INDEX ix_series_competicao_id_season_id ON public.series USING btree (competicao_id, season_id)",
        "ix_series_criada_por_usuario_id:CREATE INDEX ix_series_criada_por_usuario_id ON public.series USING btree (criada_por_usuario_id)",
        "ix_series_draft_montagem_id:CREATE INDEX ix_series_draft_montagem_id ON public.series USING btree (draft_montagem_id)",
        "ix_series_evento_id_season_id:CREATE INDEX ix_series_evento_id_season_id ON public.series USING btree (evento_id, season_id)",
        "ix_series_lado_vencedor_id_id:CREATE INDEX ix_series_lado_vencedor_id_id ON public.series USING btree (lado_vencedor_id, id)",
        "ix_series_rodada_id_competicao_id:CREATE INDEX ix_series_rodada_id_competicao_id ON public.series USING btree (rodada_id, competicao_id)",
        "ix_series_season_id:CREATE INDEX ix_series_season_id ON public.series USING btree (season_id)",
        "ix_series_versao_regras_id_competicao_id:CREATE INDEX ix_series_versao_regras_id_competicao_id ON public.series USING btree (versao_regras_id, competicao_escopo_id)",
        "ix_series_versao_regras_id_season_id:CREATE INDEX ix_series_versao_regras_id_season_id ON public.series USING btree (versao_regras_id, season_id)",
        "ix_versoes_regras_competicao_id_season_id:CREATE INDEX ix_versoes_regras_competicao_id_season_id ON public.versoes_regras USING btree (competicao_id, season_id)",
        "ix_versoes_regras_publicada_por_usuario_id:CREATE INDEX ix_versoes_regras_publicada_por_usuario_id ON public.versoes_regras USING btree (publicada_por_usuario_id)",
        "ix_versoes_regras_season_id:CREATE INDEX ix_versoes_regras_season_id ON public.versoes_regras USING btree (season_id)",
        "ux_calendarios_competitivos_singleton:CREATE UNIQUE INDEX ux_calendarios_competitivos_singleton ON public.calendarios_competitivos USING btree ((true))",
        "ux_competicoes_circuito_diario:CREATE UNIQUE INDEX ux_competicoes_circuito_diario ON public.competicoes USING btree (season_id) WHERE circuito_diario",
        "ux_competicoes_season_id_codigo:CREATE UNIQUE INDEX ux_competicoes_season_id_codigo ON public.competicoes USING btree (season_id, lower((codigo)::text))",
        "ux_evento_times_evento_id_ordem:CREATE UNIQUE INDEX ux_evento_times_evento_id_ordem ON public.evento_times USING btree (evento_id, ordem)",
        "ux_evento_times_evento_id_time_id:CREATE UNIQUE INDEX ux_evento_times_evento_id_time_id ON public.evento_times USING btree (evento_id, time_id)",
        "ux_lados_series_serie_id_ordem:CREATE UNIQUE INDEX ux_lados_series_serie_id_ordem ON public.lados_series USING btree (serie_id, ordem)",
        "ux_operacoes_idempotentes_ator_metodo_rota_chave:CREATE UNIQUE INDEX ux_operacoes_idempotentes_ator_metodo_rota_chave ON public.operacoes_idempotentes USING btree (ator_usuario_id, metodo, rota, chave)",
        "ux_partidas_serie_id_ordem:CREATE UNIQUE INDEX ux_partidas_serie_id_ordem ON public.partidas USING btree (serie_id, ordem)",
        "ux_picks_partidas_champion_valido:CREATE UNIQUE INDEX ux_picks_partidas_champion_valido ON public.picks_partidas USING btree (partida_id, champion_id) WHERE valido",
        "ux_picks_partidas_slot_valido:CREATE UNIQUE INDEX ux_picks_partidas_slot_valido ON public.picks_partidas USING btree (partida_id, lado_serie_id, ordem) WHERE valido",
        "ux_rodadas_competicao_id_ordem:CREATE UNIQUE INDEX ux_rodadas_competicao_id_ordem ON public.rodadas USING btree (competicao_id, ordem)",
        "ux_seasons_ano_ordem_no_ano:CREATE UNIQUE INDEX ux_seasons_ano_ordem_no_ano ON public.seasons USING btree (ano, ordem_no_ano)",
        "ux_seasons_ativa:CREATE UNIQUE INDEX ux_seasons_ativa ON public.seasons USING btree (estado) WHERE ((estado)::text = 'Ativa'::text)",
        "ux_versoes_regras_competicao_numero:CREATE UNIQUE INDEX ux_versoes_regras_competicao_numero ON public.versoes_regras USING btree (competicao_id, numero) WHERE (competicao_id IS NOT NULL)",
        "ux_versoes_regras_season_numero_geral:CREATE UNIQUE INDEX ux_versoes_regras_season_numero_geral ON public.versoes_regras USING btree (season_id, numero) WHERE (competicao_id IS NULL)",
    ];

    private static string[] BuildExpectedNonPrimaryConstraints()
    {
        var constraints = ExpectedForeignKeyDefinitions.Select(foreignKey =>
            $"{foreignKey.Name}:f:FOREIGN KEY ({string.Join(", ", foreignKey.Columns)}) "
            + $"REFERENCES {foreignKey.PrincipalTable}({string.Join(", ", foreignKey.PrincipalColumns)})").ToList();
        constraints.AddRange(
        [
            "ak_competicoes_id_season_id:u:UNIQUE (id, season_id)",
            "ak_eventos_competitivos_id_season_id:u:UNIQUE (id, season_id)",
            "ak_lados_series_id_serie_id:u:UNIQUE (id, serie_id)",
            "ak_partidas_id_serie_id:u:UNIQUE (id, serie_id)",
            "ak_rodadas_id_competicao_id:u:UNIQUE (id, competicao_id)",
            "ux_rodadas_competicao_id_ordem:u:UNIQUE (competicao_id, ordem) DEFERRABLE INITIALLY DEFERRED",
            "ak_versoes_regras_id_competicao_escopo_id:u:UNIQUE (id, competicao_escopo_id)",
            "ak_versoes_regras_id_season_id:u:UNIQUE (id, season_id)",
            "ck_competicoes_id_nao_reservado:c:CHECK (id <> '00000000-0000-0000-0000-000000000000'::uuid)",
            "ck_evento_times_ordem_valida:c:CHECK (ordem >= 1 AND ordem <= 4)",
            "ck_eventos_competitivos_modo_draft_padrao:c:CHECK (modo_draft::text = 'Padrao'::text)",
            "ck_lados_series_ordem_valida:c:CHECK (ordem >= 1 AND ordem <= 2)",
            "ck_lados_series_tipo_valido:c:CHECK (tipo::text = ANY (ARRAY['Temporario'::character varying, 'TimeOficial'::character varying]::text[]))",
            "ck_operacoes_idempotentes_recurso_tipo_valido:c:CHECK (recurso_tipo::text = ANY (ARRAY['CalendarioCompetitivo'::character varying, 'Season'::character varying, 'Competicao'::character varying, 'Rodada'::character varying, 'VersaoRegras'::character varying, 'EventoCompetitivo'::character varying, 'Serie'::character varying, 'Partida'::character varying]::text[]))",
            "ck_participantes_esperados_series_ordem_positiva:c:CHECK (ordem > 0)",
            "ck_partidas_decisao_picks_remake_valida:c:CHECK (decisao_picks_remake IS NULL OR (decisao_picks_remake::text = ANY (ARRAY['PreservarPicks'::character varying, 'DesconsiderarPicks'::character varying]::text[])))",
            "ck_partidas_estado_remake_coerente:c:CHECK (estado::text = 'Remake'::text AND decisao_picks_remake IS NOT NULL OR estado::text <> 'Remake'::text AND decisao_picks_remake IS NULL)",
            "ck_partidas_estado_resultado_coerente:c:CHECK (estado::text = 'Confirmada'::text AND lado_vencedor_id IS NOT NULL AND motivo_termino IS NOT NULL AND confirmada_em IS NOT NULL OR estado::text <> 'Confirmada'::text AND lado_vencedor_id IS NULL AND motivo_termino IS NULL AND confirmada_em IS NULL)",
            "ck_partidas_estado_valido:c:CHECK (estado::text = ANY (ARRAY['Rascunho'::character varying, 'Confirmada'::character varying, 'Remake'::character varying, 'Anulada'::character varying]::text[]))",
            "ck_partidas_motivo_termino_valido:c:CHECK (motivo_termino IS NULL OR (motivo_termino::text = ANY (ARRAY['Normal'::character varying, 'Surrender'::character varying]::text[])))",
            "ck_partidas_ordem_positiva:c:CHECK (ordem > 0)",
            "ck_picks_partidas_champion_id_positivo:c:CHECK (champion_id > 0)",
            "ck_picks_partidas_ordem_valida:c:CHECK (ordem >= 1 AND ordem <= 5)",
            "ck_picks_partidas_versao_fato_positiva:c:CHECK (versao_fato > 0)",
            "ck_registros_auditoria_competitiva_acao_valida:c:CHECK (acao::text = ANY (ARRAY['CalendarioCompetitivoAtualizado'::character varying, 'TemporadaCriada'::character varying, 'TemporadaAtualizada'::character varying, 'TemporadaAtivada'::character varying, 'TemporadaEncerrada'::character varying, 'CompeticaoCriada'::character varying, 'CompeticaoAtualizada'::character varying, 'RodadaCriada'::character varying, 'RodadasReordenadas'::character varying, 'RegrasGeraisSeasonPublicadas'::character varying, 'RegrasCompeticaoPublicadas'::character varying, 'EventoCriado'::character varying, 'EventoAtualizado'::character varying, 'SerieAssociadaAoEvento'::character varying, 'SerieCriada'::character varying, 'SerieIniciada'::character varying, 'PartidaAdicionada'::character varying, 'PicksPartidaRegistrados'::character varying, 'PartidaConfirmada'::character varying, 'PartidaMarcadaComoRemake'::character varying, 'PartidaCorrigida'::character varying, 'PartidaAnulada'::character varying, 'ResultadoSerieConfirmado'::character varying, 'SerieCancelada'::character varying, 'SerieAnulada'::character varying, 'FatoCompetitivoCorrigido'::character varying]::text[]))",
            "ck_registros_auditoria_competitiva_recurso_tipo_valido:c:CHECK (recurso_tipo::text = ANY (ARRAY['CalendarioCompetitivo'::character varying, 'Season'::character varying, 'Competicao'::character varying, 'Rodada'::character varying, 'VersaoRegras'::character varying, 'EventoCompetitivo'::character varying, 'Serie'::character varying, 'Partida'::character varying]::text[]))",
            "ck_rodadas_ordem_positiva:c:CHECK (ordem > 0)",
            "ck_seasons_ano_valido:c:CHECK (ano >= 2009 AND ano <= 9999)",
            "ck_seasons_estado_valido:c:CHECK (estado::text = ANY (ARRAY['Planejada'::character varying, 'Ativa'::character varying, 'Encerrada'::character varying]::text[]))",
            "ck_seasons_intervalo_valido:c:CHECK (data_fim_exclusiva > data_inicio)",
            "ck_seasons_ordem_no_ano_positiva:c:CHECK (ordem_no_ano > 0)",
            "ck_series_estado_valido:c:CHECK (estado::text = ANY (ARRAY['Agendada'::character varying, 'EmAndamento'::character varying, 'Concluida'::character varying, 'Cancelada'::character varying, 'Anulada'::character varying]::text[]))",
            "ck_series_formato_valido:c:CHECK (formato::text = ANY (ARRAY['Md3'::character varying, 'Md5'::character varying]::text[]))",
            "ck_series_modo_draft_valido:c:CHECK (modo_draft::text = ANY (ARRAY['Padrao'::character varying, 'Fearless'::character varying]::text[]))",
            "ck_series_tipo_valido:c:CHECK (tipo::text = ANY (ARRAY['DiariaTemporaria'::character varying, 'ConfrontoOficial'::character varying, 'Amistoso'::character varying]::text[]))",
            "ck_versoes_regras_formato_valido:c:CHECK (formato::text = ANY (ARRAY['Md3'::character varying, 'Md5'::character varying]::text[]))",
            "ck_versoes_regras_modo_draft_valido:c:CHECK (modo_draft::text = ANY (ARRAY['Padrao'::character varying, 'Fearless'::character varying]::text[]))",
            "ck_versoes_regras_numero_positivo:c:CHECK (numero > 0)",
            "ex_seasons_periodo:x:EXCLUDE USING gist (daterange(data_inicio, data_fim_exclusiva, '[)'::text) WITH &&)",
        ]);
        return constraints.Order(StringComparer.Ordinal).ToArray();
    }

    private static string AssertTargetMigrationOrder(DbContext context)
    {
        var migrations = context.Database.GetMigrations().ToArray();
        migrations.Should().Contain(TargetMigration);
        var targetIndex = Array.IndexOf(migrations, TargetMigration);
        targetIndex.Should().BeGreaterThan(0);
        targetIndex.Should().Be(migrations.Length - 1, "the feature migration must immediately follow the previous latest migration");
        var predecessor = migrations.Where(migration => migration != TargetMigration).Last();
        migrations[targetIndex - 1].Should().Be(predecessor);
        return predecessor;
    }

    private static async Task<CompetitivePostgresFixture> CreateTargetDatabaseAsync()
    {
        var database = await CompetitivePostgresFixture.CreateEmptyAsync();
        try
        {
            await using var context = database.CreateContext();
            _ = AssertTargetMigrationOrder(context);
            await context.GetService<IMigrator>().MigrateAsync(TargetMigration);
            return database;
        }
        catch
        {
            await database.DisposeAsync();
            throw;
        }
    }

    private static async Task AssertExactSchemaCatalogAsync(CompetitivePostgresFixture database)
    {
        var columns = await QueryAsync(
            database,
            """
            SELECT table_name || '.' || column_name
            FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = ANY (@tables)
            ORDER BY table_name, ordinal_position
            """,
            new NpgsqlParameter<string[]>("tables", FeatureTables));
        columns.Should().BeEquivalentTo(ExpectedColumns.SelectMany(pair => pair.Value.Select(column => $"{pair.Key}.{column}")));

        var columnTypes = await QueryAsync(
            database,
            """
            SELECT table_record.relname || '.' || attribute_record.attname || ':' || format_type(attribute_record.atttypid, attribute_record.atttypmod)
            FROM pg_attribute attribute_record
            JOIN pg_class table_record ON table_record.oid = attribute_record.attrelid
            JOIN pg_namespace schema_record ON schema_record.oid = table_record.relnamespace
            WHERE schema_record.nspname = 'public'
              AND table_record.relname = ANY (@tables)
              AND attribute_record.attnum > 0
              AND NOT attribute_record.attisdropped
            ORDER BY table_record.relname, attribute_record.attnum
            """,
            new NpgsqlParameter<string[]>("tables", FeatureTables));
        columnTypes.Should().BeEquivalentTo(ExpectedColumnTypes.Select(pair => $"{pair.Key}:{pair.Value}"));

        var requiredColumns = await QueryAsync(
            database,
            """
            SELECT table_name || '.' || column_name
            FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = ANY (@tables) AND is_nullable = 'NO'
            ORDER BY table_name, ordinal_position
            """,
            new NpgsqlParameter<string[]>("tables", FeatureTables));
        requiredColumns.Should().BeEquivalentTo(
            ExpectedRequiredColumns.SelectMany(pair => pair.Value.Select(column => $"{pair.Key}.{column}")));

        var generatedColumns = await QueryAsync(
            database,
            """
            SELECT table_name || '.' || column_name || ':' || generation_expression
            FROM information_schema.columns
            WHERE table_schema = 'public' AND is_generated = 'ALWAYS'
            ORDER BY table_name, ordinal_position
            """);
        generatedColumns.Should().Equal(
            "series.competicao_escopo_id:COALESCE(competicao_id, '00000000-0000-0000-0000-000000000000'::uuid)",
            "versoes_regras.competicao_escopo_id:COALESCE(competicao_id, '00000000-0000-0000-0000-000000000000'::uuid)");

        var primaryKeys = await QueryAsync(
            database,
            """
            SELECT table_record.relname || '.' || attribute_record.attname || ':' || format_type(attribute_record.atttypid, attribute_record.atttypmod)
            FROM pg_constraint constraint_record
            JOIN pg_class table_record ON table_record.oid = constraint_record.conrelid
            JOIN pg_namespace schema_record ON schema_record.oid = table_record.relnamespace
            JOIN unnest(constraint_record.conkey) WITH ORDINALITY key_record(attnum, position) ON TRUE
            JOIN pg_attribute attribute_record ON attribute_record.attrelid = table_record.oid AND attribute_record.attnum = key_record.attnum
            WHERE schema_record.nspname = 'public'
              AND table_record.relname = ANY (@tables)
              AND constraint_record.contype = 'p'
            ORDER BY table_record.relname, key_record.position
            """,
            new NpgsqlParameter<string[]>("tables", FeatureTables));
        primaryKeys.Should().BeEquivalentTo(ExpectedUuidPrimaryKeys);

        var foreignKeys = await ReadForeignKeysAsync(database);
        foreignKeys.Select(foreignKey => foreignKey[..foreignKey.LastIndexOf(':')])
            .Should().BeEquivalentTo(ExpectedForeignKeys);
    }

    private static async Task<IReadOnlyCollection<string>> ReadForeignKeysAsync(CompetitivePostgresFixture database) =>
        await QueryAsync(
            database,
            """
            SELECT child.relname || '.' || child_column.attname || '->' || parent.relname || '.' || parent_column.attname || ':' || constraint_record.confdeltype::text
            FROM pg_constraint constraint_record
            JOIN pg_class child ON child.oid = constraint_record.conrelid
            JOIN pg_class parent ON parent.oid = constraint_record.confrelid
            JOIN pg_namespace schema_record ON schema_record.oid = child.relnamespace
            JOIN unnest(constraint_record.conkey, constraint_record.confkey) WITH ORDINALITY key_record(child_attnum, parent_attnum, position) ON TRUE
            JOIN pg_attribute child_column ON child_column.attrelid = child.oid AND child_column.attnum = key_record.child_attnum
            JOIN pg_attribute parent_column ON parent_column.attrelid = parent.oid AND parent_column.attnum = key_record.parent_attnum
            WHERE schema_record.nspname = 'public'
              AND child.relname = ANY (@tables)
              AND constraint_record.contype = 'f'
            ORDER BY child.relname, constraint_record.conname, key_record.position
            """,
            new NpgsqlParameter<string[]>("tables", FeatureTables));

    private static async Task<IReadOnlyList<string>> CapturePublicSchemaCatalogAsync(CompetitivePostgresFixture database)
    {
        var catalog = await QueryAsync(
            database,
            """
            SELECT entry
            FROM (
                SELECT 'relation|' || table_record.relkind::text || '|' || table_record.relname AS entry
                FROM pg_class table_record
                JOIN pg_namespace schema_record ON schema_record.oid = table_record.relnamespace
                WHERE schema_record.nspname = 'public' AND table_record.relkind IN ('r', 'p', 'S', 'v', 'm')
                UNION ALL
                SELECT 'column|' || table_record.relname || '|' || attribute_record.attnum || '|' || attribute_record.attname
                       || '|' || format_type(attribute_record.atttypid, attribute_record.atttypmod)
                       || '|' || attribute_record.attnotnull
                       || '|' || COALESCE(pg_get_expr(default_record.adbin, default_record.adrelid), '')
                FROM pg_attribute attribute_record
                JOIN pg_class table_record ON table_record.oid = attribute_record.attrelid
                JOIN pg_namespace schema_record ON schema_record.oid = table_record.relnamespace
                LEFT JOIN pg_attrdef default_record
                    ON default_record.adrelid = attribute_record.attrelid AND default_record.adnum = attribute_record.attnum
                WHERE schema_record.nspname = 'public'
                  AND table_record.relkind IN ('r', 'p', 'S', 'v', 'm')
                  AND attribute_record.attnum > 0
                  AND NOT attribute_record.attisdropped
                UNION ALL
                SELECT 'index|' || table_record.relname || '|' || index_record.relname || '|'
                       || pg_get_indexdef(index_record.oid)
                FROM pg_index index_metadata
                JOIN pg_class index_record ON index_record.oid = index_metadata.indexrelid
                JOIN pg_class table_record ON table_record.oid = index_metadata.indrelid
                JOIN pg_namespace schema_record ON schema_record.oid = table_record.relnamespace
                WHERE schema_record.nspname = 'public'
                UNION ALL
                SELECT 'constraint|' || table_record.relname || '|' || constraint_record.conname || '|'
                       || constraint_record.contype::text || '|' || pg_get_constraintdef(constraint_record.oid, true)
                FROM pg_constraint constraint_record
                JOIN pg_class table_record ON table_record.oid = constraint_record.conrelid
                JOIN pg_namespace schema_record ON schema_record.oid = table_record.relnamespace
                WHERE schema_record.nspname = 'public'
                UNION ALL
                SELECT 'trigger|' || table_record.relname || '|' || trigger_record.tgname || '|'
                       || pg_get_triggerdef(trigger_record.oid, true)
                FROM pg_trigger trigger_record
                JOIN pg_class table_record ON table_record.oid = trigger_record.tgrelid
                JOIN pg_namespace schema_record ON schema_record.oid = table_record.relnamespace
                WHERE schema_record.nspname = 'public' AND NOT trigger_record.tgisinternal
                UNION ALL
                SELECT 'function|' || procedure_record.oid::regprocedure::text || '|'
                       || procedure_record.prokind::text || '|' || pg_get_functiondef(procedure_record.oid)
                FROM pg_proc procedure_record
                JOIN pg_namespace schema_record ON schema_record.oid = procedure_record.pronamespace
                WHERE schema_record.nspname = 'public'
            ) catalog
            ORDER BY entry
            """);

        return catalog.Select(NormalizeCatalogDefinition).ToArray();
    }

    private static string NormalizeCatalogDefinition(string definition) =>
        CatalogWhitespaceRegex().Replace(definition, " ").Trim();

    private static string NormalizeConstraintDefinition(string definition) =>
        NormalizeCatalogDefinition(definition)
            .Replace(" ON DELETE RESTRICT", string.Empty, StringComparison.Ordinal)
            .Replace(" ON DELETE NO ACTION", string.Empty, StringComparison.Ordinal);

    private sealed record PredecessorSeed(Guid PlayerId, Guid DraftId, IReadOnlyList<string> Rows);

    private sealed record ExpectedForeignKey(
        string Name,
        string Table,
        string[] Columns,
        string PrincipalTable,
        string[] PrincipalColumns);

    private static async Task<PredecessorSeed> SeedPredecessorDataAsync(CompetitivePostgresFixture database)
    {
        var playerId = Guid.NewGuid();
        await ExecuteAsync(
            database,
            """
            INSERT INTO jogadores
                (id, nome_exibicao, nome_real, discord, riot_id, op_gg_url, deep_lol_url, elo, divisao, status,
                 data_cadastro, data_atualizacao)
            VALUES
                (@player_id, 'Jogador predecessor', 'Nome Completo', 'jogador#2026', 'Jogador#BR1',
                 'https://op.gg/lol/summoners/br/Jogador-BR1', 'https://www.deeplol.gg/summoner/br/Jogador-BR1',
                 'Diamante', 'II', 'Ativo', TIMESTAMPTZ '2026-01-02 03:04:05+00', TIMESTAMPTZ '2026-06-07 08:09:10+00')
            """,
            new NpgsqlParameter("player_id", playerId));
        var draftId = Guid.NewGuid();
        await ExecuteAsync(
            database,
            """
            INSERT INTO draft_montagens
                (id, nome, observacoes, status, tamanho_equipe, quantidade_times, quantidade_reservas,
                 criterio_capitaes, data_cadastro, data_atualizacao, modo, duracao_turno_segundos, versao_estado,
                 horario_encerramento_presenca, discord_guild_id, discord_presence_message_id,
                 presenca_continuada_manualmente)
            VALUES
                (@draft_id, 'Draft predecessor', 'Valores devem sobreviver ao downgrade', 'Configuracao', 5, 2, 2,
                 'Manual', TIMESTAMPTZ '2026-02-03 04:05:06+00', TIMESTAMPTZ '2026-07-08 09:10:11+00',
                 'Manual', 45, 7, TIMESTAMPTZ '2026-07-09 22:30:00+00', 'guild-987654321', 'message-123456789', TRUE)
            """,
            new NpgsqlParameter("draft_id", draftId));
        return new PredecessorSeed(playerId, draftId, await ReadPredecessorRowsAsync(database, playerId, draftId));
    }

    private static async Task AssertPredecessorDataAsync(
        CompetitivePostgresFixture database,
        PredecessorSeed seeded)
    {
        (await ReadPredecessorRowsAsync(database, seeded.PlayerId, seeded.DraftId)).Should().Equal(seeded.Rows);
    }

    private static async Task<IReadOnlyList<string>> ReadPredecessorRowsAsync(
        CompetitivePostgresFixture database,
        Guid playerId,
        Guid draftId) =>
        (await QueryAsync(
            database,
            """
            SELECT 'jogadores:' || row_to_json(player_record)::text
            FROM jogadores player_record
            WHERE id = @player_id
            UNION ALL
            SELECT 'draft_montagens:' || row_to_json(draft_record)::text
            FROM draft_montagens draft_record
            WHERE id = @draft_id
            ORDER BY 1
            """,
            new NpgsqlParameter("player_id", playerId),
            new NpgsqlParameter("draft_id", draftId))).ToArray();

    private static async Task<Guid> SeedActorAsync(CompetitivePostgresFixture database)
    {
        var actorId = Guid.NewGuid();
        await ExecuteAsync(
            database,
            """
            INSERT INTO usuarios
                (id, nome, ativo, data_cadastro, data_atualizacao, email_confirmed, phone_number_confirmed,
                 two_factor_enabled, lockout_enabled, access_failed_count)
            VALUES
                (@id, 'Ator de teste', TRUE, NOW(), NOW(), FALSE, FALSE, FALSE, FALSE, 0)
            """,
            new NpgsqlParameter("id", actorId));
        return actorId;
    }

    private static Task InsertSeasonAsync(
        CompetitivePostgresFixture database,
        Guid actorId,
        Guid seasonId,
        string name,
        int year,
        int order,
        string startsOn,
        string endsOn,
        string state) =>
        ExecuteAsync(
            database,
            """
            INSERT INTO seasons
                (id, nome, ano, ordem_no_ano, data_inicio, data_fim_exclusiva, estado, versao, criada_em,
                 atualizada_em, criada_por_usuario_id, atualizada_por_usuario_id)
            VALUES
                (@id, @name, @year, @order, CAST(@starts_on AS date), CAST(@ends_on AS date), @state, 0, NOW(),
                 NOW(), @actor_id, @actor_id)
            """,
            new NpgsqlParameter("id", seasonId),
            new NpgsqlParameter("name", name),
            new NpgsqlParameter("year", year),
            new NpgsqlParameter("order", order),
            new NpgsqlParameter("starts_on", startsOn),
            new NpgsqlParameter("ends_on", endsOn),
            new NpgsqlParameter("state", state),
            new NpgsqlParameter("actor_id", actorId));

    private static async Task<(Guid ActorId, Guid SeasonId, Guid CompetitionId)> SeedCompetitionAsync(
        CompetitivePostgresFixture database,
        bool dailyCircuit)
    {
        var actorId = await SeedActorAsync(database);
        var seasonId = Guid.NewGuid();
        await InsertSeasonAsync(database, actorId, seasonId, "Season base", 2026, 1, "2026-01-01", "2027-01-01", "Planejada");
        var competitionId = Guid.NewGuid();
        await InsertCompetitionAsync(database, actorId, seasonId, competitionId, "Competição base", "base", dailyCircuit);
        return (actorId, seasonId, competitionId);
    }

    private static Task InsertCompetitionAsync(
        CompetitivePostgresFixture database,
        Guid actorId,
        Guid seasonId,
        Guid competitionId,
        string name,
        string code,
        bool dailyCircuit) =>
        ExecuteAsync(
            database,
            """
            INSERT INTO competicoes
                (id, season_id, nome, codigo, circuito_diario, versao, criada_em, atualizada_em,
                 criada_por_usuario_id, atualizada_por_usuario_id)
            VALUES
                (@id, @season_id, @name, @code, @daily_circuit, 0, NOW(), NOW(), @actor_id, @actor_id)
            """,
            new NpgsqlParameter("id", competitionId),
            new NpgsqlParameter("season_id", seasonId),
            new NpgsqlParameter("name", name),
            new NpgsqlParameter("code", code),
            new NpgsqlParameter("daily_circuit", dailyCircuit),
            new NpgsqlParameter("actor_id", actorId));


    private static Task InsertRoundAsync(
        CompetitivePostgresFixture database,
        Guid competitionId,
        Guid roundId,
        string name,
        int order) =>
        ExecuteAsync(
            database,
            """
            INSERT INTO rodadas (id, competicao_id, nome, ordem, versao, criada_em, atualizada_em)
            VALUES (@id, @competition_id, @name, @order, 0, NOW(), NOW())
            """,
            new NpgsqlParameter("id", roundId),
            new NpgsqlParameter("competition_id", competitionId),
            new NpgsqlParameter("name", name),
            new NpgsqlParameter("order", order));

    private static Task InsertRulesAsync(
        CompetitivePostgresFixture database,
        Guid actorId,
        Guid seasonId,
        Guid? competitionId,
        Guid rulesId,
        int number) =>
        ExecuteAsync(
            database,
            """
            INSERT INTO versoes_regras
                (id, season_id, competicao_id, numero, formato, modo_draft, publicada_em, publicada_por_usuario_id)
            VALUES (@id, @season_id, @competition_id, @number, 'Md3', 'Padrao', NOW(), @actor_id)
            """,
            new NpgsqlParameter("id", rulesId),
            new NpgsqlParameter("season_id", seasonId),
            new NpgsqlParameter("competition_id", (object?)competitionId ?? DBNull.Value),
            new NpgsqlParameter("number", number),
            new NpgsqlParameter("actor_id", actorId));

    private static async Task<(Guid FirstSeriesId, Guid SecondSeriesId)> SeedSeriesAsync(CompetitivePostgresFixture database)
    {
        var foundation = await SeedCompetitionAsync(database, false);
        var roundId = Guid.NewGuid();
        await InsertRoundAsync(database, foundation.CompetitionId, roundId, "Rodada base", 1);
        var rulesId = Guid.NewGuid();
        await InsertRulesAsync(
            database, foundation.ActorId, foundation.SeasonId, foundation.CompetitionId, rulesId, 1);
        var firstSeriesId = Guid.NewGuid();
        var secondSeriesId = Guid.NewGuid();
        await InsertSeriesAsync(database, foundation, roundId, rulesId, firstSeriesId, DateTimeOffset.UtcNow.AddDays(1));
        await InsertSeriesAsync(database, foundation, roundId, rulesId, secondSeriesId, DateTimeOffset.UtcNow.AddDays(2));
        return (firstSeriesId, secondSeriesId);
    }

    private static Task InsertSeriesAsync(
        CompetitivePostgresFixture database,
        (Guid ActorId, Guid SeasonId, Guid CompetitionId) foundation,
        Guid roundId,
        Guid rulesId,
        Guid seriesId,
        DateTimeOffset scheduledFor,
        Guid? eventId = null) =>
        ExecuteAsync(
            database,
            """
            INSERT INTO series
                (id, season_id, competicao_id, rodada_id, versao_regras_id, evento_id, tipo, formato, modo_draft,
                 fearless_habilitado, estado, agendada_para, revisao_necessaria, versao, criada_em, atualizada_em,
                 criada_por_usuario_id, atualizada_por_usuario_id)
            VALUES
                (@id, @season_id, @competition_id, @round_id, @rules_id, @event_id, 'ConfrontoOficial', 'Md3', 'Padrao',
                 FALSE, 'Agendada', @scheduled_for, FALSE, 0, NOW(), NOW(), @actor_id, @actor_id)
            """,
            new NpgsqlParameter("id", seriesId),
            new NpgsqlParameter("season_id", foundation.SeasonId),
            new NpgsqlParameter("competition_id", foundation.CompetitionId),
            new NpgsqlParameter("round_id", roundId),
            new NpgsqlParameter("rules_id", rulesId),
            new NpgsqlParameter("event_id", (object?)eventId ?? DBNull.Value),
            new NpgsqlParameter("scheduled_for", scheduledFor),
            new NpgsqlParameter("actor_id", foundation.ActorId));

    private static Task InsertSideAsync(
        CompetitivePostgresFixture database,
        Guid seriesId,
        Guid sideId,
        int order,
        string name) =>
        ExecuteAsync(
            database,
            """
            INSERT INTO lados_series (id, serie_id, ordem, tipo, origem_id, nome_snapshot)
            VALUES (@id, @series_id, @order, 'TimeOficial', @origin_id, @name)
            """,
            new NpgsqlParameter("id", sideId),
            new NpgsqlParameter("series_id", seriesId),
            new NpgsqlParameter("order", order),
            new NpgsqlParameter("origin_id", Guid.NewGuid()),
            new NpgsqlParameter("name", name));

    private static Task InsertMatchAsync(
        CompetitivePostgresFixture database,
        Guid seriesId,
        Guid matchId,
        int order) =>
        ExecuteAsync(
            database,
            """
            INSERT INTO partidas
                (id, serie_id, ordem, estado, conflito_fearless, versao, criada_em, atualizada_em)
            VALUES (@id, @series_id, @order, 'Rascunho', FALSE, 0, NOW(), NOW())
            """,
            new NpgsqlParameter("id", matchId),
            new NpgsqlParameter("series_id", seriesId),
            new NpgsqlParameter("order", order));

    private static Task InsertPickAsync(
        CompetitivePostgresFixture database,
        Guid matchId,
        Guid seriesId,
        Guid sideId,
        int order,
        int championId,
        int factVersion,
        bool valid) =>
        ExecuteAsync(
            database,
            """
            INSERT INTO picks_partidas
                (id, partida_id, serie_id, lado_serie_id, champion_id, ordem, versao_fato, valido, registrado_em)
            VALUES (@id, @match_id, @series_id, @side_id, @champion_id, @order, @fact_version, @valid, NOW())
            """,
            new NpgsqlParameter("id", Guid.NewGuid()),
            new NpgsqlParameter("match_id", matchId),
            new NpgsqlParameter("series_id", seriesId),
            new NpgsqlParameter("side_id", sideId),
            new NpgsqlParameter("champion_id", championId),
            new NpgsqlParameter("order", order),
            new NpgsqlParameter("fact_version", factVersion),
            new NpgsqlParameter("valid", valid));

    private sealed class CommandCaptureInterceptor : Microsoft.EntityFrameworkCore.Diagnostics.DbCommandInterceptor
    {
        internal List<string> CommandTexts { get; } = [];

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            CommandTexts.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }
    }

    private static Task InsertEventAsync(
        CompetitivePostgresFixture database,
        Guid actorId,
        Guid seasonId,
        Guid eventId,
        string name,
        string draftMode) =>
        ExecuteAsync(
            database,
            """
            INSERT INTO eventos_competitivos
                (id, season_id, nome, modo_draft, versao, criado_em, atualizado_em,
                 criado_por_usuario_id, atualizado_por_usuario_id)
            VALUES (@id, @season_id, @name, @draft_mode, 0, NOW(), NOW(), @actor_id, @actor_id)
            """,
            new NpgsqlParameter("id", eventId),
            new NpgsqlParameter("season_id", seasonId),
            new NpgsqlParameter("name", name),
            new NpgsqlParameter("draft_mode", draftMode),
            new NpgsqlParameter("actor_id", actorId));

    private static Task InsertAuditAsync(
        CompetitivePostgresFixture database,
        Guid actorId,
        string resourceType,
        string action) =>
        ExecuteAsync(
            database,
            """
            INSERT INTO registros_auditoria_competitiva
                (id, recurso_tipo, recurso_id, acao, ator_usuario_id, capacidade, correlation_id, ocorrido_em)
            VALUES (@id, @resource_type, @resource_id, @action, @actor_id, 'CanViewCompetitiveAudit', @correlation_id, NOW())
            """,
            new NpgsqlParameter("id", Guid.NewGuid()),
            new NpgsqlParameter("resource_type", resourceType),
            new NpgsqlParameter("resource_id", Guid.NewGuid()),
            new NpgsqlParameter("action", action),
            new NpgsqlParameter("actor_id", actorId),
            new NpgsqlParameter("correlation_id", Guid.NewGuid()));

    private static Task InsertIdempotencyAsync(
        CompetitivePostgresFixture database,
        Guid actorId,
        string method,
        string route,
        string key,
        string resourceType = "Serie") =>
        ExecuteAsync(
            database,
            """
            INSERT INTO operacoes_idempotentes
                (id, ator_usuario_id, metodo, rota, chave, request_hash, status_code, recurso_tipo,
                 resposta_minima, criada_em, expira_em)
            VALUES
                (@id, @actor_id, @method, @route, @key, @request_hash, 201, @resource_type,
                 '{"id":"created"}', TIMESTAMPTZ '2026-07-28 12:00:00+00', TIMESTAMPTZ '2026-10-26 12:00:00+00')
            """,
            new NpgsqlParameter("id", Guid.NewGuid()),
            new NpgsqlParameter("actor_id", actorId),
            new NpgsqlParameter("method", method),
            new NpgsqlParameter("route", route),
            new NpgsqlParameter("key", key),
            new NpgsqlParameter("resource_type", resourceType),
            new NpgsqlParameter("request_hash", new string('a', 64)));

    private static async Task AssertPostgresFailureAsync(Func<Task> action, string expectedSqlState)
    {
        var assertion = await action.Should().ThrowAsync<PostgresException>();
        assertion.Which.SqlState.Should().Be(expectedSqlState);
    }

    private static async Task<IReadOnlyCollection<string>> ReadAppliedMigrationsAsync(
        CompetitivePostgresFixture database) =>
        await QueryAsync(database, "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\"");

    private static async Task ExecuteAsync(
        CompetitivePostgresFixture database,
        string sql,
        params NpgsqlParameter[] parameters)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<T> ScalarAsync<T>(
        CompetitivePostgresFixture database,
        string sql,
        params NpgsqlParameter[] parameters)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        return (T)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<IReadOnlyCollection<string>> QueryAsync(
        CompetitivePostgresFixture database,
        string sql,
        params NpgsqlParameter[] parameters)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        await using var reader = await command.ExecuteReaderAsync();
        var results = new List<string>();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        return results;
    }

    [GeneratedRegex("^[a-z][a-z0-9]*(?:_[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex SnakeCaseRegex();

    [GeneratedRegex("\\s+", RegexOptions.CultureInvariant)]
    private static partial Regex CatalogWhitespaceRegex();
}
