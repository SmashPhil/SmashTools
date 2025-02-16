using SmashTools.Animations;
using Verse;

namespace SmashTools
{
  [StaticConstructorOnStartup]
  public static class ProjectStartup
  {
    static ProjectStartup()
    {
      AnimationLoader.ResolveAllReferences();
    }
  }
}
