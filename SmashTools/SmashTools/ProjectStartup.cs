using System.Collections;
using HarmonyLib;
using SmashTools.Animations;
using SmashTools.Patching;
using SmashTools.Performance;
using SmashTools.Xml;
using UnityEngine;
using Verse;

namespace SmashTools;

[StaticConstructorOnStartup]
public static class ProjectStartup
{
	private const float UnpatchDelay = 3;

	static ProjectStartup()
	{
		HarmonyPatcher.Run(PatchSequence.PostDefDatabase);
		DelayedCrossRefResolver.ResolveAll();

#if DEBUG
		HarmonyPatcher.Patch(
			original: AccessTools.Method(typeof(DebugWindowsOpener), "DrawButtons"),
			postfix: new HarmonyMethod(typeof(ProjectStartup),
				nameof(DrawDebugWindowButton)));

		// Input handling
		UnityThread.StartGUI(MainMenuKeyBindHandler.HandleKeyInputs);

		// Need to wait for static constructor patches to all run so we don't miss any unpatches from bad timing.
		CoroutineManager.Instance.StartCoroutine(UnpatchAfterSeconds(UnpatchDelay));
#endif

#if ANIMATOR
    AnimationLoader.ResolveAllReferences();
#endif

		ConditionalPatches.DumpPatchReport();
		HarmonyPatcher.DumpPatchReport();
	}

	private static IEnumerator UnpatchAfterSeconds(float seconds)
	{
		yield return new WaitForSeconds(seconds);
		HarmonyPatcher.RunUnpatches();
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
}