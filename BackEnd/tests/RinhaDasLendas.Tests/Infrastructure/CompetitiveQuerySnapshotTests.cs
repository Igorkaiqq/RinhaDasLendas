using System.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RinhaDasLendas.Infrastructure.Persistence;
using RinhaDasLendas.Tests.Fixtures;

namespace RinhaDasLendas.Tests.Infrastructure;

public sealed class CompetitiveQuerySnapshotTests
{
    [Fact]
    public async Task ExecuteAsync_WithoutTransaction_ShouldOwnRepeatableReadTransactionAndDisposeIt()
    {
        await using var database = await CompetitivePostgresFixture.CreateEmptyAsync();
        await using var context = database.CreateContext();
        var snapshot = new CompetitiveQuerySnapshot(context);

        var result = await snapshot.ExecuteAsync(
            _ =>
            {
                var transaction = context.Database.CurrentTransaction;
                transaction.Should().NotBeNull();
                transaction!.GetDbTransaction().IsolationLevel.Should().Be(IsolationLevel.RepeatableRead);
                return Task.FromResult(42);
            },
            CancellationToken.None);

        result.Should().Be(42);
        context.Database.CurrentTransaction.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WithReadCommittedTransaction_ShouldRejectWithoutDisposingOwnerTransaction()
    {
        await using var database = await CompetitivePostgresFixture.CreateEmptyAsync();
        await using var context = database.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var snapshot = new CompetitiveQuerySnapshot(context);
        var executed = false;

        var act = () => snapshot.ExecuteAsync(
            _ =>
            {
                executed = true;
                return Task.FromResult(42);
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        executed.Should().BeFalse();
        context.Database.CurrentTransaction.Should().BeSameAs(transaction);
        transaction.GetDbTransaction().Connection.Should().NotBeNull();
    }

    [Theory]
    [InlineData(IsolationLevel.RepeatableRead)]
    [InlineData(IsolationLevel.Serializable)]
    public async Task ExecuteAsync_WithSufficientExistingTransaction_ShouldReuseWithoutDisposingIt(
        IsolationLevel isolationLevel)
    {
        await using var database = await CompetitivePostgresFixture.CreateEmptyAsync();
        await using var context = database.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync(isolationLevel);
        var snapshot = new CompetitiveQuerySnapshot(context);

        var result = await snapshot.ExecuteAsync(
            _ => Task.FromResult(context.Database.CurrentTransaction),
            CancellationToken.None);

        result.Should().BeSameAs(transaction);
        context.Database.CurrentTransaction.Should().BeSameAs(transaction);
        transaction.GetDbTransaction().Connection.Should().NotBeNull();
    }
}
