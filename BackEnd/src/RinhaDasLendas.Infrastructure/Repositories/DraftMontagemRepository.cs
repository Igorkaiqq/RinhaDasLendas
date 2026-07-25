using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Models;
using RinhaDasLendas.Domain.Repositories;
using RinhaDasLendas.Infrastructure.Persistence;

namespace RinhaDasLendas.Infrastructure.Repositories;

public sealed class DraftMontagemRepository(RinhaDasLendasDbContext dbContext) : IDraftMontagemRepository
{
    public async Task AddAsync(DraftMontagem montagem, CancellationToken cancellationToken)
    {
        await dbContext.DraftMontagens.AddAsync(montagem, cancellationToken);
    }

    public Task<DraftMontagem?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return IncludeMontagem(dbContext.DraftMontagens).FirstOrDefaultAsync(montagem => montagem.Id == id, cancellationToken);
    }

    public Task<DraftMontagem?> ReloadByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();
        return GetByIdAsync(id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<DraftMontagem>> ListExpiredRealtimeAsync(DateTimeOffset now, int limit, CancellationToken cancellationToken)
    {
        return await IncludeMontagem(dbContext.DraftMontagens)
            .Where(montagem => montagem.Status == DraftMontagemStatus.Aberta && montagem.Modo == DraftMontagemModo.TempoReal && montagem.TurnoExpiraEm != null && montagem.TurnoExpiraEm <= now)
            .OrderBy(montagem => montagem.TurnoExpiraEm)
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<DraftMontagem>> ListExpiredPresenceAsync(DateTimeOffset now, int limit, CancellationToken cancellationToken)
    {
        return await IncludeMontagem(dbContext.DraftMontagens)
            .Where(montagem => montagem.Status == DraftMontagemStatus.PresencaAberta && montagem.HorarioEncerramentoPresenca != null && montagem.HorarioEncerramentoPresenca <= now)
            .OrderBy(montagem => montagem.HorarioEncerramentoPresenca)
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<DraftMontagem>> ListActiveForDiscordAsync(CancellationToken cancellationToken)
    {
        return await IncludeMontagem(dbContext.DraftMontagens.AsNoTracking())
            .Where(montagem => (montagem.Status != DraftMontagemStatus.Cancelada && montagem.Status != DraftMontagemStatus.Finalizada)
                || montagem.PublicacoesDiscord.Any(publicacao =>
                    publicacao.Status == DraftMontagemPublicacaoDiscordStatus.Pendente
                    || publicacao.Status == DraftMontagemPublicacaoDiscordStatus.EmAndamento
                    || publicacao.Status == DraftMontagemPublicacaoDiscordStatus.RequerReconciliacao))
            .OrderByDescending(montagem => montagem.HorarioEncerramentoPresenca ?? montagem.DataAtualizacao)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<DraftMontagem>> ListAsync(string? search, DraftMontagemStatus? status, bool includeCancelled, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        return await IncludeMontagem(ApplyFilters(dbContext.DraftMontagens.AsNoTracking(), search, status, includeCancelled))
            .OrderByDescending(montagem => montagem.HorarioEncerramentoPresenca ?? montagem.DataCadastro)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(string? search, DraftMontagemStatus? status, bool includeCancelled, CancellationToken cancellationToken)
    {
        return ApplyFilters(dbContext.DraftMontagens.AsNoTracking(), search, status, includeCancelled).CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Jogador>> GetJogadoresByIdsAsync(IReadOnlyCollection<Guid> jogadoresIds, CancellationToken cancellationToken)
    {
        return await dbContext.Jogadores.Where(jogador => jogadoresIds.Contains(jogador.Id)).ToListAsync(cancellationToken);
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

    public async Task<DraftMontagemPublicacaoClaim?> TryClaimPublicacaoDiscordAsync(
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

        bool adquirido;
        DateTimeOffset? acquiredExpiry;
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
                )
                INSERT INTO draft_montagem_publicacoes_discord
                    (id, draft_montagem_id, tipo, status, ultima_tentativa_em, claim_id, claim_expira_em)
                SELECT @id, id, @tipo, 'EmAndamento', @agora, @claimId,
                       CASE WHEN @tipo IN ('Presenca', 'ChamadaPresenca')
                           THEN LEAST(@expiraEm, horario_encerramento_presenca)
                           ELSE @expiraEm END
                FROM draft_montagens
                WHERE id = @draftMontagemId
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
                RETURNING claim_expira_em
                """;
            AddParameter(claimCommand, "id", Guid.NewGuid());
            AddParameter(claimCommand, "draftMontagemId", draftMontagemId);
            AddParameter(claimCommand, "tipo", tipo.ToString());
            AddParameter(claimCommand, "claimId", claimId);
            AddParameter(claimCommand, "expiraEm", expiraEm);
            AddParameter(claimCommand, "agora", agora);
            var acquiredValue = await claimCommand.ExecuteScalarAsync(cancellationToken);
            acquiredExpiry = acquiredValue switch
            {
                DateTimeOffset value => value,
                DateTime value => new DateTimeOffset(value),
                _ => null,
            };
            adquirido = acquiredExpiry.HasValue;
        }

        DraftMontagemPublicacaoClaim? result;
        await using (var stateCommand = dbContext.Database.GetDbConnection().CreateCommand())
        {
            stateCommand.Transaction = dbTransaction;
            stateCommand.CommandText = """
                SELECT status
                FROM draft_montagem_publicacoes_discord
                WHERE draft_montagem_id = @draftMontagemId AND tipo = @tipo
                """;
            AddParameter(stateCommand, "draftMontagemId", draftMontagemId);
            AddParameter(stateCommand, "tipo", tipo.ToString());
            var status = (string?)await stateCommand.ExecuteScalarAsync(cancellationToken);
            result = status is null
                ? null
                : new DraftMontagemPublicacaoClaim(
                    adquirido,
                    adquirido ? claimId : null,
                    adquirido ? acquiredExpiry : null,
                    Enum.Parse<DraftMontagemPublicacaoDiscordStatus>(status));
        }

        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<bool> TryConcluirPublicacaoDiscordAsync(
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
                  AND (@tipo NOT IN ('Presenca', 'ChamadaPresenca') OR EXISTS (
                      SELECT 1 FROM draft_montagens AS draft
                      WHERE draft.id = @draftMontagemId
                        AND draft.status = 'PresencaAberta'
                        AND draft.horario_encerramento_presenca > clock_timestamp()))
                RETURNING draft_montagem_id
            ), legacy AS (
                UPDATE draft_montagens
                SET discord_guild_id = @guildId,
                    discord_presence_message_id = @messageId,
                    data_atualizacao = @agora
                FROM updated
                WHERE draft_montagens.id = updated.draft_montagem_id
                  AND @tipo = 'Presenca'
            )
            SELECT EXISTS (SELECT 1 FROM updated)
            """;

        return await ExecuteTransitionAsync(sql, draftMontagemId, tipo, claimId, guildId, channelId, messageId, null, agora, cancellationToken);
    }

    public Task<bool> TryRegistrarFalhaPublicacaoDiscordAsync(
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
                  AND claim_expira_em > @agora
                RETURNING 1
            )
            SELECT EXISTS (SELECT 1 FROM updated)
            """;

        return ExecuteTransitionAsync(sql, draftMontagemId, tipo, claimId, guildId, channelId, null, erroCodigo, agora, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Guid>> MarcarPublicacoesExpiradasParaReconciliacaoAsync(DateTimeOffset agora, CancellationToken cancellationToken)
    {
        const string sql = """
            WITH updated AS (
                UPDATE draft_montagem_publicacoes_discord
                SET status = 'RequerReconciliacao',
                    claim_expira_em = NULL
                WHERE status = 'EmAndamento'
                  AND claim_expira_em <= @agora
                RETURNING draft_montagem_id
            )
            SELECT DISTINCT draft_montagem_id
            FROM updated
            """;

        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "agora", agora);
        await OpenConnectionAsync(cancellationToken);
        var ids = new List<Guid>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            ids.Add(reader.GetGuid(0));
        }

        return ids;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<DraftMontagemSaveResultado> TrySaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return DraftMontagemSaveResultado.Persistido;
        }
        catch (Exception exception) when (DraftMontagemSaveConflictClassifier.Classify(exception) is { } result)
        {
            return result;
        }
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

    private static IQueryable<DraftMontagem> ApplyFilters(IQueryable<DraftMontagem> query, string? search, DraftMontagemStatus? status, bool includeCancelled)
    {
        if (status is not null)
        {
            query = query.Where(montagem => montagem.Status == status);
        }
        else if (!includeCancelled)
        {
            query = query.Where(montagem => montagem.Status != DraftMontagemStatus.Cancelada);
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
        var confirmed = dbContext.DraftMontagemPresencas
            .Where(presenca => presenca.DraftMontagemId == draftMontagemId && presenca.Status == DraftMontagemPresencaStatus.Confirmada)
            .Select(presenca => presenca.JogadorId);

        var query = dbContext.Jogadores.AsNoTracking()
            .Where(jogador => jogador.Status == JogadorStatus.Ativo && jogador.UsuarioId != null && !confirmed.Contains(jogador.Id));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToUpperInvariant();
            query = query.Where(jogador => jogador.NomeExibicao.ToUpper().Contains(normalized));
        }

        return query;
    }

    private async Task<bool> ExecuteTransitionAsync(
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
        return (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
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
