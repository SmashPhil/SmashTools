using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace SmashTools.Pathfinding
{
	public class BFS<T>
	{
		private readonly Queue<T> openQueue = new Queue<T>();

		private readonly HashSet<T> visited = new HashSet<T>();

		public bool IsRunning { get; private set; }

		public bool LogRetraceAttempts { get; set; } = false;

		/// <summary>
		/// Halt and clear the BFS traverser
		/// </summary>
		public void Stop()
		{
			openQueue.Clear();
			visited.Clear();
		}

		public void FloodFill(T start, Func<T, IEnumerable<T>> neighbors, Action<T> processor, Func<T, bool> canEnter = null)
		{
			if (IsRunning)
			{
				Log.Error($"Attempting to run FloodFill while it's already in use.");
				return;
			}

			IsRunning = true;
			try
			{
				openQueue.Clear();
				openQueue.Enqueue(start);
				visited.Add(start);
				while (openQueue.Count > 0)
				{
					T current = openQueue.Dequeue();
					processor.Invoke(current);
					foreach (T neighbor in neighbors(current))
					{
						if (visited.Contains(neighbor))
						{
							if (LogRetraceAttempts) SmashLog.Error($"Attempting to open closed node {neighbor}. Skipping to avoid infinite loop.");
							continue;
						}
						if (canEnter == null || canEnter(neighbor))
						{
							visited.Add(neighbor);
							openQueue.Enqueue(neighbor);
						}
					}
				}
			}
			catch (Exception ex)
			{
				SmashLog.Error($"Exception thrown while performing BFS FloodFill.\n{ex.Message}");
			}
			finally
			{
				IsRunning = false;
				Stop();
			}
		}

		public List<T> FloodFill(T start, Func<T, IEnumerable<T>> neighbors, Func<T, bool> canEnter = null)
		{
			List<T> nodes = new List<T>();
			FloodFill(start, neighbors, canEnter: canEnter, processor: delegate (T current)
			{
				nodes.Add(current);
			});
			return nodes;
		}
	}
}
