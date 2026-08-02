using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RinhaDasLendas.Application.Interfaces;

namespace RinhaDasLendas.Infrastructure.Persistence;

public sealed class CompetitiveQuerySnapshot(RinhaDasLendasDbContext dbContext)
    : ICompetitiveQuerySnapshot
{
    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> query,
        CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsRelational())
        {
            return await query(cancellationToken);
        }

        var currentTransaction = dbContext.Database.CurrentTransaction;
        if (currentTransaction is not null)
        {
            var isolationLevel = currentTransaction.GetDbTransaction().IsolationLevel;
            if (isolationLevel is not (IsolationLevel.RepeatableRead or IsolationLevel.Serializable))
            {
                throw new InvalidOperationException(nameof(CompetitiveQuerySnapshot));
            }

            return await query(cancellationToken);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.RepeatableRead,
            cancellationToken);
        var result = await query(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }
}
