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
using StateType = SmashTools.Animations.AnimationState.StateType;

namespace SmashTools.Animations
{
	public class AnimationControllerEditor : AnimationEditor
	{
		private const float MinLeftWindowSize = TabWidth * 2 + TopBarIconSize;
		private const float MinRightWindowSize = 300;
		private const int GridSize = 150;

		private const float WidgetBarHeight = 24;
		private const float LayerItemHeight = 38;
		private const float LayerInputWidth = 75;
		private const float TransitionLineWidth = 2;
		private const float TransitionAnchorSpacing = 5;
		private const float TabWidth = 110;
		public const int GridSquareSize = 20;
		private const float TopBarIconSize = WidgetBarHeight;
		private const float HighlightPadding = 2;

		public const int StateWidth = 16;
		private const int StateHeight = 4;
		private const int StateHeightSmall = 3;

		private const float ZoomRate = 0.03f;

		private readonly Texture2D stateTex = ContentFinder<Texture2D>.Get("SmashTools/Square");
		private readonly Texture2D stateMachineTex = ContentFinder<Texture2D>.Get("SmashTools/Hexagon");
		private readonly Texture2D eyeTex = ContentFinder<Texture2D>.Get("SmashTools/Eye");
		private readonly Texture2D eyeStrikedTex = ContentFinder<Texture2D>.Get("SmashTools/EyeStriked");

		private readonly Color lineDarkColor = new ColorInt(25, 25, 25).ToColor;
		private readonly Color lineLightColor = new ColorInt(35, 35, 35).ToColor;

		private readonly Color topBarFadeColor = new ColorInt(40, 40, 40).ToColor;
		private readonly Color entryStateColor = new ColorInt(20, 110, 50).ToColor;
		private readonly Color defaultStateColor = new ColorInt(185, 105, 25).ToColor;
		private readonly Color exitStateColor = new ColorInt(150, 25, 25).ToColor;
		private readonly Color anyStateColor = new ColorInt(90, 160, 140).ToColor;
		private readonly Color stateColor = new ColorInt(75, 75, 75).ToColor;

		private readonly Color highlightColor = Widgets.HighlightStrongBgColor.ToTransparent(0.75f);

		private readonly Selector selector = new Selector();
		private readonly Clipboard clipboard = new Clipboard();

		private readonly Vector2 gridSize = new Vector2(GridSize * GridSquareSize, GridSize * GridSquareSize);

		private float leftWindowSize = MinLeftWindowSize;
		private bool hideLeftWindow = false;
		private bool initialized = false;

		private float zoom = 2;
		private Vector2 scrollPos;
		private Vector2 dragPos;

		private DragItem dragging;
		private LeftSection leftSectionTab = LeftSection.Layers;

		private IntVec2 mouseGridPos;
		private IntVec2 clickedGridPos;

		private IntVec2 draggingStateOrigPos;
		private bool draggingState;
		private AnimationState makingTransitionFrom;
		
		public AnimationControllerEditor(Dialog_AnimationEditor parent) : base(parent)
		{
			scrollPos = new Vector2(gridSize.x / 2f, gridSize.y / 2f);
		}

		private bool UnsavedChanges { get; set; }

		private AnimationLayer EditingLayer { get; set; }

		private GameFont Font
		{
			get
			{
				if (zoom < 1.5f) return GameFont.Medium;
				if (zoom < 2) return GameFont.Small;
				return GameFont.Tiny;
			}
		}

		private float MaxZoom(Rect rect) => gridSize.y / rect.size.y;

		public override void OnTabOpen()
		{
			base.OnTabOpen();
		}

		public override void ResetToCenter()
		{
			base.ResetToCenter();
			initialized = false;
			zoom = 2;
		}

		private void ChangeMade()
		{
			parent.ChangeMade();
			UnsavedChanges = true;
		}

		public override void Save()
		{
			if (parent.controller)
			{
				AnimationLoader.Save(parent.controller);
			}
		}

		public override void CopyToClipboard()
		{
			if (selector.AnyStatesSelected)
			{
				CopySelectedStates();
			}
		}

