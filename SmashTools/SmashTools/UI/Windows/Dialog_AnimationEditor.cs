using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;
using Verse.Sound;
using System.Reflection.Emit;
using SmashTools.Xml;
using System.IO;
using System.Threading.Tasks;

namespace SmashTools.Animations
{
	[StaticConstructorOnStartup]
	public class Dialog_AnimationEditor : Window
	{
		private const float MinLeftWindowSize = 260;
		private const float MinRightWindowSize = 250;
		
		private const float MinFrameSpacing = 10;
		private const float WidgetBarHeight = 24;
		private const float KeyframeSize = 20;
		private const float FrameInputWidth = 50;
		private const float FrameBarPadding = 40;

		private const float TabWidth = 110;

		private const int DefaultAnimationFrameCount = 100;

		private static readonly Texture2D pauseTexture = ContentFinder<Texture2D>.Get("SmashTools/VideoPause");
		private static readonly Texture2D playTexture = ContentFinder<Texture2D>.Get("SmashTools/VideoPlay");

		private static readonly Texture2D skipToBeginningTexture = ContentFinder<Texture2D>.Get("SmashTools/VideoReturnToBeginning");
		private static readonly Texture2D skipToPreviousTexture = ContentFinder<Texture2D>.Get("SmashTools/VideoReturnToPrevious");
		private static readonly Texture2D skipToNextTexture = ContentFinder<Texture2D>.Get("SmashTools/VideoSkipToNext");
		private static readonly Texture2D skipToEndTexture = ContentFinder<Texture2D>.Get("SmashTools/VideoSkipToEnd");

		private static readonly Texture2D animationEventTexture = ContentFinder<Texture2D>.Get("SmashTools/KeyFrame");
		private static readonly Texture2D keyFrameTexture = ContentFinder<Texture2D>.Get("SmashTools/KeyFrame");
		private static readonly Texture2D addAnimationEventTexture = ContentFinder<Texture2D>.Get("SmashTools/AddEvent");
		private static readonly Texture2D addKeyFrameTexture = ContentFinder<Texture2D>.Get("SmashTools/AddKeyFrame");

		private static readonly Color backgroundDopesheetColor = new ColorInt(56, 56, 56).ToColor;
		private static readonly Color backgroundCurvesColor = new ColorInt(40, 40, 40).ToColor;

		private static readonly Color separatorColor = new ColorInt(35, 35, 35).ToColor;
		private static readonly Color animationEventBarColor = new ColorInt(49, 49, 49).ToColor;
		private static readonly Color animationKeyFrameBarColor = new ColorInt(47, 47, 47).ToColor;
		private static readonly Color animationKeyFrameBarFadeColor = new ColorInt(45, 45, 45).ToColor;

		private static readonly Color frameTimeBarColor = new ColorInt(40, 64, 75).ToColor;
		private static readonly Color frameTimeBarColorDisabled = new ColorInt(41, 47, 50).ToColor;

		private float leftWindowSize = MinLeftWindowSize;

		private IAnimator animator;

		private AnimationClip animation;
		private bool previewInGame = false;
		private float frameZoom = 0;

		private Tab tab;
		private int frameNumber = 0;
		private bool isPlaying;

		private Vector2 editorScrollPos;

		/* ----- Left Panel Resizing ----- */

		private bool resizing = false;

		private float startingWidth;

		/* ------------------------------- */

		public Dialog_AnimationEditor(IAnimator animator)
		{
			SetWindowProperties();
			this.animator = animator;
		}

		public bool IsPlaying
		{
			get
			{
				return isPlaying;
			}
			private set
			{
				if (isPlaying != value)
				{
					isPlaying = value;
				}
			}
		}

		public override Vector2 InitialSize
		{
			get
			{
				return new Vector2(UI.screenWidth * 0.75f, UI.screenHeight * 0.75f);
			}
		}

		private void SetWindowProperties()
		{
			this.resizeable = true;
			this.doCloseX = true;
			this.closeOnClickedOutside = false;
			this.draggable = true;
			this.absorbInputAroundWindow = false;
			this.preventCameraMotion = false;
		}

		public override void DoWindowContents(Rect inRect)
		{
			GUIState.Push();

			Text.Font = GameFont.Small;
			Rect labelRect = new Rect(inRect.x, inRect.y, inRect.width, 24);
			Widgets.Label(labelRect, "ST_AnimationWindow".Translate());

			inRect.yMin = labelRect.yMax;
			inRect.SplitVertically(leftWindowSize, out Rect leftRect, out Rect rightRect);

			GUI.enabled = animator != null;
			{
				DrawLeftSection(leftRect);

				DrawRightSection(rightRect);
			}
			GUI.enabled = true;

			DoResizerButton(leftRect);

			GUIState.Pop();
		}

