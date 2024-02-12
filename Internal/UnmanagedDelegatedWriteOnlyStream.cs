namespace DistantWorlds.IDE;

public sealed partial class UnmanagedDelegatedWriteOnlyStream : Stream {

    private readonly object _lock = new();

    private unsafe delegate * unmanaged[Cdecl]<void*, void*, nuint, void> _write;

    private readonly unsafe void* _state;

    public unsafe UnmanagedDelegatedWriteOnlyStream(
        delegate* unmanaged[Cdecl]<void*, void*, nuint, void> write,
        void* state) {
        _write = write;
        _state = state;
    }

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => throw new NotSupportedException();

    public override long Position {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush() {
        Monitor.Enter(_lock);
        Monitor.Exit(_lock);
    }

    public override int Read(byte[] buffer, int offset, int count)
        => throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin origin)
        => throw new NotSupportedException();

    public override void SetLength(long value)
        => throw new NotSupportedException();

    public override unsafe void Write(byte[] buffer, int offset, int count) {
        lock (_lock) {
            fixed (byte* pBuffer = buffer)
                _write(_state, pBuffer + offset, (nuint)count);
        }
    }

    public override unsafe void WriteByte(byte value) {
        lock (_lock)
            _write(_state, &value, 1);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        var ctx = new WriteAsyncContext(this, buffer, offset, count, cancellationToken);

        ThreadPool.UnsafeQueueUserWorkItem(static ctx => {
            ctx.Stream.Write(ctx.Buffer, ctx.Offset, ctx.Count);
            ctx.CompletionSource.TrySetResult();
        }, ctx, true);
        return ctx.CompletionSource.Task;
    }

    protected override void Dispose(bool disposing)
        => Close();

    public override unsafe void Close() {
        Flush();
        _write = null!;
    }

    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        => throw new NotSupportedException();

    public override IAsyncResult BeginWrite(byte[] buffer,
        int offset, int count,
        AsyncCallback? callback,
        object? state) {
        var ar = new AsyncResultImpl(this, buffer, offset, count, callback, state);
        ThreadPool.UnsafeQueueUserWorkItem(static ar => {
            ar.Stream.Write(ar.Buffer, ar.Offset, ar.Count);
            ar.Complete();
        }, ar, true);
        return ar;
    }

    public override void EndWrite(IAsyncResult asyncResult) {
        if (asyncResult is not AsyncResultImpl ar)
            throw new ArgumentException("Invalid async result.", nameof(asyncResult));

        if (ar.IsCompleted)
            return;

        do ar.Wait();
        while (!ar.IsCompleted);
    }

}