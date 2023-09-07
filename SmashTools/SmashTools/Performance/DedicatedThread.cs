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

		private readonly ConcurrentQueue<AsyncAction> queue;

		public DedicatedThread(int id, ThreadType type)
		{
			this.id = id;
			this.type = type;
			ShouldExit = false;

			queue = new ConcurrentQueue<AsyncAction>();
			thread = new Thread(Execute);
			thread.Start();
		}

		private bool ShouldExit { get; set; }

		public void Queue(AsyncAction action)
		{
			queue.Enqueue(action);
		}

		internal void Stop()
		{
			ShouldExit = true;
		}

		private void Execute()
		{
			while (!ShouldExit)
			{
				if (queue.TryDequeue(out AsyncAction asyncAction))
				{
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
					AsyncPool<AsyncAction>.Return(asyncAction);
				}
				while (queue.Count == 0) Thread.Sleep(1);
			}
		}

		public enum ThreadType
		{ 
			Single,
			Shared
		}
	}
}
