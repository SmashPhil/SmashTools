using System;
using UnityEngine.Assertions;
using Verse;

namespace SmashTools;

public static class MapComponentCache<T> where T : MapComponent
{
  private const int CompCacheSize = sbyte.MaxValue;

  private static readonly T[] MapComps = new T[CompCacheSize];

  // NOTE - Map::get_Index ends up iterating through the global map list to search for THIS specific
  // map, but the player will usually have less than 3~4 maps open at a time. This is roughly the
  // cutoff between an array lookup time taking more time than a dictionary. It's faster in almost
  // all cases to fetch the index and then use that for an array lookup.
  public static T GetComponent(Map map)
  {
    T component = MapComps[map.Index];
    if (component == null)
    {
      component = map.GetComponent<T>();
      MapComps[map.Index] = component;
    }
    Assert.AreEqual(map, component.map);
    return component;
  }

  public static void ClearMap(Map map)
  {
    MapComps[map.Index] = null;
  }

  public static void ClearAll()
  {
    Array.Clear(MapComps, 0, CompCacheSize);
  }

  internal static T GetComponent(int index)
  {
    return MapComps[index];
  }

  internal static int Count()
  {
    return MapComps.CountWhere(item => item is not null);
  }
}