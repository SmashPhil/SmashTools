using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using System;

namespace SmashTools
{
	public static class Ext_Math
	{
		//Precalculated √2
		public static readonly float Sqrt2 = Mathf.Sqrt(2);

		//Up to n=16
		private static readonly float[] factorials = new float[]
		{
			1,
			1,
			2,
			6,
			24,
			120,
			720,
			5040,
			40320,
			362880,
			3628800,
			39916800,
			479001600,
			6227020800,
			87178291200,
			1307674368000,
			20922789888000,
		};

		public static float Binomial(int n, int i)
		{
			float a1 = factorials[n];
			float a2 = factorials[i];
			float a3 = factorials[n - i];
			return a1 / (a2 * a3);
		}

		public static float Bernstein(int n, int i, float t)
		{
			float t1 = Mathf.Pow(t, i); //t_i
			float t2 = Mathf.Pow(1 - t, n - i); //t_n-i
			return Binomial(n, i) * t1 * t2;
		}

		/// <summary>
		/// De Casteljau's Algorithm
		/// </summary>
		/// <remarks>See https://en.wikipedia.org/wiki/De_Casteljau%27s_algorithm for reference.</remarks>
		/// <param name="r"></param>
		/// <param name="i"></param>
		/// <param name="t"
		public static Vector2 DeCasteljau(List<CurvePoint> points, float t)
		{
			//Only setup for p-count = 4
			Vector2 p0 = Vector2.Lerp(points[0], points[1], t);
			Vector2 p1 = Vector2.Lerp(points[1], points[2], t);
			Vector2 p2 = Vector2.Lerp(points[2], points[3], t);

			Vector2 r0 = Vector2.Lerp(p0, p1, t);
			Vector2 r1 = Vector2.Lerp(p1, p2, t);

			return Vector2.Lerp(r0, r1, t);
		}

		/// <summary>
		/// Sign for boolean condition
		/// </summary>
		/// <param name="value"></param>
		/// <returns>True = 1, False = -1</returns>
		public static int Sign(bool value)
		{
			return value ? 1 : -1;
		}

		/// <summary>
		/// Get Absolute Value of IntVec2
		/// </summary>
		/// <param name="c"></param>
		public static IntVec2 Abs(IntVec2 c)
		{
			return new IntVec2(Mathf.Abs(c.x), Mathf.Abs(c.z));
		}

		/// <summary>
		/// Get Absolute Value of IntVec3
		/// </summary>
		/// <param name="c"></param>
		public static IntVec3 Abs(IntVec3 c)
		{
			return new IntVec3(Mathf.Abs(c.x), Mathf.Abs(c.y), Mathf.Abs(c.z));
		}

		/// <summary>
		/// Shift <paramref name="cell"/>.
		/// </summary>
		/// <param name="cell"></param>
		/// <param name="dir"></param>
		/// <param name="a">vertical relative to <paramref name="dir"/></param>
		/// <param name="b">horizontal relative to <paramref name="dir"/></param>
		public static IntVec2 Shifted(this IntVec2 cell, Rot4 dir, int a, int b = 0)
		{
			if (!dir.IsValid)
			{
				return cell;
			}
			return dir.AsInt switch
			{
				0 => new IntVec2(cell.x + b, cell.z + a),
				1 => new IntVec2(cell.x + a, cell.z - b),
				2 => new IntVec2(cell.x - b, cell.z - a),
				3 => new IntVec2(cell.x - a, cell.z + b),
				_ => throw new NotImplementedException("Beyond what Rot4 supports")
			};
		}

		/// <summary>
		/// Round to nearest n digits
		/// </summary>
		/// <param name="num"></param>
		/// <param name="roundTo"></param>
		public static float RoundTo(this float num, float roundTo)
		{
			return Mathf.Round(num / roundTo) * roundTo;
		}

		/// <summary>
		/// Round to nearest n
		/// </summary>
		/// <param name="num"></param>
		/// <param name="roundTo"></param>
		public static int RoundTo(this int num, int roundTo)
		{
			return Mathf.RoundToInt(num / roundTo) * roundTo;
		}

		/// <summary>
		/// Math.Pow simple casting for integers
		/// </summary>
		/// <param name="val"></param>
		/// <param name="exp"></param>
		public static long Pow(this int val, int exp)
		{
			return (long)Math.Pow(val, exp);
		}

