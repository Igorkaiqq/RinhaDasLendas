using FluentValidation;
using MediatR;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Security;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Application.Queries.Seasons;

public sealed record GetSeasonsQuery(SeasonEstado? Estado, int Page, int PageSize)
    : IRequest<SeasonPageDto>;

public sealed record GetSeasonByIdQuery(Guid SeasonId)
    : IRequest<SeasonDetailDto?>;

public sealed record GetSeasonMutationDetailQuery(Season Season)
    : IRequest<SeasonDetailDto>;

public sealed class GetSeasonsQueryHandler(
    ICalendarioCompetitivoRepository repository,
    ICompetitiveQuerySnapshot querySnapshot,
    IValidator<GetSeasonsQuery> validator)
    : IRequestHandler<GetSeasonsQuery, SeasonPageDto>
{
    public async Task<SeasonPageDto> Handle(GetSeasonsQuery request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return await querySnapshot.ExecuteAsync(
            async token =>
            {
                var calendar = await repository.GetCalendarAsync(token);
                var activeSeason = await repository.GetActiveSeasonAsync(token);
                var items = await repository.ListSeasonsAsync(
                    Array.Empty<Guid>(),
                    request.Estado,
                    request.Page,
                    request.PageSize,
                    token);
                var total = await repository.CountSeasonsAsync(
                    Array.Empty<Guid>(),
                    request.Estado,
                    token);

                return new SeasonPageDto(
                    request.Page,
                    request.PageSize,
                    items.Select(SeasonSummaryDto.FromEntity).ToArray(),
                    total,
                    QueryValidation.TotalPages(total, request.PageSize),
                    activeSeason is not null,
                    activeSeason is null ? null : SeasonSummaryDto.FromEntity(activeSeason),
                    calendar?.Versao ?? 0);
            },
            cancellationToken);
    }

}

public sealed class GetSeasonByIdQueryHandler(
    ICalendarioCompetitivoRepository calendarRepository,
    ICompeticaoRepository competitionRepository,
    ISerieRepository seriesRepository,
    ICompetitiveAuthorizationService authorization,
    ICompetitiveQuerySnapshot querySnapshot,
    IValidator<GetSeasonByIdQuery> validator)
    : IRequestHandler<GetSeasonByIdQuery, SeasonDetailDto?>
{
    public async Task<SeasonDetailDto?> Handle(
        GetSeasonByIdQuery request,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return await querySnapshot.ExecuteAsync(
            async token =>
            {
                var season = await calendarRepository.GetSeasonByIdAsync(request.SeasonId, token);
                if (season is null)
                {
                    return null;
                }

                var competitionCount = await competitionRepository.CountAsync([season.Id], token);
                var hasStartedSeries = await seriesRepository.HasStartedSeriesAsync(
                    season.Id,
                    competitionId: null,
                    token);
                var actions = await GetAllowedActionsAsync(
                    season,
                    hasStartedSeries,
                    authorization,
                    token);
                return SeasonDetailDto.FromEntity(season, competitionCount, actions);
            },
            cancellationToken);
    }

    internal static async Task<IReadOnlyCollection<string>> GetAllowedActionsAsync(
        Season season,
        bool hasStartedSeries,
        ICompetitiveAuthorizationService authorization,
        CancellationToken cancellationToken)
    {
        var candidates = new List<(string Action, CompetitiveAuthorizationContext Context)>();
        if (season.Estado == SeasonEstado.Planejada)
        {
            candidates.Add(("edit", SeasonContext("AtualizarSeason", season.Estado)));
            candidates.Add(("activate", SeasonContext("AtivarSeason", season.Estado)));
        }

        if (season.Estado == SeasonEstado.Ativa)
        {
            candidates.Add(("close", SeasonContext("EncerrarSeason", season.Estado)));
        }

        if (season.Estado is SeasonEstado.Planejada or SeasonEstado.Ativa)
        {
            candidates.Add((
                "create-competition",
                CompetitionContext("ConfigurarCompeticao", "Competicao", season, hasStartedSeries)));
            candidates.Add((
                "publish-rules",
                CompetitionContext("Publicar", "Regra", season, hasStartedSeries)));
        }

        var allowed = await authorization.GetAllowedActionsAsync(
            candidates.Select(item => item.Context).ToArray(),
            cancellationToken);
        return candidates
            .Where(item => allowed.Contains(item.Context.Operation, StringComparer.Ordinal))
            .Select(item => item.Action)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static CompetitiveAuthorizationContext SeasonContext(
        string operation,
        SeasonEstado state) =>
        new(
            AuthPermissions.CanManageSeasons,
            operation,
            nameof(Season),
            SerieType: null,
            SeasonState: state.ToString(),
            HasStartedSeries: false,
            HasRequiredJustification: false);

    private static CompetitiveAuthorizationContext CompetitionContext(
        string operation,
        string resourceType,
        Season season,
        bool hasStartedSeries) =>
        new(
            AuthPermissions.CanManageCompetitions,
            operation,
            resourceType,
            SerieType: null,
            SeasonState: season.Estado.ToString(),
            HasStartedSeries: hasStartedSeries,
            HasRequiredJustification: false);
}

public sealed class GetSeasonMutationDetailQueryHandler(
    ICompeticaoRepository competitionRepository,
    ISerieRepository seriesRepository,
    ICompetitiveAuthorizationService authorization)
    : IRequestHandler<GetSeasonMutationDetailQuery, SeasonDetailDto>
{
    public async Task<SeasonDetailDto> Handle(
        GetSeasonMutationDetailQuery request,
        CancellationToken cancellationToken)
    {
        var competitionCount = await competitionRepository.CountAsync([request.Season.Id], cancellationToken);
        var hasStartedSeries = await seriesRepository.HasStartedSeriesAsync(
            request.Season.Id,
            competitionId: null,
            cancellationToken);
        var actions = await GetSeasonByIdQueryHandler.GetAllowedActionsAsync(
            request.Season,
            hasStartedSeries,
            authorization,
            cancellationToken);
        return SeasonDetailDto.FromEntity(request.Season, competitionCount, actions);
    }
}

internal static class QueryValidation
{
    public static int TotalPages(int totalItems, int pageSize) =>
        totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);
}