		public override void Paste()
		{
			if (!clipboard.IsEmpty)
			{
				PasteState();
			}
		}

		public override void Delete()
		{
			if (selector.AnySelected)
			{
				DeleteSelection();
			}
		}

		public override void Escape()
		{
			StopMakingTransition();
		}


		public override void Draw(Rect rect)
		{
			if (parent.animLayer == null)
			{
				parent.animLayer = parent.controller.layers.FirstOrDefault();
				Debug.Assert(parent.animLayer != null, "No layers found");
			}

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

		#region Left Section
		private void DrawControllerSectionLeft(Rect rect)
		{
			DrawBackground(rect);

			Rect tabRect = new Rect(rect.x, rect.y, TabWidth, WidgetBarHeight);
			if (ToggleText(tabRect, "ST_Layers".Translate(), null, leftSectionTab == LeftSection.Layers))
			{
				leftSectionTab = LeftSection.Layers;
			}
			tabRect.x += tabRect.width;
			if (ToggleText(tabRect, "ST_Parameters".Translate(), null, leftSectionTab == LeftSection.Parameters))
			{
				leftSectionTab = LeftSection.Parameters;
			}

			Rect toggleVisibilityRect = new Rect(rect.xMax - WidgetBarHeight, rect.y, WidgetBarHeight, WidgetBarHeight).ContractedBy(2);
			if (!hideLeftWindow && Widgets.ButtonImage(toggleVisibilityRect, eyeTex))
			{
				hideLeftWindow = true;
			}

			DoSeparatorHorizontal(rect.x, rect.y, rect.width);
			DoSeparatorHorizontal(rect.x, tabRect.yMax, rect.width);

			switch (leftSectionTab)
			{
				case LeftSection.Layers:
					DrawLayersTab(rect);
					break;
				case LeftSection.Parameters:
					DrawParametersTab(rect);
					break;
			}

			rect.yMin += tabRect.height;

			DoResizerButton(rect, ref leftWindowSize, MinLeftWindowSize, MinRightWindowSize);
		}

		private void DrawLayersTab(Rect rect)
		{
			Rect buttonBarRect = new Rect(rect.x, rect.y + WidgetBarHeight, rect.width, WidgetBarHeight);
			Rect addLayerBtnRect = new Rect(buttonBarRect.xMax - WidgetBarHeight - 5, buttonBarRect.y, WidgetBarHeight, WidgetBarHeight);
			if (Widgets.ButtonImage(addLayerBtnRect.ContractedBy(2), TexButton.Plus))
			{
				parent.controller.AddLayer("New Layer");
			}
			DoSeparatorHorizontal(rect.x, buttonBarRect.yMax, rect.width);

			Rect paddingRect = new Rect(rect.x, buttonBarRect.yMax, rect.width, 5).ContractedBy(1);
			Widgets.DrawBoxSolid(paddingRect, backgroundDopesheetColor.Subtract255NoAlpha(5, 5, 5));

			Rect layerRect = new Rect(rect.x, buttonBarRect.yMax, rect.width, LayerItemHeight);
			foreach (AnimationLayer layer in parent.controller.layers)
			{
				if (parent.animLayer == layer)
				{
					Widgets.DrawBoxSolid(layerRect, backgroundDopesheetColor.Add255NoAlpha(10, 10, 10));
				}
				Rect dragHandleRect = new Rect(layerRect.x, layerRect.y, layerRect.height, layerRect.height);
				Rect entryRect = new Rect(dragHandleRect.xMax, layerRect.y, LayerInputWidth, layerRect.height);
				Rect labelRect = new Rect(dragHandleRect.xMax, layerRect.y, layerRect.width - dragHandleRect.width - entryRect.width, layerRect.height);

				Text.Anchor = TextAnchor.MiddleLeft;
				if (EditingLayer == layer)
				{
					layer.name = Widgets.TextField(labelRect, layer.name);
					// Clicking anywhere outside of the text field control will confirm edit
					if (Input.GetMouseButtonDown(0) && !Mouse.IsOver(labelRect))
					{
						ConfirmLayerEdit();
					}
					// Return keys confirm edit
					if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
					{
						ConfirmLayerEdit();
					}
				}
				else
				{
					Widgets.Label(labelRect, layer.name);
					if (Widgets.ButtonInvisible(labelRect))
					{
						// Double clicking layer (even late) will begin name edit
						if (parent.animLayer == layer) 
						{
							EditingLayer = layer;
						}
						parent.animLayer = layer;
					}
				}


				DoSeparatorHorizontal(layerRect.x, layerRect.yMax, layerRect.width);
				layerRect.y += LayerItemHeight;
			}
		}

		private void ConfirmLayerEdit()
		{
			EditingLayer.name = AnimationLoader.GetAvailableName(parent.controller.layers.Where(layer => layer != EditingLayer)
																						 .Select(layer => layer.name), EditingLayer.name);
			EditingLayer = null;
		}

		private void DrawParametersTab(Rect rect)
		{
			Rect buttonBarRect = new Rect(rect.x, rect.y + WidgetBarHeight, rect.width, WidgetBarHeight);
			Rect addParameterBtnRect = new Rect(buttonBarRect.xMax - WidgetBarHeight - 5, rect.y, WidgetBarHeight, WidgetBarHeight);

			GUI.DrawTexture(addParameterBtnRect.ContractedBy(5), TexButton.Reveal);
			GUI.DrawTexture(addParameterBtnRect.ContractedBy(2), TexButton.Plus);
			if (Widgets.ButtonInvisible(addParameterBtnRect))
			{
				// TODO - Add Parameter via dropdown
			}

			DoSeparatorHorizontal(rect.x, buttonBarRect.yMax, rect.width);
		}

		#endregion Left Section


		#region Right Section

		private void DrawControllerSectionRight(Rect rect)
		{
			Widgets.BeginGroup(rect);
			Rect editorRect = rect.AtZero();
			Rect viewRect = new Rect(editorRect.x, editorRect.y, gridSize.x, gridSize.y);

			Vector2 mousePos = MouseUIPos(rect.position);
			mouseGridPos = GridPosition(editorRect, viewRect, mousePos);

			if (!initialized)
			{
				//Start at center of grid
				SetScrollPosNormalized(editorRect, ref scrollPos, viewRect, new Vector2(0.5f, 0.5f));
				initialized = true;
			}

			Rect visibleRect = GetVisibleRect(editorRect, scrollPos, viewRect);
			UIElements.BeginScrollView(editorRect, ref scrollPos, viewRect, showHorizontalScrollbar: false, showVerticalScrollbar: false);
			{
				DrawBackgroundDark(viewRect);

				Rect blendRect = new Rect(rect.x, rect.yMax, rect.width, WidgetBarHeight);
				DrawBlend(blendRect, topBarFadeColor, backgroundCurvesColor);

				DrawGrid(viewRect);

				Rect dragRect = DragRect(rect.position - visibleRect.position);
				DrawAnimationStates(editorRect, viewRect, dragRect);

				if (RightClick && Mouse.IsOver(viewRect))
				{
					clickedGridPos = mouseGridPos;
					FloatMenuOption newStateOption = new FloatMenuOption("ST_CreateState".Translate(), CreateNewState);
					FloatMenuOption newSubStateOption = new FloatMenuOption("ST_CreateSubState".Translate(), CreateNewState);
					newSubStateOption.Disabled = true;
					FloatMenuOption pasteOption = new FloatMenuOption("ST_Paste".Translate(), PasteState);
					pasteOption.Disabled = clipboard.IsEmpty;

					List<FloatMenuOption> options = new List<FloatMenuOption>()
					{
						newStateOption,
						newSubStateOption,
						pasteOption
					};
					Find.WindowStack.Add(new FloatMenu(options));
				}
					
				if (!draggingState)
				{
					SelectionBox(rect.position, visibleRect, viewRect, out _);
				}
			}
			UIElements.EndScrollView(false);
			Widgets.EndGroup();

			DrawTopBar(rect);

#if DEBUG
			//Text.Font = GameFont.Small;
			//Text.Anchor = TextAnchor.UpperLeft;

			//Rect debugTextRect = new Rect(rect.x, rect.y + WidgetBarHeight + 5, rect.width, 24);
			//Widgets.Label(debugTextRect, $"Zoom: {zoom:0.0}");
			//debugTextRect.y += debugTextRect.height;
			//Widgets.Label(debugTextRect, $"Grid: {mouseGridPos}");
#endif

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
				Vector2 mousePos = new Vector2(UI.MousePositionOnUI.x, UI.MousePositionOnUI.y);
				Vector2 mouseDiff = dragPos - mousePos;
				dragPos = mousePos;
				scrollPos += new Vector2(mouseDiff.x, -mouseDiff.y);
			}
		}

