using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static SmashTools.Dialog_GraphEditor;
using Verse.Noise;
using Verse.Sound;

namespace SmashTools.Animations
{
	public class Dialog_CameraView : Window
	{
		private const float WindowMargin = 15;
		private const float LeftWindowWidth = 160;
		private const float WindowRatio = (LeftWindowWidth + DefaultViewSize) / DefaultViewSize;

		private const float LeftWindowMinHeight = 250;
		private const float MinWidth = LeftWindowMinHeight * WindowRatio + WindowMargin * 2;

		private const float DefaultViewSize = 300;
		private const float ResizerBtnSize = 24;

		private readonly Func<bool> disabled;
		private readonly Func<Vector3> position;

		private Listing_SplitColumns lister = new Listing_SplitColumns();
		private Vector2 windowPosition;
		private Vector2 windowSize;

		private bool dragging = false;
		private float timeFading = 0;

		/* ----- Resizing ----- */

		private bool resizing = false;
		private bool needsResizing = false;
		private Rect startingWindowRect;
		private Rect resizeLaterRect;

		/* ------------------------------- */

		public Dialog_CameraView(Func<bool> disabled, Func<Vector3> position, Vector2 bottomRight, float size = 1)
		{
			this.disabled = disabled;
			this.position = position;

			windowSize = new Vector2(LeftWindowWidth + DefaultViewSize * size, DefaultViewSize * size);
			windowPosition = new Vector2(bottomRight.x - windowSize.x, bottomRight.y - windowSize.y);
			SetWindowProperties();
		}

		public override Vector2 InitialSize => windowSize;

		protected override float Margin => 0;

		private void SetWindowProperties()
		{
			this.closeOnClickedOutside = false;
			this.draggable = true;
			this.absorbInputAroundWindow = false;
			this.preventCameraMotion = false;
			this.doCloseX = true;
			this.layer = WindowLayer.Super;
		}

		protected override void SetInitialSizeAndPosition()
		{
			windowRect = new Rect(windowPosition, windowSize).Rounded();
		}

		public override void WindowUpdate()
		{
			base.WindowUpdate();
			if (!disabled())
			{
				if (CameraView.animationSettings.drawCellGrid)
				{
					CameraView.DrawMapGridInView();
				}
				CameraView.Update(position());
			}
		}

		public override void WindowOnGUI()
		{
			if (needsResizing)
			{
				needsResizing = false;
				windowRect = resizeLaterRect;
			}
			base.WindowOnGUI();
		}

		public override void DoWindowContents(Rect inRect)
		{
			Widgets.DrawWindowBackground(inRect);
			DoResizerButton(windowRect);
			Rect rect = inRect.ContractedBy(WindowMargin);

			DrawPreviewWindow(rect, disabled.Invoke());
		}

