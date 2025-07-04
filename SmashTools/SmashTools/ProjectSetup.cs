using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using LudeonTK;
using RimWorld;
using SmashTools.Patching;
using SmashTools.Performance;
using SmashTools.Xml;
using UnityEngine;
using UnityEngine.SceneManagement;
using Verse;
using Verse.Profile;

namespace SmashTools;

public class ProjectSetup : Mod
{
  public const string ProjectLabel = "SmashTools";
  public const string LogLabel = $"[{ProjectLabel}]";
  public const string HarmonyId = "SmashPhil.SmashTools";

  public ProjectSetup(ModContentPack content) : base(content)
  {
    HarmonyPatcher.Init(content);

    // Logging
#if !RELEASE
    // Just removing brackets from stacktrace for clarity. Let's not force other modders to deal
    // with the performance hit of constant regex filtering in release builds.
    HarmonyPatcher.Harmony.Patch(
      original: AccessTools.Method(typeof(Log), nameof(Log.Message),
        parameters: [typeof(string)]),
      transpiler: new HarmonyMethod(typeof(SmashLog),
        nameof(SmashLog.RemoveRichTextFromDebugLogTranspiler)));
    HarmonyPatcher.Harmony.Patch(original: AccessTools.Method(typeof(Log), nameof(Log.Warning)),
      transpiler: new HarmonyMethod(typeof(SmashLog),
        nameof(SmashLog.RemoveRichTextFromDebugLogWarningTranspiler)));
    HarmonyPatcher.Harmony.Patch(original: AccessTools.Method(typeof(Log), nameof(Log.Error)),
      transpiler: new HarmonyMethod(typeof(SmashLog),
        nameof(SmashLog.RemoveRichTextFromDebugLogErrorTranspiler)));
    HarmonyPatcher.Harmony.Patch(
      original: AccessTools.Method(typeof(EditWindow_Log), "DoMessageDetails"),
      transpiler: new HarmonyMethod(typeof(SmashLog),
        nameof(SmashLog.RemoveRichTextMessageDetailsTranspiler)));
#endif

    // Xml Parsing
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(DirectXmlLoader), nameof(DirectXmlLoader.DefFromNode)),
      postfix: new HarmonyMethod(typeof(XmlParseHelper),
        nameof(XmlParseHelper.ReadCustomAttributesOnDef)));
    HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(XmlToObjectUtils),
        nameof(XmlToObjectUtils.DoFieldSearch)),
      postfix: new HarmonyMethod(typeof(XmlParseHelper),
        nameof(XmlParseHelper.ReadCustomAttributes)));

    // Map Components
    HarmonyPatcher.Harmony.Patch(original: AccessTools.Method(typeof(Game), nameof(Game.AddMap)),
      postfix: new HarmonyMethod(typeof(ComponentCache),
        nameof(ComponentCache.PreCache)));
    HarmonyPatcher.Harmony.Patch(
      original: AccessTools.Method(typeof(Map), nameof(Map.FinalizeLoading)),
      prefix: new HarmonyMethod(typeof(ComponentCache),
        nameof(ComponentCache.PreCacheInst)));
    HarmonyPatcher.Harmony.Patch(
      original: AccessTools.Method(typeof(MapDeiniter),
        nameof(MapDeiniter.Deinit)),
      postfix: new HarmonyMethod(typeof(ComponentCache),
        nameof(ComponentCache.ClearMap), [typeof(Map)]));

    // Game events
    HarmonyPatcher.Harmony.Patch(
      original: AccessTools.Method(typeof(DefGenerator),
        nameof(DefGenerator.GenerateImpliedDefs_PreResolve)),
      prefix: new HarmonyMethod(typeof(GameEvent),
        nameof(GameEvent.RaiseOnGenerateImpliedDefs)));
    HarmonyPatcher.Harmony.Patch(
      original: AccessTools.Method(typeof(MemoryUtility),
        nameof(MemoryUtility.ClearAllMapsAndWorld)),
      prefix: new HarmonyMethod(typeof(GameEvent),
        nameof(GameEvent.RaiseOnWorldUnloading)),
      postfix: new HarmonyMethod(typeof(GameEvent),
        nameof(GameEvent.RaiseOnWorldRemoved)));
    HarmonyPatcher.Harmony.Patch(
      original: AccessTools.Method(typeof(GameComponentUtility),
        nameof(GameComponentUtility.StartedNewGame)),
      postfix: new HarmonyMethod(typeof(GameEvent),
        nameof(GameEvent.RaiseOnNewGame)));
    HarmonyPatcher.Harmony.Patch(
      original: AccessTools.Method(typeof(GameComponentUtility),
        nameof(GameComponentUtility.LoadedGame)),
      postfix: new HarmonyMethod(typeof(GameEvent),
        nameof(GameEvent.RaiseOnLoadGame)));
    HarmonyPatcher.Harmony.Patch(
      original: AccessTools.Method(typeof(UIRoot_Entry), nameof(UIRoot_Entry.Init)),
      postfix: new HarmonyMethod(typeof(GameEvent),
        nameof(GameEvent.RaiseOnMainMenu)));

    // IThingHolderPawnOverlayer
    HarmonyPatcher.Harmony.Patch(original: AccessTools.Method(typeof(PawnRenderer), "GetBodyPos"),
      transpiler: new HarmonyMethod(typeof(PawnOverlayRenderer),
        nameof(PawnOverlayRenderer.ShowBodyTranspiler)));
    HarmonyPatcher.Harmony.Patch(
      original: AccessTools.Method(typeof(PawnRenderer), nameof(PawnRenderer.LayingFacing)),
      prefix: new HarmonyMethod(typeof(PawnOverlayRenderer),
        nameof(PawnOverlayRenderer.LayingFacing)));

