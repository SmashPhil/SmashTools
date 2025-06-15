using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Verse;

namespace SmashTools;

[StaticConstructorOnStartup]
public static class ComponentCache
{
  private static readonly List<Type> priorityComponentTypes;
  private static readonly List<Type> detachedComponentTypes;

  static ComponentCache()
  {
    priorityComponentTypes = typeof(MapComponent).AllSubclassesNonAbstract().ToList();
    detachedComponentTypes = typeof(DetachedMapComponent).AllSubclassesNonAbstract().ToList();
  }

  internal static int PriorityComponentTypeCount => priorityComponentTypes.Count;

  internal static int DetachedComponentTypeCount => detachedComponentTypes.Count;


  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static T GetCachedMapComponent<T>(this Map map) where T : MapComponent
  {
    return MapComponentCache<T>.GetComponent(map);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static T GetDetachedMapComponent<T>(this Map map) where T : DetachedMapComponent
  {
    return DetachedMapComponentCache<T>.GetComponent(map);
  }

  internal static void PreCache(Map map)
  {
    foreach (Type type in detachedComponentTypes)
    {
      GenGeneric.InvokeStaticMethodOnGenericType(typeof(DetachedMapComponentCache<>), type,
        nameof(DetachedMapComponentCache<DetachedMapComponent>.AddComponent), map);
    }
  }

  internal static void ClearMap(Map map)
  {
    foreach (Type type in priorityComponentTypes)
    {
      GenGeneric.InvokeStaticMethodOnGenericType(typeof(MapComponentCache<>), type,
        nameof(MapComponentCache<MapComponent>.ClearMap), map);
    }
    foreach (Type type in detachedComponentTypes)
    {
      GenGeneric.InvokeStaticMethodOnGenericType(typeof(DetachedMapComponentCache<>), type,
        nameof(DetachedMapComponentCache<DetachedMapComponent>.ClearMap), map);
    }
  }

  internal static void ClearAll()
  {
    foreach (Type type in priorityComponentTypes)
    {
      GenGeneric.InvokeStaticMethodOnGenericType(typeof(MapComponentCache<>), type,
        nameof(MapComponentCache<MapComponent>.ClearAll));
    }
    foreach (Type type in detachedComponentTypes)
    {
      GenGeneric.InvokeStaticMethodOnGenericType(typeof(DetachedMapComponentCache<>), type,
        nameof(DetachedMapComponentCache<DetachedMapComponent>.ClearAll));
    }
  }

  public static int PriorityComponentCount()
  {
    int count = 0;
    foreach (Type type in priorityComponentTypes)
    {
      count += (int)GenGeneric.InvokeStaticMethodOnGenericType(typeof(MapComponentCache<>), type,
        nameof(MapComponentCache<MapComponent>.Count));
    }
    return count;
  }

  public static int DetachedComponentCount()
  {
    int count = 0;
    foreach (Type type in detachedComponentTypes)
    {
      count += (int)GenGeneric.InvokeStaticMethodOnGenericType(typeof(DetachedMapComponentCache<>),
        type,
        nameof(DetachedMapComponentCache<DetachedMapComponent>.Count));
    }
    return count;
  }
}