using Microsoft.EntityFrameworkCore;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Repositories;
using RinhaDasLendas.Infrastructure.Persistence;

namespace RinhaDasLendas.Infrastructure.Repositories;

public sealed class CompeticaoRepository(RinhaDasLendasDbContext dbContext) : ICompeticaoRepository
{
    public Task<Competicao?> GetWithRoundsAndRulesAsync(
        Guid competicaoId,
        CancellationToken cancellationToken) =>
        WithRoundsAndRules(dbContext.Competicoes)
            .AsSplitQuery()
            .SingleOrDefaultAsync(competition => competition.Id == competicaoId, cancellationToken);

    public Task<VersaoRegras?> GetRulesVersionAsync(
        Guid versaoRegrasId,
        CancellationToken cancellationToken) =>
        dbContext.VersoesRegras
            .AsNoTracking()
            .SingleOrDefaultAsync(rules => rules.Id == versaoRegrasId, cancellationToken);

    public async Task<IReadOnlyCollection<Competicao>> ListAsync(
        IReadOnlyCollection<Guid> seasonIds,
        int page,
        int pageSize,
        CancellationToken cancellationToken) =>
        await WithRoundsAndRules(ApplySeasonFilter(
                dbContext.Competicoes.AsNoTrackingWithIdentityResolution(), seasonIds))
            .AsSplitQuery()
            .OrderBy(competition => competition.Nome)
            .ThenBy(competition => competition.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

    public Task<int> CountAsync(
        IReadOnlyCollection<Guid> seasonIds,
        CancellationToken cancellationToken) =>
        ApplySeasonFilter(dbContext.Competicoes.AsNoTracking(), seasonIds)
            .CountAsync(cancellationToken);

    public Task<bool> ExistsCodeAsync(
        Guid seasonId,
        string codigo,
        Guid? excludedCompetitionId,
        CancellationToken cancellationToken)
    {
        var normalizedCode = codigo.Trim().ToLowerInvariant();
        return dbContext.Competicoes
            .AsNoTracking()
            .AnyAsync(
                competition => competition.SeasonId == seasonId
                    && competition.Codigo.ToLower() == normalizedCode
                    && (!excludedCompetitionId.HasValue || competition.Id != excludedCompetitionId.Value),
                cancellationToken);
    }

    public Task<bool> ExistsDailyCircuitAsync(
        Guid seasonId,
        Guid? excludedCompetitionId,
        CancellationToken cancellationToken) =>
        dbContext.Competicoes
            .AsNoTracking()
            .AnyAsync(
                competition => competition.SeasonId == seasonId
                    && competition.CircuitoDiario
                    && (!excludedCompetitionId.HasValue || competition.Id != excludedCompetitionId.Value),
                cancellationToken);

    public async Task<int> GetNextGeneralRulesNumberAsync(
        Guid seasonId,
        CancellationToken cancellationToken)
    {
        var currentNumber = await dbContext.VersoesRegras
            .AsNoTracking()
            .Where(rules => rules.SeasonId == seasonId && rules.CompeticaoId == null)
            .Select(rules => (int?)rules.Numero)
            .MaxAsync(cancellationToken);
        return currentNumber.GetValueOrDefault() + 1;
    }

    public Task AddAsync(Competicao competicao, CancellationToken cancellationToken) =>
        dbContext.Competicoes.AddAsync(competicao, cancellationToken).AsTask();

    public Task AddRoundAsync(Rodada rodada, CancellationToken cancellationToken) =>
        dbContext.Rodadas.AddAsync(rodada, cancellationToken).AsTask();

    public Task AddRulesVersionAsync(VersaoRegras versaoRegras, CancellationToken cancellationToken) =>
        dbContext.VersoesRegras.AddAsync(versaoRegras, cancellationToken).AsTask();

    private static IQueryable<Competicao> WithRoundsAndRules(IQueryable<Competicao> query) =>
        query
            .Include(competition => competition.Rodadas)
            .Include(competition => competition.VersoesRegras);

    private static IQueryable<Competicao> ApplySeasonFilter(
        IQueryable<Competicao> query,
        IReadOnlyCollection<Guid> seasonIds) =>
        seasonIds.Count == 0
            ? query
            : query.Where(competition => seasonIds.Contains(competition.SeasonId));
}
