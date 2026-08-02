using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Repositories;
using RinhaDasLendas.Infrastructure.Persistence;
using RinhaDasLendas.Infrastructure.Repositories;
using RinhaDasLendas.Infrastructure.Services;
using RinhaDasLendas.Tests.Fixtures;

namespace RinhaDasLendas.Tests.Integration;

public sealed class CompetitionPostgresConstraintTests
{
    [Fact]
    public async Task RoundOrderConstraint_ShouldAllowAtomicSwapAndRemainDeferredUntilCommit()
    {
        await using var database = await CompetitivePostgresFixture.CreateAsync();
        var foundation = await SeedCompetitionAsync(database);
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        await InsertRoundAsync(database, foundation.CompetitionId, firstId, 1);
        await InsertRoundAsync(database, foundation.CompetitionId, secondId, 2);
        await using var connection = await database.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        await ExecuteAsync(connection, transaction, "UPDATE rodadas SET ordem = 2 WHERE id = @id", firstId);
        await ExecuteAsync(connection, transaction, "UPDATE rodadas SET ordem = 1 WHERE id = @id", secondId);
        await transaction.CommitAsync();

        await using var metadata = connection.CreateCommand();
        metadata.CommandText =
            """
            SELECT condeferrable, condeferred
            FROM pg_constraint
            WHERE conname = 'ux_rodadas_competicao_id_ordem'
            """;
        await using var reader = await metadata.ExecuteReaderAsync();
        (await reader.ReadAsync()).Should().BeTrue();
        reader.GetBoolean(0).Should().BeTrue();
        reader.GetBoolean(1).Should().BeTrue();
    }

    [Fact]
    public async Task CompetitionCodeIndex_ShouldRejectConcurrentCaseInsensitiveDuplicate()
    {
        await using var database = await CompetitivePostgresFixture.CreateAsync();
        var foundation = await SeedCompetitionAsync(database);
        await using var firstConnection = await database.OpenConnectionAsync();
        await using var secondConnection = await database.OpenConnectionAsync();
        await using var firstTransaction = await firstConnection.BeginTransactionAsync();
        await using var secondTransaction = await secondConnection.BeginTransactionAsync();
        await InsertCompetitionAsync(
            firstConnection, firstTransaction, foundation, Guid.NewGuid(), "Concorrente 1", "race");

        var secondInsert = InsertCompetitionAsync(
            secondConnection, secondTransaction, foundation, Guid.NewGuid(), "Concorrente 2", "RACE");
        await firstTransaction.CommitAsync();

        Func<Task> secondAction = async () => await secondInsert;
        var exception = (await secondAction.Should().ThrowAsync<PostgresException>()).Which;
        exception.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
        exception.ConstraintName.Should().Be("ux_competicoes_season_id_codigo");

        await using var indexCommand = firstConnection.CreateCommand();
        indexCommand.CommandText =
            "SELECT pg_get_indexdef(indexrelid) FROM pg_index WHERE indexrelid = 'ux_competicoes_season_id_codigo'::regclass";
        (await indexCommand.ExecuteScalarAsync()).Should().BeOfType<string>().Which
            .Should().Contain("(season_id, lower((codigo)::text))");
    }

