using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using RinhaDasLendas.Api.Filters;
using RinhaDasLendas.Api.Middleware;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Tests.Api;

public sealed class IdempotencyMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldCanonicalizeJsonAndUseRouteTemplateWithoutQueryValues()
    {
        var requests = new List<IdempotencyRequest>();
        var service = ExecutingService(requests);
        var middleware = Middleware(service.Object);

        await middleware.InvokeAsync(
            Context("/api/v1/temporadas/11111111-1111-1111-1111-111111111111/aberturas", "x=1",
                "{\"nome\":\"Season\",\"ano\":2026}"),
            SuccessNext("{}"));
        await middleware.InvokeAsync(
            Context("/api/v1/temporadas/22222222-2222-2222-2222-222222222222/aberturas", "x=2",
                "{\"ano\":2026,\"nome\":\"Season\"}"),
            SuccessNext("{}"));

        requests.Should().HaveCount(2);
        requests.Select(request => request.RequestHash).Distinct().Should().ContainSingle();
        requests.Should().OnlyContain(request =>
            request.Route == "/api/v1/temporadas/{seasonId}/aberturas"
            && request.Method == "POST");
    }

    [Fact]
    public async Task InvokeAsync_ShouldNotWriteRealResponseUntilServiceReturnsAfterCommit()
    {
        var service = new Mock<IIdempotencyService>();
        service.Setup(item => item.ExecuteAsync(
                It.IsAny<IdempotencyRequest>(),
                It.IsAny<Func<CancellationToken, Task<IdempotencyResult>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<IdempotencyRequest, Func<CancellationToken, Task<IdempotencyResult>>, CancellationToken>(
                async (_, callback, cancellationToken) =>
                {
                    var response = await callback(cancellationToken);
                    response.Content.Should().Equal(Encoding.UTF8.GetBytes("committed"));
                    return new IdempotencyExecutionResult(response, false);
                });
        var context = Context(body: "{}");
        var realBody = (MemoryStream)context.Response.Body;

        await Middleware(service.Object).InvokeAsync(context, async capturedContext =>
        {
            await capturedContext.Response.WriteAsync("committed");
            realBody.Length.Should().Be(0);
        });

        Encoding.UTF8.GetString(realBody.ToArray()).Should().Be("committed");
    }

    [Fact]
    public async Task InvokeAsync_WhenPersistenceFails_ShouldLeaveRealResponseUntouched()
    {
        var service = new Mock<IIdempotencyService>();
        service.Setup(item => item.ExecuteAsync(
                It.IsAny<IdempotencyRequest>(),
                It.IsAny<Func<CancellationToken, Task<IdempotencyResult>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<IdempotencyRequest, Func<CancellationToken, Task<IdempotencyResult>>, CancellationToken>(
                async (_, callback, cancellationToken) =>
                {
                    await callback(cancellationToken);
                    throw new InvalidOperationException("persistence failed");
                });
        var context = Context(body: "{}");

        var act = () => Middleware(service.Object).InvokeAsync(context, SuccessNext("must-not-leak"));

        await act.Should().ThrowAsync<InvalidOperationException>();
        ReadResponse(context).Should().BeEmpty();
        context.Response.HasStarted.Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_ShouldReplayExactBytesStatusAndAllowlistedMetadata()
    {
        var endpointCalls = 0;
        var stored = Response(Encoding.UTF8.GetBytes("{\"id\":\"original\"}"));
        var service = new Mock<IIdempotencyService>();
        service.Setup(item => item.ExecuteAsync(
                It.IsAny<IdempotencyRequest>(),
                It.IsAny<Func<CancellationToken, Task<IdempotencyResult>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdempotencyExecutionResult(stored, true));
        var context = Context(body: "{}");

        await Middleware(service.Object).InvokeAsync(context, _ =>
        {
            endpointCalls++;
            return Task.CompletedTask;
        });

        endpointCalls.Should().Be(0);
        context.Response.StatusCode.Should().Be(stored.StatusCode);
        context.Response.ContentType.Should().Be(stored.Metadata[MetadadoResultadoOperacao.TipoConteudo] as string);
        context.Response.Headers.Location.ToString().Should().Be(stored.Metadata[MetadadoResultadoOperacao.Localizacao] as string);
        context.Response.Headers.ETag.ToString().Should().Be(stored.Metadata[MetadadoResultadoOperacao.VersaoRecurso] as string);
        context.Response.Headers["Idempotency-Replayed"].ToString().Should().Be("true");
        ReadResponse(context).Should().Equal(stored.Content);
    }

    [Fact]
    public async Task InvokeAsync_ShouldAcceptRequestAndChunkedResponseAtExactLimits()
    {
        var service = ExecutingService();
        var middleware = Middleware(service.Object, requestLimit: 2, responseLimit: 6);
        var context = Context(body: "{}");

        await middleware.InvokeAsync(context, async capturedContext =>
        {
            await capturedContext.Response.Body.WriteAsync(Encoding.UTF8.GetBytes("123"));
            await capturedContext.Response.Body.WriteAsync(Encoding.UTF8.GetBytes("456"));
        });

        Encoding.UTF8.GetString(ReadResponse(context)).Should().Be("123456");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task InvokeAsync_ShouldRejectResponseOverLimitBeforePersistenceOrRealWrite(bool contentLengthKnown)
    {
        var service = ExecutingService();
        var context = Context(body: "{}");
        var middleware = Middleware(service.Object, responseLimit: 6);

        var act = () => middleware.InvokeAsync(context, async capturedContext =>
        {
            if (contentLengthKnown)
            {
                capturedContext.Response.ContentLength = 7;
                return;
            }

            await capturedContext.Response.Body.WriteAsync(Encoding.UTF8.GetBytes("123"));
            await capturedContext.Response.Body.WriteAsync(Encoding.UTF8.GetBytes("4567"));
        });

        await act.Should().ThrowAsync<DomainException>();
        ReadResponse(context).Should().BeEmpty();
    }

    [Fact]
    public async Task InvokeAsync_ShouldRejectRequestOverStreamingLimitBeforeService()
    {
        var service = new Mock<IIdempotencyService>(MockBehavior.Strict);
        var context = Context(body: "{} ");

        var act = () => Middleware(service.Object, requestLimit: 2).InvokeAsync(context, SuccessNext("unused"));

        await act.Should().ThrowAsync<DomainException>();
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task InvokeAsync_ShouldBypassEndpointWithoutRequiredMetadata()
    {
        var service = new Mock<IIdempotencyService>(MockBehavior.Strict);
        var context = Context(body: "{}");
        context.SetEndpoint(new RouteEndpoint(
            _ => Task.CompletedTask,
            RoutePatternFactory.Parse("/api/v1/plain"),
            0,
            new EndpointMetadataCollection(),
            "plain"));
        var called = false;

        await Middleware(service.Object).InvokeAsync(context, _ =>
        {
            called = true;
            return Task.CompletedTask;
        });

        called.Should().BeTrue();
        service.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null, "valid-idempotency-key")]
    [InlineData("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", "short")]
    public async Task InvokeAsync_ShouldValidateActorAndKey(string? actor, string key)
    {
        var service = new Mock<IIdempotencyService>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(item => item.UserId).Returns(actor is null ? null : Guid.Parse(actor));
        var context = Context(body: "{}", key: key);
        var middleware = Middleware(service.Object, currentUser.Object);

        var act = () => middleware.InvokeAsync(context, SuccessNext("unused"));

        await act.Should().ThrowAsync<DomainException>();
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ApiExceptionMiddleware_ShouldMapIdempotencyConflictTo409()
    {
        var messages = new Mock<IMessageProvider>();
        messages.Setup(item => item.GetMessage(MessageCodes.CompetitiveIdempotencyConflict))
            .Returns("localized");
        var middleware = new ApiExceptionMiddleware(
            _ => throw new DomainException(MessageCodes.CompetitiveIdempotencyConflict),
            NullLogger<ApiExceptionMiddleware>.Instance,
            messages.Object);
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }

    private static IdempotencyMiddleware Middleware(
        IIdempotencyService service,
        ICurrentUser? currentUser = null,
        int requestLimit = 65_536,
        int responseLimit = 65_536) =>
        new(
            service,
            currentUser ?? CurrentUser(),
            Options.Create(new IdempotencyMiddlewareOptions
            {
                MaxRequestBodyBytes = requestLimit,
                MaxResponseBodyBytes = responseLimit,
            }));

    private static Mock<IIdempotencyService> ExecutingService(List<IdempotencyRequest>? requests = null)
    {
        var service = new Mock<IIdempotencyService>();
        service.Setup(item => item.ExecuteAsync(
                It.IsAny<IdempotencyRequest>(),
                It.IsAny<Func<CancellationToken, Task<IdempotencyResult>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<IdempotencyRequest, Func<CancellationToken, Task<IdempotencyResult>>, CancellationToken>(
                async (request, callback, cancellationToken) =>
                {
                    requests?.Add(request);
                    return new IdempotencyExecutionResult(await callback(cancellationToken), false);
                });
        service.Setup(item => item.CleanupExpiredAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);
        return service;
    }

    private static RequestDelegate SuccessNext(string body) => async context =>
    {
        context.Response.StatusCode = StatusCodes.Status201Created;
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.Headers.Location = "/api/v1/temporadas/original";
        context.Response.Headers.ETag = "W/\"7\"";
        await context.Response.WriteAsync(body);
    };

    private static DefaultHttpContext Context(
        string path = "/api/v1/temporadas/11111111-1111-1111-1111-111111111111/aberturas",
        string? query = null,
        string body = "{}",
        string key = "valid-idempotency-key")
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = path;
        context.Request.QueryString = query is null ? QueryString.Empty : new QueryString($"?{query}");
        context.Request.ContentType = "application/json";
        context.Request.Headers["Idempotency-Key"] = key;
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        context.Response.Body = new MemoryStream();
        context.SetEndpoint(new RouteEndpoint(
            _ => Task.CompletedTask,
            RoutePatternFactory.Parse("/api/v1/temporadas/{seasonId}/aberturas"),
            0,
            new EndpointMetadataCollection(new RequireIdempotencyAttribute(RecursoCompetitivoTipo.Season)),
            "competitive mutation"));
        return context;
    }

    private static ICurrentUser CurrentUser()
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(item => item.UserId)
            .Returns(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        return currentUser.Object;
    }

    private static IdempotencyResult Response(byte[] content) => new(
        StatusCodes.Status201Created,
        RecursoCompetitivoTipo.Season,
        Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
        content,
        new Dictionary<MetadadoResultadoOperacao, object?>
        {
            [MetadadoResultadoOperacao.TipoConteudo] = "application/json; charset=utf-8",
            [MetadadoResultadoOperacao.Localizacao] = "/api/v1/temporadas/original",
            [MetadadoResultadoOperacao.VersaoRecurso] = "W/\"7\"",
        });

    private static byte[] ReadResponse(HttpContext context) =>
        ((MemoryStream)context.Response.Body).ToArray();
}
