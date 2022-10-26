using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace SmashTools
{
	public class BezierCurve : Curve
	{
		public BezierCurve() : base()
		{
		}

		public BezierCurve(List<CurvePoint> points) : base(points)
		{
		}

		public static float BezierFunction(List<CurvePoint> controlPoints, float t)
		{
			if (t <= 0)
			{
				return controlPoints.FirstOrDefault().y;
			}
			if (t >= 1)
			{
				return controlPoints.LastOrDefault().y;
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
			return lerp.y;
		}

		public override float Evaluate(float x)
		{
			if (points is null || points.Count < 2)
			{
				return 0;
			}
			if (LeftBound.x == x)
			{
				return LeftBound.y;
			}
			else if (RightBound.x == x)
			{
				return RightBound.y;
			}
			else if (LeftBound.x < x && RightBound.x > x)
			{
				
			}
			float t = (x - LeftBound.x) / (RightBound.x - LeftBound.x);
			return BezierFunction(points, t);
		}
	}
}
