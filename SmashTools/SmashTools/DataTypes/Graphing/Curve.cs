using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace SmashTools
{
	public abstract class Curve : IEnumerable<CurvePoint>, IEnumerable
	{
		public List<CurvePoint> points = new List<CurvePoint>();

		public Curve()
		{
		}

		public Curve(List<CurvePoint> points)
		{
			this.points = new List<CurvePoint>(points);
		}

		public CurvePoint LeftBound => points.FirstOrDefault();

		public CurvePoint RightBound => points.LastOrDefault();

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

		public abstract float Evaluate(float x);

		public virtual void Graph()
		{
			FloatRange xRange = new FloatRange(LeftBound.x, RightBound.x);
			Find.WindowStack.Add(new Dialog_Graph(Evaluate, xRange, points));
		}
	}
}