		private void DrawLeftSection(Rect panelRect)
		{
			DrawBackground(panelRect);

			string previewLabel = "ST_PreviewAnimation".Translate();
			float width = Text.CalcSize(previewLabel).x;
			Rect toggleRect = new Rect(panelRect.x, panelRect.y, width + 20, WidgetBarHeight);
			if (ToggleText(toggleRect, previewLabel, "ST_PreviewAnimationTooltip".Translate(), previewInGame))
			{
				previewInGame = !previewInGame;
			}

			DoSeparatorHorizontal(panelRect.x, panelRect.y + WidgetBarHeight, panelRect.width);

			DoSeparatorHorizontal(panelRect.x, panelRect.yMax, panelRect.width);

			DoSeparatorVertical(panelRect.xMax, panelRect.y, panelRect.height);

			Rect buttonRect = new Rect(toggleRect.xMax, panelRect.y, WidgetBarHeight, WidgetBarHeight);
			if (AnimationButton(buttonRect, skipToBeginningTexture, "ST_SkipFrameBeginningTooltip".Translate()))
			{

			}
			DoSeparatorVertical(buttonRect.x, buttonRect.y, buttonRect.height);
			buttonRect.x += 1;

			buttonRect.x += buttonRect.width;
			if (AnimationButton(buttonRect, skipToPreviousTexture, "ST_SkipFramePreviousTooltip".Translate()))
			{

			}
			DoSeparatorVertical(buttonRect.x, buttonRect.y, buttonRect.height);
			buttonRect.x += 1;

			buttonRect.x += buttonRect.width;
			if (AnimationButton(buttonRect, IsPlaying ? pauseTexture : playTexture, IsPlaying ? "ST_PauseAnimationTooltip".Translate() : "ST_PlayAnimationTooltip".Translate()))
			{
				IsPlaying = !IsPlaying;
			}
			DoSeparatorVertical(buttonRect.x, buttonRect.y, buttonRect.height);
			buttonRect.x += 1;

			buttonRect.x += buttonRect.width;
			if (AnimationButton(buttonRect, skipToNextTexture, "ST_SkipFrameNextTooltip".Translate()))
			{

			}
			DoSeparatorVertical(buttonRect.x, buttonRect.y, buttonRect.height);
			buttonRect.x += 1;

			buttonRect.x += buttonRect.width;
			if (AnimationButton(buttonRect, skipToEndTexture, "ST_SkipFrameEndTooltip".Translate()))
			{

			}
			DoSeparatorVertical(buttonRect.x, buttonRect.y, buttonRect.height);
			DoSeparatorVertical(buttonRect.xMax, buttonRect.y, buttonRect.height);

			Rect frameNumberRect = new Rect(panelRect.xMax - FrameInputWidth, panelRect.y, FrameInputWidth, buttonRect.height).ContractedBy(2);
			string buffer = null;
			Widgets.TextFieldNumeric(frameNumberRect, ref frameNumber, ref buffer);

			Rect animClipDropdownRect = new Rect(panelRect.x, buttonRect.yMax, 200, buttonRect.height);
			Rect animClipSelectRect = new Rect(windowRect.x + Margin + animClipDropdownRect.x, windowRect.y + Margin + animClipDropdownRect.yMax, animClipDropdownRect.width, 500);
			if (Dropdown(animClipDropdownRect, "Rotate", "Assets/SomePath/Rotation.rwa"))
			{
				Find.WindowStack.Add(new Dialog_AnimationClipLister(animator, animClipSelectRect));
			}
			DoSeparatorVertical(animClipDropdownRect.xMax, animClipDropdownRect.y, animClipDropdownRect.height);

			Rect animButtonRect = new Rect(panelRect.xMax - buttonRect.height, animClipDropdownRect.y, buttonRect.height, buttonRect.height);
			if (AnimationButton(animButtonRect, addAnimationEventTexture, "ST_AddAnimationEvent".Translate()))
			{

			}
			DoSeparatorVertical(animButtonRect.x, animButtonRect.y, animButtonRect.height);
			animButtonRect.x -= 1;

			animButtonRect.x -= animButtonRect.height;
			if (AnimationButton(animButtonRect, addKeyFrameTexture, "ST_AddKeyFrame".Translate()))
			{

			}
			DoSeparatorVertical(animButtonRect.x, animButtonRect.y, animButtonRect.height);
			animButtonRect.x -= 1;

			DoSeparatorHorizontal(animClipDropdownRect.x, animClipDropdownRect.yMax, panelRect.width);

			Rect tabRect = new Rect(panelRect.xMax - TabWidth - 24, panelRect.yMax - WidgetBarHeight, TabWidth, WidgetBarHeight);
			DoSeparatorHorizontal(tabRect.xMax, tabRect.y, 24);
			if (ToggleText(tabRect, "ST_CurvesTab".Translate(), null, tab == Tab.Curves))
			{
				FlipTab();
			}
			tabRect.x -= tabRect.width;
			if (ToggleText(tabRect, "ST_DopesheetTab".Translate(), null, tab == Tab.Dopesheet))
			{
				FlipTab();
			}
			DoSeparatorHorizontal(panelRect.x, tabRect.y, panelRect.x + tabRect.x);

			void FlipTab()
			{
				tab = tab switch
				{
					Tab.Dopesheet => Tab.Curves,
					Tab.Curves => Tab.Dopesheet,
					_ => throw new NotImplementedException(),
				};
			}
		}

