using System.Runtime.CompilerServices;
using UnityEngine;

namespace SmashTools;

public static class GUIState
{
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void Disable()
  {
    GUI.enabled = false;
    GUI.color = UIElements.inactiveColor;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void Enable()
  {
    GUI.enabled = true;
    GUI.color = Color.white;
  }
}