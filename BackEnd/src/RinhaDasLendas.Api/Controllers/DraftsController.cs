using MediatR;
using Microsoft.AspNetCore.Mvc;
using RinhaDasLendas.Api.Filters;
using RinhaDasLendas.Application.Commands.Drafts;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Queries.Drafts;

namespace RinhaDasLendas.Api.Controllers;

[ApiController]
[Route("api/v1/drafts")]
[Produces("application/json")]
public sealed class DraftsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponseDto<DraftResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var drafts = await sender.Send(new GetDraftsQuery(search, status, page, pageSize), cancellationToken);
        return Ok(drafts);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DraftResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var draft = await sender.Send(new GetDraftByIdQuery(id), cancellationToken);
        return draft is null
            ? NotFound(new ApiErrorResponse("Draft nao encontrado", []))
            : Ok(draft);
    }

    [HttpPost]
    [ProducesResponseType(typeof(DraftResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateDraftRequestDto request, CancellationToken cancellationToken)
    {
        var draft = await sender.Send(new CreateDraftCommand(request), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = draft.Id }, draft);
    }

    [HttpPost("{id:guid}/picks")]
    [ProducesResponseType(typeof(DraftResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Pick([FromRoute] Guid id, [FromBody] RegistrarPickDraftRequestDto request, CancellationToken cancellationToken)
    {
        var draft = await sender.Send(new RegistrarPickDraftCommand(id, request), cancellationToken);
        return draft is null
            ? NotFound(new ApiErrorResponse("Draft nao encontrado", []))
            : Ok(draft);
    }

    [HttpPatch("{id:guid}/cancelar")]
    [ProducesResponseType(typeof(DraftResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancelar([FromRoute] Guid id, [FromBody] CancelarDraftRequestDto request, CancellationToken cancellationToken)
    {
        var draft = await sender.Send(new CancelarDraftCommand(id, request), cancellationToken);
        return draft is null
            ? NotFound(new ApiErrorResponse("Draft nao encontrado", []))
            : Ok(draft);
    }
}
