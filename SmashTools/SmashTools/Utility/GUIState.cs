using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace SmashTools;

public static class GUIState
{
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

	public readonly struct Disabler : IDisposable
	{
		private readonly bool prevState;
		private readonly Color prevColor;

		public Disabler()
		{
			prevState = GUI.enabled;
			prevColor = GUI.color;
		}

		void IDisposable.Dispose()
		{
			GUI.enabled = prevState;
			GUI.color = prevColor;
		}
	}
}