		private void DrawPreviewWindow(Rect rect, bool disabled)
		{
			string previewWindowString = "Preview Window Disabled";

			bool darkenWindow = false;
			if (!disabled)
			{
				try
				{
					float camRectSize = rect.height;// * PercentCameraBoxWidth;
					Rect cameraRect = new Rect(rect.x + rect.width - camRectSize, rect.y, camRectSize, camRectSize);
					disabled = !CameraView.RenderAt(cameraRect);

					if (!disabled)
					{
						GUIState.Push();
						{
							UIElements.DrawLineVertical(cameraRect.x - 1, cameraRect.y, camRectSize, UIElements.MenuSectionBGBorderColor);
							//UIElements.DrawLineHorizontal(cameraRect.x - 1, cameraRect.y + camRectSize + 1, camRectSize, UIElements.MenuSectionBGBorderColor);

							Rect leftColumnRect = new Rect(rect.x, rect.y, rect.width - camRectSize - 1, camRectSize).ContractedBy(5);
							lister.columnGap = 0;
							lister.Begin(leftColumnRect, 1);
							{
								lister.Header("Settings", anchor: TextAnchor.MiddleCenter);

								CameraView.OrthographicSize = lister.SliderLabeled("Orthographic Size", CameraView.OrthographicSize, "Camera Zoom", string.Empty, string.Empty, ZoomRange.min, ZoomRange.max, decimalPlaces: 1);
								if (lister.Button($"Playback Speed: {AnimationManager.PlaybackSpeed}", highlightTag: "Speed multiplier on animation window"))
								{
									List<FloatMenuOption> options = new List<FloatMenuOption>();

									foreach (float speed in AnimationManager.playbackSpeeds)
									{
										options.Add(new FloatMenuOption($"{speed}", () => AnimationManager.PlaybackSpeed = speed));
									}

									Find.WindowStack.Add(new FloatMenu(options));
								}

								lister.Gap(4);

								//lister.CheckboxLabeled("Pause Transition", ref animationSettings.pauseOnTransition, "Pause the viewer when transitioning between animation curves.", string.Empty, false);
								lister.CheckboxLabeled("Loop", ref CameraView.animationSettings.loop, "Loop the viewer when reaching the end of the animation.", string.Empty, false);
								lister.CheckboxLabeled("Display Ticks", ref CameraView.animationSettings.displayTicks, "Display the time remaining in ticks, rather than seconds.", string.Empty, false);
								lister.CheckboxLabeled("Draw Cell Grid", ref CameraView.animationSettings.drawCellGrid, "Draw lines along edges of the map's cells.", string.Empty, false);
								//lister.Button("Curve", )
							}
							lister.End();

							if (Mouse.IsOver(cameraRect))
							{
								CameraView.HandleZoom();

								Widgets.DrawTextureFitted(cameraRect, UIData.TransparentBlackBG, 1);

								float buttonSpacing = PlaybackRectHeight / 2;
								Rect playerBarRect = new Rect(cameraRect.x + buttonSpacing, cameraRect.yMax - PlaybackRectHeight - 5, PlaybackRectHeight, PlaybackRectHeight).ContractedBy(2);
								Texture2D pauseTex = AnimationManager.Paused ? CameraView.playTexture : CameraView.pauseTexture;
								if (Widgets.ButtonImage(playerBarRect, pauseTex))
								{
									if (AnimationManager.CurrentDriver != null && AnimationManager.TicksPassed >= AnimationManager.CurrentDriver.AnimationLength)
									{
										AnimationManager.Reset();
									}
									AnimationManager.TogglePause(true);
									SoundDefOf.Click.PlayOneShotOnCamera();
								}
								Rect invisibleClickableWindowRect = new Rect(cameraRect.x, cameraRect.y, cameraRect.width, cameraRect.height - playerBarRect.height * 2.5f);
								float playButtonAnimatedSize = cameraRect.width / 5;
								Rect playButtonAnimated = new Rect(cameraRect.x, cameraRect.y, playButtonAnimatedSize, playButtonAnimatedSize);
								if (AnimationManager.ButtonUpdated(invisibleClickableWindowRect, () => timeFading += Time.deltaTime, () => ButtonFadeHandler(playButtonAnimated), () => timeFading <= 0, doMouseoverSound: false))
								{
									timeFading = 0;
									AnimationManager.TogglePause(true);
									SoundDefOf.Click.PlayOneShotOnCamera();
								}
								Text.Anchor = TextAnchor.MiddleCenter;
								Text.Font = GameFont.Small;

								string timeCount = CameraView.animationSettings.displayTicks ? "0 / 0" : "0:00/0:00";
								if (AnimationManager.CurrentDriver != null)
								{
									if (CameraView.animationSettings.displayTicks)
									{
										timeCount = $"{AnimationManager.TicksPassed} / {AnimationManager.CurrentDriver.AnimationLength}";
									}
									else
									{
										TimeSpan timePassed = new TimeSpan(0, 0, Mathf.CeilToInt(AnimationManager.TicksPassed.TicksToSeconds()));
										TimeSpan timeMax = new TimeSpan(0, 0, Mathf.CeilToInt(AnimationManager.CurrentDriver.AnimationLength.TicksToSeconds()));
										timeCount = $"{timePassed:m\\:ss} / {timeMax:m\\:ss}";
									}
								}

								Rect timeLeftRect = new Rect(cameraRect.xMax - cameraRect.width / 2 - 10, playerBarRect.y, cameraRect.width / 2, playerBarRect.height);
								Text.Anchor = TextAnchor.MiddleRight;
								Widgets.Label(timeLeftRect, timeCount);
								GUIState.Reset();

								Rect progressBarRect = new Rect(cameraRect.x + 8, playerBarRect.y - PlaybackBarHeight - PlaybackRectHeight / 3, cameraRect.width - 16, PlaybackBarHeight);
								float handleSize = PlaybackHandleSize;
								if (Mouse.IsOver(progressBarRect.ExpandedBy(2)))
								{
									progressBarRect.height *= 1.5f;
									handleSize *= 1.5f;
									progressBarRect.y -= (progressBarRect.height - PlaybackBarHeight) / 2;
								}
								float viewerPercent = 0;
								if (AnimationManager.CurrentDriver != null)
								{
									viewerPercent = (float)AnimationManager.TicksPassed / AnimationManager.CurrentDriver.AnimationLength;
								}
								UIElements.FillableBar(progressBarRect, viewerPercent, UIData.FillableBarProgressBar, UIData.FillableBarProgressBarBG);

								Rect progressBarHandleRect = new Rect(progressBarRect.x + (progressBarRect.width * viewerPercent) - handleSize / 2, progressBarRect.y + progressBarRect.height / 2 - handleSize / 2, handleSize, handleSize);
								GUI.color = UIData.ProgressBarRed;
								Widgets.DrawTextureFitted(progressBarHandleRect, CameraView.dragHandleIcon, 1);
								GUIState.Reset();

								Widgets.DraggableResult result = Widgets.ButtonInvisibleDraggable(progressBarRect);
								if (result == Widgets.DraggableResult.Dragged && AnimationManager.CurrentDriver != null)
								{
									dragging = true;
									AnimationManager.EditingTicks = true;
								}
								if (!Input.GetMouseButton(0))
								{
									dragging = false;
									AnimationManager.EditingTicks = false;
								}
								if ((dragging || result == Widgets.DraggableResult.Pressed) && AnimationManager.CurrentDriver != null)
								{
									float percentDrag = Mathf.Clamp01((Event.current.mousePosition.x - progressBarRect.x) / progressBarRect.width);
									AnimationManager.TicksPassed = Mathf.RoundToInt(AnimationManager.CurrentDriver.AnimationLength * percentDrag);
								}
							}
						}
						GUIState.Pop();
					}
				}
				catch (Exception ex)
				{
					disabled = true;
					darkenWindow = true;
					previewWindowString = ex.ToString();
					Log.ErrorOnce($"Exception thrown in CameraView. Exception = {ex}", "CameraView_GraphEditor".GetHashCode());
				}
			}
			if (disabled)
			{
				UIElements.Header(rect, previewWindowString, darkenWindow ? new Color(0, 0, 0, 0.75f) : ListingExtension.BannerColor, anchor: TextAnchor.MiddleCenter);
			}
		}

