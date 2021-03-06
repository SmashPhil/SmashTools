using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld.Planet;
using UnityEngine;

namespace SmashTools
{
	public static class Ext_Map
	{
		/// <summary>
		/// Calculate distance between 2 cells as float value
		/// </summary>
		/// <param name="c1"></param>
		/// <param name="c2"></param>
		public static float Distance(IntVec3 c1, IntVec3 c2)
		{
			int x = Mathf.Abs(c1.x - c2.x);
			int y = Mathf.Abs(c1.z - c2.z);
			return Mathf.Sqrt(x.Pow(2) + y.Pow(2));
		}

		/// <summary>
		/// Check if pawn is within certain distance of edge of map. Useful for multicell pawns who are clamped to the map beyond normal edge cell checks.
		/// </summary>
		/// <param name="position"></param>
		/// <param name="distance"></param>
		/// <param name="map"></param>
		public static bool WithinDistanceToEdge(this IntVec3 position, int distance, Map map)
		{
			return position.x < distance || position.z < distance || (map.Size.x - position.x < distance) || (map.Size.z - position.z < distance);
		}

		/// <summary>
		/// Get adjacent cells that are cardinal to position c bounded by Map
		/// </summary>
		/// <param name="c"></param>
		/// <param name="map"></param>
		public static IEnumerable<IntVec3> AdjacentCellsCardinal(this IntVec3 c, Map map)
		{
			IntVec3 north = new IntVec3(c.x, c.y, c.z + 1);
			IntVec3 east = new IntVec3(c.x + 1, c.y, c.z);
			IntVec3 south = new IntVec3(c.x, c.y, c.z - 1);
			IntVec3 west = new IntVec3(c.x - 1, c.y, c.z);

			if (north.InBounds(map))
			{
				yield return north;
			}
			if (east.InBounds(map))
			{
				yield return east;
			}
			if (south.InBounds(map))
			{
				yield return south;
			}
			if (west.InBounds(map))
			{
				yield return west;
			}
		}

		/// <summary>
		/// Get adjacent cells that are diagonal to position c bounded by Map
		/// </summary>
		/// <param name="c"></param>
		/// <param name="map"></param>
		public static IEnumerable<IntVec3> AdjacentCellsDiagonal(this IntVec3 c, Map map)
		{
			IntVec3 NE = new IntVec3(c.x + 1, c.y, c.z + 1);
			IntVec3 SE = new IntVec3(c.x + 1, c.y, c.z - 1);
			IntVec3 SW = new IntVec3(c.x - 1, c.y, c.z - 1);
			IntVec3 NW = new IntVec3(c.x - 1, c.y, c.z + 1);

			if (NE.InBounds(map))
			{
				yield return NE;
			}
			if (SE.InBounds(map))
			{
				yield return SE;
			}
			if (SW.InBounds(map))
			{
				yield return SW;
			}
			if (NW.InBounds(map))
			{
				yield return NW;
			}
		}

		/// <summary>
		/// Get all adjacent cells to position c bounded by Map
		/// </summary>
		/// <param name="c"></param>
		/// <param name="map"></param>
		public static IEnumerable<IntVec3> AdjacentCells8Way(this IntVec3 c, Map map)
		{
			return c.AdjacentCellsCardinal(map).Concat(c.AdjacentCellsDiagonal(map));
		}

		/// <summary>
		/// Find Rot4 direction with largest cell count
		/// <para>Useful for taking edge cells of specific terrain and getting edge with highest cell count</para>
		/// </summary>
		/// <param name="northCellCount"></param>
		/// <param name="eastCellCount"></param>
		/// <param name="southCellCount"></param>
		/// <param name="westCellCount"></param>
		/// <returns></returns>
		public static Rot4 Max4IntToRot(int northCellCount, int eastCellCount, int southCellCount, int westCellCount)
		{
			int ans1 = northCellCount > eastCellCount ? northCellCount : eastCellCount;
			int ans2 = southCellCount > westCellCount ? southCellCount : westCellCount;
			int ans3 = ans1 > ans2 ? ans1 : ans2;
			if (ans3 == northCellCount)
			{
				return Rot4.North;
			}
			if (ans3 == eastCellCount)
			{
				return Rot4.East;
			}
			if (ans3 == southCellCount)
			{
				return Rot4.South;
			}
			if (ans3 == westCellCount)
			{
				return Rot4.West;
			}
			return Rot4.Invalid;
		}

		/// <summary>
		/// Get direction of river in Rot4 value. (Can be either start or end of River)
		/// </summary>
		/// <param name="map"></param>
		/// <returns></returns>
		public static Rot4 RiverDirection(Map map)
		{
			List<Tile.RiverLink> rivers = Find.WorldGrid[map.Tile].Rivers;

			float angle = Find.WorldGrid.GetHeadingFromTo(map.Tile, (from r1 in rivers
																	 orderby -r1.river.degradeThreshold
																	 select r1).First<Tile.RiverLink>().neighbor);
			if (angle < 45)
			{
				return Rot4.South;
			}
			else if (angle < 135)
			{
				return Rot4.East;
			}
			else if (angle < 225)
			{
				return Rot4.North;
			}
			else if (angle < 315)
			{
				return Rot4.West;
			}
			else
			{
				return Rot4.South;
			}
		}

		/// <summary>
		/// Translate cell to cell comparison into 8-way direction; [0, 1, 2, 3] = N E S W, [4, 5, 6, 7] = NE, SE, SW, NW
		/// </summary>
		/// <param name="c1"></param>
		/// <param name="c2"></param>
		/// <returns></returns>
		public static int DirectionToCell(IntVec3 c1, IntVec3 c2)
		{
			int xDiff = c1.x - c2.x;
			int zDiff = c1.z - c2.z;
			if (xDiff < 0)
			{
				if (zDiff < 0)
				{
					return 4;
				}
				else if (zDiff > 0)
				{
					return 5;
				}
				else
				{
					return 1;
				}
			}
			else if (xDiff > 0)
			{
				if (zDiff < 0)
				{
					return 7;
				}
				else if (zDiff > 0)
				{
					return 6;
				}
				else
				{
					return 3;
				}
			}
			else
			{
				if (zDiff < 0)
				{
					return 0;
				}
				else if (zDiff > 0)
				{
					return 2;
				}
			}
			return -1;
		}
	}
}
