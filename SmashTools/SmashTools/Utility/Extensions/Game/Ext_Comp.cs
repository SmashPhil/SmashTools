using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using Verse;

namespace SmashTools
{
  public static class Ext_Comp
  {
    private static readonly FieldInfo compList = AccessTools.Field(typeof(ThingWithComps), "comps");

    /// <summary>
    /// Adds <paramref name="comp"/> to <paramref name="thingWithComps"/> and inits inner 'comps' list if empty.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="thingWithComps"></param>
    /// <param name="comp"></param>
    [Pure]
    public static bool TryAddComp<T>(this ThingWithComps thingWithComps, T comp) where T : ThingComp
    {
      try
      {
        thingWithComps.EnsureUncachedCompList();
        thingWithComps.AllComps.Add(comp);
        return true;
      }
      catch (Exception ex)
      {
        SmashLog.Error(
          $"Exception thrown while trying to reflectively add <type>{comp.GetType()}</type> to {thingWithComps}.\nException={ex}");
      }
      return false;
    }

    /// <summary>
    /// Assigns new list reference to comps field of <paramref name="thingWithComps"/>, rather than using an empty static list shared among all instances
    /// </summary>
    public static bool EnsureUncachedCompList(this ThingWithComps thingWithComps)
    {
      try
      {
        var compListInstance = (List<ThingComp>)compList.GetValue(thingWithComps);
        if (compListInstance == null)
        {
          compList.SetValue(thingWithComps, new List<ThingComp>());
          return true;
        }
      }
      catch (Exception ex)
      {
        SmashLog.Error(
          $"Exception thrown while trying to uncache compList for {thingWithComps}.\nException={ex}");
      }
      return false;
    }
  }
}