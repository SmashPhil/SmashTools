using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using System;

namespace SmashTools
{
	public static class Ext_Numeric
	{
		/// <summary>
		/// Clamp value of type T between max and min
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="val"></param>
		/// <param name="min"></param>
		/// <param name="max"></param>
		public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
		{
			if (val.CompareTo(min) < 0) return min;
			if (val.CompareTo(max) > 0) return max;
			return val;
		}

		/// <summary>
		/// Clamp value between a min and max but wrap around rather than return min / max
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="val"></param>
		/// <param name="min"></param>
		/// <param name="max"></param>
		public static float ClampAndWrap(this float val, float min, float max)
		{
			while (val < min || val > max)
			{
				if (val < min)
				{
					val += max;
				}
				if (val > max)
				{
					val -= max;
				}
			}
			return val;
		}

		/// <summary>
		/// Clamp value between a in and max but wrap around rather than return min / max
		/// </summary>
		/// <param name="val"></param>
		/// <param name="min"></param>
		/// <param name="max"></param>
		public static int ClampAndWrap(this int val, int min, int max)
		{
			while (val < min || val > max)
			{
				if (val < min)
				{
					val += max;
				}
				if (val > max)
				{
					val -= max;
				}
			}
			return val;
		}

		/// <summary>
		/// Convert > 360 and < 0 angles to relative 0:360 angles in a unit circle
		/// </summary>
		/// <param name="theta"></param>
		public static float ClampAngle(this float theta)
		{
			while (theta > 360 || theta < 0)
			{
				if (theta > 360)
				{
					theta -= 360;
				}
				else if (theta < 0)
				{
					theta += 360;
				}
			}
			return theta;
		}

		public static bool InRange(this FloatRange range, float value)
		{
			return value >= range.min && value <= range.max;
		}

		public static bool InRange(this IntRange range, int value)
		{
			return value >= range.min && value <= range.max;
		}
	}
}
