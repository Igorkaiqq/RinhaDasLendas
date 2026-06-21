using System.Net;
using FluentValidation;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Api.Filters;

public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
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
                "Erro de validacao",
                exception.Errors.Select(error => error.ErrorMessage).Distinct().ToArray()));
        }
        catch (DomainException exception)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await context.Response.WriteAsJsonAsync(new ApiErrorResponse("Erro de validacao", [exception.Message]));
        }
        catch (UnauthorizedAccessException exception)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await context.Response.WriteAsJsonAsync(new ApiErrorResponse(exception.Message, []));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Erro inesperado ao processar requisicao.");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await context.Response.WriteAsJsonAsync(new ApiErrorResponse("Erro interno", []));
        }
    }
}

public sealed record ApiErrorResponse(string Message, IReadOnlyCollection<string> Errors);
