using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace SmashTools.Pathfinding
{
	public class AStar<T> : Dijkstra<T>
	{
		private Func<T, int> heuristic;

		public AStar(IPathfinder<T> pathfinder, Func<T, int> heuristic) : base(pathfinder)
		{
			this.heuristic = heuristic;
		}

		public AStar(Func<T, int> heuristic, Func<T, T, int> cost, Func<T, List<T>> neighbors, Func<T, bool> canEnter = null) : base(cost, neighbors, canEnter)
		{
			this.heuristic = heuristic;
		}

		protected override bool CreateNode(T current, T neighbor, out Node node)
		{
			bool result = base.CreateNode(current, neighbor, out node);
			if (result)
			{
				node.heuristicCost = heuristic(neighbor); //Add heuristic cost
			}
			return result;
		}
	}
}
