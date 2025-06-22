using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Verse;

namespace SmashTools.Patching;

[PublicAPI]
public static class ConditionalPatches
{
  private static readonly Dictionary<string, List<ConditionalPatch.Result>> patches = [];

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
      if (ModLister.GetActiveModWithIdentifier(patch.PackageId, ignorePostfix: true) is
        { } modMetaData)
      {
        patches.AddOrAppend(patch.SourceId, result);
        try
        {
          result.FriendlyName = modMetaData.Name;
          patch.PatchAll(modMetaData, HarmonyPatcher.Harmony);
          result.Active = true;
        }
        catch (Exception ex)
        {
          Log.Error($"{ProjectSetup.LogLabel} Failed to apply patch {type}.\n{ex}");
          result.Active = false;
          result.ExceptionThrown = ex;
        }
      }
    }
  }

  public static List<ConditionalPatch.Result> GetPatches(string sourceId)
  {
    return patches.TryGetValue(sourceId);
  }
}