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

		/// <summary>
		/// Get occupied cells of pawn with hitbox larger than 1x1
		/// </summary>
		/// <param name="pawn"></param>
		/// <param name="centerPoint"></param>
		/// <param name="direction"></param>
		/// <returns></returns>
		public static List<IntVec3> PawnOccupiedCells(this Pawn pawn, IntVec3 centerPoint, Rot4 direction)
		{
			int sizeX;
			int sizeZ;
			switch (direction.AsInt)
			{
				case 0:
					sizeX = pawn.def.Size.x;
					sizeZ = pawn.def.Size.z;
					break;
				case 1:
					sizeX = pawn.def.Size.z;
					sizeZ = pawn.def.Size.x;
					break;
				case 2:
					sizeX = pawn.def.Size.x;
					sizeZ = pawn.def.Size.z;
					break;
				case 3:
					sizeX = pawn.def.Size.z;
					sizeZ = pawn.def.Size.x;
					break;
				default:
					throw new NotImplementedException("MoreThan4Rotations");
			}
			return CellRect.CenteredOn(centerPoint, sizeX, sizeZ).Cells.ToList();
		}

		/// <summary>
		/// OccupiedRect shifted from pawns position
		/// </summary>
		/// <param name="pawn"></param>
		/// <param name="shift"></param>
		/// <returns></returns>
		public static CellRect OccupiedRectShifted(this Pawn pawn, IntVec2 shift, Rot4? newRot = null)
		{
			IntVec3 center = pawn.Position;
			if (newRot is null)
			{
				newRot = pawn.Rotation;
			}
			switch (newRot.Value.AsInt)
			{
				case 0:
					break;
				case 1:
					int x = shift.x;
					shift.x = shift.z;
					shift.z = x;
					break;
				case 2:
					shift.z *= -1;
					break;
				case 3:
					int x2 = shift.x;
					shift.x = -shift.z;
					shift.z = x2;
					break;
			}
			center.x += shift.x;
			center.z += shift.z;
			IntVec2 size = pawn.def.size;
			GenAdj.AdjustForRotation(ref center, ref size, newRot.Value);
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
		/// <returns></returns>
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

			switch (pawnAngle)
			{
				case 45f:
					for (int i = 0; i < 4; i++)
					{
						float xPos = bracketLocs[i].x - worldPos.x;
						float yPos = bracketLocs[i].z - worldPos.z;
						Pair<float, float> newPos = Ext_Math.RotatePointClockwise(xPos, yPos, 45f);
						bracketLocs[i].x = newPos.First + worldPos.x;
						bracketLocs[i].z = newPos.Second + worldPos.z;
					}
					break;
				case -45:
					for (int i = 0; i < 4; i++)
					{
						float xPos = bracketLocs[i].x - worldPos.x;
						float yPos = bracketLocs[i].z - worldPos.z;
						Pair<float, float> newPos = Ext_Math.RotatePointCounterClockwise(xPos, yPos, 45f);
						bracketLocs[i].x = newPos.First + worldPos.x;
						bracketLocs[i].z = newPos.Second + worldPos.z;
					}
					break;
			}
		}
	}
}
