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
    public UpdateLoop update;

    private readonly ConcurrentQueue<AsyncAction> queue;

    private bool shouldExit;

    public delegate void UpdateLoop();

    public DedicatedThread(int id, ThreadType type)
    {
      this.id = id;
      this.type = type;

      queue = new ConcurrentQueue<AsyncAction>();
      thread = new Thread(Execute)
      {
        IsBackground = true
      };
      thread.Start();
    }

    public bool Terminated { get; private set; }

    public bool InLongOperation { get; private set; }

    /// <summary>
    /// Halt new actions from being queued in this thread while still executing existing queue.
    /// </summary>
    /// <remarks>Use when thread needs to be halted temporarily.</remarks>
    public bool Suspended { get; set; }

    /// <summary>
    /// Remaining actions to be executed in this thread.
    /// </summary>
    public int QueueCount => queue.Count;

    /// <summary>
    /// For Debugging purposes only. Allows reading of action queue with moment-in-time snapshot.
    /// </summary>
    internal IEnumerator<AsyncAction> GetEnumerator() => queue.GetEnumerator();

    public void Queue(AsyncAction action)
    {
      if (Suspended)
      {
        Log.Error($"Thread {id} has been queued an item while suspended. It will not execute.");
        return;
      }
      queue.Enqueue(action);
    }

    internal void Stop()
    {
      shouldExit = true;
    }

    private void Execute()
    {
      while (!shouldExit)
      {
        if (queue.TryDequeue(out AsyncAction asyncAction))
        {
          if (asyncAction is { IsValid: true })
          {
            try
            {
              InLongOperation = asyncAction.LongOperation;
              asyncAction.Invoke();
            }
            catch (Exception ex)
            {
              Log.Error($"Exception thrown while executing {asyncAction} on DedicatedThread #{id:D3}.\nException={ex}");
              asyncAction.ExceptionThrown(ex);
            }
          }
          asyncAction.ReturnToPool();
        }
        // Prioritize running queue over update loop
        do
        {
          if (!Suspended) update?.Invoke();
          Thread.Sleep(10);
        }
        while (!shouldExit && queue.Count == 0);
      }
      Terminated = true;
    }

    public enum ThreadType
    {
      Single,
      Shared
    }
  }
}
