using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace SmashTools
{
	public static class GUIState
	{
		private static readonly Stack<StateValues> stack = new Stack<StateValues>();

		private static bool ActiveState => stack.Count > 0;

		public static void Push()
		{
			stack.Push(new StateValues(GUI.color, Text.Font, Text.Anchor));
		}

		public static void Reset()
		{
			if (!ActiveState)
			{
				SmashLog.Error($"Attempting to reset GUI and Text fields without pushing values.");
				return;
			}
			StateValues state = stack.Peek();
			GUI.color = state.guiColor;
			Text.Font = state.gameFont;
			Text.Anchor = state.textAnchor;
		}

		public static void Pop()
		{
			Reset();
			stack.Pop();
		}

		public static void Disable()
		{
			if (!ActiveState)
			{
				SmashLog.Error($"Attempting to reset GUI and Text fields without pushing values.");
				return;
			}
			GUI.enabled = false;
			GUI.color = UIElements.InactiveColor;
		}

		public static void Enable()
		{
			if (!ActiveState)
			{
				SmashLog.Error($"Attempting to reset GUI and Text fields without pushing values.");
				return;
			}
			GUI.enabled = true;
			GUI.color = stack.Peek().guiColor;
		}

		private struct StateValues
		{
			public Color guiColor;
			public GameFont gameFont;
			public TextAnchor textAnchor;

			public StateValues(Color guiColor, GameFont gameFont, TextAnchor textAnchor)
			{
				this.guiColor = guiColor;
				this.gameFont = gameFont;
				this.textAnchor = textAnchor;
			}
		}
	}
}
