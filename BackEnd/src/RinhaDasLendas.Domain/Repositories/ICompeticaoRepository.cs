using RinhaDasLendas.Domain.Entities;

namespace RinhaDasLendas.Domain.Repositories;

public interface ICompeticaoRepository
{
    Task<Competicao?> GetWithRoundsAndRulesAsync(Guid competicaoId, CancellationToken cancellationToken);
    Task<VersaoRegras?> GetRulesVersionAsync(Guid versaoRegrasId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Competicao>> ListAsync(IReadOnlyCollection<Guid> seasonIds, int page, int pageSize, CancellationToken cancellationToken);
    Task<int> CountAsync(IReadOnlyCollection<Guid> seasonIds, CancellationToken cancellationToken);
    Task<bool> ExistsCodeAsync(Guid seasonId, string codigo, Guid? excludedCompetitionId, CancellationToken cancellationToken);
    Task<bool> ExistsDailyCircuitAsync(Guid seasonId, Guid? excludedCompetitionId, CancellationToken cancellationToken);
    Task<int> GetNextGeneralRulesNumberAsync(Guid seasonId, CancellationToken cancellationToken);
    Task AddAsync(Competicao competicao, CancellationToken cancellationToken);
    Task AddRoundAsync(Rodada rodada, CancellationToken cancellationToken);
    Task AddRulesVersionAsync(VersaoRegras versaoRegras, CancellationToken cancellationToken);
}
