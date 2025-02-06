using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Verse;
using UnityEngine.SceneManagement;
using System.Collections.ObjectModel;
using HarmonyLib;

namespace SmashTools.Performance
{
	/// <summary>
	/// Thread creation and management for dedicated threads that run continuously until released
	/// </summary>
	/// <remarks>This is for dedicated threads only. For long running tasks that don't occur very frequently, use <seealso cref="ThreadPool"/> or <seealso cref="Task"/></remarks>
	public static class ThreadManager
	{
		public const sbyte MaxThreads = 20;
		public const byte MaxPooledThreads = 20;

		private static int nextId = 0;

		private static readonly DedicatedThread[] threads = new DedicatedThread[MaxThreads + MaxPooledThreads];

		private static readonly ushort[] pooledThreadCounts = new ushort[MaxPooledThreads];

		private static readonly List<DedicatedThread> activeThreads = new List<DedicatedThread>();

		private static object threadListLock = new object();

		public static bool AllThreadsTerminated => activeThreads.Count == 0;

		/// <summary>
		/// Copy for list of active threads.
		/// </summary>
		private static List<DedicatedThread> ThreadsSnapshot
		{
			// DedicatedThreads should not be modified from this endpoint. If I could mark DedicatedThread const here I would,
			// but I don't know of any C# equivalent. This is for snapshotting the current list of active threads to monitor
			// disposal and termination of the thread. There is a slight delay from when a thread is released and when it has
			// finished executing its last operation and can be disposed.
			get
			{
				List<DedicatedThread> threads = new List<DedicatedThread>();
				lock (threadListLock)
				{
					threads.AddRange(activeThreads);
				}
				return threads;
			}
		}

		/// <summary>
		/// Create new <see cref="DedicatedThread"/> for asynchronous 
		/// </summary>
		/// <returns>new DedicatedThread</returns>

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

		internal static void ReleaseAllActiveThreads()
		{
			lock (threadListLock)
			{
				for (int i = activeThreads.Count - 1; i >= 0; i--)
				{
					DedicatedThread dedicatedThread = activeThreads[i];
					dedicatedThread?.Release();
				}
			}
		}

		public static void ReleaseThreadsAndClearCache()
		{
			var threads = ThreadsSnapshot;
			ReleaseAllActiveThreads();
			while (threads.Any(thread => !thread.Terminated))
			{
				// May be a few ms for threads to finish executing their current operation and terminate.
				// We need to wait for full thread termination otherwise MemoryUtility.ClearAllMapsAndWorld
				// will set map fields to null and ongoing operations may throw. This method is executed as
				// a prefix patch on the aformentioned method.
				Thread.Sleep(10);
			}
			ComponentCache.ClearCache();
		}
	}
}
