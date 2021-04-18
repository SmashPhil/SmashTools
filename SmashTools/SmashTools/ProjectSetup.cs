using System;
using HarmonyLib;
using Verse;
using RimWorld.Planet;
using SmashTools.Xml;

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

			harmony.Patch(original: AccessTools.Method(typeof(World), "FillComponents"),
				postfix: new HarmonyMethod(typeof(ComponentCache),
				nameof(ComponentCache.ConstructWorldComponents)));
			harmony.Patch(original: AccessTools.Method(typeof(Game), "FillComponents"),
				postfix: new HarmonyMethod(typeof(ComponentCache),
				nameof(ComponentCache.ConstructGameComponents)));
		}

		private static void RegisterParseableStructs()
		{
			ParseHelper.Parsers<Rot8>.Register(new Func<string, Rot8>(Rot8.FromString));
			ParseHelper.Parsers<Quadrant>.Register(new Func<string, Quadrant>(Quadrant.FromString));
			ParseHelper.Parsers<RimWorldTime>.Register(new Func<string, RimWorldTime>(RimWorldTime.FromString));
		}
	}
}
