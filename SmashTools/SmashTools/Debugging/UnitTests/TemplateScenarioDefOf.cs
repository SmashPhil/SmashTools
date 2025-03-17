using RimWorld;

namespace SmashTools.Debugging;

#if DEBUG
[DefOf]
#endif
internal class TemplateScenarioDefOf
{
  public static ScenarioDef TestScenario;

  static TemplateScenarioDefOf()
  {
    DefOfHelper.EnsureInitializedInCtor(typeof(TemplateScenarioDefOf));
  }
}