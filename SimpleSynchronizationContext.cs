using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DW2IDE;

public class SimpleSynchronizationContext : SynchronizationContext {

  private readonly object _executeLock = new();

  private readonly ManualResetEventSlim _workAvailable = new(false);

  private readonly CancellationTokenSource _cts = new();

  private readonly ConcurrentQueue<Tuple<SendOrPostCallback, object?, TaskCompletionSource?>> _queue = new();

  public CancellationToken CancellationToken => _cts.Token;

  public bool IsCancellationRequested => _cts.IsCancellationRequested;

  public void Cancel() => _cts.Cancel();

  public override void Post(SendOrPostCallback d, object? state) {
    _queue.Enqueue(Tuple.Create(d, state, (TaskCompletionSource?)null));
    _workAvailable.Set();
  }

  public override void Send(SendOrPostCallback d, object? state) {
    var tcs = new TaskCompletionSource();
    _queue.Enqueue(Tuple.Create(d, state, tcs)!);
    _workAvailable.Set();
    // ReSharper disable once MethodSupportsCancellation
    tcs.Task.Wait();
  }

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