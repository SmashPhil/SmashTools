using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace SmashTools
{
	public class Dialog_GraphEditor : Dialog_Graph
	{
		private Graph.GraphType graphType;
		private Curve curve;

		public Dialog_GraphEditor() : base(null, new FloatRange(0, 5), new List<CurvePoint>())
		{
			doCloseX = true;
			forcePause = true;
			GraphType = Graph.GraphType.Linear;
			ReinstantiateCurve();
		}

		public Dialog_GraphEditor(Graph.Function function, FloatRange range) : base(function, range, new List<CurvePoint>())
		{
			doCloseX = true;
			forcePause = true;
			GraphType = Graph.GraphType.Linear;
			ReinstantiateCurve();
		}

		public Dialog_GraphEditor(Graph.Function function, FloatRange range, List<CurvePoint> plotPoints) : base(function, range, plotPoints)
		{
			doCloseX = true;
			forcePause = true;
			GraphType = Graph.GraphType.Linear;
			ReinstantiateCurve();
		}

		public override List<CurvePoint> CurvePoints => curve.points;

		public override Vector2 InitialSize
		{
			get
			{
				float minSize = Mathf.Min(UI.screenWidth, UI.screenHeight);
				return new Vector2(minSize * 1.5f, minSize);
			}
		}

		public Graph.GraphType GraphType
		{
			get
			{
				return graphType;
			}
			set
			{
				graphType = value;
				ReinstantiateCurve();
			}
		}

		public Type CurveType
		{
			get
			{
				return GraphType switch
				{
					Graph.GraphType.Linear => typeof(LinearCurve),
					Graph.GraphType.Bezier => typeof(BezierCurve),
					_ => null
				};
			}
		}

		public void ReinstantiateCurve()
		{
			curve = (Curve)Activator.CreateInstance(CurveType, curve?.points ?? new List<CurvePoint>());
			Function = new Graph.Function(curve.Evaluate);
		}

		public override void DoWindowContents(Rect inRect)
		{
			Rect inputRect = new Rect(inRect)
			{
				width = InitialSize.x - InitialSize.y - 10
			};

			GUIState.Push();
			try
			{
				Widgets.DrawMenuSection(inputRect);
				inputRect = inputRect.ContractedBy(10);
				Rect graphTypeRect = new Rect(inputRect.x, inputRect.y, 120, 30);
				if (Widgets.ButtonText(graphTypeRect, graphType.ToString()))
				{
					List<FloatMenuOption> floatMenuOptions = new List<FloatMenuOption>();
					floatMenuOptions.Add(new FloatMenuOption(Graph.GraphType.Linear.ToString(), () => GraphType = Graph.GraphType.Linear));
					floatMenuOptions.Add(new FloatMenuOption(Graph.GraphType.Bezier.ToString(), () => GraphType = Graph.GraphType.Bezier));
					//floatMenuOptions.Add(new FloatMenuOption(Graph.GraphType.Freeform.ToString(), () => graphType = Graph.GraphType.Freeform));
					Find.WindowStack.Add(new FloatMenu(floatMenuOptions));
				}

				string formula = graphType switch
				{
					Graph.GraphType.Linear => "y = mx + b",
					Graph.GraphType.Bezier => "Σ{n:i=0} nC_i((1-t)^(n-i))(t^i)(P_i)",
					_ => string.Empty
				};
				Vector2 textSize = Text.CalcSize(formula);
				Rect formulaRect = new Rect(inputRect.x + inputRect.width - textSize.x, inputRect.y, textSize.x, textSize.y);
				//Widgets.Label(formulaRect, formula);

				Text.Font = GameFont.Medium;
				Rect graphSizeRect = new Rect(inputRect.x, formulaRect.yMax + 10, inputRect.width / 3, 30);
				Widgets.Label(graphSizeRect, "Axis Limits");
				GUIState.Reset();

				float xMin = XRange.min;
				float xMax = XRange.max;
				Rect xRangeRect = new Rect(inputRect.x, graphSizeRect.yMax + 10, inputRect.width / 3, 30);
				
				UIElements.NumericBox(xRangeRect, ref xMin, "xMin", string.Empty, string.Empty, float.MinValue, float.MaxValue);
				xRangeRect.x += xRangeRect.width;
				UIElements.NumericBox(xRangeRect, ref xMax, "xMax", string.Empty, string.Empty, float.MinValue, float.MaxValue);
				

				float yMin = YRange.min;
				float yMax = YRange.max;
				Rect yRangeRect = new Rect(inputRect.x, xRangeRect.yMax + 10, inputRect.width / 3, 30);
				UIElements.NumericBox(yRangeRect, ref yMin, "yMin", string.Empty, string.Empty, float.MinValue, float.MaxValue);
				yRangeRect.x += yRangeRect.width;
				UIElements.NumericBox(yRangeRect, ref yMax, "yMax", string.Empty, string.Empty, float.MinValue, float.MaxValue);

				if (xMax - xMin < 1)
				{
					xMax = xMin + 1;
				}
				if (xMin - xMax > -1)
				{
					xMin = xMax - 1;
				}
				if (yMax - yMin < 1)
				{
					yMax = yMin + 1;
				}
				if (yMin - yMax > -1)
				{
					yMin = yMax - 1;
				}

				(float xMin, float xMax, float yMin, float yMax) bounds = GetPlotBounds();
				xMin = Mathf.Min(xMin, bounds.xMin);
				xMax = Mathf.Max(xMin, bounds.xMax);
				yMin = Mathf.Min(xMin, bounds.yMin);
				yMax = Mathf.Max(xMin, bounds.yMax);

				XRange = new FloatRange(xMin, xMax);
				YRange = new FloatRange(yMin, yMax);

				Rect coordinatesLabelRect = new Rect(inputRect.x, yRangeRect.yMax + 5, inputRect.width, 30);
				Widgets.Label(coordinatesLabelRect, "Coordinates");

				Rect coordinatesListRect = new Rect(inputRect.x, coordinatesLabelRect.yMax + 5, inputRect.width / 2, 30);
				if (!curve.points.NullOrEmpty())
				{
					for (int i = 0; i < curve.points.Count; i++)
					{
						CurvePoint coord = curve.points[i];
						Vector2 vector = UIElements.Vector2Box(coordinatesListRect, $"p{i}", coord, labelProportion: 0.1f);
						curve.points[i] = new CurvePoint(vector);
						coordinatesListRect.y += coordinatesListRect.height;
					}
				}

				Rect addCoordinateRect = new Rect(inputRect.x, coordinatesListRect.yMax + 5, 90, 30);
				if (Widgets.ButtonText(addCoordinateRect, "Add Point"))
				{
					CurvePoint curvePoint = curve.points.NullOrEmpty() ? new CurvePoint(0, 0) : new CurvePoint(XRange.Average, YRange.Average);
					curve.Add(curvePoint);
				}

				Rect graphRect = new Rect(inRect.width - inRect.height, 0, inRect.height, inRect.height);
				base.DoWindowContents(graphRect);
			}
			catch (Exception ex)
			{
				Log.Error($"Exception thrown while in graph editor. Exception = {ex}");
			}
			GUIState.Pop();
		}

		public (float minX, float maxX, float minY, float maxY) GetPlotBounds()
		{
			if (curve.points.NullOrEmpty())
			{
				return (0, 1, 0, 1);
			}
			float minXPoint = curve.points.Min(curvePoint => curvePoint.x);
			float maxXPoint = curve.points.Max(curvePoint => curvePoint.x);
			float minYPoint = curve.points.Min(curvePoint => curvePoint.y);
			float maxYPoint = curve.points.Max(curvePoint => curvePoint.y);
			return (minXPoint, maxXPoint, minYPoint, maxYPoint);
		}
	}
}
