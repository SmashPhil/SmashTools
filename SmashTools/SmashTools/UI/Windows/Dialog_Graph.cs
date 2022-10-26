using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace SmashTools
{
	public class Dialog_Graph : Window
	{
		protected List<CurvePoint> plotPoints;

		public Dialog_Graph(Graph.Function function, FloatRange range, List<CurvePoint> plotPoints = null)
		{
			Function = function;
			XRange = range;
			YRange = range;
			this.plotPoints = plotPoints;
		}

		public Dialog_Graph(Graph.Function function, FloatRange xRange, FloatRange yRange, List<CurvePoint> plotPoints = null)
		{
			Function = function;
			XRange = xRange;
			YRange = yRange;
			this.plotPoints = plotPoints;
		}

		public FloatRange XRange { get; protected set; }

		public FloatRange YRange { get; protected set; }
		
		public Graph.Function Function { get; protected set; }

		public virtual List<CurvePoint> CurvePoints => plotPoints;

		public override Vector2 InitialSize
		{
			get
			{
				float minSize = Mathf.Min(UI.screenWidth, UI.screenHeight);
				return new Vector2(minSize, minSize);
			}
		}

		public override void DoWindowContents(Rect inRect)
		{
			Widgets.DrawMenuSection(inRect);
			Rect rect = inRect.ContractedBy(45);
			Graph.DrawGraph(rect, Function, XRange, YRange, CurvePoints);
		}
	}
}
