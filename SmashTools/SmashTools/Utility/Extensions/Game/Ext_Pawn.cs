using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld.Planet;
using UnityEngine;

namespace SmashTools
{
	public static class Ext_Pawn
	{
		/// <summary>
		/// Clamp a Pawn's exit point to their hitbox size. Avoids derendering issues for multicell-pawns
		/// </summary>
		/// <param name="pawn"></param>
		/// <param name="exitPoint"></param>
		/// <param name="map"></param>
		/// <param name="extraOffset"></param>
		public static void ClampToMap(this Pawn pawn, ref IntVec3 exitPoint, Map map, int extraOffset = 0)
		{
			int x = pawn.def.size.x;
			int z = pawn.def.size.z;
			int offset = x > z ? x + extraOffset : z + extraOffset;
			int offsetFitted = Mathf.CeilToInt(offset / 2f);
			if (exitPoint.x < offset)
			{
				exitPoint.x = offsetFitted;
			}
			else if (exitPoint.x >= (map.Size.x - offsetFitted))
			{
				exitPoint.x = map.Size.x - offsetFitted;
			}
			if (exitPoint.z < offset)
			{
				exitPoint.z = offsetFitted;
			}
			else if (exitPoint.z > (map.Size.z - offsetFitted))
			{
				exitPoint.z = map.Size.z - offsetFitted;
			}
		}

		/// <summary>
		/// Clamp a Pawn's spawn point to their hitbox size. Avoids derendering issues for multicell-pawns
		/// </summary>
		/// <param name="pawn"></param>
		/// <param name="spawnPoint"></param>
		/// <param name="map"></param>
		/// <param name="extraOffset"></param>
		public static IntVec3 ClampToMap(this Pawn pawn, IntVec3 spawnPoint, Map map, int extraOffset = 0)
		{
			int x = pawn.def.size.x;
			int z = pawn.def.size.z;
			return ClampToMap(x, z, spawnPoint, map, extraOffset);
		}

		public static IntVec3 ClampToMap(int width, int height, IntVec3 spawnPoint, Map map, int extraOffset = 0)
		{
			int offset = width > height ? width + extraOffset : height + extraOffset;
			if (spawnPoint.x < offset)
			{
				spawnPoint.x = offset / 2;
			}
			else if (spawnPoint.x >= (map.Size.x - (offset / 2)))
			{
				spawnPoint.x = map.Size.x - (offset / 2);
			}
			if (spawnPoint.z < offset)
			{
				spawnPoint.z = offset / 2;
			}
			else if (spawnPoint.z > (map.Size.z - (offset / 2)))
			{
				spawnPoint.z = map.Size.z - (offset / 2);
			}
			return spawnPoint;
		}

		/// <summary>
		/// Verify that <paramref name="pawn"/> is inside the map at <paramref name="cell"/>
		/// </summary>
		/// <param name="pawn"></param>
		/// <param name="nextCell"></param>
		/// <param name="map"></param>
		public static bool InsideMap(this Pawn pawn, IntVec3 cell, Map map)
		{
			int x = pawn.def.size.x % 2 == 0 ? pawn.def.size.x / 2 : (pawn.def.size.x + 1) / 2;
			int z = pawn.def.size.z % 2 == 0 ? pawn.def.size.z / 2 : (pawn.def.size.z + 1) / 2;

			int hitbox = x > z ? x : z;
			if (cell.x + hitbox > map.Size.x || cell.z + hitbox > map.Size.z)
			{
				return true;
			}
			if (cell.x - hitbox < 0 || cell.z - hitbox < 0)
			{
				return true;
			}
			return false;
		}

		public static IntVec3 ThingPositionFromRect(this CellRect cellRect)
		{
			return new IntVec3(cellRect.minX + (cellRect.Width - 1) / 2, 0, cellRect.minZ + (cellRect.Height - 1) / 2);
		}

