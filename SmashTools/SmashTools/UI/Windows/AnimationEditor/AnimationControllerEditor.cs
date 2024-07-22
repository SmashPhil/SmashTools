using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace SmashTools.Animations
{
	public class AnimationControllerEditor : AnimationEditor
	{
		private const float MinLeftWindowSize = TabWidth * 2 + TopBarIconSize;
		private const float MinRightWindowSize = 300;
		private const int GridSize = 100;

		private const float WidgetBarHeight = 24;
		private const float TabWidth = 110;
		private const int GridSquareSize = 20;
		private const float TopBarIconSize = WidgetBarHeight;

		private const float ZoomRate = 0.01f;
		private const float MaxZoom = 4;

		private static readonly Color topBarFadeColor = new ColorInt(40, 40, 40).ToColor;

		private float leftWindowSize = MinLeftWindowSize;
		private bool hideLeftWindow = false;

		private float zoom = 1;
		private Vector2 scrollPos = new Vector2(0.5f, 0.5f);
		private Vector2 dragPos;

		private DragItem dragging;
		private LeftSection leftSectionTab = LeftSection.Layers;

		private readonly Vector2 gridSize = new Vector2(GridSize * GridSquareSize, GridSize * GridSquareSize);

		private IntVec2 creatingAt;

		public AnimationControllerEditor(Dialog_AnimationEditor parent) : base(parent)
		{
		}

		public override void OnTabOpen()
		{
			base.OnTabOpen();
		}

		public override void Draw(Rect rect)
		{
			if (hideLeftWindow)
			{
				DrawControllerSectionRight(rect);
			}
			else
			{
				rect.SplitVertically(leftWindowSize, out Rect leftRect, out Rect rightRect);

				DrawControllerSectionLeft(leftRect);
				DrawControllerSectionRight(rightRect);
			}
		}

		private void DrawControllerSectionLeft(Rect rect)
		{
			DrawBackground(rect);

			Rect tabRect = new Rect(rect.x, rect.y, TabWidth, WidgetBarHeight);
			DoSeparatorHorizontal(rect.x, tabRect.y, TabWidth);
			if (ToggleText(tabRect, "ST_Layers".Translate(), null, leftSectionTab == LeftSection.Layers))
			{
				FlipTab();
			}
			tabRect.x += tabRect.width;
			if (ToggleText(tabRect, "ST_Parameters".Translate(), null, leftSectionTab == LeftSection.Parameters))
			{
				FlipTab();
			}
			DoSeparatorHorizontal(rect.x, tabRect.y, tabRect.width);

			rect.yMin += tabRect.height;

			DoResizerButton(rect, ref leftWindowSize, MinLeftWindowSize, MinRightWindowSize);

			void FlipTab()
			{
				leftSectionTab = leftSectionTab switch
				{
					LeftSection.Layers => LeftSection.Parameters,
					LeftSection.Parameters => LeftSection.Layers,
					_ => throw new NotImplementedException(),
				};
			}
		}

		private void DrawControllerSectionRight(Rect rect)
		{
			Widgets.BeginGroup(rect);
			{
				Rect editorRect = rect.AtZero();
				Rect topBarRect = new Rect(editorRect.x, editorRect.y, editorRect.width, WidgetBarHeight);
				DrawBackground(topBarRect);
				editorRect.yMin += topBarRect.height;

				Rect viewRect = new Rect(editorRect.x, editorRect.y, gridSize.x, gridSize.y);
				Widgets.BeginScrollView(editorRect, ref scrollPos, viewRect, showScrollbars: false);
				{
					DrawBackgroundDark(viewRect);

					Rect blendRect = new Rect(topBarRect.x, topBarRect.yMax, topBarRect.width, WidgetBarHeight);
					DrawBlend(blendRect, topBarFadeColor, backgroundCurvesColor);

					for (float x = 0; x < viewRect.width; x += GridSquareSize * zoom)
					{
						UIElements.DrawLineVertical(viewRect.x + x, viewRect.y, viewRect.height, Color.black);
					}
					for (float y = 0; y < viewRect.height; y += GridSquareSize * zoom)
					{
						UIElements.DrawLineHorizontal(viewRect.x, viewRect.y + y, viewRect.width, Color.black);
					}

					if (Mouse.IsOver(editorRect) && Input.GetMouseButtonDown(1))
					{
						creatingAt = new IntVec2(MouseUIPos(rect.position));
						FloatMenuOption newStateOption = new FloatMenuOption("ST_CreateState".Translate(), CreateNewState);
						FloatMenuOption newSubStateOption = new FloatMenuOption("ST_CreateSubState".Translate(), CreateNewState);
						newSubStateOption.Disabled = true;
						FloatMenuOption pasteOption = new FloatMenuOption("ST_Paste".Translate(), CreateNewState);
						pasteOption.Disabled = true;

						List<FloatMenuOption> options = new List<FloatMenuOption>()
						{
							newStateOption,
							newSubStateOption,
							pasteOption
						};
						Find.WindowStack.Add(new FloatMenu(options));
					}

					if (SelectionBox(rect.position, viewRect, out Rect dragRect))
					{

					}
				}
				EndScrollViewNoScrollbarControls();
			}
			Widgets.EndGroup();

			if (Mouse.IsOver(rect) && Event.current.type == EventType.ScrollWheel)
			{
				float value = Event.current.delta.y * ZoomRate;
				zoom = Mathf.Clamp(zoom - value, 1, MaxZoom);
				Event.current.Use();
			}

			if (DragWindow(rect, DragItem.Grid, button: 2))
			{
				SetDragPos();
			}

			void SetDragPos()
			{
				Vector2 mousePos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
				Vector2 mouseDiff = dragPos - mousePos;
				dragPos = mousePos;
				scrollPos += new Vector2(mouseDiff.x, -mouseDiff.y);
			}
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

		private void LoadController(FileInfo fileInfo)
		{
			AnimationController controller = AnimationLoader.LoadFile<AnimationController>(fileInfo.FullName);
			parent.controller = controller;
			if (!controller)
			{
				Messages.Message($"Unable to load animation file at {fileInfo.FullName}.", MessageTypeDefOf.RejectInput);
			}
		}

		private void CreateNewState()
		{

		}

		private enum DragItem
		{
			None,
			Grid,
		}

		private enum LeftSection
		{
			Layers,
			Parameters
		}
	}
}
