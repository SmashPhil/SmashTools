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
using Verse.Noise;
using System.ComponentModel;

namespace SmashTools.Animations
{
	[StaticConstructorOnStartup]
	public class Dialog_AnimationEditor : Window
	{
		private const float MinLeftWindowSize = 260;
		private const float MinRightWindowSize = 250;
		
		private const float WidgetBarHeight = 24;
		private const float KeyframeSize = 20;
		private const float FrameInputWidth = 50;
		private const float TabWidth = 110;
		private const float PropertyEntryHeight = 24;
		private const float PropertyBtnWidth = 180;

		private const float FrameBarPadding = 40;
		private const float CollapseFrameDistance = 100;
		private const float MaxFrameSpacing = 250; //Max amount of space between two frames in the editor window
		private const float MaxFrameZoom = 1000;
		private const float ZoomRate = 0.01f;

		private const int DefaultFrameCount = 60;
		private const float SecondsPerFrame = 1 / 60f;

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
		private static readonly Color propertyButtonColor = new ColorInt(88, 88, 88).ToColor;
		private static readonly Color propertyButtonPressedColor = new ColorInt(70, 96, 124).ToColor;

		private static readonly Color animationEventBarColor = new ColorInt(49, 49, 49).ToColor;
		private static readonly Color animationKeyFrameBarColor = new ColorInt(47, 47, 47).ToColor;
		private static readonly Color animationKeyFrameBarFadeColor = new ColorInt(40, 40, 40).ToColor;

		private static readonly Color frameTimeBarColor = new ColorInt(40, 64, 75).ToColor;
		private static readonly Color frameTimeBarColorDisabled = new ColorInt(10, 10, 10, 100).ToColor;
		private static readonly Color frameTickColor = new ColorInt(140, 140, 140).ToColor;

		private static readonly Color frameLineMajorDopesheetColor = new ColorInt(75, 75, 75).ToColor;
		private static readonly Color frameLineMinorDopesheetColor = new ColorInt(66, 66, 66).ToColor;
		private static readonly Color frameLineCurvesColor = new ColorInt(90, 90, 90).ToColor;

		private float leftWindowSize = MinLeftWindowSize;

		private IAnimator animator;

		private AnimationClip animation;
		private bool previewInGame = false;
		private float frameZoom = 1;

		private Tab tab;
		private int frame = 0;
		private bool isPlaying;
		private int tickInterval = 1;

		private Vector2 editorScrollPos;
		private float realTimeToTick;
		private bool dragging = false;

		/* ----- Left Panel Resizing ----- */

		private bool resizing = false;

		private float startingWidth;

		/* ------------------------------- */

		public Dialog_AnimationEditor(IAnimator animator)
		{
			SetWindowProperties();
			this.animator = animator;
		}

		private float ExtraPadding { get; set; }

		public float FrameBarWidth => EditorWidth - FrameBarPadding * 2;

		private int FrameCountShown { get; set; }

		public int FrameCount
		{
			get
			{
				if (animation == null || animation.frameCount <= 0)
				{
					return DefaultFrameCount;
				}
				return animation.frameCount;
			}
		}

		public float FrameTickMarkSpacing
		{
			get
			{
				float spacing = CollapseFrameDistance / Mathf.Lerp((float)TickInterval / 2, (float)TickInterval, frameZoom % 1);
				if (TickInterval == 1)
				{
					spacing /= 2.5f; //Return to being factor of 2, with max frame distance of 250 at 1 tick interval
				}
				return spacing;
			}
		}
		 
		public int TickInterval
		{
			get
			{
				return tickInterval;
			}
		}

		private float Zoom
		{
			get
			{
				return frameZoom;
			}
			set
			{
				if (frameZoom != value)
				{
					frameZoom = Mathf.Clamp(value, 1, MaxFrameZoom);
					RecalculateTickInterval();
				}
			}
		}

