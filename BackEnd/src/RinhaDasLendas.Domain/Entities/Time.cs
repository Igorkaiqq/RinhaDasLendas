using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Domain.Entities;

public sealed class Time
{
    public const int MaximoJogadoresPrincipais = 5;

    private readonly List<TimeMembro> _membros = [];

    private Time()
    {
    }

    public Time(string nome, string tag, string? observacoes, IEnumerable<Guid> jogadoresIds, Guid? capitaoId)
    {
        Id = Guid.NewGuid();
        Status = TimeStatus.Ativo;
        DataCadastro = DateTimeOffset.UtcNow;
        DataAtualizacao = DataCadastro;

        AtualizarDados(nome, tag, observacoes);
        AtualizarComposicao(jogadoresIds, capitaoId);
    }

    public Guid Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string NomeNormalizado { get; private set; } = string.Empty;
    public string Tag { get; private set; } = string.Empty;
    public string TagNormalizada { get; private set; } = string.Empty;
    public string? Observacoes { get; private set; }
    public TimeStatus Status { get; private set; }
    public Guid? CapitaoId { get; private set; }
    public DateTimeOffset DataCadastro { get; private set; }
    public DateTimeOffset DataAtualizacao { get; private set; }
    public IReadOnlyCollection<TimeMembro> Membros => _membros.AsReadOnly();

    public void Atualizar(string nome, string tag, string? observacoes, IEnumerable<Guid> jogadoresIds, Guid? capitaoId)
    {
        AtualizarDados(nome, tag, observacoes);
        AtualizarComposicao(jogadoresIds, capitaoId);
    }

    public void Inativar()
    {
        Status = TimeStatus.Inativo;
        Touch();
    }

    public void Reativar()
    {
        ValidarComposicao(_membros.Select(membro => membro.JogadorId).ToList(), CapitaoId);
        Status = TimeStatus.Ativo;
        Touch();
    }

    private void AtualizarDados(string nome, string tag, string? observacoes)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new DomainException(MessageCodes.TeamNameRequired);
        }

        if (string.IsNullOrWhiteSpace(tag))
        {
            throw new DomainException(MessageCodes.TeamTagRequired);
        }

        if (nome.Trim().Length > 100)
        {
            throw new DomainException(MessageCodes.MaxLengthExceeded);
        }

        if (tag.Trim().Length > 10)
        {
            throw new DomainException(MessageCodes.MaxLengthExceeded);
        }

        Nome = nome.Trim();
        NomeNormalizado = NormalizarChave(Nome);
        Tag = tag.Trim().ToUpperInvariant();
        TagNormalizada = NormalizarChave(Tag);
        Observacoes = string.IsNullOrWhiteSpace(observacoes) ? null : observacoes.Trim();
        Touch();
    }

    private void AtualizarComposicao(IEnumerable<Guid> jogadoresIds, Guid? capitaoId)
    {
        var jogadores = jogadoresIds.Where(id => id != Guid.Empty).ToList();
        if (jogadores.Distinct().Count() != jogadores.Count)
        {
            throw new DomainException(MessageCodes.PlayerAlreadyInTeam);
        }

        ValidarComposicao(jogadores, capitaoId);

        _membros.RemoveAll(membro => !jogadores.Contains(membro.JogadorId));

        foreach (var jogadorId in jogadores)
        {
            if (_membros.All(membro => membro.JogadorId != jogadorId))
            {
                _membros.Add(new TimeMembro(jogadorId));
            }
        }

        CapitaoId = capitaoId;
        Touch();
    }

    private static void ValidarComposicao(IReadOnlyCollection<Guid> jogadoresIds, Guid? capitaoId)
    {
        if (jogadoresIds.Count == 0)
        {
            throw new DomainException(MessageCodes.TeamPlayersRequired);
        }

        if (jogadoresIds.Count > MaximoJogadoresPrincipais)
        {
            throw new DomainException(MessageCodes.TeamPlayerLimitReached);
        }

        if (capitaoId is not null && !jogadoresIds.Contains(capitaoId.Value))
        {
            throw new DomainException(MessageCodes.TeamCaptainMustBeMember);
        }
    }

    public static string NormalizarChave(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

    private void Touch()
    {
        DataAtualizacao = DateTimeOffset.UtcNow;
    }
}
