using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace SmashTools.Animations
{
	public class AnimationCurve
	{
		public List<KeyFrame> points = new List<KeyFrame>();

		public KeyFrame LeftBound => points.FirstOrDefault();

		public KeyFrame RightBound => points.LastOrDefault();

		public int PointsCount => points.Count;

		public bool IsValid => !points.NullOrEmpty();

		public float this[int frame]
		{
			get
			{
				return Function(frame);
			}
		}

		public void SetValue(int frame, float value)
		{
			for (int i = 0; i < points.Count; i++)
			{
				KeyFrame point = points[i];
				if (point.frame == frame)
				{
					points[i] = new KeyFrame()
					{
						frame = point.frame,
						value = value,
					};
					break;
				}
				else if (point.frame > frame)
				{
					points.Insert(i, new KeyFrame()
					{
						frame = frame,
						value = value,
					});
					break;
				}
			}
		}

		private float Function(int frame)
		{
			if (points.NullOrEmpty() || RightBound.frame <= 0)
			{
				return 0;
			}
			if (points.Count == 1)
			{
				return LeftBound.value;
			}
			if (frame <= LeftBound.frame)
			{
				return LeftBound.value;
			}
			else if (frame >= RightBound.frame)
			{
				return RightBound.value;
			}
			return LagrangeFunction(points, frame);
		}

		/// <summary>
		/// Interpolation using Lagrange polynomial
		/// </summary>
		/// <remarks>See https://paulbourke.net/miscellaneous/interpolation and https://en.wikipedia.org/wiki/Lagrange_polynomial for reference</remarks>
		/// <param name="coordinates"></param>
		/// <param name="x"></param>
		private float LagrangeFunction(List<KeyFrame> coordinates, int frame)
		{
			float y = 0;
			float t = (float)frame / RightBound.frame;
			/// Σ{n-1:i=0} y * ∏{n-1:j=0,j≠i} (x - xj) / (xi - xj)
			/// <summary>Summation from 0 to n-1, and multiplicative of 0 to n-1, given an arbitrary set of points</summary>
			for (int i = 0; i < coordinates.Count; i++)
			{
				KeyFrame pointLeft = coordinates[i];
				float pointLeftT = (float)pointLeft.frame / RightBound.frame;
				float numerator = pointLeft.value;
				float denominator = 1;
				for (int j = 0; j < coordinates.Count; j++)
				{
					if (i != j)
					{
						KeyFrame pointRight = coordinates[j];
						float pointRightT = (float)pointRight.frame / RightBound.frame;
						numerator *= t - pointRightT;
						denominator *= pointLeftT - pointRightT;
					}
				}
				y += numerator / denominator;
			}
			return y;
		}

		public struct KeyFrame : IExposable
		{
			public int frame;
			public float value;

			public void ExposeData()
			{
				Scribe_Values.Look(ref frame, nameof(frame));
				Scribe_Values.Look(ref value, nameof(value));
			}
		}
	}
}
