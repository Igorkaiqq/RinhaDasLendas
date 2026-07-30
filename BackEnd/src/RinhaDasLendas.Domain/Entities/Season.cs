using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Domain.Entities;

public sealed class Season
{
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

        if (ano is < 2009 or > 9999 || ordemNoAno <= 0 || criadaPorUsuarioId == Guid.Empty)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        if (dataFimExclusiva <= dataInicio)
        {
            throw new DomainException(MessageCodes.SeasonPeriodInvalid);
        }

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
}