#if DEBUG
    HarmonyPatcher.Harmony.Patch(
      original: AccessTools.Method(typeof(DebugWindowsOpener), "DrawButtons"),
      postfix: new HarmonyMethod(typeof(ProjectSetup),
        nameof(DrawDebugWindowButton)));

    // Input handling (DEBUG)
    HarmonyPatcher.Harmony.Patch(
      original: AccessTools.Method(typeof(UIRoot_Entry), nameof(UIRoot_Entry.UIRootOnGUI)),
      prefix: new HarmonyMethod(typeof(MainMenuKeyBindHandler),
        nameof(MainMenuKeyBindHandler.HandleKeyInputs)));
    HarmonyPatcher.Harmony.Patch(
      original: AccessTools.Method(typeof(UIRoot_Play), nameof(UIRoot_Play.UIRootOnGUI)),
      prefix: new HarmonyMethod(typeof(MainMenuKeyBindHandler),
        nameof(MainMenuKeyBindHandler.HandleKeyInputs)));
#endif

    // Input handling
    HarmonyPatcher.Harmony.Patch(
      original: AccessTools.Method(typeof(WindowStack), nameof(WindowStack.Add)),
      postfix: new HarmonyMethod(typeof(HighPriorityInputs),
        nameof(HighPriorityInputs.WindowAddedToStack)));
    HarmonyPatcher.Harmony.Patch(
      original: AccessTools.Method(typeof(WindowStack), nameof(WindowStack.TryRemove),
        parameters: [typeof(Window), typeof(bool)]),
      postfix: new HarmonyMethod(typeof(HighPriorityInputs),
        nameof(HighPriorityInputs.WindowRemovedFromStack)));
    HarmonyPatcher.Harmony.Patch(
      original: AccessTools.Method(typeof(WindowStack),
        nameof(WindowStack.HandleEventsHighPriority)),
      postfix: new HarmonyMethod(typeof(HighPriorityInputs),
        nameof(HighPriorityInputs.HighPriorityOnGUI)));

    // UI
    HarmonyPatcher.Harmony.Patch(
      original: AccessTools.Method(typeof(MainTabWindow_Inspect),
        nameof(MainTabWindow_Inspect.DoInspectPaneButtons)),
      prefix: new HarmonyMethod(typeof(ProjectSetup),
        nameof(InspectablePaneButtons)));

    // Mod Init
    StaticConstructorOnModInit();

    ConditionalPatches.RunAll();
    HarmonyPatcher.Run(PatchSequence.Mod);
    HarmonyPatcher.Run(PatchSequence.Async);

    SceneManager.sceneLoaded += ThreadManager.OnSceneChanged;
    GameEvent.OnWorldUnloading += ThreadManager.ReleaseAll;
    GameEvent.OnWorldUnloading += ComponentCache.ClearAll;

#if DEBUG
    GameEvent.OnNewGame += StartupTest.ExecuteNewGameTesting;
    GameEvent.OnLoadGame += StartupTest.ExecutePostLoadTesting;
    GameEvent.OnMainMenu += StartupTest.ExecuteOnStartupTesting;
#endif
  }

  private static void StaticConstructorOnModInit()
  {
    foreach (Type type in GenTypes.AllTypesWithAttribute<StaticConstructorOnModInitAttribute>())
    {
      try
      {
        RuntimeHelpers.RunClassConstructor(type.TypeHandle);
      }
      catch (Exception ex)
      {
        SmashLog.Error(
          $"Exception thrown running constructor of type <type>{type}</type>. Ex=\"{ex}\"");
      }
    }
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