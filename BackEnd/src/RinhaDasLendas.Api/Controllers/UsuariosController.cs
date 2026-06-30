using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using RinhaDasLendas.Api.Filters;
using RinhaDasLendas.Api.Services;
using RinhaDasLendas.Application.Commands.Usuarios;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Queries.Usuarios;
using RinhaDasLendas.Domain.Constants;

namespace RinhaDasLendas.Api.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme + "," + BotInternalAuthOptions.SchemeName)]
[Route("api/v1/usuarios")]
[Produces("application/json")]
public sealed class UsuariosController(ISender sender, IMessageProvider messages, IDiscordIdentityLookupService discordIdentityLookup) : ControllerBase
{
    [HttpGet("discord/{discordUserId}/vinculo")]
    [Authorize(Policy = AuthPermissions.CanUseDiscordBotApi)]
    [ProducesResponseType(typeof(DiscordUserLinkDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDiscordLink([FromRoute] string discordUserId, CancellationToken cancellationToken)
    {
        return Ok(await discordIdentityLookup.GetByDiscordUserIdAsync(discordUserId, cancellationToken));
    }

    [HttpGet]
    [Authorize(Policy = AuthPermissions.CanViewUsers)]
    [ProducesResponseType(typeof(PaginatedResponseDto<UsuarioResumoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] string? search, [FromQuery] string? nome, [FromQuery] string? email, [FromQuery] string? role, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        return Ok(await sender.Send(new GetUsuariosQuery(search, nome, email, role, status, page, pageSize), cancellationToken));
    }

    [HttpGet("roles")]
    [Authorize(Policy = AuthPermissions.CanViewUsers)]
    [ProducesResponseType(typeof(RoleListResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Roles(CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new GetAssignableRolesQuery(), cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthPermissions.CanViewUsers)]
    [ProducesResponseType(typeof(UsuarioResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var user = await sender.Send(new GetUsuarioByIdQuery(id), cancellationToken);
        return user is null ? NotFound(ApiErrorResponse.FromCode(messages, MessageCodes.UserNotFound)) : Ok(user);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthPermissions.CanManageUsers)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateUsuarioRequestDto request, CancellationToken cancellationToken)
    {
        var user = await sender.Send(new UpdateUsuarioCommand(id, request), cancellationToken);
        return user is null ? NotFound(ApiErrorResponse.FromCode(messages, MessageCodes.UserNotFound)) : Ok(user);
    }

    [HttpPut("{id:guid}/roles")]
    [Authorize(Policy = AuthPermissions.CanManageRoles)]
    public async Task<IActionResult> UpdateRoles([FromRoute] Guid id, [FromBody] UpdateUsuarioRolesRequestDto request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateUsuarioRolesCommand(id, request), cancellationToken);
        return result is null ? NotFound(ApiErrorResponse.FromCode(messages, MessageCodes.UserNotFound)) : Ok(result);
    }

    [HttpPatch("{id:guid}/ativar")]
    [Authorize(Policy = AuthPermissions.CanActivateDeactivateUsers)]
    public async Task<IActionResult> Ativar([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var user = await sender.Send(new AtivarUsuarioCommand(id), cancellationToken);
        return user is null ? NotFound(ApiErrorResponse.FromCode(messages, MessageCodes.UserNotFound)) : Ok(user);
    }

    [HttpPatch("{id:guid}/desativar")]
    [Authorize(Policy = AuthPermissions.CanActivateDeactivateUsers)]
    public async Task<IActionResult> Desativar([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var user = await sender.Send(new DesativarUsuarioCommand(id), cancellationToken);
        return user is null ? NotFound(ApiErrorResponse.FromCode(messages, MessageCodes.UserNotFound)) : Ok(user);
    }

    [HttpPost("{id:guid}/reset-password")]
    [Authorize(Policy = AuthPermissions.CanResetUserPassword)]
    public async Task<IActionResult> ResetPassword([FromRoute] Guid id, [FromBody] ResetUsuarioPasswordRequestDto request, CancellationToken cancellationToken)
    {
        await sender.Send(new ResetUsuarioPasswordCommand(id, request), cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/auditoria")]
    [Authorize(Policy = AuthPermissions.CanViewAdminLogs)]
    public async Task<IActionResult> Auditoria([FromRoute] Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        return Ok(await sender.Send(new GetUsuarioAuditoriaQuery(id, page, pageSize), cancellationToken));
    }
}
