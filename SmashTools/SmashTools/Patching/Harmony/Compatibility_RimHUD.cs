using System;
using System.Reflection;
using HarmonyLib;
using Verse;
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
			Type classType = AccessTools.TypeByName("RimHUD.Interface.InspectPanePlus");
			harmony.Patch(AccessTools.Method(classType, "DrawButtons"),
				postfix: new HarmonyMethod(typeof(Compatibility_RimHUD),
				nameof(DrawButtonsOnRimHUD)));
		}

		public static void DrawButtonsOnRimHUD(Rect rect, ref float lineEndWidth)
		{
			if (Find.Selector.SingleSelectedThing is IInspectable inspectable)
			{
				lineEndWidth += inspectable.DoInspectPaneButtons(rect.width - lineEndWidth);
			}
		}
	}
}
