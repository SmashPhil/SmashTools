using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Verse;

namespace SmashTools.Performance
{
	public class DedicatedThread
	{
		public readonly Thread thread;
		public readonly int id;
		public readonly ThreadType type;

		private readonly Queue<AsyncAction> queue;

		private object queueLock = new object();

		public DedicatedThread(int id, ThreadType type)
		{
			this.id = id;

			ShouldExit = false;

			queue = new Queue<AsyncAction>();
			thread = new Thread(Execute);
			thread.Start();
		}

		private bool ShouldExit { get; set; }

		private int Count { get; set; }

		public void Queue(AsyncAction action)
		{
			lock (queueLock)
			{
				queue.Enqueue(action);
				Count = queue.Count;
			}
		}

		internal void Stop()
		{
			lock (queueLock)
			{
				queue.Clear();
			}
			ShouldExit = true;
		}

		private void Execute()
		{
			while (!ShouldExit)
			{
				AsyncAction asyncAction = null;
				lock (queueLock)
				{
					if (queue.Count > 0)
					{
						asyncAction = queue.Dequeue();
					}
					Count = queue.Count;
				}
				if (asyncAction != null && asyncAction.IsValid)
				{
					try
					{
						asyncAction.Invoke();
					}
					catch (Exception ex)
					{
						Log.Error($"Exception thrown while executing {asyncAction} on DedicatedThread #{id:D3}.\nException={ex}");
						if (asyncAction.exceptionHandler != null)
						{
							asyncAction.exceptionHandler(ex);
						}
					}
				}
				while (Count == 0) Thread.Sleep(1);
			}
		}

		public enum ThreadType
		{ 
			Single,
			Shared
		}
	}
}
