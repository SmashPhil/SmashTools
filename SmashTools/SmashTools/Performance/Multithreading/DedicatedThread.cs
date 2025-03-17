using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using JetBrains.Annotations;
using Verse;

namespace SmashTools.Performance
{
  public class DedicatedThread
  {
    public readonly Thread thread;
    public readonly int id;
    public readonly ThreadType type;

    private bool isSuspended;
    private bool shouldExit;
    private readonly ManualResetEvent pauseConsumer;
    private readonly ConcurrentQueue<AsyncAction> queue;
    private readonly CancellationTokenSource cts;

    public DedicatedThread(int id, ThreadType type)
    {
      this.id = id;
      this.type = type;
      cts = new CancellationTokenSource();

      pauseConsumer = new ManualResetEvent(false);
      queue = [];
      thread = new Thread(Execute)
      {
        IsBackground = true,
        Priority = ThreadPriority.BelowNormal,
      };
      thread.Start();
    }

    internal int QueueCount => queue.Count;

    public bool Terminated { get; private set; }

    internal bool IsBlocked => !pauseConsumer.WaitOne(0);

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
          TryUnpauseConsumer();
        }
      }
    }

    /// <summary>
    /// For Debugging purposes only. Allows reading of action queue with moment-in-time snapshot.
    /// </summary>
    [Conditional("DEBUG")]
    internal void Snapshot(List<AsyncAction> items)
    {
      items.AddRange(queue);
    }

    /// <summary>
    /// Enqueue action to queue for execution and unblock the consumer thread.
    /// </summary>
    public void Enqueue(AsyncAction action)
    {
      if (IsSuspended)
      {
        Trace.Fail($"Thread {id} has been queued an item while suspended. It will not execute.");
        return;
      }

      queue.Enqueue(action);
      bool unpaused = TryUnpauseConsumer();
      Trace.IsTrue(unpaused, "Unable to resume thread with item added to queue.");
    }

    /// <summary>
    /// Enqueue item to action queue without unblocking consumer thread.
    /// </summary>
    /// <remarks>Should not be used outside of debugging contexts.</remarks>
    internal void EnqueueSilently(AsyncAction action)
    {
      queue.Enqueue(action);
    }

    internal bool TryUnpauseConsumer()
    {
      if (queue.Count == 0) return false;

      pauseConsumer.Set();
      return true;
    }

    /// <summary>
    /// Thread will finish executing the rest of the queue and then terminate
    /// </summary>
    [UsedImplicitly]
    internal void Stop()
    {
      shouldExit = true;
      // Unblock consumer so we can exit the loop and terminate
      Assert.IsTrue(Terminated == pauseConsumer.SafeWaitHandle.IsClosed);
      if (!pauseConsumer.SafeWaitHandle.IsClosed)
      {
        pauseConsumer.Set();
      }
    }

    /// <summary>
    /// Stops thread execution as soon as possible, discarding any remaining queued actions
    /// </summary>
    internal void StopImmediately()
    {
      cts.Cancel();
      Stop();
    }

    private void Execute()
    {
      while (!shouldExit)
      {
        bool blocked = pauseConsumer.WaitOne();
        pauseConsumer.Reset();

        // Should always be resuming from a blocked state at this point in time. The idea is to
        // achieve BlockingCollection-like behavior that waits for an item to enqueue before
        // resuming execution, rather than polling constantly like an idiot.
        Assert.IsTrue(blocked);

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

      Terminated = true;
      // Release all resources currently waiting on this event handle
      pauseConsumer.Close();
    }

    public enum ThreadType
    {
      Single,
      Shared
    }
  }
}