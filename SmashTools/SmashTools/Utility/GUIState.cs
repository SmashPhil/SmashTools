using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace SmashTools
{
	/// <summary>
	/// Captures and maintains GUI and Text states
	/// </summary>
	/// <remarks>Ensures values of GUI and Text remain consistent from beginning to end of OnGUI function.</remarks>
	public static class GUIState
	{
		private static readonly Stack<StateValues> stack = new Stack<StateValues>();

		public static bool Empty => stack.Count == 0;

		[Obsolete("Use TextBlock instead.")]
		public static void Push()
		{
			stack.Push(StateValues.Capture());
		}

		[Obsolete("Use TextBlock instead.")]
		public static void Reset()
		{
			if (Empty)
			{
				SmashLog.Error($"Attempting to reset GUI and Text fields without pushing values.");
				return;
			}
			StateValues state = stack.Peek();
			state.Reset(); //Can still be performed on copy of StateValues. GUI settings get set with its values, state does not change
		}

		[Obsolete("Use TextBlock instead.")]
		public static void Pop()
		{
			Reset();
			stack.Pop();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Disable()
		{
			GUI.enabled = false;
			GUI.color = UIElements.InactiveColor;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Enable()
		{
			GUI.enabled = true;
			GUI.color = Color.white;
		}

		private struct StateValues
		{
			//GUI
			public Color color;
			public Color backgroundColor;
			public bool enabled;
			public Matrix4x4 matrix;

			//Text
			public GameFont gameFont;
			public TextAnchor textAnchor;
			public bool wordWrap;
			
			public void Reset()
			{
				GUI.color = color;
				GUI.backgroundColor = backgroundColor;
				GUI.enabled = enabled;
				GUI.matrix = matrix;

				Text.Font = gameFont;
				Text.Anchor = textAnchor;
				Text.WordWrap = wordWrap;
			}

			public static StateValues Capture()
			{
				StateValues values = new StateValues();
				values.color = GUI.color;
				values.backgroundColor = GUI.backgroundColor;
				values.enabled = GUI.enabled;
				values.matrix = GUI.matrix;

				values.gameFont = Text.Font;
				values.textAnchor = Text.Anchor;
				values.wordWrap = Text.WordWrap;

				return values;
			}
		}
	}
}
