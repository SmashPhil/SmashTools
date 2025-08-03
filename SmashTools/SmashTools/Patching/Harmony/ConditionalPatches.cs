using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Verse;

namespace SmashTools.Patching;

[PublicAPI]
public static class ConditionalPatches
{
  private static readonly Dictionary<string, List<ConditionalPatch.Result>> Patches = [];

  /// <summary>
  /// Apply all conditional patches for a mod
  /// </summary>
  internal static void RunAll()
  {
    List<Type> conditionalPatchTypes = typeof(ConditionalPatch).AllSubclassesNonAbstract();
    foreach (Type type in conditionalPatchTypes)
    {
      ConditionalPatch patch = (ConditionalPatch)Activator.CreateInstance(type, null);
      ConditionalPatch.Result result = new()
      {
        PackageId = patch.PackageId
      };
      if (Ext_Mods.GetActiveMod(patch.PackageId) is { } modMetaData)
      {
        Patches.AddOrAppend(patch.SourceId, result);
        try
        {
          result.FriendlyName = modMetaData.Name;
          patch.PatchAll(modMetaData, HarmonyPatcher.Harmony);
          result.Active = true;
        }
        catch (Exception ex)
        {
          Log.Error($"{ProjectSetup.LogLabel} Failed to apply compatibility patch {type.Name}.\n{ex}");
          result.Active = false;
          result.ExceptionThrown = ex;
        }
      }
    }
  }

  public static List<ConditionalPatch.Result> GetPatches(string sourceId)
  {
    return Patches.TryGetValue(sourceId);
  }
}