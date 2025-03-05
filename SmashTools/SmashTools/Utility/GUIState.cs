using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace SmashTools
{
	public static class GUIState
	{
		// TODO 1.6 - Remove
		[Obsolete("Use TextBlock with disposable pattern", true)]
    public static void Push()
    {
    }
    // TODO 1.6 - Remove
    [Obsolete("Use TextBlock with disposable pattern", true)]
    public static void Reset()
    {
    }
    // TODO 1.6 - Remove
    [Obsolete("Use TextBlock with disposable pattern", true)]
    public static void Pop()
    {
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
	}
}
