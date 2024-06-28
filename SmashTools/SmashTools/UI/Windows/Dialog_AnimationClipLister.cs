using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Verse;
using RimWorld;
using UnityEngine;
using Verse.Noise;
using System.Diagnostics;

namespace SmashTools.Animations
{
	public class Dialog_AnimationClipLister : Window
	{
		private const float EntryHeight = 30;

		private static readonly Color highlightColor = new ColorInt(70, 96, 124, 50).ToColor;

		private readonly IAnimator animator;
		private readonly float width;

		private Vector2 windowSize;
		private Vector2 position;

		private List<FileInfo> files;

		public Dialog_AnimationClipLister(IAnimator animator, Rect rect)
		{
			this.animator = animator;
			this.width = rect.width;
			this.position = rect.position;

			this.closeOnClickedOutside = true;
			this.absorbInputAroundWindow = false;
			this.preventCameraMotion = false;
			this.doWindowBackground = false;
		}

		public override Vector2 InitialSize => windowSize;

		protected override float Margin => 0;

		public override void PreOpen()
		{
			windowSize = CalculateWindowSize();
			base.PreOpen();
		}

		public override void Notify_ClickOutsideWindow()
		{
			base.Notify_ClickOutsideWindow();
			Close();
		}

		private Vector2 CalculateWindowSize()
		{
			files = AnimationLoader.GetAnimationClipFileInfo(animator.ModContentPack);
			return new Vector2(width, EntryHeight * files.Count + EntryHeight); //Make room for additional row for 'create' button
		}

		protected override void SetInitialSizeAndPosition()
		{
			if (position.x + InitialSize.x > UI.screenWidth)
			{
				position.x = UI.screenWidth - InitialSize.x;
			}
			if (position.y + InitialSize.y > UI.screenHeight)
			{
				position.y = UI.screenHeight - InitialSize.y;
			}
			windowRect = new Rect(position.x, position.y, InitialSize.x, InitialSize.y);
		}

		public override void DoWindowContents(Rect inRect)
		{
			GUIState.Push();

			Widgets.DrawBoxSolid(inRect, Color.white);

			Text.Font = GameFont.Small;

			Rect rowRect = new Rect(inRect.x, inRect.y, inRect.width, EntryHeight);
			for (int i = 0; i < files.Count; i++)
			{
				FileInfo file = files[i];
				Rect entryRect = rowRect.ContractedBy(3);
				entryRect.SplitVertically(50, out Rect checkboxRect, out Rect fileLabelRect);

				Widgets.ButtonText(fileLabelRect, file.Name, false, false, Color.black, overrideTextAnchor: TextAnchor.MiddleLeft);
				GUI.color = Color.white;

				if (Mouse.IsOver(entryRect))
				{
					//Widgets.DrawBoxSolid(entryRect, highlightColor);
				}

				rowRect.y += rowRect.height;
			}

			Rect createFileRect = rowRect.ContractedBy(3);
			createFileRect.SplitVertically(EntryHeight + 10, out Rect _, out Rect createFileBtnRect);

			if (!files.NullOrEmpty())
			{
				UIElements.DrawLineHorizontalGrey(createFileBtnRect.x, createFileBtnRect.y, createFileBtnRect.width);
			}
			if (Widgets.ButtonText(createFileBtnRect, "ST_CreateNewClip".Translate(), false, false, Color.black, overrideTextAnchor: TextAnchor.MiddleLeft))
			{
				Log.Message("CREATING ANIMATION");
			}

			GUIState.Pop();
		}
	}
}
