using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RinhaDasLendas.Api.Filters;
using RinhaDasLendas.Application.Commands.DraftMontagens;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Queries.DraftMontagens;
using RinhaDasLendas.Domain.Constants;

namespace RinhaDasLendas.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/draft-montagens")]
[Produces("application/json")]
public sealed class DraftMontagensController(ISender sender, IMessageProvider messages) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponseDto<DraftMontagemResumoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] string? search = null, [FromQuery] string? status = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var montagens = await sender.Send(new GetDraftMontagensQuery(search, status, page, pageSize), cancellationToken);
        return Ok(montagens);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DraftMontagemResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var montagem = await sender.Send(new GetDraftMontagemByIdQuery(id), cancellationToken);
        return montagem is null ? NotFound(new ApiErrorResponse(messages.GetMessage(MessageCodes.DraftMontagemNotFound), [])) : Ok(montagem);
    }

    [HttpGet("{id:guid}/realtime-state")]
    [ProducesResponseType(typeof(DraftMontagemRealtimeStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRealtimeState([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var state = await sender.Send(new GetDraftMontagemRealtimeStateQuery(id), cancellationToken);
        return state is null ? NotFound(new ApiErrorResponse(messages.GetMessage(MessageCodes.DraftMontagemNotFound), [])) : Ok(state);
    }

    [HttpPost]
    [Authorize(Policy = AuthPermissions.CanManageDrafts)]
    [ProducesResponseType(typeof(DraftMontagemResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateDraftMontagemRequestDto request, CancellationToken cancellationToken)
    {
        var montagem = await sender.Send(new CreateDraftMontagemCommand(request), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = montagem.Id }, montagem);
    }

    [HttpPost("{id:guid}/iniciar-tempo-real")]
    [Authorize(Policy = AuthPermissions.CanManageDrafts)]
    [ProducesResponseType(typeof(DraftMontagemRealtimeStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartRealtime([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var state = await sender.Send(new IniciarDraftMontagemTempoRealCommand(id), cancellationToken);
        return state is null ? NotFound(new ApiErrorResponse(messages.GetMessage(MessageCodes.DraftMontagemNotFound), [])) : Ok(state);
    }

    [HttpPost("{id:guid}/picks")]
    [ProducesResponseType(typeof(DraftMontagemRealtimeStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Pick([FromRoute] Guid id, [FromBody] RegistrarPickDraftMontagemRequestDto request, CancellationToken cancellationToken)
    {
        var state = await sender.Send(new RegistrarPickDraftMontagemCommand(id, request), cancellationToken);
        return state is null ? NotFound(new ApiErrorResponse(messages.GetMessage(MessageCodes.DraftMontagemNotFound), [])) : Ok(state);
    }

    [HttpPost("{id:guid}/reservas/substituir")]
    [Authorize(Policy = AuthPermissions.CanManageDrafts)]
    [ProducesResponseType(typeof(DraftMontagemRealtimeStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubstituteReserve([FromRoute] Guid id, [FromBody] SubstituirReservaDraftMontagemRequestDto request, CancellationToken cancellationToken)
    {
        var state = await sender.Send(new SubstituirReservaDraftMontagemCommand(id, request), cancellationToken);
        return state is null ? NotFound(new ApiErrorResponse(messages.GetMessage(MessageCodes.DraftMontagemNotFound), [])) : Ok(state);
    }

    [HttpPut("{id:guid}/layout")]
    [Authorize(Policy = AuthPermissions.CanManageDrafts)]
    [ProducesResponseType(typeof(DraftMontagemResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SaveLayout([FromRoute] Guid id, [FromBody] SalvarLayoutDraftMontagemRequestDto request, CancellationToken cancellationToken)
    {
        var montagem = await sender.Send(new SalvarLayoutDraftMontagemCommand(id, request), cancellationToken);
        return montagem is null ? NotFound(new ApiErrorResponse(messages.GetMessage(MessageCodes.DraftMontagemNotFound), [])) : Ok(montagem);
    }

    [HttpPost("{id:guid}/capitaes/sortear")]
    [Authorize(Policy = AuthPermissions.CanManageDrafts)]
    [ProducesResponseType(typeof(DraftMontagemResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DrawCaptains([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var montagem = await sender.Send(new SortearCapitaesDraftMontagemCommand(id), cancellationToken);
        return montagem is null ? NotFound(new ApiErrorResponse(messages.GetMessage(MessageCodes.DraftMontagemNotFound), [])) : Ok(montagem);
    }

    [HttpPatch("{id:guid}/finalizar")]
    [Authorize(Policy = AuthPermissions.CanManageDrafts)]
    [ProducesResponseType(typeof(DraftMontagemResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Finalize([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var montagem = await sender.Send(new FinalizarDraftMontagemCommand(id), cancellationToken);
        return montagem is null ? NotFound(new ApiErrorResponse(messages.GetMessage(MessageCodes.DraftMontagemNotFound), [])) : Ok(montagem);
    }

    [HttpPatch("{id:guid}/cancelar")]
    [Authorize(Policy = AuthPermissions.CanManageDrafts)]
    [ProducesResponseType(typeof(DraftMontagemResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel([FromRoute] Guid id, [FromBody] CancelarDraftMontagemRequestDto request, CancellationToken cancellationToken)
    {
        var montagem = await sender.Send(new CancelarDraftMontagemCommand(id, request), cancellationToken);
        return montagem is null ? NotFound(new ApiErrorResponse(messages.GetMessage(MessageCodes.DraftMontagemNotFound), [])) : Ok(montagem);
    }
}