		private void DrawRightSection(Rect rect)
		{
			Rect editorOutRect = new Rect(rect.x, rect.y, rect.width, rect.height);
			Rect editorViewRect = new Rect(rect.x, rect.y, editorOutRect.width * 1.5f, editorOutRect.height - 16);

			Widgets.BeginScrollView(editorOutRect, ref editorScrollPos, editorViewRect);
			{
				Rect  leftFrameBarPadding = new Rect(editorViewRect.x, editorViewRect.y, FrameBarPadding, WidgetBarHeight);
				Widgets.DrawBoxSolid(leftFrameBarPadding, frameTimeBarColorDisabled);
				
				Rect frameBarRect = new Rect(leftFrameBarPadding.xMax, editorViewRect.y, editorViewRect.width - FrameBarPadding * 2, WidgetBarHeight);
				Widgets.DrawBoxSolid(frameBarRect, frameTimeBarColor);
				DoSeparatorVertical(frameBarRect.x, frameBarRect.y, frameBarRect.height);

				Rect rightFrameBarPadding = new Rect(frameBarRect.xMax, editorViewRect.y, FrameBarPadding, WidgetBarHeight);
				Widgets.DrawBoxSolid(rightFrameBarPadding, frameTimeBarColorDisabled);

				DoSeparatorHorizontal(editorViewRect.x, frameBarRect.yMax, editorViewRect.width);
				frameBarRect.yMax += 1;

				Rect animationEventBarRect = new Rect(editorViewRect.x, frameBarRect.yMax, editorViewRect.width, WidgetBarHeight);
				Widgets.DrawBoxSolid(animationEventBarRect, animationEventBarColor);
				
				switch (tab)
				{
					case Tab.Dopesheet:
						{
							Rect keyFrameTopBarFadeRect = new Rect(editorViewRect.x, animationEventBarRect.yMax, editorViewRect.width, 2);
							Widgets.DrawBoxSolid(keyFrameTopBarFadeRect, animationKeyFrameBarFadeColor);
							Rect keyFrameTopBarRect = new Rect(editorViewRect.x, keyFrameTopBarFadeRect.yMax, editorViewRect.width, KeyframeSize - keyFrameTopBarFadeRect.height);
							Widgets.DrawBoxSolid(keyFrameTopBarRect, animationKeyFrameBarColor);
							
							Rect dopeSheetRect = new Rect(editorViewRect.x, keyFrameTopBarRect.yMax, editorViewRect.width, editorViewRect.height - keyFrameTopBarRect.yMax);
							DrawBackground(dopeSheetRect);
						}
						break;
					case Tab.Curves:
						{
							Rect curvesRect = new Rect(editorViewRect.x, animationEventBarRect.yMax, editorViewRect.width, editorViewRect.height - animationEventBarRect.yMax);
							DrawBackgroundDark(curvesRect);
						}
						break;
				}
			}
			Widgets.EndScrollView();
		}

		private void DoResizerButton(Rect rect)
		{
			float currentWidth = rect.width;

			Rect resizeButtonRect = new Rect(rect.xMax - 24, rect.yMax - 24, 24, 24);
			Vector2 mousePosition = Event.current.mousePosition;
			if (Event.current.type == EventType.MouseDown && Mouse.IsOver(resizeButtonRect))
			{
				resizing = true;
				startingWidth = mousePosition.x;
			}
			if (resizing)
			{
				rect.width = startingWidth + (mousePosition.x - startingWidth);
				rect.width = Mathf.Clamp(rect.width, MinLeftWindowSize, windowRect.width - MinRightWindowSize);

				if (Event.current.type == EventType.MouseUp)
				{
					resizing = false;
				}
			}
			Widgets.ButtonImage(resizeButtonRect, TexUI.WinExpandWidget);

			if (rect.width != currentWidth)
			{
				leftWindowSize = rect.width;
			}
		}

