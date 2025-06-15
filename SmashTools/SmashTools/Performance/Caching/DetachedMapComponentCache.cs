using System;
using Verse;

namespace SmashTools;

public static class DetachedMapComponentCache<T> where T : DetachedMapComponent
{
  private static T[] mapComps = new T[sbyte.MaxValue];

  public static void AddComponent(Map map)
  {
    mapComps[map.Index] = (T)Activator.CreateInstance(typeof(T), map);
  }

  public static T GetComponent(Map map)
  {
    return mapComps[map.Index];
  }

  public static void ClearMap(Map map)
  {
    // Free up cached component so it can be fetched when index is reused
    mapComps[map.Index] = null;
  }

  public static void ClearAll()
  {
    mapComps = new T[sbyte.MaxValue];
  }

  internal static int Count()
  {
    return mapComps.CountWhere(item => item is not null);
  }
}