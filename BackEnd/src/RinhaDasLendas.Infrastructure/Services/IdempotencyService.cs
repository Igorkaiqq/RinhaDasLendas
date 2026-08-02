using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Repositories;
using RinhaDasLendas.Domain.ValueObjects;
using RinhaDasLendas.Infrastructure.Persistence;

namespace RinhaDasLendas.Infrastructure.Services;

public sealed class IdempotencyService(
    RinhaDasLendasDbContext dbContext,
    IIdempotencyRepository repository,
    CompetitiveUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IIdempotencyAdvisoryLockHook? lockHook = null) : IIdempotencyService
{
    public async Task<IdempotencyExecutionResult> ExecuteAsync(
        IdempotencyRequest request,
        Func<CancellationToken, Task<IdempotencyResult>> callback,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var normalized = Normalize(request);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        try
        {
            await AcquireLockAsync(normalized, transaction, cancellationToken);
            var operation = await repository.FindAsync(
                normalized.ActorId,
                normalized.Method,
                normalized.Route,
                normalized.Key,
                cancellationToken);
            var now = timeProvider.GetUtcNow();

            if (operation is not null && now <= operation.ExpiraEm)
            {
                if (!string.Equals(operation.RequestHash, normalized.RequestHash, StringComparison.Ordinal))
                {
                    throw new DomainException(MessageCodes.CompetitiveIdempotencyConflict);
                }

                var replay = Restore(operation);
                await transaction.CommitAsync(cancellationToken);
                return new IdempotencyExecutionResult(replay, true);
            }

            if (operation is not null)
            {
                await repository.RemoveAsync(operation, cancellationToken);
            }

            using var deferredSaveScope = unitOfWork.BeginDeferredSaveScope();
            var response = await callback(cancellationToken);
            var result = CreateStoredResult(response);
            await repository.AddAsync(new OperacaoIdempotente(
                normalized.ActorId,
                normalized.Method,
                normalized.Route,
                normalized.Key,
                normalized.RequestHash,
                response.StatusCode,
                response.ResourceType,
                response.ResourceId,
                result,
                now), cancellationToken);
            await unitOfWork.FlushDeferredChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new IdempotencyExecutionResult(response, false);
        }
        catch (Exception exception)
        {
            await RollbackIfUsableAsync(transaction);
            dbContext.ChangeTracker.Clear();
            if (CompetitiveSaveConflictClassifier.Classify(exception) is { } messageCode)
            {
                throw new DomainException(messageCode);
            }
            throw;
        }
    }

    public async Task<int> CleanupExpiredAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        try
        {
            var removed = await repository.DeleteExpiredAsync(
                CeilingToPostgresMicrosecond(timeProvider.GetUtcNow()),
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return removed;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            dbContext.ChangeTracker.Clear();
            throw;
        }
    }

    private async Task AcquireLockAsync(
        IdempotencyRequest request,
        IDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        if (lockHook is not null)
        {
            await lockHook.BeforeAcquireAsync(request, cancellationToken);
        }

        var connection = dbContext.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = "SELECT pg_advisory_xact_lock(hashtextextended(@resource, 0))";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "resource";
        parameter.Value = $"idempotency:{request.ActorId:N}:{request.Method}:{request.Route}:{request.Key}";
        command.Parameters.Add(parameter);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static IdempotencyResult Restore(OperacaoIdempotente operation)
    {
        var stored = ResultadoOperacaoIdempotente.Desserializar(operation.RespostaMinima);
        return new IdempotencyResult(
            operation.StatusCode,
            operation.RecursoTipo,
            operation.RecursoId,
            stored.Conteudo.ToArray(),
            stored.Metadados);
    }

    private static ResultadoOperacaoIdempotente CreateStoredResult(IdempotencyResult response)
    {
        var metadata = response.Metadata
            .Where(item => item.Key != MetadadoResultadoOperacao.Repeticao)
            .ToDictionary(item => item.Key, item => item.Value);
        metadata[MetadadoResultadoOperacao.Repeticao] = false;
        return ResultadoOperacaoIdempotente.Criar(response.Content, metadata);
    }

    private static IdempotencyRequest Normalize(IdempotencyRequest request)
    {
        var route = request.Route.Trim();
        var queryStart = route.IndexOf('?');
        if (queryStart >= 0)
        {
            route = route[..queryStart];
        }

        route = route.Length > 1 ? route.TrimEnd('/') : route;
        return request with
        {
            Method = request.Method.Trim().ToUpperInvariant(),
            Route = route.ToLowerInvariant(),
        };
    }

    private static DateTimeOffset CeilingToPostgresMicrosecond(DateTimeOffset value)
    {
        const long ticksPerMicrosecond = 10;
        var remainder = value.UtcTicks % ticksPerMicrosecond;
        return remainder == 0 ? value : value.AddTicks(ticksPerMicrosecond - remainder);
    }

    private static async Task RollbackIfUsableAsync(IDbContextTransaction transaction)
    {
        try
        {
            await transaction.RollbackAsync(CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
            // PostgreSQL completes the transaction when a deferred constraint fails during commit.
        }
    }
}
