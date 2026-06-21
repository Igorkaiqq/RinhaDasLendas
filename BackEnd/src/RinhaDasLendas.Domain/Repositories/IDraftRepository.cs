using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Domain.Repositories;

public interface IDraftRepository
{
    Task AddAsync(DraftSessao draft, CancellationToken cancellationToken);
    Task<DraftSessao?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DraftSessao>> ListAsync(string? search, DraftStatus? status, int page, int pageSize, CancellationToken cancellationToken);
    Task<int> CountAsync(string? search, DraftStatus? status, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Jogador>> GetJogadoresByIdsAsync(IReadOnlyCollection<Guid> jogadoresIds, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