		private void DrawTopBar(Rect rect)
		{
			Rect topBarRect = new Rect(rect.x, rect.y, rect.width, WidgetBarHeight);
			DrawBackground(topBarRect);
			
			if (hideLeftWindow)
			{
				Rect toggleVisibilityRect = new Rect(rect.x, rect.y, WidgetBarHeight, WidgetBarHeight).ContractedBy(2);
				if (Widgets.ButtonImage(toggleVisibilityRect, eyeStrikedTex))
				{
					hideLeftWindow = false;
				}
			}
			else
			{
				// TODO - show layer heirarchy
			}
		}

		private void DrawGrid(Rect viewRect)
		{
			int increment = 1;// Mathf.FloorToInt((zoom + 3) % 3);
			float centerX = viewRect.x + viewRect.width / 2;
			float centerY = viewRect.y + viewRect.height / 2;

			UIElements.DrawLineVertical(centerX, viewRect.y, viewRect.height, GetColor(0));
			int lines = Mathf.RoundToInt(viewRect.width / (GridSquareSize / zoom) / 2);
			for (int i = 1; i < lines; i += increment)
			{
				UIElements.DrawLineVertical(centerX + GridSquareSize * i * (1f / zoom), viewRect.y, viewRect.height, GetColor(i));
				UIElements.DrawLineVertical(centerX - GridSquareSize * i * (1f / zoom), viewRect.y, viewRect.height, GetColor(i));
			}

			UIElements.DrawLineHorizontal(viewRect.x, centerY, viewRect.width, GetColor(0));
			lines = Mathf.RoundToInt(viewRect.height / (GridSquareSize / zoom) / 2);
			for (int i = 1; i < lines; i += increment)
			{
				UIElements.DrawLineHorizontal(viewRect.x, centerY + GridSquareSize * i * (1f / zoom), viewRect.width, GetColor(i));
				UIElements.DrawLineHorizontal(viewRect.x, centerY - GridSquareSize * i * (1f / zoom), viewRect.width, GetColor(i));
			}

			Color GetColor(int lineNumber)
			{
				return lineNumber % 10 == 0 ? lineDarkColor : lineLightColor;
			}
		}

