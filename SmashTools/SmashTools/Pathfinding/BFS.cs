using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmashTools.Pathfinding
{
	public class BFS<T>
	{
		private readonly Queue<T> openQueue = new Queue<T>();

		private readonly Dictionary<T, T> backTracker = new Dictionary<T, T>();

		private readonly Func<T, bool> canEnter;
		private readonly Func<T, IEnumerable<T>> neighbors;

		public BFS(IPathfinder<T> pathfinder)
		{
			canEnter = pathfinder.CanEnter;
			neighbors = pathfinder.Neighbors;
		}

		public BFS(Func<T, IEnumerable<T>> neighbors, Func<T, bool> canEnter = null)
		{
			this.neighbors = neighbors;
			this.canEnter = canEnter;
		}

		public bool IsRunning { get; private set; }

		public bool LogRetraceAttempts { get; set; } = false;

		/// <summary>
		/// Halt and clear the BFS traverser
		/// </summary>
		public void Stop()
		{
			openQueue.Clear();
			backTracker.Clear();
		}

		/// <summary>
		/// Breadth First Traversal search algorithm
		/// </summary>
		public List<T> Run(T start, T destination)
		{
			IsRunning = true;
			try
			{
				openQueue.Clear();
				openQueue.Enqueue(start);

				while (openQueue.Count > 0)
				{
					T current = openQueue.Dequeue();
					foreach (T neighbor in neighbors(current))
					{
						if (backTracker.ContainsKey(neighbor))
						{
							if (LogRetraceAttempts) SmashLog.Error($"Attempting to open closed node {neighbor}. Skipping to avoid infinite loop.");
							continue;
						}
						if (canEnter is null || canEnter(neighbor))
						{
							backTracker[neighbor] = current;
							if (neighbor.Equals(destination))
							{
								Stop();
								return SolvePath(start, destination);
							}
							openQueue.Enqueue(neighbor);
						}
					}
				}
			}
			catch (Exception ex)
			{
				SmashLog.Error($"Exception thrown while performing BFS search. Exception = {ex}");
			}
			finally
			{
				IsRunning = false;
			}
			return null;
		}

		private List<T> SolvePath(T start, T destination)
		{
			List<T> result = new List<T>();

			T current = destination;
			while (!start.Equals(current))
			{
				result.Add(current);
				current = backTracker[current];
			}
			result.Add(start);
			result.Reverse();

			if (!result[0].Equals(start))
			{
				SmashLog.Error($"BFS was unable to solve path from {start} to {destination}.");
			}
			return result;
		}
	}
}
