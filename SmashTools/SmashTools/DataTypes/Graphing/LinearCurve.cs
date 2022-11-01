using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;

namespace SmashTools
{
	public class LinearCurve : IEnumerable<CurvePoint>, IEnumerable
	{
		public List<CurvePoint> points = new List<CurvePoint>();

		public LinearCurve()
		{
		}

		public LinearCurve(List<CurvePoint> points)
		{
			this.points = new List<CurvePoint>(points);
		}

		public CurvePoint LeftBound => points.FirstOrDefault();

		public CurvePoint RightBound => points.LastOrDefault();

		public int PointsCount => points.Count;

		public CurvePoint this[int i]
		{
			get
			{
				return points[i];
			}
			set
			{
				points[i] = value;
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public IEnumerator<CurvePoint> GetEnumerator()
		{
			foreach (CurvePoint point in points)
			{
				yield return point;
			}
		}

		public virtual void Add(CurvePoint curvePoint)
		{
			points.Add(curvePoint);
		}

		public float Evaluate(float x)
		{
			return Function(x).y;
		}

		public virtual Vector2 Function(float x)
		{
			if (points.Count == 0)
			{
				Log.Error("Evaluating a LinearCurve with no points.");
				return Vector2.zero;
			}
			if (x <= points[0].x)
			{
				return points[0];
			}
			if (x >= points[points.Count - 1].x)
			{
				return points[points.Count - 1];
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
			return new Vector2(x, Mathf.Lerp(leftPoint.y, rightPoint.y, t));
		}

		public virtual Vector2 EvaluateT(float t)
		{
			if (t <= 0)
			{
				return LeftBound;
			}
			if (t >= 1)
			{
				return RightBound;
			}
			float x = t * RightBound.x;
			return Function(x);
		}

		public virtual void Graph()
		{
			FloatRange xRange = new FloatRange(LeftBound.x, RightBound.x);
			Find.WindowStack.Add(new Dialog_Graph(Function, xRange, points));
		}

		public static implicit operator Graph.Function(LinearCurve curve)
		{
			return curve.Function;
		}
	}
}
