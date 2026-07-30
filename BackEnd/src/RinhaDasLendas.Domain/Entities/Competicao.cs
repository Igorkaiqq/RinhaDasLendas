using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Domain.Entities;

public sealed class Competicao
{
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
}
