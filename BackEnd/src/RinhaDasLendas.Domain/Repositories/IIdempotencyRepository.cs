using RinhaDasLendas.Domain.Entities;

namespace RinhaDasLendas.Domain.Repositories;

public interface IIdempotencyRepository
{
    Task<OperacaoIdempotente?> FindAsync(Guid atorUsuarioId, string metodo, string rota, string chave, CancellationToken cancellationToken);
    Task AddAsync(OperacaoIdempotente operacao, CancellationToken cancellationToken);
    Task RemoveAsync(OperacaoIdempotente operacao, CancellationToken cancellationToken);
    Task<int> DeleteExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken);
}
