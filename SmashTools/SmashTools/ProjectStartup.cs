using SmashTools.Animations;
using SmashTools.Xml;
using Verse;

namespace SmashTools
{
  [StaticConstructorOnStartup]
  public static class ProjectStartup
  {
    static ProjectStartup()
    {
      DelayedCrossRefResolver.ResolveAll();
      AnimationLoader.ResolveAllReferences();
    }
  }
}
