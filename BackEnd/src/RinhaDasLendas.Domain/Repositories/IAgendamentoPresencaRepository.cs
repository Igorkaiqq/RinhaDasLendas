using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Models;

namespace RinhaDasLendas.Domain.Repositories;

public interface IAgendamentoPresencaRepository
{
    Task AddAsync(AgendamentoPresenca agenda, CancellationToken ct);
    Task<AgendamentoPresenca?> GetByIdAsync(Guid id, bool tracking, CancellationToken ct);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct);
    Task<AgendamentoPresencaListItem?> GetSummaryAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyCollection<AgendamentoPresencaListItem>> ListAsync(bool includePaused, int page, int pageSize, CancellationToken ct);
    Task<int> CountAsync(bool includePaused, CancellationToken ct);
    Task<OcorrenciaAgendamentoPresenca?> GetLatestOccurrenceAsync(Guid agendaId, CancellationToken ct);
    Task<IReadOnlyDictionary<Guid, OcorrenciaAgendamentoPresenca>> ListLatestOccurrencesAsync(
        IReadOnlyCollection<Guid> agendaIds,
        CancellationToken ct);
    Task<IReadOnlyCollection<OcorrenciaAgendamentoPresenca>> ListOccurrencesAsync(Guid agendaId, int page, int pageSize, CancellationToken ct);
    Task<int> CountOccurrencesAsync(Guid agendaId, CancellationToken ct);
    Task<IReadOnlyCollection<AgendamentoPresenca>> ListCandidatesAsync(DateOnly throughLocalDate, CancellationToken ct);
    Task<IReadOnlyCollection<OcorrenciaAgendamentoPresenca>> ListBlockedAsync(DateTimeOffset now, CancellationToken ct);
    Task<AgendamentoPresencaOcorrenciaClaim?> TryClaimOccurrenceAsync(
        Guid agendaId,
        DateOnly localDate,
        DateTimeOffset publicationAt,
        DateTimeOffset closureAt,
        Guid claimId,
        DateTimeOffset claimExpiresAt,
        DateTimeOffset now,
        CancellationToken ct);
    Task<bool> TryUpsertBlockedOccurrenceAsync(
        Guid agendaId,
        DateOnly localDate,
        DateTimeOffset publicationAt,
        DateTimeOffset closureAt,
        string code,
        DateTimeOffset now,
        CancellationToken ct);
    Task<bool> TryUpsertMissedOccurrenceAsync(
        Guid agendaId,
        DateOnly localDate,
        DateTimeOffset publicationAt,
        DateTimeOffset closureAt,
        string code,
        DateTimeOffset now,
        CancellationToken ct);
    Task<bool> TryCompleteWithDraftAsync(
        Guid occurrenceId,
        Guid claimId,
        DraftMontagem draft,
        DateTimeOffset now,
        CancellationToken ct);
    Task<bool> TryMarkFailedAsync(
        Guid occurrenceId,
        Guid claimId,
        string code,
        DateTimeOffset now,
        CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