		private void ButtonFadeHandler(Rect rect)
		{
			GUIState.Push();
			{
				float size = rect.width;
				float percentFaded = timeFading / PlayButtonFadeTime;
				float expanded = Mathf.Lerp(size, size * 2, percentFaded);
				rect.size = new Vector2(expanded, expanded);
				float alpha = Mathf.Lerp(0.5f, 0, percentFaded);
				GUI.color = new Color(0, 0, 0, alpha);
				Widgets.DrawTextureFitted(rect, CameraView.dragHandleIcon, 1);
				GUI.color = new Color(1, 1, 1, alpha);
				Widgets.DrawTextureFitted(rect, CameraView.pauseTexture, 1);
			}
			GUIState.Pop();
		}

		private void DoResizerButton(Rect winRect)
		{
			Vector2 mousePosition = Event.current.mousePosition;
			Rect rect = new Rect(winRect.width - ResizerBtnSize, winRect.height - ResizerBtnSize, ResizerBtnSize, ResizerBtnSize);
			if (Event.current.type == EventType.MouseDown && Mouse.IsOver(rect))
			{
				resizing = true;
				startingWindowRect = new Rect(mousePosition.x, mousePosition.y, winRect.width, winRect.height);
			}
			if (resizing)
			{
				float diffX = (mousePosition.x - startingWindowRect.x);
				float diffY = (mousePosition.y - startingWindowRect.y);

				float maxSize = Mathf.Max(diffX, diffY);

				winRect.width = startingWindowRect.width + maxSize;
				winRect.height = startingWindowRect.height + maxSize;

				if (winRect.width < MinWidth || winRect.height < LeftWindowMinHeight)
				{
					winRect.width = MinWidth;
					winRect.height = LeftWindowMinHeight;
				}
				if (winRect.xMax > UI.screenWidth || winRect.yMax > UI.screenHeight)
				{
					return;
				}
				
				if (Event.current.type == EventType.MouseUp)
				{
					resizing = false;
				}
				if (winRect != resizeLaterRect)
				{
					needsResizing = true;
					resizeLaterRect = winRect;
				}
			}
			Widgets.ButtonImage(rect, TexUI.WinExpandWidget);
		}
	}
}
