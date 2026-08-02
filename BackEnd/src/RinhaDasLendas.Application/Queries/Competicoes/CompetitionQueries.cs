using FluentValidation;
using MediatR;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Queries.Seasons;
using RinhaDasLendas.Application.Security;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Repositories;
using RinhaDasLendas.Domain.Rules;

namespace RinhaDasLendas.Application.Queries.Competicoes;

public sealed record GetCompeticoesQuery(
    SelecaoSazonal Selecao,
    int Page,
    int PageSize) : IRequest<CompetitionPageDto>;

public sealed record GetSeasonCompeticoesQuery(
    Guid SeasonId,
    int Page,
    int PageSize) : IRequest<CompetitionPageDto?>;

public sealed record GetCompeticaoByIdQuery(Guid CompetitionId)
    : IRequest<CompetitionDetailDto?>;

public sealed class GetCompeticoesQueryHandler(
    ICalendarioCompetitivoRepository calendarRepository,
    ICompeticaoRepository competitionRepository,
    ISerieRepository seriesRepository,
    ICompetitiveAuthorizationService authorization,
    ICompetitiveQuerySnapshot querySnapshot,
    IValidator<GetCompeticoesQuery> competitionsValidator,
    IValidator<GetSeasonCompeticoesQuery> seasonCompetitionsValidator,
    IValidator<GetCompeticaoByIdQuery> competitionByIdValidator)
    : IRequestHandler<GetCompeticoesQuery, CompetitionPageDto>,
      IRequestHandler<GetSeasonCompeticoesQuery, CompetitionPageDto?>,
      IRequestHandler<GetCompeticaoByIdQuery, CompetitionDetailDto?>
{
    private const int SeasonPageSize = 100;

    public async Task<CompetitionPageDto> Handle(
        GetCompeticoesQuery request,
        CancellationToken cancellationToken)
    {
        await competitionsValidator.ValidateAndThrowAsync(request, cancellationToken);
        return await querySnapshot.ExecuteAsync(
            async token =>
            {
                var context = await ResolveSelectionAsync(request.Selecao, token);
                if (request.Selecao.Tipo == SelecaoSazonalTipo.Padrao
                    && context.ActiveSeason is null)
                {
                    return EmptyPage(request.Page, request.PageSize);
                }

                return await BuildPageAsync(
                    context.SeasonIds,
                    context.IncludedSeasons,
                    context.ActiveSeason,
                    request.Page,
                    request.PageSize,
                    token);
            },
            cancellationToken);
    }

    public async Task<CompetitionPageDto?> Handle(
        GetSeasonCompeticoesQuery request,
        CancellationToken cancellationToken)
    {
        await seasonCompetitionsValidator.ValidateAndThrowAsync(request, cancellationToken);
        return await querySnapshot.ExecuteAsync<CompetitionPageDto?>(
            async token =>
            {
                var season = await calendarRepository.GetSeasonByIdAsync(request.SeasonId, token);
                if (season is null)
                {
                    return null;
                }

                var activeSeason = await calendarRepository.GetActiveSeasonAsync(token);
                return await BuildPageAsync(
                    [season.Id],
                    [season],
                    activeSeason,
                    request.Page,
                    request.PageSize,
                    token);
            },
            cancellationToken);
    }

    public async Task<CompetitionDetailDto?> Handle(
        GetCompeticaoByIdQuery request,
        CancellationToken cancellationToken)
    {
        await competitionByIdValidator.ValidateAndThrowAsync(request, cancellationToken);
        return await querySnapshot.ExecuteAsync<CompetitionDetailDto?>(
            async token =>
            {
                var competition = await competitionRepository.GetWithRoundsAndRulesAsync(
                    request.CompetitionId,
                    token);
                if (competition is null)
                {
                    return null;
                }

                var season = await calendarRepository.GetSeasonByIdAsync(competition.SeasonId, token);
                if (season is null)
                {
                    throw new DomainException(MessageCodes.SeasonNotFound);
                }

                var actions = await GetCompetitionActionsAsync(competition, season, token);
                return CompetitionDetailDto.FromEntity(competition, actions);
            },
            cancellationToken);
    }

    private async Task<CompetitionPageDto> BuildPageAsync(
        IReadOnlyCollection<Guid> seasonIds,
        IReadOnlyCollection<Season> includedSeasons,
        Season? activeSeason,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var competitions = await competitionRepository.ListAsync(
            seasonIds,
            page,
            pageSize,
            cancellationToken);
        var total = await competitionRepository.CountAsync(seasonIds, cancellationToken);
        var seasonsById = includedSeasons.ToDictionary(season => season.Id);
        var competitionIdsRequiringStartedCheck = competitions
            .Where(competition => seasonsById.TryGetValue(competition.SeasonId, out var season)
                && season.Estado != SeasonEstado.Encerrada)
            .Select(competition => competition.Id)
            .ToArray();
        var startedCompetitionIds = (await seriesRepository.ListCompetitionIdsWithStartedSeriesAsync(
                competitionIdsRequiringStartedCheck,
                cancellationToken))
            .ToHashSet();
        var items = new List<CompetitionDetailDto>(competitions.Count);
        foreach (var competition in competitions)
        {
            if (!seasonsById.TryGetValue(competition.SeasonId, out var season))
            {
                throw new DomainException(MessageCodes.SeasonNotFound);
            }

            var actions = await GetCompetitionActionsAsync(
                competition,
                season,
                startedCompetitionIds.Contains(competition.Id),
                cancellationToken);
            items.Add(CompetitionDetailDto.FromEntity(competition, actions));
        }

        return new CompetitionPageDto(
            page,
            pageSize,
            items,
            total,
            QueryValidation.TotalPages(total, pageSize),
            activeSeason is not null,
            activeSeason is null ? null : SeasonSummaryDto.FromEntity(activeSeason),
            includedSeasons.Select(SeasonSummaryDto.FromEntity).ToArray());
    }

    private async Task<SelectionContext> ResolveSelectionAsync(
        SelecaoSazonal selection,
        CancellationToken cancellationToken)
    {
        var activeSeason = await calendarRepository.GetActiveSeasonAsync(cancellationToken);
        switch (selection.Tipo)
        {
            case SelecaoSazonalTipo.Padrao:
                return activeSeason is null
                    ? new SelectionContext([], [], null)
                    : new SelectionContext([activeSeason.Id], [activeSeason], activeSeason);
            case SelecaoSazonalTipo.Especifica:
                return new SelectionContext(
                    selection.SeasonIds,
                    await LoadSeasonsAsync(selection.SeasonIds, cancellationToken),
                    activeSeason);
            case SelecaoSazonalTipo.Todas:
                return new SelectionContext(
                    [],
                    await LoadAllSeasonsAsync(cancellationToken),
                    activeSeason);
            default:
                throw new DomainException(MessageCodes.ValidationError);
        }
    }

    private async Task<IReadOnlyCollection<Season>> LoadSeasonsAsync(
        IReadOnlyCollection<Guid>? seasonIds,
        CancellationToken cancellationToken)
    {
        var seasons = new List<Season>();
        for (var page = 1; ; page++)
        {
            var items = await calendarRepository.ListSeasonsAsync(
                seasonIds,
                estado: null,
                page,
                SeasonPageSize,
                cancellationToken);
            seasons.AddRange(items);
            if (items.Count < SeasonPageSize)
            {
                break;
            }
        }

        return seasons;
    }

    private async Task<IReadOnlyCollection<Season>> LoadAllSeasonsAsync(
        CancellationToken cancellationToken)
    {
        var total = await calendarRepository.CountSeasonsAsync(
            seasonIds: null,
            estado: null,
            cancellationToken);
        var pageCount = QueryValidation.TotalPages(total, SeasonPageSize);
        var seasons = new List<Season>(total);
        for (var page = 1; page <= pageCount; page++)
        {
            seasons.AddRange(await calendarRepository.ListSeasonsAsync(
                seasonIds: null,
                estado: null,
                page,
                SeasonPageSize,
                cancellationToken));
        }

        return seasons;
    }

    private async Task<IReadOnlyCollection<string>> GetCompetitionActionsAsync(
        Competicao competition,
        Season season,
        CancellationToken cancellationToken)
    {
        if (season.Estado == SeasonEstado.Encerrada)
        {
            return [];
        }

        var hasStartedSeries = await seriesRepository.HasStartedSeriesAsync(
            season.Id,
            competition.Id,
            cancellationToken);
        return await GetCompetitionActionsAsync(
            competition,
            season,
            hasStartedSeries,
            cancellationToken);
    }

    private async Task<IReadOnlyCollection<string>> GetCompetitionActionsAsync(
        Competicao competition,
        Season season,
        bool hasStartedSeries,
        CancellationToken cancellationToken)
    {
        if (season.Estado == SeasonEstado.Encerrada)
        {
            return [];
        }

        var candidates = new[]
        {
            (Action: "edit", Context: CompetitionContext(
                "ConfigurarCompeticao", "Competicao", season, hasStartedSeries)),
            (Action: "create-round", Context: CompetitionContext(
                "ConfigurarRodada", "Rodada", season, hasStartedSeries)),
            (Action: "reorder-rounds", Context: CompetitionContext(
                "ConfigurarRodada", "Rodada", season, hasStartedSeries)),
            (Action: "publish-rules", Context: CompetitionContext(
                "Publicar", "Regra", season, hasStartedSeries))
        };
        var allowed = await authorization.GetAllowedActionsAsync(
            candidates.Select(item => item.Context).ToArray(),
            cancellationToken);
        return candidates
            .Where(item => allowed.Contains(item.Context.Operation, StringComparer.Ordinal))
            .Select(item => item.Action)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

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

    private static CompetitionPageDto EmptyPage(int page, int pageSize) =>
        new(page, pageSize, [], 0, 0, false, null, []);

    private sealed record SelectionContext(
        IReadOnlyCollection<Guid> SeasonIds,
        IReadOnlyCollection<Season> IncludedSeasons,
        Season? ActiveSeason);
}
