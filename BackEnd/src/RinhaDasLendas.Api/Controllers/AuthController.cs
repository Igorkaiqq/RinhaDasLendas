using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RinhaDasLendas.Api.Filters;
using RinhaDasLendas.Application.Commands.Auth;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Queries.Auth;
using RinhaDasLendas.Infrastructure.Identity;

namespace RinhaDasLendas.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public sealed class AuthController(ISender sender, IOptions<AuthOptions> options) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthenticatedUserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request, CancellationToken cancellationToken)
    {
        var user = await sender.Send(new RegisterUserCommand(request), cancellationToken);
        return CreatedAtAction(nameof(Me), user);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var response = await sender.Send(new LoginCommand(request, IpAddress(), UserAgent()), cancellationToken);
        SetRefreshCookie(response.RefreshToken);
        return Ok(response);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[options.Value.Cookie.RefreshTokenName];
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Unauthorized(new ApiErrorResponse("Sessao expirada", []));
        }

        var response = await sender.Send(new RefreshSessionCommand(refreshToken, IpAddress(), UserAgent()), cancellationToken);
        SetRefreshCookie(response.RefreshToken);
        return Ok(response);
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[options.Value.Cookie.RefreshTokenName];
        await sender.Send(new LogoutCommand(refreshToken, CurrentUserIdOrNull(), IpAddress(), UserAgent()), cancellationToken);
        Response.Cookies.Delete(options.Value.Cookie.RefreshTokenName);
        return NoContent();
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ForgotPasswordResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new ForgotPasswordCommand(request), cancellationToken));
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request, CancellationToken cancellationToken)
    {
        await sender.Send(new ResetPasswordCommand(request), cancellationToken);
        return NoContent();
    }

    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request, CancellationToken cancellationToken)
    {
        await sender.Send(new ChangePasswordCommand(CurrentUserId(), request), cancellationToken);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(AuthenticatedUserDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var user = await sender.Send(new GetCurrentUserQuery(CurrentUserId()), cancellationToken);
        return user is null ? NotFound(new ApiErrorResponse("Usuario nao encontrado", [])) : Ok(user);
    }

    [HttpPut("me/profile")]
    [Authorize]
    [ProducesResponseType(typeof(AuthenticatedUserDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateOwnProfileRequestDto request, CancellationToken cancellationToken)
    {
        var user = await sender.Send(new UpdateOwnProfileCommand(CurrentUserId(), request), cancellationToken);
        return user is null ? NotFound(new ApiErrorResponse("Usuario nao encontrado", [])) : Ok(user);
    }

    [HttpGet("me/permissions")]
    [Authorize]
    [ProducesResponseType(typeof(UserPermissionsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Permissions(CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new GetCurrentUserPermissionsQuery(CurrentUserId()), cancellationToken));
    }

    [HttpGet("me/discord")]
    [Authorize]
    [ProducesResponseType(typeof(DiscordLinkStatusDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Discord(CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new GetDiscordLinkStatusQuery(CurrentUserId()), cancellationToken));
    }

    [HttpPost("me/discord/link")]
    [Authorize]
    public IActionResult StartDiscordLink() => StatusCode(StatusCodes.Status501NotImplemented, new ApiErrorResponse("Discord OAuth ainda nao implementado", []));

    private void SetRefreshCookie(string? refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        Response.Cookies.Append(options.Value.Cookie.RefreshTokenName, refreshToken, new Microsoft.AspNetCore.Http.CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(options.Value.Jwt.RefreshTokenDays),
        });
    }

    private Guid CurrentUserId()
    {
        return CurrentUserIdOrNull() ?? throw new UnauthorizedAccessException("Usuario nao autenticado.");
    }

    private Guid? CurrentUserIdOrNull()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private string? IpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? UserAgent() => Request.Headers.UserAgent.ToString();
}
