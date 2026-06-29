using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using RinhaDasLendas.Api.Filters;
using RinhaDasLendas.Api.Services;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Constants;

namespace RinhaDasLendas.Api.Controllers;

[ApiController]
[Route("api/v1/discord")]
[Produces("application/json")]
public sealed class DiscordController(IDiscordConfigurationService configurationService, IMessageProvider messages) : ControllerBase
{
    [HttpGet("configuracoes")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme + "," + BotInternalAuthOptions.SchemeName)]
    [ProducesResponseType(typeof(DiscordConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConfiguration(CancellationToken cancellationToken)
    {
        var configuration = await configurationService.GetAsync(cancellationToken);
        return configuration is null
            ? NotFound(new ApiErrorResponse(messages.GetMessage(MessageCodes.DiscordConfigurationNotFound), []))
            : Ok(configuration);
    }

    [HttpPut("configuracoes")]
    [Authorize(Policy = AuthPermissions.CanManageUsers)]
    [ProducesResponseType(typeof(DiscordConfigurationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateConfiguration([FromBody] DiscordConfigurationDto request, CancellationToken cancellationToken)
    {
        return Ok(await configurationService.SaveAsync(request, cancellationToken));
    }
}
