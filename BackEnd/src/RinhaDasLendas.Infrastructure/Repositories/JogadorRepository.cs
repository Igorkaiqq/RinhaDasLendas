using Microsoft.EntityFrameworkCore;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Repositories;
using RinhaDasLendas.Infrastructure.Persistence;

namespace RinhaDasLendas.Infrastructure.Repositories;

public sealed class JogadorRepository(RinhaDasLendasDbContext dbContext) : IJogadorRepository
{
    public async Task AddAsync(Jogador jogador, CancellationToken cancellationToken)
    {
        await dbContext.Jogadores.AddAsync(jogador, cancellationToken);
    }

    public Task<Jogador?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Jogadores
            .Include(jogador => jogador.Preferencias)
            .FirstOrDefaultAsync(jogador => jogador.Id == id, cancellationToken);
    }

    public Task<Jogador?> GetByUsuarioIdAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        return dbContext.Jogadores
            .Include(jogador => jogador.Preferencias)
            .FirstOrDefaultAsync(jogador => jogador.UsuarioId == usuarioId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Jogador>> ListAsync(bool somenteAtivos, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = dbContext.Jogadores
            .AsNoTracking()
            .Include(jogador => jogador.Preferencias)
            .OrderBy(jogador => jogador.NomeExibicao)
            .AsQueryable();

        if (somenteAtivos)
        {
            query = query.Where(jogador => jogador.Status == JogadorStatus.Ativo);
        }

        return await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Jogador>> ListCapitaesElegiveisAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var capitaoRoleId = await dbContext.Roles
            .Where(role => role.Name == AuthRoles.Capitao)
            .Select(role => role.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return await dbContext.Jogadores
            .AsNoTracking()
            .Include(jogador => jogador.Preferencias)
            .Where(jogador => jogador.Status == JogadorStatus.Ativo && jogador.UsuarioId != null)
            .Where(jogador => dbContext.UserRoles.Any(userRole => userRole.UserId == jogador.UsuarioId && userRole.RoleId == capitaoRoleId))
            .OrderBy(jogador => jogador.NomeExibicao)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }
}
