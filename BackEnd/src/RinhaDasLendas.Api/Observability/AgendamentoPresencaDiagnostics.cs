using RinhaDasLendas.Application.Interfaces;

namespace RinhaDasLendas.Api.Observability;

public sealed class AgendamentoPresencaDiagnostics(
    ILogger<AgendamentoPresencaDiagnostics> logger) : IAgendamentoPresencaDiagnostics
{
    public void RecordFailure(
        AgendamentoPresencaDiagnosticStage stage,
        string errorType,
        string code)
    {
        logger.LogWarning(
            "Presence schedule processing failure. Stage: {Stage}; Error type: {ErrorType}; Code: {Code}",
            stage,
            errorType,
            code);
    }
}
