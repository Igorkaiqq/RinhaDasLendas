using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Domain.Entities;

public sealed class HistoricoAgendamentoPresenca
{
    private const int TamanhoMaximoCamposAlterados = 200;

    private static readonly HashSet<string> CamposPermitidos =
    [
        "Nome",
        "Observacao",
        "DiasSemana",
        "HorarioPublicacaoLocal",
        "HorarioEncerramentoLocal",
        "Status"
    ];

    private HistoricoAgendamentoPresenca()
    {
    }

    internal HistoricoAgendamentoPresenca(
        Guid agendamentoPresencaId,
        AgendamentoPresencaAcao acao,
        Guid responsavelUsuarioId,
        DateTimeOffset registradoEm,
        IEnumerable<string> camposAlterados)
    {
        Id = Guid.NewGuid();
        AgendamentoPresencaId = agendamentoPresencaId;
        Acao = acao;
        ResponsavelUsuarioId = responsavelUsuarioId;
        RegistradoEm = registradoEm;
        CamposAlterados = NormalizarCamposAlterados(camposAlterados);
    }

    public Guid Id { get; private set; }
    public Guid AgendamentoPresencaId { get; private set; }
    public AgendamentoPresencaAcao Acao { get; private set; }
    public Guid ResponsavelUsuarioId { get; private set; }
    public DateTimeOffset RegistradoEm { get; private set; }
    public string CamposAlterados { get; private set; } = string.Empty;

    private static string NormalizarCamposAlterados(IEnumerable<string> camposAlterados)
    {
        var campos = camposAlterados?.ToArray();
        if (campos is null || campos.Any(campo => campo is null || !CamposPermitidos.Contains(campo)))
        {
            throw new DomainException(MessageCodes.PresenceScheduleOccurrenceConflict);
        }

        var resultado = string.Join(",", campos.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));
        if (resultado.Length > TamanhoMaximoCamposAlterados)
        {
            throw new DomainException(MessageCodes.PresenceScheduleOccurrenceConflict);
        }

        return resultado;
    }
}
