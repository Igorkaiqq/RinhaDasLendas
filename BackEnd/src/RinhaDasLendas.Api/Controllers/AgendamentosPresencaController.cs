using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RinhaDasLendas.Api.Filters;
using RinhaDasLendas.Application.Commands.AgendamentosPresenca;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Queries.AgendamentosPresenca;
using RinhaDasLendas.Domain.Constants;

namespace RinhaDasLendas.Api.Controllers;

[ApiController]
[Route("api/v1/discord/agendamentos-presenca")]
[Authorize(Policy = AuthPermissions.CanManageDrafts)]
[Produces("application/json")]
public sealed class AgendamentosPresencaController(ISender sender, IMessageProvider messages) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponseDto<AgendamentoPresencaSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!ValidPagination(page, pageSize))
        {
            return BadRequest(ApiErrorResponse.FromCode(messages, MessageCodes.ValidationError));
        }

        return Ok(await sender.Send(new ListAgendamentosPresencaQuery(page, pageSize), cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType(typeof(AgendamentoPresencaSummaryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] SaveAgendamentoPresencaRequestDto request,
        CancellationToken cancellationToken)
    {
        var agenda = await sender.Send(
            new CreateAgendamentoPresencaCommand(request, CurrentUserId()),
            cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = agenda.Id }, agenda);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AgendamentoPresencaSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var agenda = await sender.Send(new GetAgendamentoPresencaQuery(id), cancellationToken);
        return agenda is null
            ? NotFound(ApiErrorResponse.FromCode(messages, MessageCodes.PresenceScheduleNotFound))
            : Ok(agenda);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(AgendamentoPresencaSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] SaveAgendamentoPresencaRequestDto request,
        CancellationToken cancellationToken)
    {
        var agenda = await sender.Send(
            new UpdateAgendamentoPresencaCommand(id, request, CurrentUserId()),
            cancellationToken);
        return agenda is null
            ? NotFound(ApiErrorResponse.FromCode(messages, MessageCodes.PresenceScheduleNotFound))
            : Ok(agenda);
    }

    [HttpPost("{id:guid}/pausar")]
    [ProducesResponseType(typeof(AgendamentoPresencaSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Pause([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var agenda = await sender.Send(new PausarAgendamentoPresencaCommand(id, CurrentUserId()), cancellationToken);
        return agenda is null
            ? NotFound(ApiErrorResponse.FromCode(messages, MessageCodes.PresenceScheduleNotFound))
            : Ok(agenda);
    }

    [HttpPost("{id:guid}/reativar")]
    [ProducesResponseType(typeof(AgendamentoPresencaSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reactivate([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var agenda = await sender.Send(new ReativarAgendamentoPresencaCommand(id, CurrentUserId()), cancellationToken);
        return agenda is null
            ? NotFound(ApiErrorResponse.FromCode(messages, MessageCodes.PresenceScheduleNotFound))
            : Ok(agenda);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Archive([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var archived = await sender.Send(new ArquivarAgendamentoPresencaCommand(id, CurrentUserId()), cancellationToken);
        return archived
            ? NoContent()
            : NotFound(ApiErrorResponse.FromCode(messages, MessageCodes.PresenceScheduleNotFound));
    }

    [HttpGet("{id:guid}/ocorrencias")]
    [ProducesResponseType(typeof(PaginatedResponseDto<OcorrenciaAgendamentoPresencaSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListOccurrences(
        [FromRoute] Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!ValidPagination(page, pageSize))
        {
            return BadRequest(ApiErrorResponse.FromCode(messages, MessageCodes.ValidationError));
        }

        var occurrences = await sender.Send(
            new ListOcorrenciasAgendamentoPresencaQuery(id, page, pageSize),
            cancellationToken);
        return occurrences is null
            ? NotFound(ApiErrorResponse.FromCode(messages, MessageCodes.PresenceScheduleNotFound))
            : Ok(occurrences);
    }

    private Guid CurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id)
            ? id
            : throw new UnauthorizedAccessException(MessageCodes.UnauthorizedAccess);
    }

    private static bool ValidPagination(int page, int pageSize) => page >= 1 && pageSize is >= 1 and <= 100;
}
