using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;
using Verse;

namespace SmashTools.Performance;

public class DedicatedThread
{
  public readonly Thread thread;
  public readonly int id;
  public readonly ThreadType type;

  private bool isSuspended;
  private bool shouldExit;
  private readonly ManualResetEvent waitHandle;
  private readonly ConcurrentQueue<AsyncAction> queue;
  private readonly CancellationTokenSource cts;

  public DedicatedThread(int id, ThreadType type)
  {
    this.id = id;
    this.type = type;
    cts = new CancellationTokenSource();

    waitHandle = new ManualResetEvent(false);
    queue = [];
    thread = new Thread(Execute)
    {
      IsBackground = true,
      Priority = ThreadPriority.BelowNormal,
    };
    thread.Start();
  }

  internal int QueueCount => queue.Count;

  public bool Terminated => waitHandle.SafeWaitHandle.IsClosed;

  internal bool IsBlocked => !waitHandle.WaitOne(0);

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
    if (queue.Count == 0) return;

    if (!waitHandle.Set())
      Trace.Fail("Unable to resume thread with item added to queue.");
  }

  /// <summary>
  /// Thread will finish executing the rest of the queue and then terminate
  /// </summary>
  [UsedImplicitly]
  public void Stop()
  {
    shouldExit = true;
    // Unblock consumer so we can exit the loop and terminate
    if (!Terminated)
    {
      waitHandle.Set();
    }
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
    while (!shouldExit)
    {
      bool signalReceived = waitHandle.WaitOne();
      waitHandle.Reset();

      // Should always be resuming from a blocked state at this point in time. The idea is to
      // achieve BlockingCollection-like behavior that waits for an item to enqueue before
      // resuming execution, rather than polling constantly like an idiot.
      Assert.IsTrue(signalReceived);

      while (queue.Count > 0 && !cts.IsCancellationRequested && !IsSuspended)
      {
        if (!queue.TryDequeue(out AsyncAction asyncAction) || !asyncAction.IsValid)
          continue;

        try
        {
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

    // Release all resources currently waiting on this event handle
    waitHandle.Close();
  }

  public enum ThreadType
  {
    Single,
    Shared
  }
}