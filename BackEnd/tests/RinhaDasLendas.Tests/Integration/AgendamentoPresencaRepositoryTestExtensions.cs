using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Models;
using RinhaDasLendas.Infrastructure.Repositories;

namespace RinhaDasLendas.Tests.Integration;

internal static class AgendamentoPresencaRepositoryTestExtensions
{
    internal static Task<AgendamentoPresencaOcorrenciaClaim?> TryClaimOccurrenceAsync(
        this AgendamentoPresencaRepository repository,
        Guid agendaId,
        DateOnly localDate,
        DateTimeOffset publicationAt,
        DateTimeOffset closureAt,
        Guid claimId,
        DateTimeOffset claimExpiresAt,
        DateTimeOffset now,
        CancellationToken ct) => repository.TryClaimOccurrenceAsync(
            agendaId, localDate, publicationAt, closureAt, claimId, claimExpiresAt, now, ct,
            "guild-1", "presence-channel");

    internal static Task<bool> TryCompleteWithDraftAsync(
        this AgendamentoPresencaRepository repository,
        Guid occurrenceId,
        Guid claimId,
        DraftMontagem draft,
        DateTimeOffset now,
        CancellationToken ct) => repository.TryCompleteWithDraftAsync(
            occurrenceId, claimId, draft, now, ct, "guild-1", "presence-channel");
}
