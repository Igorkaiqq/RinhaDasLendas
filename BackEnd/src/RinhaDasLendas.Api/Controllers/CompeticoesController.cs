using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RinhaDasLendas.Api.Filters;
using RinhaDasLendas.Api.Middleware;
using RinhaDasLendas.Api.Services;
using RinhaDasLendas.Application.Commands.Competicoes;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Queries.Competicoes;
using RinhaDasLendas.Application.Security;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Rules;

namespace RinhaDasLendas.Api.Controllers;

[ApiController]
[Route("api/v1/competicoes")]
[Authorize(Policy = ApiAuthenticationDefaults.AuthenticatedPolicyName)]
[Produces("application/json")]
public sealed class CompeticoesController(
    ISender sender,
    IMessageProvider messages,
    ICompetitiveAuthorizationService authorization) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(CompetitionPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CompetitionPageDto>> GetAll(
        [FromQuery] Guid[]? temporadaIds = null,
        [FromQuery] bool todas = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (temporadaIds is { Length: > 0 }
            && temporadaIds.Distinct().Count() != temporadaIds.Length)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }

        var selection = SelecaoSazonal.Criar(temporadaIds, todas);
        return Ok(await sender.Send(
            new GetCompeticoesQuery(selection, page, pageSize),
            cancellationToken));
    }

    [HttpGet("{competitionId:guid}")]
    [ProducesResponseType(typeof(CompetitionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CompetitionDetailDto>> GetById(
        Guid competitionId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCompeticaoByIdQuery(competitionId), cancellationToken);
        if (result is null)
        {
            return NotFound(ApiErrorResponse.FromCode(messages, MessageCodes.CompetitionNotFound));
        }

        SetETag(result.Versao);
        return Ok(result);
    }

    [HttpPatch("{competitionId:guid}")]
    [Authorize(Policy = AuthPermissions.CanManageCompetitions)]
    [RequireIdempotency(RecursoCompetitivoTipo.Competicao)]
    [ProducesResponseType(typeof(CompetitionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CompetitionDetailDto>> Update(
        Guid competitionId,
        [FromBody] UpdateCompetitionRequestDto request,
        CancellationToken cancellationToken)
    {
        var competition = await sender.Send(
            new UpdateCompetitionCommand(competitionId, request, ReadIfMatch()),
            cancellationToken);
        var actions = await CompetitiveApiActions.GetCompetitionActionsAsync(authorization, cancellationToken);
        var result = CompetitionDetailDto.FromEntity(competition, actions);
        SetETag(result.Versao);
        return Ok(result);
    }

    [HttpGet("{competitionId:guid}/rodadas")]
    [ProducesResponseType(typeof(IReadOnlyCollection<RoundDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<RoundDto>>> GetRounds(
        Guid competitionId,
        CancellationToken cancellationToken)
    {
        var competition = await sender.Send(new GetCompeticaoByIdQuery(competitionId), cancellationToken);
        return competition is null
            ? NotFound(ApiErrorResponse.FromCode(messages, MessageCodes.CompetitionNotFound))
            : Ok(competition.Rodadas);
    }

    [HttpPost("{competitionId:guid}/rodadas")]
    [Authorize(Policy = AuthPermissions.CanManageCompetitions)]
    [RequireIdempotency(RecursoCompetitivoTipo.Rodada)]
    [ProducesResponseType(typeof(RoundDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RoundDto>> CreateRound(
        Guid competitionId,
        [FromBody] CreateRoundRequestDto request,
        CancellationToken cancellationToken)
    {
        var expectedVersion = ReadIfMatch();
        var round = await sender.Send(
            new CreateRoundCommand(competitionId, request, expectedVersion),
            cancellationToken);
        SetETag(expectedVersion + 1);
        var result = RoundDto.FromEntity(round);
        return Created($"/api/v1/competicoes/{competitionId}/rodadas/{result.Id}", result);
    }

    [HttpPost("{competitionId:guid}/ordenacoes-rodadas")]
    [Authorize(Policy = AuthPermissions.CanManageCompetitions)]
    [RequireIdempotency(RecursoCompetitivoTipo.Competicao)]
    [ProducesResponseType(typeof(IReadOnlyCollection<RoundDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IReadOnlyCollection<RoundDto>>> ReorderRounds(
        Guid competitionId,
        [FromBody] ReorderRoundsRequestDto request,
        CancellationToken cancellationToken)
    {
        var expectedVersion = ReadIfMatch();
        var rounds = await sender.Send(
            new ReorderRoundsCommand(competitionId, request, expectedVersion),
            cancellationToken);
        SetETag(expectedVersion + 1);
        return Ok(rounds.Select(RoundDto.FromEntity).ToArray());
    }

    [HttpPost("{competitionId:guid}/regras-publicadas")]
    [Authorize(Policy = AuthPermissions.CanManageCompetitions)]
    [RequireIdempotency(RecursoCompetitivoTipo.VersaoRegras)]
    [ProducesResponseType(typeof(RulesVersionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RulesVersionDto>> PublishRules(
        Guid competitionId,
        [FromBody] PublishRulesRequestDto request,
        CancellationToken cancellationToken)
    {
        var expectedVersion = ReadIfMatch();
        var rules = await sender.Send(
            new PublishCompetitionRulesCommand(competitionId, request, expectedVersion),
            cancellationToken);
        SetETag(expectedVersion + 1);
        var result = RulesVersionDto.FromEntity(rules);
        return Created($"/api/v1/competicoes/{competitionId}/regras-publicadas/{result.Id}", result);
    }

    private long ReadIfMatch() => CompetitiveHttpPreconditions.ReadWeakVersion(Request.Headers.IfMatch.ToString());

    private void SetETag(long version) => Response.Headers.ETag = CompetitiveHttpPreconditions.FormatWeak(version);
}

internal static class CompetitiveApiActions
{
    internal static async Task<IReadOnlyCollection<string>> GetCompetitionActionsAsync(
        ICompetitiveAuthorizationService authorization,
        CancellationToken cancellationToken)
    {
        var candidates = new[]
        {
            (Action: "edit", Context: Context("ConfigurarCompeticao", "Competicao")),
            (Action: "create-round", Context: Context("ConfigurarRodada", "Rodada")),
            (Action: "reorder-rounds", Context: Context("ConfigurarRodada", "Rodada")),
            (Action: "publish-rules", Context: Context("Publicar", "Regra")),
        };
        var allowed = await authorization.GetAllowedActionsAsync(
            candidates.Select(candidate => candidate.Context).ToArray(),
            cancellationToken);
        return candidates
            .Where(candidate => allowed.Contains(candidate.Context.Operation, StringComparer.Ordinal))
            .Select(candidate => candidate.Action)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static CompetitiveAuthorizationContext Context(string operation, string resourceType) =>
        new(
            AuthPermissions.CanManageCompetitions,
            operation,
            resourceType,
            SerieType: null,
            SeasonState: SeasonEstado.Planejada.ToString(),
            HasStartedSeries: false,
            HasRequiredJustification: false);
}
