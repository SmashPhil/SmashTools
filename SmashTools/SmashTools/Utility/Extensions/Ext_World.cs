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
		private static readonly Dictionary<Pair<int, int>, List<int>> tileNeighbors = new Dictionary<Pair<int, int>, List<int>>();

		/// <summary>
		/// Get neighbors of <paramref name="tile"/> on world map.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="offsets"></param>
		/// <param name="values"></param>
		/// <param name="tile"></param>
		/// <param name="outList"></param>
		public static List<int> GetTileNeighbors(int tile, int radius = 1, Vector3? nearestTo = null)
		{
			if (radius == 1)
			{
				List<int> outList = new List<int>();
				Find.WorldGrid.GetTileNeighbors(tile, outList);
				return outList;
			}
			//if (tileNeighbors.TryGetValue(new Pair<int, int>(tile, radius), out outList))
			//{
			//	return;
			//}
			List<int> ringTiles = new List<int>();

			Find.WorldFloodFiller.FloodFill(tile, (int tile) => true, delegate (int tile, int dist)
			{
				if (dist > radius + 1)
				{
					return true;
				}
				if (dist == radius + 1)
				{
					ringTiles.Add(tile);
				}
				return false;
			}, int.MaxValue, null);

			WorldGrid worldGrid = Find.WorldGrid;
			Vector3 c = worldGrid.GetTileCenter(tile);
			Vector3 n = c.normalized;
			ringTiles.Sort(delegate (int a, int b)
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
				int closestTile = ringTiles.MinBy(t => Vector3.Dot(n, Vector3.Cross(worldGrid.GetTileCenter(t) - c, nearestTo.Value - c)));
				return ringTiles.ReorderOn(closestTile);
			}
			return ringTiles;	
		}
	}
}
