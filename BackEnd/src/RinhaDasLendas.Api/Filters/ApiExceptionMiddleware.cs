using System.Net;
using FluentValidation;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Api.Filters;

public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger, IMessageProvider messages)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException exception)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await context.Response.WriteAsJsonAsync(new ApiErrorResponse(
                messages.GetMessage(MessageCodes.ValidationError),
                exception.Errors.Select(error => messages.GetMessage(error.ErrorMessage)).Distinct().ToArray()));
        }
        catch (DomainException exception)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await context.Response.WriteAsJsonAsync(new ApiErrorResponse(messages.GetMessage(MessageCodes.ValidationError), [messages.GetMessage(exception.Message)]));
        }
        catch (UnauthorizedAccessException exception)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await context.Response.WriteAsJsonAsync(new ApiErrorResponse(messages.GetMessage(exception.Message), []));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Erro inesperado ao processar requisicao.");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await context.Response.WriteAsJsonAsync(new ApiErrorResponse(messages.GetMessage(MessageCodes.UnexpectedError), []));
        }
    }
}

public sealed record ApiErrorResponse(string Message, IReadOnlyCollection<string> Errors);