		public float EditorWidth
		{
			get
			{
				return FrameTickMarkSpacing * FrameCount + FrameBarPadding * 2;
			}
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

		private int FrameAtMousePos(Rect rect)
		{
			return Mathf.RoundToInt(Mathf.Clamp((Event.current.mousePosition.x - rect.x) / rect.width * FrameCountShown, 0, FrameCountShown));
		}

		private void RecalculateTickInterval()
		{
			int power = Mathf.FloorToInt(Zoom) - 2;
			if (power < 0)
			{
				tickInterval = 1; //Needs to start at TickInterval = 1, then increment in powers of 2 by base 5
				return;
			}
			int interval = 5 * Ext_Math.PowTwo(power);
			tickInterval = Mathf.Clamp(interval, 1, int.MaxValue);
		}

		private void LoadAnimation(FileInfo fileInfo)
		{
			AnimationClip clip = AnimationLoader.LoadAnimation(fileInfo.FullName);
			if (clip != null)
			{
				animation = clip;
			}
		}

		public string TimeStamp(int frame)
		{
			int seconds = frame / 60;
			int frames = frame % 60;
			return $"{seconds}:{frames:00}";
		}

		public override void WindowUpdate()
		{
			base.WindowUpdate();
			if (IsPlaying)
			{
				if (Mathf.Abs(Time.deltaTime - SecondsPerFrame) < SecondsPerFrame * 0.1f)
				{
					realTimeToTick += SecondsPerFrame;
				}
				else
				{
					realTimeToTick += Time.deltaTime;
				}
				if (realTimeToTick >= SecondsPerFrame)
				{
					frame++;
					realTimeToTick -= SecondsPerFrame;
					if (frame >= FrameCount)
					{
						frame = 0;
					}
				}
			}
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

			if (animator == null)
			{
				GUI.enabled = false;
			}
			Rect buttonRect = new Rect(toggleRect.xMax, panelRect.y, WidgetBarHeight, WidgetBarHeight);
			if (AnimationButton(buttonRect, skipToBeginningTexture, "ST_SkipFrameBeginningTooltip".Translate()))
			{
				frame = 0;
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
				frame = FrameCount;
			}
			DoSeparatorVertical(buttonRect.x, buttonRect.y, buttonRect.height);
			DoSeparatorVertical(buttonRect.xMax, buttonRect.y, buttonRect.height);

			Rect frameNumberRect = new Rect(panelRect.xMax - FrameInputWidth, panelRect.y, FrameInputWidth, buttonRect.height).ContractedBy(2);
			string buffer = null;
			Widgets.TextFieldNumeric(frameNumberRect, ref frame, ref buffer);

			Rect animClipDropdownRect = new Rect(panelRect.x, buttonRect.yMax, 200, buttonRect.height);
			Rect animClipSelectRect = new Rect(windowRect.x + Margin + animClipDropdownRect.x, windowRect.y + Margin + animClipDropdownRect.yMax, animClipDropdownRect.width, 500);
			string animLabel = animation?.FileName ?? "[No Clip]";
			string animPath = animation?.FilePath ?? string.Empty;
			if (Dropdown(animClipDropdownRect, animLabel, animPath))
			{
				Find.WindowStack.Add(new Dialog_AnimationClipLister(animator, animClipSelectRect, onFilePicked: LoadAnimation));
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

			Rect rowRect = new Rect(panelRect.x, animClipDropdownRect.yMax + PropertyEntryHeight, panelRect.width, PropertyEntryHeight);
			if (animation != null && !animation.properties.NullOrEmpty())
			{
				foreach (AnimationPropertyParent propertyParent in animation.properties)
				{
					rowRect.y += rowRect.height;
					Rect propertyParentRect = new Rect(rowRect.x, rowRect.y, rowRect.width, rowRect.height);
					Widgets.Label(propertyParentRect, propertyParent.Name);
				}
				rowRect.y += PropertyEntryHeight; //Extra padding for add property btn
			}

			var enabled = GUI.enabled;
			if (animation == null)
			{
				GUI.enabled = false;
			}
			Rect propertyButtonRect = new Rect(panelRect.xMax / 2 - PropertyBtnWidth / 2, rowRect.yMax, PropertyBtnWidth, WidgetBarHeight);
			if (ButtonText(propertyButtonRect, "ST_AddProperty".Translate()))
			{
				Vector2 propertyDropdownPosition = new Vector2(windowRect.x + Margin + propertyButtonRect.position.x + 2, windowRect.y + Margin + propertyButtonRect.position.y);
				Find.WindowStack.Add(new Dialog_PropertySelect(animator, animation, propertyDropdownPosition));
			}
			GUI.enabled = enabled;

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

			GUI.enabled = true;

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
			Widgets.BeginGroup(rect);
			{
				Rect editorRect = rect.AtZero();
				Rect editorOutRect = new Rect(editorRect.x, editorRect.y, editorRect.width, editorRect.height);
				float viewWidth = Mathf.Clamp(EditorWidth, editorOutRect.width, EditorWidth);
				Rect editorViewRect = new Rect(editorRect.x, editorRect.y, viewWidth, editorOutRect.height - 16);

				ExtraPadding = 0;
				if (EditorWidth < editorViewRect.width)
				{
					ExtraPadding = editorViewRect.width - EditorWidth; //Pad all the way to the edge of the screen if necessary
				}

				Widgets.BeginScrollView(editorOutRect, ref editorScrollPos, editorViewRect);
				{
					editorViewRect.height += 16; //Should still render editor background + frame ticks underneath scrollbar

					FrameCountShown = Mathf.CeilToInt((FrameBarWidth + ExtraPadding + FrameBarPadding) / FrameTickMarkSpacing);

					Rect frameBarRect = DrawFrameBar(editorViewRect);

					Rect animationEventBarRect = new Rect(editorViewRect.x, frameBarRect.yMax, viewWidth, WidgetBarHeight);
					Widgets.DrawBoxSolid(animationEventBarRect, animationEventBarColor);

					switch (tab)
					{
						case Tab.Dopesheet:
							{
								float fadeSize = 10;
								int fadeLines = 10;
								float fadeHeight = fadeSize / fadeLines;
								Rect keyFrameTopBarFadeRect = new Rect(editorViewRect.x, animationEventBarRect.yMax, editorViewRect.width, fadeHeight);

								Color fadeColor;
								for (int i = 0; i < fadeLines; i++)
								{
									float t = (float)i / fadeLines;
									float r = Mathf.Lerp(animationKeyFrameBarFadeColor.r, animationKeyFrameBarColor.r, t);
									float g = Mathf.Lerp(animationKeyFrameBarFadeColor.g, animationKeyFrameBarColor.g, t);
									float b = Mathf.Lerp(animationKeyFrameBarFadeColor.b, animationKeyFrameBarColor.b, t);
									fadeColor = new Color(r, g, b);

									Widgets.DrawBoxSolid(keyFrameTopBarFadeRect, fadeColor);
									keyFrameTopBarFadeRect.y += fadeHeight;
								}

								Rect keyFrameTopBarRect = new Rect(editorViewRect.x, keyFrameTopBarFadeRect.y, editorViewRect.width, KeyframeSize - keyFrameTopBarFadeRect.height);
								Widgets.DrawBoxSolid(keyFrameTopBarRect, animationKeyFrameBarColor);

								Rect dopeSheetRect = new Rect(editorViewRect.x, keyFrameTopBarRect.yMax, editorViewRect.width, editorViewRect.height - keyFrameTopBarRect.yMax);
								DrawBackground(dopeSheetRect);

								DoDopesheetFrameTicks(dopeSheetRect);
							}
							break;
						case Tab.Curves:
							{
								Rect curvesRect = new Rect(editorViewRect.x, animationEventBarRect.yMax, editorViewRect.width, editorViewRect.height - animationEventBarRect.yMax);
								DrawBackgroundDark(curvesRect);
							}
							break;
					}

					float frameLinePos = frameBarRect.x + frame * FrameTickMarkSpacing;
					UIElements.DrawLineVertical(frameLinePos, frameBarRect.y, editorViewRect.height, Color.white);
				}
				//Must use GUI implementation and not Widgets, in order to disable scrollwheel handling
				Widgets.mouseOverScrollViewStack.Pop();
				GUI.EndScrollView(false); 
			}
			Widgets.EndGroup();

			if (Mouse.IsOver(rect) && Event.current.type == EventType.ScrollWheel)
			{
				float value = Event.current.delta.y * ZoomRate;
				Zoom += value;
				Event.current.Use();
			}
		}

		private Rect DrawFrameBar(Rect editorViewRect)
		{
			//Left padding
			Rect leftFrameBarPadding = new Rect(editorViewRect.x, editorViewRect.y, FrameBarPadding, WidgetBarHeight);
			Widgets.DrawBoxSolid(leftFrameBarPadding, frameTimeBarColor);
			Widgets.DrawBoxSolid(leftFrameBarPadding, frameTimeBarColorDisabled);
			UIElements.DrawLineVertical(leftFrameBarPadding.xMax - 1, leftFrameBarPadding.y, leftFrameBarPadding.height, frameTickColor);

			float rightPaddingWidth = FrameBarPadding + ExtraPadding;

			//FrameBar
			Rect frameBarRect = new Rect(leftFrameBarPadding.xMax, editorViewRect.y, FrameBarWidth + rightPaddingWidth, WidgetBarHeight);
			DoFrameSlider(frameBarRect);

			//Right padding
			Rect rightFrameBarPadding = new Rect(FrameBarWidth + FrameBarPadding, editorViewRect.y, rightPaddingWidth, WidgetBarHeight);
			Widgets.DrawBoxSolid(rightFrameBarPadding, frameTimeBarColorDisabled);
			UIElements.DrawLineVertical(rightFrameBarPadding.x, rightFrameBarPadding.y, rightFrameBarPadding.height, frameTickColor);

			Rect frameInputRect = new Rect(frameBarRect.x, frameBarRect.y, frameBarRect.width, frameBarRect.height);
			DoFrameSliderHandle(frameInputRect);

			DoSeparatorHorizontal(editorViewRect.x, frameBarRect.yMax, editorViewRect.width);
			frameBarRect.yMax += 1;

			return frameBarRect;
		}

		private void DoFrameSlider(Rect frameBarRect)
		{
			Widgets.DrawBoxSolid(frameBarRect, frameTimeBarColor);

			Widgets.BeginGroup(frameBarRect);
			{
				Text.Anchor = TextAnchor.MiddleLeft;
				Text.Font = GameFont.Tiny;
				GUI.color = frameTickColor;

				float height = frameBarRect.height * 0.65f;
				float tickMarkPos;
				for (int i = 0; i <= FrameCountShown; i += TickInterval)
				{
					tickMarkPos = i * FrameTickMarkSpacing;

					float tickHeight;
					if (i % NextTickInterval() == 0)
					{
						tickHeight = height;
					}
					else if (i % TickInterval == 0)
					{
						tickHeight = Mathf.Lerp(height, height / 2, Zoom % 1);
					}
					else
					{
						tickHeight = Mathf.Lerp(height / 2, height / 4, Zoom % 1);
					}

					UIElements.DrawLineVertical(tickMarkPos, frameBarRect.yMax, -tickHeight, frameTickColor);

					if (TickInterval > 1)
					{
						tickHeight = height / 4;

						float subTickPos;
						int subTickCount = SubTickCount();
						for (int n = 1; n <= subTickCount; n++)
						{
							subTickPos = tickMarkPos + ((float)n / (subTickCount + 1)) * TickInterval * FrameTickMarkSpacing;
							UIElements.DrawLineVertical(subTickPos, frameBarRect.yMax, -tickHeight, frameTickColor);
						}
					}

					Rect labelRect = new Rect(tickMarkPos, frameBarRect.y, FrameTickMarkSpacing * TickInterval, frameBarRect.height).ContractedBy(3);
					Widgets.Label(labelRect, TimeStamp(i));
				}

				GUI.color = Color.white;
				Text.Font = GameFont.Small;
			}
			Widgets.EndGroup();

			DoSeparatorVertical(frameBarRect.x, frameBarRect.y, frameBarRect.height);
			DoSeparatorVertical(frameBarRect.xMax, frameBarRect.y, frameBarRect.height);
		}

		private void DoFrameSliderHandle(Rect rect)
		{
			if (UnityGUIBugsFixer.IsLeftMouseButtonPressed())
			{
				if (Mouse.IsOver(rect))
				{
					dragging = true;
					frame = FrameAtMousePos(rect);
					if (Event.current.type == EventType.MouseDown)
					{
						Event.current.Use();
					}
				}
				if (dragging && UnityGUIBugsFixer.MouseDrag(0))
				{
					frame = FrameAtMousePos(rect);
					if (Event.current.type == EventType.MouseDrag)
					{
						Event.current.Use();
					}
				}
			}
			else
			{
				dragging = false;
			}
		}

		private void DoDopesheetFrameTicks(Rect rect)
		{
			Widgets.BeginGroup(rect);
			{
				GUI.color = frameLineMajorDopesheetColor;

				float tickMarkPos;
				for (int i = 0; i <= FrameCountShown; i += TickInterval)
				{
					tickMarkPos = FrameBarPadding + i * FrameTickMarkSpacing;
					UIElements.DrawLineVertical(tickMarkPos, 0, rect.height - 1, frameLineMajorDopesheetColor);

					float subTickPos;
					int subTickCount = SubTickCount();
					for (int n = 1; n <= subTickCount; n++)
					{
						subTickPos = tickMarkPos + ((float)n / (subTickCount + 1)) * TickInterval * FrameTickMarkSpacing;
						UIElements.DrawLineVertical(subTickPos, 0, rect.height - 1, frameLineMinorDopesheetColor);
					}
				}

				GUI.color = Color.white;
			}
			Widgets.EndGroup();
		}

		private int SubTickCount()
		{
			if (TickInterval == 1)
			{
				return 0;
			}
			else if (TickInterval == 5)
			{
				return 4;
			}
			return 9;
		}

		private int NextTickInterval()
		{
			if (TickInterval == 1)
			{
				return 5;
			}
			return TickInterval * 2;
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

		private bool ButtonText(Rect rect, string label)
		{
			bool pressed = false;
			var anchor = Text.Anchor;
			Text.Anchor = TextAnchor.MiddleCenter;
			var font = Text.Font;
			Text.Font = GameFont.Small;

			Color buttonColor = propertyButtonColor;
			if (Mouse.IsOver(rect))
			{
				GUI.color = new Color(0.75f, 0.75f, 0.75f);
				if (Input.GetMouseButton(0))
				{
					buttonColor = propertyButtonPressedColor;
				}
			}
			Widgets.DrawBoxSolidWithOutline(rect, buttonColor, separatorColor);
			Widgets.Label(rect, label);
			if (Widgets.ButtonInvisible(rect))
			{
				pressed = true;
				SoundDefOf.Click.PlayOneShotOnCamera();
			}
			GUI.color = Color.white;
			Text.Anchor = anchor;
			Text.Font = font;
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

		private enum Tab
		{
			Dopesheet,
			Curves
		}
	}
}
