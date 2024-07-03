using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace SmashTools
{
	internal static class HighPriorityInputs
	{
		private static readonly List<IHighPriorityOnGUI> highPriorityOnGUIs = new List<IHighPriorityOnGUI>();

		internal static void WindowAddedToStack(Window window)
		{
			if (window is IHighPriorityOnGUI highPriorityOnGUI)
			{
				highPriorityOnGUIs.Add(highPriorityOnGUI);
			}
		}

		internal static void WindowRemovedFromStack(Window window, bool __result)
		{
			if (__result && window is IHighPriorityOnGUI highPriorityOnGUI)
			{
				highPriorityOnGUIs.Remove(highPriorityOnGUI);
			}
		}

		internal static void HighPriorityOnGUI()
		{
			for (int i = highPriorityOnGUIs.Count - 1; i >= 0; i--)
			{
				IHighPriorityOnGUI highPriorityOnGUI = highPriorityOnGUIs[i];
				highPriorityOnGUI.OnGUIHighPriority();
			}
		}
	}
}
