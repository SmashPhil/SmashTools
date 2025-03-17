using RimWorld;
using Verse;

namespace SmashTools.Debugging;

#if DEBUG
[DefOf]
#endif
internal class MapGeneratorDefOf
{
  public static MapGeneratorDef TestMapGenerator;

  static MapGeneratorDefOf()
  {
    DefOfHelper.EnsureInitializedInCtor(typeof(MapGeneratorDefOf));
  }
}