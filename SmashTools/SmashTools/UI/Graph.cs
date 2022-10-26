using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace SmashTools
{
	[StaticConstructorOnStartup]
	public static class Graph
	{
		public const int AxisMajorNotchCount = 5;
		public const int AxisNotchCount = 40;
		public const float NotchSize = 5;
		public const float GraphNodeSize = 8;

		private static readonly Texture2D CurvePoint = ContentFinder<Texture2D>.Get("UI/Widgets/Dev/CurvePoint", true);

		private static int draggingPlotPointIndex = -1;

		//XY Function
		public delegate float Function(float x);

		public static void DrawGraph(Rect rect, Function function, FloatRange xRange, FloatRange yRange, List<CurvePoint> plotPoints = null)
		{
			Rect axisRect = rect.ContractedBy(5);
			DrawAxis(axisRect, xRange, yRange);
			if (function != null && !plotPoints.NullOrEmpty())
			{
				PlotFunction(axisRect, function, xRange, yRange, plotPoints);
				//DrawLegend(graphRect);
			}
		}

		private static void DrawAxis(Rect rect, FloatRange xRange, FloatRange yRange)
		{
			float xAxisPos = 0;
			float yAxisPos = 0;

			if (xRange.min < 0)
			{
				xAxisPos = Mathf.Abs(xRange.min / xRange.max) * rect.width;
				yAxisPos = Mathf.Abs(yRange.min / yRange.max) * rect.height;
			}

			Vector2 xAxis = new Vector2(rect.x, rect.y + rect.height - yAxisPos);
			Widgets.DrawLineHorizontal(xAxis.x, xAxis.y, rect.width);
			Vector2 yAxis = new Vector2(rect.x + xAxisPos, rect.y);
			Widgets.DrawLineVertical(yAxis.x, yAxis.y, rect.height);

			float stepSizeX = rect.width / AxisNotchCount;
			float stepSizeY = rect.height / AxisNotchCount;
			for (int i = 0; i < AxisNotchCount + 1; i++)
			{
				bool majorNotch = i % 5 == 0;
				float notchSize = majorNotch ? NotchSize * 2 : NotchSize;

				Vector2 xAxisNotchCoord = new Vector2(xAxis.x + stepSizeX * i, xAxis.y - notchSize / 2);
				Widgets.DrawLineVertical(xAxisNotchCoord.x, xAxisNotchCoord.y, notchSize);

				Vector2 yAxisNotchCoord = new Vector2(yAxis.x - notchSize / 2, xAxis.y - stepSizeY * i);
				Widgets.DrawLineHorizontal(yAxisNotchCoord.x, yAxisNotchCoord.y, notchSize);

				if (majorNotch)
				{
					float axisXNum = xRange.min + xRange.max * ((float)i / AxisNotchCount);
					string labelTextX = axisXNum.RoundTo(0.1f).ToString();
					Vector2 textSizeX = Text.CalcSize(labelTextX);
					Rect xNumRect = new Rect(xAxisNotchCoord.x - textSizeX.x / 2, xAxis.y + NotchSize * 2, textSizeX.x, textSizeX.y);
					Widgets.Label(xNumRect, labelTextX);

					float axisYNum = yRange.min + yRange.max * ((float)i / AxisNotchCount);
					string labelTextY = axisYNum.RoundTo(0.1f).ToString();
					Vector2 textSizeY = Text.CalcSize(labelTextY);
					Rect yNumRect = new Rect(rect.x + xAxisPos - NotchSize * 2 - textSizeY.x, yAxisNotchCoord.y - textSizeY.y / 2, textSizeY.x, textSizeY.y);
					Widgets.Label(yNumRect, labelTextY);
				}
			}
		}

		private static void PlotFunction(Rect rect, Function function, FloatRange xRange, FloatRange yRange, List<CurvePoint> plotPoints, bool mouseHandle = true)
		{
			if (!plotPoints.NullOrEmpty() && plotPoints.Count > 1)
			{
				float step = Mathf.Abs((xRange.max - xRange.min) / (AxisNotchCount * 5));
				if (step > 0)
				{
					float x = xRange.min;
					float y = function(x);
					Vector2 coordLeft = GraphCoordToScreenPos(rect, (x, y), xRange, yRange);
					for (x = xRange.min + step; x <= xRange.max; x += step) //start 1 step in
					{
						y = function(x);
						if (!float.IsNaN(y) && xRange.InRange(x) && yRange.InRange(y))
						{
							Vector2 coordRight = GraphCoordToScreenPos(rect, (x, y), xRange, yRange);
							if (xRange.InRange(x) && yRange.InRange(y))
							{
								Widgets.DrawLine(coordLeft, coordRight, Color.white, 1);
							}
							coordLeft = coordRight;
						}
					}
				}
			}

			if (!plotPoints.NullOrEmpty())
			{
				for (int i = 0; i < plotPoints.Count; i++)
				{
					CurvePoint curvePoint = plotPoints[i];
					Vector2 graphPos = GraphCoordToScreenPos(rect, (curvePoint.x, curvePoint.y), xRange, yRange);
					if (xRange.InRange(curvePoint.x))
					{
						Rect texRect = new Rect(graphPos.x - GraphNodeSize / 2, graphPos.y - GraphNodeSize / 2, GraphNodeSize, GraphNodeSize);
						if (Widgets.ButtonImage(texRect, BaseContent.WhiteTex))
						{
							
						}
					}
				}
			}

			if (mouseHandle && draggingPlotPointIndex < 0)
			{
				Vector2 mousePosition = Event.current.mousePosition;
				(float x, float y) coord = ScreenPosToGraphCoord(rect, mousePosition, xRange, yRange);
				coord.y = function(coord.x);
				Vector2 graphPos = GraphCoordToScreenPos(rect, (coord.x, coord.y), xRange, yRange);
				if (xRange.InRange(coord.x) && Vector2.Distance(mousePosition, graphPos) <= 100)
				{
					Rect texRect = new Rect(graphPos.x - GraphNodeSize / 2, graphPos.y - GraphNodeSize / 2, GraphNodeSize, GraphNodeSize);
					Widgets.DrawTextureFitted(texRect, CurvePoint, 1);
					string coordLabel = $"({coord.x:0.##}, {coord.y:0.##})";
					Vector2 labelSize = Text.CalcSize(coordLabel);
					(float x, float y) coordLabelPos = coord.x <= (xRange.min + xRange.max / 2) ? (graphPos.x + texRect.width, graphPos.y - texRect.height / 2) :
																								  (graphPos.x - texRect.width - labelSize.x, graphPos.y - texRect.height / 2);
					Rect coordLabelRect = new Rect(coordLabelPos.x, coordLabelPos.y, labelSize.x, labelSize.y);
					Widgets.DrawMenuSection(coordLabelRect);
					Widgets.Label(coordLabelRect, coordLabel);
				}
			}
		}

		private static void DrawLegend(Rect rect)
		{

		}

		private static Vector2 GraphCoordToScreenPos(Rect rect, (float x, float y) coord, FloatRange xRange, FloatRange yRange)
		{
			float xStep = coord.x / (xRange.max - xRange.min);
			float yStep = coord.y / (yRange.max - yRange.min);
			return new Vector2(rect.x + rect.width * xStep, rect.y + rect.height - rect.height * yStep);
		}

		private static (float x, float y) ScreenPosToGraphCoord(Rect rect, Vector2 coord, FloatRange xRange, FloatRange yRange)
		{
			float xStep = (coord.x - rect.x) / rect.width;
			float yStep = (coord.y - rect.y - rect.height) / -rect.height;
			float x = xStep * (xRange.max - xRange.min);
			float y = yStep * (yRange.max - yRange.min);
			return (x, y);
		}

		public enum GraphType
		{
			Linear,
			Bezier,
			Freeform
		}
	}
}
