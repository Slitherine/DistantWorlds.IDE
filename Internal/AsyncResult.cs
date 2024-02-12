using System.Diagnostics;

namespace DistantWorlds.IDE;

public class AsyncResult : IAsyncResult {

    private readonly AsyncCallback? _callback;

    private ManualResetEvent? _waitHandle;

    private readonly object _lock = new();

    public AsyncResult(AsyncCallback? callback, object? state) {
        _callback = callback;
        AsyncState = state;
    }

    public object? AsyncState { get; }

    public WaitHandle AsyncWaitHandle
        => _waitHandle ??= new(IsCompleted);

    public bool CompletedSynchronously
        => false;

    public bool IsCompleted { get; private set; }

    public void Complete() {
        IsCompleted = true;
        _waitHandle?.Set();
        _callback?.Invoke(this);
    }

    public bool Wait(int millisecondsTimeout = Timeout.Infinite) {
        if (IsCompleted)
            return true;

        if (millisecondsTimeout == 0)
            return IsCompleted;

        if (_waitHandle != null) {
            _waitHandle.WaitOne(millisecondsTimeout);
            return IsCompleted;
        }

        // never allocate a _waitHandle, just use the lock
        lock (_lock) {
            var remainingTimeout = millisecondsTimeout;
            while (!IsCompleted) {
                var started = Stopwatch.GetTimestamp();
                var success = Monitor.Wait(_lock, remainingTimeout);
                if (!success) return IsCompleted;

                var elapsed = Stopwatch.GetElapsedTime(started);

                remainingTimeout -= (int)Math.Ceiling(elapsed.TotalMilliseconds);
                if (remainingTimeout <= 0) return IsCompleted;
            }

            return IsCompleted;
        }
    }

}