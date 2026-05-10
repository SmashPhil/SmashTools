using System;
using System.Collections.Generic;
using CoreLib.Collections;
using JetBrains.Annotations;

namespace CoreLib.PathFinding;

/// <summary>
/// Resulting path from start to end in sequential order.
/// </summary>
/// <remarks>Can be traversed to receive nodes in order as they are processed.</remarks>
[PublicAPI]
public class Path
{
  private List<Node> nodes = [];

  /// <summary>
  /// Path is valid and maps from start to end sequentially.
  /// </summary>
  public bool Found { get; private set; }

  /// <summary>
  /// Current index traversing the path.
  /// </summary>
  public int Current { get; private set; } = -1;

  /// <summary>
  /// Start of the path.
  /// </summary>
  public Node FirstNode => nodes[^1];

  /// <summary>
  /// End of the path.
  /// </summary>
  public Node LastNode => nodes[0];

  /// <summary>
  /// Remaining untraversed nodes.
  /// </summary>
  public int NodesLeft => Current + 1;

  /// <summary>
  /// Path has been fully traversed.
  /// </summary>
  public bool IsFinished => NodesLeft <= 1;

  /// <summary>
  /// How many nodes have been traversed.
  /// </summary>
  public int NodesConsumedCount => nodes.Count - NodesLeft;

  /// <summary>
  /// View of underlying node list.
  /// </summary>
  /// <remarks>This is the raw list so nodes will be stored in reverse order.</remarks>
  public ReadOnlyList<Node> Nodes => new(nodes);

  public unsafe void Populate(int* array, int length, int width)
  {
    nodes.Capacity = length;
    for (int i = 0; i < length; i++)
    {
      int index = array[i];
      int x = index % width;
      int y = index / width;
      nodes.Add(new Node(x, y));
    }
    ResetPathToStart();
  }

  public void Add(int index, int width)
  {
    int x = index % width;
    int y = index / width;
    nodes.Add(new Node(x, y));
  }

  /// <summary>
  /// Insert nodes from <paramref name="list"/> into path result.
  /// </summary>
  /// <remarks>Path is assumed to be found if <paramref name="list"/> is not empty.</remarks>
  public void Populate(List<Node> list)
  {
    nodes.Clear();
    nodes.Capacity = list.Count;
    foreach (Node node in list)
    {
      nodes.Add(node);
    }
    Validate(FirstNode, LastNode);
    ResetPathToStart();
  }

  /// <summary>
  /// Insert nodes from <paramref name="enumerable"/> into path result.
  /// </summary>
  /// <remarks>Path is assumed to be found if <paramref name="enumerable"/> is not empty.</remarks>
  public void Populate(IEnumerable<Node> enumerable)
  {
    nodes.Clear();
    foreach (Node node in enumerable)
    {
      nodes.Add(node);
    }
    Validate(FirstNode, LastNode);
    ResetPathToStart();
  }

  internal void Validate(Node start, Node end)
  {
    Found = nodes.Count > 1 && FirstNode.x == start.x && FirstNode.y == start.y &&
            LastNode.x == end.x && LastNode.y == end.y;
  }

  /// <summary>
  /// Appends <paramref name="otherPath"/> to the end of this path.
  /// </summary>
  /// <remarks><paramref name="otherPath"/> is cleared after nodes are appended.</remarks>
  /// <param name="otherPath">Path to append.</param>
  /// <exception cref="ArgumentException"></exception>
  /// <exception cref="InvalidOperationException"></exception>
  public void Combine(Path otherPath)
  {
    if (!Found || !otherPath.Found)
      throw new ArgumentException("Both paths need to be found.");
    if (LastNode != otherPath.FirstNode)
      throw new InvalidOperationException("otherPath's first node must match this path's last node.");

    // Skip 1 of the connection nodes, we don't need duplicates
    Current += otherPath.nodes.Count - 1;
    for (int i = 1; i < nodes.Count; i++)
    {
      otherPath.nodes.Add(nodes[i]);
    }

    // Append and swap is the easiest way to prepend without moving elements
    (nodes, otherPath.nodes) = (otherPath.nodes, nodes);
    otherPath.Clear();
  }

  /// <summary>
  /// Reset current node to beginning of the path
  /// </summary>
  public void ResetPathToStart()
  {
    Current = nodes.Count - 1;
  }

  /// <summary>
  /// Clear path and reset back to empty state.
  /// </summary>
  public void Clear()
  {
    Current = -1;
    Found = false;
    nodes.Clear();
  }

  /// <summary>
  /// Increment to the next node in the path.
  /// </summary>
  /// <returns>The next node in the path.</returns>
  public Node ConsumeNextNode()
  {
    Node cell = Peek(nodesAhead: 1);
    Current--;
    return cell;
  }

  /// <summary>
  /// Peek N nodes ahead in the path.
  /// </summary>
  public Node Peek(int nodesAhead)
  {
    return nodes[Current - nodesAhead];
  }

  public struct Node(int x, int y)
  {
    public int x = x;
    public int y = y;

    public static bool operator ==(Node a, Node b)
    {
      return a.x == b.x && a.y == b.y;
    }

    public static bool operator !=(Node a, Node b)
    {
      return !(a == b);
    }

    public override bool Equals(object obj)
    {
      return obj is Node other && this == other;
    }

    public bool Equals(Node other)
    {
      return this == other;
    }

    public override int GetHashCode()
    {
      return HashCode.Combine(x, y);
    }
  }
}