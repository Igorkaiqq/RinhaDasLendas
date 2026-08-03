using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Models;
using RinhaDasLendas.Domain.Repositories;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Infrastructure.Persistence;

namespace RinhaDasLendas.Infrastructure.Repositories;

public sealed class DraftMontagemRepository(RinhaDasLendasDbContext dbContext) : IDraftMontagemRepository
{
    public async Task AddAsync(DraftMontagem montagem, CancellationToken cancellationToken)
    {
        await dbContext.DraftMontagens.AddAsync(montagem, cancellationToken);
    }

    public Task<bool> AnyAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.DraftMontagens
            .AsNoTracking()
            .AnyAsync(montagem => montagem.Id == id && montagem.ArquivadoEm == null, cancellationToken);
    }

    public Task<DraftMontagem?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return IncludeMontagem(dbContext.DraftMontagens).FirstOrDefaultAsync(montagem => montagem.Id == id && montagem.ArquivadoEm == null, cancellationToken);
    }

    public Task<DraftMontagem?> ReloadByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();
        return GetByIdAsync(id, cancellationToken);
    }

    public Task<DraftMontagem?> GetByIdIncludingArchivedAsync(Guid id, CancellationToken cancellationToken)
    {
        return IncludeMontagem(dbContext.DraftMontagens).FirstOrDefaultAsync(montagem => montagem.Id == id, cancellationToken);
    }

    public Task<DraftMontagem?> ReloadByIdIncludingArchivedAsync(Guid id, CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();
        return GetByIdIncludingArchivedAsync(id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<DraftMontagemRealtimeCandidate>> ListExpiredRealtimeAsync(DateTimeOffset now, int limit, CancellationToken cancellationToken)
    {
        return await BuildExpiredRealtimeCandidatesQuery(dbContext.DraftMontagens, now, limit)
            .ToListAsync(cancellationToken);
    }

    internal static IQueryable<DraftMontagemRealtimeCandidate> BuildExpiredRealtimeCandidatesQuery(
        IQueryable<DraftMontagem> source,
        DateTimeOffset now,
        int limit)
    {
        return source
            .AsNoTracking()
            .Where(montagem => montagem.ArquivadoEm == null && montagem.Status == DraftMontagemStatus.Aberta && montagem.Modo == DraftMontagemModo.TempoReal && montagem.TurnoExpiraEm != null && montagem.TurnoExpiraEm <= now)
            .OrderBy(montagem => montagem.TurnoExpiraEm)
            .Select(montagem => new DraftMontagemRealtimeCandidate(montagem.Id))
            .Take(Math.Clamp(limit, 1, 100));
    }

    public async Task<IReadOnlyCollection<DraftMontagemPresenceClosureCandidate>> ListExpiredPresenceAsync(DateTimeOffset now, int limit, CancellationToken cancellationToken)
    {
        return await BuildExpiredPresenceCandidatesQuery(dbContext.DraftMontagens, now, limit)
            .ToListAsync(cancellationToken);
    }

    internal static IQueryable<DraftMontagemPresenceClosureCandidate> BuildExpiredPresenceCandidatesQuery(
        IQueryable<DraftMontagem> source,
        DateTimeOffset now,
        int limit)
    {
        return source
            .AsNoTracking()
            .Where(montagem => montagem.ArquivadoEm == null && montagem.Status == DraftMontagemStatus.PresencaAberta && montagem.HorarioEncerramentoPresenca != null && montagem.HorarioEncerramentoPresenca <= now)
            .OrderBy(montagem => montagem.HorarioEncerramentoPresenca)
            .Select(montagem => new DraftMontagemPresenceClosureCandidate(montagem.Id))
            .Take(Math.Clamp(limit, 1, 100));
    }

    public async Task<IReadOnlyCollection<DraftMontagem>> ListActiveForDiscordAsync(CancellationToken cancellationToken)
    {
        return await IncludeMontagem(dbContext.DraftMontagens.AsNoTracking())
            .Where(montagem => (!montagem.AcoesAdministrativas.Any(acao => acao.Tipo == "CancelamentoPorArquivamento")
                    && montagem.ArquivadoEm == null
                    && ((montagem.Status != DraftMontagemStatus.Cancelada && montagem.Status != DraftMontagemStatus.Finalizada)
                        || montagem.PublicacoesDiscord.Any(publicacao =>
                            publicacao.Status == DraftMontagemPublicacaoDiscordStatus.Pendente
                            || publicacao.Status == DraftMontagemPublicacaoDiscordStatus.EmAndamento
                            || publicacao.Status == DraftMontagemPublicacaoDiscordStatus.RequerReconciliacao)))
                || (montagem.AcoesAdministrativas.Any(acao => acao.Tipo == "CancelamentoPorArquivamento")
                    && montagem.PublicacoesDiscord.Any(publicacao =>
                        publicacao.Tipo == DraftMontagemPublicacaoDiscordTipo.Cancelamento
                        && (publicacao.Status == DraftMontagemPublicacaoDiscordStatus.Pendente
                            || publicacao.Status == DraftMontagemPublicacaoDiscordStatus.EmAndamento
                            || publicacao.Status == DraftMontagemPublicacaoDiscordStatus.RequerReconciliacao))))
            .OrderByDescending(montagem => montagem.HorarioEncerramentoPresenca ?? montagem.DataAtualizacao)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<DraftMontagem>> ListAsync(string? search, DraftMontagemStatus? status, bool includeCancelled, bool includeArchived, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        return await ApplyFilters(dbContext.DraftMontagens.AsNoTracking(), search, status, includeCancelled, includeArchived)
            .OrderByDescending(montagem => montagem.HorarioEncerramentoPresenca ?? montagem.DataCadastro)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(string? search, DraftMontagemStatus? status, bool includeCancelled, bool includeArchived, CancellationToken cancellationToken)
    {
        return ApplyFilters(dbContext.DraftMontagens.AsNoTracking(), search, status, includeCancelled, includeArchived).CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Jogador>> GetJogadoresByIdsAsync(IReadOnlyCollection<Guid> jogadoresIds, CancellationToken cancellationToken)
    {
        return await dbContext.Jogadores.Where(jogador => jogadoresIds.Contains(jogador.Id)).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Guid>> GetCapitaesElegiveisIdsAsync(
        IReadOnlyCollection<Guid> jogadoresIds,
        CancellationToken cancellationToken)
    {
        return await dbContext.Jogadores
            .AsNoTracking()
            .Where(jogador => jogadoresIds.Contains(jogador.Id)
                && jogador.Status == JogadorStatus.Ativo
                && jogador.UsuarioId != null)
            .Where(jogador => dbContext.Users.Any(usuario => usuario.Id == jogador.UsuarioId && usuario.Ativo))
            .Where(jogador => dbContext.UserRoles.Any(usuarioRole => usuarioRole.UserId == jogador.UsuarioId
                && dbContext.Roles.Any(role => role.Id == usuarioRole.RoleId && role.Name == AuthRoles.Capitao)))
            .Select(jogador => jogador.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<Jogador?> GetJogadorByUsuarioIdAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        return dbContext.Jogadores
            .Include(jogador => jogador.Preferencias)
            .FirstOrDefaultAsync(jogador => jogador.UsuarioId == usuarioId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Jogador>> SearchJogadoresElegiveisParaPresencaManualAsync(Guid draftMontagemId, string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        return await ApplyEligibleManualPresenceFilters(draftMontagemId, search)
            .OrderBy(jogador => jogador.NomeExibicao)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountJogadoresElegiveisParaPresencaManualAsync(Guid draftMontagemId, string? search, CancellationToken cancellationToken)
    {
        return ApplyEligibleManualPresenceFilters(draftMontagemId, search).CountAsync(cancellationToken);
    }

    public async Task<DraftMontagemPublicacaoClaimResult?> TryClaimPublicacaoDiscordAsync(
        Guid draftMontagemId,
        DraftMontagemPublicacaoDiscordTipo tipo,
        Guid claimId,
        DateTimeOffset expiraEm,
        DateTimeOffset agora,
        CancellationToken cancellationToken)
    {
        await OpenConnectionAsync(cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var dbTransaction = transaction.GetDbTransaction();

        await using (var lockCommand = dbContext.Database.GetDbConnection().CreateCommand())
        {
            lockCommand.Transaction = dbTransaction;
            lockCommand.CommandText = "SELECT pg_advisory_xact_lock(hashtextextended(@resource, 0))";
            AddParameter(lockCommand, "resource", $"draft-publication:{draftMontagemId:N}:{tipo}");
            await lockCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        DraftMontagemPublicacaoClaimResult? result;
        await using (var claimCommand = dbContext.Database.GetDbConnection().CreateCommand())
        {
            claimCommand.Transaction = dbTransaction;
            claimCommand.CommandText = """
                WITH terminal AS (
                    UPDATE draft_montagem_publicacoes_discord AS publication
                    SET status = 'Falha',
                        ultimo_erro_codigo = CASE
                            WHEN draft.status <> 'PresencaAberta' THEN 'DRAFT_NOT_OPEN'
                            ELSE 'PRESENCE_DEADLINE_EXPIRED'
                        END,
                        claim_id = NULL,
                        claim_expira_em = NULL,
                        ultima_tentativa_em = @agora
                    FROM draft_montagens AS draft
                    WHERE publication.draft_montagem_id = draft.id
                      AND draft.id = @draftMontagemId
                      AND @tipo IN ('Presenca', 'ChamadaPresenca')
                      AND (draft.status <> 'PresencaAberta'
                           OR draft.horario_encerramento_presenca IS NULL
                           OR draft.horario_encerramento_presenca <= clock_timestamp())
                      AND publication.tipo = @tipo
                      AND publication.status = 'Pendente'
                    RETURNING publication.draft_montagem_id
                ), claimed AS (
                INSERT INTO draft_montagem_publicacoes_discord
                    (id, draft_montagem_id, tipo, status, ultima_tentativa_em, claim_id, claim_expira_em)
                SELECT @id, id, @tipo, 'EmAndamento', @agora, @claimId,
                       CASE WHEN @tipo IN ('Presenca', 'ChamadaPresenca')
                           THEN LEAST(@expiraEm, horario_encerramento_presenca)
                           ELSE @expiraEm END
                FROM draft_montagens
                WHERE id = @draftMontagemId
                  AND ((@tipo = 'Cancelamento' AND EXISTS (
                           SELECT 1 FROM draft_montagem_acoes_administrativas AS action
                           WHERE action.draft_montagem_id = draft_montagens.id
                             AND action.tipo = 'CancelamentoPorArquivamento'))
                       OR (@tipo <> 'Cancelamento'
                           AND arquivado_em IS NULL
                           AND NOT EXISTS (
                               SELECT 1 FROM draft_montagem_acoes_administrativas AS action
                               WHERE action.draft_montagem_id = draft_montagens.id
                                 AND action.tipo = 'CancelamentoPorArquivamento')))
                  AND (@tipo NOT IN ('Presenca', 'ChamadaPresenca') OR (
                      status = 'PresencaAberta'
                      AND horario_encerramento_presenca > clock_timestamp()))
                ON CONFLICT (draft_montagem_id, tipo) DO UPDATE
                SET status = 'EmAndamento',
                    ultima_tentativa_em = @agora,
                    claim_id = @claimId,
                    claim_expira_em = CASE WHEN @tipo IN ('Presenca', 'ChamadaPresenca')
                        THEN LEAST(@expiraEm, (
                            SELECT horario_encerramento_presenca
                            FROM draft_montagens
                            WHERE id = @draftMontagemId))
                        ELSE @expiraEm END
                WHERE draft_montagem_publicacoes_discord.status = 'Pendente'
                  AND (@tipo NOT IN ('Presenca', 'ChamadaPresenca') OR EXISTS (
                      SELECT 1 FROM draft_montagens AS draft
                      WHERE draft.id = @draftMontagemId
                        AND draft.status = 'PresencaAberta'
                        AND draft.horario_encerramento_presenca > clock_timestamp()))
                  AND EXISTS (
                      SELECT 1 FROM draft_montagens AS draft
                      WHERE draft.id = @draftMontagemId
                        AND ((@tipo = 'Cancelamento' AND EXISTS (
                                 SELECT 1 FROM draft_montagem_acoes_administrativas AS action
                                 WHERE action.draft_montagem_id = draft.id
                                   AND action.tipo = 'CancelamentoPorArquivamento'))
                             OR (@tipo <> 'Cancelamento'
                                 AND draft.arquivado_em IS NULL
                                 AND NOT EXISTS (
                                     SELECT 1 FROM draft_montagem_acoes_administrativas AS action
                                     WHERE action.draft_montagem_id = draft.id
                                       AND action.tipo = 'CancelamentoPorArquivamento'))))
                RETURNING draft_montagem_id, claim_expira_em
                ), changed AS (
                    SELECT draft_montagem_id FROM terminal
                    UNION
                    SELECT draft_montagem_id FROM claimed
                ), parent AS (
                    UPDATE draft_montagens AS draft
                    SET versao_estado = draft.versao_estado + 1,
                        data_atualizacao = @agora
                    FROM changed
                    WHERE draft.id = changed.draft_montagem_id
                    RETURNING draft.id, draft.versao_estado, draft.data_atualizacao
                ), state AS (
                    SELECT publication.status
                    FROM draft_montagem_publicacoes_discord AS publication
                    WHERE publication.draft_montagem_id = @draftMontagemId
                      AND publication.tipo = @tipo
                    UNION ALL
                    SELECT 'EmAndamento'
                    WHERE EXISTS (SELECT 1 FROM claimed)
                      AND NOT EXISTS (
                          SELECT 1
                          FROM draft_montagem_publicacoes_discord AS publication
                          WHERE publication.draft_montagem_id = @draftMontagemId
                            AND publication.tipo = @tipo)
                )
                SELECT CASE
                           WHEN terminal.draft_montagem_id IS NOT NULL THEN 'Falha'
                           WHEN claimed.draft_montagem_id IS NOT NULL THEN 'EmAndamento'
                           ELSE state.status
                       END,
                       claimed.claim_expira_em,
                       parent.id,
                       parent.versao_estado,
                       parent.data_atualizacao
                FROM state
                LEFT JOIN terminal ON true
                LEFT JOIN claimed ON true
                LEFT JOIN parent ON true
                """;
            AddParameter(claimCommand, "id", Guid.NewGuid());
            AddParameter(claimCommand, "draftMontagemId", draftMontagemId);
            AddParameter(claimCommand, "tipo", tipo.ToString());
            AddParameter(claimCommand, "claimId", claimId);
            AddParameter(claimCommand, "expiraEm", expiraEm);
            AddParameter(claimCommand, "agora", agora);
            await using var reader = await claimCommand.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                result = null;
            }
            else
            {
                var acquiredExpiry = reader.IsDBNull(1) ? null : reader.GetFieldValue<DateTimeOffset?>(1);
                var adquirido = acquiredExpiry.HasValue;
                var stamp = reader.IsDBNull(2)
                    ? null
                    : new DraftMontagemVersionStamp(
                        reader.GetGuid(2),
                        reader.GetInt64(3),
                        reader.GetFieldValue<DateTimeOffset>(4));
                result = new DraftMontagemPublicacaoClaimResult(
                    new DraftMontagemPublicacaoClaim(
                        adquirido,
                        adquirido ? claimId : null,
                        acquiredExpiry,
                        Enum.Parse<DraftMontagemPublicacaoDiscordStatus>(reader.GetString(0))),
                    stamp);
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<DraftMontagemVersionStamp?> TryConcluirPublicacaoDiscordAsync(
        Guid draftMontagemId,
        DraftMontagemPublicacaoDiscordTipo tipo,
        Guid claimId,
        string? guildId,
        string? channelId,
        string messageId,
        DateTimeOffset agora,
        CancellationToken cancellationToken)
    {
        const string sql = """
            WITH updated AS (
                UPDATE draft_montagem_publicacoes_discord
                SET status = 'Publicada',
                    guild_id = @guildId,
                    channel_id = @channelId,
                    message_id = @messageId,
                    ultimo_erro_codigo = NULL,
                    publicada_em = @agora,
                    ultima_tentativa_em = @agora,
                    claim_expira_em = NULL
                WHERE draft_montagem_id = @draftMontagemId
                  AND tipo = @tipo
                  AND status = 'EmAndamento'
                  AND claim_id = @claimId
                  AND claim_expira_em > @agora
                  AND EXISTS (
                      SELECT 1 FROM draft_montagens AS draft
                      WHERE draft.id = @draftMontagemId
                        AND ((@tipo = 'Cancelamento' AND EXISTS (
                                 SELECT 1 FROM draft_montagem_acoes_administrativas AS action
                                 WHERE action.draft_montagem_id = draft.id
                                   AND action.tipo = 'CancelamentoPorArquivamento'))
                             OR (@tipo <> 'Cancelamento'
                                 AND draft.arquivado_em IS NULL
                                 AND NOT EXISTS (
                                     SELECT 1 FROM draft_montagem_acoes_administrativas AS action
                                     WHERE action.draft_montagem_id = draft.id
                                       AND action.tipo = 'CancelamentoPorArquivamento'))))
                  AND (@tipo NOT IN ('Presenca', 'ChamadaPresenca') OR EXISTS (
                      SELECT 1 FROM draft_montagens AS draft
                      WHERE draft.id = @draftMontagemId
                        AND draft.status = 'PresencaAberta'
                        AND draft.horario_encerramento_presenca > clock_timestamp()))
                RETURNING draft_montagem_id
            ), parent AS (
                UPDATE draft_montagens AS draft
                SET discord_guild_id = CASE WHEN @tipo = 'Presenca' THEN @guildId ELSE draft.discord_guild_id END,
                    discord_presence_message_id = CASE WHEN @tipo = 'Presenca' THEN @messageId ELSE draft.discord_presence_message_id END,
                    versao_estado = draft.versao_estado + 1,
                    data_atualizacao = @agora
                FROM updated
                WHERE draft.id = updated.draft_montagem_id
                RETURNING draft.id, draft.versao_estado, draft.data_atualizacao
            )
            SELECT id, versao_estado, data_atualizacao FROM parent
            """;

        return await ExecuteTransitionAsync(sql, draftMontagemId, tipo, claimId, guildId, channelId, messageId, null, agora, cancellationToken);
    }

    public Task<DraftMontagemVersionStamp?> TryRegistrarFalhaPublicacaoDiscordAsync(
        Guid draftMontagemId,
        DraftMontagemPublicacaoDiscordTipo tipo,
        Guid claimId,
        string? guildId,
        string? channelId,
        string? erroCodigo,
        DateTimeOffset agora,
        CancellationToken cancellationToken)
    {
        const string sql = """
            WITH updated AS (
                UPDATE draft_montagem_publicacoes_discord
                SET status = 'Falha',
                    guild_id = @guildId,
                    channel_id = @channelId,
                    ultimo_erro_codigo = @erroCodigo,
                    ultima_tentativa_em = @agora,
                    claim_expira_em = NULL
                WHERE draft_montagem_id = @draftMontagemId
                  AND tipo = @tipo
                  AND status = 'EmAndamento'
                  AND claim_id = @claimId
                  AND EXISTS (
                      SELECT 1 FROM draft_montagens AS draft
                      WHERE draft.id = @draftMontagemId
                        AND ((@tipo = 'Cancelamento' AND EXISTS (
                                 SELECT 1 FROM draft_montagem_acoes_administrativas AS action
                                 WHERE action.draft_montagem_id = draft.id
                                   AND action.tipo = 'CancelamentoPorArquivamento'))
                             OR (@tipo <> 'Cancelamento'
                                 AND draft.arquivado_em IS NULL
                                 AND NOT EXISTS (
                                     SELECT 1 FROM draft_montagem_acoes_administrativas AS action
                                     WHERE action.draft_montagem_id = draft.id
                                       AND action.tipo = 'CancelamentoPorArquivamento'))))
                  AND (
                      claim_expira_em > clock_timestamp()
                      OR (
                          @erroCodigo = 'PRESENCE_DEADLINE_EXPIRED'
                          AND @tipo IN ('Presenca', 'ChamadaPresenca')
                          AND message_id IS NULL
                          AND EXISTS (
                              SELECT 1
                              FROM draft_montagens AS draft
                              WHERE draft.id = @draftMontagemId
                                AND draft.horario_encerramento_presenca <= clock_timestamp()
                                AND (@tipo <> 'Presenca' OR draft.discord_presence_message_id IS NULL))))
                RETURNING draft_montagem_id
            ), parent AS (
                UPDATE draft_montagens AS draft
                SET versao_estado = draft.versao_estado + 1,
                    data_atualizacao = @agora
                FROM updated
                WHERE draft.id = updated.draft_montagem_id
                RETURNING draft.id, draft.versao_estado, draft.data_atualizacao
            )
            SELECT id, versao_estado, data_atualizacao FROM parent
            """;

        return ExecuteTransitionAsync(sql, draftMontagemId, tipo, claimId, guildId, channelId, null, erroCodigo, agora, cancellationToken);
    }

    public async Task<IReadOnlyCollection<DraftMontagemVersionStamp>> MarcarPublicacoesExpiradasParaReconciliacaoAsync(DateTimeOffset agora, CancellationToken cancellationToken)
    {
        const string sql = """
            WITH updated AS (
                UPDATE draft_montagem_publicacoes_discord
                SET status = 'RequerReconciliacao',
                    claim_expira_em = NULL
                WHERE status = 'EmAndamento'
                  AND claim_expira_em <= @agora
                RETURNING draft_montagem_id
            ), changed AS (
                SELECT DISTINCT draft_montagem_id FROM updated
            ), parent AS (
                UPDATE draft_montagens AS draft
                SET versao_estado = draft.versao_estado + 1,
                    data_atualizacao = @agora
                FROM changed
                WHERE draft.id = changed.draft_montagem_id
                RETURNING draft.id, draft.versao_estado, draft.data_atualizacao
            )
            SELECT id, versao_estado, data_atualizacao
            FROM parent
            """;

        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "agora", agora);
        await OpenConnectionAsync(cancellationToken);
        var stamps = new List<DraftMontagemVersionStamp>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            stamps.Add(new DraftMontagemVersionStamp(
                reader.GetGuid(0),
                reader.GetInt64(1),
                reader.GetFieldValue<DateTimeOffset>(2)));
        }

        return stamps;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            if (DraftMontagemSaveConflictClassifier.Classify(exception) is not null
                || (DraftMontagemSaveConflictClassifier.IsStructuralUniqueViolation(exception)
                    && await HasStaleTrackedDraftVersionAsync(cancellationToken)))
            {
                throw new DomainException(MessageCodes.DraftStateConflict);
            }

            throw;
        }
    }

    public async Task SaveTeamReorderingAsync(Guid draftMontagemId, CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsRelational())
        {
            await SaveChangesAsync(cancellationToken);
            return;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        try
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE draft_montagem_times
                SET ordem = -ordem
                WHERE draft_montagem_id = {draftMontagemId}
                """, cancellationToken);

            foreach (var entry in dbContext.ChangeTracker.Entries<DraftMontagemTime>()
                         .Where(entry => entry.Entity.DraftMontagemId == draftMontagemId))
            {
                var order = entry.Property(team => team.Ordem);
                order.OriginalValue = -order.OriginalValue;
                order.IsModified = true;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            var isConflict = DraftMontagemSaveConflictClassifier.Classify(exception) is not null;
            try
            {
                await transaction.RollbackAsync(CancellationToken.None);
                isConflict = isConflict
                    || (DraftMontagemSaveConflictClassifier.IsStructuralUniqueViolation(exception)
                        && await HasStaleTrackedDraftVersionAsync(CancellationToken.None));
            }
            finally
            {
                dbContext.ChangeTracker.Clear();
            }

            if (isConflict)
            {
                throw new DomainException(MessageCodes.DraftStateConflict);
            }

            throw;
        }
    }

    public async Task<DraftMontagemSaveResultado> TrySaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return DraftMontagemSaveResultado.Persistido;
        }
        catch (Exception exception)
        {
            if (DraftMontagemSaveConflictClassifier.Classify(exception) is { } result)
            {
                return result;
            }

            if (DraftMontagemSaveConflictClassifier.IsStructuralUniqueViolation(exception)
                && await HasStaleTrackedDraftVersionAsync(cancellationToken))
            {
                return DraftMontagemSaveResultado.ConflitoDeVersao;
            }

            throw;
        }
    }

    private async Task<bool> HasStaleTrackedDraftVersionAsync(CancellationToken cancellationToken)
    {
        var trackedVersions = dbContext.ChangeTracker.Entries<DraftMontagem>()
            .Where(entry => entry.State is not EntityState.Added and not EntityState.Detached)
            .Select(entry => new
            {
                entry.Entity.Id,
                OriginalVersion = entry.Property(montagem => montagem.VersaoEstado).OriginalValue,
            })
            .ToList();
        if (trackedVersions.Count == 0)
        {
            return false;
        }

        var ids = trackedVersions.Select(item => item.Id).ToList();
        var persistedVersions = await dbContext.DraftMontagens
            .AsNoTracking()
            .Where(montagem => ids.Contains(montagem.Id))
            .Select(montagem => new { montagem.Id, montagem.VersaoEstado })
            .ToDictionaryAsync(item => item.Id, item => item.VersaoEstado, cancellationToken);
        return trackedVersions.Any(item => persistedVersions.GetValueOrDefault(item.Id, item.OriginalVersion) != item.OriginalVersion);
    }

    private static IQueryable<DraftMontagem> IncludeMontagem(IQueryable<DraftMontagem> query)
    {
        return query
            .AsSplitQuery()
            .Include(montagem => montagem.Times)
            .Include(montagem => montagem.Participantes)
            .ThenInclude(participante => participante.Jogador!)
            .ThenInclude(jogador => jogador.Preferencias)
            .Include(montagem => montagem.Presencas)
            .ThenInclude(presenca => presenca.Jogador!)
            .ThenInclude(jogador => jogador.Preferencias)
            .Include(montagem => montagem.Escolhas)
            .ThenInclude(escolha => escolha.Jogador)
            .Include(montagem => montagem.Substituicoes)
            .ThenInclude(substituicao => substituicao.JogadorSaiu)
            .Include(montagem => montagem.Substituicoes)
            .ThenInclude(substituicao => substituicao.ReservaEntrou)
            .Include(montagem => montagem.PublicacoesDiscord)
            .Include(montagem => montagem.AcoesAdministrativas);
    }

    private static IQueryable<DraftMontagem> ApplyFilters(IQueryable<DraftMontagem> query, string? search, DraftMontagemStatus? status, bool includeCancelled, bool includeArchived)
    {
        if (!includeArchived)
        {
            query = query.Where(montagem => montagem.ArquivadoEm == null);
        }
        if (status is not null)
        {
            query = query.Where(montagem => montagem.Status == status);
        }
        else if (!includeCancelled)
        {
            query = query.Where(montagem => montagem.Status != DraftMontagemStatus.Cancelada
                || (includeArchived && montagem.ArquivadoEm != null));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToUpperInvariant();
            query = query.Where(montagem => montagem.Nome.ToUpper().Contains(normalized));
        }

        return query;
    }

    private IQueryable<Jogador> ApplyEligibleManualPresenceFilters(Guid draftMontagemId, string? search)
    {
        var visibleDraft = dbContext.DraftMontagens
            .Where(montagem => montagem.Id == draftMontagemId && montagem.ArquivadoEm == null)
            .Select(montagem => montagem.Id);
        var confirmed = dbContext.DraftMontagemPresencas
            .Where(presenca => visibleDraft.Contains(presenca.DraftMontagemId) && presenca.Status == DraftMontagemPresencaStatus.Confirmada)
            .Select(presenca => presenca.JogadorId);

        var query = dbContext.Jogadores.AsNoTracking()
            .Where(jogador => visibleDraft.Any()
                && jogador.Status == JogadorStatus.Ativo
                && jogador.UsuarioId != null
                && !confirmed.Contains(jogador.Id));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToUpperInvariant();
            query = query.Where(jogador => jogador.NomeExibicao.ToUpper().Contains(normalized));
        }

        return query;
    }

    private async Task<DraftMontagemVersionStamp?> ExecuteTransitionAsync(
        string sql,
        Guid draftMontagemId,
        DraftMontagemPublicacaoDiscordTipo tipo,
        Guid claimId,
        string? guildId,
        string? channelId,
        string? messageId,
        string? erroCodigo,
        DateTimeOffset agora,
        CancellationToken cancellationToken)
    {
        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "draftMontagemId", draftMontagemId);
        AddParameter(command, "tipo", tipo.ToString());
        AddParameter(command, "claimId", claimId);
        AddParameter(command, "guildId", guildId);
        AddParameter(command, "channelId", channelId);
        AddParameter(command, "messageId", messageId);
        AddParameter(command, "erroCodigo", erroCodigo);
        AddParameter(command, "agora", agora);
        await OpenConnectionAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new DraftMontagemVersionStamp(
                reader.GetGuid(0),
                reader.GetInt64(1),
                reader.GetFieldValue<DateTimeOffset>(2))
            : null;
    }

    private async Task OpenConnectionAsync(CancellationToken cancellationToken)
    {
        if (dbContext.Database.GetDbConnection().State != ConnectionState.Open)
        {
            await dbContext.Database.OpenConnectionAsync(cancellationToken);
        }
    }

    private static void AddParameter(System.Data.Common.DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}
