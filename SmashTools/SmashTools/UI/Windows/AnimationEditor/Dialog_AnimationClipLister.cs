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

		private static readonly Color highlightColor = new ColorInt(150, 150, 150, 40).ToColor;

		private readonly IAnimator animator;
		private readonly float width;
		private readonly AnimationClip animation;
		private readonly Action<FileInfo> onFilePicked;

		private Vector2 windowSize;
		private Vector2 position;

		private List<FileInfo> files;

		public Dialog_AnimationClipLister(IAnimator animator, Rect rect, AnimationClip animation, Action<FileInfo> onFilePicked = null)
		{
			this.animator = animator;
			this.width = rect.width;
			this.position = rect.position;
			this.animation = animation;
			this.onFilePicked = onFilePicked;

			this.closeOnClickedOutside = true;
			this.absorbInputAroundWindow = false;
			this.preventCameraMotion = false;
			this.doWindowBackground = false;
			this.layer = WindowLayer.Super;
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

			Widgets.DrawMenuSection(inRect);

			Text.Font = GameFont.Small;

			Rect rowRect = new Rect(inRect.x, inRect.y, inRect.width, EntryHeight);
			for (int i = 0; i < files.Count; i++)
			{
				FileInfo file = files[i];
				Rect entryRect = rowRect.ContractedBy(3);
				entryRect.SplitVertically(EntryHeight, out Rect leftRect, out Rect fileLabelRect);

				Rect checkboxRect = new Rect(leftRect.x, leftRect.y, leftRect.height, leftRect.height).ContractedBy(3);
				if (animation != null && animation.FilePath == file.FullName)
				{
					GUI.DrawTexture(checkboxRect, Widgets.CheckboxOnTex);
				}
				if (Widgets.ButtonText(fileLabelRect, Path.GetFileNameWithoutExtension(file.Name), drawBackground: false))
				{
					onFilePicked?.Invoke(file);
					Close();
				}

				if (Mouse.IsOver(entryRect))
				{
					Widgets.DrawBoxSolid(fileLabelRect, highlightColor);
				}

				rowRect.y += rowRect.height;
			}

			Rect createFileRect = rowRect.ContractedBy(3);
			createFileRect.SplitVertically(EntryHeight, out Rect _, out Rect createFileBtnRect);

			if (!files.NullOrEmpty())
			{
				UIElements.DrawLineHorizontalGrey(createFileBtnRect.x, createFileBtnRect.y, createFileBtnRect.width);
			}
			if (Widgets.ButtonText(createFileBtnRect, "ST_CreateNewClip".Translate(), drawBackground: false))
			{
				DirectoryInfo directoryInfo = AnimationLoader.AnimationDirectory(animator.ModContentPack);
				if (directoryInfo == null || !directoryInfo.Exists)
				{
					directoryInfo = Directory.CreateDirectory(Path.Combine(animator.ModContentPack.RootDir, AnimationLoader.AnimationFolderName));
				}
				FileInfo fileInfo = AnimationLoader.CreateEmptyAnimFile(directoryInfo);
				onFilePicked?.Invoke(fileInfo);
				Close();
			}
			if (Mouse.IsOver(createFileBtnRect))
			{
				Widgets.DrawBoxSolid(createFileBtnRect, highlightColor);
			}

			GUIState.Pop();
		}
	}
}
