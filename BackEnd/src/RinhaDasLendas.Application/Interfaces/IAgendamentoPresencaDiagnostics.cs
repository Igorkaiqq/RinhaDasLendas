namespace RinhaDasLendas.Application.Interfaces;

public enum AgendamentoPresencaDiagnosticStage
{
    DiscordConfiguration,
    BlockedOccurrence,
    CandidateSchedule,
    MarkerPersistence,
    Cycle
}

public interface IAgendamentoPresencaDiagnostics
{
    void RecordFailure(AgendamentoPresencaDiagnosticStage stage, string errorType, string code);
}
