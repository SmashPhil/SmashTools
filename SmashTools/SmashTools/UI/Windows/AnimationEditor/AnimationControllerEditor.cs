using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Noise;
using Verse.Sound;
using static UnityEngine.Random;
using StateType = SmashTools.Animations.AnimationState.StateType;

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
		private const float HighlightPadding = 4;

		private const int StateWidth = 9;
		private const int StateHeight = 3;

		private const float ZoomRate = 0.03f;

		private readonly Color lineDarkColor = new ColorInt(25, 25, 25).ToColor;
		private readonly Color lineLightColor = new ColorInt(35, 35, 35).ToColor;

		private readonly Color topBarFadeColor = new ColorInt(40, 40, 40).ToColor;
		private readonly Color entryStateColor = new ColorInt(20, 110, 50).ToColor;
		private readonly Color defaultStateColor = new ColorInt(185, 105, 25).ToColor;
		private readonly Color exitStateColor = new ColorInt(150, 25, 25).ToColor;
		private readonly Color anyStateColor = new ColorInt(90, 160, 140).ToColor;
		private readonly Color stateColor = new ColorInt(75, 75, 75).ToColor;

		private readonly Selector selector = new Selector();
		private readonly Clipboard clipboard = new Clipboard();

		private readonly Vector2 gridSize = new Vector2(GridSize * GridSquareSize, GridSize * GridSquareSize);

		private float leftWindowSize = MinLeftWindowSize;
		private bool hideLeftWindow = false;
		private bool initialized = false;

		private float zoom = 1;
		private Vector2 scrollPos;
		private Vector2 dragPos;

		private DragItem dragging;
		private LeftSection leftSectionTab = LeftSection.Layers;

		private IntVec2 creatingAt;
		private AnimationState makingTransitionFrom;

		public AnimationControllerEditor(Dialog_AnimationEditor parent) : base(parent)
		{
			scrollPos = new Vector2(gridSize.x / 2f, gridSize.y / 2f);
		}

		private bool UnsavedChanges { get; set; }

		private float MaxZoom(Rect rect) => gridSize.y / rect.size.y;

		public override void OnTabOpen()
		{
			base.OnTabOpen();
		}

		public override void ResetToCenter()
		{
			base.ResetToCenter();
			initialized = false;
			zoom = 1;
		}

		private void ChangeMade()
		{
			parent.ChangeMade();
			UnsavedChanges = true;
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

				if (!initialized)
				{
					//Start at center of grid
					SetScrollPosNormalized(editorRect, ref scrollPos, viewRect, new Vector2(0.5f, 0.5f));
					initialized = true;
				}

				Rect visibleRect = GetVisibleRect(editorRect, scrollPos, viewRect);
				Widgets.BeginScrollView(editorRect, ref scrollPos, viewRect, showScrollbars: false);
				{
					DrawBackgroundDark(viewRect);

					Rect blendRect = new Rect(topBarRect.x, topBarRect.yMax, topBarRect.width, WidgetBarHeight);
					DrawBlend(blendRect, topBarFadeColor, backgroundCurvesColor);

					DrawGrid(viewRect);

					Rect dragRect = DragRect(rect.position - visibleRect.position);
					DrawAnimationStates(editorRect, viewRect, dragRect);

					if (Input.GetMouseButtonDown(1) && Mouse.IsOver(viewRect))
					{
						Vector2 mousePos = MouseUIPos(rect.position);
						creatingAt = GridPosition(editorRect, viewRect, mousePos);
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
					
					if (SelectionBox(rect.position, visibleRect, viewRect, out dragRect))
					{

					}
				}
				EndScrollViewNoScrollbarControls();
			}
			Widgets.EndGroup();

			if (Mouse.IsOver(rect) && Event.current.type == EventType.ScrollWheel)
			{
				float value = Event.current.delta.y * ZoomRate;
				zoom = Mathf.Clamp(zoom + value, 1, MaxZoom(rect));
				Event.current.Use();
			}

			if (!draggingSelectionBox && DragWindow(rect, DragItem.Grid, button: 2))
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

		private void DrawGrid(Rect viewRect)
		{
			int i = 0;
			for (float x = 0; x < viewRect.width; x += GridSquareSize / zoom)
			{
				UIElements.DrawLineVertical(viewRect.x + x, viewRect.y, viewRect.height, GetColor(i));
				i++;
			}
			i = 1;
			for (float y = 0; y < viewRect.height; y += GridSquareSize / zoom)
			{
				UIElements.DrawLineHorizontal(viewRect.x, viewRect.y + y, viewRect.width, GetColor(i));
				i++;
			}

			Color GetColor(int lineNumber)
			{
				return lineNumber % 10 == 0 ? lineDarkColor : lineLightColor;
			}
		}

		private void DrawAnimationStates(Rect outRect, Rect viewRect, in Rect dragRect)
		{
			if (parent.controller && parent.animLayer != null && !parent.animLayer.states.NullOrEmpty())
			{
				bool leftClick = Input.GetMouseButtonDown(0);
				bool rightClick = Input.GetMouseButtonDown(1);
				bool selected = false;
				foreach (AnimationState state in parent.animLayer.states)
				{
					Vector2 size = new Vector2(StateWidth * GridSquareSize / zoom, StateHeight * GridSquareSize / zoom);
					Vector2 position = StatePosition(viewRect, state.position);
					Rect stateRect = new Rect(position, size);

					if (selector.IsSelected(state))
					{
						Rect highlightRect = stateRect.ExpandedBy(HighlightPadding);
						Widgets.DrawStrongHighlight(highlightRect);
					}
					
					Color color = GetStateColor(state);
					Widgets.DrawBoxSolid(stateRect, color);
					Text.Anchor = TextAnchor.MiddleCenter;
					Text.Font = GameFont.Small;
					Widgets.Label(stateRect, /*state.name*/stateRect.ToString());

					if ((leftClick || rightClick) && Mouse.IsOver(stateRect))
					{
						selector.Select(state, clear: !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.LeftControl));
						selected = true;

						if (rightClick)
						{
							List<FloatMenuOption> options = new List<FloatMenuOption>();
							FloatMenuOption makeTransitionOption = new FloatMenuOption("ST_MakeTransition".Translate(), CreateNewState);
							options.Add(makeTransitionOption);
							if (state.Type != StateType.Entry && state.Type != StateType.Exit)
							{
								FloatMenuOption newSubStateOption = new FloatMenuOption("ST_SetStateDefault".Translate(), CreateNewState);
								newSubStateOption.Disabled = state.Type == StateType.Default;
								options.Add(newSubStateOption);

								FloatMenuOption copyOption = new FloatMenuOption("ST_Copy".Translate(), CopySelectedStates);
								options.Add(copyOption);

								FloatMenuOption deleteOption = new FloatMenuOption("ST_Delete".Translate(), CreateNewState);
								options.Add(deleteOption);
							}
							Find.WindowStack.Add(new FloatMenu(options));
						}
					}
					else if (draggingSelectionBox)
					{
						if (dragRect.Overlaps(stateRect, true))
						{
							selector.Select(state, clear: false);
							selected = true;
						}
						else
						{
							selector.Unselect(state);
						}
					}
				}
				if (leftClick && Mouse.IsOver(viewRect) && !selected)
				{
					selector.ClearSelectedStates();
				}
			}
		}

		private Vector2 StatePosition(Rect viewRect, IntVec2 gridPos)
		{
			float xT = (gridPos.x + GridSize / 2f) / GridSize;
			float yT = (-gridPos.z + GridSize / 2f) / GridSize;

			float rectX = xT * viewRect.width;
			float rectY = yT * viewRect.height;
			return new Vector2(rectX, rectY);
		}

		private IntVec2 GridPosition(Rect outRect, Rect viewRect, Vector2 pos)
		{
			Rect visibleRect = GetVisibleRect(outRect, scrollPos, viewRect);
			Vector2 finalPos = visibleRect.position + pos;
			Vector2 posT = finalPos / viewRect.size; 
			int gridX = Mathf.RoundToInt(Mathf.Lerp(-GridSize / 2, GridSize / 2, posT.x));
			int gridY = Mathf.RoundToInt(Mathf.Lerp(-GridSize / 2, GridSize / 2, posT.y));
			return new IntVec2(gridX, -gridY); //Invert Y - UI is top to bottom | Grid is bottom to top
		}

		private Color GetStateColor(AnimationState state)
		{
			return state.Type switch
			{
				StateType.None => stateColor,
				StateType.Entry => entryStateColor,
				StateType.Default => defaultStateColor,
				StateType.Exit => exitStateColor,
				StateType.AnyState => anyStateColor,
				_ => stateColor
			};
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

		private void StartMakingTransition(AnimationState from)
		{
			makingTransitionFrom = from;
		}

		private void SetTransition(AnimationState to)
		{
			makingTransitionFrom.AddTransition(to);
		}

		private void StopMakingTransition()
		{
			makingTransitionFrom = null;
		}

		private void DeleteSelection()
		{
			if (selector.AnySelected)
			{

			}
		}

		private void CreateNewState()
		{
			string name = AnimationLoader.GetAvailableName(parent.animLayer.states.Select(state => state.name), "New State");
			parent.animLayer.AddState(name, creatingAt);
		}

		private void CopySelectedStates()
		{
			if (!selector.AnyStatesSelected)
			{
				return;
			}

			clipboard.CopyToClipboard(selector.SelectedStates);
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

		private class Clipboard
		{
			private HashSet<AnimationState> copiedStates = new HashSet<AnimationState>();

			public void CopyToClipboard(IEnumerable<AnimationState> states)
			{
				ClearClipboard();
				copiedStates.AddRange(states);
			}

			public void ClearClipboard()
			{
				copiedStates.Clear();
			}
		}

		private class Selector
		{
			private HashSet<AnimationState> selectedStates = new HashSet<AnimationState>();
			private HashSet<AnimationTransition> selectedTransitions = new HashSet<AnimationTransition>();

			public bool AnySelected => AnyStatesSelected || AnyTransitionsSelected;

			public bool AnyStatesSelected => selectedStates.Any();

			public bool AnyTransitionsSelected => selectedTransitions.Any();

			public HashSet<AnimationState> SelectedStates => selectedStates;

			public bool IsSelected(AnimationState state)
			{
				return selectedStates.Contains(state);
			}

			public bool IsSelected(AnimationTransition transition)
			{
				return selectedTransitions.Contains(transition);
			}

			public void Select(AnimationState state, bool clear = true)
			{
				if (clear)
				{
					ClearSelectedStates();
				}
				selectedStates.Add(state);
			}

			public void Unselect(AnimationState state)
			{
				selectedStates.Remove(state);
			}

			public void ClearSelectedStates()
			{
				selectedStates.Clear();
			}

			public void ClearSelectedTransitions()
			{
				selectedTransitions.Clear();
			}
		}
	}
}
