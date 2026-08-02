using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Domain.Entities;

public sealed class Competicao
{
    private readonly List<Rodada> _rodadas = [];
    private readonly List<VersaoRegras> _versoesRegras = [];

    private Competicao()
    {
    }

    public Competicao(
        Guid seasonId,
        string nome,
        string codigo,
        bool circuitoDiario,
        Guid criadaPorUsuarioId,
        DateTimeOffset criadaEm)
    {
        if (seasonId == Guid.Empty || criadaPorUsuarioId == Guid.Empty)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(codigo))
        {
            throw new DomainException(MessageCodes.FieldRequired);
        }

        var nomeNormalizado = nome.Trim();
        var codigoNormalizado = codigo.Trim();
        if (nomeNormalizado.Length > 120 || codigoNormalizado.Length > 40)
        {
            throw new DomainException(MessageCodes.MaxLengthExceeded);
        }

        var criadaEmUtc = criadaEm.ToUniversalTime();
        Id = Guid.NewGuid();
        SeasonId = seasonId;
        Nome = nomeNormalizado;
        Codigo = codigoNormalizado;
        CircuitoDiario = circuitoDiario;
        CriadaEm = criadaEmUtc;
        AtualizadaEm = criadaEmUtc;
        CriadaPorUsuarioId = criadaPorUsuarioId;
        AtualizadaPorUsuarioId = criadaPorUsuarioId;
    }

    public Guid Id { get; private set; }
    public Guid SeasonId { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string Codigo { get; private set; } = string.Empty;
    public bool CircuitoDiario { get; private set; }
    public long Versao { get; private set; }
    public DateTimeOffset CriadaEm { get; private set; }
    public DateTimeOffset AtualizadaEm { get; private set; }
    public Guid CriadaPorUsuarioId { get; private set; }
    public Guid AtualizadaPorUsuarioId { get; private set; }

    public IReadOnlyCollection<Rodada> Rodadas => _rodadas
        .OrderBy(rodada => rodada.Ordem)
        .ToArray();

    public IReadOnlyCollection<VersaoRegras> VersoesRegras => _versoesRegras
        .OrderBy(regras => regras.Numero)
        .ToArray();

    public void ValidarInclusao(IEnumerable<Competicao> competicoes)
    {
        if (competicoes.Any(competicao =>
                competicao.Id != Id
                && competicao.SeasonId == SeasonId
                && string.Equals(competicao.Codigo, Codigo, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomainException(MessageCodes.CompetitionCodeConflict);
        }

        if (CircuitoDiario && competicoes.Any(competicao =>
                competicao.Id != Id
                && competicao.SeasonId == SeasonId
                && competicao.CircuitoDiario))
        {
            throw new DomainException(MessageCodes.DailyCircuitCompetitionInvalid);
        }
    }

    public void Atualizar(
        string nome,
        string codigo,
        bool circuitoDiario,
        long versaoEsperada,
        Guid usuarioId,
        DateTimeOffset atualizadaEm)
    {
        ValidarVersao(versaoEsperada);
        ValidarDados(nome, codigo, usuarioId);

        Nome = nome.Trim();
        Codigo = codigo.Trim();
        CircuitoDiario = circuitoDiario;
        RegistrarAtualizacao(usuarioId, atualizadaEm);
    }

    public Rodada AdicionarRodada(
        string nome,
        int ordem,
        DateTimeOffset criadaEm,
        Guid? usuarioId = null)
    {
        if (_rodadas.Any(rodada => rodada.Ordem == ordem))
        {
            throw new DomainException(MessageCodes.RoundOrderConflict);
        }

        var rodada = new Rodada(Id, nome, ordem, criadaEm);
        _rodadas.Add(rodada);
        RegistrarAtualizacao(usuarioId ?? AtualizadaPorUsuarioId, criadaEm);
        return rodada;
    }

    public void ReordenarRodadas(
        IReadOnlyCollection<Guid> rodadaIds,
        long versaoEsperada,
        Guid usuarioId,
        DateTimeOffset atualizadaEm)
    {
        ValidarVersao(versaoEsperada);
        if (rodadaIds.Count != _rodadas.Count
            || rodadaIds.Count == 0
            || rodadaIds.Distinct().Count() != rodadaIds.Count
            || rodadaIds.Any(id => _rodadas.All(rodada => rodada.Id != id)))
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        var rodadasPorId = _rodadas.ToDictionary(rodada => rodada.Id);
        var novaOrdem = rodadaIds.Select(id => rodadasPorId[id]).ToArray();
        for (var index = 0; index < novaOrdem.Length; index++)
        {
            novaOrdem[index].Reordenar(index + 1, atualizadaEm);
        }

        RegistrarAtualizacao(usuarioId, atualizadaEm);
    }

    public VersaoRegras PublicarRegras(
        SerieFormato formato,
        ModoDraft modoDraft,
        long versaoEsperada,
        Guid usuarioId,
        DateTimeOffset publicadaEm)
    {
        ValidarVersao(versaoEsperada);
        var regras = new VersaoRegras(
            SeasonId,
            Id,
            _versoesRegras.Count == 0 ? 1 : _versoesRegras.Max(item => item.Numero) + 1,
            formato,
            modoDraft,
            usuarioId,
            publicadaEm);
        _versoesRegras.Add(regras);
        RegistrarAtualizacao(usuarioId, publicadaEm);
        return regras;
    }

    private static void ValidarDados(string nome, string codigo, Guid usuarioId)
    {
        if (usuarioId == Guid.Empty)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(codigo))
        {
            throw new DomainException(MessageCodes.FieldRequired);
        }

        if (nome.Trim().Length > 120 || codigo.Trim().Length > 40)
        {
            throw new DomainException(MessageCodes.MaxLengthExceeded);
        }
    }

    private void ValidarVersao(long versaoEsperada)
    {
        if (Versao != versaoEsperada)
        {
            throw new DomainException(MessageCodes.CompetitiveResourceVersionStale);
        }
    }

    private void RegistrarAtualizacao(Guid usuarioId, DateTimeOffset atualizadaEm)
    {
        if (usuarioId == Guid.Empty)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        AtualizadaPorUsuarioId = usuarioId;
        AtualizadaEm = atualizadaEm.ToUniversalTime();
        Versao++;
    }
}