		/// <summary>
		/// Take amount from a value bounded by 0, returning the amount remaining in the value
		/// </summary>
		/// <param name="value"></param>
		/// <param name="take"></param>
		/// <param name="remaining"></param>
		public static int Take(this int value, int take, out int remaining)
		{
			remaining = 0;
			if (take >= value)
			{
				return value;
			}
			remaining = value - take;
			return take;
		}

		/// <summary>
		/// Inverse value. Useful for tick methods that increase from <paramref name="min"/> to <paramref name="max"/> for animation curves.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="min"></param>
		/// <param name="max"></param>
		public static int Inverse(this int value, int min, int max)
		{
			return max - value + min;
		}

		public static Vector2 RotatePointClockwise(Vector2 coord, float theta)
		{
			return RotatePointClockwise(coord.x, coord.y, theta);
		}

		public static Vector2 RotatePointCounterClockwise(Vector2 coord, float theta)
		{
			return RotatePointCounterClockwise(coord.x, coord.y, theta);
		}

		/// <summary>
		/// Rotate point clockwise by angle theta
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="theta"></param>
		public static Vector2 RotatePointClockwise(float x, float y, float theta)
		{
			theta = -theta;
			float xPrime = (float)(x * Mathf.Cos(theta * Mathf.Deg2Rad)) - (float)(y * Mathf.Sin(theta * Mathf.Deg2Rad));
			float yPrime = (float)(x * Mathf.Sin(theta * Mathf.Deg2Rad)) + (float)(y * Mathf.Cos(theta * Mathf.Deg2Rad));
			return new Vector2(xPrime, yPrime);
		}

		/// <summary>
		/// Rotate point counter clockwise by angle theta
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="theta"></param>
		public static Vector2 RotatePointCounterClockwise(float x, float y, float theta)
		{
			float xPrime = (float)(x * Mathf.Cos(theta * Mathf.Deg2Rad)) - (float)(y * Mathf.Sin(theta * Mathf.Deg2Rad));
			float yPrime = (float)(x * Mathf.Sin(theta * Mathf.Deg2Rad)) + (float)(y * Mathf.Cos(theta * Mathf.Deg2Rad));
			return new Vector2(xPrime, yPrime);
		}

		/// <summary>
		/// Rotates point around another
		/// </summary>
		/// <param name="point"></param>
		/// <param name="origin"></param>
		/// <param name="angle">In Degrees</param>
		public static Vector3 RotatePoint(Vector3 point, Vector3 origin, float angle)
		{
			float newX = (float)(Mathf.Cos(angle * Mathf.Deg2Rad) * (point.x - origin.x) - Mathf.Sin(angle * Mathf.Deg2Rad) * (point.z - origin.z) + origin.x);
			float newZ = (float)(Mathf.Sin(angle * Mathf.Deg2Rad) * (point.x - origin.x) + Mathf.Cos(angle * Mathf.Deg2Rad) * (point.z - origin.z) + origin.z);

			return new Vector3(newX, point.y, newZ);
		}

		/// <summary>
		/// Rotates angle clockwise in [0:360] range. Used for clamping angle in this range
		/// </summary>
		/// <param name="angle"></param>
		/// <param name="rotation"></param>
		public static float RotateAngle(float angle, float rotation)
		{
			angle += rotation;
			while (angle > 360 || angle < 0)
			{
				if (angle > 360)
				{
					angle -= 360f;
				}
				else if (angle < 0)
				{
					angle += 360f;
				}
			}
			return angle;
		}

		/// <summary>
		/// Calculate angle from origin to point on map relative to positive x axis
		/// </summary>
		/// <param name="c"></param>
		/// <param name="map"></param>
		public static double AngleThroughOrigin(this IntVec3 c, Map map)
		{
			int xPrime = c.x - (map.Size.x / 2);
			int yPrime = c.z - (map.Size.z / 2);
			float slope = yPrime / xPrime;
			float angleRadians = Mathf.Atan(slope);
			float angle = Mathf.Abs(angleRadians * Mathf.Deg2Rad);
			return Quadrant.QuadrantOfIntVec3(c, map).AsInt switch
			{
				2 => 360 - angle,
				3 => 180 + angle,
				4 => 180 - angle,
				_ => angle,
			};
		}

