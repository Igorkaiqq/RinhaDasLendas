using Microsoft.EntityFrameworkCore;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Repositories;
using RinhaDasLendas.Infrastructure.Persistence;

namespace RinhaDasLendas.Infrastructure.Repositories;

public sealed class SerieRepository(RinhaDasLendasDbContext dbContext) : ISerieRepository
{
    private static readonly TimeZoneInfo SaoPauloTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public Task<bool> HasConfirmedSeriesOutsidePeriodAsync(
        Guid seasonId,
        DateOnly dataInicio,
        DateOnly dataFimExclusiva,
        CancellationToken cancellationToken)
    {
        var startUtc = AtStartOfDayUtc(dataInicio);
        var endUtc = AtStartOfDayUtc(dataFimExclusiva);
        return dbContext.Series
            .AsNoTracking()
            .AnyAsync(
                series => series.SeasonId == seasonId
                    && (series.AgendadaPara < startUtc || series.AgendadaPara >= endUtc)
                    && series.Partidas.Any(match => match.Estado == PartidaEstado.Confirmada),
                cancellationToken);
    }

    public Task<bool> HasStartedSeriesAsync(
        Guid seasonId,
        Guid? competitionId,
        CancellationToken cancellationToken) =>
        dbContext.Series
            .AsNoTracking()
            .AnyAsync(
                series => series.SeasonId == seasonId
                    && (!competitionId.HasValue || series.CompeticaoId == competitionId.Value)
                    && series.Estado != SerieEstado.Agendada
                    && (series.Estado != SerieEstado.Cancelada || series.Partidas.Any()),
                cancellationToken);

    public async Task<IReadOnlyCollection<Guid>> ListCompetitionIdsWithStartedSeriesAsync(
        IReadOnlyCollection<Guid> competitionIds,
        CancellationToken cancellationToken)
    {
        if (competitionIds.Count == 0)
        {
            return [];
        }

        return await dbContext.Series
            .AsNoTracking()
            .Where(series => series.CompeticaoId.HasValue
                && competitionIds.Contains(series.CompeticaoId.Value)
                && series.Estado != SerieEstado.Agendada
                && (series.Estado != SerieEstado.Cancelada || series.Partidas.Any()))
            .Select(series => series.CompeticaoId!.Value)
            .Distinct()
            .ToArrayAsync(cancellationToken);
    }

    public Task<Serie?> GetAggregateAsync(Guid serieId, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<Serie?> GetAggregateByPartidaIdAsync(Guid partidaId, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<IReadOnlyCollection<Serie>> ListAsync(
        IReadOnlyCollection<Guid> seasonIds,
        SerieTipo? tipo,
        SerieEstado? estado,
        int page,
        int pageSize,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<int> CountAsync(
        IReadOnlyCollection<Guid> seasonIds,
        SerieTipo? tipo,
        SerieEstado? estado,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<IReadOnlyCollection<Serie>> ListByEventAsync(
        Guid eventoId,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<bool> ExistsForDraftAsync(Guid draftMontagemId, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task AddAsync(Serie serie, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    private static DateTimeOffset AtStartOfDayUtc(DateOnly date)
    {
        var local = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        return new DateTimeOffset(local, SaoPauloTimeZone.GetUtcOffset(local)).ToUniversalTime();
    }
}
