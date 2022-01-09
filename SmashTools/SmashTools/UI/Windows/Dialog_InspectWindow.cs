using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace SmashTools
{
	public class Dialog_InspectWindow : Window
	{
		private IInspectable inspectable;
		private Vector2 size = new Vector2(950, 760);

		public Dialog_InspectWindow(IInspectable inspectable)
		{
			this.inspectable = inspectable;
		}

		public Dialog_InspectWindow(IInspectable inspectable, Vector2 size)
		{
			this.inspectable = inspectable;
			this.size = size;
			SetInitialSizeAndPosition();
		}

		public override Vector2 InitialSize => size;

		public override void DoWindowContents(Rect inRect) => inspectable.DrawInspectDialog(inRect);
	}
}