		/// <summary>
		/// Calculate angle between 2 points on Cartesian coordinate plane.
		/// </summary>
		/// <param name="pos"></param>
		/// <param name="point"></param>
		/// <param name="map"></param>
		public static float AngleToCell(this IntVec3 pos, IntVec3 point, Map map)
		{
			Vector3 posVector = pos.ToVector3Shifted();
			Vector3 pointVector = point.ToVector3Shifted();
			float xPrime = posVector.x - pointVector.x;
			float yPrime = posVector.z - pointVector.z;
			float slope = yPrime / xPrime;
			float angleRadians = Mathf.Atan(slope);
			float angle = Mathf.Abs(angleRadians * Mathf.Rad2Deg);
			return Quadrant.QuadrantRelativeToPoint(pos, point, map).AsInt switch
			{
				2 => 360 - angle,
				3 => 180 + angle,
				4 => 180 - angle,
				_ => angle,
			};
		}

		/// <summary>
		/// Angle between 2 points in the in-game map. 0 is West, 270 is North
		/// </summary>
		/// <param name="pos"></param>
		/// <param name="point"></param>
		/// <param name="map"></param>
		public static float AngleToPointRelative(this Vector3 pos, Vector3 point)
		{
			float xPrime = pos.x - point.x;
			float yPrime = pos.z - point.z;
			return (360 + Mathf.Atan2(yPrime, xPrime) * Mathf.Rad2Deg) % 360;
		}

		/// <summary>
		/// Angle between 2 points in the in-game map. 0 is North, 270 is West
		/// </summary>
		/// <param name="pos"></param>
		/// <param name="point"></param>
		/// <param name="map"></param>
		public static float AngleToPoint(this Vector3 pos, Vector3 point)
		{
			float xPrime = pos.x - point.x;
			float yPrime = pos.z - point.z;
			return (180 + Mathf.Atan2(xPrime, yPrime) * Mathf.Rad2Deg) % 360;
		}

		/// <summary>
		/// Angle between 2 cells in the in-game map. 0 is North, 270 is West
		/// </summary>
		/// <param name="pos"></param>
		/// <param name="point"></param>
		public static float AngleToPoint(this IntVec3 start, IntVec3 end)
		{
			return AngleToPoint(start.ToVector3Shifted(), end.ToVector3Shifted());
		}

		/// <summary>
		/// Returns point from origin given radius and angle
		/// </summary>
		/// <param name="pos"></param>
		/// <param name="distance"></param>
		/// <param name="angle"></param>
		public static Vector3 PointFromAngle(this Vector3 pos, float distance, float angle)
		{
			float x = (float)(pos.x + distance * Mathf.Sin(angle * Mathf.Deg2Rad));
			float z = (float)(pos.z + distance * Mathf.Cos(angle * Mathf.Deg2Rad));
			return new Vector3(x, pos.y, z);
		}

		/// <summary>
		/// Returns point from origin given radius and angle
		/// </summary>
		/// <param name="pos"></param>
		/// <param name="distance"></param>
		/// <param name="angle"></param>
		public static IntVec3 PointFromAngle(this IntVec3 pos, float distance, float angle)
		{
			int x = Mathf.CeilToInt(pos.x + distance * Mathf.Sin(angle * Mathf.Deg2Rad));
			int z = Mathf.CeilToInt(pos.z + distance * Mathf.Cos(angle * Mathf.Deg2Rad));
			return new IntVec3(x, pos.y, z);
		}

