using System.Net;
using System.Diagnostics.Metrics;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RinhaDasLendas.Api.Observability;
using RinhaDasLendas.Api.Filters;
using RinhaDasLendas.Api.Services;
using RinhaDasLendas.Application.Commands.DraftMontagens;
using RinhaDasLendas.Application.Commands.Jogadores;
using RinhaDasLendas.Application.Dtos;
using RinhaDasLendas.Application.Handlers.DraftMontagens;
using RinhaDasLendas.Application.Handlers.Jogadores;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Application.Validators;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Repositories;
using RinhaDasLendas.Tests.Infrastructure;
using RinhaDasLendas.Tests.Jogadores;
using ResourceMessageProvider = RinhaDasLendas.Infrastructure.Messages.ResourceMessageProvider;

namespace RinhaDasLendas.Tests.Security;

public sealed class SecurityHardeningTests
{
    [Fact]
    public void RateLimitPartition_ShouldUseBotIdentity()
    {
        var context = AuthenticatedContext("discord-bot");

        ApiRateLimitPartition.GetPartitionKey(context).Should().Be("bot:discord-bot");
    }

    [Fact]
    public void RateLimitPartition_ShouldUseAuthenticatedUserIdentity()
    {
        var userId = Guid.NewGuid().ToString();
        var context = AuthenticatedContext(userId);

        ApiRateLimitPartition.GetPartitionKey(context).Should().Be($"user:{userId}");
    }

    [Fact]
    public void RateLimitPartition_ShouldUseAnonymousIpAddress()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.10");

