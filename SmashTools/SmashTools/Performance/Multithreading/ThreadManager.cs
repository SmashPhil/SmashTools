using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Verse;
using UnityEngine.SceneManagement;

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
			activeThreads.Add(dedicatedThread);
			threads[nextId] = dedicatedThread;
			FindNextUsableId();
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
			if (threads[index] == null)
			{
				DedicatedThread dedicatedThread = new DedicatedThread(index, DedicatedThread.ThreadType.Shared);
				threads[index] = dedicatedThread;
				activeThreads.Add(dedicatedThread);
			}
			pooledThreadCounts[id]++;
			return threads[index];
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
			int id = dedicatedThread.id;
			pooledThreadCounts[id - MaxThreads]--;
			//Log.Message($"Used by: {pooledThreadCounts[id - MaxThreads]}");
			if (pooledThreadCounts[id - MaxThreads] == 0)
			{
				DisposeThread(dedicatedThread);
				return true;
			}
			return false;
		}

		private static void DisposeThread(DedicatedThread dedicatedThread)
		{
			dedicatedThread.Stop();
			activeThreads.Remove(dedicatedThread);
			threads[dedicatedThread.id] = null;
			FindNextUsableId();
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

		public static bool QueueAsync(int id, AsyncAction action)
		{
			if (threads[id] is null)
			{
				return false;
			}
			threads[id].Queue(action);
			return true;
		}

		internal static void ReleaseAllActiveThreads()
		{
			for (int i = activeThreads.Count - 1; i >= 0; i--)
			{
				DedicatedThread dedicatedThread = activeThreads[i];
				dedicatedThread?.Release();
			}
		}
	}
}
