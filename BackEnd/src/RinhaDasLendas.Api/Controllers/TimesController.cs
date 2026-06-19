using MediatR;
using Microsoft.AspNetCore.Mvc;
using RinhaDasLendas.Api.Filters;
using RinhaDasLendas.Application.Commands.Times;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Queries.Times;

namespace RinhaDasLendas.Api.Controllers;

[ApiController]
[Route("api/v1/times")]
[Produces("application/json")]
public sealed class TimesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponseDto<TimeResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var times = await sender.Send(new GetTimesQuery(search, status, page, pageSize), cancellationToken);
        return Ok(times);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TimeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var time = await sender.Send(new GetTimeByIdQuery(id), cancellationToken);
        return time is null
            ? NotFound(new ApiErrorResponse("Time nao encontrado", []))
            : Ok(time);
    }

    [HttpPost]
    [ProducesResponseType(typeof(TimeResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTimeRequestDto request, CancellationToken cancellationToken)
    {
        var time = await sender.Send(new CreateTimeCommand(request), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = time.Id }, time);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TimeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateTimeRequestDto request, CancellationToken cancellationToken)
    {
        var time = await sender.Send(new UpdateTimeCommand(id, request), cancellationToken);
        return time is null
            ? NotFound(new ApiErrorResponse("Time nao encontrado", []))
            : Ok(time);
    }

    [HttpPatch("{id:guid}/inativar")]
    [ProducesResponseType(typeof(TimeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Inativar([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var time = await sender.Send(new InativarTimeCommand(id), cancellationToken);
        return time is null
            ? NotFound(new ApiErrorResponse("Time nao encontrado", []))
            : Ok(time);
    }

    [HttpPatch("{id:guid}/reativar")]
    [ProducesResponseType(typeof(TimeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reativar([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var time = await sender.Send(new ReativarTimeCommand(id), cancellationToken);
        return time is null
            ? NotFound(new ApiErrorResponse("Time nao encontrado", []))
            : Ok(time);
    }
}
