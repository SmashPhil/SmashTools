using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Verse;

namespace SmashTools.Performance;

public class DedicatedThread
{
  public readonly Thread thread;
  public readonly int id;
  public readonly ThreadType type;

  private bool isSuspended;
  private bool shouldExit;
  private readonly ManualResetEventSlim waitHandle;
  private readonly ConcurrentQueue<AsyncAction> queue;
  private readonly CancellationTokenSource cts;

  public DedicatedThread(int id, ThreadType type)
  {
    this.id = id;
    this.type = type;
    cts = new CancellationTokenSource();

    waitHandle = new ManualResetEventSlim(false);
    queue = [];
    thread = new Thread(Execute)
    {
      IsBackground = true,
      Priority = ThreadPriority.BelowNormal,
    };
    thread.Start();
  }

  public int QueueCount => queue.Count;

  public bool Terminated { get; private set; }

  public bool IsBlocked => !waitHandle.IsSet;

  /// <summary>
  /// Halt new actions from being enqueued and block thread execution.
  /// </summary>
  public bool IsSuspended
  {
    get { return isSuspended; }
    set
    {
      isSuspended = value;
      if (!isSuspended)
      {
        UnpauseConsumer();
      }
    }
  }

  /// <summary>
  /// For Debugging purposes only. Allows reading of action queue with moment-in-time snapshot.
  /// </summary>
  internal void Snapshot(List<AsyncAction> items)
  {
    items.AddRange(queue);
  }

  /// <summary>
  /// Enqueue action to queue for execution and unblock the thread worker.
  /// </summary>
  public void Enqueue(AsyncAction action)
  {
    if (IsSuspended)
    {
      Trace.Fail($"Thread {id} has been queued an item while suspended. It will not execute.");
      return;
    }

    queue.Enqueue(action);
    UnpauseConsumer();
  }

  /// <summary>
  /// Enqueue item to action queue without unblocking thread worker.
  /// </summary>
  /// <remarks>Should not be used outside of debugging contexts.</remarks>
  internal void EnqueueSilently(AsyncAction action)
  {
    queue.Enqueue(action);
  }

  private void UnpauseConsumer()
  {
    waitHandle.Set();
  }

  /// <summary>
  /// Thread will finish executing the rest of the queue and then terminate
  /// </summary>
  public void Stop()
  {
    shouldExit = true;
    // Unblock consumer so we can exit the loop and terminate
    if (!Terminated)
      waitHandle.Set();
  }

  /// <summary>
  /// Stops thread execution as soon as possible, discarding any remaining queued actions
  /// </summary>
  public void StopImmediately()
  {
    cts.Cancel();
    Stop();
  }

  private void Execute()
  {
    try
    {
      while (!shouldExit)
      {
        waitHandle.Wait();
        waitHandle.Reset();

        while (!cts.IsCancellationRequested && !IsSuspended &&
          queue.TryDequeue(out AsyncAction asyncAction))
        {
          try
          {
            if (asyncAction.IsValid)
              asyncAction.Invoke();
          }
          catch (Exception ex)
          {
            Log.Error($"Exception thrown while executing {asyncAction} on DedicatedThread " +
              $"#{id:D3}.\nException={ex}");
            asyncAction.ExceptionThrown(ex);
          }
          finally
          {
            asyncAction.ReturnToPool();
          }
        }
      }
    }
    catch (Exception ex)
    {
      Log.Error($"Exception thrown from thread={thread.ManagedThreadId}.\n{ex}");
    }
    finally
    {
      // Release all resources currently waiting on this event handle
      waitHandle.Dispose();
      Terminated = true;
    }
  }

  public enum ThreadType
  {
    Single,
    Shared
  }
}