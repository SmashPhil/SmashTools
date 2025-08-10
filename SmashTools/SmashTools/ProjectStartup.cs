using System.Collections;
using SmashTools.Animations;
using SmashTools.Patching;
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
    // Need to wait for static constructor patches to all run so we don't miss any unpatches from bad timing.
    CoroutineManager.Instance.StartCoroutine(UnpatchAfterSeconds(UnpatchDelay));
#endif

#if ANIMATOR
    AnimationLoader.ResolveAllReferences();
#endif
  }

  private static IEnumerator UnpatchAfterSeconds(float seconds)
  {
    yield return new WaitForSeconds(seconds);
    HarmonyPatcher.RunUnpatches();
  }
}