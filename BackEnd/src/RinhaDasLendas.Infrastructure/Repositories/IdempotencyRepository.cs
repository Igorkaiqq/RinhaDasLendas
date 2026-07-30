using Microsoft.EntityFrameworkCore;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Repositories;
using RinhaDasLendas.Infrastructure.Persistence;

namespace RinhaDasLendas.Infrastructure.Repositories;

public sealed class IdempotencyRepository(RinhaDasLendasDbContext dbContext) : IIdempotencyRepository
{
    public Task<OperacaoIdempotente?> FindAsync(
        Guid atorUsuarioId,
        string metodo,
        string rota,
        string chave,
        CancellationToken cancellationToken) =>
        dbContext.OperacoesIdempotentes
            .SingleOrDefaultAsync(operation =>
                    operation.AtorUsuarioId == atorUsuarioId
                    && operation.Metodo == metodo
                    && operation.Rota == rota
                    && operation.Chave == chave,
                cancellationToken);

    public async Task AddAsync(OperacaoIdempotente operacao, CancellationToken cancellationToken) =>
        await dbContext.OperacoesIdempotentes.AddAsync(operacao, cancellationToken);

    public Task RemoveAsync(OperacaoIdempotente operacao, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        dbContext.OperacoesIdempotentes.Remove(operacao);
        return Task.CompletedTask;
    }

    public Task<int> DeleteExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
        dbContext.OperacoesIdempotentes
            .Where(operation => operation.ExpiraEm < now)
            .ExecuteDeleteAsync(cancellationToken);
}
