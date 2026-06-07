using MediatR;
using Microsoft.AspNetCore.Mvc;
using RinhaDasLendas.Api.Filters;
using RinhaDasLendas.Application.Commands.Jogadores;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Queries.Jogadores;

namespace RinhaDasLendas.Api.Controllers;

[ApiController]
[Route("api/v1/jogadores")]
[Produces("application/json")]
public sealed class JogadoresController(ISender sender) : ControllerBase
{
    [HttpPost]
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

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(JogadorResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var jogador = await sender.Send(new GetJogadorByIdQuery(id), cancellationToken);
        return jogador is null
            ? NotFound(new ApiErrorResponse("Jogador nao encontrado", []))
            : Ok(jogador);
    }

    [HttpPut("{id:guid}/dados-basicos")]
    [ProducesResponseType(typeof(JogadorResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDadosBasicos([FromRoute] Guid id, [FromBody] JogadorUpdateRequestDto request, CancellationToken cancellationToken)
    {
        var jogador = await sender.Send(new UpdateJogadorCommand(id, request), cancellationToken);
        return jogador is null
            ? NotFound(new ApiErrorResponse("Jogador nao encontrado", []))
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
            ? NotFound(new ApiErrorResponse("Jogador nao encontrado", []))
            : Ok(jogador);
    }

    [HttpPatch("{id:guid}/inativar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Inativar([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var inativado = await sender.Send(new InativarJogadorCommand(id), cancellationToken);
        return inativado
            ? NoContent()
            : NotFound(new ApiErrorResponse("Jogador nao encontrado", []));
    }
}
