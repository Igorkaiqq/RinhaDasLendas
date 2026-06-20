using Microsoft.EntityFrameworkCore;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Repositories;
using RinhaDasLendas.Infrastructure.Persistence;

namespace RinhaDasLendas.Infrastructure.Repositories;

public sealed class DraftMontagemRepository(RinhaDasLendasDbContext dbContext) : IDraftMontagemRepository
{
    public async Task AddAsync(DraftMontagem montagem, CancellationToken cancellationToken)
    {
        await dbContext.DraftMontagens.AddAsync(montagem, cancellationToken);
    }

    public Task<DraftMontagem?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return IncludeMontagem(dbContext.DraftMontagens).FirstOrDefaultAsync(montagem => montagem.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<DraftMontagem>> ListAsync(string? search, DraftMontagemStatus? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        return await IncludeMontagem(ApplyFilters(dbContext.DraftMontagens.AsNoTracking(), search, status))
            .OrderByDescending(montagem => montagem.DataCadastro)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(string? search, DraftMontagemStatus? status, CancellationToken cancellationToken)
    {
        return ApplyFilters(dbContext.DraftMontagens.AsNoTracking(), search, status).CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Jogador>> GetJogadoresByIdsAsync(IReadOnlyCollection<Guid> jogadoresIds, CancellationToken cancellationToken)
    {
        return await dbContext.Jogadores.Where(jogador => jogadoresIds.Contains(jogador.Id)).ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<DraftMontagem> IncludeMontagem(IQueryable<DraftMontagem> query)
    {
        return query
            .AsSplitQuery()
            .Include(montagem => montagem.Times)
            .Include(montagem => montagem.Participantes)
            .ThenInclude(participante => participante.Jogador!)
            .ThenInclude(jogador => jogador.Preferencias);
    }

    private static IQueryable<DraftMontagem> ApplyFilters(IQueryable<DraftMontagem> query, string? search, DraftMontagemStatus? status)
    {
        if (status is not null)
        {
            query = query.Where(montagem => montagem.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToUpperInvariant();
            query = query.Where(montagem => montagem.Nome.ToUpper().Contains(normalized));
        }

        return query;
    }
}
