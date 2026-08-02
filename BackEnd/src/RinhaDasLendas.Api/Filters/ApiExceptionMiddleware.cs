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
            var fieldErrors = exception.Errors
                .Select(error => new ApiFieldError(
                    error.PropertyName,
                    error.ErrorMessage,
                    messages.GetMessage(error.ErrorMessage)))
                .ToArray();
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await context.Response.WriteAsJsonAsync(new ApiErrorResponse(
                messages.GetMessage(MessageCodes.ValidationError),
                fieldErrors.Select(error => error.Message).Distinct().ToArray(),
                MessageCodes.ValidationError)
            {
                FieldErrors = fieldErrors
            });
        }
        catch (DomainException exception)
        {
            context.Response.StatusCode = exception.MessageCode is MessageCodes.PresenceScheduleOccurrenceConflict
                or MessageCodes.DraftStateConflict
                or MessageCodes.CompetitiveIdempotencyConflict
                or MessageCodes.CompetitiveCalendarVersionStale
                or MessageCodes.CompetitiveResourceVersionStale
                or MessageCodes.CompetitionCodeConflict
                or MessageCodes.RoundOrderConflict
                or MessageCodes.ActiveSeasonConflict
                or MessageCodes.SeasonOrderConflict
                or MessageCodes.SeasonPeriodOverlap
                or MessageCodes.DailyCircuitCompetitionInvalid
                ? (int)HttpStatusCode.Conflict
                : exception.MessageCode == MessageCodes.CompetitiveAccessDenied
                    ? (int)HttpStatusCode.Forbidden
                : exception.MessageCode is MessageCodes.SeasonNotFound
                    or MessageCodes.CompetitionNotFound
                    or MessageCodes.RoundNotFound
                    or MessageCodes.RulesVersionNotFound
                    ? (int)HttpStatusCode.NotFound
                : (int)HttpStatusCode.BadRequest;
            await context.Response.WriteAsJsonAsync(ApiErrorResponse.FromCode(messages, exception.MessageCode));
        }
        catch (UnauthorizedAccessException exception)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await context.Response.WriteAsJsonAsync(new ApiErrorResponse(messages.GetMessage(exception.Message), [], exception.Message));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled request exception.");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await context.Response.WriteAsJsonAsync(new ApiErrorResponse(messages.GetMessage(MessageCodes.UnexpectedError), [], MessageCodes.UnexpectedError));
        }
    }
}
