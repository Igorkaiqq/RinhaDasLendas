using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Domain.Repositories;

public interface IDraftMontagemRepository
{
    Task AddAsync(DraftMontagem montagem, CancellationToken cancellationToken);
    Task<DraftMontagem?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DraftMontagem>> ListExpiredRealtimeAsync(DateTimeOffset now, int limit, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DraftMontagem>> ListExpiredPresenceAsync(DateTimeOffset now, int limit, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DraftMontagem>> ListActiveForDiscordAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DraftMontagem>> ListAsync(string? search, DraftMontagemStatus? status, bool includeCancelled, int page, int pageSize, CancellationToken cancellationToken);
    Task<int> CountAsync(string? search, DraftMontagemStatus? status, bool includeCancelled, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Jogador>> GetJogadoresByIdsAsync(IReadOnlyCollection<Guid> jogadoresIds, CancellationToken cancellationToken);
    Task<Jogador?> GetJogadorByUsuarioIdAsync(Guid usuarioId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Jogador>> SearchJogadoresElegiveisParaPresencaManualAsync(Guid draftMontagemId, string? search, int page, int pageSize, CancellationToken cancellationToken);
    Task<int> CountJogadoresElegiveisParaPresencaManualAsync(Guid draftMontagemId, string? search, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
