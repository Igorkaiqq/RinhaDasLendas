using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Domain.Entities;

public sealed class Jogador
{
    private readonly List<PreferenciaRota> _preferencias = [];

    private Jogador()
    {
    }

    public Jogador(
        string nomeExibicao,
        string? nomeReal,
        string? discord,
        string? riotId,
        string? opGgUrl,
        string? deepLolUrl,
        Elo elo,
        Divisao? divisao,
        IEnumerable<PreferenciaRota> preferencias)
    {
        Id = Guid.NewGuid();
        Status = JogadorStatus.Ativo;
        DataCadastro = DateTimeOffset.UtcNow;
        DataAtualizacao = DataCadastro;

        AtualizarDadosBasicos(nomeExibicao, nomeReal, discord, riotId, opGgUrl, deepLolUrl, elo, divisao);
        AtualizarPreferencias(preferencias);
    }

    public Guid Id { get; private set; }
    public Guid? UsuarioId { get; private set; }
    public string NomeExibicao { get; private set; } = string.Empty;
    public string? NomeReal { get; private set; }
    public string? Discord { get; private set; }
    public string? RiotId { get; private set; }
    public string? OpGgUrl { get; private set; }
    public string? DeepLolUrl { get; private set; }
    public Elo Elo { get; private set; }
    public Divisao? Divisao { get; private set; }
    public JogadorStatus Status { get; private set; }
    public DateTimeOffset DataCadastro { get; private set; }
    public DateTimeOffset DataAtualizacao { get; private set; }
    public IReadOnlyCollection<PreferenciaRota> Preferencias => _preferencias.AsReadOnly();

    public void VincularUsuario(Guid usuarioId)
    {
        UsuarioId = usuarioId;
        Touch();
    }

    public void AtualizarDadosBasicos(
        string nomeExibicao,
        string? nomeReal,
        string? discord,
        string? riotId,
        string? opGgUrl,
        string? deepLolUrl,
        Elo elo,
        Divisao? divisao)
    {
        if (string.IsNullOrWhiteSpace(nomeExibicao))
        {
            throw new DomainException("Nome de exibicao e obrigatorio.");
        }

        if (nomeExibicao.Trim().Length > 100)
        {
            throw new DomainException("Nome de exibicao deve ter no maximo 100 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(discord))
        {
            throw new DomainException("Informe o Discord do jogador.");
        }

        if (elo.ExigeDivisao() && divisao is null)
        {
            throw new DomainException("Selecione uma Divisao.");
        }

        if (!elo.ExigeDivisao() && divisao is not null)
        {
            throw new DomainException("Nao informe Divisao para este Elo.");
        }

        NomeExibicao = nomeExibicao.Trim();
        NomeReal = Normalize(nomeReal);
        Discord = discord.Trim();
        RiotId = Normalize(riotId);
        OpGgUrl = Normalize(opGgUrl);
        DeepLolUrl = Normalize(deepLolUrl);
        Elo = elo;
        Divisao = divisao;
        Touch();
    }

    public void AtualizarPreferencias(IEnumerable<PreferenciaRota> preferencias)
    {
        var novasPreferencias = preferencias.ToList();
        ValidarPreferencias(novasPreferencias);

        var rotasAtualizadas = novasPreferencias.Select(preferencia => preferencia.Rota).ToHashSet();
        _preferencias.RemoveAll(preferencia => !rotasAtualizadas.Contains(preferencia.Rota));

        foreach (var novaPreferencia in novasPreferencias)
        {
            var preferenciaExistente = _preferencias.FirstOrDefault(preferencia => preferencia.Rota == novaPreferencia.Rota);

            if (preferenciaExistente is null)
            {
                _preferencias.Add(novaPreferencia);
                continue;
            }

            preferenciaExistente.Atualizar(novaPreferencia.Prioridade, novaPreferencia.NaoJogoNemLascando);
        }

        Touch();
    }

    public void Inativar()
    {
        Status = JogadorStatus.Inativo;
        Touch();
    }

    public void Ativar()
    {
        Status = JogadorStatus.Ativo;
        Touch();
    }

    private static void ValidarPreferencias(IReadOnlyCollection<PreferenciaRota> preferencias)
    {
        if (preferencias.Count != 5)
        {
            throw new DomainException("Informe exatamente cinco preferencias de rota.");
        }

        if (preferencias.Select(preferencia => preferencia.Rota).Distinct().Count() != 5)
        {
            throw new DomainException("Cada rota deve aparecer uma unica vez.");
        }

        if (preferencias.Select(preferencia => preferencia.Prioridade).Distinct().Count() != 5)
        {
            throw new DomainException("Cada prioridade de rota deve ser unica.");
        }

        if (preferencias.Any(preferencia => preferencia.Prioridade is < 1 or > 5))
        {
            throw new DomainException("As prioridades devem estar entre 1 e 5.");
        }

        if (preferencias.Count(preferencia => preferencia.NaoJogoNemLascando) > 1)
        {
            throw new DomainException("Apenas uma rota pode ser marcada como nao jogo nem lascando.");
        }
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private void Touch()
    {
        DataAtualizacao = DateTimeOffset.UtcNow;
    }
}
