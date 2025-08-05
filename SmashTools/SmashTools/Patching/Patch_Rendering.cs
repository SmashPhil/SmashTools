using HarmonyLib;
using RimWorld;
using SmashTools.Performance;
using UnityEngine;
using Verse;

namespace SmashTools.Patching;

internal class Patch_Rendering : IPatchCategory
{
  PatchSequence IPatchCategory.PatchAt => PatchSequence.Async;

  void IPatchCategory.PatchMethods()
  {
    // IThingHolderPawnOverlayer
    HarmonyPatcher.Patch(original: AccessTools.Method(typeof(PawnRenderer), "GetBodyPos"),
      transpiler: new HarmonyMethod(typeof(PawnOverlayRenderer),
        nameof(PawnOverlayRenderer.ShowBodyTranspiler)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(PawnRenderer), nameof(PawnRenderer.LayingFacing)),
      prefix: new HarmonyMethod(typeof(PawnOverlayRenderer),
        nameof(PawnOverlayRenderer.LayingFacing)));

#if DEBUG
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(DebugWindowsOpener), "DrawButtons"),
      postfix: new HarmonyMethod(typeof(Patch_Rendering),
        nameof(DrawDebugWindowButton)));

    // Input handling
    LongEventHandler.ExecuteWhenFinished(() => UnityThread.StartGUI(MainMenuKeyBindHandler.HandleKeyInputs));
#endif

    // Input handling
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(WindowStack), nameof(WindowStack.Add)),
      postfix: new HarmonyMethod(typeof(HighPriorityInputs),
        nameof(HighPriorityInputs.WindowAddedToStack)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(WindowStack), nameof(WindowStack.TryRemove),
        parameters: [typeof(Window), typeof(bool)]),
      postfix: new HarmonyMethod(typeof(HighPriorityInputs),
        nameof(HighPriorityInputs.WindowRemovedFromStack)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(WindowStack),
        nameof(WindowStack.HandleEventsHighPriority)),
      postfix: new HarmonyMethod(typeof(HighPriorityInputs),
        nameof(HighPriorityInputs.HighPriorityOnGUI)));

    // UI
    // NOTE - A few other mods patch DoInspectPaneButtons destructively, but inspectables don't need to show
    // other mods' pins right now. Just show Inspectable's and let those mods work with non-VF pawns.
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(MainTabWindow_Inspect),
        nameof(MainTabWindow_Inspect.DoInspectPaneButtons)),
      prefix: new HarmonyMethod(AccessTools.Method(typeof(Patch_Rendering),
        nameof(InspectablePaneButtons)), priority: Priority.First));
  }

  private static void DrawDebugWindowButton(WidgetRow ___widgetRow, out float ___widgetRowFinalX)
  {
    if (___widgetRow.ButtonIcon(TexButton.OpenDebugActionsMenu,
      "Open Startup Actions menu.\n\n This lets you initiate certain static methods on startup for quick testing."))
    {
      StartupTest.OpenMenu();
    }

    ___widgetRowFinalX = ___widgetRow.FinalX;
  }

  private static bool InspectablePaneButtons(Rect rect, ref float lineEndWidth)
  {
    if (Find.Selector.SingleSelectedThing is IInspectable inspectable)
    {
      lineEndWidth += 30;
      Widgets.InfoCardButton(rect.width - lineEndWidth, 0f, Find.Selector.SingleSelectedThing);
      lineEndWidth += inspectable.DoInspectPaneButtons(rect.width - lineEndWidth);
      return false;
    }
    return true;
  }
}