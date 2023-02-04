using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld.Planet;

namespace SmashTools
{
	public static class Ext_World
	{
		/// <summary>
		/// Fetch all tiles within <paramref name="radius"/> of <paramref name="tile"/>.  For radial search within <paramref name="radius"/>, use <paramref name="resultValidator"/>
		/// </summary>
		/// <param name="tile"></param>
		/// <param name="searchedTiles"></param>
		/// <param name="radius"></param>
		/// <param name="resultValidator"></param>
		/// <returns>If radial search exist prematurely, the tile it stopped on will be returned</returns>
		public static int BFS(int tile, List<int> searchedTiles, int radius = 1, Func<int, bool> validator = null, Func<int, int, bool> result = null)
		{
			if (radius < 1)
			{
				Log.Error($"Attempting to perform BFS search with max radius < 1.  Obviously this isn't possible, stop it.");
				return tile;
			}

			Queue<int> queue = new Queue<int>();
			queue.Enqueue(tile); //Queue start

			//Preinitialized list for neighbor search
			List<int> neighbors = new List<int>();

			//Handles visitation flags
			HashSet<int> visitedTiles = new HashSet<int>() { tile };
			
			int currentRadius = 0;
			//Need to track amount of new neighbor tiles added for radii limits
			int tilesAdded = 0;
			int searchCount = queue.Count;
			while (currentRadius < radius && queue.Count > 0)
			{
				for (int r = 0; r < searchCount; r++)
				{
					int currentTile = queue.Dequeue();
					neighbors.Clear();
					Find.WorldGrid.GetTileNeighbors(currentTile, neighbors);
					for (int i = 0; i < neighbors.Count; i++)
					{
						int neighbor = neighbors[i];
						//Check if tile is already visited
						if (visitedTiles.Contains(neighbor) || (validator != null && !validator(neighbor)))
						{
							continue;
						}

						//Add to result list, officially 'traversed' by BFS
						searchedTiles.Add(neighbors[i]);
						tilesAdded++;

						//Check if tile is valid result
						if (result != null && result(neighbors[i], currentRadius))
						{
							return neighbors[i];
						}

						//Enqueue for further search
						queue.Enqueue(neighbors[i]);
						visitedTiles.Add(neighbors[i]);
					}
				}
				searchCount = tilesAdded;
				tilesAdded = 0;
				currentRadius++;
			}
			//If BFS executes to full radius, return source tile as searched tiles are the result
			return tile;
		}

		/// <summary>
		/// Get neighbors of <paramref name="tile"/> on world map.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="offsets"></param>
		/// <param name="values"></param>
		/// <param name="tile"></param>
		/// <param name="outList"></param>
		public static void GetTileNeighbors(int tile, List<int> tileNeighbors, int radius = 1, Vector3? nearestTo = null)
		{
			if (radius == 1)
			{
				Find.WorldGrid.GetTileNeighbors(tile, tileNeighbors);
				return;
			}

			Find.WorldFloodFiller.FloodFill(tile, (int tile) => true, delegate (int tile, int dist)
			{
				if (dist > radius + 1)
				{
					return true;
				}
				if (dist == radius + 1)
				{
					tileNeighbors.Add(tile);
				}
				return false;
			}, int.MaxValue, null);

			WorldGrid worldGrid = Find.WorldGrid;
			Vector3 c = worldGrid.GetTileCenter(tile);
			Vector3 n = c.normalized;
			tileNeighbors.Sort(delegate (int a, int b)
			{
				float num = Vector3.Dot(n, Vector3.Cross(worldGrid.GetTileCenter(a) - c, worldGrid.GetTileCenter(b) - c));
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
				int closestTile = tileNeighbors.MinBy(t => Vector3.Dot(n, Vector3.Cross(worldGrid.GetTileCenter(t) - c, nearestTo.Value - c)));
				tileNeighbors = tileNeighbors.ReorderOn(closestTile);
			}
		}
	}
}