		/// <returns>Animation state was right clicked</returns>
		private void DrawAnimationStates(Rect outRect, Rect viewRect, Rect dragRect)
		{
			if (!parent.controller || parent.animLayer == null || parent.animLayer.states.NullOrEmpty())
			{
				return;
			}

			Vector2 mousePos = StatePosition(viewRect, mouseGridPos);

			foreach (AnimationState state in parent.animLayer.states)
			{
				Vector2 sizeFrom = SizeFor(state.Type);
				Vector2 positionFrom = StatePosition(viewRect, state.position);
				//positionFrom.x -= TransitionAnchorSpacing;
				Rect stateRectFrom = new Rect(positionFrom, sizeFrom);
				if (Mouse.IsOver(stateRectFrom))
				{
					mousePos = stateRectFrom.center;
				}

				foreach (AnimationTransition transition in state.transitions)
				{
					Vector2 sizeTo = SizeFor(transition.ToState.Type);
					Vector2 positionTo = StatePosition(viewRect, transition.ToState.position);
					//positionTo.x += TransitionAnchorSpacing;
					Rect stateRectTo = new Rect(positionTo, sizeTo);

					Widgets.DrawLine(stateRectFrom.center, stateRectTo.center, Color.white, TransitionLineWidth);
					TransitionArrows(stateRectFrom.center, stateRectTo.center);
				}
			}

			if (makingTransitionFrom != null)
			{
				Vector2 size = SizeFor(makingTransitionFrom.Type);
				Vector2 position = StatePosition(viewRect, makingTransitionFrom.position);
				Rect stateRect = new Rect(position, size);

				Widgets.DrawLine(stateRect.center, mousePos, Color.white, TransitionLineWidth + 1);
				TransitionArrows(stateRect.center, mousePos);
			}

			bool mouseClick = LeftClick || RightClick;
			bool selected = false;
			foreach (AnimationState state in parent.animLayer.states)
			{
				Vector2 size = SizeFor(state.Type);
				Vector2 position = StatePosition(viewRect, state.position);
				Rect stateRect = new Rect(position, size);

				if (selector.IsSelected(state))
				{
					Rect highlightRect = stateRect.ExpandedBy(HighlightPadding);
					GUI.color = highlightColor;
					GUI.DrawTexture(highlightRect, stateTex);
				}

				GUI.color = GetStateColor(state);
				GUI.DrawTexture(stateRect, stateTex);
				GUI.color = Color.white;

				if (zoom < 5)
				{
					Text.Anchor = TextAnchor.MiddleCenter;
					Text.Font = Font;
					Widgets.Label(stateRect, state.name);
				}
				if (mouseClick && Mouse.IsOver(stateRect))
				{
					selector.Select(state, clear: !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.LeftControl));
					selected = true;

					clickedGridPos = mouseGridPos;

					if (LeftClick)
					{
						if (makingTransitionFrom != null)
						{
							ConfirmTransition(state);
						}
						draggingState = true;
						draggingStateOrigPos = state.position;
					}
					else if (RightClick)
					{
						draggingState = false;
						List<FloatMenuOption> options = new List<FloatMenuOption>();
						if (state.Type != StateType.Exit)
						{
							FloatMenuOption makeTransitionOption = new FloatMenuOption("ST_MakeTransition".Translate(), () => SetTransition(state));
							options.Add(makeTransitionOption);
						}
						if (state.Type != StateType.Entry && state.Type != StateType.Exit)
						{
							FloatMenuOption newSubStateOption = new FloatMenuOption("ST_SetStateDefault".Translate(), CreateNewState);
							newSubStateOption.Disabled = state.Type == StateType.Default;
							options.Add(newSubStateOption);

							FloatMenuOption copyOption = new FloatMenuOption("ST_Copy".Translate(), CopySelectedStates);
							options.Add(copyOption);

							FloatMenuOption deleteOption = new FloatMenuOption("ST_Delete".Translate(), () => DeleteState(state));
							options.Add(deleteOption);
						}
						Find.WindowStack.Add(new FloatMenu(options));
					}
					
					Event.current.Use();
				}
				else if (!draggingState && draggingSelectionBox)
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

			if (!Input.GetMouseButton(0))
			{
				draggingState = false;
			}
			if (mouseClick)
			{
				StopMakingTransition();
			}
			if (draggingState)
			{
				foreach (AnimationState state in selector.SelectedStates)
				{
					IntVec2 gridPosDiff = mouseGridPos - clickedGridPos;
					state.position = draggingStateOrigPos + gridPosDiff;
				}
			}
			// Any click that is non-selecting should clear selection
			if (LeftClick && Mouse.IsOver(viewRect) && !selected)
			{
				selector.ClearSelectedStates();
			}

			static void TransitionArrows(Vector2 from, Vector2 to) // TODO - enable multiple transitions
			{
				Vector2 point = Vector2.Lerp(from, to, 0.5f);
				float rotation = Vector2.Angle(from - to, Vector2.up) - 90;
				Widgets.DrawTextureRotated(point, TexButton.Play, rotation, scale: 0.65f);
			}
		}

