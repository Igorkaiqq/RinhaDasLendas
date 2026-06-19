using Microsoft.EntityFrameworkCore;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Repositories;
using RinhaDasLendas.Infrastructure.Persistence;

namespace RinhaDasLendas.Infrastructure.Repositories;

public sealed class TimeRepository(RinhaDasLendasDbContext dbContext) : ITimeRepository
{
    public async Task AddAsync(Time time, CancellationToken cancellationToken)
    {
        await dbContext.Times.AddAsync(time, cancellationToken);
    }

    public Task<Time?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Times
            .Include(time => time.Membros)
            .ThenInclude(membro => membro.Jogador)
            .FirstOrDefaultAsync(time => time.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Time>> ListAsync(string? search, TimeStatus? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = ApplyFilters(dbContext.Times.AsNoTracking(), search, status)
            .Include(time => time.Membros)
            .ThenInclude(membro => membro.Jogador)
            .OrderBy(time => time.Nome);

        return await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(string? search, TimeStatus? status, CancellationToken cancellationToken)
    {
        return ApplyFilters(dbContext.Times.AsNoTracking(), search, status).CountAsync(cancellationToken);
    }

    private static IQueryable<Time> ApplyFilters(IQueryable<Time> query, string? search, TimeStatus? status)
    {

        if (status is not null)
        {
            query = query.Where(time => time.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToUpperInvariant();
            query = query.Where(time =>
                time.NomeNormalizado.Contains(normalizedSearch) ||
                time.TagNormalizada.Contains(normalizedSearch) ||
                time.Membros.Any(membro => membro.Jogador != null && membro.Jogador.NomeExibicao.ToUpper().Contains(normalizedSearch)));
        }

        return query;
    }

    public Task<bool> ExistsActiveNameAsync(string nomeNormalizado, Guid? ignoredId, CancellationToken cancellationToken)
    {
        return dbContext.Times.AnyAsync(time =>
            time.Status == TimeStatus.Ativo &&
            time.NomeNormalizado == nomeNormalizado &&
            (!ignoredId.HasValue || time.Id != ignoredId.Value), cancellationToken);
    }

    public Task<bool> ExistsActiveTagAsync(string tagNormalizada, Guid? ignoredId, CancellationToken cancellationToken)
    {
        return dbContext.Times.AnyAsync(time =>
            time.Status == TimeStatus.Ativo &&
            time.TagNormalizada == tagNormalizada &&
            (!ignoredId.HasValue || time.Id != ignoredId.Value), cancellationToken);
    }

    public async Task<IReadOnlyCollection<Jogador>> GetJogadoresByIdsAsync(IReadOnlyCollection<Guid> jogadoresIds, CancellationToken cancellationToken)
    {
        return await dbContext.Jogadores
            .Where(jogador => jogadoresIds.Contains(jogador.Id))
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
