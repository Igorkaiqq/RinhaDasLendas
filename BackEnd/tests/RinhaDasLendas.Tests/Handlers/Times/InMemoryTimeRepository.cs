using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Tests.Handlers.Times;

internal sealed class InMemoryTimeRepository : ITimeRepository
{
    private readonly List<Time> _times = [];
    private readonly List<Jogador> _jogadores = [];

    public void AddJogador(Jogador jogador)
    {
        _jogadores.Add(jogador);
    }

    public Task AddAsync(Time time, CancellationToken cancellationToken)
    {
        AttachJogadores(time);
        _times.Add(time);
        return Task.CompletedTask;
    }

    public Task<Time?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var time = _times.FirstOrDefault(time => time.Id == id);
        if (time is not null)
        {
            AttachJogadores(time);
        }

        return Task.FromResult(time);
    }

    public Task<IReadOnlyCollection<Time>> ListAsync(string? search, TimeStatus? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        var items = ApplyFilters(search, status)
            .OrderBy(time => time.Nome)
            .Skip((Math.Max(page, 1) - 1) * Math.Clamp(pageSize, 1, 100))
            .Take(Math.Clamp(pageSize, 1, 100))
            .ToList();

        foreach (var time in items)
        {
            AttachJogadores(time);
        }

        return Task.FromResult<IReadOnlyCollection<Time>>(items);
    }

    public Task<int> CountAsync(string? search, TimeStatus? status, CancellationToken cancellationToken)
    {
        return Task.FromResult(ApplyFilters(search, status).Count());
    }

    public Task<bool> ExistsActiveNameAsync(string nomeNormalizado, Guid? ignoredId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_times.Any(time =>
            time.Status == TimeStatus.Ativo &&
            time.NomeNormalizado == nomeNormalizado &&
            (!ignoredId.HasValue || time.Id != ignoredId.Value)));
    }

    public Task<bool> ExistsActiveTagAsync(string tagNormalizada, Guid? ignoredId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_times.Any(time =>
            time.Status == TimeStatus.Ativo &&
            time.TagNormalizada == tagNormalizada &&
            (!ignoredId.HasValue || time.Id != ignoredId.Value)));
    }

    public Task<IReadOnlyCollection<Jogador>> GetJogadoresByIdsAsync(IReadOnlyCollection<Guid> jogadoresIds, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<Jogador>>(_jogadores.Where(jogador => jogadoresIds.Contains(jogador.Id)).ToList());
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private IEnumerable<Time> ApplyFilters(string? search, TimeStatus? status)
    {
        var query = _times.AsEnumerable();
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
                time.Membros.Any(membro => _jogadores.Any(jogador => jogador.Id == membro.JogadorId && jogador.NomeExibicao.ToUpperInvariant().Contains(normalizedSearch))));
        }

        return query;
    }

    private void AttachJogadores(Time time)
    {
        foreach (var membro in time.Membros)
        {
            var jogador = _jogadores.FirstOrDefault(jogador => jogador.Id == membro.JogadorId);
            if (jogador is not null)
            {
                typeof(TimeMembro).GetProperty(nameof(TimeMembro.Jogador))!.SetValue(membro, jogador);
            }
        }
    }
}
