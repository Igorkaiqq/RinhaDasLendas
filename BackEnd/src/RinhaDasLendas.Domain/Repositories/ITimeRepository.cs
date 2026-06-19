using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Domain.Repositories;

public interface ITimeRepository
{
    Task AddAsync(Time time, CancellationToken cancellationToken);
    Task<Time?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Time>> ListAsync(string? search, TimeStatus? status, int page, int pageSize, CancellationToken cancellationToken);
    Task<int> CountAsync(string? search, TimeStatus? status, CancellationToken cancellationToken);
    Task<bool> ExistsActiveNameAsync(string nomeNormalizado, Guid? ignoredId, CancellationToken cancellationToken);
    Task<bool> ExistsActiveTagAsync(string tagNormalizada, Guid? ignoredId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Jogador>> GetJogadoresByIdsAsync(IReadOnlyCollection<Guid> jogadoresIds, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
