using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace DistantWorlds.IDE;

public class SimpleSynchronizationContext : SynchronizationContext {

    private readonly object _executeLock = new();

    private readonly ManualResetEventSlim _workAvailable = new(false);

    private readonly CancellationTokenSource _cts = new();

    private readonly ConcurrentQueue<Tuple<SendOrPostCallback, object?, TaskCompletionSource?>> _queue = new();

    public CancellationToken CancellationToken => _cts.Token;

    public bool IsCancellationRequested => _cts.IsCancellationRequested;

    public void Cancel() {
        try {
            _cts.Cancel();
        } catch {
            // ignored
        }
    }

    public override void Post(SendOrPostCallback d, object? state) {
        _queue.Enqueue(Tuple.Create(d, state, (TaskCompletionSource?)null));
        _workAvailable.Set();
    }

    public override void Send([InstantHandle]SendOrPostCallback d, object? state) {
        var tcs = new TaskCompletionSource();
        _queue.Enqueue(Tuple.Create(d, state, tcs)!);
        _workAvailable.Set();
        // ReSharper disable once MethodSupportsCancellation
        tcs.Task.ConfigureAwait(true).GetAwaiter().GetResult();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Send([InstantHandle]Action action)
        => Send(_ => action(), null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Send<T>([InstantHandle]Action<T> action, T state)
        => Send((SendOrPostCallback)(s => action((T)s!)), state);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Send<T1, T2>([InstantHandle]Action<T1, T2> action, T1 state1, T2 state2)
        => Send(tuple => action(tuple.state1, tuple.state2), (state1, state2));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Send<T1, T2, T3>([InstantHandle]Action<T1, T2, T3> action, T1 state1, T2 state2, T3 state3)
        => Send(tuple => action(tuple.state1, tuple.state2, tuple.state3), (state1, state2, state3));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Send<T1, T2, T3, T4>([InstantHandle]Action<T1, T2, T3, T4> action, T1 state1, T2 state2, T3 state3, T4 state4)
        => Send(tuple => action(tuple.state1, tuple.state2, tuple.state3, tuple.state4),
            (state1, state2, state3, state4));

    public TResult Send<TResult>([InstantHandle]Func<TResult> func) {
        TResult result = default!;
        Send(_ => result = func(), null);
        return result;
    }

    public TResult Send<T, TResult>([InstantHandle]Func<T, TResult> func, T state) {
        TResult result = default!;
        Send((SendOrPostCallback)(s => result = func((T)s!)), state);
        return result;
    }

    public TResult Send<T1, T2, TResult>([InstantHandle]Func<T1, T2, TResult> func, T1 state1, T2 state2)
        => Send(tuple => func(tuple.state1, tuple.state2), (state1, state2));

    public TResult Send<T1, T2, T3, TResult>([InstantHandle]Func<T1, T2, T3, TResult> func, T1 state1, T2 state2, T3 state3)
        => Send(tuple => func(tuple.state1, tuple.state2, tuple.state3), (state1, state2, state3));

    public TResult Send<T1, T2, T3, T4, TResult>([InstantHandle]Func<T1, T2, T3, T4, TResult> func, T1 state1, T2 state2, T3 state3,
        T4 state4)
        => Send(tuple => func(tuple.state1, tuple.state2, tuple.state3, tuple.state4),
            (state1, state2, state3, state4));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Post(Action action)
        => Post(_ => action(), null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Post<T>(Action<T> action, T state)
        => Post((SendOrPostCallback)((s) => action((T)s!)), state);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Post<T1, T2>(Action<T1, T2> action, T1 state1, T2 state2)
        => Post(tuple => action(tuple.state1, tuple.state2), (state1, state2));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Post<T1, T2, T3>(Action<T1, T2, T3> action, T1 state1, T2 state2, T3 state3)
        => Post(tuple => action(tuple.state1, tuple.state2, tuple.state3), (state1, state2, state3));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Post<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 state1, T2 state2, T3 state3, T4 state4)
        => Post(tuple => action(tuple.state1, tuple.state2, tuple.state3, tuple.state4),
            (state1, state2, state3, state4));

    [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods")]
    public Task Run(SendOrPostCallback d, object? state) {
        var tcs = new TaskCompletionSource();
        _queue.Enqueue(Tuple.Create(d, state, tcs)!);
        _workAvailable.Set();
        return tcs.Task;
    }

    public bool WaitForWork(int timeout) {
        try {
            var result = _workAvailable.Wait(timeout, CancellationToken);
            if (result) _workAvailable.Reset();
            return result;
        }
        catch {
            return false;
        }
    }

#if DEBUG
    public int ManagedThreadId => Environment.CurrentManagedThreadId;
#endif

    public bool ExecuteQueue() {
#if DEBUG
        Debug.Assert(Environment.CurrentManagedThreadId == ManagedThreadId);
#endif
        lock (_executeLock) {
            var ran = !_queue.IsEmpty;

            while (_queue.TryDequeue(out var work)) {
                var (cb, state, tcs) = work;
                try { cb.Invoke(state); }
                catch (OperationCanceledException oce) {
                    tcs?.SetCanceled(oce.CancellationToken);
                    tcs = null;
                }
                catch (Exception ex) {
                    tcs?.SetException(ex);
                    tcs = null;
                }

                tcs?.SetResult();
            }

            return ran;
        }
    }

}