using System;
using System.Reflection;
using HarmonyLib;
using Verse;
using RimWorld;
using SmashTools;
using UnityEngine;

namespace SmashTools
{
	internal class Compatibility_RimHUD : ConditionalPatch
	{
		public override string PackageId => "Jaxe.RimHUD";

		public override string SourceId => ProjectSetup.HarmonyId;

		public override void PatchAll(ModMetaData mod, Harmony harmony)
		{
			Type classType = AccessTools.TypeByName("RimHUD.Interface.Screen.InspectPaneButtons");
			harmony.Patch(AccessTools.Method(classType, "Draw"),
				postfix: new HarmonyMethod(typeof(Compatibility_RimHUD),
				nameof(DrawButtonsOnRimHUD)));
		}

		public static void DrawButtonsOnRimHUD(Rect bounds, IInspectPane pane, ref float offset)
		{
			if (Find.Selector.SingleSelectedThing is IInspectable inspectable)
			{
				offset += inspectable.DoInspectPaneButtons(bounds.width - offset);
			}
		}
	}
}
