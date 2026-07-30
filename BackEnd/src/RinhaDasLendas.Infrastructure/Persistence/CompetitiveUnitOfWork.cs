using RinhaDasLendas.Domain.Repositories;

namespace RinhaDasLendas.Infrastructure.Persistence;

public sealed class CompetitiveUnitOfWork(RinhaDasLendasDbContext dbContext) : ICompetitiveUnitOfWork
{
    private bool _deferredSaveActive;
    private bool _deferredSaveRequested;
    private bool _physicalFlushPerformed;
    private int _scopeVersion;

    internal bool DeferredSaveRequested => _deferredSaveRequested;

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        if (!_deferredSaveActive)
        {
            return dbContext.SaveChangesAsync(cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();
        _deferredSaveRequested = true;
        return Task.CompletedTask;
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

    internal Task FlushDeferredChangesAsync(CancellationToken cancellationToken)
    {
        if (!_deferredSaveActive || _physicalFlushPerformed)
        {
            throw new InvalidOperationException("The deferred competitive save scope cannot be flushed.");
        }

        _physicalFlushPerformed = true;
        return dbContext.SaveChangesAsync(cancellationToken);
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
