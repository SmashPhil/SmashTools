using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using UnityEngine;
using HarmonyLib;
using Verse;
using RimWorld;
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

			harmony.Patch(original: AccessTools.Method(typeof(PawnRenderer), "GetBodyPos"),
				prefix: new HarmonyMethod(typeof(PawnOverlayRenderer),
				nameof(PawnOverlayRenderer.GetBodyPos)));
			harmony.Patch(original: AccessTools.Method(typeof(PawnRenderer), nameof(PawnRenderer.BodyAngle)),
				prefix: new HarmonyMethod(typeof(PawnOverlayRenderer),
				nameof(PawnOverlayRenderer.BodyAngle)));
			harmony.Patch(original: AccessTools.Method(typeof(PawnUtility), nameof(PawnUtility.GetPosture)),
				prefix: new HarmonyMethod(typeof(PawnOverlayRenderer),
				nameof(PawnOverlayRenderer.PawnOverlayerPosture)));
			harmony.Patch(original: AccessTools.Method(typeof(PawnRenderer), nameof(PawnRenderer.LayingFacing)),
				prefix: new HarmonyMethod(typeof(PawnOverlayRenderer),
				nameof(PawnOverlayRenderer.LayingFacing)));

			harmony.Patch(original: AccessTools.Method(typeof(DebugWindowsOpener), "DrawButtons"),
				postfix: new HarmonyMethod(typeof(UnitTesting),
				nameof(UnitTesting.DrawDebugWindowButton)));
			harmony.Patch(original: AccessTools.Method(typeof(Game), nameof(Game.InitNewGame)),
				postfix: new HarmonyMethod(typeof(UnitTesting),
				nameof(UnitTesting.ExecuteNewGameTesting)));
			harmony.Patch(original: AccessTools.Method(typeof(Game), nameof(Game.LoadGame)),
				postfix: new HarmonyMethod(typeof(UnitTesting),
				nameof(UnitTesting.ExecutePostLoadTesting)));
			harmony.Patch(original: AccessTools.Method(typeof(UIRoot_Entry), nameof(UIRoot_Entry.Init)),
				postfix: new HarmonyMethod(typeof(UnitTesting),
				nameof(UnitTesting.ExecuteOnStartupTesting)));

			harmony.Patch(original: AccessTools.Method(typeof(MainTabWindow_Inspect), nameof(MainTabWindow_Inspect.DoInspectPaneButtons)),
				prefix: new HarmonyMethod(typeof(ProjectSetup),
				nameof(InspectablePaneButtons)));

			harmony.Patch(original: AccessTools.Method(typeof(StaticConstructorOnStartupUtility), nameof(StaticConstructorOnStartupUtility.ReportProbablyMissingAttributes)),
				transpiler: new HarmonyMethod(typeof(ProjectSetup),
				nameof(IgnoreWarningsOnStaticClassTranspiler)));

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
					SmashLog.Error($"Exception thrown running constructor of type <type>{type}</type>. Ex=\"{ex.Message}\"");
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
				lineEndWidth += 24f;
				inspectable.DoInspectPaneButtons(num);
				return false;
			}
			return true;
		}

		private static IEnumerable<CodeInstruction> IgnoreWarningsOnStaticClassTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> instructionList = instructions.ToList();

			for (int i = 0; i < instructionList.Count; i++)
			{
				CodeInstruction instruction = instructionList[i];

				if (instruction.Calls(AccessTools.Method(typeof(GenAttribute), nameof(GenAttribute.HasAttribute), generics: new Type[] { typeof(StaticConstructorOnStartup) })))
				{
					yield return instruction;	//Call | GenAttribute.HasAttribute<StaticConstructorOnStartup>()
					instruction = instructionList[++i];
					
					yield return new CodeInstruction(opcode: OpCodes.Ldloc_2);
					yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(ProjectSetup), nameof(SuppressMainThreadWarning)));
				}

				yield return instruction;
			}
		}

		private static bool SuppressMainThreadWarning(bool hasStaticCtorAttribute, Type type)
		{
			return hasStaticCtorAttribute || type.HasAttribute<IsMainThreadAttribute>();
		}
	}
}
