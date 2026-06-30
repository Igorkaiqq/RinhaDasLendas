using System.Net;
using System.Diagnostics.Metrics;
using System.Text.Encodings.Web;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RinhaDasLendas.Api.Observability;
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
using RinhaDasLendas.Tests.Jogadores;

namespace RinhaDasLendas.Tests.Security;

public sealed class SecurityHardeningTests
{
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
        var handler = new ConfirmarPresencaDraftMontagemCommandHandler(
            repository,
            new TestCurrentUser(userA, [AuthRoles.Jogador]),
            new TestDiscordIdentityLookupService(),
            new ConfirmarPresencaDraftMontagemValidator());

        await handler.Handle(new ConfirmarPresencaDraftMontagemCommand(
            montagem.Id,
            new ConfirmarPresencaDraftMontagemRequestDto(userB, null, DraftMontagemPresencaOrigem.Web.ToString())), CancellationToken.None);

        montagem.Presencas.Should().ContainSingle(presenca => presenca.UsuarioId == userA && presenca.JogadorId == jogadorA.Id);
        montagem.Presencas.Should().NotContain(presenca => presenca.UsuarioId == userB);
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
        var handler = new CancelarPresencaDraftMontagemCommandHandler(repository, new TestCurrentUser(userA, [AuthRoles.Jogador]), new TestDiscordIdentityLookupService());

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
    public void ProductionStartup_ShouldRejectDefaultJwtKey()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseEnvironment("Production"));

        var act = () => factory.CreateClient();

        act.Should().Throw<InvalidOperationException>();
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

    private sealed class InMemoryDraftMontagemRepository(IReadOnlyCollection<DraftMontagem> montagens, IReadOnlyCollection<Jogador> jogadores) : IDraftMontagemRepository
    {
        public Task AddAsync(DraftMontagem montagem, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<DraftMontagem?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(montagens.FirstOrDefault(montagem => montagem.Id == id));

        public Task<IReadOnlyCollection<DraftMontagem>> ListExpiredRealtimeAsync(DateTimeOffset now, int limit, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<DraftMontagem>>([]);

        public Task<IReadOnlyCollection<DraftMontagem>> ListExpiredPresenceAsync(DateTimeOffset now, int limit, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<DraftMontagem>>([]);

        public Task<IReadOnlyCollection<DraftMontagem>> ListActiveForDiscordAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<DraftMontagem>>([]);

        public Task<IReadOnlyCollection<DraftMontagem>> ListAsync(string? search, DraftMontagemStatus? status, int page, int pageSize, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<DraftMontagem>>([]);

        public Task<int> CountAsync(string? search, DraftMontagemStatus? status, CancellationToken cancellationToken) => Task.FromResult(0);

        public Task<IReadOnlyCollection<Jogador>> GetJogadoresByIdsAsync(IReadOnlyCollection<Guid> jogadoresIds, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<Jogador>>(jogadores.Where(jogador => jogadoresIds.Contains(jogador.Id)).ToArray());

        public Task<Jogador?> GetJogadorByUsuarioIdAsync(Guid usuarioId, CancellationToken cancellationToken) => Task.FromResult(jogadores.FirstOrDefault(jogador => jogador.UsuarioId == usuarioId));

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class TestOptionsMonitor(BotInternalAuthOptions options) : IOptionsMonitor<BotInternalAuthOptions>
    {
        public BotInternalAuthOptions CurrentValue => options;

        public BotInternalAuthOptions Get(string? name) => options;

        public IDisposable? OnChange(Action<BotInternalAuthOptions, string?> listener) => null;
    }
}
