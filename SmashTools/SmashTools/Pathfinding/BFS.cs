using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Verse;

namespace SmashTools.Pathfinding
{
  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  public class BFS<T>
  {
    private readonly Queue<T> openQueue = [];

    private readonly HashSet<T> visited = [];

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

    public void FloodFill(T start, Func<T, IEnumerable<T>> neighbors, Action<T> processor,
      Func<T, bool> canEnter = null)
    {
      FloodFill(start, neighbors, processor, onEntered: null, onSkipped: null, canEnter);
    }

    public void FloodFill(T start, Func<T, IEnumerable<T>> neighbors, Action<T> processor,
      Action<T> onEntered,
      Action<T> onSkipped,
      Func<T, bool> canEnter = null)
    {
      if (IsRunning)
      {
        Log.Error("Attempting to run FloodFill while it's already in use.");
        return;
      }

      if (canEnter != null && !canEnter(start))
        return;

      IsRunning = true;
      try
      {
        openQueue.Clear();
        openQueue.Enqueue(start);
        visited.Add(start);
        onEntered?.Invoke(start);
        while (openQueue.Count > 0)
        {
          T current = openQueue.Dequeue();
          processor?.Invoke(current);
          foreach (T neighbor in neighbors(current))
          {
            if (visited.Contains(neighbor))
            {
              if (LogRetraceAttempts)
                SmashLog.Error(
                  $"Attempting to open closed node {neighbor}. Skipping to avoid infinite loop.");
              continue;
            }

            if (canEnter == null || canEnter(neighbor))
            {
              visited.Add(neighbor);
              openQueue.Enqueue(neighbor);
              onEntered?.Invoke(neighbor);
            }
            else
            {
              onSkipped?.Invoke(neighbor);
            }
          }
        }
      }
      catch (Exception ex)
      {
        SmashLog.Error($"Exception thrown while performing BFS FloodFill.\n{ex}");
      }
      finally
      {
        IsRunning = false;
        Stop();
      }
    }

    public List<T> FloodFill(T start, Func<T, IEnumerable<T>> neighbors,
      Func<T, bool> canEnter = null)
    {
      List<T> nodes = [];
      FloodFill(start, neighbors, canEnter: canEnter, processor: nodes.Add);
      return nodes;
    }
  }
}