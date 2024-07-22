using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace SmashTools.Animations
{
	[StaticConstructorOnStartup]
	public class AnimationClipEditor : AnimationEditor
	{
		private const float MinLeftWindowSize = 300;
		private const float MinRightWindowSize = 250;

		private const float WidgetBarHeight = 24;
		private const float KeyframeSize = 20;
		private const float FrameInputWidth = 50;
		private const float TabWidth = 110;
		private const float PropertyEntryHeight = 24;
		private const float PropertyBtnWidth = 180;

		private const float FrameBarPadding = 40;
		private const float CollapseFrameDistance = 100;
		private const float MaxFrameZoom = 1000;
		private const float ZoomRate = 0.01f;

		private const int DefaultFrameCount = 60;
		private const float SecondsPerFrame = 1 / 60f;
		private const float DefaultAxisCount = 100;

		private static readonly Texture2D skipToBeginningTexture = ContentFinder<Texture2D>.Get("SmashTools/VideoReturnToBeginning");
		private static readonly Texture2D skipToPreviousTexture = ContentFinder<Texture2D>.Get("SmashTools/VideoReturnToPrevious");
		private static readonly Texture2D skipToNextTexture = ContentFinder<Texture2D>.Get("SmashTools/VideoSkipToNext");
		private static readonly Texture2D skipToEndTexture = ContentFinder<Texture2D>.Get("SmashTools/VideoSkipToEnd");

		private static readonly Texture2D animationEventTexture = ContentFinder<Texture2D>.Get("SmashTools/AnimationEvent");
		private static readonly Texture2D keyFrameTexture = ContentFinder<Texture2D>.Get("SmashTools/KeyFrame");
		private static readonly Texture2D addAnimationEventTexture = ContentFinder<Texture2D>.Get("SmashTools/AddEvent");
		private static readonly Texture2D addKeyFrameTexture = ContentFinder<Texture2D>.Get("SmashTools/AddKeyFrame");

		private static readonly Color propertyExpandedNameColor = new ColorInt(123, 123, 123).ToColor;
		private static readonly Color propertyLabelHighlightColor = new ColorInt(255, 255, 255, 10).ToColor;
		private static readonly Color itemSelectedColor = new ColorInt(87, 133, 217).ToColor;

		private static readonly Color animationEventBarColor = new ColorInt(49, 49, 49).ToColor;
		private static readonly Color animationKeyFrameBarColor = new ColorInt(47, 47, 47).ToColor;
		private static readonly Color animationKeyFrameBarFadeColor = new ColorInt(40, 40, 40).ToColor;
		private static readonly Color curveTopColor = new Color(backgroundCurvesColor.r, backgroundCurvesColor.g, backgroundCurvesColor.b, curveTopFadeColor.a);
		private static readonly Color curveTopFadeColor = new ColorInt(0, 0, 0, 25).ToColor;

		private static readonly Color frameTimeBarColor = new ColorInt(40, 64, 75).ToColor;
		private static readonly Color frameTimeBarColorDisabled = new ColorInt(10, 10, 10, 100).ToColor;
		private static readonly Color frameTickColor = new ColorInt(140, 140, 140).ToColor;
		private static readonly Color frameBarHighlightColor = new ColorInt(255, 255, 255, 5).ToColor;
		private static readonly Color frameBarHighlightMinorColor = new ColorInt(255, 255, 255, 2).ToColor;
		private static readonly Color frameBarHighlightOutlineColor = new ColorInt(68, 68, 68).ToColor;
		private static readonly Color frameBarCurveColor = new ColorInt(73, 73, 73).ToColor;
		private static readonly Color curveAxisColor = new ColorInt(93, 93, 93).ToColor;

		private static readonly Color keyFrameColor = new ColorInt(153, 153, 153).ToColor;
		private static readonly Color keyFrameTopColor = new ColorInt(108, 108, 108).ToColor;
		private static readonly Color keyFrameHighlightColor = new ColorInt(200, 200, 200).ToColor;

		private static readonly Color frameLineMajorDopesheetColor = new ColorInt(75, 75, 75).ToColor;
		private static readonly Color frameLineMinorDopesheetColor = new ColorInt(66, 66, 66).ToColor;
		private static readonly Color frameLineCurvesColor = new ColorInt(51, 51, 51).ToColor;

		private AnimationClip animation;
		private float zoomX = 1;
		private float zoomY = 1;

		private int frame = 0;
		private bool isPlaying;
		private int tickInterval = 1;
		private float curveTickInterval = 0.5f;
		private Dictionary<AnimationPropertyParent, bool> propertyExpanded = new Dictionary<AnimationPropertyParent, bool>();
		private Selector selector = new Selector();

		private Dialog_CameraView previewWindow;

		private float leftWindowSize = MinLeftWindowSize;

		private Vector2 editorScrollPos;
		private float realTimeToTick;

		private Vector2 dragPos;
		private DragItem dragging = DragItem.None;
		private EditTab tab = EditTab.Dopesheet;

		private readonly List<AnimationPropertyParent> propertiesToRemove = new List<AnimationPropertyParent>();
		private readonly HashSet<int> framesToDraw = new HashSet<int>();
		private readonly HashSet<int> parentFramesToDraw = new HashSet<int>();

		public AnimationClipEditor(Dialog_AnimationEditor parent) : base(parent)
		{
		}

		private float ExtraPadding { get; set; }

		public float FrameBarWidth => EditorWidth - FrameBarPadding * 2;

		private int FrameCountShown { get; set; }

		private int FrameCount
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

		private float FrameTickMarkSpacing
		{
			get
			{
				float spacing = CollapseFrameDistance / Mathf.Lerp(TickInterval / 2f, TickInterval, ZoomFrames % 1f);
				if (TickInterval == 1)
				{
					spacing /= 2.5f; //Return to being factor of 2, with max frame distance of 250 at 1 tick interval
				}
				return spacing;
			}
		}

		private float CurveAxisSpacing
		{
			get
			{
				float spacing = CollapseFrameDistance / Mathf.Lerp(CurveTickInterval / 2f, CurveTickInterval, ZoomCurve % 1f);
				if (CurveTickInterval == 1)
				{
					spacing /= 2.5f; //Return to being factor of 2, with max frame distance of 250 at 1 tick interval
				}
				return spacing;
			}
		}

		private int TickInterval
		{
			get
			{
				return tickInterval;
			}
		}

		private float CurveTickInterval
		{
			get
			{
				return curveTickInterval;
			}
		}

		private float ZoomFrames
		{
			get
			{
				return zoomX;
			}
			set
			{
				if (zoomX != value)
				{
					zoomX = Mathf.Clamp(value, 1, MaxFrameZoom);
					RecalculateTickInterval();
				}
			}
		}

		private float ZoomCurve
		{
			get
			{
				return zoomY;
			}
			set
			{
				if (zoomY != value)
				{
					zoomY = Mathf.Clamp(value, 1, MaxFrameZoom);
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
					SoundDefOf.Clock_Stop.PlayOneShotOnCamera();
				}
			}
		}

		public override void AnimatorLoaded(IAnimator animator)
		{
			base.AnimatorLoaded(animator);

			if (previewWindow != null && previewWindow.IsOpen)
			{
				previewWindow.Close();
			}
			if (parent.animator != null)
			{
				previewWindow = new Dialog_CameraView(DisableCameraView, () => animator.DrawPos, new Vector2(parent.windowRect.xMax - 50, parent.windowRect.yMax - 50));
				if (animator is Thing thing)
				{
					CameraJumper.TryJump(thing, mode: CameraJumper.MovementMode.Cut);
				}
				CameraView.Start(orthographicSize: CameraView.animationSettings.orthographicSize);
				Find.Selector.ClearSelection();
			}
		}

		public override void Update()
		{
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

		public override void OnClose()
		{
			if (previewWindow.IsOpen)
			{
				previewWindow.Close();
			}
		}

		public override void OnGUIHighPriority()
		{
			if (KeyBindingDefOf.TogglePause.KeyDownEvent)
			{
				Event.current.Use();
				IsPlaying = !IsPlaying;
			}

			if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftCommand))
			{
				if (animation && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.S)
				{
					Event.current.Use();
					AnimationLoader.ExportAnimationXml(animation);
				}
			}
		}

		public override void Draw(Rect rect)
		{
			rect.SplitVertically(leftWindowSize, out Rect leftRect, out Rect rightRect);

			DrawAnimatorSectionLeft(leftRect);
			DrawAnimatorSectionRight(rightRect);
		}

		private void DrawAnimatorSectionLeft(Rect rect)
		{
			DrawBackground(rect);

			bool previewInGame = previewWindow.IsOpen;

			string previewLabel = "ST_PreviewAnimation".Translate();
			float width = Text.CalcSize(previewLabel).x;
			Rect toggleRect = new Rect(rect.x, rect.y, width + 20, WidgetBarHeight);
			if (ToggleText(toggleRect, previewLabel, "ST_PreviewAnimationTooltip".Translate(), previewInGame))
			{
				if (previewInGame)
				{
					previewWindow.Close();
				}
				else
				{
					CameraView.ResetSize();
					Find.WindowStack.Add(previewWindow);
				}
			}

			DoSeparatorHorizontal(rect.x, rect.y + WidgetBarHeight, rect.width);

			DoSeparatorHorizontal(rect.x, rect.yMax, rect.width);

			DoSeparatorVertical(rect.xMax, rect.y, rect.height);

			if (parent.animator == null)
			{
				GUI.enabled = false;
			}
			Rect buttonRect = new Rect(toggleRect.xMax, rect.y, WidgetBarHeight, WidgetBarHeight);
			if (AnimationButton(buttonRect, skipToBeginningTexture, "ST_SkipFrameBeginningTooltip".Translate()))
			{
				frame = 0;
			}
			DoSeparatorVertical(buttonRect.x, buttonRect.y, buttonRect.height);
			buttonRect.x += 1;

			buttonRect.x += buttonRect.width;
			if (AnimationButton(buttonRect, skipToPreviousTexture, "ST_SkipFramePreviousTooltip".Translate()))
			{
				//TODO
			}
			DoSeparatorVertical(buttonRect.x, buttonRect.y, buttonRect.height);
			buttonRect.x += 1;

			buttonRect.x += buttonRect.width;
			if (AnimationButton(buttonRect, IsPlaying ? CameraView.pauseTexture : CameraView.playTexture, IsPlaying ? "ST_PauseAnimationTooltip".Translate() : "ST_PlayAnimationTooltip".Translate()))
			{
				IsPlaying = !IsPlaying;
			}
			DoSeparatorVertical(buttonRect.x, buttonRect.y, buttonRect.height);
			buttonRect.x += 1;

			buttonRect.x += buttonRect.width;
			if (AnimationButton(buttonRect, skipToNextTexture, "ST_SkipFrameNextTooltip".Translate()))
			{
				//TODO
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

			Rect frameNumberRect = new Rect(rect.xMax - FrameInputWidth, rect.y, FrameInputWidth, buttonRect.height).ContractedBy(2);
			string nullBuffer = null;
			Widgets.TextFieldNumeric(frameNumberRect, ref frame, ref nullBuffer);
			CheckTextFieldControlFocus(frameNumberRect);

			Rect animClipDropdownRect = new Rect(rect.x, buttonRect.yMax, 200, buttonRect.height);
			Rect animClipSelectRect = new Rect(parent.windowRect.x + parent.EditorMargin + animClipDropdownRect.x, 
				parent.windowRect.y + parent.EditorMargin + animClipDropdownRect.yMax, animClipDropdownRect.width, 500);
			string animLabel = animation?.FileName ?? "[No Clip]";
			string animPath = animation?.FilePath ?? string.Empty;
			if (Dropdown(animClipDropdownRect, animLabel, animPath))
			{
				Find.WindowStack.Add(new Dialog_AnimationClipLister(parent.animator, animClipSelectRect, animation, onFilePicked: LoadAnimation));
			}
			DoSeparatorVertical(animClipDropdownRect.xMax, animClipDropdownRect.y, animClipDropdownRect.height);

			Rect animButtonRect = new Rect(rect.xMax - buttonRect.height, animClipDropdownRect.y, buttonRect.height, buttonRect.height);
			if (AnimationButton(animButtonRect, addAnimationEventTexture, "ST_AddAnimationEvent".Translate()))
			{
				parent.ChangeMade();
			}
			DoSeparatorVertical(animButtonRect.x, animButtonRect.y, animButtonRect.height);
			animButtonRect.x -= 1;

			animButtonRect.x -= animButtonRect.height;

			var enabled = GUI.enabled;
			if (enabled && (animation == null || animation.properties.NullOrEmpty() || IsPlaying))
			{
				GUI.enabled = false;
			}
			if (AnimationButton(animButtonRect, addKeyFrameTexture, "ST_AddKeyFrame".Translate()))
			{
				foreach (AnimationPropertyParent propertyParent in animation.properties)
				{
					AddKeyFramesForParent(propertyParent);
				}
				animation.RecacheFrameCount();
				parent.ChangeMade();
			}
			GUI.enabled = enabled;
			DoSeparatorVertical(animButtonRect.x, animButtonRect.y, animButtonRect.height);
			animButtonRect.x -= 1;

			DoSeparatorHorizontal(animClipDropdownRect.x, animClipDropdownRect.yMax, rect.width);

			//Add KeyframeSize to keep keyframe bars aligned with their properties
			Rect rowRect = new Rect(rect.x + 4, animClipDropdownRect.yMax + KeyframeSize, rect.width - 10, PropertyEntryHeight);
			Rect fullPropertyRect = rowRect;
			if (animation != null && !animation.properties.NullOrEmpty())
			{
				float collapseBtnSize = rowRect.height;
				float propertyBtnSize = rowRect.height;
				float expandedIndent = collapseBtnSize;

				bool lightBg = false;
				foreach (AnimationPropertyParent propertyParent in animation.properties)
				{
					if (selector.IsSelected(propertyParent))
					{
						Widgets.DrawBoxSolid(rowRect, itemSelectedColor);
					}
					else if (lightBg)
					{
						Widgets.DrawBoxSolid(rowRect, backgroundLightColor);
					}

					Rect selectParentRect = new Rect(rowRect.x + collapseBtnSize, rowRect.y, rowRect.width - PropertyEntryHeight * 3 - propertyBtnSize * 2 - collapseBtnSize, PropertyEntryHeight);
					if (Widgets.ButtonInvisible(selectParentRect, doMouseoverSound: false))
					{
						selector.Select(propertyParent, clear: !Input.GetKey(KeyCode.LeftControl));
					}

					Rect collapseBtnRect = new Rect(rowRect.x, rowRect.y, collapseBtnSize, collapseBtnSize).ContractedBy(3);
					bool expanded = propertyExpanded.TryGetValue(propertyParent, false);
					if (!propertyParent.Children.NullOrEmpty() && Widgets.ButtonImage(collapseBtnRect, expanded ? TexButton.Collapse : TexButton.Reveal, keyFrameColor, keyFrameHighlightColor))
					{
						expanded = !expanded; //modify local variable so expanded state can be used for drawing inner properties
						propertyExpanded[propertyParent] = expanded;

						if (expanded)
						{
							SoundDefOf.TabOpen.PlayOneShotOnCamera();
						}
						else
						{
							SoundDefOf.TabClose.PlayOneShotOnCamera();
						}
					}
					else if (propertyParent.Single != null)
					{
						float inputBoxWidth = PropertyEntryHeight * 3;
						Rect inputRect = new Rect(rowRect.xMax - collapseBtnSize - inputBoxWidth, rowRect.y, inputBoxWidth, rowRect.height);
						KeyFrameInput(inputRect, propertyParent.Single);
					}

					Rect propertyPropertyBtnRect = new Rect(rowRect.xMax - collapseBtnRect.width, rowRect.y, propertyBtnSize, propertyBtnSize).ContractedBy(6);
					if (Widgets.ButtonImage(propertyPropertyBtnRect, keyFrameTexture, keyFrameColor, keyFrameHighlightColor))
					{
						List<FloatMenuOption> options = new List<FloatMenuOption>();
						var removePropsOption = new FloatMenuOption("ST_RemoveProperties".Translate(), delegate ()
						{
							propertiesToRemove.Add(propertyParent);
							parent.ChangeMade();
						});
						options.Add(removePropsOption);

						var addKeyOption = new FloatMenuOption("ST_AddKey".Translate(), delegate ()
						{
							AddKeyFramesForParent(propertyParent);
							parent.ChangeMade();
						});
						addKeyOption.Disabled = propertyParent.AllKeyFramesAt(frame);
						options.Add(addKeyOption);

						var removeKeyOption = new FloatMenuOption("ST_RemoveKey".Translate(), delegate ()
						{
							RemoveKeyFramesForParent(propertyParent);
							parent.ChangeMade();
						});
						removeKeyOption.Disabled = !propertyParent.AnyKeyFrameAt(frame);
						options.Add(removeKeyOption);

						Find.WindowStack.Add(new FloatMenu(options));
					}

					Rect propertyParentRect = new Rect(collapseBtnRect.xMax, rowRect.y, rowRect.width - collapseBtnSize - propertyBtnSize, rowRect.height);
					if (Mouse.IsOver(propertyParentRect))
					{
						Widgets.DrawBoxSolid(propertyParentRect, propertyLabelHighlightColor);
					}
					Widgets.Label(propertyParentRect, $"{propertyParent.Type.Name} : {propertyParent.Name}");

					if (expanded)
					{
						foreach (AnimationProperty property in propertyParent.Children)
						{
							lightBg = !lightBg;
							rowRect.y += rowRect.height;
							fullPropertyRect.height += rowRect.height;

							if (selector.IsSelected(property))
							{
								Widgets.DrawBoxSolid(rowRect, itemSelectedColor);
							}
							else if (lightBg)
							{
								Widgets.DrawBoxSolid(rowRect, backgroundLightColor);
							}

							Rect selectPropertyRect = new Rect(rowRect.x + collapseBtnSize, rowRect.y, rowRect.width - PropertyEntryHeight * 3 - propertyBtnSize * 2 - collapseBtnSize, PropertyEntryHeight);
							if (Widgets.ButtonInvisible(selectPropertyRect, doMouseoverSound: false))
							{
								selector.Select(property, clear: !Input.GetKey(KeyCode.LeftControl));
							}

							Rect propertyBtnRect = new Rect(rowRect.xMax - collapseBtnRect.width, rowRect.y, propertyBtnSize, propertyBtnSize).ContractedBy(6);
							if (Widgets.ButtonImage(propertyBtnRect, keyFrameTexture, keyFrameColor, keyFrameHighlightColor))
							{
								List<FloatMenuOption> options = new List<FloatMenuOption>();
								var removePropsOption = new FloatMenuOption("ST_RemoveProperties".Translate(), delegate ()
								{
									propertiesToRemove.Add(propertyParent);
									parent.ChangeMade();
								});
								options.Add(removePropsOption);

								var addKeyOption = new FloatMenuOption("ST_AddKey".Translate(), delegate ()
								{
									property.curve.Add(frame, 0);
									parent.ChangeMade();
								});
								addKeyOption.Disabled = property.curve.KeyFrameAt(frame);
								options.Add(addKeyOption);

								var removeKeyOption = new FloatMenuOption("ST_RemoveKey".Translate(), delegate ()
								{
									property.curve.Remove(frame);
									parent.ChangeMade();
								});
								removeKeyOption.Disabled = !property.curve.KeyFrameAt(frame);
								options.Add(removeKeyOption);

								Find.WindowStack.Add(new FloatMenu(options));
							}

							float inputBoxWidth = PropertyEntryHeight * 3;
							Rect inputRect = new Rect(rowRect.xMax - collapseBtnSize - inputBoxWidth, rowRect.y, inputBoxWidth, rowRect.height);
							KeyFrameInput(inputRect, property);

							GUI.color = propertyExpandedNameColor;
							Rect propertyRect = new Rect(propertyParentRect.x + expandedIndent, rowRect.y, rowRect.width - collapseBtnSize - propertyBtnSize, rowRect.height);
							if (Mouse.IsOver(propertyRect))
							{
								Widgets.DrawBoxSolid(propertyRect, propertyLabelHighlightColor);
							}
							Widgets.Label(propertyRect, $"{propertyParent.Name}.{property.Name}");
							GUI.color = Color.white;
						}
					}
					lightBg = !lightBg;
					rowRect.y += rowRect.height;
					fullPropertyRect.height += rowRect.height;
				}
				rowRect.y += PropertyEntryHeight / 2; //Extra padding for add property btn
			}

			if (!Mouse.IsOver(fullPropertyRect) && Input.GetMouseButton(0))
			{
				selector.ClearSelectedProperties();
			}

			RemoveFlaggedProperties();

			enabled = GUI.enabled;
			if (animation == null)
			{
				GUI.enabled = false;
			}
			Rect propertyButtonRect = new Rect(rect.xMax / 2 - PropertyBtnWidth / 2, rowRect.yMax, PropertyBtnWidth, WidgetBarHeight);
			if (ButtonText(propertyButtonRect, "ST_AddProperty".Translate()))
			{
				Vector2 propertyDropdownPosition = new Vector2(parent.windowRect.x + parent.EditorMargin + propertyButtonRect.xMax + 2, 
					parent.windowRect.y + parent.EditorMargin + propertyButtonRect.y + 1);
				Find.WindowStack.Add(new Dialog_PropertySelect(parent.animator, animation, propertyDropdownPosition, propertyAdded: InjectKeyFramesNewProperty));
			}
			GUI.enabled = enabled;

			Rect tabRect = new Rect(rect.xMax - TabWidth - 24, rect.yMax - WidgetBarHeight, TabWidth, WidgetBarHeight);
			DoSeparatorHorizontal(tabRect.xMax, tabRect.y, 24);
			if (ToggleText(tabRect, "ST_CurvesTab".Translate(), null, tab == EditTab.Curves))
			{
				FlipTab();
			}
			tabRect.x -= tabRect.width;
			if (ToggleText(tabRect, "ST_DopesheetTab".Translate(), null, tab == EditTab.Dopesheet))
			{
				FlipTab();
			}
			DoSeparatorHorizontal(rect.x, tabRect.y, rect.x + tabRect.x);

			GUI.enabled = true;

			DoResizerButton(rect, ref leftWindowSize, MinLeftWindowSize, MinRightWindowSize);

			void FlipTab()
			{
				tab = tab switch
				{
					EditTab.Dopesheet => EditTab.Curves,
					EditTab.Curves => EditTab.Dopesheet,
					_ => throw new NotImplementedException(),
				};
			}
		}

		private void KeyFrameInput(Rect inputRect, AnimationProperty property)
		{
			inputRect = inputRect.ContractedBy(2);
			string nullBuffer = null;
			switch (property.PropType)
			{
				case AnimationProperty.PropertyType.Float:
					{
						float value = property.curve[frame];
						float valueBefore = value;
						Widgets.TextFieldNumeric(inputRect, ref value, ref nullBuffer, float.MinValue, float.MaxValue);
						if (!Mathf.Approximately(value, valueBefore))
						{
							property.curve.Set(frame, value);
							animation.RecacheFrameCount();
							parent.ChangeMade();
						}
					}
					break;
				case AnimationProperty.PropertyType.Int:
					{
						int value = Mathf.RoundToInt(property.curve[frame]);
						int valueBefore = value;
						Widgets.TextFieldNumeric(inputRect, ref value, ref nullBuffer, float.MinValue, float.MaxValue);
						if (value != valueBefore)
						{
							property.curve.Set(frame, value);
							animation.RecacheFrameCount();
							parent.ChangeMade();
						}
					}
					break;
				case AnimationProperty.PropertyType.Bool:
					{
						//bool value = Mathf.Approximately(propertyParent.Single.curve.Evaluate(frame / FrameCount), 1);
						//Widgets.Checkbox(inputBox, ref value, float.MinValue, float.MaxValue);
						animation.RecacheFrameCount();
						parent.ChangeMade();
					}
					break;
			}
			CheckTextFieldControlFocus(inputRect);
		}

		private void DrawAnimatorSectionRight(Rect rect)
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

					Rect blendRect = new Rect(editorViewRect.x, animationEventBarRect.yMax, editorViewRect.width, fadeHeight);

					switch (tab)
					{
						case EditTab.Dopesheet:
							{
								float blendY = DrawBlend(blendRect, animationKeyFrameBarFadeColor, animationKeyFrameBarColor);

								Rect keyFrameTopBarRect = new Rect(editorViewRect.x, blendY, editorViewRect.width, KeyframeSize - fadeSize);
								Widgets.DrawBoxSolid(keyFrameTopBarRect, animationKeyFrameBarColor);

								Rect dopeSheetRect = new Rect(editorViewRect.x, keyFrameTopBarRect.yMax, editorViewRect.width, editorViewRect.height - keyFrameTopBarRect.yMax);
								DrawBackground(dopeSheetRect);

								DrawDopesheetFrameTicks(dopeSheetRect);

								DrawKeyFrameMarkers(dopeSheetRect);

								if (DragWindow(dopeSheetRect, DragItem.KeyFrameWindow, button: 2))
								{
									SetDragPos();
								}
								if (SelectionBox(rect.position, dopeSheetRect, out Rect dragRect))
								{

								}
							}
							break;
						case EditTab.Curves:
							{
								Rect curveBackgroundRect = new Rect(editorViewRect.x, animationEventBarRect.yMax, editorViewRect.width, editorViewRect.height - animationEventBarRect.height);
								DrawBackgroundDark(curveBackgroundRect);
								DrawCurvesFrameTicks(curveBackgroundRect);

								DrawBlend(blendRect, curveTopFadeColor, curveTopColor);

								Rect curveFrameBarRect = new Rect(editorOutRect.x, curveBackgroundRect.y, FrameBarPadding, curveBackgroundRect.height);
								DrawAxis(curveFrameBarRect);

								Rect curvesRect = new Rect(frameBarRect.x, curveBackgroundRect.y, FrameBarWidth, curveBackgroundRect.height);
								DrawCurves(curvesRect);

								if (DragWindow(curveBackgroundRect, DragItem.KeyFrameWindow, button: 2))
								{
									SetDragPos();
								}
								if (SelectionBox(rect.position, curveBackgroundRect, out Rect dragRect))
								{

								}
							}
							break;
					}

					float frameLinePos = frameBarRect.x + frame * FrameTickMarkSpacing;
					UIElements.DrawLineVertical(frameLinePos, frameBarRect.y, editorViewRect.height, Color.white);
				}
				EndScrollViewNoScrollbarControls();
			}
			Widgets.EndGroup();

			if (Mouse.IsOver(rect) && Event.current.type == EventType.ScrollWheel)
			{
				float value = Event.current.delta.y * ZoomRate;

				bool horizontal = Input.GetKey(KeyCode.LeftControl);
				bool vertical = Input.GetKey(KeyCode.LeftShift);
				if (!horizontal && !vertical)
				{
					ZoomFrames += value;
					ZoomCurve += value;
					Event.current.Use();
				}
				else if (horizontal)
				{
					ZoomFrames += value;
					Event.current.Use();
				}
				else if (vertical)
				{
					ZoomCurve += value;
					Event.current.Use();
				}
			}

			void SetDragPos()
			{
				Vector2 mousePos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
				Vector2 mouseDiff = dragPos - mousePos;
				dragPos = mousePos;
				editorScrollPos += new Vector2(mouseDiff.x, -mouseDiff.y);
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

			DoFrameSliderHandle(frameBarRect);

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
						tickHeight = Mathf.Lerp(height, height / 2, ZoomFrames % 1);
					}
					else
					{
						tickHeight = Mathf.Lerp(height / 2, height / 4, ZoomFrames % 1);
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
			if (DragWindow(rect, DragItem.FrameBar))
			{
				frame = FrameAtMousePos(rect);
			}
		}

		private void DrawDopesheetFrameTicks(Rect rect)
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

		private bool DisableCameraView()
		{
			if (parent.animator is Thing thing)
			{
				return !thing.Spawned;
			}
			return parent.animator == null;
		}

		private void RemoveFlaggedProperties()
		{
			foreach (AnimationPropertyParent propertyParent in propertiesToRemove)
			{
				animation.properties.Remove(propertyParent);
			}
			propertiesToRemove.Clear();
		}

		private int FrameAtMousePos(Rect rect)
		{
			return Mathf.RoundToInt(Mathf.Clamp((Event.current.mousePosition.x - rect.x) / rect.width * FrameCountShown, 0, FrameCountShown));
		}

		private void RecalculateTickInterval()
		{
			int power = Mathf.FloorToInt(ZoomFrames) - 2;
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
			propertyExpanded.Clear();
			AnimationClip clip = AnimationLoader.LoadFile<AnimationClip>(fileInfo.FullName);
			animation = clip;
			if (!clip)
			{
				Messages.Message($"Unable to load animation file at {fileInfo.FullName}.", MessageTypeDefOf.RejectInput);
			}
		}

		private string TimeStamp(int frame)
		{
			int seconds = frame / 60;
			int frames = frame % 60;
			return $"{seconds}:{frames:00}";
		}

		private string AxisStamp(float axis)
		{
			return $"{axis:####0.###}";
		}

		private void DrawCurvesFrameTicks(Rect rect)
		{
			Widgets.BeginGroup(rect);
			{
				GUI.color = frameLineMajorDopesheetColor;

				float tickMarkPos;
				for (int i = 0; i <= FrameCountShown; i += TickInterval)
				{
					tickMarkPos = FrameBarPadding + i * FrameTickMarkSpacing;
					UIElements.DrawLineVertical(tickMarkPos, 0, rect.height - 1, frameLineCurvesColor);
				}

				GUI.color = Color.white;
			}
			Widgets.EndGroup();
		}

		private void DrawKeyFrameMarkers(Rect rect)
		{
			if (animation != null && !animation.properties.NullOrEmpty())
			{
				if ((Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete)) && selector.AnyKeyFramesSelected)
				{
					foreach ((AnimationProperty property, int frame) in selector.selPropKeyFrames)
					{
						property.curve.Remove(frame);
					}
				}

				bool clearSelect = Input.GetMouseButtonUp(0) && !Input.GetKeyUp(KeyCode.LeftControl);
				framesToDraw.Clear();
				Rect rowRect = new Rect(rect.x, rect.y, rect.width, PropertyEntryHeight);
				float parentIconY = rowRect.y - KeyframeSize; //Next bar up
				foreach (AnimationPropertyParent propertyParent in animation.properties)
				{
					float propertyIconY = rowRect.y;

					parentFramesToDraw.Clear();
					Widgets.DrawBoxSolidWithOutline(rowRect.ContractedBy(1), frameBarHighlightColor, frameBarHighlightOutlineColor);
					if (propertyParent.Single?.curve != null && !propertyParent.Single.curve.points.NullOrEmpty())
					{
						foreach (AnimationCurve.KeyFrame keyFrame in propertyParent.Single.curve.points)
						{
							framesToDraw.Add(keyFrame.frame);
							if (KeyFrameButton(rowRect.y, keyFrame.frame, keyFrameColor, selector.IsSelected(propertyParent.Single, keyFrame.frame), ref clearSelect))
							{
								selector.SelectFrame(propertyParent, keyFrame.frame);
							}
						}
					}
					else if (!propertyParent.Children.NullOrEmpty())
					{
						Widgets.DrawBoxSolidWithOutline(rowRect.ContractedBy(2), frameBarHighlightColor, frameBarHighlightOutlineColor);

						bool expanded = propertyExpanded.TryGetValue(propertyParent, false);
						foreach (AnimationProperty property in propertyParent.Children)
						{
							if (expanded)
							{
								rowRect.y += rowRect.height;
								Widgets.DrawBoxSolidWithOutline(rowRect.ContractedBy(1), frameBarHighlightMinorColor, frameBarHighlightOutlineColor);
							}

							foreach (AnimationCurve.KeyFrame keyFrame in property.curve.points)
							{
								framesToDraw.Add(keyFrame.frame);
								parentFramesToDraw.Add(keyFrame.frame);
								if (expanded)
								{
									if (KeyFrameButton(rowRect.y, keyFrame.frame, keyFrameColor, selector.IsSelected(property, keyFrame.frame), ref clearSelect))
									{
										clearSelect = false;
										selector.SelectFrame(property, keyFrame.frame);
									}
								}
							}
						}
					}

					if (parentFramesToDraw.Count > 0)
					{
						foreach (int frame in parentFramesToDraw)
						{
							if (KeyFrameButton(propertyIconY, frame, keyFrameColor, selector.IsSelected(propertyParent, frame), ref clearSelect))
							{
								clearSelect = false;
								selector.SelectFrame(propertyParent, frame);
							}
						}
					}

					rowRect.y += rowRect.height;
				}
				if (framesToDraw.Count > 0)
				{
					foreach (int frame in framesToDraw)
					{
						if (KeyFrameButton(parentIconY, frame, keyFrameTopColor, selector.IsSelected(frame), ref clearSelect))
						{
							clearSelect = false;
							selector.SelectAll(animation, frame);
						}
					}
				}

				if (clearSelect)
				{
					selector.ClearSelectedKeyFrames();
				}
			}

			bool KeyFrameButton(float y, int frame, Color color, bool selected, ref bool clearSelect)
			{
				bool result = false;

				GUI.color = selected ? itemSelectedColor : color;
				float tickMarkPos = FrameBarPadding + frame * FrameTickMarkSpacing;
				Rect keyFrameRect = new Rect(tickMarkPos - PropertyEntryHeight / 2 + 0.5f, y, PropertyEntryHeight, PropertyEntryHeight).ContractedBy(4);
				clearSelect &= !Mouse.IsOver(keyFrameRect); //Only allow clear select if mouse is not over any key frame button
				GUI.DrawTexture(keyFrameRect, keyFrameTexture);
				if (Widgets.ButtonInvisible(keyFrameRect, doMouseoverSound: false))
				{
					result = true;
				}
				GUI.color = Color.white;

				return result;
			}
		}

		private void DrawCurves(Rect rect)
		{
			FloatRange xRange = new FloatRange(0, 60);
			FloatRange yRange = new FloatRange(-1, 1);

			if (animation != null && !animation.properties.NullOrEmpty())
			{
				if (!selector.AnyPropertiesSelected)
				{
					//TODO
					foreach (AnimationPropertyParent propertyParent in animation.properties)
					{
						DrawPropertyParent(propertyParent);
						//Graph.DrawAnimationCurve(rect, func, xRange, yRange);
					}
				}
				else
				{
					foreach (AnimationPropertyParent propertyParent in selector.selectedParents)
					{
						DrawPropertyParent(propertyParent);
					}
					foreach (AnimationProperty property in selector.selectedProperties)
					{
						DrawProperty(property);
					}
				}
			}

			void DrawPropertyParent(AnimationPropertyParent propertyParent)
			{
				//If neither statements are true, propertyParent is not valid so it shouldn't render anyways
				if (propertyParent.Single != null)
				{
					DrawProperty(propertyParent.Single);
				}
				else if (!propertyParent.Children.NullOrEmpty())
				{
					foreach (AnimationProperty property in propertyParent.Children)
					{
						if (!selector.selectedProperties.Contains(property))
						{
							DrawProperty(property);
						}
					}
				}
			}

			void DrawProperty(AnimationProperty property)
			{
				Graph.DrawAnimationCurve(rect, property.curve, xRange, yRange);
			}
		}

		private void DrawAxis(Rect rect)
		{
			Widgets.BeginGroup(rect);
			{
				Text.Anchor = TextAnchor.LowerRight;
				Text.Font = GameFont.Tiny;

				float height = CurveAxisSpacing * TickInterval;
				for (float i = 0; i <= CurveAxisSpacing; i += CurveTickInterval)
				{
					float tickMarkPos = i * CurveAxisSpacing;

					UIElements.DrawLineHorizontal(rect.x, tickMarkPos, rect.width, frameBarCurveColor);

					GUI.color = curveAxisColor;
					Rect labelRect = new Rect(rect.x, tickMarkPos - height, rect.width, height).ContractedBy(3); //Subtract height since y axis is top to bottom
					Widgets.Label(labelRect, AxisStamp(i));
				}

				GUI.color = Color.white;
				Text.Font = GameFont.Small;
			}
			Widgets.EndGroup();
		}

		private bool DragWindow(Rect rect, DragItem dragItem, int button = 0)
		{
			return DragWindow(rect, ref dragPos, SetDragItem, IsDragging, StopDragging, button: button);

			void SetDragItem()
			{
				dragging = dragItem;
			}

			bool IsDragging()
			{
				return dragging == dragItem;
			}

			void StopDragging()
			{
				dragging = DragItem.None;
			}
		}

		private void InjectKeyFramesNewProperty(AnimationPropertyParent propertyParent)
		{
			if (propertyParent.Single != null)
			{
				Inject(propertyParent.Single);
			}
			else if (!propertyParent.Children.NullOrEmpty())
			{
				foreach (AnimationProperty property in propertyParent.Children)
				{
					Inject(property);
				}
			}

			void Inject(AnimationProperty property)
			{
				property.curve.Set(0, 0);
				property.curve.Set(FrameCount, 0);
				parent.ChangeMade();
			}
		}

		private void AddKeyFramesForParent(AnimationPropertyParent propertyParent)
		{
			if (propertyParent.Single != null)
			{
				propertyParent.Single.curve.Add(frame, 0);
			}
			else if (!propertyParent.Children.NullOrEmpty())
			{
				foreach (AnimationProperty property in propertyParent.Children)
				{
					property.curve.Add(frame, 0);
				}
			}
			parent.ChangeMade();
		}

		private void RemoveKeyFramesForParent(AnimationPropertyParent propertyParent)
		{
			if (propertyParent.Single != null)
			{
				propertyParent.Single.curve.Remove(frame);
			}
			else if (!propertyParent.Children.NullOrEmpty())
			{
				foreach (AnimationProperty property in propertyParent.Children)
				{
					property.curve.Remove(frame);
				}
			}
			parent.ChangeMade();
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

		private enum DragItem
		{
			None,
			FrameBar,
			KeyFrameWindow,
		}

		private enum EditTab
		{
			Dopesheet,
			Curves
		}

		private class Selector
		{
			public List<(AnimationProperty property, int frame)> selPropKeyFrames = new List<(AnimationProperty property, int frame)>();

			public HashSet<AnimationPropertyParent> selectedParents = new HashSet<AnimationPropertyParent>();
			public HashSet<AnimationProperty> selectedProperties = new HashSet<AnimationProperty>();

			public bool AnyPropertiesSelected => selectedParents.Count > 0 || selectedProperties.Count > 0;

			public bool AnyKeyFramesSelected => selPropKeyFrames.Count > 0;

			public bool IsSelected(int frame)
			{
				return selPropKeyFrames.Any(selection => selection.frame == frame);
			}

			public bool IsSelected(AnimationPropertyParent propertyParent, int frame)
			{
				if (propertyParent == null || !propertyParent.IsValid)
				{
					return false;
				}
				if (propertyParent.Single != null)
				{
					return IsSelected(propertyParent.Single, frame);
				}
				else if (!propertyParent.Children.NullOrEmpty())
				{
					foreach (AnimationProperty property in propertyParent.Children)
					{
						if (IsSelected(property, frame))
						{
							return true;
						}
					}
				}
				return false;
			}

			public bool IsSelected(AnimationProperty property, int frame)
			{
				if (property == null || !property.IsValid)
				{
					return false;
				}
				return selPropKeyFrames.Contains((property, frame));
			}

			public bool IsSelected(AnimationPropertyParent propertyParent)
			{
				if (propertyParent == null || !propertyParent.IsValid)
				{
					return false;
				}
				return selectedParents.Contains(propertyParent);
			}

			public bool IsSelected(AnimationProperty property)
			{
				if (property == null || !property.IsValid)
				{
					return false;
				}
				return selectedProperties.Contains(property);
			}

			public void SelectAll(AnimationClip clip, int frame)
			{
				if (!Input.GetKey(KeyCode.LeftControl))
				{
					ClearSelectedKeyFrames();
				}
				foreach (AnimationPropertyParent propertyParent in clip.properties)
				{
					SelectFrame(propertyParent, frame, clear: false);
				}
			}

			public void SelectFrame(AnimationPropertyParent propertyParent, int frame, bool clear = true)
			{
				if (clear && !Input.GetKey(KeyCode.LeftControl))
				{
					ClearSelectedKeyFrames();
				}
				if (propertyParent.Single != null)
				{
					SelectFrame(propertyParent.Single, frame);
				}
				else if (!propertyParent.Children.NullOrEmpty())
				{
					foreach (AnimationProperty property in propertyParent.Children)
					{
						SelectFrame(property, frame, clear: false, sort: false);
					}
					selPropKeyFrames.SortBy(selProp => selProp.frame);
				}
			}

			public void SelectFrame(AnimationProperty property, int frame, bool clear = true, bool sort = true)
			{
				if (clear && !Input.GetKey(KeyCode.LeftControl))
				{
					ClearSelectedKeyFrames();
				}
				if (selPropKeyFrames.Contains((property, frame)))
				{
					return;
				}

				selPropKeyFrames.Add((property, frame));
				if (sort)
				{
					selPropKeyFrames.SortBy(selProp => selProp.frame);
				}
			}

			public void Select(AnimationPropertyParent propertyParent, bool clear = true)
			{
				if (clear)
				{
					ClearSelectedProperties();
				}
				selectedParents.Add(propertyParent);
			}

			public void Select(AnimationProperty property, bool clear = true)
			{
				if (clear)
				{
					ClearSelectedProperties();
				}
				selectedProperties.Add(property);
			}

			public void ClearSelectedKeyFrames()
			{
				selPropKeyFrames.Clear();
			}

			public void ClearSelectedProperties()
			{
				selectedParents.Clear();
				selectedProperties.Clear();
			}
		}
	}
}
