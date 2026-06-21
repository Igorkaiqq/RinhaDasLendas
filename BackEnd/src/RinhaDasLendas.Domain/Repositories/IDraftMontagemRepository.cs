using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Domain.Repositories;

public interface IDraftMontagemRepository
{
    Task AddAsync(DraftMontagem montagem, CancellationToken cancellationToken);
    Task<DraftMontagem?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DraftMontagem>> ListAsync(string? search, DraftMontagemStatus? status, int page, int pageSize, CancellationToken cancellationToken);
    Task<int> CountAsync(string? search, DraftMontagemStatus? status, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Jogador>> GetJogadoresByIdsAsync(IReadOnlyCollection<Guid> jogadoresIds, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