		/// <summary>
		/// Get occupied cells of pawn with hitbox larger than 1x1
		/// </summary>
		/// <param name="pawn"></param>
		/// <param name="centerPoint"></param>
		/// <param name="direction"></param>
		/// <returns></returns>
		public static List<IntVec3> PawnOccupiedCells(this Pawn pawn, IntVec3 centerPoint, Rot4 rot)
		{
			return GenAdj.OccupiedRect(centerPoint, rot, pawn.def.Size).Cells.ToList();
		}

		public static CellRect MinRectShifted(this Pawn pawn, IntVec2 shift, Rot4? rot = null)
		{
			if (rot is null)
			{
				rot = pawn.Rotation;
			}
			int minSize = Mathf.Min(pawn.def.size.x, pawn.def.size.z);
			return RectShifted(pawn.Position, shift, new IntVec2(minSize, minSize), rot.Value);
		}

		/// <summary>
		/// OccupiedRect shifted from pawns position
		/// </summary>
		/// <param name="pawn"></param>
		/// <param name="shift"></param>
		public static CellRect OccupiedRectShifted(this Pawn pawn, IntVec2 shift, Rot4? rot = null)
		{
			if (rot is null)
			{
				rot = pawn.Rotation;
			}
			return RectShifted(pawn.Position, shift, pawn.def.size, rot.Value);
		}

		private static CellRect RectShifted(IntVec3 center, IntVec2 shift, IntVec2 size, Rot8 rot)
		{
			if (rot == Rot8.North)
			{
				//Do nothing
			}
			if (rot == Rot8.East || rot == Rot8.NorthEast || rot == Rot8.SouthEast)
			{
				int x = shift.x;
				shift.x = shift.z;
				shift.z = x;
			}
			if (rot == Rot8.South || rot == Rot8.SouthEast || rot == Rot8.SouthWest)
			{
				shift.z *= -1;
			}
			if (rot == Rot8.West || rot == Rot8.SouthWest || rot == Rot8.NorthWest)
			{
				int x = shift.x;
				shift.x = -shift.z;
				shift.z = x;
			}

			center.x += shift.x;
			center.z += shift.z;

			GenAdj.AdjustForRotation(ref center, ref size, rot);
			return new CellRect(center.x - (size.x - 1) / 2, center.z - (size.z - 1) / 2, size.x, size.z);
		}

		/// <summary>
		/// Get edge of map the pawn is closest too
		/// </summary>
		/// <param name = "pawn" ></ param >
		/// < param name="map"></param>
		/// <returns></returns>
		public static Rot4 ClosestEdge(this Pawn pawn, Map map)
		{
			IntVec2 mapSize = new IntVec2(map.Size.x, map.Size.z);
			IntVec2 position = new IntVec2(pawn.Position.x, pawn.Position.z);

			Pair<Rot4, int> hDistance = Mathf.Abs(position.x) < Math.Abs(position.x - mapSize.x) ? new Pair<Rot4, int>(Rot4.West, position.x) : new Pair<Rot4, int>(Rot4.East, Math.Abs(position.x - mapSize.x));
			Pair<Rot4, int> vDistance = Mathf.Abs(position.z) < Math.Abs(position.z - mapSize.z) ? new Pair<Rot4, int>(Rot4.South, position.z) : new Pair<Rot4, int>(Rot4.North, Math.Abs(position.z - mapSize.z));

			return hDistance.Second <= vDistance.Second ? hDistance.First : vDistance.First;
		}

