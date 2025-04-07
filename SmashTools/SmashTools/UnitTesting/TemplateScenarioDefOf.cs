using RimWorld;

namespace SmashTools.UnitTesting;

[DefOf]
internal class TemplateScenarioDefOf
{
  public static ScenarioDef TestScenario;

  static TemplateScenarioDefOf()
  {
    DefOfHelper.EnsureInitializedInCtor(typeof(TemplateScenarioDefOf));
  }
}