    [Fact]
    public async Task GeneralSeasonRulesAuditAction_ShouldBeAcceptedByMigratedConstraint()
    {
        await using var database = await CompetitivePostgresFixture.CreateAsync();
        var foundation = await SeedCompetitionAsync(database);
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO registros_auditoria_competitiva
                (id, recurso_tipo, recurso_id, acao, ator_usuario_id, capacidade, correlation_id, ocorrido_em)
            VALUES
                (@id, 'VersaoRegras', @resource_id, 'RegrasGeraisSeasonPublicadas', @actor_id,
                 'CanManageCompetitions', @correlation_id, NOW())
            """;
        command.Parameters.AddWithValue("id", Guid.NewGuid());
        command.Parameters.AddWithValue("resource_id", Guid.NewGuid());
        command.Parameters.AddWithValue("actor_id", foundation.ActorId);
        command.Parameters.AddWithValue("correlation_id", Guid.NewGuid());

        (await command.ExecuteNonQueryAsync()).Should().Be(1);
    }

    [Fact]
    public async Task IdempotencyCommit_ShouldTranslateDeferredRoundOrderConflictAfterSuccessfulEfReorderFlush()
    {
        await using var database = await CompetitivePostgresFixture.CreateAsync();
        var foundation = await SeedCompetitionAsync(database);
        await InsertRoundAsync(database, foundation.CompetitionId, Guid.NewGuid(), 1);
        await InsertRoundAsync(database, foundation.CompetitionId, Guid.NewGuid(), 2);
        var saveObserver = new SuccessfulSaveObserver();
        await using var context = database.CreateContext(saveObserver);
        var unitOfWork = new CompetitiveUnitOfWork(context);
        var service = new IdempotencyService(
            context,
            new IdempotencyRepository(context),
            unitOfWork,
            TimeProvider.System);
        var request = new IdempotencyRequest(
            foundation.ActorId,
            "POST",
            $"/api/v1/competicoes/{foundation.CompetitionId}/ordenacoes-rodadas",
            "deferred-round-conflict",
            new string('a', 128));

        var action = () => service.ExecuteAsync(request, async cancellationToken =>
        {
            var competition = await context.Competicoes
                .Include(item => item.Rodadas)
                .SingleAsync(item => item.Id == foundation.CompetitionId, cancellationToken);
            var rounds = competition.Rodadas.OrderBy(item => item.Ordem).ToArray();
            competition.ReordenarRodadas(
                [rounds[1].Id, rounds[0].Id],
                competition.Versao,
                foundation.ActorId,
                DateTimeOffset.UtcNow);
            context.Entry(rounds[0]).Property(item => item.Ordem).CurrentValue = 1;
            await ((ICompetitiveUnitOfWork)unitOfWork).SaveChangesAsync(cancellationToken);
            return new IdempotencyResult(
                200,
                RecursoCompetitivoTipo.Competicao,
                competition.Id,
                [],
                new Dictionary<MetadadoResultadoOperacao, object?>());
        }, CancellationToken.None);

        var exception = (await action.Should().ThrowAsync<DomainException>()).Which;
        exception.MessageCode.Should().Be(MessageCodes.RoundOrderConflict);
        saveObserver.SuccessfulSaves.Should().Be(1, "EF must flush before the deferred constraint fails at commit");
        await using var verification = database.CreateContext();
        (await verification.Rodadas
            .Where(item => item.CompeticaoId == foundation.CompetitionId)
            .OrderBy(item => item.Ordem)
            .Select(item => item.Ordem)
            .ToArrayAsync()).Should().Equal(1, 2);
        (await verification.OperacoesIdempotentes.CountAsync()).Should().Be(0);
    }

    private static async Task<(Guid ActorId, Guid SeasonId, Guid CompetitionId)> SeedCompetitionAsync(
        CompetitivePostgresFixture database)
    {
        var actorId = Guid.NewGuid();
        var seasonId = Guid.NewGuid();
        var competitionId = Guid.NewGuid();
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO usuarios
                (id, nome, ativo, data_cadastro, data_atualizacao, email_confirmed, phone_number_confirmed,
                 two_factor_enabled, lockout_enabled, access_failed_count)
            VALUES (@actor_id, 'Ator', TRUE, NOW(), NOW(), FALSE, FALSE, FALSE, FALSE, 0);

            INSERT INTO seasons
                (id, nome, ano, ordem_no_ano, data_inicio, data_fim_exclusiva, estado, versao, criada_em,
                 atualizada_em, criada_por_usuario_id, atualizada_por_usuario_id)
            VALUES
                (@season_id, 'Season', 2026, 1, DATE '2026-01-01', DATE '2027-01-01', 'Planejada', 0,
                 NOW(), NOW(), @actor_id, @actor_id);

            INSERT INTO competicoes
                (id, season_id, nome, codigo, circuito_diario, versao, criada_em, atualizada_em,
                 criada_por_usuario_id, atualizada_por_usuario_id)
            VALUES
                (@competition_id, @season_id, 'Competição', 'BASE', FALSE, 0, NOW(), NOW(),
                 @actor_id, @actor_id);
            """;
        command.Parameters.AddWithValue("actor_id", actorId);
        command.Parameters.AddWithValue("season_id", seasonId);
        command.Parameters.AddWithValue("competition_id", competitionId);
        await command.ExecuteNonQueryAsync();
        return (actorId, seasonId, competitionId);
    }

    private static async Task InsertCompetitionAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        (Guid ActorId, Guid SeasonId, Guid CompetitionId) foundation,
        Guid competitionId,
        string name,
        string code)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT INTO competicoes
                (id, season_id, nome, codigo, circuito_diario, versao, criada_em, atualizada_em,
                 criada_por_usuario_id, atualizada_por_usuario_id)
            VALUES (@id, @season_id, @name, @code, FALSE, 0, NOW(), NOW(), @actor_id, @actor_id)
            """;
        command.Parameters.AddWithValue("id", competitionId);
        command.Parameters.AddWithValue("season_id", foundation.SeasonId);
        command.Parameters.AddWithValue("name", name);
        command.Parameters.AddWithValue("code", code);
        command.Parameters.AddWithValue("actor_id", foundation.ActorId);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task InsertRoundAsync(
        CompetitivePostgresFixture database,
        Guid competitionId,
        Guid roundId,
        int order)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO rodadas (id, competicao_id, nome, ordem, versao, criada_em, atualizada_em)
            VALUES (@id, @competition_id, 'Rodada', @order, 0, NOW(), NOW())
            """;
        command.Parameters.AddWithValue("id", roundId);
        command.Parameters.AddWithValue("competition_id", competitionId);
        command.Parameters.AddWithValue("order", order);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task ExecuteAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        Guid id)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.Parameters.AddWithValue("id", id);
        await command.ExecuteNonQueryAsync();
    }

    private sealed class SuccessfulSaveObserver : SaveChangesInterceptor
    {
        public int SuccessfulSaves { get; private set; }

        public override ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            SuccessfulSaves++;
            return ValueTask.FromResult(result);
        }
    }
}
