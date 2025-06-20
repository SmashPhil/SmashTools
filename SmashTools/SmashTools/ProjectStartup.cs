using SmashTools.Animations;
using SmashTools.Patching;
using SmashTools.Xml;
using Verse;

namespace SmashTools;

[StaticConstructorOnStartup]
public static class ProjectStartup
{
  static ProjectStartup()
  {
    HarmonyPatcher.Run(PatchSequence.PostDefDatabase);
    DelayedCrossRefResolver.ResolveAll();
    AnimationLoader.ResolveAllReferences();
    HarmonyPatcher.DumpPatchReport();
  }
}