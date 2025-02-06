using System.Collections;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace SmashTools.Performance
{
	public class Dialog_Profiler : Window, IHighPriorityOnGUI
	{
		private const float Width = 600;
		private const float Height = 900;

		private const float HeaderHeight = 30;
		private const float EntryHeight = 20;
		private const float IndentSpace = 12;

		private const float DefaultProfilerColumnWidth = 100;
		private const float SortTime = (ProfilerWatch.FramesPerCapture / 60f) * 2; // Sort every other capture
		private const float FrameBarSize = 6;

		private readonly Color separatorColor = new Color32(25, 25, 25, 255);
		private readonly Color headerColor = new Color32(38, 38, 38, 255);
		private readonly Color lineAlternateColorLight = new Color32(48, 48, 48, 255);
		private readonly Color lineAlternateColorDark = new Color32(44, 44, 44, 255);
		private readonly Color graphColor = new Color32(235, 235, 15, 255);
		private readonly Color framePauseColor = new Color32(255, 255, 255, 200);

		private float topPanelHeight = 200;
		private double graphCeiling = 100; //ms

		private int cachedEntryCount;
		private float panelHeight;
		private Vector2 scrollPos;

		private static ColumnSort sortBy = ColumnSort.TotalMS;

		private bool dragging = false;
		private Vector2 dragPos;

		private Stack<(int indent, ProfilerWatch.Block block)> stack = new Stack<(int indent, ProfilerWatch.Block block)>();

		private static Dictionary<ProfilerWatch.Block, bool> expandedBlocks = new Dictionary<ProfilerWatch.Block, bool>();

		private static List<ProfilerColumn> columns = new List<ProfilerColumn>()
		{
			new CallCountColumn(),
			new MaxMSColumn(),
			new TotalMSColumn(),
			//new SelfMSColumn(),
			new EndColumn(),
		};

		public Dialog_Profiler()
		{
			layer = WindowLayer.Super;
			this.doCloseX = false;
			this.closeOnAccept = false;
			this.closeOnCancel = true;
			this.closeOnClickedOutside = false;
			this.doCloseButton = false;
			this.focusWhenOpened = false;
			this.onlyOneOfTypeAllowed = true;
			this.resizeable = true;
			this.draggable = true;
			this.preventCameraMotion = false;
			this.doWindowBackground = false;
		}

		public override Vector2 InitialSize => new Vector2(Width, Height);

		protected override float Margin => 0;

		private int PausedAtFrame { get; set; } = -1;

		protected override void SetInitialSizeAndPosition()
		{
			windowRect = new Rect(50, 50, InitialSize.x, InitialSize.y).Rounded();
		}

		public override void PreOpen()
		{
			base.PreOpen();
			ProfilerWatch.StartLoggingResults();
		}

		public override void PostOpen()
		{
			base.PostOpen();
			CoroutineManager.StartCoroutine(SortRoutine);
		}

		public override void PostClose()
		{
			base.PostClose();
			ProfilerWatch.Suspend = false;
			ProfilerWatch.StopLoggingResults();
		}

		/// <summary>
		/// Sorted from greatest to smallest
		/// </summary>
		private int BlockSort(ProfilerWatch.Block block, ProfilerWatch.Block other)
		{
			Assert.IsTrue(sortBy > ColumnSort.None);
			switch (sortBy)
			{
				case ColumnSort.TotalMS:
					if (block.Elapsed < other.Elapsed) return 1;
					if (block.Elapsed > other.Elapsed) return -1;
					return 0;
				case ColumnSort.SelfMS:
					if (block.Self < other.Self) return 1;
					if (block.Self > other.Self) return -1;
					return 0;
				case ColumnSort.MaxMS:
					if (block.Max < other.Max) return 1;
					if (block.Max > other.Max) return -1;
					return 0;
				case ColumnSort.CallCount:
					if (block.CallCount < other.CallCount) return 1;
					if (block.CallCount > other.CallCount) return -1;
					return 0;
				default:
					return 0;
			}
		}

		private IEnumerator SortRoutine()
		{
			while (IsOpen)
			{
				ResortResults();
				yield return new WaitForSeconds(SortTime);
			}
		}

		private void ResortResults()
		{
			lock (ProfilerWatch.cacheLock)
			{
				ProfilerWatch.BlockContainer block = ProfilerWatch.Cache[PausedAtFrame];
				if (block != null && !block.InnerList.NullOrEmpty())
				{
					SortListRecursive(block.InnerList);
				}
			}
		}

		private void SortListRecursive(List<ProfilerWatch.Block> children)
		{
			children.Sort(BlockSort);
			foreach (ProfilerWatch.Block block in children)
			{
				if (!block.Children.NullOrEmpty())
				{
					SortListRecursive(block.Children);
				}
			}
		}

		public void OnGUIHighPriority()
		{
			//if (Event.current != null && Event.current.type == EventType.KeyDown)
			//{
			//	if (Event.current.keyCode == KeyCode.Space)
			//	{
			//		ProfilerWatch.Paused = !ProfilerWatch.Paused;
			//		Event.current.Use();
			//	}
			//}
		}

		public override void Notify_ClickOutsideWindow()
		{
			base.Notify_ClickOutsideWindow();
			PausedAtFrame = -1;
			ProfilerWatch.Suspend = false;
		}

		public override void DoWindowContents(Rect inRect)
		{
			using var textBlock = new TextBlock(Color.white);
			
			Widgets.DrawMenuSection(inRect);
			Rect rect = inRect.ContractedBy(1);

			rect.SplitHorizontally(topPanelHeight, out Rect topRect, out Rect bottomRect);

			DrawGraph(topRect);

			stack.Clear();
			if (PausedAtFrame >= 0)
			{
				lock (ProfilerWatch.cacheLock)
				{
					List<ProfilerWatch.Block> blocksAtFrame = ProfilerWatch.Cache[PausedAtFrame]?.InnerList;
					if (!blocksAtFrame.NullOrEmpty())
					{
						for (int i = blocksAtFrame.Count - 1; i >= 0; i--)
						{
							stack.Push((0, blocksAtFrame[i]));
						}
					}
				}
			}

			Rect headerRect = bottomRect.ContractedBy(2);
			headerRect.height = HeaderHeight;
			DrawHeaders(headerRect);
			bottomRect.yMin = headerRect.yMax;
			DrawBlocks(bottomRect);
		}

		private void DrawGraph(Rect rect)
		{
			if (DragGraphFrame(rect))
			{
				Vector2 mousePos = Event.current.mousePosition;
				float xT = mousePos.x / rect.xMax;
				PausedAtFrame = Mathf.RoundToInt(Mathf.Lerp(0, ProfilerWatch.CacheSize, xT));
				ProfilerWatch.Suspend = true;
			}

			Widgets.BeginGroup(rect); // Start Graph Group

			Rect graphRect = rect.AtZero();
			graphRect.yMax -= 3;

			if (ProfilerWatch.Patching != ProfilerWatch.ProfilePatching.Enabled)
			{
				string label = ProfilerWatch.Patching switch
				{
					ProfilerWatch.ProfilePatching.Disabled => "Disabled",
					ProfilerWatch.ProfilePatching.Applying => "Patching",
					ProfilerWatch.ProfilePatching.Removing => "Disabling",
					_ => "Missing Profile State"
				};
				using (new TextBlock(GameFont.Medium, TextAnchor.MiddleCenter))
				{
					Widgets.Label(graphRect, $"{label}{GenText.MarchingEllipsis()}");
				}
			}
			else
			{
				double maxElapsed = 60;
				lock (ProfilerWatch.cacheLock)
				{
					Vector2 p1 = Vector2.zero;
					int length = ProfilerWatch.Cache.Length;
					for (int i = 0; i < length; i++)
					{
						ProfilerWatch.BlockContainer container = ProfilerWatch.Cache[i];
						if (container == null || container.InnerList.NullOrEmpty()) continue;

						double result = container.TotalElapsed;
						float posX = graphRect.x + (float)i / length * graphRect.width;
						float posY = Mathf.Lerp(graphRect.yMax, graphRect.yMin, (float)(result / graphCeiling));
						Vector2 p2 = new Vector2(posX, posY);
						if (p1 != Vector2.zero)
						{
							if (result > maxElapsed) maxElapsed = result;

							Widgets.DrawLine(p1, p2, graphColor, 1);
						}
						p1 = p2;
					}
				}
				graphCeiling = maxElapsed;
			}

			Widgets.EndGroup(); // End Graph Group
			
			if (PausedAtFrame >= 0)
			{
				float xPos = Mathf.Lerp(rect.xMin, rect.xMax, (float)PausedAtFrame / ProfilerWatch.CacheSize);
				Rect boxRect = new Rect(xPos - FrameBarSize / 2f, rect.y, FrameBarSize, rect.height);
				Widgets.DrawBoxSolid(boxRect, framePauseColor);
			}
			
			Widgets.DrawLineHorizontal(rect.x, rect.yMax, rect.width, UIElements.MenuSectionBGBorderColor);
		}

		private bool DragGraphFrame(Rect rect)
		{
			if (!GUI.enabled)
			{
				return false;
			}
			if (Input.GetMouseButtonDown(0) && Mouse.IsOver(rect))
			{
				dragging = true;
				dragPos = Input.mousePosition;
				return true;
			}
			if (dragging)
			{
				if (Input.GetMouseButton(0))
				{
					if (UnityGUIBugsFixer.MouseDrag(0))
					{
						Event.current.Use();
					}
				}
				else
				{
					dragging = false;
					if (Input.GetMouseButtonUp(0))
					{
						Event.current.Use();
					}
				}
				return true;
			}
			return false;
		}

		private void DrawHeaders(Rect rect)
		{
			Widgets.DrawBoxSolidWithOutline(rect, headerColor, separatorColor);

			rect = rect.ContractedBy(1);

			using var fontSize = new TextBlock(GameFont.Small);
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.MiddleRight;
			for (int i = columns.Count - 1; i >= 0; i--)
			{
				ProfilerColumn column = columns[i];
				rect.xMax -= column.ColumnWidth;
				
				if (column.HeaderLabelBtn(rect))
				{
					ResortResults();
					SoundDefOf.TabOpen.PlayOneShotOnCamera();
				}
				UIElements.DrawLineVertical(rect.x, rect.y, -rect.height, separatorColor);
			}
			using var alignment = new TextBlock(TextAnchor.MiddleLeft);
			Widgets.Label(rect, " <b>Overview</b>");
		}

		private void DrawBlocks(Rect rect)
		{
			if (PausedAtFrame < 0) return;

			int rowCount = 0;
			Text.Font = GameFont.Tiny;
			Rect outRect = rect;
			Rect viewRect = new Rect(rect.x, rect.y, rect.width - 16, panelHeight);
			Widgets.BeginScrollView(outRect, ref scrollPos, viewRect);
			{
				while (stack.Count > 0)
				{
					(int indent, ProfilerWatch.Block block) = stack.Pop();
					Rect rowRect = new Rect(viewRect.x, viewRect.y + rowCount * EntryHeight, rect.width, EntryHeight);
					DoRow(rowRect, block, indent, rowCount % 2 == 0);

					if (block.Children.Count > 0 && expandedBlocks.TryGetValue(block, out bool expanded) && expanded)
					{
						indent++;
						for (int i = block.Children.Count - 1; i >= 0; i--)
						{
							ProfilerWatch.Block childBlock = block.Children[i];
							stack.Push((indent, childBlock));
						}
					}
					rowCount++;
				}
			}
			Widgets.EndScrollView();

			RecacheEntryStats(rowCount);
		}

		private void DoRow(Rect rect, ProfilerWatch.Block block, int indent, bool alternateColor)
		{
			bool expanded = expandedBlocks.TryGetValue(block, false);

			Widgets.DrawBoxSolid(rect, alternateColor ? lineAlternateColorLight : lineAlternateColorDark);

			Text.Anchor = TextAnchor.MiddleRight;
			for (int i = columns.Count - 1; i >= 0; i--)
			{
				ProfilerColumn column = columns[i];
				rect.xMax -= column.ColumnWidth;
				column.DrawResult(rect, block);
			}

			Rect collapseBtnRect = new Rect(rect.x, rect.y, rect.height, rect.height);
			collapseBtnRect.x += indent * IndentSpace;
			if (block.Children.Count > 0 && UIElements.CollapseButton(collapseBtnRect.ContractedBy(2), ref expanded))
			{
				expandedBlocks[block] = expanded;
			}

			Text.Anchor = TextAnchor.MiddleLeft;
			rect.xMin = collapseBtnRect.xMax;
			Widgets.Label(rect, block.Label);
		}

		private void RecacheEntryStats(int count)
		{
			if (cachedEntryCount == count)
			{
				return;
			}
			cachedEntryCount = count;
			panelHeight = EntryHeight * count;
		}

		private enum ColumnSort
		{
			None,
			TotalMS,
			SelfMS,
			MaxMS,
			CallCount,
		}

		private abstract class ProfilerColumn
		{
			public abstract string Label { get; }

			public abstract ColumnSort SortBy { get; }

			public float ColumnWidth { get; internal set; } = DefaultProfilerColumnWidth;

			public abstract string ResultFor(ProfilerWatch.Block block);

			/// <param name="rect"></param>
			/// <returns>true if header rect is clicked.</returns>
			public virtual bool HeaderLabelBtn(Rect rect)
			{
				if (Label != null)
				{
					using var alignment = new TextBlock(TextAnchor.MiddleCenter);

					Rect columnRect = new Rect(rect.xMax, rect.y, ColumnWidth, rect.height);
					Widgets.Label(columnRect, $"<b>{Label}</b>");
					if (SortBy > ColumnSort.None)
					{
						if (SortBy == sortBy)
						{
							Widgets.DrawHighlight(columnRect);
						}
						else if (Widgets.ButtonInvisible(columnRect))
						{
							sortBy = SortBy;
							return true;
						}
					}
				}
				return false;
			}

			public virtual void DrawResult(Rect rect, ProfilerWatch.Block block)
			{
				string result = ResultFor(block);
				if (result != null)
				{
					Rect columnRect = new Rect(rect.xMax, rect.y, ColumnWidth, rect.height);
					Widgets.Label(columnRect, result);
				}
			}
		}

		private class TotalMSColumn : ProfilerColumn
		{
			public override string Label => "Total ms";

			public override ColumnSort SortBy => ColumnSort.TotalMS;

			public override string ResultFor(ProfilerWatch.Block block)
			{
				return $"{block.Elapsed:0.000}";
			}
		}

		private class SelfMSColumn : ProfilerColumn
		{
			public override string Label => "Self ms";

			public override ColumnSort SortBy => ColumnSort.SelfMS;

			public override string ResultFor(ProfilerWatch.Block block)
			{
				return $"{block.Self:0.000}";
			}
		}

		private class MaxMSColumn : ProfilerColumn
		{
			public override string Label => "Max ms";

			public override ColumnSort SortBy => ColumnSort.MaxMS;

			public override string ResultFor(ProfilerWatch.Block block)
			{
				return $"{block.Max:0.000}";
			}
		}

		private class CallCountColumn : ProfilerColumn
		{
			public override string Label => "Calls";

			public override ColumnSort SortBy => ColumnSort.CallCount;

			public override string ResultFor(ProfilerWatch.Block block)
			{
				return block.CallCount.ToString();
			}
		}

		private class ColumnSpace : ProfilerColumn
		{
			public ColumnSpace()
			{
				ColumnWidth = HeaderHeight;
			}

			public override string Label => null;

			public override ColumnSort SortBy => ColumnSort.None;

			public override string ResultFor(ProfilerWatch.Block block) => null;
		}

		private class EndColumn : ColumnSpace
		{
			public override bool HeaderLabelBtn(Rect rect)
			{
				Rect columnRect = new Rect(rect.xMax, rect.y, ColumnWidth, ColumnWidth).ContractedBy(5);
				if (Widgets.ButtonImage(columnRect, TexButton.HotReloadDefs))
				{
					ProfilerWatch.ClearCache();
				}
				return false; // Handles button input separately from sort logic
			}
		}
	}
}
