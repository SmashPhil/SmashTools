using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace SmashTools.Pathfinding
{
	public class Dijkstra<T>
	{
		private readonly PriorityQueue<T, int> openQueue = new PriorityQueue<T, int>();
		private readonly Dictionary<T, Node> nodes = new Dictionary<T, Node>();

		private readonly Func<T, bool> canEnter;
		private readonly Func<T, IEnumerable<T>> neighbors;
		private readonly Func<T, T, int> cost;

		public bool LogRetraceAttempts { get; set; } = false;

		public Dijkstra(IPathfinder<T> pathfinder)
		{
			cost = pathfinder.Cost;
			canEnter = pathfinder.CanEnter;
			neighbors = pathfinder.Neighbors;
		}

		public Dijkstra(Func<T, T, int> cost, Func<T, List<T>> neighbors, Func<T, bool> canEnter = null)
		{
			this.cost = cost;
			this.neighbors = neighbors;
			this.canEnter = canEnter;
		}
		
		public List<T> Run(T start, T destination)
		{
			while (openQueue.Count > 0)
			{
				if (!openQueue.TryDequeue(out T current, out int priority))
				{
					goto PathNotFound;
				}
				if (current.Equals(destination))
				{
					return SolvePath(start, destination); //SOLVE PATH
				}
				foreach (T neighbor in neighbors(current))
				{
					if (canEnter != null && !canEnter(neighbor))
					{
						continue;
					}
					if (!CreateNode(current, neighbor, out Node node))
					{
						continue;
					}
					nodes[neighbor] = node;
					openQueue.Enqueue(neighbor, node.cost + node.heuristicCost); //0 cost heuristic for Dijkstra
				}
			}
			PathNotFound:;
			Log.Error($"Unable to find path from {start} to {destination}.");
			return null;
		}

		protected virtual bool CreateNode(T current, T neighbor, out Node node)
		{
			if (nodes.TryGetValue(neighbor, out node) && node.closed)
			{
				if (LogRetraceAttempts) SmashLog.Error($"Attempting to open closed node {neighbor}. Skipping to avoid infinite loop.");
				return false;
			}
			node = new Node()
			{
				parent = current,
				cost = cost(current, neighbor),
				heuristicCost = 0,
				closed = true
			};
			return true;
		}

		protected List<T> SolvePath(T start, T destination)
		{
			List<T> result = new List<T>();

			T current = destination;
			Node node = nodes[current];
			while (!start.Equals(current))
			{
				result.Add(current);
				current = node.parent;
				node = nodes[current];
			}
			result.Add(start);
			result.Reverse();

			if (!result[0].Equals(start))
			{
				SmashLog.Error($"BFS was unable to solve path from {start} to {destination}.");
			}
			return result;
		}

		protected struct Node
		{
			public T parent;
			public int cost;
			public int heuristicCost; //strictly for A* implementation, is not considered by Dijkstra
			public bool closed;
		}
	}
}
