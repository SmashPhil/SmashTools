using SmashTools.Animations;
using SmashTools.Debugging;
using SmashTools.Xml;
using Verse;

namespace SmashTools
{
  [StaticConstructorOnStartup]
  public static class ProjectStartup
  {
    static ProjectStartup()
    {
      AssertHandler.Enable();
      DelayedCrossRefResolver.ResolveAll();
      AnimationLoader.ResolveAllReferences();
    }
  }
}