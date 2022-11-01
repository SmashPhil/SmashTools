using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace SmashTools
{
	public class Dialog_GraphEditor : Dialog_Graph
	{
		/// <summary>
		/// For all animations, t is the time ratio between 0 and max ticks.
		/// Bounds: 0 <= t <= 1
		/// </summary>
		public const float DefaultMinX = 0;
		public const float DefaultMaxX = 1;

		public const float CoordinatesListButtonSize = 24;

		private Graph.GraphType graphType;
		private LinearCurve curve;

		private Vector2 scrollPos;
		private Thing animationTarget;

		public Dialog_GraphEditor() : base(null, new FloatRange(DefaultMinX, DefaultMaxX), new List<CurvePoint>())
		{
			doCloseX = true;
			forcePause = true;
			GraphType = Graph.GraphType.Linear;
			ReinstantiateCurve();
		}

		public Dialog_GraphEditor(Graph.Function function, FloatRange range, bool vectorEvaluation = false) : base(function, range, new List<CurvePoint>(), vectorEvaluation)
		{
			doCloseX = true;
			forcePause = true;
			GraphType = Graph.GraphType.Linear;
			ReinstantiateCurve();
		}

		public Dialog_GraphEditor(Graph.Function function, FloatRange range, List<CurvePoint> plotPoints, bool vectorEvaluation = false) : base(function, range, plotPoints, vectorEvaluation)
		{
			doCloseX = true;
			forcePause = true;
			GraphType = Graph.GraphType.Linear;
			ReinstantiateCurve();
		}

		public Dialog_GraphEditor(Thing animationTarget = null, bool vectorEvaluation = false) : base(null, new FloatRange(DefaultMinX, DefaultMaxX), plotPoints: new List<CurvePoint>(), vectorEvaluation: vectorEvaluation)
		{
			doCloseX = true;
			forcePause = true;
			GraphType = Graph.GraphType.Linear;
			this.animationTarget = animationTarget;
			ReinstantiateCurve();
		}

		public override List<CurvePoint> CurvePoints => curve.points;

		public override bool Editable => true;

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

		public Graph.GraphType CurveToGraphType(Type type)
		{
			if (type == typeof(LinearCurve))
			{
				return Graph.GraphType.Linear;
			}
			if (type == typeof(BezierCurve))
			{
				return Graph.GraphType.Bezier;
			}
			throw new NotImplementedException("GraphType");
		}

		public void ReinstantiateCurve()
		{
			curve = (LinearCurve)Activator.CreateInstance(CurveType, curve?.points ?? new List<CurvePoint>());
			Function = new Graph.Function(curve.Function);
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
				Rect evaluationTypeRect = new Rect(inputRect.x + inputRect.width - graphTypeRect.width - 5, graphTypeRect.y, graphTypeRect.width, graphTypeRect.height);
				if (Widgets.ButtonText(evaluationTypeRect, VectorEvaluation ? "Vector" : "Simplified"))
				{
					VectorEvaluation = !VectorEvaluation;
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
				
				//UIElements.NumericBox(xRangeRect, ref xMin, "xMin", string.Empty, string.Empty, float.MinValue, float.MaxValue);
				xRangeRect.x += xRangeRect.width;
				//UIElements.NumericBox(xRangeRect, ref xMax, "xMax", string.Empty, string.Empty, float.MinValue, float.MaxValue);
				

				float yMin = YRange.min;
				float yMax = YRange.max;
				//NOTE - If x axis becomes editable again, swap graphSizeRect.yMax to xRangeRect.yMax
				Rect yRangeRect = new Rect(inputRect.x, graphSizeRect.yMax + 10, inputRect.width / 3, 30);
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
				//xMin = Mathf.Min(xMin, bounds.xMin);
				//xMax = Mathf.Max(xMin, bounds.xMax);
				//yMin = Mathf.Min(xMin, bounds.yMin);
				//yMax = Mathf.Max(xMin, bounds.yMax);

				XRange = new FloatRange(xMin, xMax);
				YRange = new FloatRange(yMin, yMax);

				Rect coordinatesLabelRect = new Rect(inputRect.x, yRangeRect.yMax + 5, inputRect.width, Text.CalcHeight("Coordinates", inputRect.width));
				Widgets.Label(coordinatesLabelRect, "Coordinates");

				Rect coordinateOutRect = new Rect(inputRect.x, coordinatesLabelRect.yMax + 5, inputRect.width, inputRect.height / 2);
				float curvePointsMenuHeight = (curve.PointsCount + 1) * CoordinatesListButtonSize * 2 - 10;
				//16 for width of scrollbar
				Rect coordinateViewRect = new Rect(coordinateOutRect.x, coordinateOutRect.y, coordinateOutRect.width - 16, curvePointsMenuHeight);
				Widgets.DrawMenuSection(coordinateOutRect);
				Widgets.BeginScrollView(coordinateOutRect, ref scrollPos, coordinateViewRect);
				{
					Rect coordinatesListRect = new Rect(coordinateViewRect.x, coordinateViewRect.y, coordinateViewRect.width, CoordinatesListButtonSize * 2).ContractedBy(5);
					if (!curve.points.NullOrEmpty())
					{
						GUIState.Push();
						{
							int indexToRemove = -1;
							for (int i = 0; i < curve.PointsCount; i++)
							{
								UIHighlighter.HighlightOpportunity(coordinatesListRect, $"GraphEditor_CurvePoint_{i}");
								Rect buttonArrowRect = new Rect(coordinatesListRect)
								{
									width = CoordinatesListButtonSize,
									height = coordinatesListRect.height / 2
								};

								if (i == 0) GUIState.Disable();
								if (Widgets.ButtonImage(buttonArrowRect, TexButton.ReorderUp, GUI.enabled ? Color.white : UIElements.InactiveColor, GUI.enabled ? GenUI.MouseoverColor : UIElements.InactiveColor, doMouseoverSound: GUI.enabled) && GUI.enabled)
								{
									curve.points.Swap(i, i - 1);
								}
								GUIState.Enable();

								buttonArrowRect.y += buttonArrowRect.height;

								if (i == curve.PointsCount - 1) GUIState.Disable();
								if (Widgets.ButtonImage(buttonArrowRect, TexButton.ReorderDown, GUI.enabled ? Color.white : UIElements.InactiveColor, GUI.enabled ? GenUI.MouseoverColor : UIElements.InactiveColor, doMouseoverSound: GUI.enabled) && GUI.enabled)
								{
									curve.points.Swap(i, i + 1);
								}
								GUIState.Enable();

								CurvePoint coord = curve.points[i];
								Rect vectorRect = new Rect(coordinatesListRect)
								{
									x = coordinatesListRect.x + buttonArrowRect.width + 10,
									width = (coordinatesListRect.width - CoordinatesListButtonSize) / 7
								};
								float x = UIElements.NumericBox(vectorRect, coord.x, "X", string.Empty, string.Empty, labelProportion: 0.25f);
								vectorRect.x += vectorRect.width + 5;
								float y = UIElements.NumericBox(vectorRect, coord.y, "Y", string.Empty, string.Empty, labelProportion: 0.25f);
								curve.points[i] = new CurvePoint(x.RoundTo(0.01f), y.RoundTo(0.01f));

								Rect deleteButtonRect = new Rect(coordinatesListRect.width - CoordinatesListButtonSize, coordinatesListRect.y + (coordinatesListRect.height - CoordinatesListButtonSize) / 2, CoordinatesListButtonSize, CoordinatesListButtonSize);
								if (Widgets.ButtonImage(deleteButtonRect, TexButton.Minus))
								{
									indexToRemove = i;
								}

								coordinatesListRect.y += coordinatesListRect.height;
							}
							if (indexToRemove >= 0)
							{
								curve.points.RemoveAt(indexToRemove);
							}
						}
						GUIState.Pop();
					}

					Rect addCoordinateRect = new Rect(coordinatesListRect.x, coordinatesListRect.y + (coordinatesListRect.height - CoordinatesListButtonSize) / 2, CoordinatesListButtonSize, CoordinatesListButtonSize);
					if (Widgets.ButtonImage(addCoordinateRect, TexButton.Plus))
					{
						CurvePoint curvePoint = curve.points.NullOrEmpty() ? new CurvePoint(0, 0) : new CurvePoint(XRange.Average, YRange.Average);
						curve.Add(curvePoint);
					}
				}
				Widgets.EndScrollView();

				float cameraViewerHeight = inputRect.height - coordinateOutRect.yMax;
				Rect cameraViewerRect = new Rect(coordinateOutRect.x, coordinateOutRect.yMax + 5, coordinateOutRect.width, cameraViewerHeight - 5);
				DrawPreviewWindow(cameraViewerRect);

				Rect graphRect = new Rect(inRect.width - inRect.height, 0, inRect.height, inRect.height);
				base.DoWindowContents(graphRect);
			}
			catch (Exception ex)
			{
				Log.Error($"Exception thrown while in graph editor. Exception = {ex}");
			}
			GUIState.Pop();
		}

		private void DrawPreviewWindow(Rect rect)
		{
			Widgets.DrawMenuSection(rect);
			if (animationTarget is null)
			{
				UIElements.Header(rect, "Preview Window Disabled", ListingExtension.BannerColor, anchor: TextAnchor.MiddleCenter);
			}
			else
			{

			}
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
