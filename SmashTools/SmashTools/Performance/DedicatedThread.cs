using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Verse;

namespace SmashTools.Performance
{
	public class DedicatedThread
	{
		public readonly Thread thread;
		public readonly int id;

		private readonly Queue<AsyncAction> queue;

		private object queueLock = new object();

		public DedicatedThread(int id)
		{
			this.id = id;

			queue = new Queue<AsyncAction>();
			thread = new Thread(Execute);
			thread.Start();
		}

		private bool ShouldExit { get; set; } = false;

		public void Queue(AsyncAction action)
		{
			lock (queueLock)
			{
				queue.Enqueue(action);
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
				lock (queueLock)
				{
					if (queue.Count > 0)
					{
						AsyncAction asyncAction = queue.Dequeue();
						if (asyncAction.IsValid)
						{
							try
							{
								asyncAction.Invoke();
							}
							catch (Exception ex)
							{
								Log.Error($"Exception thrown while executing {asyncAction} on DedicatedThread #{id:D3}.\nException={ex}");
							}
						}
					}
				}
			}
		}
	}
}