        ApiRateLimitPartition.GetPartitionKey(context).Should().Be("ip:203.0.113.10");
    }

    [Fact]
    public void RateLimitPartition_ShouldIgnoreNameIdentifierFromUnauthenticatedIdentity()
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            ])),
        };
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.11");

        ApiRateLimitPartition.GetPartitionKey(context).Should().Be("ip:203.0.113.11");
    }

    [Fact]
    public async Task RateLimiter_ShouldAuthenticateBotBeforeSelectingPartition()
    {
        using var factory = new RateLimitedApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(BotInternalAuthOptions.HeaderName, RateLimitedApiFactory.InternalToken);

        var firstBotResponse = await client.PostAsJsonAsync("/api/v1/drafts", new { });
        client.DefaultRequestHeaders.Remove(BotInternalAuthOptions.HeaderName);
        var userResponse = await client.PostAsJsonAsync("/api/v1/drafts", new { });
        client.DefaultRequestHeaders.Add(BotInternalAuthOptions.HeaderName, RateLimitedApiFactory.InternalToken);
        var secondBotResponse = await client.PostAsJsonAsync("/api/v1/drafts", new { });

        firstBotResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        userResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        secondBotResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task RealJwtBearer_ShouldPartitionAuthenticatedUsersByIdentifier()
    {
        using var factory = new RealAuthenticationApiFactory(1);
        using var client = factory.CreateClient();
        var firstUserToken = SecurityApiFactory.CreateJwt(Guid.NewGuid(), AuthRoles.Admin);
        var secondUserToken = SecurityApiFactory.CreateJwt(Guid.NewGuid(), AuthRoles.Admin);

        client.DefaultRequestHeaders.Authorization = new("Bearer", firstUserToken);
        var firstUserResponse = await client.PostAsJsonAsync("/api/v1/draft-montagens", new { });
        client.DefaultRequestHeaders.Authorization = new("Bearer", secondUserToken);
        var secondUserResponse = await client.PostAsJsonAsync("/api/v1/draft-montagens", new { });
        client.DefaultRequestHeaders.Authorization = new("Bearer", firstUserToken);
        var repeatedFirstUserResponse = await client.PostAsJsonAsync("/api/v1/draft-montagens", new { });

        firstUserResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        secondUserResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        repeatedFirstUserResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task RealJwtBearer_ShouldNotAuthorizeMixedEndpoint_WhenInternalHeaderIsInvalid()
    {
        using var factory = new RealAuthenticationApiFactory(10);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", SecurityApiFactory.CreateJwt(Guid.NewGuid(), AuthRoles.Admin));
        client.DefaultRequestHeaders.Add(BotInternalAuthOptions.HeaderName, "invalid-internal-token");

        var response = await client.PostAsJsonAsync("/api/v1/draft-montagens", new { });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RealJwtBearer_ShouldNotAuthorizeJwtOnlyEndpoint_WhenInternalHeaderIsInvalid()
    {
        using var factory = new RealAuthenticationApiFactory(10);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", SecurityApiFactory.CreateJwt(Guid.NewGuid(), AuthRoles.Admin));
        client.DefaultRequestHeaders.Add(BotInternalAuthOptions.HeaderName, "invalid-internal-token");

        var response = await client.PostAsJsonAsync(
            $"/api/v1/draft-montagens/{Guid.NewGuid()}/presencas/confirmar",
            new { });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RealBot_ShouldNotAuthorizePlainJwtEndpoints()
    {
        using var factory = new RealAuthenticationApiFactory(100);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(BotInternalAuthOptions.HeaderName, SecurityApiFactory.BotToken);

        var authResponse = await client.GetAsync("/api/v1/auth/me");
        var jogadoresResponse = await client.GetAsync("/api/v1/jogadores");
        var draftsResponse = await client.GetAsync("/api/v1/drafts");

        authResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
        jogadoresResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
        draftsResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RealJwtBearer_ShouldContinueAuthorizingPlainJwtEndpoints()
    {
        using var factory = new RealAuthenticationApiFactory(100);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", SecurityApiFactory.CreateJwt(Guid.NewGuid(), AuthRoles.Admin));

        var authResponse = await client.GetAsync("/api/v1/auth/me");
        var jogadoresResponse = await client.GetAsync("/api/v1/jogadores");
        var draftsResponse = await client.GetAsync("/api/v1/drafts");

        authResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        jogadoresResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        draftsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RealBot_ShouldContinueAuthorizingMixedEndpoint()
    {
        using var factory = new RealAuthenticationApiFactory(100);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(BotInternalAuthOptions.HeaderName, SecurityApiFactory.BotToken);

        var response = await client.PostAsJsonAsync("/api/v1/draft-montagens", new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("pt-BR", "Falha na autenticação")]
    [InlineData("en-US", "Authentication failed")]
    public async Task AnonymousRequest_ShouldReturnLocalizedAuthenticationError(string language, string expectedMessage)
    {
        using var factory = new RealAuthenticationApiFactory(100);
        using var client = factory.CreateAnonymousClient();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(language);

        var response = await client.GetAsync($"/api/v1/draft-montagens/{Guid.NewGuid()}/administracao");

        await AssertApiErrorAsync(response, HttpStatusCode.Unauthorized, MessageCodes.AuthenticationFailed, expectedMessage);
    }

    [Theory]
    [InlineData("pt-BR", "Acesso negado")]
    [InlineData("en-US", "Access denied")]
    public async Task PlayerRequest_ShouldReturnLocalizedForbiddenError(string language, string expectedMessage)
    {
        using var factory = new RealAuthenticationApiFactory(100);
        using var client = factory.CreateJwtClient(Guid.NewGuid(), AuthRoles.Jogador);
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(language);

        var response = await client.GetAsync($"/api/v1/draft-montagens/{Guid.NewGuid()}/administracao");

        await AssertApiErrorAsync(response, HttpStatusCode.Forbidden, MessageCodes.AccessDenied, expectedMessage);
    }

    [Theory]
    [InlineData(AuthRoles.Admin)]
    [InlineData(AuthRoles.Moderador)]
    [InlineData(AuthRoles.SuperAdmin)]
    public async Task DraftManagers_ShouldReachAdministrativeEndpoint(string role)
    {
        using var factory = new RealAuthenticationApiFactory(100);
        using var client = factory.CreateJwtClient(Guid.NewGuid(), role);

        var response = await client.GetAsync($"/api/v1/draft-montagens/{Guid.NewGuid()}/administracao");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("pt-BR", "Token interno do bot inválido")]
    [InlineData("en-US", "Invalid internal bot token")]
    public async Task InvalidBotToken_ShouldReturnLocalizedSpecificError(string language, string expectedMessage)
    {
        using var factory = new RealAuthenticationApiFactory(100);
        using var client = factory.CreateInvalidBotClient();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(language);

        var response = await client.GetAsync("/api/v1/draft-montagens/ativos");

        await AssertApiErrorAsync(response, HttpStatusCode.Unauthorized, MessageCodes.BotInternalTokenInvalid, expectedMessage);
    }

    [Fact]
    public async Task WrongAuthenticationSchemes_ShouldNotCrossAuthorizeEndpoints()
    {
        using var factory = new RealAuthenticationApiFactory(100);
        using var jwtClient = factory.CreateJwtClient(Guid.NewGuid(), AuthRoles.Admin);
        using var botClient = factory.CreateBotClient();

        var jwtOnBotOnly = await jwtClient.GetAsync("/api/v1/draft-montagens/ativos");
        var botOnAdminOnly = await botClient.GetAsync($"/api/v1/draft-montagens/{Guid.NewGuid()}/administracao");

        await AssertApiErrorAsync(jwtOnBotOnly, HttpStatusCode.Forbidden, MessageCodes.AccessDenied, "Acesso negado");
        await AssertApiErrorAsync(botOnAdminOnly, HttpStatusCode.Forbidden, MessageCodes.AccessDenied, "Acesso negado");
    }

    [Fact]
    public async Task JwtWithInvalidInternalHeader_ShouldReturnInternalTokenErrorWithoutDetails()
    {
        using var factory = new RealAuthenticationApiFactory(100);
        using var client = factory.CreateJwtClient(Guid.NewGuid(), AuthRoles.Admin);
        client.DefaultRequestHeaders.Add(BotInternalAuthOptions.HeaderName, "invalid-internal-token");

        var response = await client.GetAsync($"/api/v1/draft-montagens/{Guid.NewGuid()}/administracao");

        await AssertApiErrorAsync(
            response,
            HttpStatusCode.Unauthorized,
            MessageCodes.BotInternalTokenInvalid,
            "Token interno do bot inválido");
        (await response.Content.ReadAsStringAsync()).Should().NotContain("invalid-internal-token");
    }

    [Fact]
    public async Task RateLimitExceeded_ShouldHonorAcceptLanguageWithStandardShape()
    {
        using var factory = new RealAuthenticationApiFactory(1);
        using var client = factory.CreateJwtClient(Guid.NewGuid(), AuthRoles.Admin);
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US");

        var firstResponse = await client.GetAsync($"/api/v1/draft-montagens/{Guid.NewGuid()}/administracao");
        var limitedResponse = await client.GetAsync($"/api/v1/draft-montagens/{Guid.NewGuid()}/administracao");

        firstResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await AssertApiErrorAsync(
            limitedResponse,
            HttpStatusCode.TooManyRequests,
            MessageCodes.RateLimitExceeded,
            "Request limit exceeded. Try again later.");
    }

    [Fact]
    public async Task Player_ShouldReachPublicDraftShapeButNotAdministrativeShape()
    {
        using var factory = new RealAuthenticationApiFactory(100);
        using var client = factory.CreateJwtClient(Guid.NewGuid(), AuthRoles.Jogador);

        var publicResponse = await client.GetAsync($"/api/v1/draft-montagens/{Guid.NewGuid()}");
        var administrativeResponse = await client.GetAsync($"/api/v1/draft-montagens/{Guid.NewGuid()}/administracao");

        publicResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await AssertApiErrorAsync(
            administrativeResponse,
            HttpStatusCode.Forbidden,
            MessageCodes.AccessDenied,
            "Acesso negado");
    }

    [Theory]
    [InlineData("POST", "/api/v1/draft-montagens/00000000-0000-0000-0000-000000000001/discord/presencas/confirmar")]
    [InlineData("POST", "/api/v1/draft-montagens/00000000-0000-0000-0000-000000000001/discord/presencas/cancelar")]
    [InlineData("POST", "/api/v1/draft-montagens/00000000-0000-0000-0000-000000000001/discord/publicacoes/claim")]
    [InlineData("POST", "/api/v1/draft-montagens/00000000-0000-0000-0000-000000000001/discord/publicacao")]
    [InlineData("POST", "/api/v1/draft-montagens/00000000-0000-0000-0000-000000000001/discord/publicacao/falha")]
    public async Task BotOnlyMutationFamily_ShouldRejectJwtAndAcceptBotScheme(string method, string route)
    {
        using var factory = new RealAuthenticationApiFactory(100);
        using var jwtClient = factory.CreateJwtClient(Guid.NewGuid(), AuthRoles.Admin);
        using var botClient = factory.CreateBotClient();

        var jwtResponse = await SendAsync(jwtClient, method, route);
        var botResponse = await SendAsync(botClient, method, route);

        await AssertApiErrorAsync(jwtResponse, HttpStatusCode.Forbidden, MessageCodes.AccessDenied, "Acesso negado");
        AssertNotAuthorizationFailure(botResponse);
    }

    [Theory]
    [InlineData("GET", "/api/v1/draft-montagens/00000000-0000-0000-0000-000000000001/administracao")]
    [InlineData("POST", "/api/v1/draft-montagens/00000000-0000-0000-0000-000000000001/iniciar-tempo-real")]
    [InlineData("POST", "/api/v1/draft-montagens/00000000-0000-0000-0000-000000000001/presencas/manual")]
    [InlineData("DELETE", "/api/v1/draft-montagens/00000000-0000-0000-0000-000000000001/presencas/00000000-0000-0000-0000-000000000002")]
    [InlineData("GET", "/api/v1/draft-montagens/00000000-0000-0000-0000-000000000001/presencas/elegiveis")]
    [InlineData("POST", "/api/v1/draft-montagens/00000000-0000-0000-0000-000000000001/discord/publicacoes/republicar")]
    [InlineData("POST", "/api/v1/draft-montagens/00000000-0000-0000-0000-000000000001/reservas/substituir")]
    [InlineData("PUT", "/api/v1/draft-montagens/00000000-0000-0000-0000-000000000001/layout")]
    [InlineData("POST", "/api/v1/draft-montagens/00000000-0000-0000-0000-000000000001/capitaes/sortear")]
    [InlineData("PATCH", "/api/v1/draft-montagens/00000000-0000-0000-0000-000000000001/finalizar")]
    [InlineData("PATCH", "/api/v1/draft-montagens/00000000-0000-0000-0000-000000000001/cancelar")]
    public async Task AdministrativeMutationFamily_ShouldRejectPlayerAndBot(string method, string route)
    {
        using var factory = new RealAuthenticationApiFactory(100);
        using var playerClient = factory.CreateJwtClient(Guid.NewGuid(), AuthRoles.Jogador);
        using var botClient = factory.CreateBotClient();

        var playerResponse = await SendAsync(playerClient, method, route);
        var botResponse = await SendAsync(botClient, method, route);

        await AssertApiErrorAsync(playerResponse, HttpStatusCode.Forbidden, MessageCodes.AccessDenied, "Acesso negado");
        await AssertApiErrorAsync(botResponse, HttpStatusCode.Forbidden, MessageCodes.AccessDenied, "Acesso negado");
    }

    [Theory]
    [InlineData("POST", "/api/v1/draft-montagens")]
    [InlineData("POST", "/api/v1/draft-montagens/00000000-0000-0000-0000-000000000001/encerrar-presenca")]
    [InlineData("POST", "/api/v1/draft-montagens/00000000-0000-0000-0000-000000000001/capitaes")]
    [InlineData("POST", "/api/v1/draft-montagens/00000000-0000-0000-0000-000000000001/ordem-escolha")]
    public async Task SharedMutationFamily_ShouldAllowManagersAndBotButRejectPlayer(string method, string route)
    {
        using var factory = new RealAuthenticationApiFactory(100);
        using var playerClient = factory.CreateJwtClient(Guid.NewGuid(), AuthRoles.Jogador);
        using var managerClient = factory.CreateJwtClient(Guid.NewGuid(), AuthRoles.Moderador);
        using var botClient = factory.CreateBotClient();

        var playerResponse = await SendAsync(playerClient, method, route);
        var managerResponse = await SendAsync(managerClient, method, route);
        var botResponse = await SendAsync(botClient, method, route);

        await AssertApiErrorAsync(playerResponse, HttpStatusCode.Forbidden, MessageCodes.AccessDenied, "Acesso negado");
        AssertNotAuthorizationFailure(managerResponse);
        AssertNotAuthorizationFailure(botResponse);
    }

    [Theory]
    [InlineData("/api/v1/draft-montagens/00000000-0000-0000-0000-000000000001/presencas/confirmar")]
    [InlineData("/api/v1/draft-montagens/00000000-0000-0000-0000-000000000001/presencas/cancelar")]
    public async Task PlayerPresenceMutationFamily_ShouldRequireJwtIdentity(string route)
    {
        using var factory = new RealAuthenticationApiFactory(100);
        using var anonymousClient = factory.CreateAnonymousClient();
        using var playerClient = factory.CreateJwtClient(Guid.NewGuid(), AuthRoles.Jogador);
        using var botClient = factory.CreateBotClient();

        var anonymousResponse = await SendAsync(anonymousClient, "POST", route);
        var playerResponse = await SendAsync(playerClient, "POST", route);
        var botResponse = await SendAsync(botClient, "POST", route);

        await AssertApiErrorAsync(anonymousResponse, HttpStatusCode.Unauthorized, MessageCodes.AuthenticationFailed, "Falha na autenticação");
        AssertNotAuthorizationFailure(playerResponse);
        await AssertApiErrorAsync(botResponse, HttpStatusCode.Forbidden, MessageCodes.AccessDenied, "Acesso negado");
    }

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, string method, string route)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), route);
        if (!string.Equals(method, "GET", StringComparison.Ordinal))
        {
            request.Content = JsonContent.Create(new { });
        }

        return client.SendAsync(request);
    }

    private static void AssertNotAuthorizationFailure(HttpResponseMessage response)
    {
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    private static async Task AssertApiErrorAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedCode,
        string expectedMessage)
    {
        response.StatusCode.Should().Be(expectedStatus);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error.Should().NotBeNull();
        error!.Message.Should().Be(expectedMessage);
        error.MessageCode.Should().Be(expectedCode);
        error.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("PermitLimit")]
    [InlineData("WindowSeconds")]
    public void RateLimiter_ShouldRejectNonPositiveOptionsAtStartup(string optionName)
    {
        using var factory = new InvalidRateLimitApiFactory(optionName);

        var action = () => factory.CreateClient();

        action.Should().Throw<InvalidOperationException>()
            .WithMessage(new ResourceMessageProvider().GetMessage(MessageCodes.RateLimitConfigurationInvalid));
    }

    [Fact]
    public async Task RateLimiter_ShouldReturnLocalizedApiError_WhenPartitionLimitIsExceeded()
    {
        using var factory = new RateLimitedApiFactory();
        using var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/v1/drafts", new { });
        var response = await client.PostAsJsonAsync("/api/v1/drafts", new { });
        using var error = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var expected = ApiErrorResponse.FromCode(
            new ResourceMessageProvider(),
            MessageCodes.RateLimitExceeded);

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        error.RootElement.GetProperty("messageCode").GetString().Should().Be(MessageCodes.RateLimitExceeded);
        error.RootElement.GetProperty("message").GetString().Should().Be(expected.Message);
        error.RootElement.GetProperty("errors").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task UpdatePreferencias_ShouldRejectUserEditingAnotherPlayer()
    {
        var repository = new InMemoryJogadorRepository();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var jogadorB = JogadorTestData.JogadorAtivo();
        jogadorB.VincularUsuario(userB);
        await repository.AddAsync(jogadorB, CancellationToken.None);
        var handler = new UpdatePreferenciasCommandHandler(repository, new TestCurrentUser(userA, [AuthRoles.Jogador]), new UpdatePreferenciasRotasRequestDtoValidator());
        var request = ValidPreferencesRequest();

        var act = () => handler.Handle(new UpdatePreferenciasCommand(jogadorB.Id, request), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().Where(ex => ex.Message == MessageCodes.InsufficientPermission);
    }

    [Fact]
    public async Task ConfirmPresence_ShouldUseAuthenticatedUser_WhenPayloadTargetsAnotherUser()
    {
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var jogadorA = JogadorTestData.JogadorAtivo();
        jogadorA.VincularUsuario(userA);
        var jogadorB = JogadorTestData.JogadorAtivo();
        jogadorB.VincularUsuario(userB);
        var montagem = new DraftMontagem("Draft", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
        var repository = new InMemoryDraftMontagemRepository([montagem], [jogadorA, jogadorB]);
        var notifier = new TestDraftMontagemRealtimeNotifier();
        var metrics = new TestDraftMontagemMetrics();
        var handler = new ConfirmarPresencaDraftMontagemCommandHandler(
            repository,
            new TestCurrentUser(userA, [AuthRoles.Jogador]),
            new TestDiscordIdentityLookupService(),
            new ConfirmarPresencaDraftMontagemValidator(),
            notifier,
            metrics);

        await handler.Handle(new ConfirmarPresencaDraftMontagemCommand(
            montagem.Id,
            new ConfirmarPresencaDraftMontagemRequestDto(userB, null, DraftMontagemPresencaOrigem.Web.ToString())), CancellationToken.None);

        montagem.Presencas.Should().ContainSingle(presenca => presenca.UsuarioId == userA && presenca.JogadorId == jogadorA.Id);
        montagem.Presencas.Should().NotContain(presenca => presenca.UsuarioId == userB);
        notifier.Calls.Should().Be(1);
        notifier.LastDraftMontagemId.Should().Be(montagem.Id);
        metrics.ConfirmedPresenceCalls.Should().Be(1);
    }

    [Fact]
    public async Task RepeatedPresenceConfirmation_ShouldNotPersistNotifyOrRecordMetric()
    {
        var userId = Guid.NewGuid();
        var jogador = JogadorTestData.JogadorAtivo();
        jogador.VincularUsuario(userId);
        var montagem = new DraftMontagem("Draft", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
        montagem.ConfirmarPresenca(userId, jogador.Id, null, DraftMontagemPresencaOrigem.Web);
        var repository = new InMemoryDraftMontagemRepository([montagem], [jogador]);
        var notifier = new TestDraftMontagemRealtimeNotifier();
        var metrics = new TestDraftMontagemMetrics();
        var handler = new ConfirmarPresencaDraftMontagemCommandHandler(
            repository,
            new TestCurrentUser(userId, [AuthRoles.Jogador]),
            new TestDiscordIdentityLookupService(),
            new ConfirmarPresencaDraftMontagemValidator(),
            notifier,
            metrics);

        await handler.Handle(new ConfirmarPresencaDraftMontagemCommand(
            montagem.Id,
            new ConfirmarPresencaDraftMontagemRequestDto(userId, null, DraftMontagemPresencaOrigem.Web.ToString())), CancellationToken.None);

        repository.TrySaveCalls.Should().Be(0);
        notifier.Calls.Should().Be(0);
        metrics.ConfirmedPresenceCalls.Should().Be(0);
    }

    [Fact]
    public async Task RepeatedPresenceCancellation_ShouldNotPersistNotifyOrRecordMetric()
    {
        var userId = Guid.NewGuid();
        var jogador = JogadorTestData.JogadorAtivo();
        jogador.VincularUsuario(userId);
        var montagem = new DraftMontagem("Draft", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
        montagem.ConfirmarPresenca(userId, jogador.Id, null, DraftMontagemPresencaOrigem.Web);
        montagem.CancelarPresenca(userId);
        var repository = new InMemoryDraftMontagemRepository([montagem], [jogador]);
        var notifier = new TestDraftMontagemRealtimeNotifier();
        var metrics = new TestDraftMontagemMetrics();
        var handler = new CancelarPresencaDraftMontagemCommandHandler(
            repository,
            new TestCurrentUser(userId, [AuthRoles.Jogador]),
            new TestDiscordIdentityLookupService(),
            notifier,
            metrics);

        await handler.Handle(new CancelarPresencaDraftMontagemCommand(
            montagem.Id,
            new CancelarPresencaDraftMontagemRequestDto(userId, null)), CancellationToken.None);

        repository.TrySaveCalls.Should().Be(0);
        notifier.Calls.Should().Be(0);
        metrics.CancelledPresenceCalls.Should().Be(0);
    }

    [Fact]
    public async Task CancelPresence_ShouldRejectImpersonation_WhenAuthenticatedUserHasNoPresence()
    {
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var jogadorB = JogadorTestData.JogadorAtivo();
        jogadorB.VincularUsuario(userB);
        var montagem = new DraftMontagem("Draft", null, 5, DraftMontagemCriterioCapitaes.Manual, [], []);
        montagem.ConfirmarPresenca(userB, jogadorB.Id, null, DraftMontagemPresencaOrigem.Web);
        var repository = new InMemoryDraftMontagemRepository([montagem], [jogadorB]);
        var handler = new CancelarPresencaDraftMontagemCommandHandler(repository, new TestCurrentUser(userA, [AuthRoles.Jogador]), new TestDiscordIdentityLookupService(), new TestDraftMontagemRealtimeNotifier(), new TestDraftMontagemMetrics());

        var act = () => handler.Handle(new CancelarPresencaDraftMontagemCommand(montagem.Id, new CancelarPresencaDraftMontagemRequestDto(userB, null)), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().Where(ex => ex.Message == MessageCodes.PresenceNotFound);
        montagem.Presencas.Should().ContainSingle(presenca => presenca.UsuarioId == userB && presenca.Confirmada);
    }

    [Fact]
    public async Task BotInternalAuth_ShouldRejectInvalidToken()
    {
        using var serviceProvider = new ServiceCollection().AddMetrics().BuildServiceProvider();
        var options = new BotInternalAuthOptions { ValidTokens = ["valid-token"] };
        var handler = new BotInternalAuthHandler(
            new TestOptionsMonitor(options),
            NullLoggerFactory.Instance,
            UrlEncoder.Default,
            new ApiMetrics(serviceProvider.GetRequiredService<IMeterFactory>()));
        var context = new DefaultHttpContext();
        context.Request.Headers[BotInternalAuthOptions.HeaderName] = "invalid-token";
        await handler.InitializeAsync(new AuthenticationScheme(BotInternalAuthOptions.SchemeName, null, typeof(BotInternalAuthHandler)), context);

        var result = await handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
        result.Failure.Should().NotBeNull();
    }

    [Fact]
    public void ResolveTokens_ShouldReturnDistinctConfiguredTokens()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["RINHA_API_INTERNAL_TOKEN"] = "primary-token",
            ["DiscordBot:InternalToken"] = "primary-token",
        }).Build();

        var tokens = InternalTokenSecurity.ResolveTokens(configuration);

        tokens.Should().Equal("primary-token");
    }

    [Theory]
    [InlineData("")]
    [InlineData("short-token")]
    [InlineData("change-me-generate-a-long-random-secret")]
    public void ProductionStartup_ShouldRejectUnsafeInternalToken(string token)
    {
        var action = () => InternalTokenSecurity.ValidateProductionTokens(ProductionEnvironment(), [token], new ResourceMessageProvider());

        action.Should().Throw<InvalidOperationException>()
            .WithMessage(new ResourceMessageProvider().GetMessage(MessageCodes.BotInternalTokenNotSecurelyConfigured));
    }

    [Fact]
    public void ProductionStartup_ShouldRejectMissingInternalToken()
    {
        var action = () => InternalTokenSecurity.ValidateProductionTokens(ProductionEnvironment(), [], new ResourceMessageProvider());

        action.Should().Throw<InvalidOperationException>()
            .WithMessage(new ResourceMessageProvider().GetMessage(MessageCodes.BotInternalTokenNotSecurelyConfigured));
    }

    [Fact]
    public void ProductionStartup_ShouldAcceptStrongInternalToken()
    {
        var action = () => InternalTokenSecurity.ValidateProductionTokens(
            ProductionEnvironment(),
            ["a-strong-internal-token-with-32-characters"],
            new ResourceMessageProvider());

        action.Should().NotThrow();
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    [InlineData("IntegrationTesting")]
    public void NonProductionStartup_ShouldIgnoreUnsafeInternalToken(string environmentName)
    {
        var environment = new Moq.Mock<IWebHostEnvironment>();
        environment.SetupGet(value => value.EnvironmentName).Returns(environmentName);

        var action = () => InternalTokenSecurity.ValidateProductionTokens(environment.Object, [], new ResourceMessageProvider());

        action.Should().NotThrow();
    }

    [Theory]
    [InlineData("same-token", "same-token", true)]
    [InlineData("same-token", "different-token", false)]
    public void FixedTimeEquals_ShouldCompareTokenValues(string provided, string expected, bool result)
    {
        InternalTokenSecurity.FixedTimeEquals(provided, expected).Should().Be(result);
    }

    [Fact]
    public void ProductionStartup_ShouldRejectDefaultJwtKey()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseEnvironment("Production"));

        var act = () => factory.CreateClient();

        act.Should().Throw<InvalidOperationException>();
    }

    private static IWebHostEnvironment ProductionEnvironment()
    {
        var environment = new Moq.Mock<IWebHostEnvironment>();
        environment.SetupGet(value => value.EnvironmentName).Returns("Production");
        return environment.Object;
    }

    private static DefaultHttpContext AuthenticatedContext(string id)
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, id)],
            "Test"));
        return context;
    }

    private static UpdatePreferenciasRotasRequestDto ValidPreferencesRequest() => new([
        new("Top", 1, false),
        new("Jungle", 2, false),
        new("Mid", 3, false),
        new("Adc", 4, false),
        new("Support", 5, true),
    ]);

    private sealed record TestCurrentUser(Guid? UserId, IReadOnlyCollection<string> Roles) : ICurrentUser
    {
        public string? IpAddress => null;

        public string? UserAgent => null;
    }

    private sealed class TestDiscordIdentityLookupService : IDiscordIdentityLookupService
    {
        public Task<DiscordUserLinkDto> GetByDiscordUserIdAsync(string discordUserId, CancellationToken cancellationToken)
        {
            return Task.FromResult(new DiscordUserLinkDto(false, null, null, null, []));
        }
    }

    private sealed class TestDraftMontagemRealtimeNotifier : IDraftMontagemRealtimeNotifier
    {
        public int Calls { get; private set; }

        public Guid? LastDraftMontagemId { get; private set; }

        public Task StateUpdatedAsync(Guid draftMontagemId, DraftMontagemRealtimeStateDto state, CancellationToken cancellationToken)
        {
            Calls++;
            LastDraftMontagemId = draftMontagemId;
            return Task.CompletedTask;
        }
    }

    private sealed class TestDraftMontagemMetrics : IDraftMontagemMetrics
    {
        public int ConfirmedPresenceCalls { get; private set; }

        public int CancelledPresenceCalls { get; private set; }

        public void RecordPresenceConfirmed(Guid draftMontagemId, string origin)
        {
            ConfirmedPresenceCalls++;
        }

        public void RecordPresenceCancelled(Guid draftMontagemId, string origin)
        {
            CancelledPresenceCalls++;
        }

        public void RecordPresenceClosed(Guid draftMontagemId)
        {
        }

        public void RecordDiscordPublication(Guid draftMontagemId, string type, string status)
        {
        }

        public void RecordPick(Guid draftMontagemId, string type)
        {
        }

        public void RecordDraftTimeout(Guid draftMontagemId)
        {
        }

        public void RecordDraftCancelled(Guid draftMontagemId)
        {
        }
    }

    private sealed class InMemoryDraftMontagemRepository(IReadOnlyCollection<DraftMontagem> montagens, IReadOnlyCollection<Jogador> jogadores) : IDraftMontagemRepository
    {
        public int TrySaveCalls { get; private set; }

        public Task AddAsync(DraftMontagem montagem, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<DraftMontagem?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(montagens.FirstOrDefault(montagem => montagem.Id == id));

        public Task<DraftMontagem?> ReloadByIdAsync(Guid id, CancellationToken cancellationToken) => GetByIdAsync(id, cancellationToken);

        public Task<IReadOnlyCollection<DraftMontagem>> ListExpiredRealtimeAsync(DateTimeOffset now, int limit, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<DraftMontagem>>([]);

        public Task<IReadOnlyCollection<DraftMontagem>> ListExpiredPresenceAsync(DateTimeOffset now, int limit, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<DraftMontagem>>([]);

        public Task<IReadOnlyCollection<DraftMontagem>> ListActiveForDiscordAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<DraftMontagem>>([]);

        public Task<IReadOnlyCollection<DraftMontagem>> ListAsync(string? search, DraftMontagemStatus? status, bool includeCancelled, int page, int pageSize, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<DraftMontagem>>([]);

        public Task<int> CountAsync(string? search, DraftMontagemStatus? status, bool includeCancelled, CancellationToken cancellationToken) => Task.FromResult(0);

        public Task<IReadOnlyCollection<Jogador>> GetJogadoresByIdsAsync(IReadOnlyCollection<Guid> jogadoresIds, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<Jogador>>(jogadores.Where(jogador => jogadoresIds.Contains(jogador.Id)).ToArray());

        public Task<Jogador?> GetJogadorByUsuarioIdAsync(Guid usuarioId, CancellationToken cancellationToken) => Task.FromResult(jogadores.FirstOrDefault(jogador => jogador.UsuarioId == usuarioId));

        public Task<IReadOnlyCollection<Jogador>> SearchJogadoresElegiveisParaPresencaManualAsync(Guid draftMontagemId, string? search, int page, int pageSize, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<Jogador>>([]);

        public Task<int> CountJogadoresElegiveisParaPresencaManualAsync(Guid draftMontagemId, string? search, CancellationToken cancellationToken) => Task.FromResult(0);

        public Task<RinhaDasLendas.Domain.Models.DraftMontagemPublicacaoClaim?> TryClaimPublicacaoDiscordAsync(Guid draftMontagemId, DraftMontagemPublicacaoDiscordTipo tipo, Guid claimId, DateTimeOffset expiraEm, DateTimeOffset agora, CancellationToken cancellationToken) => Task.FromResult<RinhaDasLendas.Domain.Models.DraftMontagemPublicacaoClaim?>(null);

        public Task<bool> TryConcluirPublicacaoDiscordAsync(Guid draftMontagemId, DraftMontagemPublicacaoDiscordTipo tipo, Guid claimId, string? guildId, string? channelId, string messageId, DateTimeOffset agora, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<bool> TryRegistrarFalhaPublicacaoDiscordAsync(Guid draftMontagemId, DraftMontagemPublicacaoDiscordTipo tipo, Guid claimId, string? guildId, string? channelId, string? erroCodigo, DateTimeOffset agora, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<IReadOnlyCollection<Guid>> MarcarPublicacoesExpiradasParaReconciliacaoAsync(DateTimeOffset agora, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<Guid>>([]);

        public Task<DraftMontagemSaveResultado> TrySaveChangesAsync(CancellationToken cancellationToken)
        {
            TrySaveCalls++;
            return Task.FromResult(DraftMontagemSaveResultado.Persistido);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class TestOptionsMonitor(BotInternalAuthOptions options) : IOptionsMonitor<BotInternalAuthOptions>
    {
        public BotInternalAuthOptions CurrentValue => options;

        public BotInternalAuthOptions Get(string? name) => options;

        public IDisposable? OnChange(Action<BotInternalAuthOptions, string?> listener) => null;
    }

    private sealed class RateLimitedApiFactory : WebApplicationFactory<Program>
    {
        public const string InternalToken = "task-four-review-internal-token";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder
                .UseEnvironment("Testing")
                .UseSetting("DiscordBot:InternalToken", InternalToken)
                .UseSetting("RateLimiting:Api:PermitLimit", "1")
                .UseSetting("RateLimiting:Api:WindowSeconds", "60");
        }
    }

    private sealed class InvalidRateLimitApiFactory(string optionName) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder
                .UseEnvironment("Testing")
                .UseSetting("RateLimiting:Api:PermitLimit", optionName == "PermitLimit" ? "0" : "1")
                .UseSetting("RateLimiting:Api:WindowSeconds", optionName == "WindowSeconds" ? "0" : "60");
        }
    }

    private sealed class RealAuthenticationApiFactory(int permitLimit) : SecurityApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder
                .UseSetting("RateLimiting:Api:PermitLimit", permitLimit.ToString())
                .UseSetting("RateLimiting:Api:WindowSeconds", "60");
        }
    }
}
