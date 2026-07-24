using System.Net;
using System.Text.Json;
using FluentAssertions;
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
    }
}
