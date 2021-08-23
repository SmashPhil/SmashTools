using System;
using HarmonyLib;
using Verse;
using RimWorld.Planet;
using SmashTools.Xml;
using SmashTools.Debugging;

namespace SmashTools
{
	public class ProjectSetup : Mod
	{
		public const string ProjectLabel = "[SmashTools]";

		public ProjectSetup(ModContentPack content) : base(content)
		{
			RegisterParseableStructs();

			Harmony harmony = new Harmony("smashphil.smashtools");

			harmony.Patch(original: AccessTools.Method(typeof(DirectXmlLoader), nameof(DirectXmlLoader.DefFromNode)),
				postfix: new HarmonyMethod(typeof(XmlParseHelper),
				nameof(XmlParseHelper.ReadCustomAttributesOnDef)));
			harmony.Patch(original: AccessTools.Method(typeof(DirectXmlToObject), "GetFieldInfoForType"),
				postfix: new HarmonyMethod(typeof(XmlParseHelper),
				nameof(XmlParseHelper.ReadCustomAttributes)));
			harmony.Patch(original: AccessTools.Method(typeof(EditWindow_Log), "DoMessageDetails"),
				transpiler: new HarmonyMethod(typeof(SmashLog),
				nameof(SmashLog.RemoveRichTextTranspiler)));

			harmony.Patch(original: AccessTools.Method(typeof(MapDeiniter), nameof(MapDeiniter.Deinit)),
				postfix: new HarmonyMethod(typeof(ComponentCache),
				nameof(ComponentCache.ClearMapComps)));
			harmony.Patch(original: AccessTools.Method(typeof(Game), nameof(Game.AddMap)),
				postfix: new HarmonyMethod(typeof(ComponentCache),
				nameof(ComponentCache.RegisterMapComps)));
			harmony.Patch(original: AccessTools.Method(typeof(World), "FillComponents"),
				postfix: new HarmonyMethod(typeof(ComponentCache),
				nameof(ComponentCache.ConstructWorldComponents)));
			harmony.Patch(original: AccessTools.Method(typeof(Game), "FillComponents"),
				postfix: new HarmonyMethod(typeof(ComponentCache),
				nameof(ComponentCache.ConstructGameComponents)));
			harmony.Patch(original: AccessTools.Method(typeof(DebugWindowsOpener), "DrawButtons"),
				postfix: new HarmonyMethod(typeof(UnitTesting),
				nameof(UnitTesting.DrawDebugWindowButton)));

			if (Prefs.DevMode)
			{
				harmony.Patch(original: AccessTools.Method(typeof(MapGenerator), nameof(MapGenerator.GenerateMap)),
					postfix: new HarmonyMethod(typeof(UnitTesting),
					nameof(UnitTesting.ExecuteNewGameTesting)));
				harmony.Patch(original: AccessTools.Method(typeof(Game), nameof(Game.LoadGame)),
					postfix: new HarmonyMethod(typeof(UnitTesting),
					nameof(UnitTesting.ExecutePostLoadTesting)));
				harmony.Patch(original: AccessTools.Method(typeof(UIRoot_Entry), nameof(UIRoot_Entry.Init)),
					postfix: new HarmonyMethod(typeof(UnitTesting),
					nameof(UnitTesting.ExecuteOnStartupTesting)));
			}
		}

		private static void RegisterParseableStructs()
		{
			ParseHelper.Parsers<Rot8>.Register(new Func<string, Rot8>(Rot8.FromString));
			ParseHelper.Parsers<Quadrant>.Register(new Func<string, Quadrant>(Quadrant.FromString));
			ParseHelper.Parsers<RimWorldTime>.Register(new Func<string, RimWorldTime>(RimWorldTime.FromString));
		}
	}
}
