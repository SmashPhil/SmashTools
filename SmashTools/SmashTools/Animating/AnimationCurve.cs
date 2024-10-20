using SmashTools.Xml;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Verse;
using static SmashTools.Debug;

namespace SmashTools.Animations
{
	public sealed class AnimationCurve : IXmlExport
	{
		public List<KeyFrame> points = new List<KeyFrame>();

		public KeyFrame LeftBound => points.Count > 0 ? points[0] : KeyFrame.Invalid;

		public KeyFrame RightBound => points.Count > 0 ? points[points.Count - 1] : KeyFrame.Invalid;

		public FloatRange RangeX => new FloatRange(LeftBound.frame, RightBound.frame);

		public FloatRange RangeY => throw new NotImplementedException();

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
			points.Add(new KeyFrame(frame, value));
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
					points[i] = new KeyFrame(point.frame, value);
					return;
				}
				if (point.frame > frame) 
				{
					points.Insert(i, new KeyFrame(frame, value));
					return;
				}
			}
			Add(frame, value); // Only add if insert attempt failed
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

		public bool KeyFrameAt(float frame)
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

		public float Function(float frame)
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
			return CubicSpline(frame);
		}

		/// <summary>
		/// Catmull-Rom spline interpolation
		/// </summary>
		/// <remarks>See https://github.khronos.org/glTF-Tutorials/gltfTutorial/gltfTutorial_007_Animations.html#cubic-spline-interpolation for reference</remarks>
		private float CubicSpline(float frame)
		{
			KeyFrame prev = KeyFrame.Invalid;
			KeyFrame next = KeyFrame.Invalid;
			for (int i = 0; i < points.Count; i++)
			{
				if (points[i].frame == frame)
				{
					return points[i].value;
				}
				if (points[i].frame > frame)
				{
					break;
				}
				Assert(!points.OutOfBounds(i + 1));
				prev = points[i];
				next = points[i + 1];
			}
			if (prev.outTangent == float.PositiveInfinity) return prev.value;
			if (prev.outTangent == float.NegativeInfinity) return next.value;
			if (next.inTangent == float.PositiveInfinity) return prev.value;
			if (next.inTangent == float.NegativeInfinity) return next.value;

			float deltaFrame = next.frame - prev.frame;
			// (f - kt(i))
			float t = (frame - prev.frame) / deltaFrame;
			// t^2
			float t2 = t * t;
			// t^3
			float t3 = t * t * t;
			// k(0) * (2t^3 - 3t^2 + 1) +
			// k(1) * (3t^2 - 2t^3) +
			// k'(0) * (t^3 - 2t^2 + t +
			// k'(1) * (t^3 - t^2)
			return prev.value * (2 * t3 - 3 * t2 + 1) +
				   next.value * (3 * t2 - 2 * t3) +
				   deltaFrame * prev.outTangent * (t3 - 2 * t2 + t) +
				   deltaFrame * next.inTangent * (t3 - t2);
		}

		// Linear function for testing rendering in the animation editor
		private float Lerp(float frame)
		{
			KeyFrame leftPoint = points[0];
			KeyFrame rightPoint = points[points.Count - 1];
			for (int i = 0; i < points.Count; i++)
			{
				if (frame <= points[i].frame)
				{
					rightPoint = points[i];
					if (i > 0)
					{
						leftPoint = points[i - 1];
					}
					break;
				}
			}
			float t = (frame - leftPoint.frame) / (rightPoint.frame - leftPoint.frame);
			return Mathf.LerpUnclamped(leftPoint.value, rightPoint.value, t);
		}

		void IXmlExport.Export()
		{
			XmlExporter.WriteCollection(nameof(points), points);
		}
	}
}
