using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Application.Interfaces;

public interface IIdempotencyService
{
    Task<IdempotencyExecutionResult> ExecuteAsync(
        IdempotencyRequest request,
        Func<CancellationToken, Task<IdempotencyResult>> callback,
        CancellationToken cancellationToken);

    Task<int> CleanupExpiredAsync(CancellationToken cancellationToken);
}

public interface IIdempotencyAdvisoryLockHook
{
    Task BeforeAcquireAsync(IdempotencyRequest request, CancellationToken cancellationToken);
}

public sealed record IdempotencyRequest(
    Guid ActorId,
    string Method,
    string Route,
    string Key,
    string RequestHash);

public sealed record IdempotencyResult(
    int StatusCode,
    RecursoCompetitivoTipo ResourceType,
    Guid? ResourceId,
    byte[] Content,
    IReadOnlyDictionary<MetadadoResultadoOperacao, object?> Metadata);

public sealed record IdempotencyExecutionResult(IdempotencyResult Result, bool Replayed);
