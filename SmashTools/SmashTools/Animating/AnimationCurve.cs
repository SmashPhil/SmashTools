using SmashTools.Xml;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Verse;

namespace SmashTools.Animations
{
	public class AnimationCurve : IXmlExport
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

		public bool Add(int frame, float value)
		{
			for (int i = 0; i < points.Count; i++)
			{
				KeyFrame point = points[i];
				if (point.frame == frame)
				{
					return false;
				}
			}
			points.Add(new KeyFrame()
			{
				frame = frame,
				value = value,
			});
			points.Sort();
			return true;
		}

		public void Set(int frame, float value)
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
					return;
				}
				if (point.frame > frame) 
				{
					points.Insert(i, new KeyFrame()
					{
						frame = frame,
						value = value,
					});
					return;
				}
			}
			Add(frame, value); //Only adds if insert attempt failed
		}

		public void Remove(int frame)
		{
			for (int i = 0; i < points.Count; i++)
			{
				KeyFrame point = points[i];
				if (point.frame == frame)
				{
					points.RemoveAt(i);
					break;
				}
			}
		}

		public bool KeyFrameAt(int frame)
		{
			foreach (KeyFrame keyFrame in points)
			{
				if (keyFrame.frame == frame)
				{
					return true;
				}
				else if (keyFrame.frame > frame)
				{
					return false; //If past the frame check point, it won't be found at future points. Curve is kept sorted at all times
				}
			}
			return false;
		}

		public float Function(int frame)
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

		void IXmlExport.Export()
		{
			XmlExporter.WriteList(nameof(points), points);
		}

		public struct KeyFrame : IExposable, IXmlExport, IComparable<KeyFrame>
		{
			public int frame;
			public float value;

			void IExposable.ExposeData()
			{
				Scribe_Values.Look(ref frame, nameof(frame));
				Scribe_Values.Look(ref value, nameof(value));
			}

			readonly void IXmlExport.Export()
			{
				XmlExporter.WriteString($"({frame},{Ext_Math.RoundTo(value, 0.0001f)})");
			}

			readonly int IComparable<KeyFrame>.CompareTo(KeyFrame other)
			{
				return frame.CompareTo(other.frame);
			}

			public static KeyFrame FromString(string entry)
			{
				entry = entry.Replace("(", "");
				entry = entry.Replace(")", "");
				string[] array = entry.Split(',');

				if (array.Length == 2)
				{
					CultureInfo invariantCulture = CultureInfo.InvariantCulture;
					KeyFrame keyFrame = new KeyFrame
					{
						frame = Convert.ToInt32(array[0], invariantCulture),
						value = Convert.ToSingle(array[1], invariantCulture)
					};
					return keyFrame;
				}
				throw new InvalidOperationException();
			}
		}
	}
}
