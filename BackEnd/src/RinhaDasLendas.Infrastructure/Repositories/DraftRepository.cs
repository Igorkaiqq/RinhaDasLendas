using Microsoft.EntityFrameworkCore;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Repositories;
using RinhaDasLendas.Infrastructure.Persistence;

namespace RinhaDasLendas.Infrastructure.Repositories;

public sealed class DraftRepository(RinhaDasLendasDbContext dbContext) : IDraftRepository
{
    public async Task AddAsync(DraftSessao draft, CancellationToken cancellationToken)
    {
        await dbContext.Drafts.AddAsync(draft, cancellationToken);
    }

    public Task<DraftSessao?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return IncludeDraft(dbContext.Drafts)
            .FirstOrDefaultAsync(draft => draft.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<DraftSessao>> ListAsync(string? search, DraftStatus? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        return await IncludeDraft(ApplyFilters(dbContext.Drafts.AsNoTracking(), search, status))
            .OrderByDescending(draft => draft.DataCadastro)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(string? search, DraftStatus? status, CancellationToken cancellationToken)
    {
        return ApplyFilters(dbContext.Drafts.AsNoTracking(), search, status).CountAsync(cancellationToken);
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

    private static IQueryable<DraftSessao> IncludeDraft(IQueryable<DraftSessao> query)
    {
        return query
            .Include(draft => draft.Participantes)
            .ThenInclude(participante => participante.Jogador)
            .Include(draft => draft.Escolhas)
            .ThenInclude(escolha => escolha.Jogador);
    }

    private static IQueryable<DraftSessao> ApplyFilters(IQueryable<DraftSessao> query, string? search, DraftStatus? status)
    {
        if (status is not null)
        {
            query = query.Where(draft => draft.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToUpperInvariant();
            query = query.Where(draft =>
                draft.Nome.ToUpper().Contains(normalized) ||
                draft.Participantes.Any(participante => participante.Jogador != null && participante.Jogador.NomeExibicao.ToUpper().Contains(normalized)));
        }

        return query;
    }
}
