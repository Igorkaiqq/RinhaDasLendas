using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Domain.Exceptions;

namespace RinhaDasLendas.Api.Middleware;

internal sealed class BoundedWriteStream(int maximumLength) : Stream
{
    private readonly MemoryStream _inner = new();

    public byte[] ToArray() => _inner.ToArray();

    public override bool CanRead => _inner.CanRead;
    public override bool CanSeek => _inner.CanSeek;
    public override bool CanWrite => _inner.CanWrite;
    public override long Length => _inner.Length;
    public override long Position
    {
        get => _inner.Position;
        set => _inner.Position = value;
    }

    public override void Flush() => _inner.Flush();
    public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);
    public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
    public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

    public override void SetLength(long value)
    {
        EnsureWithinLimit(value);
        _inner.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        EnsureWriteWithinLimit(count);
        _inner.Write(buffer, offset, count);
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        EnsureWriteWithinLimit(buffer.Length);
        _inner.Write(buffer);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        EnsureWriteWithinLimit(count);
        return _inner.WriteAsync(buffer, offset, count, cancellationToken);
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        EnsureWriteWithinLimit(buffer.Length);
        return _inner.WriteAsync(buffer, cancellationToken);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _inner.Dispose();
        }
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        await _inner.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    private void EnsureWriteWithinLimit(int count)
    {
        long resultingLength;
        try
        {
            resultingLength = Math.Max(Length, checked(Position + count));
        }
        catch (OverflowException)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }
        EnsureWithinLimit(resultingLength);
    }

    private void EnsureWithinLimit(long length)
    {
        if (maximumLength <= 0 || length > maximumLength)
        {
            throw new DomainException(MessageCodes.ValidationError);
        }
    }
}