		private void DrawBackground(Rect rect)
		{
			Widgets.DrawBoxSolidWithOutline(rect, backgroundDopesheetColor, separatorColor);
		}

		private void DrawBackgroundDark(Rect rect)
		{
			Widgets.DrawBoxSolidWithOutline(rect, backgroundCurvesColor, separatorColor);
		}

		private void DoSeparatorHorizontal(float x, float y, float length)
		{
			UIElements.DrawLineHorizontal(x, y, length, separatorColor);
		}

		private void DoSeparatorVertical(float x, float y, float height)
		{
			UIElements.DrawLineVertical(x, y, height, separatorColor);
		}

		private bool AnimationButton(Rect rect, Texture2D texture, string tooltip)
		{
			GUI.color = Mouse.IsOver(rect) ? GenUI.MouseoverColor : Color.white;
			Rect imageRect = rect.ContractedBy(3);
			GUI.DrawTexture(imageRect, texture);
			GUI.color = Color.white;

			if (!tooltip.NullOrEmpty())
			{
				TooltipHandler.TipRegion(rect, tooltip);
			}
			bool result = Widgets.ButtonInvisible(rect);
			if (result)
			{
				SoundDefOf.Click.PlayOneShotOnCamera();
			}
			return result;
		}

		/// <returns>xMax</returns>
		private bool ToggleText(Rect rect, string label, string tooltip, bool enabled)
		{
			bool pressed = false;
			var anchor = Text.Anchor;
			Text.Anchor = TextAnchor.MiddleCenter;
			
			DoSeparatorHorizontal(rect.x, rect.y, rect.width);
			DoSeparatorHorizontal(rect.x, rect.yMax, rect.width);
			DoSeparatorVertical(rect.x, rect.y, rect.height);
			DoSeparatorVertical(rect.xMax, rect.y, rect.height);

			if (Mouse.IsOver(rect))
			{
				GUI.color = new Color(0.75f, 0.75f, 0.75f);
			}
			Widgets.Label(rect, label);
			if (enabled)
			{
				Widgets.DrawBoxSolid(rect.ContractedBy(1), new Color(0.75f, 0.75f, 0.75f, 0.25f));
			}
			if (!tooltip.NullOrEmpty())
			{
				TooltipHandler.TipRegion(rect, tooltip);
			}
			if (Widgets.ButtonInvisible(rect))
			{
				pressed = true;
				SoundDefOf.Click.PlayOneShotOnCamera();
			}
			GUI.color = Color.white;
			Text.Anchor = anchor;
			return pressed;
		}

		private bool Dropdown(Rect rect, string label, string tooltip)
		{
			bool pressed = false;
			var anchor = Text.Anchor;
			Text.Anchor = TextAnchor.MiddleCenter;

			if (Mouse.IsOver(rect))
			{
				GUI.color = new Color(0.75f, 0.75f, 0.75f);
			}
			float dropdownSize = rect.height;
			rect.SplitVertically(rect.width - dropdownSize, out Rect labelRect, out Rect dropdownRect);
			Widgets.Label(labelRect, label);

			GUIState.Push();
			{
				UI.RotateAroundPivot(90, dropdownRect.center);
				GUI.DrawTexture(dropdownRect, TexButton.Reveal);
			}
			GUIState.Pop();
			if (!tooltip.NullOrEmpty())
			{
				TooltipHandler.TipRegion(rect, tooltip);
			}
			if (Widgets.ButtonInvisible(rect))
			{
				pressed = true;
				SoundDefOf.Click.PlayOneShotOnCamera();
			}
			GUI.color = Color.white;
			Text.Anchor = anchor;
			return pressed;
		}

		private bool ExportAnimationXml()
		{
			if (animation == null)
			{
				return false;
			}
			bool exported = true;
			try
			{
				XmlExporter.StartDocument(animation.FilePath);
				{
					XmlExporter.OpenNode("Animation");
					{
						animation.Export();
					}
					XmlExporter.CloseNode();
				}
				XmlExporter.Export();
			}
			catch (Exception ex)
			{
				exported = false;
				Log.Error($"Unable to export animation data. Exception = {ex}");
			}
			finally
			{
				XmlExporter.Close();
			}

			if (exported)
			{
				SoundDefOf.Click.PlayOneShotOnCamera();
			}
			else
			{
				SoundDefOf.ClickReject.PlayOneShotOnCamera();
			}
			return exported;
		}

		private static void AnimationClipSelect()
		{
			
		}

		private enum Tab
		{
			Dopesheet,
			Curves
		}
	}
}
