using Microsoft.EntityFrameworkCore;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Entities;
using RinhaDasLendas.Domain.Exceptions;
using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Infrastructure.Persistence;

public sealed class CompetitiveUnitOfWork(RinhaDasLendasDbContext dbContext) : ICompetitiveUnitOfWork
{
    private bool _deferredSaveActive;
    private bool _deferredSaveRequested;
    private bool _physicalFlushPerformed;
    private int _scopeVersion;

    internal bool DeferredSaveRequested => _deferredSaveRequested;

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        if (!_deferredSaveActive)
        {
            await SaveWithStableConcurrencyErrorAsync(cancellationToken);
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        _deferredSaveRequested = true;
    }

    internal IDisposable BeginDeferredSaveScope()
    {
        if (_deferredSaveActive)
        {
            throw new InvalidOperationException("A deferred competitive save scope is already active.");
        }

        _deferredSaveActive = true;
        _deferredSaveRequested = false;
        _physicalFlushPerformed = false;
        var version = ++_scopeVersion;
        return new DeferredSaveScope(this, version);
    }

    internal async Task FlushDeferredChangesAsync(CancellationToken cancellationToken)
    {
        if (!_deferredSaveActive || _physicalFlushPerformed)
        {
            throw new InvalidOperationException("The deferred competitive save scope cannot be flushed.");
        }

        _physicalFlushPerformed = true;
        await SaveWithStableConcurrencyErrorAsync(cancellationToken);
    }

    private async Task SaveWithStableConcurrencyErrorAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            var messageCode = exception.Entries.Any(entry => entry.Entity is CalendarioCompetitivo)
                ? MessageCodes.CompetitiveCalendarVersionStale
                : MessageCodes.CompetitiveResourceVersionStale;
            throw new DomainException(messageCode);
        }
        catch (DbUpdateException exception)
            when (CompetitiveSaveConflictClassifier.Classify(exception) is { } messageCode)
        {
            throw new DomainException(messageCode);
        }
    }

    private void EndDeferredSaveScope(int version)
    {
        if (!_deferredSaveActive || version != _scopeVersion)
        {
            return;
        }

        _deferredSaveActive = false;
        _deferredSaveRequested = false;
        _physicalFlushPerformed = false;
    }

    private sealed class DeferredSaveScope(CompetitiveUnitOfWork owner, int version) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            owner.EndDeferredSaveScope(version);
            _disposed = true;
        }
    }
}
