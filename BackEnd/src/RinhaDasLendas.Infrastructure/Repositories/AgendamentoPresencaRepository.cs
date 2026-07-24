using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Models;
using RinhaDasLendas.Domain.Repositories;
using RinhaDasLendas.Infrastructure.Persistence;

namespace RinhaDasLendas.Infrastructure.Repositories;

public sealed class AgendamentoPresencaRepository(RinhaDasLendasDbContext dbContext)
    : IAgendamentoPresencaRepository
{
    private const string NextExecutionExpression = """
        CASE WHEN schedule.status = 0 THEN (
            SELECT MIN((
                schedule.ultima_data_avaliada
                + (1 + MOD(
                    day.dia_semana::integer
                    - EXTRACT(ISODOW FROM schedule.ultima_data_avaliada + 1)::integer
                    + 7,
                    7))::integer
                + schedule.horario_publicacao_local) AT TIME ZONE 'America/Sao_Paulo')
            FROM agendamentos_presenca_dias_semana AS day
            WHERE day.agendamento_presenca_id = schedule.id
        ) END
        """;

    public async Task AddAsync(AgendamentoPresenca agenda, CancellationToken ct)
    {
        await dbContext.AgendamentosPresenca.AddAsync(agenda, ct);
    }

    public Task<AgendamentoPresenca?> GetByIdAsync(Guid id, bool tracking, CancellationToken ct)
    {
        var query = IncludeDays(dbContext.AgendamentosPresenca);
        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(schedule => schedule.Id == id, ct);
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct)
    {
        return dbContext.AgendamentosPresenca.AsNoTracking()
            .AnyAsync(schedule => schedule.Id == id && schedule.Status != AgendamentoPresencaStatus.Arquivado, ct);
    }

    public async Task<AgendamentoPresencaListItem?> GetSummaryAsync(Guid id, CancellationToken ct)
    {
        var rows = await ReadSummaryRowsAsync(
            "WHERE schedule.id = @id AND schedule.status <> 2 ORDER BY \"NextExecution\" ASC NULLS LAST, schedule.nome ASC, schedule.id ASC",
            [new NpgsqlParameter("id", id)],
            ct);
        var row = rows.SingleOrDefault();
        if (row is null)
        {
            return null;
        }

        var agenda = await IncludeDays(dbContext.AgendamentosPresenca.AsNoTracking())
            .SingleAsync(schedule => schedule.Id == row.Id, ct);
        return new AgendamentoPresencaListItem(agenda, row.NextExecution);
    }

    public async Task<IReadOnlyCollection<AgendamentoPresencaListItem>> ListAsync(
        bool includePaused,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var rows = await ReadSummaryRowsAsync(
            """
            WHERE schedule.status = 0 OR (@includePaused AND schedule.status = 1)
            ORDER BY "NextExecution" ASC NULLS LAST, schedule.nome ASC, schedule.id ASC
            OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY
            """,
            [
                new NpgsqlParameter("includePaused", includePaused),
                new NpgsqlParameter("offset", (page - 1) * pageSize),
                new NpgsqlParameter("pageSize", pageSize),
            ],
            ct);

        if (rows.Count == 0)
        {
            return [];
        }

        var ids = rows.Select(row => row.Id).ToArray();
        var schedules = await IncludeDays(dbContext.AgendamentosPresenca.AsNoTracking())
            .Where(schedule => ids.Contains(schedule.Id))
            .ToListAsync(ct);
        var schedulesById = schedules.ToDictionary(schedule => schedule.Id);
        return rows.Select(row => new AgendamentoPresencaListItem(schedulesById[row.Id], row.NextExecution)).ToArray();
    }

    public Task<int> CountAsync(bool includePaused, CancellationToken ct)
    {
        return dbContext.AgendamentosPresenca.AsNoTracking()
            .CountAsync(schedule => schedule.Status == AgendamentoPresencaStatus.Ativo
                || (includePaused && schedule.Status == AgendamentoPresencaStatus.Pausado), ct);
    }

    public Task<OcorrenciaAgendamentoPresenca?> GetLatestOccurrenceAsync(Guid agendaId, CancellationToken ct)
    {
        return dbContext.OcorrenciasAgendamentosPresenca.AsNoTracking()
            .Where(occurrence => occurrence.AgendamentoPresencaId == agendaId)
            .OrderByDescending(occurrence => occurrence.DataLocal)
            .ThenBy(occurrence => occurrence.Id)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyDictionary<Guid, OcorrenciaAgendamentoPresenca>> ListLatestOccurrencesAsync(
        IReadOnlyCollection<Guid> agendaIds,
        CancellationToken ct)
    {
        if (agendaIds.Count == 0)
        {
            return new Dictionary<Guid, OcorrenciaAgendamentoPresenca>();
        }

        var occurrences = await dbContext.OcorrenciasAgendamentosPresenca.AsNoTracking()
            .Where(occurrence => agendaIds.Contains(occurrence.AgendamentoPresencaId))
            .GroupBy(occurrence => occurrence.AgendamentoPresencaId)
            .Select(group => group
                .OrderByDescending(occurrence => occurrence.DataLocal)
                .ThenBy(occurrence => occurrence.Id)
                .First())
            .ToListAsync(ct);
        return occurrences.ToDictionary(occurrence => occurrence.AgendamentoPresencaId);
    }

    public async Task<IReadOnlyCollection<OcorrenciaAgendamentoPresenca>> ListOccurrencesAsync(
        Guid agendaId,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        return await dbContext.OcorrenciasAgendamentosPresenca.AsNoTracking()
            .Where(occurrence => occurrence.AgendamentoPresencaId == agendaId)
            .OrderByDescending(occurrence => occurrence.DataLocal)
            .ThenBy(occurrence => occurrence.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public Task<int> CountOccurrencesAsync(Guid agendaId, CancellationToken ct)
    {
        return dbContext.OcorrenciasAgendamentosPresenca.AsNoTracking()
            .CountAsync(occurrence => occurrence.AgendamentoPresencaId == agendaId, ct);
    }

    public async Task<IReadOnlyCollection<AgendamentoPresenca>> ListCandidatesAsync(
        DateOnly throughLocalDate,
        CancellationToken ct)
    {
        return await IncludeDays(dbContext.AgendamentosPresenca)
            .Where(schedule => schedule.Status == AgendamentoPresencaStatus.Ativo
                && schedule.UltimaDataAvaliada < throughLocalDate)
            .OrderBy(schedule => schedule.UltimaDataAvaliada)
            .ThenBy(schedule => schedule.Id)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyCollection<OcorrenciaAgendamentoPresenca>> ListBlockedAsync(
        DateTimeOffset now,
        CancellationToken ct)
    {
        return await dbContext.OcorrenciasAgendamentosPresenca.AsNoTracking()
            .Where(occurrence => occurrence.Status == OcorrenciaAgendamentoPresencaStatus.Bloqueada
                && occurrence.PublicacaoPrevistaEm <= now)
            .OrderBy(occurrence => occurrence.EncerramentoPrevistoEm)
            .ThenBy(occurrence => occurrence.Id)
            .ToListAsync(ct);
    }

    public async Task<AgendamentoPresencaOcorrenciaClaim?> TryClaimOccurrenceAsync(
        Guid agendaId,
        DateOnly localDate,
        DateTimeOffset publicationAt,
        DateTimeOffset closureAt,
        Guid claimId,
        DateTimeOffset claimExpiresAt,
        DateTimeOffset now,
        CancellationToken ct)
    {
        OcorrenciaAgendamentoPresenca.ValidarClaimProcessamento(claimId, claimExpiresAt, now);
        await OpenConnectionAsync(ct);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var dbTransaction = transaction.GetDbTransaction();

        await using (var lockCommand = CreateCommand(dbTransaction, "SELECT pg_advisory_xact_lock(hashtextextended(@resource, 0))"))
        {
            AddParameter(lockCommand, "resource", $"presence-schedule:{agendaId:N}:{localDate:yyyy-MM-dd}");
            await lockCommand.ExecuteNonQueryAsync(ct);
        }

        AgendamentoPresencaOcorrenciaClaim? result = null;
        const string sql = """
            WITH acquired AS (
                INSERT INTO ocorrencias_agendamentos_presenca
                    (id, agendamento_presenca_id, data_local, publicacao_prevista_em,
                     encerramento_previsto_em, status, claim_id, claim_expires_at,
                     ultima_tentativa_em, criada_em, atualizada_em)
                SELECT @occurrenceId, schedule.id, @localDate, @publicationAt,
                       @closureAt, 0, @claimId, @claimExpiresAt, @now, @now, @now
                FROM agendamentos_presenca AS schedule
                WHERE schedule.id = @agendaId
                  AND (
                      EXISTS (
                          SELECT 1
                          FROM ocorrencias_agendamentos_presenca AS existing
                          WHERE existing.agendamento_presenca_id = @agendaId
                            AND existing.data_local = @localDate)
                      OR (
                          schedule.status = 0
                          AND EXISTS (
                              SELECT 1
                              FROM agendamentos_presenca_dias_semana AS configured_day
                              WHERE configured_day.agendamento_presenca_id = schedule.id
                                AND configured_day.dia_semana = EXTRACT(ISODOW FROM CAST(@localDate AS date))::smallint)
                          AND @publicationAt = (CAST(@localDate AS date) + schedule.horario_publicacao_local) AT TIME ZONE 'America/Sao_Paulo'
                          AND @closureAt = (CAST(@localDate AS date) + schedule.horario_encerramento_local) AT TIME ZONE 'America/Sao_Paulo'
                          AND @now >= @publicationAt
                          AND @now < @closureAt))
                ON CONFLICT (agendamento_presenca_id, data_local) DO UPDATE
                SET status = 0,
                    codigo_falha = NULL,
                    claim_id = EXCLUDED.claim_id,
                    claim_expires_at = EXCLUDED.claim_expires_at,
                    ultima_tentativa_em = EXCLUDED.ultima_tentativa_em,
                    atualizada_em = EXCLUDED.atualizada_em
                WHERE (ocorrencias_agendamentos_presenca.status = 1
                       OR (ocorrencias_agendamentos_presenca.status = 0
                           AND ocorrencias_agendamentos_presenca.claim_expires_at <= @now))
                  AND ocorrencias_agendamentos_presenca.publicacao_prevista_em <= @now
                  AND ocorrencias_agendamentos_presenca.encerramento_previsto_em > @now
                RETURNING id, claim_id
            )
            SELECT id, claim_id, TRUE FROM acquired
            UNION ALL
            SELECT current.id, current.claim_id, FALSE
            FROM ocorrencias_agendamentos_presenca AS current
            WHERE current.agendamento_presenca_id = @agendaId
              AND current.data_local = @localDate
              AND NOT EXISTS (SELECT 1 FROM acquired)
            LIMIT 1
            """;
        await using (var command = CreateCommand(dbTransaction, sql))
        {
            AddParameter(command, "occurrenceId", Guid.NewGuid());
            AddParameter(command, "agendaId", agendaId);
            AddParameter(command, "localDate", localDate);
            AddParameter(command, "publicationAt", publicationAt);
            AddParameter(command, "closureAt", closureAt);
            AddParameter(command, "claimId", claimId);
            AddParameter(command, "claimExpiresAt", claimExpiresAt);
            AddParameter(command, "now", now);
            await using var reader = await command.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct))
            {
                result = new AgendamentoPresencaOcorrenciaClaim(
                    reader.GetGuid(0),
                    reader.IsDBNull(1) ? Guid.Empty : reader.GetGuid(1),
                    reader.GetBoolean(2));
            }
        }

        await transaction.CommitAsync(ct);
        return result;
    }

    public Task<bool> TryUpsertBlockedOccurrenceAsync(
        Guid agendaId,
        DateOnly localDate,
        DateTimeOffset publicationAt,
        DateTimeOffset closureAt,
        string code,
        DateTimeOffset now,
        CancellationToken ct)
    {
        code = OcorrenciaAgendamentoPresenca.NormalizarCodigoPublico(
            code,
            MessageCodes.PresenceScheduleDiscordUnavailable);
        const string sql = """
            INSERT INTO ocorrencias_agendamentos_presenca
                (id, agendamento_presenca_id, data_local, publicacao_prevista_em,
                 encerramento_previsto_em, status, codigo_falha, ultima_tentativa_em,
                 criada_em, atualizada_em)
            SELECT @occurrenceId, schedule.id, @localDate, @publicationAt, @closureAt,
                   1, @code, @now, @now, @now
            FROM agendamentos_presenca AS schedule
            WHERE schedule.id = @agendaId
              AND (
                  EXISTS (
                      SELECT 1
                      FROM ocorrencias_agendamentos_presenca AS existing
                      WHERE existing.agendamento_presenca_id = @agendaId
                        AND existing.data_local = @localDate)
                  OR (
                      schedule.status = 0
                      AND EXISTS (
                          SELECT 1
                          FROM agendamentos_presenca_dias_semana AS configured_day
                          WHERE configured_day.agendamento_presenca_id = schedule.id
                            AND configured_day.dia_semana = EXTRACT(ISODOW FROM CAST(@localDate AS date))::smallint)
                      AND @publicationAt = (CAST(@localDate AS date) + schedule.horario_publicacao_local) AT TIME ZONE 'America/Sao_Paulo'
                      AND @closureAt = (CAST(@localDate AS date) + schedule.horario_encerramento_local) AT TIME ZONE 'America/Sao_Paulo'
                      AND @now < @closureAt))
            ON CONFLICT (agendamento_presenca_id, data_local) DO UPDATE
            SET codigo_falha = EXCLUDED.codigo_falha,
                ultima_tentativa_em = EXCLUDED.ultima_tentativa_em,
                atualizada_em = EXCLUDED.atualizada_em
            WHERE ocorrencias_agendamentos_presenca.status = 1
            RETURNING TRUE
            """;
        return ExecuteBooleanAsync(sql, agendaId, localDate, publicationAt, closureAt, code, now, ct);
    }

    public Task<bool> TryUpsertMissedOccurrenceAsync(
        Guid agendaId,
        DateOnly localDate,
        DateTimeOffset publicationAt,
        DateTimeOffset closureAt,
        string code,
        DateTimeOffset now,
        CancellationToken ct)
    {
        code = OcorrenciaAgendamentoPresenca.NormalizarCodigoPublico(
            code,
            MessageCodes.PresenceScheduleWindowExpired);
        const string sql = """
            INSERT INTO ocorrencias_agendamentos_presenca
                (id, agendamento_presenca_id, data_local, publicacao_prevista_em,
                 encerramento_previsto_em, status, codigo_falha, ultima_tentativa_em,
                 criada_em, atualizada_em)
            SELECT @occurrenceId, schedule.id, @localDate, @publicationAt, @closureAt,
                   3, @code, @now, @now, @now
            FROM agendamentos_presenca AS schedule
            WHERE schedule.id = @agendaId
              AND (
                  EXISTS (
                      SELECT 1
                      FROM ocorrencias_agendamentos_presenca AS existing
                      WHERE existing.agendamento_presenca_id = @agendaId
                        AND existing.data_local = @localDate)
                  OR (
                      schedule.status = 0
                      AND EXISTS (
                          SELECT 1
                          FROM agendamentos_presenca_dias_semana AS configured_day
                          WHERE configured_day.agendamento_presenca_id = schedule.id
                            AND configured_day.dia_semana = EXTRACT(ISODOW FROM CAST(@localDate AS date))::smallint)
                      AND @publicationAt = (CAST(@localDate AS date) + schedule.horario_publicacao_local) AT TIME ZONE 'America/Sao_Paulo'
                      AND @closureAt = (CAST(@localDate AS date) + schedule.horario_encerramento_local) AT TIME ZONE 'America/Sao_Paulo'
                      AND @now >= @closureAt))
            ON CONFLICT (agendamento_presenca_id, data_local) DO UPDATE
            SET status = 3,
                codigo_falha = EXCLUDED.codigo_falha,
                claim_id = NULL,
                claim_expires_at = NULL,
                ultima_tentativa_em = EXCLUDED.ultima_tentativa_em,
                atualizada_em = EXCLUDED.atualizada_em
            WHERE (ocorrencias_agendamentos_presenca.status = 1
                   OR (ocorrencias_agendamentos_presenca.status = 0
                       AND ocorrencias_agendamentos_presenca.claim_expires_at <= @now))
              AND ocorrencias_agendamentos_presenca.encerramento_previsto_em <= @now
            RETURNING TRUE
            """;
        return ExecuteBooleanAsync(sql, agendaId, localDate, publicationAt, closureAt, code, now, ct);
    }

    public async Task<bool> TryCompleteWithDraftAsync(
        Guid occurrenceId,
        Guid claimId,
        DraftMontagem draft,
        DateTimeOffset now,
        CancellationToken ct)
    {
        await OpenConnectionAsync(ct);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            Guid? lockedOccurrenceId;
            await using (var lockCommand = CreateCommand(transaction.GetDbTransaction(), """
                SELECT id
                FROM ocorrencias_agendamentos_presenca
                WHERE id = @occurrenceId
                  AND status = 0
                  AND claim_id = @claimId
                  AND claim_expires_at > @now
                  AND encerramento_previsto_em > @now
                FOR UPDATE
                """))
            {
                AddParameter(lockCommand, "occurrenceId", occurrenceId);
                AddParameter(lockCommand, "claimId", claimId);
                AddParameter(lockCommand, "now", now);
                lockedOccurrenceId = await lockCommand.ExecuteScalarAsync(ct) as Guid?;
            }

            if (lockedOccurrenceId is null)
            {
                await transaction.RollbackAsync(ct);
                return false;
            }

            draft.ConfigurarPublicacaoDiscordPendente(
                DraftMontagemPublicacaoDiscordTipo.Presenca,
                draft.DiscordGuildId,
                null,
                now);
            dbContext.DraftMontagens.Add(draft);
            await dbContext.SaveChangesAsync(ct);

            bool updated;
            await using (var updateCommand = CreateCommand(transaction.GetDbTransaction(), """
                UPDATE ocorrencias_agendamentos_presenca
                SET status = 2,
                    draft_montagem_id = @draftId,
                    codigo_falha = NULL,
                    claim_id = NULL,
                    claim_expires_at = NULL,
                    ultima_tentativa_em = @now,
                    atualizada_em = @now
                WHERE id = @occurrenceId
                  AND status = 0
                  AND claim_id = @claimId
                  AND claim_expires_at > @now
                RETURNING TRUE
                """))
            {
                AddParameter(updateCommand, "draftId", draft.Id);
                AddParameter(updateCommand, "occurrenceId", occurrenceId);
                AddParameter(updateCommand, "claimId", claimId);
                AddParameter(updateCommand, "now", now);
                updated = await updateCommand.ExecuteScalarAsync(ct) is true;
            }

            if (!updated)
            {
                await transaction.RollbackAsync(ct);
                dbContext.ChangeTracker.Clear();
                return false;
            }

            await transaction.CommitAsync(ct);
            return true;
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            dbContext.ChangeTracker.Clear();
            if (IsPersistenceConflict(exception))
            {
                return false;
            }

            throw;
        }
    }

    public async Task<bool> TryMarkFailedAsync(
        Guid occurrenceId,
        Guid claimId,
        string code,
        DateTimeOffset now,
        CancellationToken ct)
    {
        code = OcorrenciaAgendamentoPresenca.NormalizarCodigoPublico(
            code,
            MessageCodes.PresenceScheduleTimeZoneInvalid);
        const string sql = """
            UPDATE ocorrencias_agendamentos_presenca
            SET status = 4,
                codigo_falha = @code,
                claim_id = NULL,
                claim_expires_at = NULL,
                ultima_tentativa_em = @now,
                atualizada_em = @now
            WHERE id = @occurrenceId
              AND status = 0
              AND claim_id = @claimId
              AND claim_expires_at > @now
            RETURNING TRUE
            """;
        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "occurrenceId", occurrenceId);
        AddParameter(command, "claimId", claimId);
        AddParameter(command, "code", code);
        AddParameter(command, "now", now);
        await OpenConnectionAsync(ct);
        return await command.ExecuteScalarAsync(ct) is true;
    }

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new DomainException(MessageCodes.PresenceScheduleOccurrenceConflict);
        }
    }

    private static IQueryable<AgendamentoPresenca> IncludeDays(IQueryable<AgendamentoPresenca> query)
    {
        return query.Include(schedule => schedule.DiasSemana);
    }

    private async Task<IReadOnlyCollection<SummaryRow>> ReadSummaryRowsAsync(
        string filterAndOrder,
        IReadOnlyCollection<NpgsqlParameter> parameters,
        CancellationToken ct)
    {
        var sql = $"""
            SELECT schedule.id AS "Id", {NextExecutionExpression} AS "NextExecution"
            FROM agendamentos_presenca AS schedule
            {filterAndOrder}
            """;
        return await dbContext.Database.SqlQueryRaw<SummaryRow>(sql, parameters.Cast<object>().ToArray()).ToListAsync(ct);
    }

    private sealed class SummaryRow
    {
        public Guid Id { get; init; }
        public DateTimeOffset? NextExecution { get; init; }
    }

    private async Task<bool> ExecuteBooleanAsync(
        string sql,
        Guid agendaId,
        DateOnly localDate,
        DateTimeOffset publicationAt,
        DateTimeOffset closureAt,
        string code,
        DateTimeOffset now,
        CancellationToken ct)
    {
        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "occurrenceId", Guid.NewGuid());
        AddParameter(command, "agendaId", agendaId);
        AddParameter(command, "localDate", localDate);
        AddParameter(command, "publicationAt", publicationAt);
        AddParameter(command, "closureAt", closureAt);
        AddParameter(command, "code", code);
        AddParameter(command, "now", now);
        await OpenConnectionAsync(ct);
        return await command.ExecuteScalarAsync(ct) is true;
    }

    private DbCommand CreateCommand(DbTransaction transaction, string sql)
    {
        var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        return command;
    }

    private async Task OpenConnectionAsync(CancellationToken ct)
    {
        if (dbContext.Database.GetDbConnection().State != ConnectionState.Open)
        {
            await dbContext.Database.OpenConnectionAsync(ct);
        }
    }

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static bool IsPersistenceConflict(Exception exception)
    {
        var postgres = exception as PostgresException
            ?? (exception as DbUpdateException)?.InnerException as PostgresException;
        return postgres?.SqlState == PostgresErrorCodes.UniqueViolation;
    }

}
