using RinhaDasLendas.Domain.Entities;

namespace RinhaDasLendas.Domain.Repositories;

public interface IJogadorRepository
{
    Task AddAsync(Jogador jogador, CancellationToken cancellationToken);
    Task<Jogador?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Jogador>> ListAsync(bool somenteAtivos, int page, int pageSize, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Jogador>> ListCapitaesElegiveisAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
