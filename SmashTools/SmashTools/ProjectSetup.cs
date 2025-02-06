using System;
using System.Runtime.CompilerServices;
using System.Threading;
using HarmonyLib;
using LudeonTK;
using RimWorld;
using SmashTools.Performance;
using SmashTools.Xml;
using UnityEngine;
using UnityEngine.SceneManagement;
using Verse;
using Verse.Profile;

namespace SmashTools
{
	public class ProjectSetup : Mod
	{
		public const string ProjectLabel = "[SmashTools]";
		public const string HarmonyId = "SmashPhil.SmashTools";

		public static event Action onNewGame;
		public static event Action onLoadGame;
		public static event Action onMainMenu;

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
			Harmony.Patch(original: AccessTools.Method(typeof(Game), nameof(Game.InitNewGame)),
				prefix: new HarmonyMethod(typeof(ComponentCache),
				nameof(ComponentCache.ClearCache)));
			Harmony.Patch(original: AccessTools.Method(typeof(MemoryUtility), nameof(MemoryUtility.ClearAllMapsAndWorld)),
				prefix: new HarmonyMethod(typeof(ThreadManager),
				nameof(ThreadManager.ReleaseThreadsAndClearCache)));
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
				postfix: new HarmonyMethod(typeof(ProjectSetup),
				nameof(OnNewGame)));
			Harmony.Patch(original: AccessTools.Method(typeof(GameComponentUtility), nameof(GameComponentUtility.LoadedGame)),
				postfix: new HarmonyMethod(typeof(ProjectSetup),
				nameof(OnLoadGame)));
			Harmony.Patch(original: AccessTools.Method(typeof(UIRoot_Entry), nameof(UIRoot_Entry.Init)),
				postfix: new HarmonyMethod(typeof(ProjectSetup),
				nameof(OnMainMenu)));

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

			SceneManager.sceneLoaded += (scene, mode) => ThreadManager.ReleaseThreadsAndClearCache();
#if DEBUG
			onNewGame += StartupTest.ExecuteNewGameTesting;
			onLoadGame += StartupTest.ExecutePostLoadTesting;
			onMainMenu += StartupTest.ExecuteOnStartupTesting;
#endif
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

		private static void OnNewGame()
		{
			onNewGame?.Invoke();
		}

		private static void OnLoadGame()
		{
			onLoadGame?.Invoke();
		}

		private static void OnMainMenu()
		{
			onMainMenu?.Invoke();
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
			Ext_Map.TryAddAreas(__instance);
		}
	}
}
