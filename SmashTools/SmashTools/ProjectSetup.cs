using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using LudeonTK;
using RimWorld;
using SmashTools.Performance;
using SmashTools.UnitTesting;
using SmashTools.Xml;
using UnityEngine;
using UnityEngine.SceneManagement;
using Verse;
using Verse.Profile;

namespace SmashTools
{
  public class ProjectSetup : Mod
  {
    public const string ProjectLabel = "SmashTools";
    public const string LogLabel = $"[{ProjectLabel}]";
    public const string HarmonyId = "SmashPhil.SmashTools";

    public static Harmony Harmony { get; private set; }

    public ProjectSetup(ModContentPack content) : base(content)
    {
      RegisterParseableStructs();

      Harmony = new Harmony(HarmonyId);

      // Xml Parsing
      Harmony.Patch(
        original: AccessTools.Method(typeof(DirectXmlLoader), nameof(DirectXmlLoader.DefFromNode)),
        postfix: new HarmonyMethod(typeof(XmlParseHelper),
          nameof(XmlParseHelper.ReadCustomAttributesOnDef)));
      Harmony.Patch(original: AccessTools.Method(typeof(DirectXmlToObject), "GetFieldInfoForType"),
        postfix: new HarmonyMethod(typeof(XmlParseHelper),
          nameof(XmlParseHelper.ReadCustomAttributes)));

      // Logging
#if !RELEASE
      // Just removing brackets from stacktrace for clarity. Let's not force other modders to deal
      // with the performance hit of constant regex filtering in release builds.
      Harmony.Patch(
        original: AccessTools.Method(typeof(Log), nameof(Log.Message),
          parameters: [typeof(string)]),
        transpiler: new HarmonyMethod(typeof(SmashLog),
          nameof(SmashLog.RemoveRichTextFromDebugLogTranspiler)));
      Harmony.Patch(original: AccessTools.Method(typeof(Log), nameof(Log.Warning)),
        transpiler: new HarmonyMethod(typeof(SmashLog),
          nameof(SmashLog.RemoveRichTextFromDebugLogWarningTranspiler)));
      Harmony.Patch(original: AccessTools.Method(typeof(Log), nameof(Log.Error)),
        transpiler: new HarmonyMethod(typeof(SmashLog),
          nameof(SmashLog.RemoveRichTextFromDebugLogErrorTranspiler)));
      Harmony.Patch(original: AccessTools.Method(typeof(EditWindow_Log), "DoMessageDetails"),
        transpiler: new HarmonyMethod(typeof(SmashLog),
          nameof(SmashLog.RemoveRichTextMessageDetailsTranspiler)));
#endif

      // Game, World, and Map events
      Harmony.Patch(original: AccessTools.Method(typeof(Map), nameof(Map.ConstructComponents)),
        postfix: new HarmonyMethod(typeof(DetachedMapComponent),
          nameof(DetachedMapComponent.InstantiateAllMapComponents)));
      Harmony.Patch(
        original: AccessTools.Method(typeof(MapComponentUtility),
          nameof(MapComponentUtility.MapRemoved)),
        prefix: new HarmonyMethod(typeof(DetachedMapComponent),
          nameof(DetachedMapComponent.ClearComponentsFromCache)));
      Harmony.Patch(original: AccessTools.Method(typeof(Map), nameof(Map.ExposeData)),
        prefix: new HarmonyMethod(typeof(ComponentCache),
          nameof(ComponentCache.ClearCache)));
      Harmony.Patch(original: AccessTools.Method(typeof(MapDeiniter), nameof(MapDeiniter.Deinit)),
        postfix: new HarmonyMethod(typeof(ComponentCache),
          nameof(ComponentCache.ClearMapComps), [typeof(Map)]));
      Harmony.Patch(original: AccessTools.Method(typeof(Game), nameof(Game.InitNewGame)),
        prefix: new HarmonyMethod(typeof(ComponentCache),
          nameof(ComponentCache.ClearCache)));
      Harmony.Patch(original: AccessTools.Method(typeof(Map), nameof(Map.FinalizeInit)),
        postfix: new HarmonyMethod(typeof(ProjectSetup),
          nameof(BackfillRegisteredAreas)));
      Harmony.Patch(
        original: AccessTools.Method(typeof(MemoryUtility),
          nameof(MemoryUtility.ClearAllMapsAndWorld)),
        prefix: new HarmonyMethod(typeof(GameEvent),
          nameof(GameEvent.OnWorldUnloading)),
        postfix: new HarmonyMethod(typeof(GameEvent),
          nameof(GameEvent.OnWorldRemoved)));
      Harmony.Patch(
        original: AccessTools.Method(typeof(GameComponentUtility),
          nameof(GameComponentUtility.StartedNewGame)),
        postfix: new HarmonyMethod(typeof(GameEvent),
          nameof(GameEvent.OnNewGame)));
      Harmony.Patch(
        original: AccessTools.Method(typeof(GameComponentUtility),
          nameof(GameComponentUtility.LoadedGame)),
        postfix: new HarmonyMethod(typeof(GameEvent),
          nameof(GameEvent.OnLoadGame)));
      Harmony.Patch(original: AccessTools.Method(typeof(UIRoot_Entry), nameof(UIRoot_Entry.Init)),
        postfix: new HarmonyMethod(typeof(GameEvent),
          nameof(GameEvent.OnMainMenu)));

      // IThingHolderPawnOverlayer
      Harmony.Patch(original: AccessTools.Method(typeof(PawnRenderer), "GetBodyPos"),
        transpiler: new HarmonyMethod(typeof(PawnOverlayRenderer),
          nameof(PawnOverlayRenderer.ShowBodyTranspiler)));
      Harmony.Patch(
        original: AccessTools.Method(typeof(PawnRenderer), nameof(PawnRenderer.LayingFacing)),
        prefix: new HarmonyMethod(typeof(PawnOverlayRenderer),
          nameof(PawnOverlayRenderer.LayingFacing)));

      // Unit Tests
#if DEBUG
      Harmony.Patch(original: AccessTools.Method(typeof(DebugWindowsOpener), "DrawButtons"),
        postfix: new HarmonyMethod(typeof(ProjectSetup),
          nameof(DrawDebugWindowButton)));
      Harmony.Patch(original: AccessTools.Method(typeof(Game), nameof(Game.InitNewGame)),
        prefix: new HarmonyMethod(typeof(UnitTestManager),
          nameof(UnitTestManager.InitNewGame)));

      // Input handling (DEBUG)
      Harmony.Patch(
        original: AccessTools.Method(typeof(UIRoot_Entry), nameof(UIRoot_Entry.UIRootOnGUI)),
        prefix: new HarmonyMethod(typeof(MainMenuKeyBindHandler),
          nameof(MainMenuKeyBindHandler.HandleKeyInputs)));
      Harmony.Patch(
        original: AccessTools.Method(typeof(UIRoot_Play), nameof(UIRoot_Play.UIRootOnGUI)),
        prefix: new HarmonyMethod(typeof(MainMenuKeyBindHandler),
          nameof(MainMenuKeyBindHandler.HandleKeyInputs)));
#endif

      // Input handling
      Harmony.Patch(original: AccessTools.Method(typeof(WindowStack), nameof(WindowStack.Add)),
        postfix: new HarmonyMethod(typeof(HighPriorityInputs),
          nameof(HighPriorityInputs.WindowAddedToStack)));
      Harmony.Patch(
        original: AccessTools.Method(typeof(WindowStack), nameof(WindowStack.TryRemove),
          parameters: [typeof(Window), typeof(bool)]),
        postfix: new HarmonyMethod(typeof(HighPriorityInputs),
          nameof(HighPriorityInputs.WindowRemovedFromStack)));
      Harmony.Patch(
        original: AccessTools.Method(typeof(WindowStack),
          nameof(WindowStack.HandleEventsHighPriority)),
        postfix: new HarmonyMethod(typeof(HighPriorityInputs),
          nameof(HighPriorityInputs.HighPriorityOnGUI)));

      // UI
      Harmony.Patch(
        original: AccessTools.Method(typeof(MainTabWindow_Inspect),
          nameof(MainTabWindow_Inspect.DoInspectPaneButtons)),
        prefix: new HarmonyMethod(typeof(ProjectSetup),
          nameof(InspectablePaneButtons)));

      // Mod Init
      StaticConstructorOnModInit();

      SceneManager.sceneLoaded += ThreadManager.OnSceneChanged;
      GameEvent.onWorldUnloading += ThreadManager.ReleaseThreadsAndClearCache;

#if DEBUG
      GameEvent.onNewGame += StartupTest.ExecuteNewGameTesting;
      GameEvent.onLoadGame += StartupTest.ExecutePostLoadTesting;
      GameEvent.onMainMenu += StartupTest.ExecuteOnStartupTesting;
#endif
    }

    private static void RegisterParseableStructs()
    {
      ParseHelper.Parsers<Rot8>.Register(Rot8.FromString);
      ParseHelper.Parsers<Quadrant>.Register(Quadrant.FromString);
      ParseHelper.Parsers<RimWorldTime>.Register(RimWorldTime.FromString);
    }

    /// <summary>
    /// Call static constructors before Mod classes are instantiated
    /// </summary>
    /// <remarks>
    /// Patch or run code without needing to initialize a Mod instance
    /// </remarks>
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

    private static void BackfillRegisteredAreas(Map __instance)
    {
      __instance.TryAddAreas();
    }
  }
}