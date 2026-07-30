using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Enums;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Repositories;
using RinhaDasLendas.Domain.ValueObjects;
using RinhaDasLendas.Infrastructure.Identity;
using RinhaDasLendas.Infrastructure.Persistence;
using RinhaDasLendas.Infrastructure.Repositories;
using RinhaDasLendas.Infrastructure.Services;
using RinhaDasLendas.Tests.Fixtures;

namespace RinhaDasLendas.Tests.Infrastructure;

public sealed class IdempotencyServiceTests
{
    private static readonly Guid ActorId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly DateTimeOffset CreatedAt = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ExecuteAsync_WithSameCompleteIdentityConcurrently_ShouldAcquireAdvisoryLockAndExecuteCallbackOnce()
    {
        await using var database = await CreateDatabaseAsync();
        var lockObserver = new AdvisoryLockObserver();
        await using var firstContext = database.CreateContext();
        await using var secondContext = database.CreateContext();
        var firstUnitOfWork = new CompetitiveUnitOfWork(firstContext);
        var secondUnitOfWork = new CompetitiveUnitOfWork(secondContext);
        var clock = new FixedTimeProvider(CreatedAt);
        var firstService = Service(firstContext, firstUnitOfWork, clock, lockObserver);
        var secondService = Service(secondContext, secondUnitOfWork, clock, lockObserver);
        var request = Request();
        var callbackEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var callbackCalls = 0;

        var first = firstService.ExecuteAsync(request, async cancellationToken =>
        {
            Interlocked.Increment(ref callbackCalls);
            await firstContext.Database.ExecuteSqlRawAsync(
                "INSERT INTO idempotency_business_effects (id) VALUES (1)", cancellationToken);
            callbackEntered.SetResult();
            await lockObserver.SecondAttempt.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            return Response();
        }, CancellationToken.None);
        await callbackEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));

        var second = secondService.ExecuteAsync(request, _ =>
        {
            Interlocked.Increment(ref callbackCalls);
            return Task.FromResult(Response());
        }, CancellationToken.None);

        var results = await Task.WhenAll(first, second);

        callbackCalls.Should().Be(1);
        lockObserver.Attempts.Should().Be(2);
        results.Count(result => result.Replayed).Should().Be(1);
        results.Select(result => result.Result.Content).Should().AllSatisfy(content => content.Should().Equal([1, 2, 3]));
        (await CountAsync(database, "idempotency_business_effects")).Should().Be(1);
        (await CountAsync(database, "operacoes_idempotentes")).Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReplayAtExactNinetyDayBoundaryAndReplaceOnlyMatchingExpiredEntryAfterIt()
    {
        await using var database = await CreateDatabaseAsync();
        await SeedOperationAsync(database, Request(), CreatedAt);
        await SeedOperationAsync(database, Request() with { Key = "unrelated-expired-key" }, CreatedAt);
        var callbackCalls = 0;

        await using (var boundaryContext = database.CreateContext())
        {
            var boundary = await Service(
                boundaryContext,
                new CompetitiveUnitOfWork(boundaryContext),
                new FixedTimeProvider(CreatedAt.AddDays(90)))
                .ExecuteAsync(Request(), _ =>
                {
                    callbackCalls++;
                    return Task.FromResult(Response());
                }, CancellationToken.None);
            boundary.Replayed.Should().BeTrue();
        }

        await using (var expiredContext = database.CreateContext())
        {
            var expired = await Service(
                expiredContext,
                new CompetitiveUnitOfWork(expiredContext),
                new FixedTimeProvider(CreatedAt.AddDays(90).AddTicks(1)))
                .ExecuteAsync(Request(), _ =>
                {
                    callbackCalls++;
                    return Task.FromResult(Response());
                }, CancellationToken.None);
            expired.Replayed.Should().BeFalse();
        }

        callbackCalls.Should().Be(1);
        (await CountAsync(database, "operacoes_idempotentes")).Should().Be(2,
            "evaluation removes only its matching expired identity, not unrelated expired rows");
    }

    [Fact]
    public async Task ExecuteAsync_WithDivergentHash_ShouldThrowStableConflictWithoutCallback()
    {
        await using var database = await CreateDatabaseAsync();
        await SeedOperationAsync(database, Request(), CreatedAt);
        await using var context = database.CreateContext();
        var service = Service(context, new CompetitiveUnitOfWork(context), new FixedTimeProvider(CreatedAt));
        var callbackCalls = 0;

        var act = () => service.ExecuteAsync(
            Request() with { RequestHash = Hash('b') },
            _ =>
            {
                callbackCalls++;
                return Task.FromResult(Response());
            },
            CancellationToken.None);

        (await act.Should().ThrowAsync<DomainException>()).Which.MessageCode
            .Should().Be(RinhaDasLendas.Domain.Constants.MessageCodes.CompetitiveIdempotencyConflict);
        callbackCalls.Should().Be(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteAsync_WhenCallbackOrResponseLimitFails_ShouldRollbackBusinessAndIdempotencyRecords(
        bool responseLimitFailure)
    {
        await using var database = await CreateDatabaseAsync();
        await using var context = database.CreateContext();
        var service = Service(context, new CompetitiveUnitOfWork(context), new FixedTimeProvider(CreatedAt));

        var act = () => service.ExecuteAsync(Request(), async cancellationToken =>
        {
            await context.Database.ExecuteSqlRawAsync(
                "INSERT INTO idempotency_business_effects (id) VALUES (2)", cancellationToken);
            if (responseLimitFailure)
            {
                throw new DomainException(RinhaDasLendas.Domain.Constants.MessageCodes.ValidationError);
            }
            throw new InvalidOperationException("callback failed");
        }, CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
        (await CountAsync(database, "idempotency_business_effects")).Should().Be(0);
        (await CountAsync(database, "operacoes_idempotentes")).Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_WhenFinalPersistenceFails_ShouldRollbackBusinessAndIdempotencyRecords()
    {
        await using var database = await CreateDatabaseAsync();
        await using var context = database.CreateContext(new ThrowOnSaveInterceptor());
        var service = Service(context, new CompetitiveUnitOfWork(context), new FixedTimeProvider(CreatedAt));

        var act = () => service.ExecuteAsync(Request(), async cancellationToken =>
        {
            await context.Database.ExecuteSqlRawAsync(
                "INSERT INTO idempotency_business_effects (id) VALUES (3)", cancellationToken);
            return Response();
        }, CancellationToken.None);

        await act.Should().ThrowAsync<DbUpdateException>();
        (await CountAsync(database, "idempotency_business_effects")).Should().Be(0);
        (await CountAsync(database, "operacoes_idempotentes")).Should().Be(0);
    }

    [Fact]
    public async Task CleanupExpiredAsync_ShouldBeExplicitAndUseInjectedClock()
    {
        await using var database = await CreateDatabaseAsync();
        await SeedOperationAsync(database, Request(), CreatedAt);
        await using var context = database.CreateContext();
        var service = Service(
            context,
            new CompetitiveUnitOfWork(context),
            new FixedTimeProvider(CreatedAt.AddDays(90).AddTicks(1)));

        var removed = await service.CleanupExpiredAsync(CancellationToken.None);

        removed.Should().Be(1);
        (await CountAsync(database, "operacoes_idempotentes")).Should().Be(0);
    }

    private static IdempotencyService Service(
        RinhaDasLendasDbContext context,
        CompetitiveUnitOfWork unitOfWork,
        TimeProvider clock,
        IIdempotencyAdvisoryLockHook? lockHook = null) =>
        new(context, new IdempotencyRepository(context), unitOfWork, clock, lockHook);

    [Fact]
    public async Task ExecuteAsync_WhenHandlerRequestsSave_ShouldPhysicallyFlushBusinessAndIdempotencyOnce()
    {
        await using var database = await CreateDatabaseAsync();
        var saveCounter = new SavingChangesCounter();
        await using var context = database.CreateContext(saveCounter);
        var unitOfWork = new CompetitiveUnitOfWork(context);
        var service = Service(context, unitOfWork, new FixedTimeProvider(CreatedAt));

        var result = await service.ExecuteAsync(Request(), async cancellationToken =>
        {
            var actor = await context.Users.SingleAsync(user => user.Id == ActorId, cancellationToken);
            actor.Nome = "Committed actor";
            await ((ICompetitiveUnitOfWork)unitOfWork).SaveChangesAsync(cancellationToken);
            return Response();
        }, CancellationToken.None);

        result.Replayed.Should().BeFalse();
        saveCounter.Calls.Should().Be(1);
        await using var verification = database.CreateContext();
        (await verification.Users.SingleAsync(user => user.Id == ActorId)).Nome.Should().Be("Committed actor");
        (await verification.OperacoesIdempotentes.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_WhenHandlerRequestsSaveThenFails_ShouldRollbackWithoutPhysicalFlush()
    {
        await using var database = await CreateDatabaseAsync();
        var saveCounter = new SavingChangesCounter();
        await using var context = database.CreateContext(saveCounter);
        var unitOfWork = new CompetitiveUnitOfWork(context);
        var service = Service(context, unitOfWork, new FixedTimeProvider(CreatedAt));

        var act = () => service.ExecuteAsync(Request(), async cancellationToken =>
        {
            var actor = await context.Users.SingleAsync(user => user.Id == ActorId, cancellationToken);
            actor.Nome = "Must roll back";
            await ((ICompetitiveUnitOfWork)unitOfWork).SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("handler failed");
        }, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        saveCounter.Calls.Should().Be(0);
        await using var verification = database.CreateContext();
        (await verification.Users.SingleAsync(user => user.Id == ActorId)).Nome.Should().Be("Idempotency actor");
        (await verification.OperacoesIdempotentes.CountAsync()).Should().Be(0);
    }

    private static async Task<CompetitivePostgresFixture> CreateDatabaseAsync()
    {
        var database = await CompetitivePostgresFixture.CreateEmptyAsync();
        await using var context = database.CreateContext();
        await context.Database.EnsureCreatedAsync();
        context.Users.Add(new ApplicationUser
        {
            Id = ActorId,
            Nome = "Idempotency actor",
            UserName = "idempotency@example.com",
            NormalizedUserName = "IDEMPOTENCY@EXAMPLE.COM",
            Email = "idempotency@example.com",
            NormalizedEmail = "IDEMPOTENCY@EXAMPLE.COM",
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
            DataCadastro = CreatedAt,
            DataAtualizacao = CreatedAt,
        });
        await context.SaveChangesAsync();
        await context.Database.ExecuteSqlRawAsync(
            "CREATE TABLE idempotency_business_effects (id integer PRIMARY KEY)");
        return database;
    }

    private static async Task SeedOperationAsync(
        CompetitivePostgresFixture database,
        IdempotencyRequest request,
        DateTimeOffset createdAt)
    {
        await using var context = database.CreateContext();
        context.OperacoesIdempotentes.Add(new OperacaoIdempotente(
            request.ActorId,
            request.Method,
            request.Route,
            request.Key,
            request.RequestHash,
            201,
            RecursoCompetitivoTipo.Season,
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            StoredResult(),
            createdAt));
        await context.SaveChangesAsync();
    }

    private static async Task<int> CountAsync(CompetitivePostgresFixture database, string table)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {table}";
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static IdempotencyRequest Request() => new(
        ActorId,
        "POST",
        "/api/v1/temporadas/{seasonId}/aberturas",
        "test-idempotency-key",
        Hash('a'));

    private static IdempotencyResult Response() => new(
        201,
        RecursoCompetitivoTipo.Season,
        Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
        [1, 2, 3],
        new Dictionary<MetadadoResultadoOperacao, object?>
        {
            [MetadadoResultadoOperacao.TipoConteudo] = "application/json",
            [MetadadoResultadoOperacao.Localizacao] = "/api/v1/temporadas/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
            [MetadadoResultadoOperacao.VersaoRecurso] = "W/\"1\"",
        });

    private static ResultadoOperacaoIdempotente StoredResult() =>
        ResultadoOperacaoIdempotente.Criar([1, 2, 3], new Dictionary<MetadadoResultadoOperacao, object?>
        {
            [MetadadoResultadoOperacao.TipoConteudo] = "application/json",
            [MetadadoResultadoOperacao.Localizacao] = "/api/v1/temporadas/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
            [MetadadoResultadoOperacao.VersaoRecurso] = "W/\"1\"",
            [MetadadoResultadoOperacao.Repeticao] = false,
        });

    private static string Hash(char value) => new(value, 128);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class SavingChangesCounter : SaveChangesInterceptor
    {
        public int Calls { get; private set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return ValueTask.FromResult(result);
        }
    }

    private sealed class AdvisoryLockObserver : IIdempotencyAdvisoryLockHook
    {
        private readonly TaskCompletionSource _secondAttempt = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _attempts;

        public int Attempts => Volatile.Read(ref _attempts);
        public Task SecondAttempt => _secondAttempt.Task;

        public Task BeforeAcquireAsync(IdempotencyRequest request, CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref _attempts) == 2)
            {
                _secondAttempt.TrySetResult();
            }
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowOnSaveInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromException<InterceptionResult<int>>(new DbUpdateException("persistence failed"));
    }
}
