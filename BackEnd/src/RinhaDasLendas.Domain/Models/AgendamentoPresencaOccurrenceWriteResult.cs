using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Domain.Models;

public sealed record AgendamentoPresencaOccurrenceWriteResult(
    OcorrenciaAgendamentoPresencaStatus Status,
    bool Changed)
{
    public bool IsTerminal => Status is OcorrenciaAgendamentoPresencaStatus.Criada
        or OcorrenciaAgendamentoPresencaStatus.Perdida
        or OcorrenciaAgendamentoPresencaStatus.Falha;
}
