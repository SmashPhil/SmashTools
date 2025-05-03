using System;
using System.Collections.Generic;
using RimWorld.Planet;
using Unity.Collections;
using UnityEngine;
using Verse;

namespace SmashTools;

public static class Ext_World
{
  /// <summary>
  /// Fetch all tiles within <paramref name="radius"/> of <paramref name="tile"/>.
  /// For radial search within <paramref name="radius"/>, use <paramref name="result"/>
  /// </summary>
  /// <returns>If radial search exits prematurely, the tile it stopped on will be returned</returns>
  public static int BFS(int tile, List<int> searchedTiles, int radius = 1,
    Func<int, bool> validator = null, Func<int, int, bool> result = null)
  {
    if (radius < 1)
    {
      Log.Error("Attempting to perform BFS search with max radius < 1");
      return tile;
    }

    Queue<int> queue = [];
    queue.Enqueue(tile); //Queue start

    //Preinitialized list for neighbor search
    List<PlanetTile> neighbors = [];

    //Handles visitation flags
    HashSet<PlanetTile> visitedTiles = [tile];

    int currentRadius = 0;
    //Need to track amount of new neighbor tiles added for radii limits
    int tilesAdded = 0;
    int searchCount = queue.Count;
    while (currentRadius < radius && queue.Count > 0)
    {
      for (int r = 0; r < searchCount; r++)
      {
        PlanetTile currentTile = queue.Dequeue();
        neighbors.Clear();
        Find.WorldGrid.GetTileNeighbors(currentTile, neighbors);
        foreach (PlanetTile neighbor in neighbors)
        {
          //Check if tile is already visited
          if (visitedTiles.Contains(neighbor) || (validator != null && !validator(neighbor)))
          {
            continue;
          }

          //Add to result list, officially 'traversed' by BFS
          searchedTiles.Add(neighbor);
          tilesAdded++;

          //Check if tile is valid result
          if (result != null && result(neighbor, currentRadius))
          {
            return neighbor;
          }

          //Enqueue for further search
          queue.Enqueue(neighbor);
          visitedTiles.Add(neighbor);
        }
      }

      searchCount = tilesAdded;
      tilesAdded = 0;
      currentRadius++;
    }

    //If BFS executes to full radius, return source tile as searched tiles are the result
    return tile;
  }

  public static IEnumerable<PlanetTile> GetTileNeighbors(PlanetTile tile)
  {
    NativeArray<int> offsets = tile.Layer.UnsafeTileIDToNeighbors_offsets;
    NativeArray<PlanetTile> values = tile.Layer.UnsafeTileIDToNeighbors_values;

    int root = offsets[tile];
    int count = tile + 1 < offsets.Length ? offsets[tile + 1] : values.Length;

    for (int i = root; i < count; i++)
      yield return values[i];
  }

  /// <summary>
  /// Get neighbors of <paramref name="tile"/> on world map.
  /// </summary>
  public static void GetTileNeighbors(PlanetTile tile, List<PlanetTile> tileNeighbors,
    int radius = 1,
    Vector3? nearestTo = null)
  {
    if (radius == 1)
    {
      Find.WorldGrid.GetTileNeighbors(tile, tileNeighbors);
      return;
    }

    tile.Layer.Filler.FloodFill(tile, _ => true,
      delegate(PlanetTile currentTile, int dist)
      {
        // ReSharper disable AccessToModifiedClosure
        if (dist > radius + 1)
          return true;
        if (dist == radius + 1)
          tileNeighbors.Add(currentTile);
        return false;
      });

    WorldGrid worldGrid = Find.WorldGrid;
    Vector3 c = worldGrid.GetTileCenter(tile);
    Vector3 n = c.normalized;
    tileNeighbors.Sort(delegate(PlanetTile a, PlanetTile b)
    {
      float num = Vector3.Dot(n,
        Vector3.Cross(worldGrid.GetTileCenter(a) - c, worldGrid.GetTileCenter(b) - c));
      if (Mathf.Abs(num) < 0.0001f)
      {
        return 0;
      }

      if (num < 0f)
      {
        return -1;
      }

      return 1;
    });
    if (nearestTo.HasValue)
    {
      int closestTile = tileNeighbors.MinBy(t =>
        Vector3.Dot(n, Vector3.Cross(worldGrid.GetTileCenter(t) - c, nearestTo.Value - c)));
      tileNeighbors = tileNeighbors.ReorderOn(closestTile);
    }
  }
}