using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Verse;

namespace SmashTools.Performance
{
	/// <summary>
	/// Thread creation and management for dedicated threads that run continuously until released
	/// </summary>
	/// <remarks>This is for dedicated threads only. For long running tasks that don't occur very frequently, use <seealso cref="ThreadPool"/> or <seealso cref="Task"/></remarks>
	public static class ThreadManager
	{
		private static int nextId = 0;

		private static readonly DedicatedThread[] threads = new DedicatedThread[sbyte.MaxValue];

		/// <summary>
		/// Create new <see cref="DedicatedThread"/> for asynchronous 
		/// </summary>
		/// <returns>DedicatedThread instance</returns>
		public static DedicatedThread CreateNew()
		{
			ThreadPool.GetAvailableThreads(out int workerThreads, out int _);
			if (nextId < 0 && workerThreads == 0)
			{
				Log.Error($"Attempting to create more dedicated threads than allowed.");
				return null;
			}
			DedicatedThread dedicatedThread = new DedicatedThread(nextId);
			threads[nextId] = dedicatedThread;
			FindNextUsableId();
			return dedicatedThread;
		}

		public static bool Release(int id)
		{
			if (threads[id] != null)
			{
				threads[id].Stop();
				threads[id] = null; //remove from array and allow GC to clean up
				FindNextUsableId();
				return true;
			}
			return false;
		}

		public static bool Release(this DedicatedThread dedicatedThread)
		{
			dedicatedThread.Stop();
			threads[dedicatedThread.id] = null; //remove from array and allow GC to clean up
			FindNextUsableId();
			return true;
		}

		private static void FindNextUsableId()
		{
			for (int i = 0; i < sbyte.MaxValue; i++)
			{
				if (threads[i] is null)
				{
					nextId = i;
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
	}
}
