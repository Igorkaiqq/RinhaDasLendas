using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Domain.Entities;

public sealed class HistoricoAgendamentoPresenca
{
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
        CamposAlterados = string.Join(",", camposAlterados.Order(StringComparer.Ordinal));
    }

    public Guid Id { get; private set; }
    public Guid AgendamentoPresencaId { get; private set; }
    public AgendamentoPresencaAcao Acao { get; private set; }
    public Guid ResponsavelUsuarioId { get; private set; }
    public DateTimeOffset RegistradoEm { get; private set; }
    public string CamposAlterados { get; private set; } = string.Empty;
}
