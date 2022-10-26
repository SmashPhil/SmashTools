using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace SmashTools
{
	public static class MainMenuKeyBindHandler
	{
		private static List<(KeyBindingDef keyBindingDef, Action action)> keyBindings = new List<(KeyBindingDef keyBind, Action action)>();

		public static void RegisterKeyBind(KeyBindingDef keyBindingDef, Action action)
		{
			if (!keyBindings.Any(pair => pair.keyBindingDef == keyBindingDef))
			{
				keyBindings.Add((keyBindingDef, action));
			}
		}

		internal static void HandleKeyInputs()
		{
			if (Prefs.DevMode)
			{
				foreach ((KeyBindingDef keyBindingDef, Action action) in keyBindings)
				{
					if (keyBindingDef.KeyDownEvent)
					{
						action();
						Event.current.Use();
					}
				}
			}
		}
	}
}
