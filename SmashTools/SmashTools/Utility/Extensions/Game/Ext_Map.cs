using System;
using System.Runtime.CompilerServices;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using HarmonyLib;

namespace SmashTools
{
	public static class Ext_Map
	{
		public static void DrawCell_ThreadSafe(this Map map, IntVec3 cell, float colorPct = 0, string text = null, int duration = 50)
		{
			if (UnityData.IsInMainThread)
			{
				map.debugDrawer.FlashCell(cell, colorPct, text, duration);
			}
			else
			{
				CoroutineManager.QueueInvoke(() => DrawCellRoutine(cell, map, colorPct, text, duration));
			}
		}

		public static void DrawLine_ThreadSafe(this Map map, IntVec3 from, IntVec3 to, SimpleColor color = SimpleColor.White, int duration = 50)
		{
			if (UnityData.IsInMainThread)
			{
				map.debugDrawer.FlashLine(from, to, color: color, duration: duration);
			}
			else
			{
				CoroutineManager.QueueInvoke(() => DrawLineRoutine(from, to, map, color: color, duration: duration));
			}
		}

		public static void DrawLine_ThreadSafe(this Map map, Vector3 from, Vector3 to, SimpleColor color = SimpleColor.White, int duration = 50)
		{
			if (UnityData.IsInMainThread)
			{
				GenDraw.DrawLineBetween(from, to, color, 0.2f);
			}
			else
			{
				CoroutineManager.QueueInvoke(() => DrawLineRoutine(from, to, map, color: color, duration: duration));
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void DrawCellRoutine(IntVec3 cell, Map map, float colorPct, string label, int duration)
		{
			map.debugDrawer.FlashCell(cell, colorPct, label, duration);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void DrawLineRoutine(IntVec3 from, IntVec3 to, Map map, SimpleColor color = SimpleColor.White, int duration = 50)
		{
			map.debugDrawer.FlashLine(from, to, duration: duration, color: color);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static IEnumerator DrawLineRoutine(Vector3 from, Vector3 to, Map map, SimpleColor color = SimpleColor.White, int duration = 50)
		{
			float elapsed = 0;
			while (elapsed < duration)
			{
				GenDraw.DrawLineBetween(from, to, color, 0.2f);
				elapsed = Time.deltaTime;
				yield return null;
			}
		}

		/// <summary>
		/// Calculate euclidean distance between 2 cells as float value
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
		public static Rot8 DirectionToCell(IntVec3 c1, IntVec3 c2)
		{
			int xDiff = c1.x - c2.x;
			int zDiff = c1.z - c2.z;
			if (xDiff < 0)
			{
				if (zDiff < 0)
				{
					return Rot8.NorthEast;
				}
				else if (zDiff > 0)
				{
					return Rot8.SouthEast;
				}
				return Rot8.East;
			}
			else if (xDiff > 0)
			{
				if (zDiff < 0)
				{
					return Rot8.NorthWest;
				}
				else if (zDiff > 0)
				{
					return Rot8.SouthWest;
				}
				return Rot8.West;
			}
			else
			{
				if (zDiff < 0)
				{
					return Rot8.North;
				}
				else if (zDiff > 0)
				{
					return Rot8.South;
				}
			}
			return Rot8.Invalid;
		}

		public static List<T> AllPawnsOnMap<T>(this Map map, Faction faction = null, Predicate<T> validator = null) where T : Pawn
		{
			return map.mapPawns.AllPawnsSpawned.Where(p => p is T t && (faction is null || p.Faction == faction) && (validator is null || validator(t))).Cast<T>().ToList();
		}


		/* ---- DetatchedMapComponent Extensions ---- */

		public static T GetCachedMapComponent<T>(this Map map) where T : DetachedMapComponent
		{
			if (DetachedMapComponent.mapMapping.TryGetValue(map, out var components))
			{
				foreach (DetachedMapComponent component in components)
				{
					if (component is T matchingComponent)
					{
						return matchingComponent;
					}
				}
				return null;
			}
			Log.Error($"Unable to locate Map={map} in detached map cache.");
			return null;
		}

		/* ----------------------------------------- */
	}
}
