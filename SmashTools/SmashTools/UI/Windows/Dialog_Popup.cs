using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace SmashTools
{
	public class Dialog_Popup : Window
	{
		private Vector2 size = new Vector2(500, 500);
		private string text;

		public Dialog_Popup(string text)
		{
			this.text = text;
			SetProperties();
		}

		public Dialog_Popup(string title, string text) : this(text)
		{
			this.optionalTitle = title;
		}

		public Dialog_Popup(Vector2 size, string title, string text) : this(title, text)
		{
			this.size = size;
		}

		public override Vector2 InitialSize => size;

		private void SetProperties()
		{
			this.absorbInputAroundWindow = true;
			this.forcePause = true;
			this.forceCatchAcceptAndCancelEventEvenIfUnfocused = true;
			this.onlyDrawInDevMode = true;
			this.onlyOneOfTypeAllowed = true;
			this.preventCameraMotion = true;
			this.doCloseButton = false;
			this.doCloseX = true;
			this.closeOnAccept = false;
			this.closeOnCancel = false;
			this.closeOnClickedOutside = false;
			this.draggable = true;
			this.resizeable = true;
		}

		public override void DoWindowContents(Rect inRect)
		{
			using var textBlock = new TextBlock(GameFont.Small);

			Widgets.Label(inRect, text);
		}
	}
}
