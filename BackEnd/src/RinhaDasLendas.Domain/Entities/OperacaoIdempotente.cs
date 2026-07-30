using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.ValueObjects;
using System.Text.RegularExpressions;

namespace RinhaDasLendas.Domain.Entities;

public sealed class OperacaoIdempotente
{
    private const int RetencaoEmDias = 90;
    private static readonly Regex MetodoValido = new("^[A-Za-z]+$", RegexOptions.CultureInvariant);
    private static readonly Regex HashSha512 = new("^[0-9a-f]{128}$", RegexOptions.CultureInvariant);

    private OperacaoIdempotente()
    {
    }

    public OperacaoIdempotente(
        Guid atorUsuarioId,
        string metodo,
        string rota,
        string chave,
        string requestHash,
        int statusCode,
        RecursoCompetitivoTipo recursoTipo,
        Guid? recursoId,
        ResultadoOperacaoIdempotente resultado,
        DateTimeOffset criadaEm)
    {
        if (atorUsuarioId == Guid.Empty
            || statusCode is < 100 or > 599
            || !Enum.IsDefined(recursoTipo)
            || resultado is null
            || criadaEm == default)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        if (string.IsNullOrWhiteSpace(metodo)
            || string.IsNullOrWhiteSpace(rota)
            || string.IsNullOrWhiteSpace(chave)
            || string.IsNullOrWhiteSpace(requestHash))
        {
            throw new DomainException(MessageCodes.FieldRequired);
        }

        var metodoNormalizado = metodo.Trim().ToUpperInvariant();
        var rotaNormalizada = CanonicalizarRota(rota);
        if (metodoNormalizado.Length > 10
            || rotaNormalizada.Length > 500
            || chave.Length > 200)
        {
            throw new DomainException(MessageCodes.MaxLengthExceeded);
        }

        if (!MetodoValido.IsMatch(metodoNormalizado)
            || !HashSha512.IsMatch(requestHash)
            || chave.Any(char.IsControl))
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        var criadaEmUtc = criadaEm.ToUniversalTime();
        DateTimeOffset expiraEm;
        try
        {
            expiraEm = criadaEmUtc.AddDays(RetencaoEmDias);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        Id = Guid.NewGuid();
        AtorUsuarioId = atorUsuarioId;
        Metodo = metodoNormalizado;
        Rota = rotaNormalizada;
        Chave = chave;
        RequestHash = requestHash;
        StatusCode = statusCode;
        RecursoTipo = recursoTipo;
        RecursoId = recursoId;
        RespostaMinima = resultado.Serializar();
        CriadaEm = criadaEmUtc;
        ExpiraEm = expiraEm;
    }

    public Guid Id { get; private set; }
    public Guid AtorUsuarioId { get; private set; }
    public string Metodo { get; private set; } = string.Empty;
    public string Rota { get; private set; } = string.Empty;
    public string Chave { get; private set; } = string.Empty;
    public string RequestHash { get; private set; } = string.Empty;
    public int StatusCode { get; private set; }
    public RecursoCompetitivoTipo RecursoTipo { get; private set; }
    public Guid? RecursoId { get; private set; }
    public string RespostaMinima { get; private set; } = string.Empty;
    public DateTimeOffset CriadaEm { get; private set; }
    public DateTimeOffset ExpiraEm { get; private set; }

    private static string CanonicalizarRota(string rota)
    {
        var normalizada = rota.Trim();
        if (!normalizada.StartsWith('/') || normalizada.StartsWith("//", StringComparison.Ordinal) || normalizada.Contains('#'))
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        var inicioQuery = normalizada.IndexOf('?');
        if (inicioQuery >= 0)
        {
            normalizada = normalizada[..inicioQuery];
        }

        normalizada = normalizada.Length > 1 ? normalizada.TrimEnd('/') : normalizada;
        if (normalizada.Length == 0)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        return normalizada.ToLowerInvariant();
    }
}
