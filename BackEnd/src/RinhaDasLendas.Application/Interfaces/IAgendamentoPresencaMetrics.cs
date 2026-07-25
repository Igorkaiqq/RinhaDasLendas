namespace RinhaDasLendas.Application.Interfaces;

public interface IAgendamentoPresencaMetrics
{
    void RecordEvaluated();
    void RecordCreated();
    void RecordBlocked();
    void RecordMissed();
    void RecordFailure(string code);
    void RecordConflict(string code);
    void RecordCycleDuration(TimeSpan duration);
}