		public static Vector3 PointToEdge(this Vector3 origin, Map map, float angle)
		{
			float clampedAngle = angle.ClampAndWrap(0, 360).RoundTo(0.01f);
			float maxX = map.Size.x;
			float maxZ = map.Size.z;
			Vector3 edgePoint = Vector3.zero;

			if (clampedAngle == 0)
			{
				edgePoint = new Vector3(origin.x, origin.y, maxZ);
			}
			else if(clampedAngle == 90)
			{
				edgePoint = new Vector3(maxX, origin.y, origin.z);
			}
			else if(clampedAngle == 180)
			{
				edgePoint = new Vector3(origin.x, origin.y, 0);
			}
			else if(clampedAngle == 270)
			{
				edgePoint = new Vector3(0, origin.y, origin.z);
			}
			else if (clampedAngle >= 0 && clampedAngle <= 45) //1
			{
				float relativeAngle = clampedAngle;
				float x = origin.x + ((maxZ - origin.z) * Mathf.Tan(relativeAngle * Mathf.Deg2Rad));
				edgePoint = new Vector3(x, origin.y, maxZ);
			}
			else if (clampedAngle >= 45 && clampedAngle <= 90) //2
			{
				float relativeAngle = 90 - clampedAngle;
				float z = origin.z + ((maxX - origin.x) * Mathf.Tan(relativeAngle * Mathf.Deg2Rad));
				edgePoint = new Vector3(maxX, origin.y, z);
			}
			else if (clampedAngle >= 90 && clampedAngle <= 135) //3
			{
				float relativeAngle = clampedAngle - 90;
				float z = origin.z - ((maxX - origin.x) * Mathf.Tan(relativeAngle * Mathf.Deg2Rad));
				edgePoint = new Vector3(maxX, origin.y, z);
			}
			else if (clampedAngle >= 135 && clampedAngle <= 180) //4
			{
				float relativeAngle = 180 - clampedAngle;
				float x = origin.x + (origin.z * Mathf.Tan(relativeAngle * Mathf.Deg2Rad));
				edgePoint = new Vector3(x, origin.y, 0);
			}
			else if (clampedAngle >= 180 && clampedAngle <= 225) //5
			{
				float relativeAngle = clampedAngle - 180;
				float x = origin.x - (origin.z * Mathf.Tan(relativeAngle * Mathf.Deg2Rad));
				edgePoint = new Vector3(x, origin.y, 0);
			}
			else if (clampedAngle >= 225 && clampedAngle <= 270) //6
			{
				float relativeAngle = 270 - clampedAngle;
				float z = origin.z - (origin.x * Mathf.Tan(relativeAngle * Mathf.Deg2Rad));
				edgePoint = new Vector3(0, origin.y, z);
			}
			else if (clampedAngle >= 270 && clampedAngle <= 315) //7
			{
				float relativeAngle = clampedAngle - 270;
				float z = origin.z + (origin.x * Mathf.Tan(relativeAngle * Mathf.Deg2Rad));
				edgePoint = new Vector3(0, origin.y, z);
			}
			else if (clampedAngle >= 315 && clampedAngle <= 360) //8
			{
				float relativeAngle = 360 - clampedAngle;
				float x = origin.x - ((maxZ - origin.z) * Mathf.Tan(relativeAngle * Mathf.Deg2Rad));
				edgePoint = new Vector3(x, origin.y, maxZ);
			}
			return edgePoint;
		}

		/// <summary>
		/// Get point on edge of square map given angle (0 to 360) relative to x axis from origin
		/// </summary>
		/// <param name="angle"></param>
		/// <param name="map"></param>
		public static IntVec3 PointFromOrigin(float angle, Map map)
		{
			int a = map.Size.x;
			int b = map.Size.z;

			if (angle < 0 || angle > 360)
			{
				return IntVec3.Invalid;
			}

			Rot4 rayDir = Rot4.Invalid;
			if (angle <= 45 || angle > 315)
			{
				rayDir = Rot4.East;
			}
			else if (angle <= 135 && angle >= 45)
			{
				rayDir = Rot4.North;
			}
			else if (angle <= 225 && angle >= 135)
			{
				rayDir = Rot4.West;
			}
			else if (angle <= 315 && angle >= 225)
			{
				rayDir = Rot4.South;
			}
			else
			{
				return new IntVec3(b / 2, 0, 1);
			}
			float v = Mathf.Tan(angle * Mathf.Deg2Rad);
			return rayDir.AsInt switch
			{
				//North
				0 => new IntVec3((int)(b / (2 * v) + b / 2), 0, b - 1),
				//East
				1 => new IntVec3(a - 1, 0, (int)(a / 2 * v) + a / 2),
				//South
				2 => new IntVec3((int)(b - (b / (2 * v) + b / 2)), 0, 1),
				//West
				3 => new IntVec3(1, 0, (int)(a - ((a / 2 * v) + a / 2))),
				//Fallthrough - should never hit
				_ => IntVec3.Invalid,
			};
		}

		/// <summary>
		/// Spherical distance from <paramref name="source"/> to <paramref name="target"/> on the World Map approximated by tiles
		/// </summary>
		/// <param name="source"></param>
		/// <param name="target"></param>
		public static float SphericalDistance(Vector3 source, Vector3 target)
		{
			float sphericalDistance = GenMath.SphericalDistance(source.normalized, target.normalized);
			return Find.WorldGrid.ApproxDistanceInTiles(sphericalDistance);
		}
	}
}
