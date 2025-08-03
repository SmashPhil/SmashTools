using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace SmashTools;

public static class MainMenuKeyBindHandler
{
  private static readonly List<(KeyBindingDef keyBindingDef, Action action)> KeyBindings = [];

  public static void RegisterKeyBind(KeyBindingDef keyBindingDef, Action action)
  {
    if (!KeyBindings.Any(pair => pair.keyBindingDef == keyBindingDef))
    {
      KeyBindings.Add((keyBindingDef, action));
    }
  }

  internal static bool HandleKeyInputs()
  {
    if (!Prefs.DevMode)
      return true;

    foreach ((KeyBindingDef keyBindingDef, Action action) in KeyBindings)
    {
      if (Event.current != null && keyBindingDef.KeyDownEvent)
      {
        action();
        Event.current.Use();
      }
    }
    return true;
  }
}