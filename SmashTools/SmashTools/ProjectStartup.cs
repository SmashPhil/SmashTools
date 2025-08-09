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

#if ANIMATOR
    AnimationLoader.ResolveAllReferences();
#endif
  }
}