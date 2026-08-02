using Microsoft.EntityFrameworkCore;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Repositories;
using RinhaDasLendas.Infrastructure.Persistence;

namespace RinhaDasLendas.Infrastructure.Repositories;

public sealed class CalendarioCompetitivoRepository(RinhaDasLendasDbContext dbContext)
    : ICalendarioCompetitivoRepository
{
    private const long BootstrapLockId = 0x52444C43414C;

    public Task AcquireBootstrapLockAsync(CancellationToken cancellationToken) =>
        dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({BootstrapLockId})",
            cancellationToken);

    public Task<CalendarioCompetitivo?> GetCalendarAsync(CancellationToken cancellationToken) =>
        dbContext.CalendariosCompetitivos
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);

    public Task<CalendarioCompetitivo?> GetWithSeasonsAsync(CancellationToken cancellationToken) =>
        dbContext.CalendariosCompetitivos.SingleOrDefaultAsync(cancellationToken);

    public Task<Season?> GetSeasonByIdAsync(Guid seasonId, CancellationToken cancellationToken) =>
        dbContext.Seasons.SingleOrDefaultAsync(season => season.Id == seasonId, cancellationToken);

    public Task<Season?> GetActiveSeasonAsync(CancellationToken cancellationToken) =>
        dbContext.Seasons
            .AsNoTracking()
            .SingleOrDefaultAsync(season => season.Estado == SeasonEstado.Ativa, cancellationToken);

    public async Task<IReadOnlyCollection<Season>> ListSeasonsAsync(
        IReadOnlyCollection<Guid>? seasonIds,
        SeasonEstado? estado,
        int page,
        int pageSize,
        CancellationToken cancellationToken) =>
        await ApplyFilters(dbContext.Seasons.AsNoTracking(), seasonIds, estado)
            .OrderByDescending(season => season.Ano)
            .ThenByDescending(season => season.OrdemNoAno)
            .ThenBy(season => season.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

    public Task<int> CountSeasonsAsync(
        IReadOnlyCollection<Guid>? seasonIds,
        SeasonEstado? estado,
        CancellationToken cancellationToken) =>
        ApplyFilters(dbContext.Seasons.AsNoTracking(), seasonIds, estado)
            .CountAsync(cancellationToken);

    public Task<bool> ExistsOverlappingSeasonAsync(
        DateOnly dataInicio,
        DateOnly dataFimExclusiva,
        Guid? excludedSeasonId,
        CancellationToken cancellationToken) =>
        dbContext.Seasons
            .AsNoTracking()
            .AnyAsync(
                season => (!excludedSeasonId.HasValue || season.Id != excludedSeasonId.Value)
                    && season.DataInicio < dataFimExclusiva
                    && dataInicio < season.DataFimExclusiva,
                cancellationToken);

    public Task<bool> ExistsSeasonOrderAsync(
        int ano,
        int ordemNoAno,
        Guid? excludedSeasonId,
        CancellationToken cancellationToken) =>
        dbContext.Seasons
            .AsNoTracking()
            .AnyAsync(
                season => season.Ano == ano
                    && season.OrdemNoAno == ordemNoAno
                    && (!excludedSeasonId.HasValue || season.Id != excludedSeasonId.Value),
                cancellationToken);

    public Task AddAsync(CalendarioCompetitivo calendario, CancellationToken cancellationToken) =>
        dbContext.CalendariosCompetitivos.AddAsync(calendario, cancellationToken).AsTask();

    public Task AddSeasonAsync(Season season, CancellationToken cancellationToken) =>
        dbContext.Seasons.AddAsync(season, cancellationToken).AsTask();

    private static IQueryable<Season> ApplyFilters(
        IQueryable<Season> query,
        IReadOnlyCollection<Guid>? seasonIds,
        SeasonEstado? estado)
    {
        if (seasonIds is { Count: > 0 })
        {
            query = query.Where(season => seasonIds.Contains(season.Id));
        }

        if (estado.HasValue)
        {
            query = query.Where(season => season.Estado == estado.Value);
        }

        return query;
    }
}
