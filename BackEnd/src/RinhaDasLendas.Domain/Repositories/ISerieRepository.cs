using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Domain.Repositories;

public interface ISerieRepository
{
    Task<Serie?> GetAggregateAsync(Guid serieId, CancellationToken cancellationToken);
    Task<Serie?> GetAggregateByPartidaIdAsync(Guid partidaId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Serie>> ListAsync(IReadOnlyCollection<Guid> seasonIds, SerieTipo? tipo, SerieEstado? estado, int page, int pageSize, CancellationToken cancellationToken);
    Task<int> CountAsync(IReadOnlyCollection<Guid> seasonIds, SerieTipo? tipo, SerieEstado? estado, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Serie>> ListByEventAsync(Guid eventoId, CancellationToken cancellationToken);
    Task<bool> ExistsForDraftAsync(Guid draftMontagemId, CancellationToken cancellationToken);
    Task<bool> HasConfirmedSeriesOutsidePeriodAsync(Guid seasonId, DateOnly dataInicio, DateOnly dataFimExclusiva, CancellationToken cancellationToken);
    Task<bool> HasStartedSeriesAsync(Guid seasonId, Guid? competitionId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Guid>> ListCompetitionIdsWithStartedSeriesAsync(IReadOnlyCollection<Guid> competitionIds, CancellationToken cancellationToken);
    Task AddAsync(Serie serie, CancellationToken cancellationToken);
}
