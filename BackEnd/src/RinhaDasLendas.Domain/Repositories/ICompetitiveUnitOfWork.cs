namespace RinhaDasLendas.Domain.Repositories;

public interface ICompetitiveUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
