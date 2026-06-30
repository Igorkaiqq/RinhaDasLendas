using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using RinhaDasLendas.Api.Filters;
using RinhaDasLendas.Api.Services;
using RinhaDasLendas.Application.Commands.DraftMontagens;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Queries.DraftMontagens;
using RinhaDasLendas.Domain.Constants;

namespace RinhaDasLendas.Api.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme + "," + BotInternalAuthOptions.SchemeName)]
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
        return montagem is null ? NotFound(ApiErrorResponse.FromCode(messages, MessageCodes.DraftMontagemNotFound)) : Ok(montagem);
    }

    [HttpGet("ativos")]
    [Authorize(Policy = AuthPermissions.CanUseDiscordBotApi)]
    [ProducesResponseType(typeof(IReadOnlyCollection<DraftMontagemResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ActiveForDiscord(CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new GetActiveDraftMontagensForDiscordQuery(), cancellationToken));
    }

    [HttpGet("{id:guid}/realtime-state")]
    [ProducesResponseType(typeof(DraftMontagemRealtimeStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRealtimeState([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var state = await sender.Send(new GetDraftMontagemRealtimeStateQuery(id), cancellationToken);
        return state is null ? NotFound(ApiErrorResponse.FromCode(messages, MessageCodes.DraftMontagemNotFound)) : Ok(state);
    }

    [HttpPost]
    [Authorize(Policy = AuthPermissions.CanManageDraftsOrUseDiscordBotApi)]
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
        return state is null ? NotFound(ApiErrorResponse.FromCode(messages, MessageCodes.DraftMontagemNotFound)) : Ok(state);
    }

    [HttpPost("{id:guid}/presencas/confirmar")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(DraftMontagemResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmPresence([FromRoute] Guid id, [FromBody] ConfirmarPresencaDraftMontagemRequestDto request, CancellationToken cancellationToken)
    {
        var montagem = await sender.Send(new ConfirmarPresencaDraftMontagemCommand(id, request), cancellationToken);
        return montagem is null ? NotFound(ApiErrorResponse.FromCode(messages, MessageCodes.DraftMontagemNotFound)) : Ok(montagem);
    }

    [HttpPost("{id:guid}/presencas/cancelar")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(DraftMontagemResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelPresence([FromRoute] Guid id, [FromBody] CancelarPresencaDraftMontagemRequestDto request, CancellationToken cancellationToken)
    {
        var montagem = await sender.Send(new CancelarPresencaDraftMontagemCommand(id, request), cancellationToken);
        return montagem is null ? NotFound(ApiErrorResponse.FromCode(messages, MessageCodes.DraftMontagemNotFound)) : Ok(montagem);
    }

    [HttpPost("{id:guid}/discord/presencas/confirmar")]
    [Authorize(Policy = AuthPermissions.CanUseDiscordBotApi)]
    [ProducesResponseType(typeof(DraftMontagemResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmDiscordPresence([FromRoute] Guid id, [FromBody] ConfirmarPresencaDraftMontagemRequestDto request, CancellationToken cancellationToken)
    {
        var montagem = await sender.Send(new ConfirmarPresencaDraftMontagemCommand(id, request), cancellationToken);
        return montagem is null ? NotFound(ApiErrorResponse.FromCode(messages, MessageCodes.DraftMontagemNotFound)) : Ok(montagem);
    }

    [HttpPost("{id:guid}/discord/presencas/cancelar")]
    [Authorize(Policy = AuthPermissions.CanUseDiscordBotApi)]
    [ProducesResponseType(typeof(DraftMontagemResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelDiscordPresence([FromRoute] Guid id, [FromBody] CancelarPresencaDraftMontagemRequestDto request, CancellationToken cancellationToken)
    {
        var montagem = await sender.Send(new CancelarPresencaDraftMontagemCommand(id, request), cancellationToken);
        return montagem is null ? NotFound(ApiErrorResponse.FromCode(messages, MessageCodes.DraftMontagemNotFound)) : Ok(montagem);
    }

    [HttpPost("{id:guid}/encerrar-presenca")]
    [Authorize(Policy = AuthPermissions.CanManageDraftsOrUseDiscordBotApi)]
    [ProducesResponseType(typeof(DraftMontagemResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ClosePresence([FromRoute] Guid id, [FromBody] EncerrarPresencaDraftMontagemRequestDto request, CancellationToken cancellationToken)
    {
        var montagem = await sender.Send(new EncerrarPresencaDraftMontagemCommand(id, request), cancellationToken);
        return montagem is null ? NotFound(ApiErrorResponse.FromCode(messages, MessageCodes.DraftMontagemNotFound)) : Ok(montagem);
    }

    [HttpPost("{id:guid}/capitaes")]
    [Authorize(Policy = AuthPermissions.CanManageDraftsOrUseDiscordBotApi)]
    [ProducesResponseType(typeof(DraftMontagemResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DefineCaptains([FromRoute] Guid id, [FromBody] DefinirCapitaesDraftMontagemRequestDto request, CancellationToken cancellationToken)
    {
        var montagem = await sender.Send(new DefinirCapitaesDraftMontagemCommand(id, request), cancellationToken);
        return montagem is null ? NotFound(ApiErrorResponse.FromCode(messages, MessageCodes.DraftMontagemNotFound)) : Ok(montagem);
    }

    [HttpPost("{id:guid}/ordem-escolha")]
    [Authorize(Policy = AuthPermissions.CanManageDraftsOrUseDiscordBotApi)]
    [ProducesResponseType(typeof(DraftMontagemResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DefinePickOrder([FromRoute] Guid id, [FromBody] DefinirOrdemEscolhaDraftMontagemRequestDto request, CancellationToken cancellationToken)
    {
        var montagem = await sender.Send(new DefinirOrdemEscolhaDraftMontagemCommand(id, request), cancellationToken);
        return montagem is null ? NotFound(ApiErrorResponse.FromCode(messages, MessageCodes.DraftMontagemNotFound)) : Ok(montagem);
    }

    [HttpPost("{id:guid}/discord/publicacao")]
    [Authorize(Policy = AuthPermissions.CanUseDiscordBotApi)]
    [ProducesResponseType(typeof(DraftMontagemResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RegisterDiscordPublication([FromRoute] Guid id, [FromBody] RegistrarPublicacaoDiscordDraftMontagemRequestDto request, CancellationToken cancellationToken)
    {
        var montagem = await sender.Send(new RegistrarPublicacaoDiscordDraftMontagemCommand(id, request), cancellationToken);
        return montagem is null ? NotFound(ApiErrorResponse.FromCode(messages, MessageCodes.DraftMontagemNotFound)) : Ok(montagem);
    }

    [HttpPost("{id:guid}/picks")]
    [ProducesResponseType(typeof(DraftMontagemRealtimeStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Pick([FromRoute] Guid id, [FromBody] RegistrarPickDraftMontagemRequestDto request, CancellationToken cancellationToken)
    {
        var state = await sender.Send(new RegistrarPickDraftMontagemCommand(id, request), cancellationToken);
        return state is null ? NotFound(ApiErrorResponse.FromCode(messages, MessageCodes.DraftMontagemNotFound)) : Ok(state);
    }

    [HttpPost("{id:guid}/reservas/substituir")]
    [Authorize(Policy = AuthPermissions.CanManageDrafts)]
    [ProducesResponseType(typeof(DraftMontagemRealtimeStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubstituteReserve([FromRoute] Guid id, [FromBody] SubstituirReservaDraftMontagemRequestDto request, CancellationToken cancellationToken)
    {
        var state = await sender.Send(new SubstituirReservaDraftMontagemCommand(id, request), cancellationToken);
        return state is null ? NotFound(ApiErrorResponse.FromCode(messages, MessageCodes.DraftMontagemNotFound)) : Ok(state);
    }

    [HttpPut("{id:guid}/layout")]
    [Authorize(Policy = AuthPermissions.CanManageDrafts)]
    [ProducesResponseType(typeof(DraftMontagemResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SaveLayout([FromRoute] Guid id, [FromBody] SalvarLayoutDraftMontagemRequestDto request, CancellationToken cancellationToken)
    {
        var montagem = await sender.Send(new SalvarLayoutDraftMontagemCommand(id, request), cancellationToken);
        return montagem is null ? NotFound(ApiErrorResponse.FromCode(messages, MessageCodes.DraftMontagemNotFound)) : Ok(montagem);
    }

    [HttpPost("{id:guid}/capitaes/sortear")]
    [Authorize(Policy = AuthPermissions.CanManageDrafts)]
    [ProducesResponseType(typeof(DraftMontagemResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DrawCaptains([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var montagem = await sender.Send(new SortearCapitaesDraftMontagemCommand(id), cancellationToken);
        return montagem is null ? NotFound(ApiErrorResponse.FromCode(messages, MessageCodes.DraftMontagemNotFound)) : Ok(montagem);
    }

    [HttpPatch("{id:guid}/finalizar")]
    [Authorize(Policy = AuthPermissions.CanManageDrafts)]
    [ProducesResponseType(typeof(DraftMontagemResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Finalize([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var montagem = await sender.Send(new FinalizarDraftMontagemCommand(id), cancellationToken);
        return montagem is null ? NotFound(ApiErrorResponse.FromCode(messages, MessageCodes.DraftMontagemNotFound)) : Ok(montagem);
    }

    [HttpPatch("{id:guid}/cancelar")]
    [Authorize(Policy = AuthPermissions.CanManageDrafts)]
    [ProducesResponseType(typeof(DraftMontagemResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel([FromRoute] Guid id, [FromBody] CancelarDraftMontagemRequestDto request, CancellationToken cancellationToken)
    {
        var montagem = await sender.Send(new CancelarDraftMontagemCommand(id, request), cancellationToken);
        return montagem is null ? NotFound(ApiErrorResponse.FromCode(messages, MessageCodes.DraftMontagemNotFound)) : Ok(montagem);
    }
}
