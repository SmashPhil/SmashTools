using System.Collections.Generic;
using Verse;

namespace SmashTools.Performance
{
  /// <summary>
  /// Thread creation and management for dedicated threads that run continuously until released.
  /// <para />
  /// Useful for continuously running background calculations such as updating map or world grids.
  /// </summary>
  public static class ThreadManager
  {
    public const sbyte MaxThreads = 20;
    public const byte MaxPooledThreads = 20;

    private static int nextId = 0;

    private static readonly DedicatedThread[] threads = new DedicatedThread[MaxThreads + MaxPooledThreads];

    private static readonly ushort[] pooledThreadCounts = new ushort[MaxPooledThreads];

    private static readonly List<DedicatedThread> activeThreads = new List<DedicatedThread>();

    private static object threadListLock = new object();

    // Just reading the _size int, it can't be guaranteed this count isn't stale
    // if being checked immediately after List has been modified.
    public static bool AllThreadsTerminated => activeThreads.Count == 0;

    /// <summary>
    /// Snapshot of active threads.
    /// </summary>
    public static ListSnapshot<DedicatedThread> ThreadsSnapshot
    {
      get
      {
        lock (threadListLock)
        {
          return new ListSnapshot<DedicatedThread>(activeThreads);
        }
      }
    }

    /// <summary>
    /// Create <see cref="DedicatedThread"/> with ownership.
    /// </summary>
    /// <remarks>
    /// Returned thread will be thread type Single. When released it will be immediately disposed.
    /// </remarks>
    public static DedicatedThread CreateNew()
    {
      if (nextId < 0)
      {
        Log.Error($"Attempting to create more dedicated threads than allowed.");
        return null;
      }
      DedicatedThread dedicatedThread = new DedicatedThread(nextId, DedicatedThread.ThreadType.Single);
      lock (threadListLock)
      {
        activeThreads.Add(dedicatedThread);
        threads[nextId] = dedicatedThread;
        FindNextUsableId();
      }
      return dedicatedThread;
    }

    /// <summary>
    /// Create or fetch <see cref="DedicatedThread"/> with shared ownership.
    /// </summary>
		/// <remarks>
		/// When all owners have released the thread, it will be disposed.
		/// </remarks>
    public static DedicatedThread GetShared(int id)
    {
      if (id < 0 || id > MaxPooledThreads)
      {
        Log.Error($"Attempting to get shared Thread with invalid id.");
        return null;
      }
      int index = id + MaxThreads;
      lock (threadListLock)
      {
        if (threads[index] == null)
        {
          DedicatedThread dedicatedThread = new DedicatedThread(index, DedicatedThread.ThreadType.Shared);
          threads[index] = dedicatedThread;
          activeThreads.Add(dedicatedThread);
        }
        pooledThreadCounts[id]++;
        return threads[index];
      }
    }

    public static bool Release(this DedicatedThread dedicatedThread)
    {
      if (dedicatedThread.type == DedicatedThread.ThreadType.Shared)
      {
        return TryReleaseShared(dedicatedThread);
      }
      DisposeThread(dedicatedThread);
      return true;
    }

    private static bool TryReleaseShared(DedicatedThread dedicatedThread)
    {
      lock (threadListLock)
      {
        int id = dedicatedThread.id;
        pooledThreadCounts[id - MaxThreads]--;
        if (pooledThreadCounts[id - MaxThreads] == 0)
        {
          DisposeThread(dedicatedThread);
          return true;
        }
      }
      return false;
    }

    private static void DisposeThread(DedicatedThread dedicatedThread)
    {
      lock (threadListLock)
      {
        dedicatedThread.Stop();
        activeThreads.Remove(dedicatedThread);
        threads[dedicatedThread.id] = null;
        FindNextUsableId();
      }
    }

    private static void FindNextUsableId()
    {
      for (int i = 0; i < sbyte.MaxValue; i++)
      {
        if (threads[i] is null)
        {
          nextId = i;
          return;
        }
      }
      nextId = -1;
    }

    public static void ReleaseThreadsAndClearCache()
    {
      using ListSnapshot<DedicatedThread> threads = ThreadsSnapshot;

      // Will take a few more clock cycles for thread worker to receive cancellation token and
      // terminate. We must wait until then otherwise MemoryUtility.ClearAllMapsAndWorld will
      // set map fields to null and ongoing operations will throw. This function is executed
      // right before that happens.
      foreach (DedicatedThread dedicatedThread in threads)
      {
        dedicatedThread.Release();
        dedicatedThread.thread.Join();
      }
      ComponentCache.ClearCache();
    }
  }
}
