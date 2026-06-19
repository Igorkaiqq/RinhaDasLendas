using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Tests.Handlers.Drafts;

internal sealed class InMemoryDraftRepository : IDraftRepository
{
    private readonly List<DraftSessao> _drafts = [];
    private readonly List<Jogador> _jogadores = [];

    public void AddJogador(Jogador jogador)
    {
        _jogadores.Add(jogador);
    }

    public Task AddAsync(DraftSessao draft, CancellationToken cancellationToken)
    {
        AttachJogadores(draft);
        _drafts.Add(draft);
        return Task.CompletedTask;
    }

    public Task<DraftSessao?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var draft = _drafts.FirstOrDefault(item => item.Id == id);
        if (draft is not null)
        {
            AttachJogadores(draft);
        }

        return Task.FromResult(draft);
    }

    public Task<IReadOnlyCollection<DraftSessao>> ListAsync(string? search, DraftStatus? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = _drafts.AsEnumerable();
        if (status is not null)
        {
            query = query.Where(draft => draft.Status == status);
        }

        return Task.FromResult<IReadOnlyCollection<DraftSessao>>(query.ToList());
    }

    public Task<int> CountAsync(string? search, DraftStatus? status, CancellationToken cancellationToken)
    {
        return Task.FromResult(_drafts.Count(draft => status is null || draft.Status == status));
    }

    public Task<IReadOnlyCollection<Jogador>> GetJogadoresByIdsAsync(IReadOnlyCollection<Guid> jogadoresIds, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<Jogador>>(_jogadores.Where(jogador => jogadoresIds.Contains(jogador.Id)).ToList());
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void AttachJogadores(DraftSessao draft)
    {
        foreach (var participante in draft.Participantes)
        {
            var jogador = _jogadores.FirstOrDefault(item => item.Id == participante.JogadorId);
            if (jogador is not null)
            {
                typeof(DraftParticipante).GetProperty(nameof(DraftParticipante.Jogador))!.SetValue(participante, jogador);
            }
        }

        foreach (var escolha in draft.Escolhas)
        {
            var jogador = _jogadores.FirstOrDefault(item => item.Id == escolha.JogadorId);
            if (jogador is not null)
            {
                typeof(DraftEscolha).GetProperty(nameof(DraftEscolha.Jogador))!.SetValue(escolha, jogador);
            }
        }
    }
}
