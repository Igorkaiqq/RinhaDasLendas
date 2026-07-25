using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
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
    Task<OcorrenciaAgendamentoPresenca?> GetOccurrenceAsync(Guid agendaId, DateOnly localDate, CancellationToken ct);
    Task<IReadOnlyDictionary<Guid, OcorrenciaAgendamentoPresenca>> ListLatestOccurrencesAsync(
        IReadOnlyCollection<Guid> agendaIds,
        CancellationToken ct);
    Task<IReadOnlyCollection<OcorrenciaAgendamentoPresenca>> ListOccurrencesAsync(Guid agendaId, int page, int pageSize, CancellationToken ct);
    Task<int> CountOccurrencesAsync(Guid agendaId, CancellationToken ct);
    Task<IReadOnlyCollection<AgendamentoPresenca>> ListCandidatesAsync(
        DateTimeOffset now,
        Guid? afterId,
        int limit,
        CancellationToken ct);
    Task<AgendamentoPresencaProcessingCandidate?> GetProcessingCandidateAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyCollection<OcorrenciaAgendamentoPresenca>> ListBlockedAsync(
        DateTimeOffset now,
        int limit,
        CancellationToken ct,
        Guid? afterId = null);
    Task<AgendamentoPresencaOcorrenciaClaim?> TryClaimOccurrenceAsync(
        Guid agendaId,
        DateOnly localDate,
        DateTimeOffset publicationAt,
        DateTimeOffset closureAt,
        Guid claimId,
        DateTimeOffset claimExpiresAt,
        DateTimeOffset now,
        CancellationToken ct,
        string expectedGuildId,
        string expectedPresenceChannelId);
    Task<AgendamentoPresencaOccurrenceWriteResult> TryUpsertBlockedOccurrenceAsync(
        Guid agendaId,
        DateOnly localDate,
        DateTimeOffset publicationAt,
        DateTimeOffset closureAt,
        string code,
        DateTimeOffset now,
        CancellationToken ct);
    Task<AgendamentoPresencaOccurrenceWriteResult> TryUpsertMissedOccurrenceAsync(
        Guid agendaId,
        DateOnly localDate,
        DateTimeOffset publicationAt,
        DateTimeOffset closureAt,
        string code,
        DateTimeOffset now,
        CancellationToken ct);
    Task<AgendamentoPresencaOccurrenceWriteResult> TryUpsertFailedTimeZoneOccurrenceAsync(
        Guid agendaId,
        DateOnly localDate,
        uint observedVersion,
        DiaSemanaIso observedDay,
        TimeOnly observedPublicationTime,
        TimeOnly observedClosureTime,
        DateTimeOffset now,
        CancellationToken ct);
    Task<AgendamentoPresencaOccurrenceWriteResult> TryMarkClaimedOccurrenceMissedAsync(
        Guid occurrenceId,
        Guid claimId,
        DateTimeOffset now,
        CancellationToken ct);
    Task<bool> TryCompleteWithDraftAsync(
        Guid occurrenceId,
        Guid claimId,
        DraftMontagem draft,
        DateTimeOffset now,
        CancellationToken ct,
        string expectedGuildId,
        string expectedPresenceChannelId);
    Task<bool> TryMarkFailedAsync(
        Guid occurrenceId,
        Guid claimId,
        string code,
        DateTimeOffset now,
        CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
