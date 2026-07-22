using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RinhaDasLendas.Api.Filters;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Infrastructure.Messages;

namespace RinhaDasLendas.Tests.Security;

public sealed class ApiAuthorizationMiddlewareResultHandlerTests
{
    [Fact]
    public async Task Challenge_ShouldInvokeSelectedSchemeBeforeWritingStandardBody()
    {
        var authentication = new Mock<IAuthenticationService>();
        authentication
            .Setup(service => service.ChallengeAsync(It.IsAny<HttpContext>(), "Probe", It.IsAny<AuthenticationProperties?>()))
            .Callback<HttpContext, string?, AuthenticationProperties?>((context, _, _) =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.Headers.WWWAuthenticate = "Probe realm=tests";
            })
            .Returns(Task.CompletedTask);
        var context = CreateContext(authentication.Object);
        var handler = new ApiAuthorizationMiddlewareResultHandler(new ResourceMessageProvider());

        await handler.HandleAsync(
            _ => Task.CompletedTask,
            context,
            ProbePolicy(),
            PolicyAuthorizationResult.Challenge());

        authentication.Verify(
            service => service.ChallengeAsync(context, "Probe", It.IsAny<AuthenticationProperties?>()),
            Times.Once);
        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        context.Response.Headers.WWWAuthenticate.ToString().Should().Be("Probe realm=tests");
        await AssertBodyAsync(context, MessageCodes.AuthenticationFailed, "Falha na autenticação");
    }

    [Fact]
    public async Task Forbid_ShouldInvokeSelectedSchemeBeforeWritingStandardBody()
    {
        var authentication = new Mock<IAuthenticationService>();
        authentication
            .Setup(service => service.ForbidAsync(It.IsAny<HttpContext>(), "Probe", It.IsAny<AuthenticationProperties?>()))
            .Callback<HttpContext, string?, AuthenticationProperties?>((context, _, _) => context.Response.StatusCode = StatusCodes.Status403Forbidden)
            .Returns(Task.CompletedTask);
        var context = CreateContext(authentication.Object);
        var handler = new ApiAuthorizationMiddlewareResultHandler(new ResourceMessageProvider());

        await handler.HandleAsync(
            _ => Task.CompletedTask,
            context,
            ProbePolicy(),
            PolicyAuthorizationResult.Forbid());

        authentication.Verify(
            service => service.ForbidAsync(context, "Probe", It.IsAny<AuthenticationProperties?>()),
            Times.Once);
        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        await AssertBodyAsync(context, MessageCodes.AccessDenied, "Acesso negado");
    }

    [Fact]
    public async Task StartedResponse_ShouldNotInvokeSchemeOrWriteAgain()
    {
        var authentication = new Mock<IAuthenticationService>();
        var responseFeature = new StartedResponseFeature(started: true);
        var context = new DefaultHttpContext { RequestServices = Services(authentication.Object) };
        context.Features.Set<IHttpResponseFeature>(responseFeature);
        var handler = new ApiAuthorizationMiddlewareResultHandler(new ResourceMessageProvider());

        await handler.HandleAsync(
            _ => Task.CompletedTask,
            context,
            ProbePolicy(),
            PolicyAuthorizationResult.Challenge());

        authentication.VerifyNoOtherCalls();
        responseFeature.Body.Length.Should().Be(0);
    }

    [Fact]
    public async Task ChallengeCallbackThatStartsResponse_ShouldNotReceiveSecondBody()
    {
        var authentication = new Mock<IAuthenticationService>();
        var responseFeature = new StartedResponseFeature();
        authentication
            .Setup(service => service.ChallengeAsync(It.IsAny<HttpContext>(), "Probe", It.IsAny<AuthenticationProperties?>()))
            .Callback<HttpContext, string?, AuthenticationProperties?>((_, _, _) =>
            {
                responseFeature.Body.Write("scheme"u8);
                responseFeature.Start();
            })
            .Returns(Task.CompletedTask);
        var context = new DefaultHttpContext { RequestServices = Services(authentication.Object) };
        context.Features.Set<IHttpResponseFeature>(responseFeature);
        var handler = new ApiAuthorizationMiddlewareResultHandler(new ResourceMessageProvider());

        await handler.HandleAsync(
            _ => Task.CompletedTask,
            context,
            ProbePolicy(),
            PolicyAuthorizationResult.Challenge());

        responseFeature.Body.Position = 0;
        using var reader = new StreamReader(responseFeature.Body);
        (await reader.ReadToEndAsync()).Should().Be("scheme");
    }

    [Fact]
    public async Task CancelledRequest_ShouldInvokeChallengeButNotWriteBody()
    {
        var authentication = new Mock<IAuthenticationService>();
        authentication
            .Setup(service => service.ChallengeAsync(It.IsAny<HttpContext>(), "Probe", It.IsAny<AuthenticationProperties?>()))
            .Returns(Task.CompletedTask);
        var context = CreateContext(authentication.Object);
        context.RequestAborted = new CancellationToken(canceled: true);
        var handler = new ApiAuthorizationMiddlewareResultHandler(new ResourceMessageProvider());

        await handler.HandleAsync(
            _ => Task.CompletedTask,
            context,
            ProbePolicy(),
            PolicyAuthorizationResult.Challenge());

        authentication.Verify(
            service => service.ChallengeAsync(context, "Probe", It.IsAny<AuthenticationProperties?>()),
            Times.Once);
        context.Response.Body.Length.Should().Be(0);
    }

    private static AuthorizationPolicy ProbePolicy() => new AuthorizationPolicyBuilder("Probe")
        .RequireAuthenticatedUser()
        .Build();

    private static DefaultHttpContext CreateContext(IAuthenticationService authentication)
    {
        return new DefaultHttpContext
        {
            RequestServices = Services(authentication),
            Response = { Body = new MemoryStream() },
        };
    }

    private static IServiceProvider Services(IAuthenticationService authentication)
    {
        return new ServiceCollection()
            .AddSingleton(authentication)
            .BuildServiceProvider();
    }

    private static async Task AssertBodyAsync(DefaultHttpContext context, string expectedCode, string expectedMessage)
    {
        context.Response.Body.Position = 0;
        var error = await System.Text.Json.JsonSerializer.DeserializeAsync<ApiErrorResponse>(
            context.Response.Body,
            System.Text.Json.JsonSerializerOptions.Web);
        error.Should().NotBeNull();
        error!.MessageCode.Should().Be(expectedCode);
        error.Message.Should().Be(expectedMessage);
        context.Response.ContentType.Should().StartWith("application/json");
    }

    private sealed class StartedResponseFeature(bool started = false) : IHttpResponseFeature
    {
        public int StatusCode { get; set; } = StatusCodes.Status401Unauthorized;
        public string? ReasonPhrase { get; set; }
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public Stream Body { get; set; } = new MemoryStream();
        public bool HasStarted { get; private set; } = started;
        public void Start() => HasStarted = true;
        public void OnStarting(Func<object, Task> callback, object state) { }
        public void OnCompleted(Func<object, Task> callback, object state) { }
    }
}
