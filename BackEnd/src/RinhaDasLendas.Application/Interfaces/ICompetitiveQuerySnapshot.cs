namespace RinhaDasLendas.Application.Interfaces;

public interface ICompetitiveQuerySnapshot
{
    Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> query,
        CancellationToken cancellationToken);
}
