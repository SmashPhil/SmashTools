using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;

namespace SmashTools
{
	/// <summary>
	/// Same functionality as SimpleCurve, but with inheritance allowed and shared parent type with BezierCurve.
	/// </summary>
	public class LinearCurve : Curve
	{
		public LinearCurve() : base()
		{
		}

		public LinearCurve(List<CurvePoint> points) : base(points)
		{
		}

		public override float Evaluate(float x)
		{
			if (points.Count == 0)
			{
				Log.Error("Evaluating a LinearCurve with no points.");
				return 0f;
			}
			if (x <= points[0].x)
			{
				return points[0].y;
			}
			if (x >= points[points.Count - 1].x)
			{
				return points[points.Count - 1].y;
			}
			CurvePoint leftPoint = points[0];
			CurvePoint rightPoint = points[points.Count - 1];
			for (int i = 0; i < points.Count; i++)
			{
				if (x <= points[i].x)
				{
					rightPoint = points[i];
					if (i > 0)
					{
						leftPoint = points[i - 1];
					}
					break;
				}
			}
			float t = (x - leftPoint.x) / (rightPoint.x - leftPoint.x);
			return Mathf.Lerp(leftPoint.y, rightPoint.y, t);
		}
	}
}
