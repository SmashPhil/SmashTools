using System;
using System.Collections.Generic;
using Verse;

namespace SmashTools;

public static class DetachedMapComponentCache<T> where T : DetachedMapComponent
{
  private static readonly Dictionary<int, T> MapComps = [];

  public static void AddComponent(Map map)
  {
    T component = (T)Activator.CreateInstance(typeof(T), map);
    MapComps.Add(map.uniqueID, component);
  }

  public static T GetComponent(Map map)
  {
    return MapComps[map.uniqueID];
  }

  public static void ClearMap(Map map)
  {
    MapComps.Remove(map.uniqueID);
  }

  public static void ClearAll()
  {
    MapComps.Clear();
  }

  internal static int Count()
  {
    return MapComps.Count;
  }
}