using RimWorld;
using System;
using System.Collections.Generic;
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
		private const float MinLeftWindowSize = 150;
		private const float MinRightWindowSize = 300;
		private const int GridSquareSize = 20;

		private float leftWindowSize = MinLeftWindowSize;
		private bool hideLeftWindow = true;

		private float zoom = 1;
		private Vector2 scrollPos;
		private Vector2 dragPos;

		private DragItem dragging;

		private readonly Vector2 gridSize = new Vector2(2000, 2000);

		public AnimationControllerEditor(Dialog_AnimationEditor parent) : base(parent)
		{
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

			DoResizerButton(rect, ref leftWindowSize, MinLeftWindowSize, MinRightWindowSize);
		}

		private void DrawControllerSectionRight(Rect rect)
		{
			Rect viewRect = new Rect(rect.x, rect.y, gridSize.x, gridSize.y);
			Widgets.BeginScrollView(rect, ref scrollPos, viewRect, showScrollbars: false);
			{
				DrawBackgroundDark(viewRect);

				for (int x = 0; x < viewRect.width; x += GridSquareSize)
				{
					UIElements.DrawLineVertical(viewRect.x + x, viewRect.y, viewRect.height, Color.black);
				}
				for (int y = 0; y < viewRect.height; y += GridSquareSize)
				{
					UIElements.DrawLineHorizontal(viewRect.x, viewRect.y + y, viewRect.width, Color.black);
				}
			}
			Widgets.EndScrollView();

			if (Mouse.IsOver(rect) && Event.current.type == EventType.ScrollWheel)
			{
				float value = Event.current.delta.y * zoom;
				zoom += value;
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

		private enum DragItem
		{
			None,
			Grid,
		}
	}
}
