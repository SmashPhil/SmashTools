﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace SmashTools
{
	public static class Graph
	{
		public const int AxisMajorNotchCount = 5;
		public const int AxisNotchCount = 50;
		public const float NotchSize = 5;
		public const float GraphNodeSize = 8;

		public const float DistToPlotCurvePoint = 50;

		private static int draggingPlotPointIndex = -1;

		//XY Function
		public delegate Vector2 Function(float x);

		public static void DrawGraph(Rect rect, Function function, FloatRange xRange, FloatRange yRange, List<CurvePoint> plotPoints = null, bool simplified = false, bool editable = true, bool drawCoordLabels = true)
		{
			Rect axisRect = rect.ContractedBy(5);
			DrawAxis(axisRect, xRange, yRange, drawAxisT: !simplified);
			if (function != null && !plotPoints.NullOrEmpty())
			{
				PlotFunction(axisRect, function, xRange, yRange, plotPoints, simplified: simplified, editable: editable, drawCoordLabels: drawCoordLabels);
				//DrawLegend(graphRect);
			}
		}

		private static void DrawAxis(Rect rect, FloatRange xRange, FloatRange yRange, bool drawAxisT = false)
		{
			float xAxisPos = 0;
			float yAxisPos = 0;

			if (xRange.min < 0)
			{
				xAxisPos = Mathf.Abs(xRange.min / xRange.max) * rect.width;
				yAxisPos = Mathf.Abs(yRange.min / yRange.max) * rect.height;
			}

			Vector2 tAxis = new Vector2(rect.x, rect.y);
			if (drawAxisT)
			{
				Widgets.DrawLineHorizontal(tAxis.x, tAxis.y, rect.width);
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

				Vector2 tAxisNotchCoord = new Vector2(tAxis.x + stepSizeX * i, tAxis.y - notchSize / 2);
				if (drawAxisT)
				{
					Widgets.DrawLineVertical(tAxisNotchCoord.x, tAxisNotchCoord.y, notchSize);
				}

				Vector2 xAxisNotchCoord = new Vector2(xAxis.x + stepSizeX * i, xAxis.y - notchSize / 2);
				Widgets.DrawLineVertical(xAxisNotchCoord.x, xAxisNotchCoord.y, notchSize);

				Vector2 yAxisNotchCoord = new Vector2(yAxis.x - notchSize / 2, rect.y + rect.height - stepSizeY * i);
				Widgets.DrawLineHorizontal(yAxisNotchCoord.x, yAxisNotchCoord.y, notchSize);

				if (majorNotch)
				{
					if (drawAxisT)
					{
						float axisTNum = 0 + 1 * ((float)i / AxisNotchCount);
						string labelTextT = axisTNum.RoundTo(0.1f).ToString();
						Vector2 textSizeT = Text.CalcSize(labelTextT);
						Rect tNumRect = new Rect(tAxisNotchCoord.x - textSizeT.x / 2, tAxis.y - NotchSize * 5, textSizeT.x, textSizeT.y);
						Widgets.Label(tNumRect, labelTextT);
					}

					float axisXNum = xRange.min + (xRange.max - xRange.min) * ((float)i / AxisNotchCount);
					string labelTextX = axisXNum.RoundTo(0.1f).ToString();
					Vector2 textSizeX = Text.CalcSize(labelTextX);
					Rect xNumRect = new Rect(xAxisNotchCoord.x - textSizeX.x / 2, xAxis.y + NotchSize * 2, textSizeX.x, textSizeX.y);
					Widgets.Label(xNumRect, labelTextX);

					float axisYNum = yRange.min + (yRange.max - yRange.min) * ((float)i / AxisNotchCount);
					string labelTextY = axisYNum.RoundTo(0.1f).ToString();
					Vector2 textSizeY = Text.CalcSize(labelTextY);
					Rect yNumRect = new Rect(rect.x + xAxisPos - NotchSize * 2 - textSizeY.x, yAxisNotchCoord.y - textSizeY.y / 2, textSizeY.x, textSizeY.y);
					Widgets.Label(yNumRect, labelTextY);
				}
			}
		}

		private static void PlotFunction(Rect rect, Function function, FloatRange xRange, FloatRange yRange, List<CurvePoint> plotPoints, bool simplified = false, bool editable = true, bool drawCoordLabels = true)
		{
			if (!plotPoints.NullOrEmpty() && plotPoints.Count > 1)
			{
				float step = Mathf.Abs((xRange.max - xRange.min) / (AxisNotchCount * 5));
				if (step > 0)
				{
					float i = xRange.min;
					Vector2 point = function(i);
					if (simplified)
					{
						point.x = i;
					}
					Vector2 coordLeft = GraphCoordToScreenPos(rect, point, xRange, yRange);
					for (i = xRange.min + step; i <= xRange.max; i += step) //start 1 step in
					{
						point = function(i);
						if (simplified)
						{
							point.x = i;
						}
						if (!float.IsNaN(point.y) && !float.IsNaN(point.x) && xRange.InRange(point.x) && yRange.InRange(point.y))
						{
							Vector2 coordRight = GraphCoordToScreenPos(rect, point, xRange, yRange);
							if (xRange.InRange(point.x) && yRange.InRange(point.y))
							{
								Widgets.DrawLine(coordLeft, coordRight, Color.white, 1);
							}
							coordLeft = coordRight;
						}
					}
				}
			}

			bool mouseOverAnyPlotPoint = false;
			if (!plotPoints.NullOrEmpty())
			{
				for (int i = 0; i < plotPoints.Count; i++)
				{
					CurvePoint curvePoint = plotPoints[i];
					Vector2 graphPosEdit = GraphCoordToScreenPos(rect, curvePoint, xRange, yRange);
					if (xRange.InRange(curvePoint.x))
					{
						Rect texRect = new Rect(graphPosEdit.x - GraphNodeSize / 2, graphPosEdit.y - GraphNodeSize / 2, GraphNodeSize, GraphNodeSize);
						bool mouseOverThisPlotPoint = Mouse.IsOver(texRect);
						if (!mouseOverAnyPlotPoint)
						{
							mouseOverAnyPlotPoint = mouseOverThisPlotPoint;
						}
						GUI.DrawTexture(texRect, BaseContent.WhiteTex);

						if (drawCoordLabels)
						{
							string coordLabel = $" P{i} ({curvePoint.x:0.##}, {curvePoint.y:0.##}) ";
							Vector2 labelSize = Text.CalcSize(coordLabel);
							(float x, float y) coordLabelPos = curvePoint.x <= (xRange.min + xRange.max / 2) ? (graphPosEdit.x + texRect.width, graphPosEdit.y - texRect.height / 2) :
																										  (graphPosEdit.x - texRect.width - labelSize.x, graphPosEdit.y - texRect.height / 2);

							Rect coordLabelRect = new Rect(coordLabelPos.x, coordLabelPos.y, labelSize.x, labelSize.y);
							Widgets.DrawMenuSection(coordLabelRect);
							Widgets.Label(coordLabelRect, coordLabel);
						}

						if (editable)
						{
							if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && mouseOverThisPlotPoint)
							{
								draggingPlotPointIndex = i;
								Event.current.Use();
							}
							if (Event.current.type == EventType.MouseDrag && Event.current.button == 0 && draggingPlotPointIndex == i)
							{
								Vector2 mousePositionDrag = Event.current.mousePosition;
								(float x, float y) coordDrag = ScreenPosToGraphCoord(rect, mousePositionDrag, xRange, yRange);
								FloatRange clampX = new FloatRange(Mathf.Min(xRange.min, xRange.max), Mathf.Max(xRange.min, xRange.max));
								FloatRange clampY = new FloatRange(Mathf.Min(yRange.min, yRange.max), Mathf.Max(yRange.min, yRange.max));
								coordDrag.x = coordDrag.x.Clamp(clampX.min, clampX.max);
								coordDrag.y = coordDrag.y.Clamp(clampY.min, clampY.max);
								plotPoints[i] = new CurvePoint(coordDrag.x, coordDrag.y);

								//ForceSequentialPoints(plotPoints, i);

								Event.current.Use();
							}
							if (Event.current.type == EventType.MouseUp && Event.current.button == 0 && draggingPlotPointIndex >= 0)
							{
								draggingPlotPointIndex = -1;
								Event.current.Use();
							}
						}
					}
				}
			}

			Vector2 mousePosition = Event.current.mousePosition;
			(float x, float y) screenToCoord = ScreenPosToGraphCoord(rect, mousePosition, xRange, yRange);
			Vector2 coord = function(screenToCoord.x);
			if (simplified)
			{
				coord.x = screenToCoord.x;
			}
			Vector2 graphPos = GraphCoordToScreenPos(rect, coord, xRange, yRange);
			if (xRange.InRange(coord.x) && Vector2.Distance(mousePosition, graphPos) <= DistToPlotCurvePoint && !mouseOverAnyPlotPoint && draggingPlotPointIndex < 0)
			{
				Rect texRect = new Rect(graphPos.x - GraphNodeSize / 2, graphPos.y - GraphNodeSize / 2, GraphNodeSize, GraphNodeSize);
				Widgets.DrawTextureFitted(texRect, UIData.CurvePoint, 1);

				string coordLabel = $" ({coord.x:0.##}, {coord.y:0.##}) ";
				Vector2 labelSize = Text.CalcSize(coordLabel);
				(float x, float y) coordLabelPos = coord.x <= (xRange.min + xRange.max / 2) ? (graphPos.x + texRect.width, graphPos.y - texRect.height / 2) :
																							  (graphPos.x - texRect.width - labelSize.x, graphPos.y - texRect.height / 2);
				Rect coordLabelRect = new Rect(coordLabelPos.x, coordLabelPos.y, labelSize.x, labelSize.y);
				Widgets.DrawMenuSection(coordLabelRect);
				Widgets.Label(coordLabelRect, coordLabel);
			}
		}

		private static Vector2 GraphCoordToScreenPos(Rect rect, Vector2 coord, FloatRange xRange, FloatRange yRange)
		{
			float xStep = (coord.x - xRange.min) / (xRange.max - xRange.min);
			float yStep = (coord.y - yRange.min) / (yRange.max - yRange.min);
			return new Vector2(rect.x + rect.width * xStep, rect.y + rect.height - rect.height * yStep);
		}

		private static (float x, float y) ScreenPosToGraphCoord(Rect rect, Vector2 mousePos, FloatRange xRange, FloatRange yRange)
		{
			//Vector2 rectPos = new Vector2(mousePos.x - rect.x, mousePos.y - rect.y);

			float xStep = (mousePos.x - rect.x) / rect.width;
			float yStep = (rect.y + rect.height - mousePos.y) / rect.height;
			float x = xStep * (xRange.max - xRange.min) + xRange.min;
			float y = yStep * (yRange.max - yRange.min) + yRange.min;
			return (x.RoundTo(0.01f), y.RoundTo(0.01f));
		}

		public enum GraphType
		{
			Linear,
			Bezier,
			Staircase,
			Freeform
		}
	}
}
