using RimWorld;
using Verse;

namespace SmashTools.UnitTesting;

[DefOf]
internal class MapGeneratorDefOf
{
  public static MapGeneratorDef TestMapGenerator;

  static MapGeneratorDefOf()
  {
    DefOfHelper.EnsureInitializedInCtor(typeof(MapGeneratorDefOf));
  }
}