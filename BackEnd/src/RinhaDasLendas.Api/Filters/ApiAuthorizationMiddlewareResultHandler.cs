using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using RinhaDasLendas.Api.Services;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Constants;

namespace RinhaDasLendas.Api.Filters;

public sealed class ApiAuthorizationMiddlewareResultHandler(IMessageProvider messages) : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Succeeded)
        {
            await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
            return;
        }

        var messageCode = authorizeResult.Forbidden
            ? MessageCodes.AccessDenied
            : await ResolveChallengeMessageCodeAsync(context);
        context.Response.StatusCode = authorizeResult.Forbidden
            ? StatusCodes.Status403Forbidden
            : StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(ApiErrorResponse.FromCode(messages, messageCode));
    }

    private static async Task<string> ResolveChallengeMessageCodeAsync(HttpContext context)
    {
        if (!context.Request.Headers.ContainsKey(BotInternalAuthOptions.HeaderName))
        {
            return MessageCodes.AuthenticationFailed;
        }

        var botAuthentication = await context.AuthenticateAsync(BotInternalAuthOptions.SchemeName);
        return botAuthentication.Failure is null
            ? MessageCodes.AuthenticationFailed
            : MessageCodes.BotInternalTokenInvalid;
    }
}
