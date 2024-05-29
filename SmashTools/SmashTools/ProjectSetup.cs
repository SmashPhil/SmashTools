using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using HarmonyLib;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools.Xml;
using SmashTools.Performance;
using LudeonTK;

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

			//Game Init
			Harmony.Patch(original: AccessTools.Method(typeof(GameComponentUtility), nameof(GameComponentUtility.FinalizeInit)),
				postfix: new HarmonyMethod(typeof(StaticConstructorOnGameInitAttribute),
				nameof(StaticConstructorOnGameInitAttribute.RunGameInitStaticConstructors)));

			//Logging
			Harmony.Patch(original: AccessTools.Method(typeof(EditWindow_Log), "DoMessageDetails"),
				transpiler: new HarmonyMethod(typeof(SmashLog),
				nameof(SmashLog.RemoveRichTextTranspiler)));

			//Component Cache + DetachedMapComponent
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

			//IThingHolderPawnOverlayer
			Harmony.Patch(original: AccessTools.Method(typeof(PawnRenderer), "GetBodyPos"),
				transpiler: new HarmonyMethod(typeof(PawnOverlayRenderer),
				nameof(PawnOverlayRenderer.ShowBodyTranspiler)));
			Harmony.Patch(original: AccessTools.Method(typeof(PawnRenderer), nameof(PawnRenderer.LayingFacing)),
				prefix: new HarmonyMethod(typeof(PawnOverlayRenderer),
				nameof(PawnOverlayRenderer.LayingFacing)));

			//Unit Tests
			Harmony.Patch(original: AccessTools.Method(typeof(DebugWindowsOpener), "DrawButtons"),
				postfix: new HarmonyMethod(typeof(UnitTesting),
				nameof(UnitTesting.DrawDebugWindowButton)));
			Harmony.Patch(original: AccessTools.Method(typeof(GameComponentUtility), nameof(GameComponentUtility.StartedNewGame)),
				postfix: new HarmonyMethod(typeof(UnitTesting),
				nameof(UnitTesting.ExecuteNewGameTesting)));
			Harmony.Patch(original: AccessTools.Method(typeof(GameComponentUtility), nameof(GameComponentUtility.LoadedGame)),
				postfix: new HarmonyMethod(typeof(UnitTesting),
				nameof(UnitTesting.ExecutePostLoadTesting)));
			Harmony.Patch(original: AccessTools.Method(typeof(UIRoot_Entry), nameof(UIRoot_Entry.Init)),
				postfix: new HarmonyMethod(typeof(UnitTesting),
				nameof(UnitTesting.ExecuteOnStartupTesting)));

			//Input handling
			Harmony.Patch(original: AccessTools.Method(typeof(UIRoot_Entry), nameof(UIRoot_Entry.UIRootOnGUI)),
				prefix: new HarmonyMethod(typeof(MainMenuKeyBindHandler),
				nameof(MainMenuKeyBindHandler.HandleKeyInputs)));
			Harmony.Patch(original: AccessTools.Method(typeof(UIRoot_Play), nameof(UIRoot_Play.UIRootOnGUI)),
				prefix: new HarmonyMethod(typeof(MainMenuKeyBindHandler),
				nameof(MainMenuKeyBindHandler.HandleKeyInputs)));

			//UI
			Harmony.Patch(original: AccessTools.Method(typeof(MainTabWindow_Inspect), nameof(MainTabWindow_Inspect.DoInspectPaneButtons)),
				prefix: new HarmonyMethod(typeof(ProjectSetup),
				nameof(InspectablePaneButtons)));
			Harmony.Patch(original: AccessTools.Method(typeof(WindowStack), nameof(WindowStack.Notify_ClickedInsideWindow)),
				prefix: new HarmonyMethod(typeof(ProjectSetup),
				nameof(HandleSingleWindowDialogs)));

			//Debugging
			if (Prefs.DevMode)
			{
				Harmony.Patch(original: AccessTools.Method(typeof(UIRoot), nameof(UIRoot.UIRootOnGUI)),
					postfix: new HarmonyMethod(typeof(ProjectSetup),
					nameof(ValidateGUIState)));
			}

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

		private static bool InspectablePaneButtons(Rect rect, ref float lineEndWidth)
		{
			if (Find.Selector.SingleSelectedThing is IInspectable inspectable)
			{
				lineEndWidth += 30;
				if (UIElements.InfoCardButton(new Rect(rect.width - lineEndWidth, 0f, 30, 30)))
				{
					Find.WindowStack.Add(new Dialog_InspectWindow(inspectable));
				}
				lineEndWidth += inspectable.DoInspectPaneButtons(rect.width - lineEndWidth);
				return false;
			}
			return true;
		}

		private static void HandleSingleWindowDialogs(Window window, WindowStack __instance)
		{
			if (Event.current.type == EventType.MouseDown)
			{
				if (window is null || !(window is SingleWindow) && (__instance.GetWindowAt(UI.GUIToScreenPoint(Event.current.mousePosition)) != SingleWindow.CurrentlyOpenedWindow))
				{
					if (SingleWindow.CurrentlyOpenedWindow != null && SingleWindow.CurrentlyOpenedWindow.closeOnAnyClickOutside)
					{
						Find.WindowStack.TryRemove(SingleWindow.CurrentlyOpenedWindow);
					}
				}
			}
		}

		private static void ValidateGUIState()
		{
			if (!GUIState.Empty)
			{
				Log.Error($"GUIState is not empty on end of frame.  GUIStates need to be popped from the stack when the containing method is finished.");

				while (!GUIState.Empty) GUIState.Pop();
			}
		}

		private static void ClearCaches()
		{
			ComponentCache.ClearCache();
			ThreadManager.ReleaseAllActiveThreads();
		}
	}
}
