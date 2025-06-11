using System;
using System.Collections.Generic;

namespace SmashTools.Algorithms;

public class AStar<T> : Dijkstra<T>
{
  private readonly Func<T, int> getHeuristic;

  public AStar(IPathfinder<T> pathfinder, Func<T, int> getHeuristic) : base(pathfinder)
  {
    this.getHeuristic = getHeuristic;
  }

  public AStar(Func<T, int> getHeuristic, Func<T, T, int> cost, Func<T, List<T>> neighbors,
    Func<T, bool> canEnter = null) : base(cost, neighbors, canEnter)
  {
    this.getHeuristic = getHeuristic;
  }

  protected override bool CreateNode(T current, T neighbor, out Node node)
  {
    bool result = base.CreateNode(current, neighbor, out node);
    if (result)
    {
      node.heuristicCost = getHeuristic(neighbor);
    }
    return result;
  }
}