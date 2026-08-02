using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;

namespace RinhaDasLendas.Domain.Repositories;

public interface ICalendarioCompetitivoRepository
{
    Task AcquireBootstrapLockAsync(CancellationToken cancellationToken);
    Task<CalendarioCompetitivo?> GetCalendarAsync(CancellationToken cancellationToken);
    Task<CalendarioCompetitivo?> GetWithSeasonsAsync(CancellationToken cancellationToken);
    Task<Season?> GetSeasonByIdAsync(Guid seasonId, CancellationToken cancellationToken);
    Task<Season?> GetActiveSeasonAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Season>> ListSeasonsAsync(IReadOnlyCollection<Guid>? seasonIds, SeasonEstado? estado, int page, int pageSize, CancellationToken cancellationToken);
    Task<int> CountSeasonsAsync(IReadOnlyCollection<Guid>? seasonIds, SeasonEstado? estado, CancellationToken cancellationToken);
    Task<bool> ExistsOverlappingSeasonAsync(DateOnly dataInicio, DateOnly dataFimExclusiva, Guid? excludedSeasonId, CancellationToken cancellationToken);
    Task<bool> ExistsSeasonOrderAsync(int ano, int ordemNoAno, Guid? excludedSeasonId, CancellationToken cancellationToken);
    Task AddAsync(CalendarioCompetitivo calendario, CancellationToken cancellationToken);
    Task AddSeasonAsync(Season season, CancellationToken cancellationToken);
}
