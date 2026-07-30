namespace RinhaDasLendas.Application.Interfaces;

public interface IDraftMontagemRealtimeTelemetry
{
    void RecordFailure(
        Guid draftId,
        long? stateVersion,
        string operation,
        long elapsedMilliseconds,
        Exception exception);
}
