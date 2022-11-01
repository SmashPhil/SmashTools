using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using UnityEngine;
using HarmonyLib;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools.Xml;

namespace SmashTools
{
	public class ProjectSetup : Mod
	{
		public const string ProjectLabel = "[SmashTools]";
		public const string HarmonyId = "SmashPhil.SmashTools";

		public const bool ExportXmlDoc = false;

		public static Harmony Harmony { get; private set; }

		public ProjectSetup(ModContentPack content) : base(content)
		{
			RegisterParseableStructs();

			Harmony = new Harmony(HarmonyId);

			Harmony.Patch(original: AccessTools.Method(typeof(DirectXmlLoader), nameof(DirectXmlLoader.DefFromNode)),
				postfix: new HarmonyMethod(typeof(XmlParseHelper),
				nameof(XmlParseHelper.ReadCustomAttributesOnDef)));
			Harmony.Patch(original: AccessTools.Method(typeof(DirectXmlToObject), "GetFieldInfoForType"),
				postfix: new HarmonyMethod(typeof(XmlParseHelper),
				nameof(XmlParseHelper.ReadCustomAttributes)));

			Harmony.Patch(original: AccessTools.Method(typeof(ModContentPack), "LoadPatches"),
				prefix: new HarmonyMethod(typeof(XmlParseHelper),
				nameof(XmlParseHelper.PatchOperationsMayRequire)));
			Harmony.Patch(original: AccessTools.Method(typeof(LoadedModManager), nameof(LoadedModManager.ParseAndProcessXML)),
				prefix: new HarmonyMethod(typeof(XmlParseHelper),
				nameof(XmlParseHelper.ParseAndProcessXmlMayRequire)));

			if (ExportXmlDoc)
			{
#pragma warning disable CS0162 // Unreachable code detected
				Harmony.Patch(original: AccessTools.Method(typeof(LoadedModManager), nameof(LoadedModManager.ParseAndProcessXML)),
					postfix: new HarmonyMethod(typeof(XmlParseHelper),
					nameof(XmlParseHelper.ExportCombinedXmlDocument)));
#pragma warning restore CS0162 // Unreachable code detected
			}

			Harmony.Patch(original: AccessTools.Method(typeof(EditWindow_Log), "DoMessageDetails"),
				transpiler: new HarmonyMethod(typeof(SmashLog),
				nameof(SmashLog.RemoveRichTextTranspiler)));

			Harmony.Patch(original: AccessTools.Method(typeof(Map), nameof(Map.ExposeData)),
				prefix: new HarmonyMethod(typeof(ComponentCache),
				nameof(ComponentCache.ClearAllMapComps)));
			Harmony.Patch(original: AccessTools.Method(typeof(MapGenerator), nameof(MapGenerator.GenerateContentsIntoMap)),
				prefix: new HarmonyMethod(typeof(ComponentCache),
				nameof(ComponentCache.MapGenerated)));
			Harmony.Patch(original: AccessTools.Method(typeof(MapDeiniter), nameof(MapDeiniter.Deinit)),
				postfix: new HarmonyMethod(typeof(ComponentCache),
				nameof(ComponentCache.ClearMapComps), new Type[] { typeof(Map) }));
			Harmony.Patch(original: AccessTools.Method(typeof(Game), nameof(Game.AddMap)),
				postfix: new HarmonyMethod(typeof(ComponentCache),
				nameof(ComponentCache.RegisterMapComps)));
			Harmony.Patch(original: AccessTools.Method(typeof(World), "FillComponents"),
				postfix: new HarmonyMethod(typeof(ComponentCache),
				nameof(ComponentCache.ConstructWorldComponents)));
			Harmony.Patch(original: AccessTools.Method(typeof(Game), "FillComponents"),
				postfix: new HarmonyMethod(typeof(ComponentCache),
				nameof(ComponentCache.ConstructGameComponents)));

			Harmony.Patch(original: AccessTools.Method(typeof(PawnRenderer), "GetBodyPos"),
				prefix: new HarmonyMethod(typeof(PawnOverlayRenderer),
				nameof(PawnOverlayRenderer.GetBodyPos)));
			Harmony.Patch(original: AccessTools.Method(typeof(PawnRenderer), nameof(PawnRenderer.BodyAngle)),
				prefix: new HarmonyMethod(typeof(PawnOverlayRenderer),
				nameof(PawnOverlayRenderer.BodyAngle)));
			Harmony.Patch(original: AccessTools.Method(typeof(PawnUtility), nameof(PawnUtility.GetPosture)),
				prefix: new HarmonyMethod(typeof(PawnOverlayRenderer),
				nameof(PawnOverlayRenderer.PawnOverlayerPosture)));
			Harmony.Patch(original: AccessTools.Method(typeof(PawnRenderer), nameof(PawnRenderer.LayingFacing)),
				prefix: new HarmonyMethod(typeof(PawnOverlayRenderer),
				nameof(PawnOverlayRenderer.LayingFacing)));

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

			Harmony.Patch(original: AccessTools.Method(typeof(UIRoot_Entry), nameof(UIRoot_Entry.UIRootOnGUI)),
				prefix: new HarmonyMethod(typeof(MainMenuKeyBindHandler),
				nameof(MainMenuKeyBindHandler.HandleKeyInputs)));
			Harmony.Patch(original: AccessTools.Method(typeof(UIRoot_Play), nameof(UIRoot_Play.UIRootOnGUI)),
				prefix: new HarmonyMethod(typeof(MainMenuKeyBindHandler),
				nameof(MainMenuKeyBindHandler.HandleKeyInputs)));

			Harmony.Patch(original: AccessTools.Method(typeof(MainTabWindow_Inspect), nameof(MainTabWindow_Inspect.DoInspectPaneButtons)),
				prefix: new HarmonyMethod(typeof(ProjectSetup),
				nameof(InspectablePaneButtons)));

			StaticConstructorOnModInit();
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
				float num = rect.width - 48f;
				if (UIElements.InfoCardButton(new Rect(num, 0f, 30, 30)))
				{
					Find.WindowStack.Add(new Dialog_InspectWindow(inspectable));
				}
				num -= 30;
				lineEndWidth += 24f + inspectable.DoInspectPaneButtons(num);
				return false;
			}
			return true;
		}
	}
}
