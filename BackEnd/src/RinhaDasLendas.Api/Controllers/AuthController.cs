using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RinhaDasLendas.Api.Filters;
using RinhaDasLendas.Application.Commands.Auth;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Queries.Auth;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Infrastructure.Identity;

namespace RinhaDasLendas.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public sealed class AuthController(ISender sender, IOptions<AuthOptions> options, IMessageProvider messages) : ControllerBase
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
            return Unauthorized(ApiErrorResponse.FromCode(messages, MessageCodes.SessionExpired));
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
        Response.Cookies.Delete(options.Value.Cookie.RefreshTokenName, new Microsoft.AspNetCore.Http.CookieOptions { Path = "/api/v1/auth" });
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
        return user is null ? NotFound(ApiErrorResponse.FromCode(messages, MessageCodes.UserNotFound)) : Ok(user);
    }

    [HttpPut("me/profile")]
    [Authorize]
    [ProducesResponseType(typeof(AuthenticatedUserDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateOwnProfileRequestDto request, CancellationToken cancellationToken)
    {
        var user = await sender.Send(new UpdateOwnProfileCommand(CurrentUserId(), request), cancellationToken);
        return user is null ? NotFound(ApiErrorResponse.FromCode(messages, MessageCodes.UserNotFound)) : Ok(user);
    }

    [HttpGet("me/jogador")]
    [Authorize]
    [ProducesResponseType(typeof(JogadorResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MeuJogador(CancellationToken cancellationToken)
    {
        var jogador = await sender.Send(new GetMeuJogadorProfileQuery(CurrentUserId()), cancellationToken);
        return jogador is null ? NotFound(ApiErrorResponse.FromCode(messages, MessageCodes.PlayerProfileNotFound)) : Ok(jogador);
    }

    [HttpPost("me/jogador")]
    [Authorize]
    [ProducesResponseType(typeof(JogadorResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CompleteMeuJogador([FromBody] MeuJogadorProfileRequestDto request, CancellationToken cancellationToken)
    {
        var jogador = await sender.Send(new CompleteMeuJogadorProfileCommand(CurrentUserId(), request), cancellationToken);
        return jogador is null
            ? Conflict(ApiErrorResponse.FromCode(messages, MessageCodes.PlayerAlreadyExists))
            : CreatedAtAction(nameof(MeuJogador), jogador);
    }

    [HttpPut("me/jogador")]
    [Authorize]
    [ProducesResponseType(typeof(JogadorResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMeuJogador([FromBody] MeuJogadorProfileRequestDto request, CancellationToken cancellationToken)
    {
        var jogador = await sender.Send(new UpdateMeuJogadorProfileCommand(CurrentUserId(), request), cancellationToken);
        return jogador is null ? NotFound(ApiErrorResponse.FromCode(messages, MessageCodes.PlayerProfileNotFound)) : Ok(jogador);
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

    [HttpGet("discord/login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> StartDiscordLogin(CancellationToken cancellationToken)
    {
        var response = await sender.Send(new StartDiscordLoginCommand(), cancellationToken);
        return Redirect(response.AuthorizationUrl);
    }

    [HttpGet("discord/callback")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> DiscordCallback([FromQuery] string? code, [FromQuery] string? state, CancellationToken cancellationToken)
    {
        var response = await sender.Send(new HandleDiscordCallbackCommand(code, state, IpAddress(), UserAgent()), cancellationToken);
        SetRefreshCookie(response.AuthResponse?.RefreshToken);
        return Redirect(response.RedirectUrl);
    }

    [HttpPost("me/discord/link")]
    [Authorize]
    [ProducesResponseType(typeof(ExternalAuthStartDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> StartDiscordLink(CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new StartDiscordLinkCommand(CurrentUserId()), cancellationToken));
    }

    [HttpDelete("me/discord")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UnlinkDiscord(CancellationToken cancellationToken)
    {
        await sender.Send(new UnlinkDiscordCommand(CurrentUserId()), cancellationToken);
        return NoContent();
    }

    private void SetRefreshCookie(string? refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        Response.Cookies.Append(options.Value.Cookie.RefreshTokenName, refreshToken, new Microsoft.AspNetCore.Http.CookieOptions
        {
            HttpOnly = true,
            Path = "/api/v1/auth",
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(options.Value.Jwt.RefreshTokenDays),
        });
    }

    private Guid CurrentUserId()
    {
        return CurrentUserIdOrNull() ?? throw new UnauthorizedAccessException(MessageCodes.UnauthorizedAccess);
    }

    private Guid? CurrentUserIdOrNull()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private string? IpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? UserAgent() => Request.Headers.UserAgent.ToString();
}
