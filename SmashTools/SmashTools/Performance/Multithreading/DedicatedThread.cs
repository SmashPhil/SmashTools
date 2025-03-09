using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Verse;

namespace SmashTools.Performance
{
  public class DedicatedThread
  {
    public readonly Thread thread;
    public readonly int id;
    public readonly ThreadType type;

    private readonly BlockingCollection<AsyncAction> queue;
    private readonly CancellationTokenSource cts;

    public DedicatedThread(int id, ThreadType type)
    {
      this.id = id;
      this.type = type;
      cts = new CancellationTokenSource();

      queue = [];
      thread = new Thread(Execute)
      {
        IsBackground = true,
        Priority = ThreadPriority.BelowNormal,
      };
      thread.Start();
    }

    public bool Terminated { get; private set; }

    /// <summary>
    /// Halt new actions from being queued in this thread while still executing existing queue.
    /// </summary>
    /// <remarks>Use when thread needs to be halted temporarily.</remarks>
    public bool Suspended { get; set; }

    /// <summary>
    /// For Debugging purposes only. Allows reading of action queue with moment-in-time snapshot.
    /// </summary>
    internal void Snapshot(List<AsyncAction> items)
    {
      items.AddRange(queue);
    }

    public void Queue(AsyncAction action)
    {
      if (Suspended)
      {
        Log.Error($"Thread {id} has been queued an item while suspended. It will not execute.");
        return;
      }
      queue.Add(action);
    }

    internal void Stop()
    {
      cts.Cancel();
    }

    private void Execute()
    {
      try
      {
        foreach (AsyncAction asyncAction in queue.GetConsumingEnumerable(cts.Token))
        {
          if (!asyncAction.IsValid) continue;

          try
          {
            asyncAction.Invoke();
          }
          catch (Exception ex)
          {
            Log.Error($"Exception thrown while executing {asyncAction} on DedicatedThread #{id:D3}.\nException={ex}");
            asyncAction.ExceptionThrown(ex);
          }
          finally
          {
            asyncAction.ReturnToPool();
          }
        }
      }
      catch (OperationCanceledException)
      {
      }
      Terminated = true;
    }

    public enum ThreadType
    {
      Single,
      Shared
    }

    public enum ThreadStatus
    {
      Idle,
      Slow,
      Busy,
    }
  }
}