		private Vector2 SizeFor(StateType stateType)
		{
			return stateType switch
			{
				StateType.None => new Vector2(StateWidth * GridSquareSize / zoom, StateHeight * GridSquareSize / zoom),
				StateType.Entry => new Vector2(StateWidth * GridSquareSize / zoom, StateHeightSmall * GridSquareSize / zoom),
				StateType.Default => new Vector2(StateWidth * GridSquareSize / zoom, StateHeight * GridSquareSize / zoom),
				StateType.Exit => new Vector2(StateWidth * GridSquareSize / zoom, StateHeightSmall * GridSquareSize / zoom),
				StateType.AnyState => new Vector2(StateWidth * GridSquareSize / zoom, StateHeight * GridSquareSize / zoom),
				_ => new Vector2(StateWidth * GridSquareSize / zoom, StateHeight * GridSquareSize / zoom),
			};
		}

		private Vector2 StatePosition(Rect viewRect, IntVec2 gridPos)
		{
			float rectX = gridPos.x * GridSquareSize / zoom + viewRect.width / 2;
			float rectY = -gridPos.z * GridSquareSize / zoom + viewRect.height / 2;
			return new Vector2(rectX, rectY);
		}

		private IntVec2 GridPosition(Rect outRect, Rect viewRect, Vector2 pos)
		{
			Rect visibleRect = GetVisibleRect(outRect, scrollPos, viewRect);
			Vector2 finalPos = visibleRect.position + pos;
			Vector2 posT = finalPos / viewRect.size; 
			int gridX = Mathf.RoundToInt(Mathf.Lerp(-GridSize / 2, GridSize / 2, posT.x) * zoom);
			int gridY = Mathf.RoundToInt(Mathf.Lerp(-GridSize / 2, GridSize / 2, posT.y) * zoom);
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

		#endregion Right Section

		#region Utils

		private void SetTransition(AnimationState from)
		{
			makingTransitionFrom = from;
		}

		private void ConfirmTransition(AnimationState target)
		{
			if (target.Type == StateType.Entry || target.Type == StateType.AnyState)
			{
				return;
			}
			makingTransitionFrom.AddTransition(target);
			StopMakingTransition();
		}

		private void StopMakingTransition()
		{
			makingTransitionFrom = null;
		}

		private void DeleteSelection()
		{
			if (selector.AnyTransitionsSelected)
			{
				foreach (AnimationTransition transition in selector.SelectedTransitions)
				{
					DeleteTransition(transition);
				}
			}
			if (selector.AnyStatesSelected)
			{
				foreach (AnimationState state in selector.SelectedStates)
				{
					DeleteState(state);
				}
			}
		}

		private void CreateNewState()
		{
			string name = AnimationLoader.GetAvailableName(parent.animLayer.states.Select(state => state.name), "New State");
			parent.animLayer.AddState(name, clickedGridPos);
		}

		private void PasteState()
		{
			foreach (AnimationState state in clipboard.GetCopiedStates())
			{
				AnimationState newState = state.CreateCopy(parent.animLayer);
				parent.animLayer.states.Add(newState);
			}
		}

		private void DeleteState(AnimationState state)
		{
			state.Dispose();
			parent.animLayer.states.Remove(state);
		}

		private void DeleteTransition(AnimationTransition transition)
		{
			foreach (AnimationState state in parent.animLayer.states)
			{
				state.transitions.Remove(transition);
			}
		}

		private void CopySelectedStates()
		{
			if (!selector.AnyStatesSelected)
			{
				return;
			}

			clipboard.CopyToClipboard(selector.SelectedStates);
		}

		#endregion Utils

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

			public bool IsEmpty => copiedStates.Count == 0;

			public void CopyToClipboard(IEnumerable<AnimationState> states)
			{
				ClearClipboard();
				copiedStates.AddRange(states);
			}

			public IEnumerable<AnimationState> GetCopiedStates()
			{
				return copiedStates;
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

			public HashSet<AnimationTransition> SelectedTransitions => selectedTransitions;

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
