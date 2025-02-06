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

		private bool shouldExit = false;

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

		public bool Terminated { get; private set; } = false;

		public bool InLongOperation { get; private set; } = false;

		public int QueueCount => queue.Count;

		/// <summary>
		/// For Debugging purposes only. Allows reading of action queue with moment-in-time snapshot.
		/// </summary>
		internal IEnumerator<AsyncAction> GetEnumerator() 
		{
			return queue.GetEnumerator();
		}

		public bool Queue(AsyncAction action)
		{
			queue.Enqueue(action);
			return true;
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
				//Prioritize running queue over update loop
				while (!shouldExit && queue.Count == 0)
				{
					update?.Invoke();
					Thread.Sleep(10);
				}
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
