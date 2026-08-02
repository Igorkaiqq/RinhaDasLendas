using System.Globalization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RinhaDasLendas.Api.Filters;
using RinhaDasLendas.Api.Middleware;
using RinhaDasLendas.Api.Services;
using RinhaDasLendas.Application.Commands.Competicoes;
using RinhaDasLendas.Application.Commands.Seasons;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Queries.Competicoes;
using RinhaDasLendas.Application.Queries.Seasons;
using RinhaDasLendas.Application.Security;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Api.Controllers;

[ApiController]
[Route("api/v1/temporadas")]
[Authorize(Policy = ApiAuthenticationDefaults.AuthenticatedPolicyName)]
[Produces("application/json")]
public sealed class TemporadasController(
    ISender sender,
    IMessageProvider messages,
    ICompetitiveAuthorizationService authorization) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(SeasonPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SeasonPageDto>> GetAll(
        [FromQuery] string? estado = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetSeasonsQuery(ParseSeasonState(estado), page, pageSize),
            cancellationToken);
        SetETag(result.VersaoCalendario);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = AuthPermissions.CanManageSeasons)]
    [RequireIdempotency(RecursoCompetitivoTipo.Season)]
    [ProducesResponseType(typeof(SeasonDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SeasonDetailDto>> Create(
        [FromBody] CreateSeasonRequestDto request,
        CancellationToken cancellationToken)
    {
        var season = await sender.Send(new CreateSeasonCommand(request), cancellationToken);
        var result = await ToDetailAsync(season, cancellationToken);
        SetETag(result.Versao);
        return CreatedAtAction(nameof(GetById), new { seasonId = result.Id }, result);
    }

    [HttpGet("{seasonId:guid}")]
    [ProducesResponseType(typeof(SeasonDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SeasonDetailDto>> GetById(
        Guid seasonId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetSeasonByIdQuery(seasonId), cancellationToken);
        if (result is null)
        {
            return NotFound(ApiErrorResponse.FromCode(messages, MessageCodes.SeasonNotFound));
        }

        SetETag(result.Versao);
        return Ok(result);
    }

    [HttpPatch("{seasonId:guid}")]
    [Authorize(Policy = AuthPermissions.CanManageSeasons)]
    [RequireIdempotency(RecursoCompetitivoTipo.Season)]
    [ProducesResponseType(typeof(SeasonDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SeasonDetailDto>> Update(
        Guid seasonId,
        [FromBody] UpdateSeasonRequestDto request,
        CancellationToken cancellationToken)
    {
        var expectedVersion = ReadIfMatch();
        var season = await sender.Send(
            new UpdateSeasonCommand(seasonId, request, expectedVersion),
            cancellationToken);
        var result = await ToDetailAsync(season, cancellationToken);
        SetETag(result.Versao);
        return Ok(result);
    }

    [HttpPost("{seasonId:guid}/aberturas")]
    [Authorize(Policy = AuthPermissions.CanManageSeasons)]
    [RequireIdempotency(RecursoCompetitivoTipo.Season)]
    [ProducesResponseType(typeof(SeasonDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SeasonDetailDto>> Activate(
        Guid seasonId,
        CancellationToken cancellationToken)
    {
        var expectedCalendarVersion = ReadIfMatchCalendar();
        var season = await sender.Send(
            new AtivarSeasonCommand(seasonId, expectedCalendarVersion),
            cancellationToken);
        var result = await ToDetailAsync(season, cancellationToken);
        SetETag(expectedCalendarVersion + 1);
        return Ok(result);
    }

    [HttpPost("{seasonId:guid}/encerramentos")]
    [Authorize(Policy = AuthPermissions.CanManageSeasons)]
    [RequireIdempotency(RecursoCompetitivoTipo.Season)]
    [ProducesResponseType(typeof(SeasonDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SeasonDetailDto>> Close(
        Guid seasonId,
        CancellationToken cancellationToken)
    {
        var expectedCalendarVersion = ReadIfMatchCalendar();
        var season = await sender.Send(
            new EncerrarSeasonCommand(seasonId, expectedCalendarVersion),
            cancellationToken);
        var result = await ToDetailAsync(season, cancellationToken);
        SetETag(expectedCalendarVersion + 1);
        return Ok(result);
    }

    [HttpGet("{seasonId:guid}/competicoes")]
    [ProducesResponseType(typeof(CompetitionPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CompetitionPageDto>> GetCompetitions(
        Guid seasonId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetSeasonCompeticoesQuery(seasonId, page, pageSize),
            cancellationToken);
        return result is null
            ? NotFound(ApiErrorResponse.FromCode(messages, MessageCodes.SeasonNotFound))
            : Ok(result);
    }

    [HttpPost("{seasonId:guid}/competicoes")]
    [Authorize(Policy = AuthPermissions.CanManageCompetitions)]
    [RequireIdempotency(RecursoCompetitivoTipo.Competicao)]
    [ProducesResponseType(typeof(CompetitionDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CompetitionDetailDto>> CreateCompetition(
        Guid seasonId,
        [FromBody] CreateCompetitionRequestDto request,
        CancellationToken cancellationToken)
    {
        var competition = await sender.Send(
            new CreateCompetitionCommand(seasonId, request),
            cancellationToken);
        var actions = await CompetitiveApiActions.GetCompetitionActionsAsync(authorization, cancellationToken);
        var result = CompetitionDetailDto.FromEntity(competition, actions);
        SetETag(result.Versao);
        return CreatedAtAction(
            nameof(CompeticoesController.GetById),
            "Competicoes",
            new { competitionId = result.Id },
            result);
    }

    [HttpPost("{seasonId:guid}/regras-publicadas")]
    [Authorize(Policy = AuthPermissions.CanManageCompetitions)]
    [RequireIdempotency(RecursoCompetitivoTipo.VersaoRegras)]
    [ProducesResponseType(typeof(RulesVersionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RulesVersionDto>> PublishRules(
        Guid seasonId,
        [FromBody] PublishRulesRequestDto request,
        CancellationToken cancellationToken)
    {
        var expectedVersion = ReadIfMatch();
        var rules = await sender.Send(
            new PublishSeasonRulesCommand(seasonId, request, expectedVersion),
            cancellationToken);
        SetETag(expectedVersion + 1);
        var result = RulesVersionDto.FromEntity(rules);
        return Created($"/api/v1/temporadas/{seasonId}/regras-publicadas/{result.Id}", result);
    }

    private long ReadIfMatch() => CompetitiveHttpPreconditions.ReadWeakVersion(Request.Headers.IfMatch.ToString());

    private long ReadIfMatchCalendar() => CompetitiveHttpPreconditions.ReadWeakVersion(
        Request.Headers["If-Match-Calendar"].ToString());

    private void SetETag(long version) => Response.Headers.ETag = CompetitiveHttpPreconditions.FormatWeak(version);

    private Task<SeasonDetailDto> ToDetailAsync(Season season, CancellationToken cancellationToken) =>
        sender.Send(new GetSeasonMutationDetailQuery(season), cancellationToken);

    private static SeasonEstado? ParseSeasonState(string? value)
    {
        if (value is null)
        {
            return null;
        }

        if (!Enum.GetNames<SeasonEstado>().Contains(value, StringComparer.Ordinal)
            || !Enum.TryParse<SeasonEstado>(value, ignoreCase: false, out var state))
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        return state;
    }
}

internal static class CompetitiveHttpPreconditions
{
    internal static long ReadWeakVersion(string value)
    {
        const string prefix = "W/\"";
        if (!value.StartsWith(prefix, StringComparison.Ordinal)
            || !value.EndsWith('"')
            || value.Length <= prefix.Length + 1
            || !long.TryParse(
                value.AsSpan(prefix.Length, value.Length - prefix.Length - 1),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var version)
            || version < 0)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        return version;
    }

    internal static string FormatWeak(long version) => $"W/\"{version.ToString(CultureInfo.InvariantCulture)}\"";
}
