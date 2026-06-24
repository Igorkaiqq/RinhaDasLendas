using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RinhaDasLendas.Api.Filters;
using RinhaDasLendas.Application.Commands.Jogadores;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Queries.Jogadores;
using RinhaDasLendas.Domain.Constants;

namespace RinhaDasLendas.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/jogadores")]
[Produces("application/json")]
public sealed class JogadoresController(ISender sender, IMessageProvider messages) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = AuthPermissions.CanManageUsers)]
    [ProducesResponseType(typeof(JogadorResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] JogadorCreateRequestDto request, CancellationToken cancellationToken)
    {
        var jogador = await sender.Send(new CreateJogadorCommand(request), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = jogador.Id }, jogador);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponseDto<JogadorResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] bool somenteAtivos = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var jogadores = await sender.Send(new GetJogadoresQuery(somenteAtivos, page, pageSize), cancellationToken);
        return Ok(jogadores);
    }

    [HttpGet("capitaes-elegiveis")]
    [ProducesResponseType(typeof(PaginatedResponseDto<JogadorResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CapitaesElegiveis([FromQuery] int page = 1, [FromQuery] int pageSize = 100, CancellationToken cancellationToken = default)
    {
        return Ok(await sender.Send(new GetCapitaesElegiveisQuery(page, pageSize), cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(JogadorResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var jogador = await sender.Send(new GetJogadorByIdQuery(id), cancellationToken);
        return jogador is null
            ? NotFound(new ApiErrorResponse(messages.GetMessage(MessageCodes.PlayerNotFound), []))
            : Ok(jogador);
    }

    [HttpPut("{id:guid}/dados-basicos")]
    [Authorize(Policy = AuthPermissions.CanManageUsers)]
    [ProducesResponseType(typeof(JogadorResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDadosBasicos([FromRoute] Guid id, [FromBody] JogadorUpdateRequestDto request, CancellationToken cancellationToken)
    {
        var jogador = await sender.Send(new UpdateJogadorCommand(id, request), cancellationToken);
        return jogador is null
            ? NotFound(new ApiErrorResponse(messages.GetMessage(MessageCodes.PlayerNotFound), []))
            : Ok(jogador);
    }

    [HttpPut("{id:guid}/preferencias-rotas")]
    [ProducesResponseType(typeof(JogadorResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePreferencias([FromRoute] Guid id, [FromBody] UpdatePreferenciasRotasRequestDto request, CancellationToken cancellationToken)
    {
        var jogador = await sender.Send(new UpdatePreferenciasCommand(id, request), cancellationToken);
        return jogador is null
            ? NotFound(new ApiErrorResponse(messages.GetMessage(MessageCodes.PlayerNotFound), []))
            : Ok(jogador);
    }

    [HttpPatch("{id:guid}/inativar")]
    [Authorize(Policy = AuthPermissions.CanManageUsers)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Inativar([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var inativado = await sender.Send(new InativarJogadorCommand(id), cancellationToken);
        return inativado
            ? NoContent()
            : NotFound(new ApiErrorResponse(messages.GetMessage(MessageCodes.PlayerNotFound), []));
    }
}
