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

		private bool shouldExit;
		private bool inLongOperation;

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

		public bool InLongOperation { get => inLongOperation; set => inLongOperation = value; }

		public int QueueCount => queue.Count;

		/// <summary>
		/// For Debugging purposes only. Allows reading of action queue with moment-in-time snapshot.
		/// </summary>
		internal IEnumerator<AsyncAction> GetEnumerator() 
		{
			return queue.GetEnumerator();
		}

		public void Queue(AsyncAction action)
		{
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
					if (asyncAction != null && asyncAction.IsValid)
					{
						try
						{
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
				while (queue.Count == 0) Thread.Sleep(15);
			}
		}

		public enum ThreadType
		{ 
			Single,
			Shared
		}
	}
}