		/// <summary>
		/// Draw selection brackets transformed on position x,z for pawns whose selection brackets have been shifted
		/// </summary>
		/// <param name="pawn"></param>
		/// <param name="x"></param>
		/// <param name="z"></param>
		/// <param name="angle"></param>
		public static Vector3 DrawPosTransformed(this Pawn pawn, Vector2 offset, float angle = 0)
		{
			float x = offset.x;
			float z = offset.y;
			Vector3 drawPos = pawn.DrawPos;
			switch (pawn.Rotation.AsInt)
			{
				case 0:
					drawPos.x += x;
					drawPos.z += z;
					break;
				case 1:
					if (angle == -45)
					{
						drawPos.x += x == 0 ? z / (float)Math.Sqrt(2d) : x / (float)Math.Sqrt(2d);
						drawPos.z += x == 0 ? z / (float)Math.Sqrt(2d) : x / (float)Math.Sqrt(2d);
						break;
					}
					else if (angle == 45)
					{
						drawPos.x += x == 0 ? z / (float)Math.Sqrt(2d) : x / (float)Math.Sqrt(2d);
						drawPos.z -= x == 0 ? z / (float)Math.Sqrt(2d) : x / (float)Math.Sqrt(2d);
						break;
					}
					drawPos.x += z;
					drawPos.z += x;
					break;
				case 2:
					drawPos.x -= x;
					drawPos.z -= z;
					break;
				case 3:
					if (angle == -45)
					{
						drawPos.x -= x == 0 ? z / (float)Math.Sqrt(2d) : x / (float)Math.Sqrt(2d);
						drawPos.z -= x == 0 ? z / (float)Math.Sqrt(2d) : x / (float)Math.Sqrt(2d);
						break;
					}
					else if (angle == 45)
					{
						drawPos.x -= x == 0 ? z / (float)Math.Sqrt(2d) : x / (float)Math.Sqrt(2d);
						drawPos.z += x == 0 ? z / (float)Math.Sqrt(2d) : x / (float)Math.Sqrt(2d);
						break;
					}
					drawPos.x -= z;
					drawPos.z -= x;
					break;
				default:
					throw new NotImplementedException("Pawn Rotation outside Rot4");
			}
			return drawPos;
		}

		/// <summary>
		/// Draw selection brackets for pawn with angle
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="bracketLocs"></param>
		/// <param name="obj"></param>
		/// <param name="worldPos"></param>
		/// <param name="worldSize"></param>
		/// <param name="dict"></param>
		/// <param name="textureSize"></param>
		/// <param name="pawnAngle"></param>
		/// <param name="jumpDistanceFactor"></param>
		public static void CalculateSelectionBracketPositionsWorldForMultiCellPawns<T>(Vector3[] bracketLocs, T obj, Vector3 worldPos, Vector2 worldSize, Dictionary<T, float> dict, Vector2 textureSize, float pawnAngle = 0f, float jumpDistanceFactor = 1f)
		{
			float num2;
			if (!dict.TryGetValue(obj, out float num))
			{
				num2 = 1f;
			}
			else
			{
				num2 = Mathf.Max(0f, 1f - (Time.realtimeSinceStartup - num) / 0.07f);
			}
			float num3 = num2 * 0.2f * jumpDistanceFactor;
			float num4 = 0.5f * (worldSize.x - textureSize.x) + num3;
			float num5 = 0.5f * (worldSize.y - textureSize.y) + num3;
			float y = AltitudeLayer.MetaOverlays.AltitudeFor();
			bracketLocs[0] = new Vector3(worldPos.x - num4, y, worldPos.z - num5);
			bracketLocs[1] = new Vector3(worldPos.x + num4, y, worldPos.z - num5);
			bracketLocs[2] = new Vector3(worldPos.x + num4, y, worldPos.z + num5);
			bracketLocs[3] = new Vector3(worldPos.x - num4, y, worldPos.z + num5);

			for (int i = 0; i < 4; i++)
			{
				float xPos = bracketLocs[i].x - worldPos.x;
				float yPos = bracketLocs[i].z - worldPos.z;
				Vector2 newPos = Ext_Math.RotatePointClockwise(xPos, yPos, pawnAngle);
				bracketLocs[i].x = newPos.x + worldPos.x;
				bracketLocs[i].z = newPos.y + worldPos.z;
			}
		}
	}
}
