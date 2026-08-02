using System.Net;
using System.Globalization;
using System.Text.Json;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using RinhaDasLendas.Api.Filters;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Infrastructure.Messages;

namespace RinhaDasLendas.Tests.Filters;

public sealed class ApiExceptionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldMapKnownPresenceScheduleConflictTo409()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ApiExceptionMiddleware(
            _ => throw new DomainException(MessageCodes.PresenceScheduleOccurrenceConflict),
            Mock.Of<ILogger<ApiExceptionMiddleware>>(),
            new ResourceMessageProvider());

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.Conflict);
        context.Response.Body.Position = 0;
        var response = await JsonSerializer.DeserializeAsync<ApiErrorResponse>(
            context.Response.Body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        response!.MessageCode.Should().Be(MessageCodes.PresenceScheduleOccurrenceConflict);
        response.FieldErrors.Should().BeNull();
    }

    [Theory]
    [InlineData(MessageCodes.CompetitionCodeConflict)]
    [InlineData(MessageCodes.RoundOrderConflict)]
    [InlineData(MessageCodes.DailyCircuitCompetitionInvalid)]
    [InlineData(MessageCodes.ActiveSeasonConflict)]
    [InlineData(MessageCodes.SeasonOrderConflict)]
    [InlineData(MessageCodes.SeasonPeriodOverlap)]
    public async Task InvokeAsync_ShouldMapKnownCompetitivePersistenceConflictTo409(string messageCode)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ApiExceptionMiddleware(
            _ => throw new DomainException(messageCode),
            Mock.Of<ILogger<ApiExceptionMiddleware>>(),
            new ResourceMessageProvider());

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.Conflict);
        context.Response.Body.Position = 0;
        var response = await JsonSerializer.DeserializeAsync<ApiErrorResponse>(
            context.Response.Body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        response!.MessageCode.Should().Be(messageCode);
    }

    [Theory]
    [InlineData("pt-BR")]
    [InlineData("en-US")]
    public async Task InvokeAsync_ShouldPreserveFlatErrorsAndExposeLocalizedStructuredFieldErrors(string cultureName)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        var culture = CultureInfo.GetCultureInfo(cultureName);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        try
        {
            var messages = new ResourceMessageProvider();
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            var middleware = new ApiExceptionMiddleware(
                _ => throw new ValidationException([
                    new ValidationFailure("Request.Nome", MessageCodes.SeasonNameRequired)
                ]),
                Mock.Of<ILogger<ApiExceptionMiddleware>>(),
                messages);

            await middleware.InvokeAsync(context);

            context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            context.Response.Body.Position = 0;
            var response = await JsonSerializer.DeserializeAsync<ApiErrorResponse>(
                context.Response.Body,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            var expectedMessage = messages.GetMessage(MessageCodes.SeasonNameRequired, cultureName);
            response!.Errors.Should().Equal(expectedMessage);
            response.FieldErrors.Should().ContainSingle().Which.Should().Be(new ApiFieldError(
                "Request.Nome",
                MessageCodes.SeasonNameRequired,
                expectedMessage));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }
}
