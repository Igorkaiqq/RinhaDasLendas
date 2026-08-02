using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Rules;

namespace RinhaDasLendas.Domain.Entities;

public sealed class Season
{
    private readonly List<VersaoRegras> _versoesRegrasGerais = [];

    private Season()
    {
    }

    public Season(
        string nome,
        int ano,
        int ordemNoAno,
        DateOnly dataInicio,
        DateOnly dataFimExclusiva,
        Guid criadaPorUsuarioId,
        DateTimeOffset criadaEm)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new DomainException(MessageCodes.SeasonNameRequired);
        }

        var nomeNormalizado = nome.Trim();
        if (nomeNormalizado.Length > 120)
        {
            throw new DomainException(MessageCodes.MaxLengthExceeded);
        }

        if (criadaPorUsuarioId == Guid.Empty)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        SeasonRules.ValidarDadosIntrinsecos(ano, ordemNoAno, dataInicio, dataFimExclusiva);

        var criadaEmUtc = criadaEm.ToUniversalTime();
        Id = Guid.NewGuid();
        Nome = nomeNormalizado;
        Ano = ano;
        OrdemNoAno = ordemNoAno;
        DataInicio = dataInicio;
        DataFimExclusiva = dataFimExclusiva;
        Estado = SeasonEstado.Planejada;
        CriadaEm = criadaEmUtc;
        AtualizadaEm = criadaEmUtc;
        CriadaPorUsuarioId = criadaPorUsuarioId;
        AtualizadaPorUsuarioId = criadaPorUsuarioId;
    }

    public Guid Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public int Ano { get; private set; }
    public int OrdemNoAno { get; private set; }
    public DateOnly DataInicio { get; private set; }
    public DateOnly DataFimExclusiva { get; private set; }
    public SeasonEstado Estado { get; private set; }
    public long Versao { get; private set; }
    public DateTimeOffset CriadaEm { get; private set; }
    public DateTimeOffset AtualizadaEm { get; private set; }
    public DateTimeOffset? AtivadaEm { get; private set; }
    public DateTimeOffset? EncerradaEm { get; private set; }
    public Guid CriadaPorUsuarioId { get; private set; }
    public Guid AtualizadaPorUsuarioId { get; private set; }
    public IReadOnlyCollection<VersaoRegras> VersoesRegrasGerais => _versoesRegrasGerais
        .OrderBy(regras => regras.Numero)
        .ToArray();

    public bool Contem(DateOnly data) => data >= DataInicio && data < DataFimExclusiva;

    public VersaoRegras PublicarRegrasGerais(
        SerieFormato formato,
        ModoDraft modoDraft,
        long versaoEsperada,
        Guid usuarioId,
        DateTimeOffset publicadaEm,
        int? numero = null)
    {
        if (Versao != versaoEsperada)
        {
            throw new DomainException(MessageCodes.CompetitiveResourceVersionStale);
        }

        var proximoNumero = numero ?? (_versoesRegrasGerais.Count == 0
            ? 1
            : _versoesRegrasGerais.Max(item => item.Numero) + 1);
        var regras = new VersaoRegras(
            Id,
            competicaoId: null,
            proximoNumero,
            formato,
            modoDraft,
            usuarioId,
            publicadaEm);
        _versoesRegrasGerais.Add(regras);
        RegistrarAtualizacao(usuarioId, publicadaEm.ToUniversalTime());
        return regras;
    }

    public void Atualizar(
        string nome,
        int ano,
        int ordemNoAno,
        DateOnly dataInicio,
        DateOnly dataFimExclusiva,
        Guid usuarioId,
        DateTimeOffset atualizadaEm)
    {
        if (Estado != SeasonEstado.Planejada || usuarioId == Guid.Empty)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new DomainException(MessageCodes.SeasonNameRequired);
        }

        var nomeNormalizado = nome.Trim();
        if (nomeNormalizado.Length > 120)
        {
            throw new DomainException(MessageCodes.MaxLengthExceeded);
        }

        SeasonRules.ValidarDadosIntrinsecos(ano, ordemNoAno, dataInicio, dataFimExclusiva);

        Nome = nomeNormalizado;
        Ano = ano;
        OrdemNoAno = ordemNoAno;
        DataInicio = dataInicio;
        DataFimExclusiva = dataFimExclusiva;
        RegistrarAtualizacao(usuarioId, atualizadaEm.ToUniversalTime());
    }

    internal void Ativar(Guid usuarioId, DateTimeOffset ativadaEm)
    {
        ValidarAtivacao(usuarioId);

        var ativadaEmUtc = ativadaEm.ToUniversalTime();
        Estado = SeasonEstado.Ativa;
        AtivadaEm = ativadaEmUtc;
        EncerradaEm = null;
        RegistrarAtualizacao(usuarioId, ativadaEmUtc);
    }

    internal void Encerrar(Guid usuarioId, DateTimeOffset encerradaEm)
    {
        ValidarEncerramento(usuarioId, encerradaEm);

        var encerradaEmUtc = encerradaEm.ToUniversalTime();
        Estado = SeasonEstado.Encerrada;
        EncerradaEm = encerradaEmUtc;
        RegistrarAtualizacao(usuarioId, encerradaEmUtc);
    }

    internal void ValidarAtivacao(Guid usuarioId)
    {
        if (usuarioId == Guid.Empty || Estado != SeasonEstado.Planejada)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }
    }

    internal void ValidarEncerramento(Guid usuarioId, DateTimeOffset encerradaEm)
    {
        if (usuarioId == Guid.Empty
            || Estado != SeasonEstado.Ativa
            || AtivadaEm is not null && encerradaEm.ToUniversalTime() < AtivadaEm.Value)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }
    }

    private void RegistrarAtualizacao(Guid usuarioId, DateTimeOffset atualizadaEm)
    {
        AtualizadaEm = atualizadaEm;
        AtualizadaPorUsuarioId = usuarioId;
        Versao++;
    }
}
