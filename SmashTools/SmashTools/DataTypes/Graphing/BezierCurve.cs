using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace SmashTools
{
	public class BezierCurve : LinearCurve
	{
		public BezierCurve() : base()
		{
		}

		public BezierCurve(List<CurvePoint> points) : base(points)
		{
		}

		public static Vector2 BezierFunction(List<CurvePoint> controlPoints, float t)
		{
			if (t <= 0)
			{
				return controlPoints.FirstOrDefault();
			}
			if (t >= 1)
			{
				return controlPoints.LastOrDefault();
			}
			int n = controlPoints.Count - 1;
			if (n > 16)
			{
				Debug.LogError("Max number of control points is 16, factorials are precalculated.");
				n = 16;
			}

			Vector2 lerp = Vector2.zero;
			for (int i = 0; i < controlPoints.Count; i++)
			{
				lerp += Ext_Math.Bernstein(n, i, t) * controlPoints[i].Loc;
			}
			return lerp;
		}

		public override Vector2 Function(float x)
		{
			if (points.Count < 3)
			{
				//No control points -> linear curve
				return base.Function(x);
			}
			if (LeftBound.x == x)
			{
				return LeftBound;
			}
			else if (RightBound.x == x)
			{
				return RightBound;
			}
			float t = (x - LeftBound.x) / (RightBound.x - LeftBound.x);
			return BezierFunction(points, t);
		}

		public override Vector2 EvaluateT(float t)
		{
			if (t <= 0)
			{
				return LeftBound;
			}
			if (t >= 1)
			{
				return RightBound;
			}
			return BezierFunction(points, t);
		}
	}
}
