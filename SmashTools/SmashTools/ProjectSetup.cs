using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using LudeonTK;
using RimWorld;
using SmashTools.Performance;
using SmashTools.Xml;
using UnityEngine;
using UnityEngine.SceneManagement;
using Verse;

namespace SmashTools
{
	public class ProjectSetup : Mod
	{
		public const string ProjectLabel = "[SmashTools]";
		public const string HarmonyId = "SmashPhil.SmashTools";

		public static Harmony Harmony { get; private set; }

		public ProjectSetup(ModContentPack content) : base(content)
		{
			RegisterParseableStructs();

			Harmony = new Harmony(HarmonyId);

			//Xml Parsing
			Harmony.Patch(original: AccessTools.Method(typeof(DirectXmlLoader), nameof(DirectXmlLoader.DefFromNode)),
				postfix: new HarmonyMethod(typeof(XmlParseHelper),
				nameof(XmlParseHelper.ReadCustomAttributesOnDef)));
			Harmony.Patch(original: AccessTools.Method(typeof(DirectXmlToObject), "GetFieldInfoForType"),
				postfix: new HarmonyMethod(typeof(XmlParseHelper),
				nameof(XmlParseHelper.ReadCustomAttributes)));

			//Logging
			Harmony.Patch(original: AccessTools.Method(typeof(EditWindow_Log), "DoMessageDetails"),
				transpiler: new HarmonyMethod(typeof(SmashLog),
				nameof(SmashLog.RemoveRichTextTranspiler)));
			
			//Map
			Harmony.Patch(original: AccessTools.Method(typeof(Map), nameof(Map.ConstructComponents)),
				postfix: new HarmonyMethod(typeof(DetachedMapComponent),
				nameof(DetachedMapComponent.InstantiateAllMapComponents)));
			Harmony.Patch(original: AccessTools.Method(typeof(MapComponentUtility), nameof(MapComponentUtility.MapRemoved)),
				prefix: new HarmonyMethod(typeof(DetachedMapComponent),
				nameof(DetachedMapComponent.ClearComponentsFromCache)));
			Harmony.Patch(original: AccessTools.Method(typeof(Map), nameof(Map.ExposeData)),
				prefix: new HarmonyMethod(typeof(ComponentCache),
				nameof(ComponentCache.ClearCache)));
			Harmony.Patch(original: AccessTools.Method(typeof(MapDeiniter), nameof(MapDeiniter.Deinit)),
				postfix: new HarmonyMethod(typeof(ComponentCache),
				nameof(ComponentCache.ClearMapComps), new Type[] { typeof(Map) }));
			Harmony.Patch(original: AccessTools.Method(typeof(Game), "ClearCaches"),
				postfix: new HarmonyMethod(typeof(ProjectSetup),
				nameof(ClearCaches)));
			Harmony.Patch(original: AccessTools.Method(typeof(Game), nameof(Game.InitNewGame)),
				prefix: new HarmonyMethod(typeof(ComponentCache),
				nameof(ComponentCache.ClearCache)));
			Harmony.Patch(original: AccessTools.Method(typeof(Map), nameof(Map.FinalizeInit)),
				postfix: new HarmonyMethod(typeof(ProjectSetup),
				nameof(BackfillRegisteredAreas)));

			//IThingHolderPawnOverlayer
			Harmony.Patch(original: AccessTools.Method(typeof(PawnRenderer), "GetBodyPos"),
				transpiler: new HarmonyMethod(typeof(PawnOverlayRenderer),
				nameof(PawnOverlayRenderer.ShowBodyTranspiler)));
			Harmony.Patch(original: AccessTools.Method(typeof(PawnRenderer), nameof(PawnRenderer.LayingFacing)),
				prefix: new HarmonyMethod(typeof(PawnOverlayRenderer),
				nameof(PawnOverlayRenderer.LayingFacing)));

			//Unit Tests
#if DEBUG
			Harmony.Patch(original: AccessTools.Method(typeof(DebugWindowsOpener), "DrawButtons"),
				postfix: new HarmonyMethod(typeof(ProjectSetup),
				nameof(DrawDebugWindowButton)));
			Harmony.Patch(original: AccessTools.Method(typeof(GameComponentUtility), nameof(GameComponentUtility.StartedNewGame)),
				postfix: new HarmonyMethod(typeof(StartupTest),
				nameof(StartupTest.ExecuteNewGameTesting)));
			Harmony.Patch(original: AccessTools.Method(typeof(GameComponentUtility), nameof(GameComponentUtility.LoadedGame)),
				postfix: new HarmonyMethod(typeof(StartupTest),
				nameof(StartupTest.ExecutePostLoadTesting)));
			Harmony.Patch(original: AccessTools.Method(typeof(UIRoot_Entry), nameof(UIRoot_Entry.Init)),
				postfix: new HarmonyMethod(typeof(StartupTest),
				nameof(StartupTest.ExecuteOnStartupTesting)));

			//Input handling (DEBUG)
			Harmony.Patch(original: AccessTools.Method(typeof(UIRoot_Entry), nameof(UIRoot_Entry.UIRootOnGUI)),
				prefix: new HarmonyMethod(typeof(MainMenuKeyBindHandler),
				nameof(MainMenuKeyBindHandler.HandleKeyInputs)));
			Harmony.Patch(original: AccessTools.Method(typeof(UIRoot_Play), nameof(UIRoot_Play.UIRootOnGUI)),
				prefix: new HarmonyMethod(typeof(MainMenuKeyBindHandler),
				nameof(MainMenuKeyBindHandler.HandleKeyInputs)));
#endif

			//Input handling
			Harmony.Patch(original: AccessTools.Method(typeof(WindowStack), nameof(WindowStack.Add)),
				postfix: new HarmonyMethod(typeof(HighPriorityInputs),
				nameof(HighPriorityInputs.WindowAddedToStack)));
			Harmony.Patch(original: AccessTools.Method(typeof(WindowStack), nameof(WindowStack.TryRemove), parameters: new Type[] { typeof(Window), typeof(bool) }),
				postfix: new HarmonyMethod(typeof(HighPriorityInputs),
				nameof(HighPriorityInputs.WindowRemovedFromStack)));
			Harmony.Patch(original: AccessTools.Method(typeof(WindowStack), nameof(WindowStack.HandleEventsHighPriority)),
				postfix: new HarmonyMethod(typeof(HighPriorityInputs),
				nameof(HighPriorityInputs.HighPriorityOnGUI)));

			//UI
			Harmony.Patch(original: AccessTools.Method(typeof(MainTabWindow_Inspect), nameof(MainTabWindow_Inspect.DoInspectPaneButtons)),
				prefix: new HarmonyMethod(typeof(ProjectSetup),
				nameof(InspectablePaneButtons)));

			//Mod Init
			StaticConstructorOnModInit();

			SceneManager.sceneLoaded += (scene, mode) => ClearCaches();
		}

		private static void RegisterParseableStructs()
		{
			ParseHelper.Parsers<Rot8>.Register(new Func<string, Rot8>(Rot8.FromString));
			ParseHelper.Parsers<Quadrant>.Register(new Func<string, Quadrant>(Quadrant.FromString));
			ParseHelper.Parsers<RimWorldTime>.Register(new Func<string, RimWorldTime>(RimWorldTime.FromString));
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
					SmashLog.Error($"Exception thrown running constructor of type <type>{type}</type>. Ex=\"{ex}\"");
				}
			}
		}

		private static void DrawDebugWindowButton(WidgetRow ___widgetRow, ref float ___widgetRowFinalX)
		{
			if (___widgetRow.ButtonIcon(TexButton.OpenDebugActionsMenu, "Open Startup Actions menu.\n\n This lets you initiate certain static methods on startup for quick testing."))
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

		private static void ClearCaches()
		{
			ComponentCache.ClearCache();
			ThreadManager.ReleaseAllActiveThreads();
		}

		private static void BackfillRegisteredAreas(Map __instance)
		{
			Ext_Map.TryAddAreas(__instance);
		}
	}
}
