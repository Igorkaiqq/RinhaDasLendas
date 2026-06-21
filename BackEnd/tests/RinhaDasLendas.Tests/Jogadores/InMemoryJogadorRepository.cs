using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Tests.Jogadores;

internal sealed class InMemoryJogadorRepository : IJogadorRepository
{
    private readonly List<Jogador> _jogadores = [];

    public Task AddAsync(Jogador jogador, CancellationToken cancellationToken)
    {
        _jogadores.Add(jogador);
        return Task.CompletedTask;
    }

    public Task<Jogador?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(_jogadores.FirstOrDefault(jogador => jogador.Id == id));
    }

    public Task<IReadOnlyCollection<Jogador>> ListAsync(bool somenteAtivos, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = _jogadores.AsEnumerable();
        if (somenteAtivos)
        {
            query = query.Where(jogador => jogador.Status == JogadorStatus.Ativo);
        }

        return Task.FromResult<IReadOnlyCollection<Jogador>>(query
            .OrderBy(jogador => jogador.NomeExibicao)
            .Skip((Math.Max(page, 1) - 1) * Math.Clamp(pageSize, 1, 100))
            .Take(Math.Clamp(pageSize, 1, 100))
            .ToList());
    }

    public Task<IReadOnlyCollection<Jogador>> ListCapitaesElegiveisAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<Jogador>>(_jogadores
            .Where(jogador => jogador.Status == JogadorStatus.Ativo && jogador.UsuarioId is not null)
            .OrderBy(jogador => jogador.NomeExibicao)
            .Skip((Math.Max(page, 1) - 1) * Math.Clamp(pageSize, 1, 100))
            .Take(Math.Clamp(pageSize, 1, 100))
            .ToList());
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
